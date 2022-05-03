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

/*
Notice: This bencmark app uses Messagepack purely for performance comparison
 */

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ChillX.Core.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChillX.Serialization.Benchmark.ChillXEntity
{
    //[SimpleJob(RuntimeMoniker.NetCoreApp20), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.Net60), SimpleJob(RuntimeMoniker.Net50), SimpleJob(RuntimeMoniker.Net471), SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    //[ConcurrencyVisualizerProfiler]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class Bench_ChillXSerializeEntity : BenchBase
    {
        private static Random rnd = new Random();
        public int numRepititions = 200000;

        private int m_numThreads;
        [Params(1, 2, 4)]
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


        private int m_arraySize;
        [Params(64)]
        public int arraySize
        {
            get { return m_arraySize; }
            set
            {
                m_arraySize = value;
                TestClassOne = TestClassOne_Create(rnd, m_arraySize);
            }
        }

        //private ThreadSafeQueue<ChillXEntity.TestClassVariantA> Queue_ChillX = new ThreadSafeQueue<ChillXEntity.TestClassVariantA>();

        private ThreadSafeQueue<RentedBuffer<byte>> Queue_Buffer = new ThreadSafeQueue<RentedBuffer<byte>>();
        //private ThreadSafeQueue<ChillXLightSpeed.RentedBuffer> Queue_RentedBuffer = new ThreadSafeQueue<ChillXLightSpeed.RentedBuffer>();
        //private TypedSerializer<ChillXEntity.TestClassVariantA> Serializer = TypedSerializer<ChillXEntity.TestClassVariantA>.Create();

        private ChillXEntity.TestClassVariantA TestClassOne = TestClassOne_Create(rnd, 64);
        private static ChillXEntity.TestClassVariantA TestClassOne_Create(Random rnd, int stringSize)
        {
            return new ChillXEntity.TestClassVariantA().RandomizeData(rnd, stringSize);
        }

        protected override void OnGlobalSetup()
        {
            TestClassOne = TestClassOne_Create(rnd, arraySize);
            numReps = numRepititions / numThreads;
            Console.WriteLine(@"==============================================================================================");
            Console.WriteLine(@"Setup is run: Num Threads: {0}  -  numReps: {1}  -  String Size {2}", numThreads, numReps, arraySize);
            Console.WriteLine(@"==============================================================================================");
        }

        [Benchmark]
        public void Bench_ChillXSerializer()
        {
            //pendingSize = 0;
            ThreadRunOneItteration();
            while (Queue_Buffer.HasItems())
            {
                Thread.Sleep(0);
            }
        }

        protected override void OnGlobalCleanup()
        {
            Console.WriteLine(@"===================================================================================================");
            Console.WriteLine(@"Cleaning up.");
            Console.WriteLine(@"===================================================================================================");
            if (Queue_Buffer.HasItems())
            {
                //Queue_Buffer.WaitHandlesSet();
                Thread.Sleep(10);
            }
            Console.WriteLine(@"===================================================================================================");
            Console.WriteLine(@"Cleanup Complete: Pending Size Check: {0} - ThreadsRunning: {1} - HasItems: {2}", 0, ThreadsIsRunning, Queue_Buffer.HasItems());
            Console.WriteLine(@"===================================================================================================");
        }

        private int numReps = 1;
        //private object SizeLock = new object();
        //private volatile int pendingSize = 0;

        protected override bool EnablePublisher
        {
            get { return true; }
        }
        protected override void Publish()
        {
            ChillXEntity.TestClassVariantA TestClassInstance = TestClassOne.Clone();
            int Counter = 0;
            for (int I = 0; I < numReps; I++)
            {
                //while (pendingSize > 10000)
                //{
                //    Thread.Sleep(0);
                //}
                Queue_Buffer.Enqueue(ChillXSerializer<ChillXEntity.TestClassVariantA>.ReadToRentedBuffer(TestClassInstance));
                Counter++;
                //if (Counter > 100)
                //{
                //    lock (SizeLock) { pendingSize+= Counter; }
                //    Counter = 0;
                //}
            }
        }
        protected override bool EnableSubscriber
        {
            get { return true; }
        }
        protected override bool SubscriberHasWork => Queue_Buffer.HasItems();
        protected override void Subscribe()
        {
            ChillXEntity.TestClassVariantA TestClassInstance = TestClassOne.Clone();
            RentedBuffer<byte> buffer;
            int bytesConsumed;
            //while (ThreadsIsRunning || Queue_Buffer.HasItems())
            //{
            //    buffer = Queue_Buffer.Dequeue();
            //    if (buffer != null)
            //    {
            //        if (buffer.buffer == null)
            //        {

            //        }
            //        else
            //        {
            //            Serializer.Write(TestClassInstance, buffer.buffer, out bytesConsumed);
            //        }
            //        buffer.Dispose();
            //        //lock (SizeLock) { pendingSize--; }
            //    }
            //}
            while (true)
            {
                buffer = Queue_Buffer.DeQueue();
                if (buffer != null)
                {
                    if (buffer._rawBufferInternal == null)
                    {

                    }
                    else
                    {
                        ChillXSerializer<ChillXEntity.TestClassVariantA>.Write(TestClassInstance, buffer._rawBufferInternal, out bytesConsumed);
                    }
                    buffer.Return();
                    //lock (SizeLock) { pendingSize--; }
                }
                else
                {
                    if ((!Queue_Buffer.HasItems()) && (!ThreadsIsRunning))
                    {
                        break;
                    }
                }
            }
            int Blah;
            if (TestClassInstance != null)
            {
                Blah = TestClassInstance.VariantAPropertyOne;
            }
        }

    }

}
