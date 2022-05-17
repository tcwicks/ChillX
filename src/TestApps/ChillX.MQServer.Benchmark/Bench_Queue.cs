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
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using ChillX.Core.Structures;
using ChillX.MQServer.Server.SystemMessage;
using ChillX.MQServer.UnitOfWork;
using ChillX.Serialization;
using System;
using System.Collections.Concurrent;
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

|                 Method |           m_TestMode | numRepititions | numThreads |     Mean |    Error |   StdDev |     Gen 0 | Completed Work Items | Lock Contentions |     Gen 1 |     Gen 2 |    Allocated |
|----------------------- |--------------------- |--------------- |----------- |---------:|---------:|---------:|----------:|---------------------:|-----------------:|----------:|----------:|-------------:|
| Bench_QueuePerformance |        LockFreeQueue |        1000000 |          4 | 607.8 ms | 11.97 ms | 15.14 ms | 5000.0000 |                    - |                - | 3000.0000 | 1000.0000 | 40,002,976 B |
| Bench_QueuePerformance |      ThreadSafeQueue |        1000000 |          4 | 322.0 ms | 19.68 ms | 58.02 ms |         - |                    - |                - |         - |         - |        992 B |
| Bench_QueuePerformance | ConcurrentQueueCount |        1000000 |          4 | 421.2 ms |  8.38 ms | 14.90 ms |         - |                    - |                - |         - |         - |      9,360 B |
| Bench_QueuePerformance |   ConcurrentQueueTry |        1000000 |          4 | 330.1 ms |  6.82 ms | 20.01 ms |         - |                    - |          11.5000 |         - |         - |  1,116,912 B |



| Method | m_TestMode | numRepititions | numThreads | Mean | Error | StdDev | Completed Work Items | Lock Contentions |     Gen 0 |     Gen 1 |    Gen 2 | Allocated |
|----------------------- |--------------------- |--------------- |----------- |----------:| ----------:| ----------:| ---------------------:| -----------------:| ----------:| ----------:| ---------:| ----------:|
        | Bench_QueuePerformance | RingBufferQueue | 1000000 | 1 | 80.31 ms | 3.194 ms | 8.742 ms | - | - | 3400.0000 | 1400.0000 | - | 28,566 KB |
| Bench_QueuePerformance | ThreadSafeQueue | 1000000 | 1 | 96.60 ms | 1.910 ms | 3.901 ms | - | - | - | - | - | 1 KB |
| Bench_QueuePerformance | ConcurrentQueueCount | 1000000 | 1 | 70.94 ms | 1.410 ms | 2.750 ms | - | - | 500.0000 | 500.0000 | 500.0000 | 2,948 KB |
| Bench_QueuePerformance | ConcurrentQueueTry | 1000000 | 1 | 49.13 ms | 2.274 ms | 6.704 ms | - | 0.1250 | - | - | - | 283 KB |
| Bench_QueuePerformance | RingBufferQueue | 1000000 | 4 | 367.08 ms | 10.954 ms | 32.128 ms | - | - | 3000.0000 | 1000.0000 | - | 28,566 KB |
| Bench_QueuePerformance | ThreadSafeQueue | 1000000 | 4 | 253.59 ms | 12.344 ms | 36.398 ms | - | - | - | - | - | 1 KB |
| Bench_QueuePerformance | ConcurrentQueueCount | 1000000 | 4 | 341.09 ms | 12.249 ms | 36.116 ms | - | 18.5000 | - | - | - | 1,540 KB |
| Bench_QueuePerformance | ConcurrentQueueTry | 1000000 | 4 | 266.15 ms | 13.365 ms | 39.408 ms | - | 11.6667 | 333.3333 | 333.3333 | 333.3333 | 2,223 KB |
 */



