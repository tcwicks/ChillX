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
    internal class RentedBufferTest
    {
        public void Run(int numProducers, int numConsumers, int _maxWorkItemLimitPerClient = int.MaxValue, int _maxWorkerThreads = 8, int _threadStartupPerWorkItems = 4, int _threadStartupMinQueueSize = 4, int _idleWorkerThreadExitSeconds = 15, int _processedQueueMaxSize = 0)
        {
           
            DoneQueue = new ThreadSafeQueue<RentedBuffer<byte>>();

            rnd = new Random();

            IsRunning = true;

            numProducers = Math.Max(numProducers, 1);
            numConsumers = Math.Max(numConsumers, 1);
            for (int i = 0; i < numProducers; i++)
            {
                Thread runThrad = new Thread(new ThreadStart(Produce));
                runThrad.Start();
            }
            for (int i = 0; i < numProducers; i++)
            {
                Thread runThrad = new Thread(new ThreadStart(Consume));
                runThrad.Start();
            }
        }

        private ThreadSafeQueue<RentedBuffer<byte>> DoneQueue;

        private Random rnd;

        private int m_BenchmarkArraySize = 64;

        private void Produce()
        {
            bool SleepOne;
            int Counter;
            RentedBuffer<byte> buffer;
            byte[] bufferData;
            bufferData = new byte[m_BenchmarkArraySize];
            for (int I = 0; I < m_BenchmarkArraySize; I++)
            {
                bufferData[I] = (byte)rnd.Next(0, byte.MaxValue);
            }
            while (IsRunning)
            {
                SleepOne = false;
                Counter = 0;
                for (int I = 0; I < 1000; I++)
                {
                    if (DoneQueue.Count < 20000)
                    {
                        Counter += 1;
                        buffer = RentedBuffer<byte>.Shared.Rent(m_BenchmarkArraySize);
                        Array.Copy(bufferData, 0, buffer._rawBufferInternal, 0, m_BenchmarkArraySize);
                        DoneQueue.Enqueue(buffer);
                    }
                    else
                    {
                        SleepOne = true;
                        break;
                    }
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

        private void Consume()
        {
            bool success;
            bool sleepOne;
            int counter;
            RentedBuffer<byte> buffer;
            while (IsRunning)
            {
                sleepOne = false;
                counter = 0;
                for (int I = 0; I < 1000; I++)
                {
                    buffer = DoneQueue.DeQueue(out success);
                    if (success)
                    {
                        buffer.Return();
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
            DoneQueue.Clear();
        }

        private int m_Stats_MessagesProduced = 0;
        private int m_Stats_MessagesConsumed = 0;
        public void CalcStatsWindow(double RunSeconds, out int Stats_MessagesProduced, out int Stats_MessagesConsumed, out int DoneQueueSize)
        {
            SyncLock.EnterReadLock();
            try
            {
                Stats_MessagesProduced = Convert.ToInt32(((double)m_Stats_MessagesProduced) / RunSeconds);
                Stats_MessagesConsumed = Convert.ToInt32(((double)m_Stats_MessagesConsumed) / RunSeconds);
                m_Stats_MessagesProduced = 0;
                m_Stats_MessagesConsumed = 0;
            }
            finally
            {
                SyncLock.ExitReadLock();
            }
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
