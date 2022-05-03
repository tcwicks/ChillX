/*
ChillX Framework Library
Copyright (C) 2022  Tikiri Chintana Wickramasingha 

Contact Details: (info at chillx dot com)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ChillX.Threading.APIProcessor
{
    /// <summary>
    /// Overall throughput is slower than ThreadedWorkItemProcessor implementation.
    /// However this implementation uses a single foreground scheduler thread and uses the background threadpool for worker threads
    /// Unless you are trying to process a large number of micro workloads as fast as possible this would be the better implementation
    /// </summary>
    public class AsyncThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>
        where TClientID : IComparable, IConvertible
        where TPriority : struct, IComparable, IFormattable, IConvertible
    {
        internal delegate bool Handler_GetNextPendingWorkItem(out ThreadWorkItem<TRequest, TResponse, TClientID> workItem);
        public delegate TResponse Handler_ProcessRequest(ThreadWorkItem<TRequest, TResponse, TClientID> request);
        public delegate void Handler_OnRequestProcessed(ThreadWorkItem<TRequest, TResponse, TClientID> workItem);
        internal delegate void Handler_OnThreadExit(int ID);
        public delegate void Handler_LogError(Exception ex);
        public delegate void Handler_LogMessage(string _message);

        public bool MaxWorkItemLimitPerClientEnabled { get; private set; } = true;
        public int MaxWorkItemLimitPerClient { get; private set; } = 100;
        public int MaxWorkerThreads { get; private set; } = 2;
        public int ThreadStartupDelayPerWorkItems { get; private set; } = 2;
        public int ThreadStartupMinQueueSize { get; private set; } = 2;
        public int IdleWorkerThreadExitMS { get; private set; } = 1000;
        public int AbandonedResponseExpiryMS { get; private set; } = 60000;
        public bool DiscardCompletedWorkItems { get; private set; } = false;
        private Handler_ProcessRequest OnProcessRequest;
        private Handler_LogError OnLogError;
        private Handler_LogMessage OnLogMessage;

        /// <summary>
        /// Auto scaling thread pool for Unit Of Work processing
        /// If using a fixed client ID value when scheduling requests then set the _maxWorkItemLimitPerClient parameter accordingly
        /// <see cref="ScheduleWorkItem(TRequest, TClientID)"/>
        /// </summary>
        /// <param name="_maxWorkItemLimitPerClient">Maximum size of pending work items per client</param>
        /// <param name="_maxWorkerThreads">Maximum number of worker threads to create in the thread pool</param>
        /// <param name="_threadStartupPerWorkItems">Auto Scale UP: Will consider starting a new thread after every _ThreadStartupPerWorkItems work items scheduled</param>
        /// <param name="_threadStartupMinQueueSize">Auto Scale UP: Will only consider staring a new thread of the work item buffer across all clients is larger than this</param>
        /// <param name="_idleWorkerThreadExitSeconds">Auto Scale DOWN: Worker threads which are idle for longer than this number of seconds will exit</param>
        /// <param name="_abandonedResponseExpirySeconds">If this value is <= 0 then processed work items will be disposed and discarded. Else if a completed work item is not picked up because maybe the requesting thread crashed then it will be abandoned and removed from the outbound queue after this number of seconds</param>
        /// <param name="_processRequestMethod">Delegate for processing work items. This is your Do Work method</param>
        /// <param name="_logErrorMethod">Delegate for logging unhandled expcetions while trying to process work items</param>
        /// <param name="_logMessageMethod">Delegate for logging info messages</param>
        public AsyncThreadedWorkItemProcessor(int _maxWorkItemLimitPerClient, int _maxWorkerThreads, int _threadStartupPerWorkItems, int _threadStartupMinQueueSize, int _idleWorkerThreadExitSeconds, int _abandonedResponseExpirySeconds
            , Handler_ProcessRequest _processRequestMethod
            , Handler_LogError _logErrorMethod
            , Handler_LogMessage _logMessageMethod
            )
        {
            if (!typeof(TPriority).IsEnum)
            {
                throw new ArgumentException("TPriority must be an enumerated type");
            }
            List<int> PriorityValueList;
            PriorityValueList = new List<int>();
            foreach (int priorityValue in Enum.GetValues(typeof(TPriority)))
            {
                TPriority priority;
                priority = (TPriority)(object)priorityValue;
                PriorityValueList.Add(priorityValue);
                PendingWorkItemFiFOQueueInbound.Add(priority, new Queue<ThreadWorkItem<TRequest, TResponse, TClientID>>());
                PendingWorkItemFiFOQueueOutbound.Add(priority, new Queue<ThreadWorkItem<TRequest, TResponse, TClientID>>());
            }
            PriorityValueList.Sort();
            while (PriorityValueList.Count > 0)
            {
                TPriority priority;
                int priorityValue;
                priorityValue = PriorityValueList[PriorityValueList.Count - 1];
                priority = (TPriority)(object)priorityValue;
                PriorityList.Add(priority);
                PriorityValueList.RemoveAt(PriorityValueList.Count - 1);
            }
            //foreach (int priorityValue in PriorityValueList)
            //{
            //    TPriority priority;
            //    //priority = (TPriority)(object)priorityValue;
            //    priority = ToEnum<TPriority>.FromInt(priorityValue);
            //    PriorityList.Add(priority);
            //}

            MaxWorkItemLimitPerClient = Math.Max(_maxWorkItemLimitPerClient, 10);
            MaxWorkItemLimitPerClientEnabled = MaxWorkItemLimitPerClient < int.MaxValue;
            MaxWorkerThreads = Math.Max(_maxWorkerThreads, 2);
            ThreadStartupDelayPerWorkItems = Math.Max(_threadStartupPerWorkItems, 0);
            ThreadStartupMinQueueSize = Math.Max(_threadStartupMinQueueSize, 0);
            IdleWorkerThreadExitMS = Math.Max(_idleWorkerThreadExitSeconds, 1) * 1000;
            AbandonedResponseExpiryMS = _abandonedResponseExpirySeconds * 1000;
            DiscardCompletedWorkItems = (_abandonedResponseExpirySeconds <= 0);

            OnProcessRequest = _processRequestMethod;
            OnLogError = _logErrorMethod;
            OnLogMessage = _logMessageMethod;

            SWPendingWorkItemFiFOQueueOutboundRefresh.Start();

            TaskManagerThread = new Thread(new ThreadStart(TaskManager));
            TaskManagerThread.Start();

            Core.BackgroundTaskSchduler.Schedule(ThreadWatchDog, 5);
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private Thread TaskManagerThread;

        private List<TPriority> PriorityList { get; } = new List<TPriority>();
        private object SyncRootQueueInbound { get; } = new object();
        private Dictionary<TClientID, QueueSizeCounter> PendingWorkItemClientQueue { get; } = new Dictionary<TClientID, QueueSizeCounter>();
        private Dictionary<TClientID, DateTime> PendingWorkItemClientQueueActivity { get; } = new Dictionary<TClientID, DateTime>();
        private Dictionary<TPriority, Queue<ThreadWorkItem<TRequest, TResponse, TClientID>>> PendingWorkItemFiFOQueueInbound { get; } = new Dictionary<TPriority, Queue<ThreadWorkItem<TRequest, TResponse, TClientID>>>();
        private QueueSizeCounter PendingWorkItemFiFOQueueSize { get; } = new QueueSizeCounter();
        /// <summary>
        /// Schedule work item with a per client queue size cap. If shutting down or if the client queue is at capacity then returned unique ID will be -1. 
        /// If the work item was successfully queued then the return value will be a positive integer ID.
        /// Use the ID to retrieve the processed work item <see cref="TryGetProcessedWorkItem(int, out ThreadWorkItem{TRequest, TResponse, TClientID})"/>
        /// or for the Async version <see cref="TryGetProcessedWorkItemAsync(int, int, ThreadProcessorAsyncTaskWaitType, int)"/>
        /// </summary>
        /// <param name="_request">Unit of work to be processed</param>
        /// <param name="_clientID">client id for unit of work or a fixed value if not using per client max queue size caps</param>
        /// <returns>Unique ID reference for scehduled work item. Use this ID to retrieve the processed work item reponse <see cref="TryGetProcessedWorkItem(int, out ThreadWorkItem{TRequest, TResponse, TClientID})"/>
        /// or Async version <see cref="TryGetProcessedWorkItemAsync(int, int, ThreadProcessorAsyncTaskWaitType, int)"/>
        /// If client queue is at capacity will return -1 which means that the work item should be retried or discarded depending on the scenario
        /// </returns>
        public int ScheduleWorkItem(TPriority _priority, TRequest _request, TClientID _clientID, params object[] parameters)
        {
            if (!IsRunning) { return -1; }
            int newWorkItemID;
            QueueSizeCounter queueCounter;
            ThreadWorkItem<TRequest, TResponse, TClientID> newWorkItem;
            Queue<ThreadWorkItem<TRequest, TResponse, TClientID>> fifoQueue;
            newWorkItem = new ThreadWorkItem<TRequest, TResponse, TClientID>(_clientID) { Request = _request };
            newWorkItem.Parameters = parameters;
            newWorkItemID = newWorkItem.ID;
            lock (SyncRootQueueInbound)
            {
                if (MaxWorkItemLimitPerClientEnabled)
                {
                    if (!PendingWorkItemClientQueue.TryGetValue(_clientID, out queueCounter))
                    {
                        queueCounter = new QueueSizeCounter();
                        PendingWorkItemClientQueue.Add(_clientID, queueCounter);
                    }
                    if (queueCounter.Value > MaxWorkItemLimitPerClient) { return -1; }
                    if (PendingWorkItemClientQueueActivity.ContainsKey(_clientID))
                    {
                        PendingWorkItemClientQueueActivity[_clientID] = DateTime.Now;
                    }
                    else
                    {
                        PendingWorkItemClientQueueActivity.Add(_clientID, DateTime.Now);
                    }
                    queueCounter.Increment();
                }
                PendingWorkItemFiFOQueueInbound[_priority].Enqueue(newWorkItem);
                PendingWorkItemFiFOQueueSize.Increment();
            }
            return newWorkItemID;
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            ShutDown();
        }

        /// <summary>
        /// Total number of queued work items waiting for processing
        /// Note: Threadsafe
        /// </summary>
        /// <returns>Total number of queued work items waiting for processing</returns>
        public int QueueSizeAllClients()
        {
            return PendingWorkItemFiFOQueueSize.Value;
            //lock (SyncRootQueue)
            //{
            //    return PendingWorkItemFiFOQueue.Count;
            //}
        }

        private object SyncRootQueueOutbound { get; } = new object();
        private Stopwatch SWPendingWorkItemFiFOQueueOutboundRefresh { get; } = new Stopwatch();
        private QueueSizeCounter PendingWorkItemFiFOQueueOutboundCount { get; } = new QueueSizeCounter();
        private Dictionary<TPriority, Queue<ThreadWorkItem<TRequest, TResponse, TClientID>>> PendingWorkItemFiFOQueueOutbound { get; } = new Dictionary<TPriority, Queue<ThreadWorkItem<TRequest, TResponse, TClientID>>>();

        /// <summary>
        /// Called internally by <see cref="WorkThread{TRequest, TResponse, TClientID}"/>
        /// </summary>
        /// <param name="_workItem">work item (unit of work) to be processed. Null if return value is false</param>
        /// <returns>True if work item is available. Otherwise false and _workItem will be null</returns>
        private bool GetNextPendingWorkItem(out ThreadWorkItem<TRequest, TResponse, TClientID> _workItem)
        {
            QueueSizeCounter queueCounter;
            HashSet<ThreadWorkItem<TRequest, TResponse, TClientID>> clientQueue;
            if (PendingWorkItemFiFOQueueSize.Value > 0)
            {
                lock (SyncRootQueueOutbound)
                {
                    if ((PendingWorkItemFiFOQueueOutboundCount.Value == 0) || (SWPendingWorkItemFiFOQueueOutboundRefresh.ElapsedMilliseconds > 1000))
                    {
                        SWPendingWorkItemFiFOQueueOutboundRefresh.Restart();
                        lock (SyncRootQueueInbound)
                        {
                            foreach (KeyValuePair<TPriority, Queue<ThreadWorkItem<TRequest, TResponse, TClientID>>> queueInboundWithPriority in PendingWorkItemFiFOQueueInbound)
                            {
                                Queue<ThreadWorkItem<TRequest, TResponse, TClientID>> queueInbound;
                                Queue<ThreadWorkItem<TRequest, TResponse, TClientID>> queueOutbound;
                                queueInbound = queueInboundWithPriority.Value;
                                queueOutbound = PendingWorkItemFiFOQueueOutbound[queueInboundWithPriority.Key];
                                int NumPending = queueInbound.Count;
                                for (int I = 0; I < NumPending; I++)
                                {
                                    queueOutbound.Enqueue(queueInbound.Dequeue());
                                    PendingWorkItemFiFOQueueOutboundCount.Increment();
                                }
                            }
                        }
                    }
                    foreach (TPriority priority in PriorityList)
                    {
                        Queue<ThreadWorkItem<TRequest, TResponse, TClientID>> queueOutbound;
                        queueOutbound = PendingWorkItemFiFOQueueOutbound[priority];
                        if (queueOutbound.Count > 0)
                        {
                            PendingWorkItemFiFOQueueSize.Decrement();
                            PendingWorkItemFiFOQueueOutboundCount.Decrement();
                            _workItem = queueOutbound.Dequeue();
                            if (MaxWorkItemLimitPerClientEnabled)
                            {
                                if (PendingWorkItemClientQueue.TryGetValue(_workItem.ClientID, out queueCounter))
                                {
                                    queueCounter.Decrement();
                                }
                            }
                            return true;
                        }
                    }
                }
            }
            _workItem = null;
            return false;
        }

        private object SyncLockProcessedQueue { get; } = new object();
        private Dictionary<int, ThreadWorkItem<TRequest, TResponse, TClientID>> ProcessedDict { get; } = new Dictionary<int, ThreadWorkItem<TRequest, TResponse, TClientID>>();
        private void OnRequestProcessed(ThreadWorkItem<TRequest, TResponse, TClientID> _workItem)
        {
            if (DiscardCompletedWorkItems)
            {
                IDisposable disposable;
                try
                {
                    disposable = _workItem.Request as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    OnLogError(ex);
                }
                try
                {
                    disposable = _workItem.Response as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    OnLogError(ex);
                }
            }
            else
            {
                lock (SyncLockProcessedQueue)
                {
                    if (ProcessedDict.ContainsKey(_workItem.ID))
                    {
                        ProcessedDict[_workItem.ID] = _workItem;
                    }
                    else
                    {
                        ProcessedDict.Add(_workItem.ID, _workItem);
                    }
                }
            }
        }

        /// <summary>
        /// Call this method to retrieve processed work items (unit of work).
        /// This is the Sync implementation. For the Async implementation <see cref="TryGetProcessedWorkItemAsync(int, int, ThreadProcessorAsyncTaskWaitType, int)"/>
        /// Note: this method is threadsafe however calling it in a tight loop with no sleep or other waits will cause lock contention.
        /// </summary>
        /// <param name="_ID">The unique ID of the work item to retrieve <see cref="ScheduleWorkItem(TRequest, TClientID)"/></param>
        /// <param name="_workItem">work item (unit of work) to be processed. Null if return value is false</param>
        /// <returns>True if processed work item is available. Otherwise false and _workItem will be null</returns>
        public bool TryGetProcessedWorkItem(int _ID, out ThreadWorkItem<TRequest, TResponse, TClientID> _workItem)
        {
            lock (SyncLockProcessedQueue)
            {
                if (!ProcessedDict.TryGetValue(_ID, out _workItem))
                {
                    return false;
                }
                else
                {
                    ProcessedDict.Remove(_ID);
                }
            }
            return true;
        }

        private object StopWatchPoolSyncLock { get; } = new object();
        private Queue<Stopwatch> StopWatchPool { get; } = new Queue<Stopwatch>();
        private Stopwatch StopWatchPool_Lease()
        {
            lock (StopWatchPoolSyncLock)
            {
                if (StopWatchPool.Count == 0)
                {
                    return new Stopwatch();
                }
                else
                {
                    return StopWatchPool.Dequeue();
                }
            }
        }
        private void StopWatchPool_Return(Stopwatch _SW)
        {
            _SW.Stop();
            _SW.Reset();
            lock (StopWatchPoolSyncLock)
            {
                StopWatchPool.Enqueue(_SW);
            }
        }

        /// <summary>
        /// Call this method to retrieve processed work items (unit of work).
        /// This is the Async implementation. For the Sync implementation <see cref="TryGetProcessedWorkItem(int, out ThreadWorkItem{TRequest, TResponse, TClientID})"/> />
        /// Note: this method is an Async wrapper around the Sync implementation with a configurable wait method (_taskWaitType parameter).
        /// </summary>
        /// <param name="_ID">The unique ID of the work item to retrieve <see cref="ScheduleWorkItem(TRequest, TClientID)"/></param>
        /// <param name="_timeoutMS">Timeout in milliseconds</param>
        /// <param name="_taskWaitType"></param>
        /// <param name="_delayMS"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, ThreadWorkItem<TRequest, TResponse, TClientID>>> TryGetProcessedWorkItemAsync(int _ID, int _timeoutMS, ThreadProcessorAsyncTaskWaitType _taskWaitType = ThreadProcessorAsyncTaskWaitType.Delay_1, int _delayMS = 1)
        {
            ThreadWorkItem<TRequest, TResponse, TClientID> workItem;
            if (_delayMS < 0) { _delayMS = 0; }
            if (!TryGetProcessedWorkItem(_ID, out workItem))
            {
                Stopwatch SW;
                SW = StopWatchPool_Lease();
                try
                {
                    SW.Start();
                    while (!TryGetProcessedWorkItem(_ID, out workItem))
                    {
                        switch (_taskWaitType)
                        {
                            case ThreadProcessorAsyncTaskWaitType.Yield:
                                await Task.Yield();
                                break;
                            case ThreadProcessorAsyncTaskWaitType.Delay_0:
                                await Task.Delay(0);
                                break;
                            case ThreadProcessorAsyncTaskWaitType.Delay_1:
                                await Task.Delay(1);
                                break;
                            case ThreadProcessorAsyncTaskWaitType.Delay_Specific:
                                await Task.Delay(_delayMS);
                                break;
                        }
                        if (SW.ElapsedMilliseconds > _timeoutMS)
                        {
                            workItem = null;
                            return new KeyValuePair<bool, ThreadWorkItem<TRequest, TResponse, TClientID>>(false, workItem);
                        }
                    }
                }
                finally
                {
                    StopWatchPool_Return(SW);
                }
            }
            return new KeyValuePair<bool, ThreadWorkItem<TRequest, TResponse, TClientID>>(true, workItem);
        }

        private object SyncLockThreadControl { get; } = new object();
        private bool m_IsRunning = true;
        private bool IsRunning
        {
            get
            {
                lock (SyncLockThreadControl)
                {
                    return m_IsRunning;
                }
            }
        }

        private bool m_IsShutDown = false;
        /// <summary>
        /// Shutdown threadpool. Once shutdown it cannot be started back up again. Instead create a new instance
        /// </summary>
        /// <param name="_timeoutSeconds">number of seconds to wait for pending work items to be processed</param>
        public void ShutDown()
        {
            lock (SyncLockThreadControl)
            {
                if (m_IsShutDown) { return; }
                m_IsShutDown = true;
                m_IsRunning = false;
            }

            Core.BackgroundTaskSchduler.Cancel(ThreadWatchDog);

            TaskManagerThread.Join();
            ProcessedDict.Clear();
            PendingWorkItemFiFOQueueInbound.Clear();
        }

        private List<TaskWrappper> runningTaskQueue = new List<TaskWrappper>();

        private void TaskManager()
        {
            ThreadWorkItem<TRequest, TResponse, TClientID> workItem;
            bool IsIdle;
            bool success;
            while (runningTaskQueue.Count < MaxWorkerThreads)
            {
                TaskWrappper newTask = new TaskWrappper(OnProcessRequest, OnLogError, OnLogMessage, OnRequestProcessed);
                runningTaskQueue.Add(newTask);
            }
            while (IsRunning)
            {
                IsIdle = true;
                int numToDeQueue;
                int queueSize;
                queueSize = QueueSizeAllClients();
                if (queueSize > 0)
                {
                    numToDeQueue = Math.Max((queueSize / MaxWorkerThreads) - 1, 1);
                    if (numToDeQueue > 1)
                    {
                        foreach (TaskWrappper taskInstance in runningTaskQueue)
                        {
                            if (taskInstance.RunningTask.IsCompleted)
                            {
                                if (GetNextPendingWorkItem(out workItem))
                                {
                                    taskInstance.WorkItemQueue.Enqueue(workItem);
                                    taskInstance.StartTask();
                                    IsIdle = false;
                                }
                                else
                                {
                                    break;
                                }
                                //if (GetNextPendingWorkItems(numToDeQueue, taskInstance.WorkItemQueue, out success) > 0)
                                //{
                                //    taskInstance.StartTask();
                                //    IsIdle = false;
                                //}
                                //else
                                //{
                                //    break;
                                //}
                            }
                        }
                    }
                    else
                    {
                        foreach (TaskWrappper taskInstance in runningTaskQueue)
                        {
                            if (taskInstance.RunningTask.IsCompleted)
                            {
                                if (GetNextPendingWorkItem(out workItem))
                                {
                                    taskInstance.WorkItemQueue.Enqueue(workItem);
                                    taskInstance.StartTask();
                                    IsIdle = false;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                foreach (TaskWrappper taskInstance in runningTaskQueue)
                {
                    if (taskInstance.WorkItemQueue.Count > 0)
                    {
                        taskInstance.StartTask();
                    }
                }
                if (IsIdle)
                {
                    Thread.Sleep(1);
                }
            }
            List<Task> Pending = new List<Task>();
            foreach (TaskWrappper taskInstance in runningTaskQueue)
            {
                Pending.Add(taskInstance.RunningTask);
            }
            Task.WaitAll(Pending.ToArray());
            foreach (TaskWrappper taskInstance in runningTaskQueue)
            {
                while (taskInstance.WorkItemQueue.Count > 0)
                {
                    workItem = taskInstance.WorkItemQueue.Dequeue();
                    ScheduleWorkItem(PriorityList[0], workItem.Request, workItem.ClientID);
                }
            }
            runningTaskQueue.Clear();
        }

        private class TaskWrappper
        {
            public TaskWrappper(Handler_ProcessRequest _OnProcessRequest, Handler_LogError _OnLogError, Handler_LogMessage _OnLogMessage, Handler_OnRequestProcessed _OnRequestProcessed)
            {
                OnProcessRequest = _OnProcessRequest;
                OnLogError = _OnLogError;
                OnLogMessage = _OnLogMessage;
                OnRequestProcessed = _OnRequestProcessed;
                RunningTask = new Task(DoWork);
                RunningTask.Start();
            }

            private bool IsComplete = true;
            public void StartTask()
            {
                if (RunningTask.IsCompleted)
                {
                    RunningTask = new Task(DoWork);
                    RunningTask.ConfigureAwait(false);
                    RunningTask.Start();
                }
            }

            public Task RunningTask { get; set; }
            public Queue<ThreadWorkItem<TRequest, TResponse, TClientID>> WorkItemQueue { get; } = new Queue<ThreadWorkItem<TRequest, TResponse, TClientID>>();
            private Handler_ProcessRequest OnProcessRequest;
            private Handler_LogError OnLogError;
            private Handler_LogMessage OnLogMessage;
            private Handler_OnRequestProcessed OnRequestProcessed;
            public void DoWork()
            {
                ThreadWorkItem<TRequest, TResponse, TClientID> workItem;
                while (WorkItemQueue.Count > 0)
                {
                    workItem = WorkItemQueue.Dequeue();
                    try
                    {
                        workItem.Response = OnProcessRequest(workItem);
                        OnRequestProcessed(workItem);
                    }
                    catch (Exception ex2)
                    {
                        try
                        {
                            workItem.Response = default(TResponse);
                            workItem.ErrorException = ex2;
                            workItem.IsError = true;
                            OnRequestProcessed(workItem);
                            OnLogError(new Exception(@"Error calling OnProcessRequest() handler for work item request. See inner exception.", ex2));
                            try
                            {

                            }
                            catch (Exception ex3)
                            {
                                OnLogError(new Exception(@"Unknown error In work thread controller. See inner exception.", ex3));
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }


        private object ThreadWatchDogSyncLock = new object();
        private bool ThreadWatchDogIsRunning = false;
        private int ThreadWatchDogCleanCountdown = 10;

        private void ThreadWatchDog(object state)
        {
            if (IsRunning)
            {
                bool CanRunNow_ThreadMonitor = false;
                bool CanRunNow_CleanQueues = false;
                lock (ThreadWatchDogSyncLock)
                {
                    if (!ThreadWatchDogIsRunning)
                    {
                        CanRunNow_ThreadMonitor = true;
                        ThreadWatchDogIsRunning = true;
                        ThreadWatchDogCleanCountdown -= 1;
                        if (ThreadWatchDogCleanCountdown <= 0)
                        {
                            ThreadWatchDogCleanCountdown = 12;
                            CanRunNow_CleanQueues = true;
                        }
                    }
                }
                if (CanRunNow_ThreadMonitor)
                {
                    try
                    {
                        WatchDogThreadMonitor();
                        if (CanRunNow_CleanQueues)
                        {
                            WatchDogCleanQueues();
                        }
                    }
                    finally
                    {
                        lock (ThreadWatchDogSyncLock)
                        {
                            ThreadWatchDogIsRunning = false;
                        }
                    }
                }
            }
            else
            {
                
                Core.BackgroundTaskSchduler.Cancel(ThreadWatchDog);
            }
        }

        private void WatchDogThreadMonitor(bool _startThreadIfQueueNotEmpty = true)
        {
            if (!TaskManagerThread.IsAlive)
            {
                try
                {
                    TaskManagerThread.Abort();
                }
                catch (Exception ex)
                {

                }
                TaskManagerThread = new Thread(new ThreadStart(TaskManager));
                TaskManagerThread.Start();
            }
        }

        private void WatchDogCleanQueues()
        {
            try
            {
                List<ThreadWorkItem<TRequest, TResponse, TClientID>> ExpiredItems = new List<ThreadWorkItem<TRequest, TResponse, TClientID>>();
                List<ThreadWorkItem<TRequest, TResponse, TClientID>> AllItems = new List<ThreadWorkItem<TRequest, TResponse, TClientID>>();
                lock (SyncLockProcessedQueue)
                {
                    AllItems = new List<ThreadWorkItem<TRequest, TResponse, TClientID>>(ProcessedDict.Values);
                }
                double ExpirySeconds = AbandonedResponseExpiryMS;
                foreach (ThreadWorkItem<TRequest, TResponse, TClientID> workItem in AllItems)
                {
                    if (workItem.ResponseAge.TotalSeconds > ExpirySeconds)
                    {
                        ExpiredItems.Add(workItem);
                    }
                }
                lock (SyncLockProcessedQueue)
                {
                    foreach (ThreadWorkItem<TRequest, TResponse, TClientID> workItem in ExpiredItems)
                    {
                        if (ProcessedDict.ContainsKey(workItem.ID))
                        {
                            ProcessedDict.Remove(workItem.ID);
                        }
                        IDisposable disposable;
                        disposable = workItem.Request as IDisposable;
                        if (disposable != null)
                        {
                            disposable.Dispose();
                        }
                        disposable = workItem.Response as IDisposable;
                        if (disposable != null)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                if (MaxWorkItemLimitPerClientEnabled)
                {
                    DateTime CurrentTime = DateTime.Now;
                    DateTime LastActiveTime;
                    List<TClientID> ClientIDList;
                    lock (SyncRootQueueInbound)
                    {
                        ClientIDList = new List<TClientID>(PendingWorkItemClientQueue.Keys);
                        foreach (TClientID clientID in ClientIDList)
                        {
                            if (PendingWorkItemClientQueueActivity.TryGetValue(clientID, out LastActiveTime))
                            {
                                if (CurrentTime.Subtract(LastActiveTime).TotalMinutes > 3)
                                {
                                    if (PendingWorkItemClientQueue[clientID].Value == 0)
                                    {
                                        PendingWorkItemClientQueue.Remove(clientID);
                                        if (PendingWorkItemClientQueueActivity.ContainsKey(clientID))
                                        {
                                            PendingWorkItemClientQueueActivity.Remove(clientID);
                                        }
                                    }
                                    else
                                    {
                                        PendingWorkItemClientQueueActivity[clientID] = CurrentTime;
                                    }
                                }
                            }
                        }
                        ClientIDList.Clear();
                        ClientIDList.AddRange(PendingWorkItemClientQueueActivity.Keys);
                        foreach (TClientID clientID in ClientIDList)
                        {
                            if (!PendingWorkItemClientQueue.ContainsKey(clientID))
                            {
                                PendingWorkItemClientQueueActivity.Remove(clientID);
                            }
                        }
                    }
                }
            }
            finally
            {
            }
        }

        /// <summary>
        /// Internal Use Private: helper class for wrapping an Interlocked counter
        /// </summary>
        private class QueueSizeCounter
        {
            public QueueSizeCounter()
            {
            }
            private volatile int _value = 0;
            public int Value
            {
                get { return _value; }
                set
                {
                    Interlocked.Exchange(ref _value, value);
                }
            }

            public virtual int Increment()
            {
                int result = Interlocked.Increment(ref _value);
                return result;
            }
            public virtual int Decrement()
            {
                int result = Interlocked.Decrement(ref _value);
                lock (this)
                {
                    result = _value;
                    if (result < 0)
                    {
                        Interlocked.Exchange(ref _value, 0);
                    }
                }
                return result;
            }
        }

    }
}