namespace ChillX.MQServer.Benchmark
{
    //[SimpleJob(RuntimeMoniker.NetCoreApp20), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.Net60), SimpleJob(RuntimeMoniker.Net50), SimpleJob(RuntimeMoniker.Net471), SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    //[ConcurrencyVisualizerProfiler]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class Bench_Queue : BenchBase
    {
        public enum TestMode
        {
            LockFreeQueue = 0,
            RingBufferQueue = 1,
            ThreadSafeQueue = 2,
            ConcurrentQueueCount = 3,
            ConcurrentQueueTry = 4,
        }

        //[Params(TestMode.RingBufferQueue, TestMode.ThreadSafeQueue, TestMode.ConcurrentQueueCount, TestMode.ConcurrentQueueTry)]
        [Params(TestMode.RingBufferQueue)]
        public TestMode m_TestMode = TestMode.LockFreeQueue;

        private static Random rnd = new Random();
        [Params(2000000)]
        public int numRepititions;

        private int m_numThreads;
        [Params(10)]
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

        private readonly LockFreeQueue<int> m_queueLockFree = new LockFreeQueue<int>(true);
        private readonly ThreadSafeQueue<int> m_queueThreadSafe = new ThreadSafeQueue<int>();
        private LockFreeRingBufferQueue<int> m_queueThreadSafeRingBuffer = new LockFreeRingBufferQueue<int>(8192);
        private readonly ConcurrentQueue<int> m_queueConcurrent = new ConcurrentQueue<int>();

        private volatile int QueueSize = 0;

        private int numReps = 1;
        protected override void OnGlobalSetup()
        {
            numReps = numRepititions / numThreads;
            m_queueLockFree.Clear();
            m_queueThreadSafe.Clear();
            m_queueThreadSafeRingBuffer.ClearNotThreadSafe();
            m_queueThreadSafeRingBuffer = new LockFreeRingBufferQueue<int>(64);
            m_queueConcurrent.Clear();
            Interlocked.Exchange(ref QueueSize, 0);
            Console.WriteLine(@"==============================================================================================");
            Console.WriteLine(@"Setup is run: Num Threads: {0}  -  numReps: {1} - Test Type: {2}", numThreads, numReps, Enum.GetName(typeof(TestMode), m_TestMode));
            Console.WriteLine(@"==============================================================================================");
        }


        [Benchmark]
        public void Bench_QueuePerformance()
        {
            m_queueLockFree.Clear();
            m_queueThreadSafe.Clear();
            m_queueThreadSafeRingBuffer.ClearNotThreadSafe();
            m_queueConcurrent.Clear();
            Interlocked.Exchange(ref QueueSize, 0);
            Interlocked.Exchange(ref SubscribeIsRunning, 1);
            ThreadRunOneItteration();
            while (QueueSize > 0)
            {
                Thread.Sleep(1);
            }
            Interlocked.Exchange(ref SubscribeIsRunning, 0);
            m_queueConcurrent.Clear();
            Interlocked.Exchange(ref QueueSize, 0);
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
            int count = 0;
            switch (m_TestMode)
            {
                case TestMode.LockFreeQueue:
                    count = m_queueLockFree.Count;
                    break;
                case TestMode.ThreadSafeQueue:
                    count = m_queueThreadSafe.Count;
                    break;
                case TestMode.RingBufferQueue:
                    count = m_queueThreadSafeRingBuffer.Count;
                    break;
                case TestMode.ConcurrentQueueCount:
                    count = m_queueConcurrent.Count;
                    break;
                case TestMode.ConcurrentQueueTry:
                    count = m_queueConcurrent.Count;
                    break;
            }

            Console.WriteLine(@"===================================================================================================");
            Console.WriteLine(@"Cleanup Complete: Pending Size Check: {0} - ThreadsRunning: {1} - HasItems: {2} - Test Type: {2}", 0, ThreadsIsRunning, count, Enum.GetName(typeof(TestMode), m_TestMode));
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
                case TestMode.LockFreeQueue:
                    for (int I = 0; I < numReps; I++)
                    {
                        m_queueLockFree.Enqueue(I);
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
                case TestMode.ThreadSafeQueue:
                    for (int I = 0; I < numReps; I++)
                    {
                        m_queueThreadSafe.Enqueue(I);
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
                case TestMode.RingBufferQueue:
                    for (int I = 0; I < numReps; I++)
                    {
                        m_queueThreadSafeRingBuffer.Enqueue(I);
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
                case TestMode.ConcurrentQueueCount:
                    for (int I = 0; I < numReps; I++)
                    {
                        m_queueConcurrent.Enqueue(I);
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
                case TestMode.ConcurrentQueueTry:
                    for (int I = 0; I < numReps; I++)
                    {
                        m_queueConcurrent.Enqueue(I);
                        Interlocked.Increment(ref QueueSize);
                    }
                    break;
            }
        }

        protected override bool EnableSubscriber
        {
            get { return true; }
        }

        protected override bool SubscriberHasWork
        {
            get
            {
                //switch (m_TestMode)
                //{
                //    case TestMode.LockFreeQueue:
                //        return m_queueLockFree.Count > 0;
                //    case TestMode.ThreadSafeQueue:
                //        return m_queueThreadSafe.Count > 0;
                //    case TestMode.ConcurrentQueue:
                //        return m_queueConcurrent.Count > 0;
                //}
                return QueueSize > 0;
            }
        }

        private volatile int SubscribeIsRunning = 0;

        protected override void Subscribe()
        {
            int item;
            bool success;
            switch (m_TestMode)
            {
                case TestMode.LockFreeQueue:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            if (m_queueLockFree.Count > 0)
                            {
                                m_queueLockFree.DeQueue();
                                success = true;
                            }
                            else
                            {
                                if (!ThreadsIsRunning) { break; }
                            }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
                case TestMode.ThreadSafeQueue:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            if (m_queueThreadSafe.Count > 0)
                            {
                                m_queueThreadSafe.DeQueue();
                                success = true;
                            }
                            else
                            {
                                if (!ThreadsIsRunning) { break; }
                            }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
                case TestMode.RingBufferQueue:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            m_queueThreadSafeRingBuffer.DeQueue(out success);
                            if (!ThreadsIsRunning) { break; }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
                case TestMode.ConcurrentQueueCount:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            // In order to keep this fair we are also checking .Count property
                            if (m_queueConcurrent.Count > 0 && m_queueConcurrent.TryDequeue(out item))
                            {
                                success = true;
                            }
                            else
                            {
                                if (!ThreadsIsRunning) { break; }
                            }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
                case TestMode.ConcurrentQueueTry:
                    while (ThreadsIsRunning)
                    {
                        success = false;
                        while (!success)
                        {
                            if (m_queueConcurrent.TryDequeue(out item))
                            {
                                success = true;
                            }
                            else
                            {
                                if (!ThreadsIsRunning) { break; }
                            }
                        }
                        Interlocked.Decrement(ref QueueSize);
                    }
                    break;
            }
        }
    }
}
