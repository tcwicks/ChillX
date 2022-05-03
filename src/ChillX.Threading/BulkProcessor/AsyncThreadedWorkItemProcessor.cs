using ChillX.Core.CapabilityInterfaces;
using ChillX.Core.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ChillX.Threading.BulkProcessor
{
    public class AsyncThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>
        where TClientID : struct, IComparable, IFormattable, IConvertible
        where TPriority : struct, IComparable, IFormattable, IConvertible
    {
        internal delegate bool Handler_GetNextPendingWorkItem(out ThreadWorkItem<TRequest, TResponse, TClientID> workItem);
        public delegate TResponse Handler_ProcessRequest(TRequest request);
        public delegate void Handler_OnRequestProcessed(ThreadWorkItem<TRequest, TResponse, TClientID> workItem);
        internal delegate void Handler_OnThreadExit(int ID);
        public delegate void Handler_LogError(Exception ex);
        public delegate void Handler_LogMessage(string _message, bool _isError, bool _isWarning);

        public bool MaxWorkItemLimitPerClientEnabled { get; private set; } = true;
        public int MaxWorkItemLimitPerClient { get; private set; } = 100;
        public int MaxWorkerThreads { get; private set; } = 2;
        public int ThreadStartupDelayPerWorkItems { get; private set; } = 2;
        public int ThreadStartupMinQueueSize { get; private set; } = 2;
        public int IdleWorkerThreadExitMS { get; private set; } = 1000;
        public int ProcessedQueueMaxSize { get; private set; } = 1000;
        public bool ProcessedQueueEnable { get; private set; } = true;
        public bool ProcessedItemAutoDispose { get; private set; } = true;
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
        /// <param name="_processedQueueMaxSize">If value is 0 Processed Items will be Disposed and discarded. Else when the processed queue size reaches this value processing of new items will be halted until the queue size drops back down</param>
        /// <param name="_processRequestMethod">Delegate for processing work items. This is your Do Work method</param>
        /// <param name="_logErrorMethod">Delegate for logging unhandled expcetions while trying to process work items</param>
        /// <param name="_logMessageMethod">Delegate for logging info messages</param>
        public AsyncThreadedWorkItemProcessor(int _maxWorkItemLimitPerClient, int _maxWorkerThreads, int _threadStartupPerWorkItems, int _threadStartupMinQueueSize, int _idleWorkerThreadExitSeconds, int _processedQueueMaxSize, bool _processedItemAutoDispose
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
                PendingWorkItemFiFOQueueInbound.Add(priority, new ThreadSafeQueue<ThreadWorkItem<TRequest, TResponse, TClientID>>());
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
            MaxWorkerThreads = Math.Max(_maxWorkerThreads, 1);
            ThreadStartupDelayPerWorkItems = Math.Max(_threadStartupPerWorkItems, 0);
            ThreadStartupMinQueueSize = Math.Max(_threadStartupMinQueueSize, 0);
            IdleWorkerThreadExitMS = Math.Max(_idleWorkerThreadExitSeconds, 1) * 1000;
            ProcessedQueueMaxSize = Math.Max(_processedQueueMaxSize, 100); //Greater than 0 should be fine but just in case of some wierd edge case.
            ProcessedQueueEnable = _processedQueueMaxSize > 0;
            ProcessedItemAutoDispose = _processedItemAutoDispose;
            OnProcessRequest = _processRequestMethod;
            OnLogError = _logErrorMethod;
            OnLogMessage = _logMessageMethod;

            TaskManagerThread = new Thread(new ThreadStart(TaskManager));
            TaskManagerThread.Start();

            Core.BackgroundTaskSchduler.Schedule(ThreadWatchDog, 5);
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }
        private Thread TaskManagerThread;
        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            ShutDown();
        }


        private List<TPriority> PriorityList { get; } = new List<TPriority>();
        private object SyncRootQueueInbound { get; } = new object();
        private Dictionary<TClientID, QueueSizeCounter> PendingWorkItemClientQueue { get; } = new Dictionary<TClientID, QueueSizeCounter>();
        private Dictionary<TClientID, DateTime> PendingWorkItemClientQueueActivity { get; } = new Dictionary<TClientID, DateTime>();
        private Dictionary<TPriority, ThreadSafeQueue<ThreadWorkItem<TRequest, TResponse, TClientID>>> PendingWorkItemFiFOQueueInbound { get; } = new Dictionary<TPriority, ThreadSafeQueue<ThreadWorkItem<TRequest, TResponse, TClientID>>>();
        private QueueSizeCounter PendingWorkItemFiFOQueueSize { get; } = new QueueSizeCounter();
        /// <summary>
        /// Schedule work item with a per client queue size cap. If shutting down or if the client queue is at capacity then returned unique ID will be -1. 
        /// If the work item was successfully queued then the return value will be a positive integer ID.
        /// Use the ID to retrieve the processed work item <see cref="TryGetProcessedWorkItem(int, out ThreadWorkItem{TRequest, TResponse, TClientID})"/>
        /// </summary>
        /// <param name="_request">Unit of work to be processed</param>
        /// <param name="_clientID">client id for unit of work or a fixed value if not using per client max queue size caps</param>
        /// <returns>Unique ID reference for scehduled work item. Use this ID to retrieve the processed work item reponse <see cref="TryGetProcessedWorkItem(int, out ThreadWorkItem{TRequest, TResponse, TClientID})"/>
        /// If client queue is at capacity will return -1 which means that the work item should be retried or discarded depending on the scenario
        /// </returns>
        public int ScheduleWorkItem(TPriority _priority, TRequest _request, TClientID _clientID)
        {
            if (!IsRunning) { return -1; }
            int newWorkItemID;
            QueueSizeCounter queueCounter;
            ThreadWorkItem<TRequest, TResponse, TClientID> newWorkItem;
            Queue<ThreadWorkItem<TRequest, TResponse, TClientID>> fifoQueue;
            newWorkItem = new ThreadWorkItem<TRequest, TResponse, TClientID>(_clientID) { Request = _request };
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
                foreach (TPriority priority in PriorityList)
                {
                    ThreadSafeQueue<ThreadWorkItem<TRequest, TResponse, TClientID>> queueOutbound;
                    queueOutbound = PendingWorkItemFiFOQueueInbound[priority];
                    if (queueOutbound.Count > 0)
                    {
                        bool success;
                        _workItem = queueOutbound.DeQueue(out success);
                        if (success)
                        {
                            PendingWorkItemFiFOQueueSize.Decrement();
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

        private int GetNextPendingWorkItems(int requestedCount, Queue<ThreadWorkItem<TRequest, TResponse, TClientID>> destinationQueue, out bool success)
        {
            QueueSizeCounter queueCounter;
            HashSet<ThreadWorkItem<TRequest, TResponse, TClientID>> clientQueue;
            success = false;
            int numResults = 0;
            if (MaxWorkItemLimitPerClientEnabled)
            {
                if (destinationQueue.Count > 0)
                {
                    throw new InvalidOperationException(@"Bulk Dequeue destinationQueue must be empty when MaxWorkItemLimitPerClient > 0");
                }
            }
            if (PendingWorkItemFiFOQueueSize.Value > 0)
            {
                foreach (TPriority priority in PriorityList)
                {
                    ThreadSafeQueue<ThreadWorkItem<TRequest, TResponse, TClientID>> queueOutbound;
                    queueOutbound = PendingWorkItemFiFOQueueInbound[priority];
                    if (queueOutbound.Count > 0)
                    {
                        numResults += queueOutbound.DeQueue(requestedCount - numResults, destinationQueue, out success);
                        if (success)
                        {
                            PendingWorkItemFiFOQueueSize.Decrement(numResults);
                            if (MaxWorkItemLimitPerClientEnabled)
                            {
                                foreach (ThreadWorkItem<TRequest, TResponse, TClientID> workItem in destinationQueue)
                                {
                                    if (PendingWorkItemClientQueue.TryGetValue(workItem.ClientID, out queueCounter))
                                    {
                                        queueCounter.Decrement();
                                    }
                                }
                                break;
                            }
                            if (numResults >= requestedCount)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            success = numResults > 0;
            return numResults;
        }

        private object SyncLockProcessedQueue { get; } = new object();
        private Queue<ThreadWorkItem<TRequest, TResponse, TClientID>> ProcessedQueue { get; } = new Queue<ThreadWorkItem<TRequest, TResponse, TClientID>>();
        private void OnRequestProcessed(ThreadWorkItem<TRequest, TResponse, TClientID> _workItem)
        {
            if (ProcessedQueueEnable)
            {
                lock (SyncLockProcessedQueue)
                {
                    ProcessedQueue.Enqueue(_workItem);
                }
            }
            else
            {
                if (ProcessedItemAutoDispose)
                {
                    IDisposable target;
                    target = _workItem.Request as IDisposable;
                    if (target != null) { target.Dispose(); }

                    target = _workItem.Response as IDisposable;
                    if (target != null) { target.Dispose(); }
                }
            }
        }

        /// <summary>
        /// Call this method to retrieve processed work items (unit of work).
        /// Note: this method is threadsafe however calling it in a tight loop with no sleep or other waits will cause lock contention.
        /// </summary>
        /// <param name="_ID">The unique ID of the work item to retrieve <see cref="ScheduleWorkItem(TRequest, TClientID)"/></param>
        /// <param name="_workItem">work item (unit of work) to be processed. Null if return value is false</param>
        /// <returns>True if processed work item is available. Otherwise false and _workItem will be null</returns>
        public bool TryGetProcessedWorkItem(out ThreadWorkItem<TRequest, TResponse, TClientID> _workItem)
        {
            lock (SyncLockProcessedQueue)
            {
                if (ProcessedQueue.Count > 0)
                {
                    _workItem = ProcessedQueue.Dequeue();
                    return true;
                }
            }
            _workItem = default(ThreadWorkItem<TRequest, TResponse, TClientID>);
            return false;
        }

        #region UnUsed
        //private object StopWatchPoolSyncLock { get; } = new object();
        //private Queue<Stopwatch> StopWatchPool { get; } = new Queue<Stopwatch>();
        //private Stopwatch StopWatchPool_Lease()
        //{
        //    lock (StopWatchPoolSyncLock)
        //    {
        //        if (StopWatchPool.Count == 0)
        //        {
        //            return new Stopwatch();
        //        }
        //        else
        //        {
        //            return StopWatchPool.Dequeue();
        //        }
        //    }
        //}
        //private void StopWatchPool_Return(Stopwatch _SW)
        //{
        //    _SW.Stop();
        //    _SW.Reset();
        //    lock (StopWatchPoolSyncLock)
        //    {
        //        StopWatchPool.Enqueue(_SW);
        //    }
        //}
        #endregion

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
            ProcessedQueue.Clear();
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
                                if (GetNextPendingWorkItems(numToDeQueue, taskInstance.WorkItemQueue, out success) > 0)
                                {
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
                        workItem.Response = OnProcessRequest(workItem.Request);
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
            if(!TaskManagerThread.IsAlive)
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
                if (result < 0)
                {
                    lock (this)
                    {
                        result = _value;
                        if (result < 0)
                        {
                            Interlocked.Exchange(ref _value, 0);
                        }
                    }
                }
                return result;
            }
            public virtual int Decrement(int count)
            {
                int result = _value;
                for (int i = 0; i < count; i++)
                {
                    result = Interlocked.Decrement(ref _value);
                }
                if (result < 0)
                {
                    lock (this)
                    {
                        result = _value;
                        if (result < 0)
                        {
                            Interlocked.Exchange(ref _value, 0);
                        }
                    }
                }
                return result;
            }
        }

    }
}
