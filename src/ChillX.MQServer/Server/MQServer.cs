using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using ChillX.Logging;
using ChillX.MQServer.Transport;
using ChillX.MQServer.UnitOfWork;
using ChillX.MQServer.Server.SystemMessage;
using ChillX.Threading.BulkProcessor;
using ChillX.Core.Structures;
using ChillX.MQServer.Service;

namespace ChillX.MQServer.Server
{
    public class MQServer
    {
        private static System.Runtime.GCLatencyMode GCLatencyModeOriginal = SwitchGCToLowLatencySustainedMode();
        private static System.Runtime.GCLatencyMode SwitchGCToLowLatencySustainedMode()
        {
            System.Runtime.GCLatencyMode previousSettings = System.Runtime.GCSettings.LatencyMode;
            if (System.Runtime.GCSettings.IsServerGC)
            {
                System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
            }
            return previousSettings;
        }

        private int m_OriginID;
        private int OriginID { get { return m_OriginID; } }

        private Dispatcher m_Dispatcher;
        private Dispatcher DispatcherInstance { get { return m_Dispatcher; } }

        public MQServer(int originID, string listenIPAdddress, string connectIPAddress, int listenBacklog = 128, int maxThreadPoolThreads = 128, int maxAsyncIOThreadPoolThreads = 128, bool runBenchMark = false)
        {
            IPAddress ip;
            int port;
            m_OriginID = originID;
            if (string.IsNullOrEmpty(listenIPAdddress))
            {
                throw new ArgumentException(string.Concat(@"Listen IP address is null or blank"));
            }
            foreach (string endPoint in listenIPAdddress.Split(@",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                string[] iPandPort = endPoint.Split(@":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (iPandPort.Length != 2)
                {
                    throw new ArgumentException(string.Concat(@"IP Address:", endPoint, @" is not a valid IP address. Example format 192.168.0.1:1234"));
                }
                if (IPAddress.TryParse(iPandPort[0], out ip))
                {
                    if (int.TryParse(iPandPort[1], out port))
                    {
                        ListenIPEndPointList.Add(new KeyValuePair<IPAddress, int>(ip, port));
                    }
                    else
                    {
                        throw new ArgumentException(string.Concat(@"Port :", endPoint, @" is not a valid port. Example format 192.168.0.1:1234"));
                    }
                }
                else
                {
                    throw new ArgumentException(string.Concat(@"IP Address:", endPoint, @" is not a valid IP address. Example format 192.168.0.1:1234"));
                }
            }
            foreach (string endPoint in connectIPAddress.Split(@",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                string[] iPandPort = endPoint.Split(@":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (iPandPort.Length != 2)
                {
                    throw new ArgumentException(string.Concat(@"IP Address:", endPoint, @" is not a valid IP address. Example format 192.168.0.1:1234"));
                }
                if (IPAddress.TryParse(iPandPort[0], out ip))
                {
                    if (int.TryParse(iPandPort[1], out port))
                    {
                        //ListenIPEndPointList.Add(new KeyValuePair<IPAddress, int>(ip, port));
                        OutboundConnectionList.Add(new ConnectionTCPSocket<MQPriority>(ip, port));
                        //for (int I = 0; I < 5; I++)
                        //{
                        //    OutboundConnectionList.Add(new ConnectionTCPSocket<MQPriority>(ip, port));
                        //}
                    }
                    else
                    {
                        throw new ArgumentException(string.Concat(@"Port :", endPoint, @" is not a valid port. Example format 192.168.0.1:1234"));
                    }
                }
                else
                {
                    throw new ArgumentException(string.Concat(@"IP Address:", endPoint, @" is not a valid IP address. Example format 192.168.0.1:1234"));
                }
            }
            ListenBacklog = listenBacklog;
            MaxThreadPoolThreads = maxThreadPoolThreads;
            MaxAsyncIOThreadPoolThreads = maxAsyncIOThreadPoolThreads;
            RunBenchMark = runBenchMark;
            m_Dispatcher = new Dispatcher(OriginID, 60d, false);
        }
        private List<KeyValuePair<IPAddress, int>> ListenIPEndPointList { get; } = new List<KeyValuePair<IPAddress, int>>();
        public IReadOnlyList<KeyValuePair<IPAddress, int>> ListeningIPEndPointList { get { return ListenIPEndPointList.AsReadOnly(); } }
        public int ListenBacklog { get; private set; }
        public int MaxThreadPoolThreads { get; private set; }
        public int MaxAsyncIOThreadPoolThreads { get; private set; }

        public bool RunBenchMark { get; private set; }

        private object SyncRootControl = new object();
        private bool m_IsListening = false;
        public bool IsListening
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_IsListening;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_IsListening = value;
                }
            }
        }

