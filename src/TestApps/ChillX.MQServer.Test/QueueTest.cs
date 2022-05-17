using ChillX.Core.Structures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillX.MQServer.Test
{
    internal class QueueTest
    {
        private static ManualResetEvent BenchWaitHandle = new ManualResetEvent(false);

        private static LockFreeRingBufferQueue<int> TestQueue = new LockFreeRingBufferQueue<int>();
        private static int EnqueueID = 0;
        private static int DeQueueID = 0;
        private static ConcurrentQueue<int> DeQueuedValues = new ConcurrentQueue<int>();
        private static int numReps = 10000000;
        private static int numEnqueueThreads = 2;
        private static int numDeQueueThreads = 2;

        private static void InterlockedAssignMethod(ref int location, int value)
        {
            Interlocked.Exchange(ref location, value);
        }

        private static void Test_Enqueue()
        {
            int result;
            int numRepsInner;
            numRepsInner = numReps / 20;
            BenchWaitHandle.WaitOne();
            for (int n = 0; n < 20; n++)
            {
                for (int I = 0; I < numRepsInner; I++)
                {
                    result = Interlocked.Increment(ref EnqueueID);
                    TestQueue.Enqueue(result);
                }
                System.Threading.Thread.Sleep(100);
            }
        }


        private static void Test_DeQueue()
        {
            bool success;
            int result;
            int validate;
            int count;

            int numRepsInner;
            numRepsInner = (numReps * numEnqueueThreads) / numDeQueueThreads;


            BenchWaitHandle.WaitOne();
            count = 0;
            while (count < numRepsInner)
            {
                result = TestQueue.DeQueue(out success);
                if (success)
                {
                    count++;
                    DeQueuedValues.Enqueue(result);
                    validate = Interlocked.Increment(ref DeQueueID);
                }
            }
        }

        private static void Test_Enqueue_Profiler()
        {
            int numRepsInner;
            int result;
            numRepsInner = numReps / numEnqueueThreads;
            BenchWaitHandle.WaitOne();
            for (int I = 0; I < numRepsInner; I++)
            {
                TestQueue.Enqueue(I);
            }
        }
        private static void Test_DeQueue_Profiler()
        {
            bool success;
            int result;
            int count;

            int numRepsInner;
            numRepsInner = numReps / numDeQueueThreads;


            BenchWaitHandle.WaitOne();
            count = 0;
            while (count < numRepsInner)
            {
                result = TestQueue.DeQueue(out success);
                if (success)
                {
                    count++;
                }
            }
        }

        public static void TestBench_Queue()
        {
            int validate;
            bool success;

            //ThreadsafeCounter CounterTest = new ThreadsafeCounter(0,8);
            //for (int I = 0; I < 16; I++)
            //{
            //    validate = CounterTest.NextID();
            //}

            //TestQueue = new LockFreeRingBufferQueue<int>();
            //for (int I = 1; I <= 3; I++)
            //{
            //    TestQueue.Enqueue(I);
            //}

            //for (int I = 1; I <= 6; I++)
            //{
            //    int result;
            //    result = TestQueue.DeQueue(out success);
            //    if (success)
            //    {
            //        if (result != I)
            //        {

            //        }
            //    }
            //    else
            //    {
            //    }
            //}
            //for (int I = 6; I <= 10; I++)
            //{
            //    TestQueue.Enqueue(I);
            //}
            //for (int I = 6; I <= 10; I++)
            //{
            //    int result;
            //    result = TestQueue.DeQueue(out success);
            //    if (success)
            //    {
            //        if (result != I)
            //        {

            //        }
            //    }
            //    else
            //    {
            //        I--;
            //    }
            //}



            bool ProfilerMode;
            ProfilerMode = false;
            Stopwatch sw = new Stopwatch();

            numEnqueueThreads = 4;
            numDeQueueThreads = 4;
            List<Thread> threadsList = new List<Thread>();
            BenchWaitHandle.Reset();
            if (ProfilerMode)
            {
                for (int I = 0; I < numEnqueueThreads; I++)
                {
                    threadsList.Add(new Thread(new ThreadStart(Test_Enqueue_Profiler)));
                }
                for (int I = 0; I < numDeQueueThreads; I++)
                {
                    threadsList.Add(new Thread(new ThreadStart(Test_DeQueue_Profiler)));
                }
            }
            else
            {
                for (int I = 0; I < numEnqueueThreads; I++)
                {
                    threadsList.Add(new Thread(new ThreadStart(Test_Enqueue)));
                }
                for (int I = 0; I < numDeQueueThreads; I++)
                {
                    threadsList.Add(new Thread(new ThreadStart(Test_DeQueue)));
                }
            }
            foreach (Thread t in threadsList)
            {
                t.Start();
            }
            sw.Start();
            BenchWaitHandle.Set();
            foreach (Thread t in threadsList)
            {
                t.Join();
            }
            sw.Stop();


            if (ProfilerMode)
            {
                Console.WriteLine(@"Done - Remaining {0} - Time: {1}", TestQueue.Count, sw.Elapsed.ToString());
                return;
            }
            else
            {
                Console.WriteLine(@"Enqueued {0}  -  Dequeued {1} - Remaining {2}- Time: {3}", EnqueueID, DeQueueID, TestQueue.Count, sw.Elapsed.ToString());
            }

            HashSet<int> ValidateHashSet = new HashSet<int>();
            for (int I = 1; I <= numReps * numEnqueueThreads; I++)
            {
                ValidateHashSet.Add(I);
            }

            success = DeQueuedValues.TryDequeue(out validate);
            int Counter = 0;
            while (success)
            {
                if (ValidateHashSet.Contains(validate))
                {
                    ValidateHashSet.Remove(validate);
                    Counter++;
                }
                else
                {
                    Console.WriteLine(@"Invalid Dequeued Value {0}", validate);
                }
                success = DeQueuedValues.TryDequeue(out validate);
            }

            Console.WriteLine(@"Done");

            Console.ReadLine();

        }

    }
}
