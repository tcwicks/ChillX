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

namespace ChillX.MQServer.Test
{
    internal class ThreadControllerTest_Produce_Process_Consume
    {
        public void Run(int numProducers, int numConsumers, int _maxWorkItemLimitPerClient = int.MaxValue, int _maxWorkerThreads = 4, int _threadStartupPerWorkItems = 4, int _threadStartupMinQueueSize = 4, int _idleWorkerThreadExitSeconds = 15, int _processedQueueMaxSize = 0)
        {
            ThreadedUOWProcessor = new AsyncThreadedWorkItemProcessor<RentedBuffer<byte>, WorkItemBase<UOWBenchMark, UOWBenchMark>, int, MQPriority>(
                            _maxWorkItemLimitPerClient: _maxWorkItemLimitPerClient // Maximum number of concurrent requests in the processing queue per client
                            , _maxWorkerThreads: _maxWorkerThreads // Maximum number of threads to scale upto
                            , _threadStartupPerWorkItems: _threadStartupPerWorkItems // Consider starting a new processing thread ever X requests
                            , _threadStartupMinQueueSize: _threadStartupMinQueueSize // Do NOT start a new processing thread if work item queue is below this size
                            , _idleWorkerThreadExitSeconds: _idleWorkerThreadExitSeconds // Idle threads will exit after X seconds
                            , _processedQueueMaxSize: _processedQueueMaxSize // Max size of processed queue. This is really a buffer for sudden bursts
                            , _processedItemAutoDispose: false
                            , _processRequestMethod: ProcessRequest // Your Do Work method for processing the request
                            , _logErrorMethod: Handler_LogError
                            , _logMessageMethod: Handler_LogMessage
                            );
            DoneQueue = new ThreadSafeQueue<WorkItemBase<UOWBenchMark, UOWBenchMark>>();

            rnd = new Random();

            IsRunning = true;

            numProducers = Math.Max(numProducers, 1);
            numConsumers = Math.Max(numConsumers, 1);
            for (int i = 0; i < numProducers; i++)
            {
                Thread runThrad = new Thread(new ThreadStart(Produce));
                runThrad.Name = @"Producer Thread";
                runThrad.Start();
            }
            for (int i = 0; i < numProducers; i++)
            {
                Thread runThrad = new Thread(new ThreadStart(Consume));
                runThrad.Name = @"Consumer Thread";
                runThrad.Start();
            }
        }

        private AsyncThreadedWorkItemProcessor<RentedBuffer<byte>, WorkItemBase<UOWBenchMark, UOWBenchMark>, int, MQPriority> ThreadedUOWProcessor;

        private ThreadSafeQueue<WorkItemBase<UOWBenchMark, UOWBenchMark>> DoneQueue;

        private Random rnd;

        private int m_BenchmarkArraySize = 64;

        private struct NoDisposeContainer<T>
        {
            public NoDisposeContainer(T _item)
            {
                item = _item;
            }
            public T item;
        }