        private bool m_IsShutDown = false;
        public bool IsShutDown
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_IsShutDown;
                }
            }
        }

        private readonly Random rnd = new Random();
        private const int BenchmarkArraySize = 64;

        private List<TcpListener> tcpListeners = new List<TcpListener>();
        private ThreadedWorkItemProcessor<ServerConnectionWorkItem, ServerConnectionWorkItem, int, MQPriority> ThreadedUOWProcessor;
        private Thread ProcessorSocketsPendingThread = null;
        private Thread ProcessorSocketsPendingThread_Reader = null;
        private Thread ProcessorSocketsPendingThread_Writer = null;
        //private Thread ProcessorSocketsPendingThread_SendWorkItems = null;
        //private Thread ProcessorSocketsPendingThread_SendSocketData = null;
        //private Thread ProcessorSocketsPendingThread_ReadSocketData = null;
        //private Thread ProcessorSocketsPendingThread_ReadWorkItems = null;

        public void Start(int _maxWorkItemLimitPerClient = int.MaxValue, int _maxWorkerThreads = 8, int _threadStartupPerWorkItems = 4, int _threadStartupMinQueueSize = 4, int _idleWorkerThreadExitSeconds = 15, int _processedQueueMaxSize = 0)
        {
            if (!IsListening)
            {
                lock (SyncRootControl)
                {
                    if (!m_IsListening)
                    {
                        m_IsListening = true;
                        ThreadedUOWProcessor = new ThreadedWorkItemProcessor<ServerConnectionWorkItem, ServerConnectionWorkItem, int, MQPriority>(
                            _maxWorkItemLimitPerClient: _maxWorkItemLimitPerClient // Maximum number of concurrent requests in the processing queue per client
                            , _maxWorkerThreads: _maxWorkerThreads // Maximum number of threads to scale upto
                            , _threadStartupPerWorkItems: _threadStartupPerWorkItems // Consider starting a new processing thread ever X requests
                            , _threadStartupMinQueueSize: _threadStartupMinQueueSize // Do NOT start a new processing thread if work item queue is below this size
                            , _idleWorkerThreadExitSeconds: _idleWorkerThreadExitSeconds // Idle threads will exit after X seconds
                            , _processedQueueMaxSize: _processedQueueMaxSize // Max size of processed queue. This is really a buffer for sudden bursts
                            , _processedItemAutoDispose: _processedQueueMaxSize <= 0
                            , _processRequestMethod: ProcessRequest // Your Do Work method for processing the request
                            , _logErrorMethod: Handler_LogError
                            , _logMessageMethod: Handler_LogMessage
                            );
                        int minThreads;
                        minThreads = Environment.ProcessorCount;
                        if (MaxThreadPoolThreads < minThreads) { MaxThreadPoolThreads = minThreads + 2; }
                        if (MaxAsyncIOThreadPoolThreads < minThreads) { MaxAsyncIOThreadPoolThreads = minThreads + 2; }
                        ThreadPool.SetMinThreads(minThreads, minThreads);
                        ThreadPool.SetMaxThreads(MaxThreadPoolThreads, MaxThreadPoolThreads);

                        if (tcpListeners.Count > 0)
                        {
                            @"TCPServerBase.Start() IsListening is false but tcpListeners list is not empty. This should not happen!!!".LogEntry(CXMQUtility.LogSeverity.warning);
                            foreach (TcpListener listener in tcpListeners)
                            {
                                listener.Stop();
                            }
                            tcpListeners.Clear();
                        }
                        foreach (KeyValuePair<IPAddress, int> endPoint in ListenIPEndPointList)
                        {
                            TcpListener listener;
                            listener = new TcpListener(endPoint.Key, endPoint.Value);
                            listener.Start();
                            tcpListeners.Add(listener);
                        }
                        ProcessorSocketsPendingThread = new Thread(new ThreadStart(ThreadProcessorSockets));
                        ProcessorSocketsPendingThread.Name = @"SocketProcessorThread";
                        ProcessorSocketsPendingThread.Start();

                        ProcessorSocketsPendingThread_Reader = new Thread(new ThreadStart(ThreadProcessortSockets_Reader));
                        ProcessorSocketsPendingThread_Reader.Name = @"SocketProcessorThread_Reader";
                        ProcessorSocketsPendingThread_Reader.Start();

                        ProcessorSocketsPendingThread_Writer = new Thread(new ThreadStart(ThreadProcessortSockets_Writer));
                        ProcessorSocketsPendingThread_Writer.Name = @"SocketProcessorThread_Writer";
                        ProcessorSocketsPendingThread_Writer.Start();

                        //ProcessorSocketsPendingThread_SendWorkItems = new Thread(new ThreadStart(ThreadProcessorSockets_SendWorkItems));
                        //ProcessorSocketsPendingThread_SendWorkItems.Start();
                        //ProcessorSocketsPendingThread_SendSocketData = new Thread(new ThreadStart(ThreadProcessorSockets_SendSocketData));
                        //ProcessorSocketsPendingThread_SendSocketData.Start();
                        //ProcessorSocketsPendingThread_ReadSocketData = new Thread(new ThreadStart(ThreadProcessorSockets_ReadSocketData));
                        //ProcessorSocketsPendingThread_ReadSocketData.Start();
                        //ProcessorSocketsPendingThread_ReadWorkItems = new Thread(new ThreadStart(ThreadProcessorSockets_ReadWorkItems));
                        //ProcessorSocketsPendingThread_ReadWorkItems.Start();
                    }
                }
            }
        }

        public void ShutDown()
        {
            lock (SyncRootControl)
            {
                m_IsShutDown = true;
            }
            IsListening = false;
        }

        public void RegisterLocalService(IMQServiceBase service)
        {
            DispatcherInstance.RegisterLocalService(service);
        }

        private Dictionary<int, ConnectionTCPSocket<MQPriority>> m_ActiveClientDict = new Dictionary<int, ConnectionTCPSocket<MQPriority>>();
        private ReaderWriterLockSlim SyncActiveClientDict = new ReaderWriterLockSlim();

        private void ActiveClientAdd(ConnectionTCPSocket<MQPriority> client)
        {
            SyncActiveClientDict.EnterWriteLock();
            try
            {
                if (!m_ActiveClientDict.ContainsKey(client.UniqueID))
                {
                    m_ActiveClientDict.Add(client.UniqueID, client);
                }
            }
            finally
            {
                SyncActiveClientDict.ExitWriteLock();
            }
        }
        private void ActiveClientRemove(ConnectionTCPSocket<MQPriority> client)
        {
            SyncActiveClientDict.EnterWriteLock();
            try
            {
                if (!m_ActiveClientDict.ContainsKey(client.UniqueID))
                {
                    m_ActiveClientDict.Remove(client.UniqueID);
                }
            }
            finally
            {
                SyncActiveClientDict.ExitWriteLock();
            }
        }
        private void ActiveClientGetList(List<ConnectionTCPSocket<MQPriority>> clientList)
        {
            clientList.Clear();
            SyncActiveClientDict.EnterReadLock();
            try
            {
                if (m_ActiveClientDict.Count > 0)
                {
                    clientList.AddRange(m_ActiveClientDict.Values);
                }
            }
            finally
            {
                SyncActiveClientDict.ExitReadLock();
            }
        }

        private HashSet<int> BenchMarksInTransit { get; } = new HashSet<int>();
        private int BenchMarksInTransitCount
        {
            get
            {
                SyncActiveClientDict.EnterReadLock();
                try
                {
                    return BenchMarksInTransit.Count;
                }
                finally
                {
                    SyncActiveClientDict.ExitReadLock();
                }
            }
        }
        private void BenchMarksInTransit_Add(int UniqueID)
        {
            SyncActiveClientDict.EnterWriteLock();
            try
            {
                BenchMarksInTransit.Add(UniqueID);
            }
            finally
            {
                SyncActiveClientDict.ExitWriteLock();
            }
        }
        private void BenchMarksInTransit_Remove(int UniqueID)
        {
            SyncActiveClientDict.EnterWriteLock();
            try
            {
                BenchMarksInTransit.Remove(UniqueID);
            }
            finally
            {
                SyncActiveClientDict.ExitWriteLock();
            }
        }

        private volatile int m_BenchMarkMessageCount = 0;
        public int BenchMarkMessageCount { get { return m_BenchMarkMessageCount; } }
        private void BenchMark()
        {
            List<ConnectionTCPSocket<MQPriority>> ConnectedClientList = new List<ConnectionTCPSocket<MQPriority>>();
            UOWBenchMark Payload;
            Payload = new UOWBenchMark().RandomizeData(rnd, BenchmarkArraySize);
            Stopwatch SW = new Stopwatch();
            ActiveClientGetList(ConnectedClientList);
            SW.Start();
            while (IsListening)
            {
                if (SW.Elapsed.TotalSeconds > 1d)
                {
                    ActiveClientGetList(ConnectedClientList);
                }
                if (ConnectedClientList.Count > 0)
                {
                    for (int I = 0; I < 10; I++)
                    {
                        foreach (ConnectionTCPSocket<MQPriority> client in ConnectedClientList)
                        {
                            if (client.IsOutBound && client.IsConnected)
                            {
                                while (BenchMarksInTransitCount < 500)
                                {
                                    WorkItemBase<UOWBenchMark, UOWBenchMark> BenchMarkWorkItem;
                                    BenchMarkWorkItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>((int)SystemServiceType.System, (int)SystemServiceModule.Transport, (int)SystemServiceFunction.BenchMark,
                                        (int)SystemServiceType.System, (int)SystemServiceModule.Transport, (int)SystemServiceFunction.BenchMark, MQPriority.System);

                                    //Todo: Remove This
                                    if (Core.Common.EnableDebug)
                                    {
                                        Payload.ArrayProperty_Char += string.Concat(@"Send ID: ", BenchMarkWorkItem.UniqueID.ToString()).ToCharArray();
                                    }

                                    BenchMarkWorkItem.RequestDetail.WorkItemData = Payload.Clone();
                                    //BenchMarkWorkItem.RequestDetail.WorkItemData = new UOWBenchMark().RandomizeData(rnd, BenchmarkArraySize);
                                    BenchMarksInTransit_Add(BenchMarkWorkItem.UniqueID);

                                    client.SendWorkItemQueue.Enqueue(BenchMarkWorkItem);
                                    client.BenchMarkSent();
                                    Interlocked.Increment(ref m_BenchMarkMessageCount);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        //private readonly ThreadSafeQueue<ConnectionTCPSocket<MQPriority>> Queue_SendWorkItems = new ThreadSafeQueue<ConnectionTCPSocket<MQPriority>>();
        //private readonly ThreadSafeQueue<ConnectionTCPSocket<MQPriority>> Queue_SendSocketData = new ThreadSafeQueue<ConnectionTCPSocket<MQPriority>>();
        //private readonly ThreadSafeQueue<ConnectionTCPSocket<MQPriority>> Queue_ReadSocketData = new ThreadSafeQueue<ConnectionTCPSocket<MQPriority>>();
        //private readonly ThreadSafeQueue<ConnectionTCPSocket<MQPriority>> Queue_ReadWorkItems = new ThreadSafeQueue<ConnectionTCPSocket<MQPriority>>();
        //private void QueueAddConnectedClient(ConnectionTCPSocket<MQPriority> client)
        //{
        //    Queue_SendWorkItems.Enqueue(client);
        //    Queue_SendSocketData.Enqueue(client);
        //    Queue_ReadSocketData.Enqueue(client);
        //    Queue_ReadWorkItems.Enqueue(client);
        //}
        //private void ThreadProcessorSockets_SendWorkItems()
        //{
        //    Dictionary<int, ConnectionTCPSocket<MQPriority>> processingClientDict = new Dictionary<int, ConnectionTCPSocket<MQPriority>>();
        //    List<ConnectionTCPSocket<MQPriority>> processingClientList = new List<ConnectionTCPSocket<MQPriority>>();
        //    ConnectionTCPSocket<MQPriority> client;
        //    bool success;
        //    bool isChanged;
        //    int loopCounter;
        //    while (IsListening)
        //    {
        //        isChanged = false;
        //        client = Queue_SendWorkItems.DeQueue(out success);
        //        while (success)
        //        {
        //            isChanged = true;
        //            if (processingClientDict.ContainsKey(client.UniqueID))
        //            {
        //                processingClientDict[client.UniqueID] = client;
        //            }
        //            else
        //            {
        //                processingClientDict.Add(client.UniqueID, client);
        //            }
        //            client = Queue_SendWorkItems.DeQueue(out success);
        //        }
        //        if (isChanged)
        //        {
        //            processingClientList.Clear();
        //            processingClientList.AddRange(processingClientDict.Values);
        //            isChanged = false;
        //        }
        //        foreach (ConnectionTCPSocket<MQPriority>  connectedClient in processingClientList)
        //        {
        //            if (connectedClient.IsConnected)
        //            {
        //                loopCounter = 0;
        //                while (connectedClient.SendWorkItems())
        //                {
        //                    loopCounter++;
        //                    if (loopCounter > 3) { break; }
        //                }
        //            }
        //            else
        //            {
        //                isChanged = true;
        //                processingClientDict.Remove(connectedClient.UniqueID);
        //            }
        //        }
        //        if (isChanged)
        //        {
        //            processingClientList.Clear();
        //            processingClientList.AddRange(processingClientDict.Values);
        //        }
        //    }
        //}

        //private void ThreadProcessorSockets_SendSocketData()
        //{
        //    Dictionary<int, ConnectionTCPSocket<MQPriority>> processingClientDict = new Dictionary<int, ConnectionTCPSocket<MQPriority>>();
        //    List<ConnectionTCPSocket<MQPriority>> processingClientList = new List<ConnectionTCPSocket<MQPriority>>();
        //    ConnectionTCPSocket<MQPriority> client;
        //    bool success;
        //    bool isChanged;
        //    int loopCounter;
        //    while (IsListening)
        //    {
        //        isChanged = false;
        //        client = Queue_SendSocketData.DeQueue(out success);
        //        while (success)
        //        {
        //            isChanged = true;
        //            if (processingClientDict.ContainsKey(client.UniqueID))
        //            {
        //                processingClientDict[client.UniqueID] = client;
        //            }
        //            else
        //            {
        //                processingClientDict.Add(client.UniqueID, client);
        //            }
        //            client = Queue_SendSocketData.DeQueue(out success);
        //        }
        //        if (isChanged)
        //        {
        //            processingClientList.Clear();
        //            processingClientList.AddRange(processingClientDict.Values);
        //            isChanged = false;
        //        }
        //        foreach (ConnectionTCPSocket<MQPriority> connectedClient in processingClientList)
        //        {
        //            if (connectedClient.IsConnected)
        //            {
        //                loopCounter = 0;
        //                while (connectedClient.SendSocketData())
        //                {
        //                    loopCounter++;
        //                    if (loopCounter > 3) { break; }
        //                }
        //            }
        //            else
        //            {
        //                isChanged = true;
        //                processingClientDict.Remove(connectedClient.UniqueID);
        //            }
        //        }
        //        if (isChanged)
        //        {
        //            processingClientList.Clear();
        //            processingClientList.AddRange(processingClientDict.Values);
        //        }
        //    }
        //}

        //private void ThreadProcessorSockets_ReadSocketData()
        //{
        //    Dictionary<int, ConnectionTCPSocket<MQPriority>> processingClientDict = new Dictionary<int, ConnectionTCPSocket<MQPriority>>();
        //    List<ConnectionTCPSocket<MQPriority>> processingClientList = new List<ConnectionTCPSocket<MQPriority>>();
        //    ConnectionTCPSocket<MQPriority> client;
        //    bool success;
        //    bool isChanged;
        //    int loopCounter;
        //    while (IsListening)
        //    {
        //        isChanged = false;
        //        client = Queue_ReadSocketData.DeQueue(out success);
        //        while (success)
        //        {
        //            isChanged = true;
        //            if (processingClientDict.ContainsKey(client.UniqueID))
        //            {
        //                processingClientDict[client.UniqueID] = client;
        //            }
        //            else
        //            {
        //                processingClientDict.Add(client.UniqueID, client);
        //            }
        //            client = Queue_ReadSocketData.DeQueue(out success);
        //        }
        //        if (isChanged)
        //        {
        //            processingClientList.Clear();
        //            processingClientList.AddRange(processingClientDict.Values);
        //            isChanged = false;
        //        }
        //        foreach (ConnectionTCPSocket<MQPriority> connectedClient in processingClientList)
        //        {
        //            if (connectedClient.IsConnected)
        //            {
        //                loopCounter = 0;
        //                while (connectedClient.ReadSocketData())
        //                {
        //                    loopCounter++;
        //                    if (loopCounter > 3) { break; }
        //                }
        //            }
        //            else
        //            {
        //                isChanged = true;
        //                processingClientDict.Remove(connectedClient.UniqueID);
        //            }
        //        }
        //        if (isChanged)
        //        {
        //            processingClientList.Clear();
        //            processingClientList.AddRange(processingClientDict.Values);
        //        }
        //    }
        //}
        //private void ThreadProcessorSockets_ReadWorkItems()
        //{
        //    Dictionary<int, ConnectionTCPSocket<MQPriority>> processingClientDict = new Dictionary<int, ConnectionTCPSocket<MQPriority>>();
        //    List<ConnectionTCPSocket<MQPriority>> processingClientList = new List<ConnectionTCPSocket<MQPriority>>();
        //    ConnectionTCPSocket<MQPriority> client;
        //    bool success;
        //    bool isChanged;
        //    int loopCounter;
        //    while (IsListening)
        //    {
        //        isChanged = false;
        //        client = Queue_ReadWorkItems.DeQueue(out success);
        //        while (success)
        //        {
        //            isChanged = true;
        //            if (processingClientDict.ContainsKey(client.UniqueID))
        //            {
        //                processingClientDict[client.UniqueID] = client;
        //            }
        //            else
        //            {
        //                processingClientDict.Add(client.UniqueID, client);
        //            }
        //            client = Queue_ReadWorkItems.DeQueue(out success);
        //        }
        //        if (isChanged)
        //        {
        //            processingClientList.Clear();
        //            processingClientList.AddRange(processingClientDict.Values);
        //            isChanged = false;
        //        }
        //        foreach (ConnectionTCPSocket<MQPriority> connectedClient in processingClientList)
        //        {
        //            if (connectedClient.IsConnected)
        //            {
        //                loopCounter = 0;
        //                while (connectedClient.ReadWorkItems())
        //                {
        //                    loopCounter++;
        //                    if (loopCounter > 3) { break; }
        //                }
        //            }
        //            else
        //            {
        //                isChanged = true;
        //                processingClientDict.Remove(connectedClient.UniqueID);
        //            }
        //        }
        //        if (isChanged)
        //        {
        //            processingClientList.Clear();
        //            processingClientList.AddRange(processingClientDict.Values);
        //        }
        //    }
        //}

        private HashSet<ConnectionTCPSocket<MQPriority>> OutboundConnectionList { get; } = new HashSet<ConnectionTCPSocket<MQPriority>>();
        private Dictionary<int, ConnectionTCPSocket<MQPriority>> ConnectedClients { get; } = new Dictionary<int, ConnectionTCPSocket<MQPriority>>();

        private void ThreadProcessortSockets_Reader()
        {
            //return;
            List<ConnectionTCPSocket<MQPriority>> ConnectedClientList = new List<ConnectionTCPSocket<MQPriority>>();
            ActiveClientGetList(ConnectedClientList);
            Stopwatch SW = new Stopwatch();
            SW.Start();
            bool SleepOne;
            int loopCounter;
            while (IsListening)
            {
                SleepOne = true;
                if (SW.Elapsed.TotalSeconds > 0.5d)
                {
                    ActiveClientGetList(ConnectedClientList);
                }
                if (ConnectedClientList.Count > 0)
                {
                    foreach (ConnectionTCPSocket<MQPriority> client in ConnectedClientList)
                    {
                        if (client.ReadSocketData())
                        {
                            SleepOne = false;
                            loopCounter = 0;
                            while (client.ReadSocketData())
                            {
                                loopCounter++;
                                if (loopCounter > 3)
                                {
                                    break;
                                }
                                //Just loop
                            }
                        }
                    }
                }
                if(SleepOne)
                {
                    Thread.Sleep(1);
                }
            }
        }
        private void ThreadProcessortSockets_Writer()
        {
            //return;
            List<ConnectionTCPSocket<MQPriority>> ConnectedClientList = new List<ConnectionTCPSocket<MQPriority>>();
            ActiveClientGetList(ConnectedClientList);
            Stopwatch SW = new Stopwatch();
            SW.Start();
            ActiveClientGetList(ConnectedClientList);
            Queue<RentedBuffer<byte>> SendSocketDataQueue;
            SendSocketDataQueue = new Queue<RentedBuffer<byte>>();
            bool SleepOne;
            int loopCounter;
            while (IsListening)
            {
                SleepOne = true;
                if (SW.Elapsed.TotalSeconds > 1d)
                {
                    ActiveClientGetList(ConnectedClientList);
                }
                if (ConnectedClientList.Count > 0)
                {
                    foreach (ConnectionTCPSocket<MQPriority> client in ConnectedClientList)
                    {
                        if (client.SendSocketData(SendSocketDataQueue))
                        {
                            SleepOne = false;
                            loopCounter = 0;
                            while (client.SendSocketData(SendSocketDataQueue))
                            {
                                loopCounter++;
                                if (loopCounter > 3) 
                                {
                                    break; 
                                }
                                //Just loop
                            }
                        }

                    }
                }
                if(SleepOne)
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void ThreadProcessorSockets()
        {
            //List<ThreadWorkItem<ServerConnectionWorkItem, ServerConnectionWorkItem, int>> processedWorkItemList;
            //processedWorkItemList = new List<ThreadWorkItem<ServerConnectionWorkItem, ServerConnectionWorkItem, int>>();

            List<ConnectionTCPSocket<MQPriority>> ConnectedClientList;
            ConnectedClientList = new List<ConnectionTCPSocket<MQPriority>>(ConnectedClients.Values);

            List<ConnectionTCPSocket<MQPriority>> OutboundConnected;
            OutboundConnected = new List<ConnectionTCPSocket<MQPriority>>();

            Queue<RentedBuffer<byte>> ReadWorkItemsQueue;
            ReadWorkItemsQueue = new Queue<RentedBuffer<byte>>();

            Queue<WorkItemBaseCore> SendWorkItemsQueue;
            SendWorkItemsQueue = new Queue<WorkItemBaseCore>();

            Queue<RentedBuffer<byte>> SendSocketDataQueue;
            SendSocketDataQueue = new Queue<RentedBuffer<byte>>();

            Stopwatch SWBenchMark = null;
            int BenchCounter = 100;
            if (RunBenchMark)
            {
                SWBenchMark = new Stopwatch();
                SWBenchMark.Start();
                System.Threading.Thread BenchThread;
                BenchThread = new Thread(new ThreadStart(BenchMark));
                BenchThread.Start();
                //for (int I = 0; I < 4; I++)
                //{
                //    BenchThread = new Thread(new ThreadStart(BenchMark));
                //    BenchThread.Start();
                //}
            }
            int TempCounter = 0;
            while (IsListening)
            {
                try
                {
                    foreach (TcpListener listener in tcpListeners)
                    {
                        while (listener.Pending())
                        {
                            ConnectionTCPSocket<MQPriority> client;
                            client = new ConnectionTCPSocket<MQPriority>(listener.AcceptTcpClient());
                            ConnectedClients.Add(client.UniqueID, client);
                            ActiveClientAdd(client);
                            WorkItemBase<UOWDiscovery, UOWDiscovery> DiscoveryRequest;
                            DiscoveryRequest = new WorkItemBase<UOWDiscovery, UOWDiscovery>((int)SystemServiceType.System, (int)SystemServiceModule.Transport, (int)SystemServiceFunction.Discovery,
                                (int)SystemServiceType.System, (int)SystemServiceModule.Transport, (int)SystemServiceFunction.Discovery,MQPriority.System);
                            DiscoveryRequest.RequestDetail.WorkItemData = DispatcherInstance.DiscoveryData();
                            DiscoveryRequest.RequestReply();
                            client.SendWorkItemQueue.Enqueue(DiscoveryRequest);
                            //QueueAddConnectedClient(client);
                        }
                    }
                    if (OutboundConnectionList.Count > 0)
                    {
                        OutboundConnected.Clear();
                        foreach (ConnectionTCPSocket<MQPriority> client in OutboundConnectionList)
                        {
                            if (client.Connect())
                            {
                                ConnectedClients.Add(client.UniqueID, client);
                                OutboundConnected.Add(client);
                                //QueueAddConnectedClient(client);

                            }
                        }
                        foreach (ConnectionTCPSocket<MQPriority> client in OutboundConnected)
                        {
                            OutboundConnectionList.Remove(client);
                            ActiveClientAdd(client);
                        }
                    }
                    ConnectedClientList.Clear();
                    ConnectedClientList.AddRange(ConnectedClients.Values);
                    foreach (ConnectionTCPSocket<MQPriority> client in ConnectedClientList)
                    {
                        if (client.IsConnected)
                        {
                            WorkItemBaseCore workItem;
                            WorkItemBaseCore workItemSystemResponse;
                            int loopCounter;
                            loopCounter = 0;
                            //while (client.ReadSocketData())
                            //{
                            //    loopCounter++;
                            //    if (loopCounter > 32)
                            //    {
                            //        break;
                            //    }
                            //    //Just loop
                            //}
                            //loopCounter = 0;
                            //while (client.ReadWorkItems())
                            //{
                            //    loopCounter++;
                            //    if (loopCounter > 3) { break; }
                            //    //Just loop
                            //}
                            client.ReadWorkItems(ReadWorkItemsQueue);
                            client.Ping();

                            workItem = client.ReadWorkItemQueue.DeQueue();
                            while (workItem != null)
                            {
                                //if (workItem.IsSystemRequest)
                                //{
                                //    workItemSystemResponse = ProcessSystemRequest(workItem, client);
                                //    if (workItemSystemResponse != null)
                                //    {
                                //        client.SendWorkItemQueue.Enqueue(workItemSystemResponse);
                                //    }
                                //    workItem.Dispose();
                                //}
                                //else
                                //{
                                //}
                                //ProcessRequestSync(client, workItem);
                                ThreadedUOWProcessor.ScheduleWorkItem(workItem.Priority, new ServerConnectionWorkItem(this, client, workItem), 0);
                                workItem = client.ReadWorkItemQueue.DeQueue();
                            }
                            DispatcherInstance.SendWorkItems();
                            client.SendWorkItems(SendWorkItemsQueue);
                            //loopCounter = 0;
                            //while (client.SendWorkItems())
                            //{
                            //    loopCounter++;
                            //    if (loopCounter > 3) { break; }
                            //    //Just loop
                            //}
                            //loopCounter = 0;
                            //while (client.SendSocketData())
                            //{
                            //    loopCounter++;
                            //    if (loopCounter > 3) { break; }
                            //    //Just loop
                            //}

                            //client.SendSocketData();
                        }
                        else
                        {
                            ConnectedClients.Remove(client.UniqueID);
                            ActiveClientRemove(client);
                            if (client.IsOutBound)
                            {
                                OutboundConnectionList.Add(client);
                            }
                            else
                            {
                                client.Dispose();
                            }
                        }

                    }

                    if (RunBenchMark)
                    {
                        //BenchCounter--;
                        //if ((TempCounter < 1000000) &&(BenchCounter <= 0))
                        //{
                        //    BenchCounter = 0;
                        //    foreach (ConnectionTCPSocket<MQPriority> client in ConnectedClientList)
                        //    {
                        //        if (client.IsConnected && (client.BenchMarksInTransit < 100))
                        //        {
                        //            for (int I = 0; I < 10; I++)
                        //            {
                        //                WorkItemBase<UOWBenchMark, UOWBenchMark> BenchMarkWorkItem;
                        //                BenchMarkWorkItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>((int)SystemServiceType.System, (int)SystemServiceModule.Transport, (int)SystemServiceFunction.BenchMark,
                        //                    (int)SystemServiceType.System, (int)SystemServiceModule.Transport, (int)SystemServiceFunction.BenchMark, MQPriority.System);
                        //                BenchMarkWorkItem.RequestDetail.WorkItemData = new UOWBenchMark(BenchMarkPayload);
                        //                BenchMarksInTransit.Add(BenchMarkWorkItem.UniqueID);
                        //                client.SendWorkItemQueue.Enqueue(BenchMarkWorkItem);
                        //                client.BenchMarkSent();
                        //                TempCounter++;
                        //            }
                        //        }
                        //    }
                        //}
                        if (SWBenchMark.Elapsed.TotalSeconds >= 1)
                        {
                            double BandwidthIn = 0;
                            double BandwidthOut = 0;
                            int MessagesIn = 0;
                            int MessagesOut = 0;
                            double ElapsedMS = SWBenchMark.ElapsedMilliseconds;
                            int SendData = 0;
                            int SendWork = 0;
                            int RecvWork = 0;
                            int RecvData = 0;
                            int BenchCount = 0;
                            long PingTicks = 0;
                            foreach (ConnectionTCPSocket<MQPriority> client in ConnectedClientList)
                            {
                                client.CalcBandwidth(ElapsedMS, false);
                                BandwidthIn += client.BandwidthInMbps;
                                BandwidthOut += client.BandwidthOutMbps;
                                MessagesIn += client.MessageCountIn;
                                MessagesOut += client.MessageCountOut;
                                SendData += client.SendDataQueue.Count;
                                SendWork += client.SendWorkItemQueue.Count;
                                RecvData += client.ReadDataQueue.Count;
                                RecvWork += client.ReadWorkItemQueue.Count;
                                BenchCount += client.BenchMarksInTransit;
                                PingTicks += client.Latency.Ticks;
                            }
                            int NumClients = ConnectedClientList.Count;
                            SendData = SendData / NumClients;
                            SendWork = SendWork / NumClients;
                            RecvWork = RecvWork / NumClients;
                            BenchCount = BenchCount / NumClients;
                            PingTicks = PingTicks / NumClients;
                            SWBenchMark.Restart();
                            lock (SyncRootControl)
                            {
                                m_BandwidthInMbps = BandwidthIn;
                                m_BandwidthOutMbps = BandwidthOut;
                                m_MessageCountIn = MessagesIn;
                                m_MessageCountOut = MessagesOut;
                                m_SendDataSize = SendData;
                                m_SendWorkSize = SendWork;
                                m_RecieveWorkSize = RecvWork;
                                m_RecieveDataSize = RecvData;
                                m_ProcessingSize = ThreadedUOWProcessor.QueueSizeAllClients();
                                m_BenchTransit = BenchCount;
                                m_PingTimeTicks = PingTicks;
                            }
                        }
                    }


                    ThreadWorkItem<ServerConnectionWorkItem, ServerConnectionWorkItem, int> processedWorkItem;
                    //processedWorkItemList.Clear();
                    //while (ThreadedUOWProcessor.TryGetProcessedWorkItem(out processedWorkItem))
                    //{
                    //    processedWorkItemList.Add(processedWorkItem);
                    //}
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    ex.Log(@"Generic Error in MQServer while processing work items. Please check work item serialization.", LogSeverity.error);
                }
            }
        }


        private double m_BandwidthInMbps = 0d;
        public double BandwidthInMbps
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_BandwidthInMbps;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_BandwidthInMbps = value;
                }
            }
        }
        private double m_BandwidthOutMbps = 0d;
        public double BandwidthOutMbps
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_BandwidthOutMbps;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_BandwidthOutMbps = value;
                }
            }
        }
        private int m_MessageCountIn = 0;
        public int MessageCountIn
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_MessageCountIn;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_MessageCountIn = value;
                }
            }
        }
        private int m_MessageCountOut = 0;
        public int MessageCountOut
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_MessageCountOut;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_MessageCountOut = value;
                }
            }
        }
        private int m_SendWorkSize = 0;
        public int SendWorkSize
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_SendWorkSize;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_SendWorkSize = value;
                }
            }
        }
        private int m_RecieveWorkSize = 0;
        public int RecieveWorkSize
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_RecieveWorkSize;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_RecieveWorkSize = value;
                }
            }
        }
        private int m_RecieveDataSize = 0;
        public int RecieveDataSize
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_RecieveDataSize;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_RecieveDataSize = value;
                }
            }
        }
        private int m_SendDataSize = 0;
        public int SendDataSize
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_SendDataSize;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_SendDataSize = value;
                }
            }
        }
        private int m_ProcessingSize = 0;
        public int ProcessingSize
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_ProcessingSize;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_ProcessingSize = value;
                }
            }
        }
        private int m_BenchTransit = 0;
        public int BenchTransit
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_BenchTransit;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_BenchTransit = value;
                }
            }
        }
        private long m_PingTimeTicks = 0;
        public long PingTimeTicks
        {
            get
            {
                lock (SyncRootControl)
                {
                    return m_PingTimeTicks;
                }
            }
            private set
            {
                lock (SyncRootControl)
                {
                    m_PingTimeTicks = value;
                }
            }
        }

        private WorkItemBaseCore ProcessSystemRequest(WorkItemBaseCore request, ConnectionTCPSocket<MQPriority> client)
        {
            try
            {
                try
                {
                    switch (request.DestinationServiceType)
                    {
                        case (int)SystemServiceType.System:
                            switch (request.DestinationServiceModule)
                            {
                                case (int)SystemServiceModule.Transport:
                                    switch (request.DestinationServiceFunction)
                                    {
                                        case (int)SystemServiceFunction.PingPong:
                                            return ProcessSystemRequest_PingPong(request, client);
                                        case ((int)SystemServiceFunction.Discovery):
                                            return ProcessSystemRequest_Discovery(request, client);
                                        case (int)SystemServiceFunction.BenchMark:
                                            return ProcessSystemRequest_BenchMark(request, client);
                                        default:
                                            return request.CreateUnprocessedErrorReply(ResponseStatusCode.DestinationUnreachable);
                                    }
                                default:
                                    return request.CreateUnprocessedErrorReply(ResponseStatusCode.DestinationUnreachable);
                            }
                        default:
                            return request.CreateUnprocessedErrorReply(ResponseStatusCode.DestinationUnreachable);
                    }
                }
                catch (Exception ex)
                {
                    return request.CreateUnprocessedErrorReply(ResponseStatusCode.ProcessingError, ex.Log(@"Unhandled Exception in TCPServerBase.ProcessSystemRequest. This is very bad!!!", LogSeverity.error).MessageException.ToString());
                }
            }
            catch (Exception ex2)
            {
                try
                {
                    ex2.Log(@"Unhandled Exception in TCPServerBase.ProcessSystemRequest caught by failsafe handler. This is extremely bad!!!", LogSeverity.error);
                }
                catch
                {

                }
                return null;
            }
        }
        private bool DumpAllRequests = false;
        private ServerConnectionWorkItem ProcessRequest(ServerConnectionWorkItem _request)
        {
            WorkItemBaseCore response;
            if (DumpAllRequests)
            {

            }
            else
            {
                if (_request.WorkItem.IsSystemRequest)
                {
                    //Todo: remove
                    if (Core.Common.EnableDebug)
                    {
                        _request.WorkItem.MessageText = _request.WorkItem.GetType().FullName;
                    }

                    response = ProcessSystemRequest(_request.WorkItem, _request.Connection);

                    if (response != null)
                    {
                        _request.Connection.SendWorkItemQueue.Enqueue(response);
                    }
                }
                else
                {
                    response = DoProcessRequest(_request.WorkItem);
                    if (response != null)
                    {
                        _request.Connection.SendWorkItemQueue.Enqueue(response);
                    }
                }
            }
            return _request;
        }

        protected virtual WorkItemBaseCore DoProcessRequest(WorkItemBaseCore request)
        {
            DispatcherInstance.Dispatch(request);
            return null;
        }

        private readonly ThreadSafeQueue<ServerConnectionWorkItem> SyncProcessingQueue = new ThreadSafeQueue<ServerConnectionWorkItem>();
        private void ProcessRequestSync(ConnectionTCPSocket<MQPriority> Connection, WorkItemBaseCore WorkItem)
        {
            WorkItemBaseCore response;
            if (DumpAllRequests)
            {

            }
            else
            {
                if (WorkItem.IsSystemRequest)
                {
                    //Todo: remove
                    if (Core.Common.EnableDebug)
                    {
                        WorkItem.MessageText = WorkItem.GetType().FullName;
                    }

                    response = ProcessSystemRequest(WorkItem, Connection);

                    if (response != null)
                    {
                        Connection.SendWorkItemQueue.Enqueue(response);
                    }
                }
                else
                {
                    response = DoProcessRequest(WorkItem);
                    if (response != null)
                    {
                        Connection.SendWorkItemQueue.Enqueue(response);
                    }
                }
            }
        }

       
        public int TransmitRequest(WorkItemBaseCore workItem)
        {
            int uniqueID;
            uniqueID = workItem.UniqueID;
            DispatcherInstance.ScheduleRequest(workItem);
            return uniqueID;
        }

        public bool GetProcessedResponse<TRequest, TResponse>(int uniqueID, out WorkItemBase<TRequest, TResponse> workItem)
            where TRequest : new()
            where TResponse : new()
        {
            bool success;
            WorkItemBaseCore workItemBase;
            success = DispatcherInstance.GetProcessedResponse(uniqueID, out workItemBase);
            if (success)
            {
                workItem = new WorkItemBase<TRequest, TResponse>(workItemBase);
            }
            else
            {
                workItem = null;
            }
            return success;
        }


        public void Handler_LogError(Exception ex)
        {
            ex.Log(@"Unhandled exception processing Unit of Work. This is very BAD!!!", LogSeverity.unhandled);
        }
        public void Handler_LogMessage(string message, bool isError, bool isWarning)
        {
            if (isError)
            {
                message.Log(LogSeverity.error);
            }
            else if (isWarning)
            {
                message.Log(LogSeverity.warning);
            }
            else
            {
                message.Log(LogSeverity.info);
            }
        }


        private WorkItemBaseCore ProcessSystemRequest_PingPong(WorkItemBaseCore request, ConnectionTCPSocket<MQPriority> client)
        {
            WorkItemBase<UOWPingPong, UOWPingPong> requestWorkItem;
            WorkItemBase<UOWPingPong, UOWPingPong> responseWorkItem;
            try
            {
                requestWorkItem = new WorkItemBase<UOWPingPong, UOWPingPong>(request);
                if (requestWorkItem.HasResponse && requestWorkItem.RequestDetail.WorkItemData != null)
                {
                    client.RegisterPong(requestWorkItem.RequestDetail.WorkItemData, requestWorkItem.ResponseDetail.WorkItemData);
                    return null;
                }
                else
                {
                    return requestWorkItem.CreateReply(UOWPingPong.Pong());
                }
            }
            catch (Exception ex)
            {
                return request.CreateUnprocessedErrorReply(ResponseStatusCode.ProcessingError, ex.ToString());
            }
        }
        private WorkItemBaseCore ProcessSystemRequest_Discovery(WorkItemBaseCore request, ConnectionTCPSocket<MQPriority> client)
        {
            WorkItemBase<UOWDiscovery, UOWDiscovery> requestWorkItem;
            WorkItemBase<UOWDiscovery, UOWDiscovery> responseWorkItem;
            try
            {
                requestWorkItem = new WorkItemBase<UOWDiscovery, UOWDiscovery>(request);
                if (requestWorkItem.IsReply && requestWorkItem.HasResponse)
                {
                    DispatcherInstance.AddConnection(client, requestWorkItem.ResponseDetail.WorkItemData);
                    return null;
                }
                else
                {
                    if (requestWorkItem.HasRequest)
                    {
                        DispatcherInstance.AddConnection(client, requestWorkItem.RequestDetail.WorkItemData);
                    }
                    return requestWorkItem.CreateReply(DispatcherInstance.DiscoveryData());
                }
            }
            catch (Exception ex)
            {
                return request.CreateUnprocessedErrorReply(ResponseStatusCode.ProcessingError, ex.ToString());
            }
        }

        private WorkItemBaseCore ProcessSystemRequest_BenchMark(WorkItemBaseCore request, ConnectionTCPSocket<MQPriority> client)
        {
            WorkItemBase<UOWBenchMark, UOWBenchMark> requestWorkItem;
            WorkItemBase<UOWBenchMark, UOWBenchMark> responseWorkItem;

            try
            {
                requestWorkItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>(request);

                //Todo: Remove This
                //if (Core.Common.EnableDebug)
                //{
                //    requestWorkItem.RequestDetail.WorkItemData.ArrayProperty_Char.DebugText.Add(string.Concat(@"Recieved For ID:", requestWorkItem.UniqueID.ToString()));
                //    if (requestWorkItem is WorkItemBase<UOWBenchMark, UOWBenchMark>)
                //    {
                //        WorkItemBase<UOWBenchMark, UOWBenchMark> Test;
                //        //Test = _request as WorkItemBase<UOWBenchMark, UOWBenchMark>;
                //        Test = requestWorkItem;
                //        if (Test == null)
                //        {
                //        }
                //        else
                //        {
                //            Test.MessageText = Test.GetType().FullName;
                //            if (Test.RequestDetail == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_Char == null || Test.RequestDetail.WorkItemData.ArrayProperty_Char._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_DateTime == null || Test.RequestDetail.WorkItemData.ArrayProperty_DateTime._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_Decimal == null || Test.RequestDetail.WorkItemData.ArrayProperty_Decimal._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_Double == null || Test.RequestDetail.WorkItemData.ArrayProperty_Double._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_Int == null || Test.RequestDetail.WorkItemData.ArrayProperty_Int._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_Long == null || Test.RequestDetail.WorkItemData.ArrayProperty_Long._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_Short == null || Test.RequestDetail.WorkItemData.ArrayProperty_Short._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_Single == null || Test.RequestDetail.WorkItemData.ArrayProperty_Single._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_TimeSpan == null || Test.RequestDetail.WorkItemData.ArrayProperty_TimeSpan._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_UInt == null || Test.RequestDetail.WorkItemData.ArrayProperty_UInt._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_ULong == null || Test.RequestDetail.WorkItemData.ArrayProperty_ULong._rawBufferInternal == null)
                //            {

                //            }
                //            else if (Test.RequestDetail.WorkItemData.ArrayProperty_UShort == null || Test.RequestDetail.WorkItemData.ArrayProperty_UShort._rawBufferInternal == null)
                //            {

                //            }
                //        }
                //    }
                //}
                if (requestWorkItem.HasResponse)
                {
                    //Todo: Remove debug lines below
                    //if (Core.Common.EnableDebug)
                    //{
                    //    requestWorkItem.RequestDetail.WorkItemData.ArrayProperty_Char.DebugText.Add(string.Concat(@"Recv Request For ID:", requestWorkItem.UniqueID.ToString()));
                    //    requestWorkItem.ResponseDetail.WorkItemData.ArrayProperty_Char.DebugText.Add(string.Concat(@"Recv Response For ID:", requestWorkItem.UniqueID.ToString()));
                    //}

                    BenchMarksInTransit_Remove(request.UniqueID);
                    client.BenchMarkRecieved();
                    return null;
                }
                else
                {
                    //Todo: Remove debug lines below
                    //if (Core.Common.EnableDebug)
                    //{
                    //    responseWorkItem = requestWorkItem;
                    //    responseWorkItem = requestWorkItem.CreateReply(requestWorkItem.RequestDetail.WorkItemData.Clone());
                    //    responseWorkItem.RequestDetail.WorkItemData.ArrayProperty_Char.DebugText.Add(string.Concat(@"Reply Request For ID:", requestWorkItem.UniqueID.ToString()));
                    //    responseWorkItem.ResponseDetail.WorkItemData.ArrayProperty_Char.DebugText.Add(string.Concat(@"Reply Response For ID:", requestWorkItem.UniqueID.ToString()));

                    //    return responseWorkItem;
                    //}
                    return requestWorkItem.CreateReply(new UOWBenchMark().RandomizeData(rnd, BenchmarkArraySize));

                }
            }
            catch (Exception ex)
            {
                return request.CreateUnprocessedErrorReply(ResponseStatusCode.ProcessingError, ex.ToString());
            }
        }
    }

}
