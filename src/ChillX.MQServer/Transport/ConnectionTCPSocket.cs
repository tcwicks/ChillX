using ChillX.MQServer.UnitOfWork;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using ChillX.Logging;
using System.Net;
using ChillX.MQServer.Server.SystemMessage;
using System.Diagnostics;
using System.Threading;
using ChillX.Core.Network;
using ChillX.Core.Structures;
using ChillX.Serialization;

namespace ChillX.MQServer.Transport
{
    internal class ConnectionTCPSocket<TPriorityEnum> : IDisposable, IEqualityComparer<ConnectionTCPSocket<TPriorityEnum>>
        where TPriorityEnum : struct, IComparable, IFormattable, IConvertible
    {
        /// <summary>
        /// lock(this) is risky here therefore use custom object for monitor
        /// </summary>
        private readonly object SyncRoot = new object();
        internal ConnectionTCPSocket(TcpClient _connectionTCPClient)
        {
            connectionTCPClient = _connectionTCPClient;
            m_SendBufferSize = connectionTCPClient.SendBufferSize;
            connectionStream = connectionTCPClient.GetStream();
            connectionStream.ReadTimeout = Timeout.Infinite;
            connectionStream.WriteTimeout = Timeout.Infinite;
            m_ConnectionEndPoint = new ConnectivityEndPoint(CXMQUtility.ConnectionUniqueIDNext(), ((IPEndPoint)connectionTCPClient.Client.RemoteEndPoint).Address, ((IPEndPoint)connectionTCPClient.Client.RemoteEndPoint).Port);
            IsOutBound = false;
            SWPingTimer.Restart();
            StartThreads();
        }

        internal ConnectionTCPSocket(IPAddress _outboundDestinationIP, int _outboundDestinationPort)
        {
            OutboundDestinationIP = _outboundDestinationIP;
            OutboundDestinationPort = _outboundDestinationPort;
            m_ConnectionEndPoint = new ConnectivityEndPoint(CXMQUtility.ConnectionUniqueIDNext(), OutboundDestinationIP, OutboundDestinationPort);
            IsOutBound = true;
            SWPingTimer.Restart();
            StartThreads();
        }

        private NetworkStream connectionStream;
        private TcpClient connectionTCPClient;
        private ConnectivityEndPoint m_ConnectionEndPoint;
        public bool IsOutBound { get; private set; } = false;
        public IPAddress OutboundDestinationIP { get; private set; } = IPAddress.None;
        public int OutboundDestinationPort { get; private set; } = 0;
        public ConnectivityEndPoint ConnectionEndPoint { get { return m_ConnectionEndPoint; } }
        public int UniqueID
        {
            get { return m_ConnectionEndPoint.UniqueID; }
        }
        public bool IsConnected { get { lock (SyncRoot) { return connectionTCPClient == null ? false : connectionTCPClient.Connected; } } }
        private bool m_IsRunning = true;
        public bool IsRunning
        {
            get
            {
                lock (SyncRoot)
                {
                    return m_IsRunning;
                }
            }
            private set
            {
                lock (SyncRoot)
                {
                    m_IsRunning = value;
                }
            }
        }
        private bool m_ResetSignal = true;
        public bool ResetSignal
        {
            get
            {
                lock (SyncRoot)
                {
                    return m_ResetSignal;
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    m_ResetSignal = value;
                }
            }
        }
        public void ShutDown()
        {
            IsRunning = false;
        }
        public int AvailableBytes { get { return connectionTCPClient.Available; } }

        public ReaderWriterLockSlim SyncLockStats { get; } = new ReaderWriterLockSlim();
        private double m_BandwidthInMbps = 0;
        public double BandwidthInMbps
        {
            get
            {
                return Volatile.Read(ref m_BandwidthInMbps);
            }
        }
        private double m_BandwidthOutMbps = 0;
        public double BandwidthOutMbps
        {
            get
            {
                return Volatile.Read(ref m_BandwidthOutMbps);
            }
        }
        private volatile int m_MessageCountIn = 0;
        public int MessageCountIn
        {
            get { return m_MessageCountIn; }
        }
        private volatile int m_MessageCountOut = 0;
        public int MessageCountOut
        {
            get { return m_MessageCountOut; }
        }
        private ReaderWriterLockSlim InternalCounterLock = new ReaderWriterLockSlim();
        private int BytesReceived = 0;
        private int BytesSent = 0;
        private int MessagesReceived = 0;
        private int MessagesSent = 0;

        public void CalcBandwidth(double ElapsedMilliseconds, bool Lock = false)
        {
            double BandwidthIn;
            double BandwidthOut;
            int MessagesIn;
            int MessagesOut;
            double TimeElapsed = ElapsedMilliseconds / 1000d;

            InternalCounterLock.EnterWriteLock();
            try
            {
                BandwidthIn = BytesReceived;
                BandwidthOut = BytesSent;
                BytesReceived = 0;
                BytesSent = 0;
                MessagesIn = MessagesReceived;
                MessagesOut = MessagesSent;
                MessagesReceived = 0;
                MessagesSent = 0;
            }
            finally
            {
                InternalCounterLock.ExitWriteLock();
            }
            BandwidthIn = (BandwidthIn / TimeElapsed) / 1048576d;
            BandwidthOut = (BandwidthOut / TimeElapsed) / 1048576d;
            MessagesIn = Convert.ToInt32(((double)MessagesIn) / TimeElapsed);
            MessagesOut = Convert.ToInt32(((double)MessagesOut) / TimeElapsed);

            if (Lock)
            {
                SyncLockStats.EnterWriteLock();
                try
                {

                    m_BandwidthInMbps = BandwidthIn;
                    m_BandwidthOutMbps = BandwidthOut;
                    m_MessageCountIn = MessagesIn;
                    m_MessageCountOut = MessagesOut;
                }
                finally
                {
                    SyncLockStats.ExitWriteLock();
                }
            }
            else
            {
                m_BandwidthInMbps = BandwidthIn;
                m_BandwidthOutMbps = BandwidthOut;
                m_MessageCountIn = MessagesIn;
                m_MessageCountOut = MessagesOut;
            }
        }

        private int m_SendBufferSize = 8192;
        private int SendBufferSize { get { return m_SendBufferSize; } }
        private const int MessageHeaderLength = 4;

        private ManualResetEvent WaitHandleConnection { get; } = new ManualResetEvent(false);

        public ThreadSafeQueue<WorkItemBaseCore> ReadWorkItemQueue { get; } = new ThreadSafeQueue<WorkItemBaseCore>();
        public ThreadSafeQueue<RentedBuffer<byte>> ReadDataQueue { get; } = new ThreadSafeQueue<RentedBuffer<byte>>();


        private byte[] InBoundMessageHeaderBytes { get; } = new byte[MessageHeaderLength];
        private int InBoundMessageHeaderBytesRead = 0;

        private bool InBoundReadModeHeader = true;
        private RentedBuffer<byte> InBoundMessageBodyBytes;
        private int InBoundMessageBodyBytesSizeBytes = 0;
        private int InBoundMessageBodyBytesBytesRead = 0;

        private void StartThreads()
        {
            System.Threading.Thread RunThread;
            //RunThread = new Thread(new ThreadStart(ReadSocketData));
            //RunThread.Start();
            //RunThread = new Thread(new ThreadStart(ReadWorkItems));
            //RunThread.Start();
            //RunThread = new Thread(new ThreadStart(SendWorkItems));
            //RunThread.Start();
            //RunThread = new Thread(new ThreadStart(SendSocketData));
            //RunThread.Start();
        }


        int NumConsequetiveFailsRead = 0;
        public bool ReadSocketData()
        {
            if (IsConnected)
            {
                int numBytesAvailable = connectionTCPClient.Available;
                if (numBytesAvailable > 0)
                {
                    if (InBoundReadModeHeader)
                    {
                        if (InBoundMessageHeaderBytesRead < MessageHeaderLength)
                        {
                            try
                            {
                                InBoundMessageHeaderBytesRead += connectionStream.Read(InBoundMessageHeaderBytes, InBoundMessageHeaderBytesRead, MessageHeaderLength - InBoundMessageHeaderBytesRead);
                                NumConsequetiveFailsRead = 0;
                            }
                            catch (Exception ex)
                            {
                                NumConsequetiveFailsRead++;
                                if (NumConsequetiveFailsRead > 3)
                                {
                                    CloseSocket(true);
                                }
                                ex.Log(@"ConnectionTCPSocket: Timeout reading from socket.", LogSeverity.debug);
                            }
                        }
                        try
                        {
                            if (InBoundMessageHeaderBytesRead == MessageHeaderLength)
                            {
                                InBoundReadModeHeader = false;
                                InBoundMessageBodyBytesSizeBytes = BinaryPrimitives.ReadInt32BigEndian(InBoundMessageHeaderBytes);
                                InBoundMessageBodyBytesBytesRead = 0;
                                //InBoundMessageBodyBytes = new byte[InBoundMessageBodyBytesSizeBytes];
                                InBoundMessageBodyBytes = RentedBuffer<byte>.Shared.Rent(InBoundMessageBodyBytesSizeBytes);
                                InBoundMessageHeaderBytesRead = 0;
                                numBytesAvailable -= MessageHeaderLength;
                            }
                        }
                        catch (Exception ex)
                        {
                            CloseSocket(true);
                            ex.Log(@"ConnectionTCPSocket: Error deserializing message header.", LogSeverity.debug);
                        }
                    }
                    if (!InBoundReadModeHeader)
                    {
                        try
                        {
                            int numBytesToRead;
                            //numBytesToRead = Math.Min(InBoundMessageBodyBytesSizeBytes - InBoundMessageBodyBytesBytesRead, numBytesAvailable);
                            numBytesToRead = InBoundMessageBodyBytesSizeBytes - InBoundMessageBodyBytesBytesRead;
                            InBoundMessageBodyBytesBytesRead += connectionStream.Read(InBoundMessageBodyBytes._rawBufferInternal, InBoundMessageBodyBytesBytesRead, numBytesToRead);
                            if (InBoundMessageBodyBytesBytesRead == InBoundMessageBodyBytesSizeBytes)
                            {
                                try
                                {
                                    //int TotalBytes;
                                    //int OldValue;
                                    //OldValue = BytesReceived;
                                    //TotalBytes = OldValue + InBoundMessageBodyBytesBytesRead + MessageHeaderLength;
                                    //while (Interlocked.CompareExchange(ref BytesReceived, TotalBytes, OldValue) != OldValue)
                                    //{
                                    //    OldValue = BytesReceived;
                                    //    TotalBytes = OldValue + InBoundMessageBodyBytesBytesRead + MessageHeaderLength;
                                    //}
                                    //Interlocked.Increment(ref MessagesReceived);

                                    InternalCounterLock.EnterWriteLock();
                                    try
                                    {
                                        BytesReceived += (InBoundMessageBodyBytesBytesRead + MessageHeaderLength);
                                        MessagesReceived += 1;
                                    }
                                    finally
                                    {
                                        InternalCounterLock.ExitWriteLock();
                                    }

                                    InBoundReadModeHeader = true;
                                    InBoundMessageBodyBytesBytesRead = 0;
                                    InBoundMessageBodyBytesSizeBytes = 0;
                                    ReadDataQueue.Enqueue(InBoundMessageBodyBytes);
                                    //workItem = MessagePackSerializer.Deserialize<WorkItemBaseCore>(InBoundMessageBodyBytes, CXMQUtility.SerializationOptions);
                                    //workItem.SourceConnectivityEndPoint = connectionEndPoint;
                                    //ReadWorkItemQueue.Enqueue(workItem);
                                    return true;
                                }
                                catch (Exception ex2)
                                {
                                    CloseSocket(true);
                                    ex2.Log(@"ConnectionTCPSocket: Error deserializing message body.", LogSeverity.debug);
                                }
                            }
                            NumConsequetiveFailsRead = 0;
                        }
                        catch (Exception ex)
                        {
                            NumConsequetiveFailsRead++;
                            if (NumConsequetiveFailsRead > 3)
                            {
                                CloseSocket(true);
                            }
                            ex.Log(@"ConnectionTCPSocket: Timeout reading from socket.", LogSeverity.debug);
                        }
                    }
                }
            }
            return false;
        }


        public bool ReadWorkItems(Queue<RentedBuffer<byte>> pendingQueue)
        {
            WorkItemBaseCore workItem;
            RentedBuffer<byte> buffer;
            int NumConsequetiveFails;
            bool success;
            int numPending;
            NumConsequetiveFails = 0;
            if (IsConnected)
            {
                numPending = ReadDataQueue.Count;
                if (numPending > 0)
                {
                    numPending = ReadDataQueue.DeQueue(numPending, pendingQueue, out success);
                    if (success)
                    {
                        while (pendingQueue.Count > 0)
                        {
                            buffer = pendingQueue.Dequeue();
                            try
                            {
                                workItem = new WorkItemBaseCore();
                                if (ChillXSerializer<WorkItemBaseCore>.Write(workItem, buffer._rawBufferInternal))
                                {
                                    workItem.SourceConnectivityEndPoint = m_ConnectionEndPoint;
                                    ReadWorkItemQueue.Enqueue(workItem);
                                }
                                buffer.Return();
                            }
                            catch (Exception ex)
                            {
                                NumConsequetiveFails++;
                                if (NumConsequetiveFails > 3)
                                {
                                    CloseSocket(true);
                                }
                                else
                                {
                                    ReadDataQueue.Enqueue(buffer);
                                }
                                ex.Log(@"TCP Connection Error", LogSeverity.debug);
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private readonly IDictionary<int, string> channelStates = new System.Collections.Concurrent.ConcurrentDictionary<int, string>();

        public ThreadSafeQueue<WorkItemBaseCore> SendWorkItemQueue { get; } = new ThreadSafeQueue<WorkItemBaseCore>();

        public ThreadSafeQueue<RentedBuffer<byte>> SendDataQueue { get; } = new ThreadSafeQueue<RentedBuffer<byte>>();

        public bool SendWorkItems(Queue<WorkItemBaseCore> pendingQueue)
        {
            WorkItemBaseCore workItem;
            RentedBuffer<byte> buffer;
            int numPending;
            bool success;
            if (IsConnected)
            {
                numPending = SendWorkItemQueue.Count;
                if (numPending > 0)
                {
                    numPending = SendWorkItemQueue.DeQueue(numPending, pendingQueue, out success);
                    if (success)
                    {
                        while (pendingQueue.Count > 0)
                        {
                            workItem = pendingQueue.Dequeue();
                            buffer = ChillXSerializer<WorkItemBaseCore>.ReadToRentedBuffer(workItem);
                            workItem.Dispose();
                            SendDataQueue.Enqueue(buffer);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        //Todo: Remove this
        private readonly System.Random rndCorruptData = new Random();
        int NumConsequetiveFailsSend = 0;
        public bool SendSocketData(Queue<RentedBuffer<byte>> pendingQueue)
        {
            RentedBuffer<byte> buffer;
            byte[] OutBoundMessageBodyBytes;
            int numPending;
            bool success;
            if (IsConnected)
            {
                numPending = SendDataQueue.Count;
                if (numPending > 0)
                {
                    numPending = SendDataQueue.DeQueue(numPending, pendingQueue, out success);
                    if (success)
                    {
                        while (pendingQueue.Count > 0)
                        {
                            buffer = pendingQueue.Dequeue();
                            OutBoundMessageBodyBytes = buffer._rawBufferInternal;
                            try
                            {
                                //if (rndCorruptData.NextDouble() < 0.001d)
                                //{
                                //    int idx = rndCorruptData.Next(0, OutBoundMessageBodyBytes.Length - 1);
                                //    OutBoundMessageBodyBytes[idx] = (byte)((OutBoundMessageBodyBytes[idx] / 2) + 1);
                                //}
                                //int idx = rndCorruptData.Next(0, OutBoundMessageBodyBytes.Length - 1);
                                //OutBoundMessageBodyBytes[idx] = (byte)((OutBoundMessageBodyBytes[idx] / 2) + 1);

                                byte[] OutBoundMessageHeaderBytes = new byte[MessageHeaderLength];
                                int OutBoundMessageBodyBytesSizeBytes = OutBoundMessageBodyBytes.Length;
                                BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(OutBoundMessageHeaderBytes), OutBoundMessageBodyBytesSizeBytes);
                                connectionStream.Write(OutBoundMessageHeaderBytes, 0, MessageHeaderLength);
                                connectionStream.Write(OutBoundMessageBodyBytes, 0, OutBoundMessageBodyBytesSizeBytes);

                                //int TotalBytes;
                                //int OldValue;
                                //OldValue = BytesSent;
                                //TotalBytes = OldValue + MessageHeaderLength + OutBoundMessageBodyBytesSizeBytes;
                                //while (Interlocked.CompareExchange(ref BytesSent, TotalBytes, OldValue) != OldValue)
                                //{
                                //    OldValue = BytesSent;
                                //    TotalBytes = OldValue + MessageHeaderLength + OutBoundMessageBodyBytesSizeBytes;
                                //}
                                //Interlocked.Increment(ref MessagesSent);
                                InternalCounterLock.EnterWriteLock();
                                try
                                {
                                    BytesSent += (MessageHeaderLength + OutBoundMessageBodyBytesSizeBytes);
                                    MessagesSent += 1;
                                }
                                finally
                                {
                                    InternalCounterLock.ExitWriteLock();
                                }

                                NumConsequetiveFailsSend = 0;

                                buffer.Return();
                            }
                            catch (Exception ex)
                            {
                                NumConsequetiveFailsSend++;
                                SendDataQueue.Enqueue(buffer);
                                if (NumConsequetiveFailsSend > 3)
                                {
                                    CloseSocket(true);
                                }
                                ex.Log(@"TCP Connection Error", LogSeverity.debug);
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private Stopwatch SWOutboundLastConnectTimer { get; } = new Stopwatch();
        public bool Connect()
        {
            if (!SWOutboundLastConnectTimer.IsRunning) { SWOutboundLastConnectTimer.Start(); }
            else if (SWOutboundLastConnectTimer.Elapsed.TotalSeconds < 3d) { return false; }
            if ((connectionTCPClient != null) && IsConnected) { WaitHandleConnection.Set(); return true; }
            CloseSocket(true);
            try
            {
                connectionTCPClient = new TcpClient();
                connectionTCPClient.Connect(OutboundDestinationIP, OutboundDestinationPort);
                m_SendBufferSize = connectionTCPClient.SendBufferSize;
                connectionStream = connectionTCPClient.GetStream();
                connectionStream.ReadTimeout = Timeout.Infinite;
                connectionStream.WriteTimeout = Timeout.Infinite;
                SWOutboundLastConnectTimer.Stop();
                SWOutboundLastConnectTimer.Reset();
                WaitHandleConnection.Set();
                InBoundReadModeHeader = true;
                InBoundMessageHeaderBytesRead = 0;
                InBoundMessageBodyBytesSizeBytes = 0;
                InBoundMessageBodyBytesBytesRead = 0;

            }
            catch
            {

            }
            return false;
        }

        private Stopwatch SWPingTimer { get; } = new Stopwatch();
        public TimeSpan LastPingTime
        {
            get { return SWPingTimer.Elapsed; }
        }

        public void Ping()
        {
            if (SWPingTimer.ElapsedMilliseconds > 1000)
            {
                WorkItemBase<UOWPingPong, UOWPingPong> pingRequest;
                pingRequest = new WorkItemBase<UOWPingPong, UOWPingPong>((int)SystemServiceType.System, (int)SystemServiceModule.Transport, (int)SystemServiceFunction.PingPong,
                    (int)SystemServiceType.System, (int)SystemServiceModule.Transport, (int)SystemServiceFunction.PingPong, MQPriority.System);
                pingRequest.RequestDetail.WorkItemData = UOWPingPong.Ping();
                pingRequest.RequestReply();
                SendWorkItemQueue.Enqueue(pingRequest);
                SWPingTimer.Restart();
            }
        }

        public TimeSpan Latency { get; private set; } = TimeSpan.Zero;
        private Queue<int> PingPongTimeHistory { get; } = new Queue<int>();
        internal void RegisterPong(UOWPingPong _request, UOWPingPong _response)
        {
            PingPongTimeHistory.Enqueue((int)(DateTime.UtcNow.Ticks - _request.TimeStampTicks));
            while (PingPongTimeHistory.Count > 10)
            {
                PingPongTimeHistory.Dequeue();
            }
            int LatencyTicks = 0;
            foreach (int i in PingPongTimeHistory)
            {
                LatencyTicks += i;
            }
            Latency = TimeSpan.FromTicks(LatencyTicks / PingPongTimeHistory.Count);
        }

        private volatile int m_BenchMarksInTransit = 0;
        public int BenchMarksInTransit { get { return Interlocked.Exchange(ref m_BenchMarksInTransit, m_BenchMarksInTransit); } }
        public void BenchMarkSent() { Interlocked.Increment(ref m_BenchMarksInTransit); }
        public void BenchMarkRecieved() { Interlocked.Decrement(ref m_BenchMarksInTransit); }


        public override int GetHashCode()
        {
            return UniqueID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ConnectionTCPSocket<TPriorityEnum> target = obj as ConnectionTCPSocket<TPriorityEnum>;
            if (target == null)
            {
                return base.Equals(obj);
            }
            return target.UniqueID.Equals(UniqueID);
        }

        #region IEqualityComparer<ConnectionTCPSocket<TPriorityEnum>>

        public bool Equals(ConnectionTCPSocket<TPriorityEnum> x, ConnectionTCPSocket<TPriorityEnum> y)
        {
            if (x == null && y == null) { return true; }
            if (x == null || y == null) { return false; }
            return x.UniqueID.Equals(UniqueID) && y.UniqueID.Equals(y.UniqueID);
        }

        public int GetHashCode(ConnectionTCPSocket<TPriorityEnum> obj)
        {
            return (obj == null ? 0 : obj.UniqueID.GetHashCode());
        }

        #endregion

        internal void CloseSocket(bool force = false)
        {
            try
            {
                if ((connectionTCPClient != null) && (connectionTCPClient.Connected || force))
                {
                    lock (SyncRoot)
                    {
                        if ((connectionTCPClient != null) && (connectionTCPClient.Connected || force))
                        {
                            WaitHandleConnection.Reset();
                            try
                            {
                                connectionStream.Close();
                            }
                            catch
                            {

                            }
                            try
                            {
                                connectionStream.Dispose();
                            }
                            catch
                            {

                            }
                            try
                            {
                                connectionTCPClient.Close();
                            }
                            catch
                            {

                            }
                            try
                            {
                                connectionTCPClient.Dispose();
                            }
                            catch
                            {

                            }
                            connectionStream = null;
                            connectionTCPClient = null;

                            InBoundReadModeHeader = true;
                            InBoundMessageHeaderBytesRead = 0;
                            InBoundMessageBodyBytesSizeBytes = 0;
                            InBoundMessageBodyBytesBytesRead = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log(@"Exception closing socket:", LogSeverity.error);
                connectionStream = null;
                connectionTCPClient = null;

                InBoundReadModeHeader = true;
                InBoundMessageHeaderBytesRead = 0;
                InBoundMessageBodyBytesSizeBytes = 0;
                InBoundMessageBodyBytesBytesRead = 0;
            }
        }

        private bool m_IsDisposed = false;
        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                DoDispose(true);
            }
        }

        private void DoDispose(bool _isDisposing = false)
        {
            if (_isDisposing)
            {
                IsRunning = false;
                CXMQUtility.ConnectionUniqueIDReturn(m_ConnectionEndPoint.UniqueID);
                CloseSocket(true);
                GC.SuppressFinalize(this);
            }
        }
    }

}
