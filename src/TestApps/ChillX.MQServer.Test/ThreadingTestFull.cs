using ChillX.Threading.BulkProcessor;
using ChillX.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChillX.Core.Structures;
using ChillX.MQServer.UnitOfWork;
using ChillX.MQServer.Server.SystemMessage;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Buffers.Binary;
using System.Buffers;
using System.Net.Sockets;
using System.Net;

namespace ChillX.MQServer.Test
{
    internal class ThreadingTestFullyUnrolled
    {
        public void Run(int numProducers, int numConsumers, int _maxWorkItemLimitPerClient = int.MaxValue, int _maxWorkerThreads = 8, int _threadStartupPerWorkItems = 4, int _threadStartupMinQueueSize = 4, int _idleWorkerThreadExitSeconds = 15, int _processedQueueMaxSize = 0)
        {
            ThreadedUOWProcessor = new AsyncThreadedWorkItemProcessor<WorkItemBase<UOWBenchMark, UOWBenchMark>, WorkItemBase<UOWBenchMark, UOWBenchMark>, int, MQPriority>(
                            _maxWorkItemLimitPerClient: _maxWorkItemLimitPerClient // Maximum number of concurrent requests in the processing queue per client
                            , _maxWorkerThreads: _maxWorkerThreads // Maximum number of threads to scale upto
                            , _threadStartupPerWorkItems: _threadStartupPerWorkItems // Consider starting a new processing thread ever X requests
                            , _threadStartupMinQueueSize: _threadStartupMinQueueSize // Do NOT start a new processing thread if work item queue is below this size
                            , _idleWorkerThreadExitSeconds: _idleWorkerThreadExitSeconds // Idle threads will exit after X seconds
                            , _processedQueueMaxSize: _processedQueueMaxSize // Max size of processed queue. This is really a buffer for sudden bursts
                            , _processedItemAutoDispose: true
                            , _processRequestMethod: ProcessRequest // Your Do Work method for processing the request
                            , _logErrorMethod: Handler_LogError
                            , _logMessageMethod: Handler_LogMessage
                            );
            QueueProducerSendWorkItems = new ThreadSafeQueue<WorkItemBase<UOWBenchMark, UOWBenchMark>>();
            QueueProducerSendByteData = new ThreadSafeQueue<RentedBuffer<byte>>();
            QueueConsumerRecieveByteData = new ThreadSafeQueue<RentedBuffer<byte>>();
            QueueConsumerRecieveWorkItemsBase = new ThreadSafeQueue<WorkItemBaseCore>();
            QueueProcessed = new ThreadSafeQueue<WorkItemBase<UOWBenchMark, UOWBenchMark>>();

            rnd = new Random();

            IsRunning = true;
            UnitTestNetworkStream.Create(out TCPProducer, out TCPProducerStream, out TCPConsumer, out TCPConsumerStream);

            numProducers = Math.Max(numProducers, 1);
            numConsumers = Math.Max(numConsumers, 1);
            Thread runThrad;
            for (int i = 0; i < numProducers; i++)
            {
                runThrad = new Thread(new ThreadStart(Produce_SendWorkItem));
                runThrad.Start();
            }
            for (int i = 0; i < numProducers; i++)
            {
                runThrad = new Thread(new ThreadStart(Produce_SendByteData));
                runThrad.Start();
            }
            runThrad = new Thread(new ThreadStart(Produce_SendToPipe));
            runThrad.Start();
            runThrad = new Thread(new ThreadStart(Consume_RecvFromPipe));
            runThrad.Start();
            for (int i = 0; i < numProducers; i++)
            {
                 runThrad = new Thread(new ThreadStart(Consume_RecvByteData));
                runThrad.Start();
            }
            for (int i = 0; i < numProducers; i++)
            {
                runThrad = new Thread(new ThreadStart(Consume_RecvWorkItem));
                runThrad.Start();
            }
            runThrad = new Thread(new ThreadStart(WorkItemSink));
            runThrad.Start();
        }

