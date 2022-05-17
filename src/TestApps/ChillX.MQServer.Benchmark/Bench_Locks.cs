/*
ChillX Framework Test Application
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

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ChillX.Core.Structures;
using ChillX.MQServer.Server.SystemMessage;
using ChillX.MQServer.UnitOfWork;
using ChillX.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1645 (21H2)
AMD Ryzen Threadripper 3970X, 1 CPU, 64 logical and 32 physical cores
.NET SDK=6.0.202
  [Host]   : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT  [AttachedDebugger]
  .NET 6.0 : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT

Job=.NET 6.0  Runtime=.NET 6.0

|                Method |           m_TestMode | numRepititions | numThreads |      Mean |     Error |     StdDev | Completed Work Items | Lock Contentions | Allocated |
|---------------------- |--------------------- |--------------- |----------- |----------:|----------:|-----------:|---------------------:|-----------------:|----------:|
| Bench_LockPerformance |     LockReaderWriter |          40000 |          4 | 166.47 ms | 15.845 ms |  46.719 ms |                    - |                - |  22,672 B |
| Bench_LockPerformance | LockReaderWriterSlim |          40000 |          4 |  19.93 ms |  1.047 ms |   3.053 ms |                    - |                - |      24 B |
| Bench_LockPerformance |          LockMonitor |          40000 |          4 |  21.73 ms |  0.896 ms |   2.643 ms |                    - |         111.4063 |       4 B |
| Bench_LockPerformance |         LockSpinLock |          40000 |          4 |  80.75 ms |  6.081 ms |  17.643 ms |                    - |                - |      96 B |
| Bench_LockPerformance | LockS(...)Fence [21] |          40000 |          4 |  89.09 ms |  6.515 ms |  19.108 ms |                    - |                - |      60 B |
| Bench_LockPerformance |      LockInterlocked |          40000 |          4 | 321.08 ms | 54.203 ms | 159.818 ms |                    - |                - |      69 B |
*/


