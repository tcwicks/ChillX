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
    /// <summary>
    /// Auto scaling thread pool for Unit Of Work processing
    /// Note: if the total number of **concurrently active**  clients is a a large number like more than say 100 then disable the per client queue
    /// by specifying a fixed constant client ID when scheduling new work items. <see cref="ScheduleWorkItem(TRequest, TClientID)"/>
    /// Note: Will attempt to autoshutdown with a 60 second timeout when the parent process exits (for example IIS app pool recycle). However this is not guaranteed. 
    /// It is recommended to explicitly call Shutdown with a suitable timeout. <see cref="ShutDown(int)"/>
    /// </summary>
    /// <typeparam name="TRequest">Unit of Work type of request. Example CreateOrderRequest</typeparam>
    /// <typeparam name="TResponse">Unit of Work type of response. Example CreateOrderResponse</typeparam>
    /// <typeparam name="TClientID">Client ID data type. This may be a string or an int or a guid etc...</typeparam>
    /// <typeparam name="TPriority">Priority Enumeration for queue priorities. This must be an Enum</typeparam>
    public class ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>
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
        public ThreadedWorkItemProcessor(int _maxWorkItemLimitPerClient, int _maxWorkerThreads, int _threadStartupPerWorkItems, int _threadStartupMinQueueSize, int _idleWorkerThreadExitSeconds, int _processedQueueMaxSize, bool _processedItemAutoDispose
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
            MaxWorkerThreads = Math.Max(_maxWorkerThreads, 2);
            ThreadStartupDelayPerWorkItems = Math.Max(_threadStartupPerWorkItems, 0);
            ThreadStartupMinQueueSize = Math.Max(_threadStartupMinQueueSize, 0);
            IdleWorkerThreadExitMS = Math.Max(_idleWorkerThreadExitSeconds, 1) * 1000;
            ProcessedQueueMaxSize = Math.Max(_processedQueueMaxSize, 100); //Greater than 0 should be fine but just in case of some wierd edge case.
            ProcessedQueueEnable = _processedQueueMaxSize > 0;
            ProcessedItemAutoDispose = _processedItemAutoDispose;
            OnProcessRequest = _processRequestMethod;
            OnLogError = _logErrorMethod;
            OnLogMessage = _logMessageMethod;

            Core.BackgroundTaskSchduler.Schedule(ThreadWatchDog, 5);
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            ShutDown(60);
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
            StartWorkThread();
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
        public void ShutDown(int _timeoutSeconds)
        {
            lock (SyncLockThreadControl)
            {
                if (m_IsShutDown) { return; }
                m_IsShutDown = true;
                m_IsRunning = false;
            }
            if (_timeoutSeconds < 1) { _timeoutSeconds = 1; }
            Stopwatch SW = new Stopwatch();
            SW.Reset();
            SW.Start();
            _timeoutSeconds = _timeoutSeconds * 1000;
            while (SW.ElapsedMilliseconds < _timeoutSeconds)
            {
                if (QueueSizeAllClients() == 0)
                {
                    break;
                }
                System.Threading.Thread.Sleep(1);
            }
            Core.BackgroundTaskSchduler.Cancel(ThreadWatchDog);

            List<WorkThread<TRequest, TResponse, TClientID, TPriority>> finishedThreads;
            List<WorkThread<TRequest, TResponse, TClientID, TPriority>> stillRunningThreads;
            finishedThreads = new List<WorkThread<TRequest, TResponse, TClientID, TPriority>>();
            stillRunningThreads = new List<WorkThread<TRequest, TResponse, TClientID, TPriority>>();
            lock (SyncLockThreadControl)
            {
                foreach (KeyValuePair<int, WorkThread<TRequest, TResponse, TClientID, TPriority>> threadItem in ThreadDict)
                {
                    if (threadItem.Value.IsAlive)
                    {
                        threadItem.Value.Exit();
                    }
                    else
                    {
                        finishedThreads.Add(threadItem.Value);
                    }
                }
                foreach (WorkThread<TRequest, TResponse, TClientID, TPriority> workThread in finishedThreads)
                {
                    if (ThreadDict.ContainsKey(workThread.ID))
                    {
                        ThreadDict.Remove(workThread.ID);
                    }
                    workThread.Dispose();
                }
            }
            Thread.Sleep(1000);
            lock (SyncLockThreadControl)
            {
                foreach (KeyValuePair<int, WorkThread<TRequest, TResponse, TClientID, TPriority>> threadItem in ThreadDict)
                {
                    if (threadItem.Value.IsAlive)
                    {
                        stillRunningThreads.Add(threadItem.Value);
                    }
                    else
                    {
                        finishedThreads.Add(threadItem.Value);
                    }
                }
                foreach (WorkThread<TRequest, TResponse, TClientID, TPriority> workThread in finishedThreads)
                {
                    if (ThreadDict.ContainsKey(workThread.ID))
                    {
                        ThreadDict.Remove(workThread.ID);
                    }
                    workThread.Dispose();
                }
            }
            foreach (WorkThread<TRequest, TResponse, TClientID, TPriority> workThread in stillRunningThreads)
            {
                if (ThreadDict.ContainsKey(workThread.ID))
                {
                    ThreadDict.Remove(workThread.ID);
                }
                try
                {
                    workThread.WorkerThread.Abort();
                }
                catch
                {

                }
                try
                {
                    workThread.Dispose();
                }
                catch
                {

                }
            }
            lock (SyncLockThreadControl)
            {
                foreach (KeyValuePair<int, WorkThread<TRequest, TResponse, TClientID, TPriority>> threadItem in ThreadDict)
                {
                    threadItem.Value.Exit();
                    threadItem.Value.Dispose();
                }
            }
            while (SW.ElapsedMilliseconds < _timeoutSeconds)
            {
                if (ThreadCount == 0)
                {
                    break;
                }
                System.Threading.Thread.Sleep(1);
            }
            lock (SyncLockThreadControl)
            {
                foreach (KeyValuePair<int, WorkThread<TRequest, TResponse, TClientID, TPriority>> threadItem in ThreadDict)
                {
                    threadItem.Value.Dispose();
                }
                ThreadDict.Clear();
            }
            SW.Stop();
            ProcessedQueue.Clear();
            PendingWorkItemFiFOQueueInbound.Clear();
        }

        private Dictionary<int, WorkThread<TRequest, TResponse, TClientID, TPriority>> ThreadDict { get; } = new Dictionary<int, WorkThread<TRequest, TResponse, TClientID, TPriority>>();
        private int ThreadStartCounter = 0;
        /// <summary>
        /// Number of active threads. 
        /// Note: Threadsafe - However do not call in a tight loop as it will cause lock contention.
        /// </summary>
        public int ThreadCount
        {
            get
            {
                lock (SyncLockThreadControl)
                {
                    return ThreadDict.Count;
                }
            }
        }
        private void StartWorkThread()
        {
            bool doStart;
            doStart = false;
            lock (SyncLockThreadControl)
            {
                int NumThreads;
                NumThreads = ThreadDict.Count;
                if (NumThreads < MaxWorkerThreads)
                {
                    if (NumThreads == 0)
                    {
                        doStart = true;
                        ThreadStartCounter = 0;
                    }
                    else if (QueueSizeAllClients() > ThreadStartupMinQueueSize)
                    {
                        ThreadStartCounter++;
                        if (ThreadStartCounter >= ThreadStartupDelayPerWorkItems)
                        {
                            doStart = true;
                            ThreadStartCounter = 0;
                        }
                    }
                }
                else
                {
                    ThreadStartCounter = 0;
                }
            }
            if (doStart)
            {
                WorkThread<TRequest, TResponse, TClientID, TPriority> newWorkThread;
                newWorkThread = new WorkThread<TRequest, TResponse, TClientID, TPriority>(GetNextPendingWorkItem, OnProcessRequest, OnRequestProcessed, OnThreadExit, OnLogError, IdleWorkerThreadExitMS);
                lock (SyncLockThreadControl)
                {
                    if (ThreadDict.Count < MaxWorkerThreads)
                    {
                        ThreadDict.Add(newWorkThread.ID, newWorkThread);
                    }
                    else
                    {
                        doStart = false;
                    }
                }
                if (doStart)
                {
                    newWorkThread.Start();
                }
                else
                {
                    newWorkThread.Dispose();
                    newWorkThread = null;
                }
            }
        }

        private void OnThreadExit(int _ID)
        {
            WorkThread<TRequest, TResponse, TClientID, TPriority> newWorkThread;
            bool doStart;

            doStart = false;
            newWorkThread = null;
            lock (SyncLockThreadControl)
            {
                if (ThreadDict.TryGetValue(_ID, out newWorkThread))
                {
                    ThreadDict.Remove(_ID);
                    if (ThreadDict.Count == 0)
                    {
                        doStart = true;
                    }
                }
            }
            //if (doStart && (QueueSizeAllClients() > 0))
            //{
            //    System.Threading.Thread recoveryThread;
            //    recoveryThread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadWatchDog));
            //    recoveryThread.Start();
            //}
            if (newWorkThread != null)
            {
                newWorkThread.Dispose();
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
                lock(ThreadWatchDogSyncLock)
                {
                    if(!ThreadWatchDogIsRunning)
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
            if (QueueSizeAllClients() > 0)
            {
                List<WorkThread<TRequest, TResponse, TClientID, TPriority>> finishedThreads;
                finishedThreads = new List<WorkThread<TRequest, TResponse, TClientID, TPriority>>();
                lock (SyncLockThreadControl)
                {
                    foreach (KeyValuePair<int, WorkThread<TRequest, TResponse, TClientID, TPriority>> threadItem in ThreadDict)
                    {
                        if (!threadItem.Value.IsRunning)
                        {
                            finishedThreads.Add(threadItem.Value);
                        }
                    }
                    foreach (WorkThread<TRequest, TResponse, TClientID, TPriority> workThread in finishedThreads)
                    {
                        if (ThreadDict.ContainsKey(workThread.ID))
                        {
                            ThreadDict.Remove(workThread.ID);
                        }
                    }
                }
                if (finishedThreads.Count > 0)
                {
                    OnLogMessage(string.Concat(@"Warning: Thread Watchdog found ", finishedThreads.Count.ToString(), @" stuck threads. Terminating stuck threads and triggering a new one."), false, false);
                }
                if (_startThreadIfQueueNotEmpty)
                {
                    StartWorkThread();
                }
                foreach (WorkThread<TRequest, TResponse, TClientID, TPriority> workThread in finishedThreads)
                {
                    try
                    {
                        workThread.WorkerThread.Abort();
                    }
                    catch
                    {

                    }
                    try
                    {
                        workThread.Dispose();
                    }
                    catch
                    {

                    }
                }
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