        private static class UnitTestNetworkStream
        {
            /// <summary>
            /// Create a TCP connection and return both ends.
            /// </summary>
            /// <param name="clientStream">Return the client end of the connection.</param>
            /// <param name="serverStream">Return the servr end of the connection.</param>
            public static void Create(out TcpClient server, out NetworkStream serverStream, out TcpClient client, out NetworkStream clientStream)
            {
                /* Create a signal to wait for the connection to be completed. */
                using var connected = new ManualResetEvent(false);

                /* Open a TCP listener and start listening, allowing the OS to pick the port. */
                TcpListener listen = new TcpListener(IPAddress.Loopback, 0);
                listen.Start();
                int port = ((IPEndPoint)listen.LocalEndpoint).Port;

                /* Start listening. Will store the server handle and raise the flag when ready. */
                listen.BeginAcceptTcpClient(OnConnect, null);
                TcpClient tcpServer = null;
                void OnConnect(IAsyncResult iar)
                {
                    tcpServer = listen.EndAcceptTcpClient(iar);
                    connected.Set();
                }

                /* Open the client end of the connection. */
                var tcpClient = new TcpClient(IPAddress.Loopback.ToString(), port);

                /* Wait for the connection to complete. */
                connected.WaitOne();

                /* Stop listening. */
                listen.Stop();

                /* Return the two ends back to the caller. */
                server = tcpServer;
                client = tcpClient;
                clientStream = tcpClient.GetStream();
                serverStream = tcpServer.GetStream();
            }
        }

        private AsyncThreadedWorkItemProcessor<WorkItemBase<UOWBenchMark, UOWBenchMark>, WorkItemBase<UOWBenchMark, UOWBenchMark>, int, MQPriority> ThreadedUOWProcessor;

        private ThreadSafeQueue<WorkItemBase<UOWBenchMark, UOWBenchMark>> QueueProducerSendWorkItems;
        private ThreadSafeQueue<RentedBuffer<byte>> QueueProducerSendByteData;
        private ThreadSafeQueue<RentedBuffer<byte>> QueueConsumerRecieveByteData;
        private ThreadSafeQueue<WorkItemBaseCore> QueueConsumerRecieveWorkItemsBase;
        private ThreadSafeQueue<WorkItemBase<UOWBenchMark, UOWBenchMark>> QueueProcessed;
        private TcpClient TCPProducer;
        private NetworkStream TCPProducerStream;
        private TcpClient TCPConsumer;
        private NetworkStream TCPConsumerStream;
        private ReaderWriterLockSlim ProducerStreamLock = new ReaderWriterLockSlim();
        private ReaderWriterLockSlim SubscriberStreamLock = new ReaderWriterLockSlim();

        private Random rnd;

        private const int MessageHeaderLength = 4;


        private int m_BenchmarkArraySize = 64;

        private void Produce_SendWorkItem()
        {
            bool SleepOne;
            int Counter;
            WorkItemBase<UOWBenchMark, UOWBenchMark> BenchMarkWorkItem;
            UOWBenchMark Payload = new UOWBenchMark().RandomizeData(rnd, m_BenchmarkArraySize);
            while (IsRunning)
            {
                SleepOne = false;
                Counter = 0;
                if (QueueProducerSendWorkItems.Count < 25000 && QueueConsumerRecieveWorkItemsBase.Count < 25000)
                {
                    for (int I = 0; I < 1000; I++)
                    {
                        Counter += 1;
                        BenchMarkWorkItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>(1, 2, 3, 1, 2, 3, MQPriority.System);
                        BenchMarkWorkItem.RequestDetail.WorkItemData = Payload.Clone();

                        QueueProducerSendWorkItems.Enqueue(BenchMarkWorkItem);
                    }
                }
                else
                {
                    SleepOne = true;
                }
                if (Counter > 0)
                {
                    MessageProduced(Counter);
                }
                if (SleepOne)
                {
                    Thread.Sleep(0);
                }
            }
        }

        private void Produce_SendByteData()
        {
            bool success;
            bool sleepOne;
            int numPending = 0;
            RentedBuffer<byte> buffer;
            WorkItemBase<UOWBenchMark, UOWBenchMark> BenchMarkWorkItem;
            Queue<WorkItemBase<UOWBenchMark, UOWBenchMark>> PendingQueue = new Queue<WorkItemBase<UOWBenchMark, UOWBenchMark>>();
            while (IsRunning)
            {
                sleepOne = false;
                numPending = QueueProducerSendWorkItems.Count;
                if (numPending > 0)
                {
                    if (QueueProducerSendByteData.Count < 5000)
                    {

                        numPending = QueueProducerSendWorkItems.DeQueue(numPending, PendingQueue, out success);
                        if (success)
                        {
                            while (PendingQueue.Count > 0)
                            {
                                BenchMarkWorkItem = PendingQueue.Dequeue();
                                //buffer = Serialization.ChillXSerializer<WorkItemBaseCore>.ReadToRentedBuffer(BenchMarkWorkItem);
                                buffer = BenchMarkWorkItem.SerializeToRentedBuffer();
                                QueueProducerSendByteData.Enqueue(buffer);
                                BenchMarkWorkItem.Dispose();
                            }
                        }
                        else
                        {
                            sleepOne = true;
                            break;
                        }
                    }
                    else
                    {
                        sleepOne = true;
                    }
                }
                else
                {
                    sleepOne = true;
                }
                if (sleepOne)
                {
                    //Thread.Sleep(0);
                }
            }
        }

