using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ChillX.Threading.Simple
{
    /// <summary>
    /// Auto scaling thread pool for Unit Of Work processing
    /// Note: This implementation deliberately cuts a few corners / takes a few shortcuts in order to reduce complexity.
    /// Note: if the total number of **concurrently active**  clients is not a small number like less than say 100
    /// then specify a fixed constant client ID when scheduling new work items. <see cref="ScheduleWorkItem(TRequest, TClientID)"/>
    /// </summary>
    /// <typeparam name="TRequest">Unit of Work type of request. Example CreateOrderRequest</typeparam>
    /// <typeparam name="TResponse">Unit of Work type of response. Example CreateOrderResponse</typeparam>
    /// <typeparam name="TClientID">Client ID data type. This may be a string or an int or a guid etc...</typeparam>
    public class SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>
        where TRequest : class, new()
        where TResponse : class, new()
        where TClientID : struct, IComparable, IFormattable, IConvertible
    {
        internal delegate bool Handler_GetNextPendingWorkItem(out SimpleThreadedWorkItem<TRequest, TResponse, TClientID> workItem);
        public delegate TResponse Handler_ProcessRequest(TRequest request);
        public delegate void Handler_OnRequestProcessed(SimpleThreadedWorkItem<TRequest, TResponse, TClientID> workItem);
        internal delegate void Handler_OnThreadExit(int ID);
        public delegate void Handler_LogError(Exception ex);
        public delegate void Handler_LogMessage(string Message);

        public int MaxWorkItemLimitPerClient { get; private set; } = 100;
        public int MaxWorkerThreads { get; private set; } = 2;
        public int ThreadStartupDelayPerWorkItems { get; private set; } = 2;
        public int ThreadStartupMinQueueSize { get; private set; } = 2;
        public int IdleWorkerThreadExitMS { get; private set; } = 1000;
        public int AbandonedResponseExpiryMS { get; private set; } = 60000;
        private Handler_ProcessRequest OnProcessRequest;
        private Handler_LogError OnLogError;
        private Handler_LogMessage OnLogMessage;

        /// <summary>
        /// Auto scaling thread pool for Unit Of Work processing
        /// </summary>
        /// <param name="_MaxWorkItemLimitPerClient">Maximum size of pending work items per client</param>
        /// <param name="_MaxWorkerThreads">Maximum number of worker threads to create in the thread pool</param>
        /// <param name="_ThreadStartupPerWorkItems">Auto Scale UP: Will consider starting a new thread after every _ThreadStartupPerWorkItems work items scheduled</param>
        /// <param name="_ThreadStartupMinQueueSize">Auto Scale UP: Will only consider staring a new thread of the work item buffer across all clients is larger than this</param>
        /// <param name="_IdleWorkerThreadExitSeconds">Auto Scale DOWN: Worker threads which are idle for longer than this number of seconds will exit</param>
        /// <param name="_AbandonedResponseExpirySeconds">If a completed work item is not picked up because maybe the requesting thread crashed then it will be abandoned and removed from the outbound queue after this number of seconds</param>
        /// <param name="_ProcessRequestMethod">Delegate for processing work items. This is your Do Work method</param>
        /// <param name="_LogErrorMethod">Delegate for logging unhandled expcetions while trying to process work items</param>
        /// <param name="_LogMessageMethod">Delegate for logging info messages</param>
        public SimpleThreadedWorkItemProcessor(int _MaxWorkItemLimitPerClient, int _MaxWorkerThreads, int _ThreadStartupPerWorkItems, int _ThreadStartupMinQueueSize, int _IdleWorkerThreadExitSeconds, int _AbandonedResponseExpirySeconds
            , Handler_ProcessRequest _ProcessRequestMethod
            , Handler_LogError _LogErrorMethod
            , Handler_LogMessage _LogMessageMethod
            )
        {
            MaxWorkItemLimitPerClient = Math.Max(_MaxWorkItemLimitPerClient, 10);
            MaxWorkerThreads = Math.Max(_MaxWorkerThreads, 2);
            ThreadStartupDelayPerWorkItems = Math.Max(_ThreadStartupPerWorkItems, 0);
            ThreadStartupMinQueueSize = Math.Max(_ThreadStartupMinQueueSize, 0);
            IdleWorkerThreadExitMS = Math.Max(_IdleWorkerThreadExitSeconds, 1) * 1000;
            AbandonedResponseExpiryMS = Math.Max(_AbandonedResponseExpirySeconds, 10) * 1000;

            OnProcessRequest = _ProcessRequestMethod;
            OnLogError = _LogErrorMethod;
            OnLogMessage = _LogMessageMethod;
        }


        private object SyncRootQueue { get; } = new object();
        private Dictionary<TClientID, HashSet<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>>> PendingWorkItemClientQueue { get; } = new Dictionary<TClientID, HashSet<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>>>();
        private Dictionary<TClientID, DateTime> PendingWorkItemClientQueueActivity { get; } = new Dictionary<TClientID, DateTime>();
        private Queue<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>> PendingWorkItemFiFOQueue { get; } = new Queue<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>>();
        public int ScheduleWorkItem(TRequest request, TClientID clientID)
        {
            int newWorkItemID;
            SimpleThreadedWorkItem<TRequest, TResponse, TClientID> newWorkItem;
            HashSet<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>> clientQueue;
            Queue<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>> FifoQueue;
            newWorkItem = new SimpleThreadedWorkItem<TRequest, TResponse, TClientID>(clientID) { Request = request };
            newWorkItemID = newWorkItem.ID;
            lock (SyncRootQueue)
            {
                if (!PendingWorkItemClientQueue.TryGetValue(clientID, out clientQueue))
                {
                    clientQueue = new HashSet<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>>();
                    PendingWorkItemClientQueue.Add(clientID, clientQueue);
                }
                if (PendingWorkItemClientQueueActivity.ContainsKey(clientID))
                {
                    PendingWorkItemClientQueueActivity[clientID] = DateTime.Now;
                }
                else
                {
                    PendingWorkItemClientQueueActivity.Add(clientID, DateTime.Now);
                }
                if (clientQueue.Count > MaxWorkItemLimitPerClient) { return -1; }
                clientQueue.Add(newWorkItem);
                lock (SyncRootQueue)
                {
                    PendingWorkItemFiFOQueue.Enqueue(newWorkItem);
                }
            }
            StartWorkThread();
            return newWorkItemID;
        }
        public int QueueSizeAllClients()
        {
            lock (SyncRootQueue)
            {
                return PendingWorkItemFiFOQueue.Count;
            }
        }

        public bool GetNextPendingWorkItem(out SimpleThreadedWorkItem<TRequest, TResponse, TClientID> workItem)
        {
            HashSet<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>> clientQueue;
            lock (SyncRootQueue)
            {
                if (PendingWorkItemFiFOQueue.Count > 0)
                {
                    workItem = PendingWorkItemFiFOQueue.Dequeue();
                    if (PendingWorkItemClientQueue.TryGetValue(workItem.ClientID, out clientQueue))
                    {
                        clientQueue.Remove(workItem);
                    }
                    return true;
                }
            }
            workItem = null;
            return false;
        }

        private object SyncLockProcessedQueue { get; } = new object();
        Dictionary<int, SimpleThreadedWorkItem<TRequest, TResponse, TClientID>> ProcessedDict { get; } = new Dictionary<int, SimpleThreadedWorkItem<TRequest, TResponse, TClientID>>();
        private void OnRequestProcessed(SimpleThreadedWorkItem<TRequest, TResponse, TClientID> workItem)
        {
            lock(SyncLockProcessedQueue)
            {
                if (ProcessedDict.ContainsKey(workItem.ID))
                {
                    ProcessedDict[workItem.ID] = workItem;
                }
                else
                {
                    ProcessedDict.Add(workItem.ID, workItem);
                }
            }
        }
        public bool TryGetProcessedWorkItem(int ID, out SimpleThreadedWorkItem<TRequest, TResponse, TClientID> workItem)
        {
            lock (SyncLockProcessedQueue)
            {
                if (!ProcessedDict.TryGetValue(ID, out workItem))
                {
                    return false;
                }
                else
                {
                    ProcessedDict.Remove(ID);
                }
                double SecondsSinceLastClean = DateTime.Now.Subtract(CleanQueuesLastCleanTime).TotalSeconds;
                if (SecondsSinceLastClean > 5)
                {
                    CleanQueuesLastCleanTime = DateTime.Now;
                    System.Threading.Thread CleanThread;
                    CleanThread = new System.Threading.Thread(new System.Threading.ThreadStart(CleanQueues));
                    CleanThread.Start();
                }
            }
            return true;
        }
        private DateTime CleanQueuesLastCleanTime = DateTime.Now;
        private bool CleanQueues_IsRunning = false;
        private void CleanQueues()
        {
            bool UnsetProcessingFlag;
            UnsetProcessingFlag = false;
            try
            {
                List<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>> ExpiredItems = new List<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>>();
                List<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>> AllItems = new List<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>>();
                lock (SyncLockProcessedQueue)
                {
                    if (CleanQueues_IsRunning) { CleanQueuesLastCleanTime = DateTime.Now; return; }
                    AllItems = new List<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>>(ProcessedDict.Values);
                    UnsetProcessingFlag = true;
                    CleanQueues_IsRunning = true;
                }
                double ExpirySeconds = AbandonedResponseExpiryMS;
                foreach (SimpleThreadedWorkItem<TRequest, TResponse, TClientID> workItem in AllItems)
                {
                    if (workItem.ResponseAge.TotalSeconds > ExpirySeconds)
                    {
                        ExpiredItems.Add(workItem);
                    }
                }
                lock (SyncLockProcessedQueue)
                {
                    foreach (SimpleThreadedWorkItem<TRequest, TResponse, TClientID> workItem in ExpiredItems)
                    {
                        if (ProcessedDict.ContainsKey(workItem.ID))
                        {
                            ProcessedDict.Remove(workItem.ID);
                        }
                    }
                    CleanQueuesLastCleanTime = DateTime.Now;
                }
                DateTime CurrentTime = DateTime.Now;
                DateTime LastActiveTime;
                List<TClientID> ClientIDList;
                HashSet<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>> clientQueue;
                lock (SyncRootQueue)
                {
                    ClientIDList = new List<TClientID>(PendingWorkItemClientQueue.Keys);
                    foreach (TClientID clientID in ClientIDList)
                    {
                        if (PendingWorkItemClientQueueActivity.TryGetValue(clientID, out LastActiveTime))
                        {
                            if (CurrentTime.Subtract(LastActiveTime).TotalMinutes > 3)
                            {
                                if (PendingWorkItemClientQueue[clientID].Count == 0)
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
            finally
            {
                if (UnsetProcessingFlag)
                {
                    CleanQueues_IsRunning = false;
                }
            }
        }

        private object SyncLockThreadControl { get; } = new object();
        private Dictionary<int, SimpleWorkThread<TRequest, TResponse, TClientID>> ThreadDict { get; } = new Dictionary<int, SimpleWorkThread<TRequest, TResponse, TClientID>>();
        private int WatchDogCounter = 0;
        private int ThreadStartCounter = 0;
        private void StartWorkThread()
        {
            bool doStart;
            int CounterWatchDog;
            doStart = false;
            lock (SyncLockThreadControl)
            {
                if (ThreadDict.Count < MaxWorkerThreads)
                {
                    if (QueueSizeAllClients() > ThreadStartupMinQueueSize)
                    {
                        ThreadStartCounter++;
                        if (ThreadStartCounter >= ThreadStartupDelayPerWorkItems)
                        {
                            doStart = true;
                            ThreadStartCounter = 0;
                        }
                    }
                    WatchDogCounter = 0;
                }
                else
                {
                    ThreadStartCounter = 0;
                    WatchDogCounter++;
                }
                CounterWatchDog = WatchDogCounter;
            }
            if (doStart)
            {
                SimpleWorkThread<TRequest, TResponse, TClientID> newWorkThread;
                newWorkThread = new SimpleWorkThread<TRequest, TResponse, TClientID>(GetNextPendingWorkItem, OnProcessRequest, OnRequestProcessed, OnThreadExit, OnLogError, IdleWorkerThreadExitMS);
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
            else
            {
                if (CounterWatchDog > 20)
                {
                    lock (SyncLockThreadControl)
                    {
                        WatchDogCounter = 0;
                    }
                    try
                    {
                        OnLogMessage(@"Warning: Triggering thread watchdog because thread count at max work items added vs completed ratio exceeded 20 to 1.");
                    }
                    catch
                    {

                    }
                    System.Threading.Thread recoveryThread;
                    recoveryThread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadWatchDog));
                    recoveryThread.Start();
                }
            }
        }

        public void OnThreadExit(int ID)
        {
            SimpleWorkThread<TRequest, TResponse, TClientID> newWorkThread;
            bool doStart;

            doStart = false;
            newWorkThread = null;
            lock (SyncLockThreadControl)
            {
                if (ThreadDict.TryGetValue(ID, out newWorkThread))
                {
                    ThreadDict.Remove(ID);
                    if (ThreadDict.Count == 0)
                    {
                        doStart = true;
                    }
                }
            }
            if (doStart && (QueueSizeAllClients() > 0))
            {
                System.Threading.Thread recoveryThread;
                recoveryThread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadWatchDog));
                recoveryThread.Start();
            }
            if (newWorkThread != null)
            {
                newWorkThread.Dispose();
            }
            
        }
        private void ThreadWatchDog()
        {
            if (QueueSizeAllClients() > 0)
            {
                
                List<SimpleWorkThread<TRequest, TResponse, TClientID>> finishedThreads;
                finishedThreads = new List<SimpleWorkThread<TRequest, TResponse, TClientID>>();
                lock (SyncLockThreadControl)
                {
                    foreach (KeyValuePair<int, SimpleWorkThread<TRequest, TResponse, TClientID>> threadItem in ThreadDict)
                    {
                        if (!threadItem.Value.IsRunning)
                        {
                            finishedThreads.Add(threadItem.Value);
                        }
                    }
                    foreach (SimpleWorkThread<TRequest, TResponse, TClientID> workThread in finishedThreads)
                    {
                        if (ThreadDict.ContainsKey(workThread.ID))
                        {
                            ThreadDict.Remove(workThread.ID);
                        }
                    }
                }
                if (finishedThreads.Count > 0)
                {
                    OnLogMessage(string.Concat(@"Warning: Thread Watchdog found ", finishedThreads.Count.ToString(), @" stuck threads. Terminating stuck threads and triggering a new one."));
                }
                StartWorkThread();
                foreach (SimpleWorkThread<TRequest, TResponse, TClientID> workThread in finishedThreads)
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
    }
}