        private void Produce()
        {
            bool SleepOne;
            int Counter;
            RentedBuffer<byte> buffer;
            RentedBuffer<byte> bufferMaster;
            byte[] bufferData;
            WorkItemBase<UOWBenchMark, UOWBenchMark> BenchMarkWorkItem;
            WorkItemBaseCore BenchMarkWorkItemBase;
            UOWBenchMark Payload = new UOWBenchMark().RandomizeData(rnd, m_BenchmarkArraySize);
            BenchMarkWorkItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>(1, 2, 3, 1, 2, 3, MQPriority.System);
            BenchMarkWorkItem.RequestDetail.WorkItemData = Payload.Clone();
            BenchMarkWorkItemBase = BenchMarkWorkItem;
            bufferMaster = BenchMarkWorkItemBase.SerializeToRentedBuffer();
            BenchMarkWorkItemBase.Dispose();
            while (IsRunning)
            {
                SleepOne = false;
                Counter = 0;
                for (int I = 0; I < 5000; I++)
                {
                    //if (ThreadedUOWProcessor.QueueSizeAllClients() < int.MaxValue)
                    //{
                        Counter += 1;
                        BenchMarkWorkItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>(1, 2, 3, 1, 2, 3, MQPriority.System);
                        BenchMarkWorkItem.RequestDetail.WorkItemData = Payload.Clone();
                        //BenchMarkWorkItem.PackToBytes();
                        BenchMarkWorkItemBase = BenchMarkWorkItem;

                        //buffer = Serialization.ChillXSerializer<WorkItemBaseCore>.ReadToRentedBuffer(BenchMarkWorkItemBase);


                        buffer = BenchMarkWorkItemBase.SerializeToRentedBuffer();
                        ThreadedUOWProcessor.ScheduleWorkItem(MQPriority.System, buffer, 0);

                        BenchMarkWorkItemBase.Dispose();
                    //}
                    //else
                    //{
                    //    SleepOne = true;
                    //    break;
                    //}

                    //if (ThreadedUOWProcessor.QueueSizeAllClients() < 20000)
                    //{
                    //    for (int N = 0; N < 1000; N++)
                    //    {
                    //        Counter += 1;
                    //        buffer = RentedBuffer<byte>.Shared.Rent(bufferMaster.Length);
                    //        bufferMaster.BufferSpan.CopyTo(buffer.BufferSpan);
                    //        ThreadedUOWProcessor.ScheduleWorkItem(MQPriority.System, buffer, 0);
                    //    }
                    //}
                    //else
                    //{
                    //    SleepOne = true;
                    //}

                    //Counter += 1;
                    //buffer = RentedBuffer<byte>.Shared.Rent(bufferMaster.Length);
                    //bufferMaster.BufferSpan.CopyTo(buffer.BufferSpan);
                    //ThreadedUOWProcessor.ScheduleWorkItem(MQPriority.System, buffer, 0);

                    //buffer.Return();

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

        private WorkItemBase<UOWBenchMark, UOWBenchMark> ProcessRequest(RentedBuffer<byte> _request)
        {
            try
            {
                WorkItemBaseCore requestWorkItemBase;
                //WorkItemBase<UOWBenchMark, UOWBenchMark> requestWorkItem;
                //WorkItemBase<UOWBenchMark, UOWBenchMark> responseWorkItem;


                if(!_request.IsRented)
                {

                }
                requestWorkItemBase = new WorkItemBaseCore(_request);
                requestWorkItemBase.Dispose();
                _request.Return();
                MessageProcessed();


                ////Serialization.ChillXSerializer<WorkItemBaseCore>.Write(requestWorkItemBase, _request._rawBufferInternal);
                //requestWorkItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>(requestWorkItemBase);
                //responseWorkItem = requestWorkItem.CreateReply(requestWorkItem.RequestDetail.WorkItemData.Clone());

                //DoneQueue.Enqueue(responseWorkItem);
                //_request.Return();
                //requestWorkItem.Dispose();
                //MessageProcessed();
                //return requestWorkItem;
            }
            catch (Exception ex)
            {
                ex.Log(@"Error Processing:", LogSeverity.error);
            }
            return null;
        }

        private void Consume()
        {
            bool success;
            bool sleepOne;
            int counter;
            WorkItemBase<UOWBenchMark, UOWBenchMark> responseWorkItem;
            while (IsRunning)
            {
                sleepOne = false;
                counter = 0;
                for (int I = 0; I < 1000; I++)
                {
                    responseWorkItem = DoneQueue.DeQueue(out success);
                    if (success)
                    {
                        responseWorkItem.Dispose();
                        counter += 1;
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
            DoneQueue.Clear();
        }

        private int m_Stats_MessagesProduced = 0;
        private int m_Stats_MessagesProcessed = 0;
        private int m_Stats_MessagesConsumed = 0;
        public void CalcStatsWindow(double RunSeconds, out int Stats_MessagesProduced, out int Stats_MessagesProcessed, out int Stats_MessagesConsumed, out int ThreadControllerQueueSize, out int DoneQueueSize)
        {
            SyncLock.EnterReadLock();
            try
            {
                Stats_MessagesProduced = Convert.ToInt32(((double)m_Stats_MessagesProduced) / RunSeconds);
                Stats_MessagesProcessed = Convert.ToInt32(((double)m_Stats_MessagesProcessed) / RunSeconds);
                Stats_MessagesConsumed = Convert.ToInt32(((double)m_Stats_MessagesConsumed) / RunSeconds);
                m_Stats_MessagesProduced = 0;
                m_Stats_MessagesProcessed = 0;
                m_Stats_MessagesConsumed = 0;
            }
            finally
            {
                SyncLock.ExitReadLock();
            }
            ThreadControllerQueueSize = ThreadedUOWProcessor.QueueSizeAllClients();
            DoneQueueSize = DoneQueue.Count;
        }
        private void MessageProduced(int numProduced)
        {
            SyncLock.EnterWriteLock();
            try
            {
                m_Stats_MessagesProduced += numProduced;
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
                m_Stats_MessagesProcessed += 1;
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
                m_Stats_MessagesConsumed += numConsumed;
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