        private void Produce_SendToPipe()
        {
            bool success;
            bool sleepOne;
            RentedBuffer<byte> buffer;
            byte[] headerBuffer;
            headerBuffer = new byte[MessageHeaderLength];
            while (IsRunning)
            {
                sleepOne = false;
                for (int I = 0; I < 5000; I++)
                {
                    buffer = QueueProducerSendByteData.DeQueue(out success);
                    if (success)
                    {
                        //Wrap the async zombie virus so that it does not propagate any further through the code.
                        int OutBoundMessageBodyBytesSizeBytes = buffer.Length;
                        Array.Clear(headerBuffer);
                        BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(headerBuffer), OutBoundMessageBodyBytesSizeBytes);

                        ProducerStreamLock.EnterWriteLock();
                        try
                        {
                            TCPProducerStream.Write(headerBuffer);
                            TCPProducerStream.Write(buffer.BufferSpan);
                        }
                        finally
                        {
                            ProducerStreamLock.ExitWriteLock();
                        }
                        SyncLock.EnterWriteLock();
                        try
                        {
                            m_Stats_ProducerBytesSent += (OutBoundMessageBodyBytesSizeBytes + MessageHeaderLength);
                            m_Stats_ProducerDataPacketsSent += 1;
                        }
                        finally
                        {
                            SyncLock.ExitWriteLock();
                        }
                        

                        buffer.Return();
                    }
                    else
                    {
                        sleepOne = true;
                        break;
                    }
                }
                if (sleepOne)
                {
                    //Thread.Sleep(0);
                }
            }
        }

        private void Consume_RecvFromPipe()
        {
            bool success;
            bool sleepOne;
            RentedBuffer<byte> buffer;
            bool InBoundReadModeHeader = true;
            int InBoundMessageHeaderBytesRead = 0;
            int InBoundMessageBodyBytesSizeBytes = 0;
            int InBoundMessageBodyBytesBytesRead = 0;
            byte[] InBoundMessageHeaderBytes = new byte[MessageHeaderLength];
            byte[] InBoundMessageBodyBytes = new byte[1];

            while (IsRunning)
            {
                sleepOne = false;
                int numBytesAvailable = TCPConsumer.Available;
                if (numBytesAvailable > 0)
                {
                    if (InBoundReadModeHeader)
                    {
                        if (InBoundMessageHeaderBytesRead < MessageHeaderLength)
                        {
                            InBoundMessageHeaderBytesRead += TCPConsumerStream.Read(InBoundMessageHeaderBytes, InBoundMessageHeaderBytesRead, MessageHeaderLength - InBoundMessageHeaderBytesRead);
                        }
                        if (InBoundMessageHeaderBytesRead == MessageHeaderLength)
                        {
                            InBoundReadModeHeader = false;
                            InBoundMessageBodyBytesSizeBytes = BinaryPrimitives.ReadInt32BigEndian(InBoundMessageHeaderBytes);
                            InBoundMessageBodyBytesBytesRead = 0;
                            if (InBoundMessageBodyBytesSizeBytes > InBoundMessageBodyBytes.Length)
                            {
                                InBoundMessageBodyBytes = new byte[InBoundMessageBodyBytesSizeBytes];
                            }
                            InBoundMessageHeaderBytesRead = 0;
                            numBytesAvailable -= MessageHeaderLength;
                        }
                    }
                    else
                    {
                        int numBytesToRead;
                        //numBytesToRead = Math.Min(InBoundMessageBodyBytesSizeBytes - InBoundMessageBodyBytesBytesRead, numBytesAvailable);
                        numBytesToRead = InBoundMessageBodyBytesSizeBytes - InBoundMessageBodyBytesBytesRead;
                        InBoundMessageBodyBytesBytesRead += TCPConsumerStream.Read(InBoundMessageBodyBytes, InBoundMessageBodyBytesBytesRead, numBytesToRead);
                        if (InBoundMessageBodyBytesBytesRead == InBoundMessageBodyBytesSizeBytes)
                        {
                            SyncLock.EnterWriteLock();
                            try
                            {
                                m_Stats_ConsumerBytesRecieved += (InBoundMessageBodyBytesBytesRead + MessageHeaderLength);
                                m_Stats_ConsumerPacketsRecieved += 1;
                            }
                            finally
                            {
                                SyncLock.ExitWriteLock();
                            }
                            buffer = RentedBuffer<byte>.Shared.Rent(InBoundMessageBodyBytesSizeBytes);
                            Array.Clear(buffer._rawBufferInternal);
                            Array.Copy(InBoundMessageBodyBytes, 0, buffer._rawBufferInternal, 0, InBoundMessageBodyBytesSizeBytes);

                            InBoundReadModeHeader = true;
                            Array.Clear(InBoundMessageHeaderBytes);
                            InBoundMessageHeaderBytesRead = 0;
                            InBoundMessageBodyBytesBytesRead = 0;
                            InBoundMessageBodyBytesSizeBytes = 0;
                            QueueConsumerRecieveByteData.Enqueue(buffer);
                        }
                    }
                }
                else
                {
                    //Thread.Sleep(0);
                }
            }
        }