namespace ChillX.MQServer.Benchmark
{
    //[SimpleJob(RuntimeMoniker.NetCoreApp20), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.Net60), SimpleJob(RuntimeMoniker.Net50), SimpleJob(RuntimeMoniker.Net471), SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    //[ConcurrencyVisualizerProfiler]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class Bench_Locks : BenchBase
    {
        public enum TestMode
        {
            ReaderWriter = 0,
            ReaderWriterSlim = 1,
            Monitor = 2,
            SpinLock = 3,
            SpinLckExitFence = 4,
            Interlocked = 5,
        }

        //[Params(TestMode.LockReaderWriter)]
        [Params(TestMode.ReaderWriter, TestMode.ReaderWriterSlim, TestMode.Monitor, TestMode.SpinLock, TestMode.SpinLckExitFence, TestMode.Interlocked)]
        public TestMode m_TestMode = TestMode.ReaderWriter;

        private static Random rnd = new Random();
        [Params(40000)]
        public int numRepititions;

        private int m_numThreads;
        [Params(4)]
        public override int numThreads
        {
            get
            {
                return m_numThreads;
            }
            set
            {
                m_numThreads = value;
            }
        }

        private readonly Queue<int> m_queue = new Queue<int>();
        private readonly ReaderWriterLock m_LockReaderWriter = new ReaderWriterLock();
        private readonly ReaderWriterLockSlim m_LockReaderWriterSlim = new ReaderWriterLockSlim();
        private readonly object m_LockMonitor = new object();
        private SpinLock m_LockSpinLock = new SpinLock();
        private int m_LockInterlocked = 0;

        private volatile int QueueSize = 0;

        private int numReps = 1;
        protected override void OnGlobalSetup()
        {
            numReps = numRepititions / numThreads;
            m_queue.Clear();
            QueueSize = 0;
            Console.WriteLine(@"==============================================================================================");
            Console.WriteLine(@"Setup is run: Num Threads: {0}  -  numReps: {1} - Test Type: {2}", numThreads, numReps, Enum.GetName(typeof(TestMode), m_TestMode));
            Console.WriteLine(@"==============================================================================================");
        }

        [Benchmark]
        public void Bench_LockPerformance()
        {
            ThreadRunOneItteration();
            while (QueueSize > 0)
            {
                Thread.Sleep(1);
            }
        }

        protected override void OnGlobalCleanup()
        {
            Console.WriteLine(@"===================================================================================================");
            Console.WriteLine(@"Cleaning up.");
            Console.WriteLine(@"===================================================================================================");
            if (QueueSize > 0)
            {
                //Queue_Buffer.WaitHandlesSet();
                Thread.Sleep(10);
            }
            Console.WriteLine(@"===================================================================================================");
            Console.WriteLine(@"Cleanup Complete: Pending Size Check: {0} - ThreadsRunning: {1} - HasItems: {2} - Test Type: {2}", 0, ThreadsIsRunning, m_queue.Count, Enum.GetName(typeof(TestMode), m_TestMode));
            Console.WriteLine(@"===================================================================================================");
        }

        protected override bool EnablePublisher
        {
            get { return true; }
        }

        protected override void Publish()
        {
            switch (m_TestMode)
            {
                case TestMode.ReaderWriter:
                    for (int I = 0; I < numReps; I++)
                    {
                        m_LockReaderWriter.AcquireWriterLock(Timeout.Infinite);
                        try
                        {
                            m_queue.Enqueue(I);
                        }
                        finally
                        {
                            m_LockReaderWriter.ReleaseWriterLock();
                        }
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
                case TestMode.ReaderWriterSlim:
                    for (int I = 0; I < numReps; I++)
                    {
                        m_LockReaderWriterSlim.EnterWriteLock();
                        try
                        {
                            m_queue.Enqueue(I);
                        }
                        finally
                        {
                            m_LockReaderWriterSlim.ExitWriteLock();
                        }
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
                case TestMode.Monitor:
                    for (int I = 0; I < numReps; I++)
                    {
                        lock(m_LockMonitor)
                        {
                            m_queue.Enqueue(I);
                        }
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
                case TestMode.SpinLock:
                    for (int I = 0; I < numReps; I++)
                    {
                        bool lockTaken = false;
                        while (!lockTaken)
                        {
                            m_LockSpinLock.Enter(ref lockTaken);
                        }
                        try
                        {
                            m_queue.Enqueue(I);
                        }
                        finally
                        {
                            m_LockSpinLock.Exit(false);
                        }
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
                case TestMode.SpinLckExitFence:
                    for (int I = 0; I < numReps; I++)
                    {
                        bool lockTaken = false;
                        while (!lockTaken)
                        {
                            m_LockSpinLock.Enter(ref lockTaken);
                        }
                        try
                        {
                            m_queue.Enqueue(I);
                        }
                        finally
                        {
                            m_LockSpinLock.Exit(true);
                        }
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
                case TestMode.Interlocked:
                    for (int I = 0; I < numReps; I++)
                    {
                        while (Interlocked.CompareExchange(ref m_LockInterlocked, 1,0) != 0) { }
                        try
                        {
                            m_queue.Enqueue(I);
                        }
                        finally
                        {
                            Interlocked.Exchange(ref m_LockInterlocked, 0);
                        }
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
            }
        }

        protected override bool EnableSubscriber
        {
            get { return true; }
        }

        protected override bool SubscriberHasWork => QueueSize > 0;

        protected override void Subscribe()
        {
            bool success;
            switch (m_TestMode)
            {
                case TestMode.ReaderWriter:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            m_LockReaderWriter.AcquireWriterLock(Timeout.Infinite);
                            try
                            {
                                if (m_queue.Count > 0)
                                {
                                    m_queue.Dequeue();
                                    success = true;
                                }
                                else
                                {
                                    if (!ThreadsIsRunning) { break; }
                                }
                            }
                            finally
                            {
                                m_LockReaderWriter.ReleaseWriterLock();
                            }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
                case TestMode.ReaderWriterSlim:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            m_LockReaderWriterSlim.EnterWriteLock();
                            try
                            {
                                if (m_queue.Count > 0)
                                {
                                    m_queue.Dequeue();
                                    success = true;
                                }
                                else
                                {
                                    if (!ThreadsIsRunning) { break; }
                                }
                            }
                            finally
                            {
                                m_LockReaderWriterSlim.ExitWriteLock();
                            }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
                case TestMode.Monitor:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            lock (m_LockMonitor)
                            {
                                if (m_queue.Count > 0)
                                {
                                    m_queue.Dequeue();
                                    success = true;
                                }
                                else
                                {
                                    if (!ThreadsIsRunning) { break; }
                                }
                            }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
                case TestMode.SpinLock:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            bool lockTaken = false;
                            while (!lockTaken)
                            {
                                m_LockSpinLock.Enter(ref lockTaken);
                            }
                            try
                            {
                                if (m_queue.Count > 0)
                                {
                                    m_queue.Dequeue();
                                    success = true;
                                }
                                else
                                {
                                    if (!ThreadsIsRunning) { break; }
                                }
                            }
                            finally
                            {
                                m_LockSpinLock.Exit(false);
                            }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
                case TestMode.SpinLckExitFence:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            bool lockTaken = false;
                            while (!lockTaken)
                            {
                                m_LockSpinLock.Enter(ref lockTaken);
                            }
                            try
                            {
                                if (m_queue.Count > 0)
                                {
                                    m_queue.Dequeue();
                                    success = true;
                                }
                                else
                                {
                                    if (!ThreadsIsRunning) { break; }
                                }
                            }
                            finally
                            {
                                m_LockSpinLock.Exit(true);
                            }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
                case TestMode.Interlocked:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            while (Interlocked.CompareExchange(ref m_LockInterlocked, 1, 0) != 0) { }
                            try
                            {
                                if (m_queue.Count > 0)
                                {
                                    m_queue.Dequeue();
                                    success = true;
                                }
                                else
                                {
                                    if (!ThreadsIsRunning) { break; }
                                }
                            }
                            finally
                            {
                                Interlocked.Exchange(ref m_LockInterlocked, 0);
                            }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
            }
        }
    }
}