        private void Consume_RecvByteData()
        {
            bool success;
            bool sleepOne;
            RentedBuffer<byte> buffer;
            WorkItemBaseCore workItemBase;
            while (IsRunning)
            {
                sleepOne = false;
                for (int I = 0; I < 1000; I++)
                {
                    buffer = QueueConsumerRecieveByteData.DeQueue(out success);
                    if (success)
                    {
                        workItemBase = new WorkItemBaseCore(buffer);
                        QueueConsumerRecieveWorkItemsBase.Enqueue(workItemBase);
                        //if (Serialization.ChillXSerializer<WorkItemBaseCore>.Write(workItemBase, buffer._rawBufferInternal))
                        //{
                        //    if (workItemBase.RequestData.Length < 200)
                        //    {

                        //    }
                        //    QueueConsumerRecieveWorkItemsBase.Enqueue(workItemBase);
                        //}
                        //else
                        //{
                        //    throw new Exception(@"Data stream is corrupt");
                        //}
                        buffer.Return();
                    }
                    else
                    {
                        sleepOne = true;
                        break;
                    }
                }
                if (sleepOne)
                {
                    //Thread.Sleep(0);
                }
            }
        }
        private void Consume_RecvWorkItem()
        {
            bool success;
            bool sleepOne;
            int counter;
            WorkItemBaseCore workItemBase;
            WorkItemBase<UOWBenchMark, UOWBenchMark> workItem;
            while (IsRunning)
            {
                sleepOne = false;
                counter = 0;
                for (int I = 0; I < 1000; I++)
                {
                    workItemBase = QueueConsumerRecieveWorkItemsBase.DeQueue(out success);
                    if (success)
                    {
                        try
                        {
                            workItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>(workItemBase);
                            ThreadedUOWProcessor.ScheduleWorkItem(MQPriority.System, workItem, 0);
                            counter += 1;
                        }
                        catch (Exception ex)
                        {
                            ex.Log(@"Consume_RecvWorkItem: ", LogSeverity.error);
                        }
                        workItemBase.Dispose();
                    }
                    else
                    {
                        sleepOne = true;
                        break;
                    }
                }
                if (counter > 0)
                {
                    MessageConsumed(counter);
                }
                if (sleepOne)
                {
                    Thread.Sleep(10);
                }
            }
        }

        private WorkItemBase<UOWBenchMark, UOWBenchMark> ProcessRequest(WorkItemBase<UOWBenchMark, UOWBenchMark> _request)
        {
            try
            {
                WorkItemBase<UOWBenchMark, UOWBenchMark> responseWorkItem;
                if (_request.RequestDetail.WorkItemData != null)
                {
                    responseWorkItem = _request.CreateReply(_request.RequestDetail.WorkItemData.Clone());

                    QueueProcessed.Enqueue(responseWorkItem);
                    MessageProcessed();
                }
                _request.Dispose();
            }
            catch (Exception ex)
            {
                ex.Log(@"Error Processing:", LogSeverity.error);
            }
            return _request;
        }

        public void WorkItemSink()
        {
            bool success;
            bool sleepOne;
            WorkItemBase<UOWBenchMark, UOWBenchMark> workItem;
            while (IsRunning)
            {
                sleepOne = false;
                for (int I = 0; I < 1000; I++)
                {
                    workItem = QueueProcessed.DeQueue(out success);
                    if (success)
                    {
                        workItem.Dispose();
                    }
                    else
                    {
                        sleepOne = true;
                        break;
                    }
                }
                if (sleepOne)
                {
                    Thread.Sleep(0);
                }
            }
        }

        private bool m_IsRunning = false;
        private ReaderWriterLockSlim SyncLock = new ReaderWriterLockSlim();
        public bool IsRunning
        {
            get
            {
                SyncLock.EnterReadLock();
                try
                {
                    return m_IsRunning;
                }
                finally
                {
                    SyncLock.ExitReadLock();
                }
            }
            private set
            {
                SyncLock.EnterWriteLock();
                try
                {
                    m_IsRunning = value;
                }
                finally
                {
                    SyncLock.ExitWriteLock();
                }
            }
        }

        public void ShutDown()
        {
            IsRunning = false;
            ThreadedUOWProcessor.ShutDown();
        }

        private int m_Stats_ProducerMessages = 0;
        private int m_Stats_ProducerDataPacketsSent = 0;
        private int m_Stats_ProducerBytesSent = 0;
        private int m_Stats_ConsumerBytesRecieved = 0;
        private int m_Stats_ConsumerPacketsRecieved = 0;
        private int m_Stats_ConsumerMessages = 0;
        private int m_Stats_ProcessingMessages = 0;

        public void CalcStatsWindow(out int Stats_MessagesProduced, out int Stats_MessagesProcessed, out int Stats_MessagesConsumed,
            out int QueueProducerSendWorkItemsSize, out int QueueProducerSendByteDataSize,
            out int QueueConsumerRecieveByteDataSize, out int QueueConsumerRecieveWorkItemsBaseSize,
            out int ThreadControllerQueueSize, out int QueueProcessedSize)
        {
            SyncLock.EnterReadLock();
            try
            {
                Stats_MessagesProduced = m_Stats_ProducerMessages;
                Stats_MessagesProcessed = m_Stats_ProcessingMessages;
                Stats_MessagesConsumed = m_Stats_ConsumerMessages;
                m_Stats_ProducerMessages = 0;
                m_Stats_ProcessingMessages = 0;
                m_Stats_ConsumerMessages = 0;
            }
            finally
            {
                SyncLock.ExitReadLock();
            }
            QueueProducerSendWorkItemsSize = QueueProducerSendWorkItems.Count;
            QueueProducerSendByteDataSize = QueueProducerSendByteData.Count;
            QueueConsumerRecieveByteDataSize = QueueConsumerRecieveByteData.Count;
            QueueConsumerRecieveWorkItemsBaseSize = QueueConsumerRecieveWorkItemsBase.Count;
            ThreadControllerQueueSize = ThreadedUOWProcessor.QueueSizeAllClients();
            QueueProcessedSize = QueueProcessed.Count;
        }
        private void MessageProduced(int numProduced)
        {
            SyncLock.EnterWriteLock();
            try
            {
                m_Stats_ProducerMessages += numProduced;
            }
            finally
            {
                SyncLock.ExitWriteLock();
            }
        }
        private void MessageProcessed()
        {
            SyncLock.EnterWriteLock();
            try
            {
                m_Stats_ProcessingMessages += 1;
            }
            finally
            {
                SyncLock.ExitWriteLock();
            }
        }
        private void MessageConsumed(int numConsumed)
        {
            SyncLock.EnterWriteLock();
            try
            {
                m_Stats_ConsumerMessages += numConsumed;
            }
            finally
            {
                SyncLock.ExitWriteLock();
            }
        }


        public void Handler_LogError(Exception ex)
        {
            ex.Log(@"Unhandled exception processing Unit of Work. This is very BAD!!!", LogSeverity.unhandled);
        }
        public void Handler_LogMessage(string _message, bool _isError, bool _isWarning)
        {
            if (_isError)
            {
                _message.Log(LogSeverity.error);
            }
            else if (_isWarning)
            {
                _message.Log(LogSeverity.warning);
            }
            else
            {
                _message.Log(LogSeverity.info);
            }
        }


    }
}
