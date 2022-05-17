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

namespace ChillX.MQServer.Benchmark
{
    //[SimpleJob(RuntimeMoniker.NetCoreApp20), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.Net60), SimpleJob(RuntimeMoniker.Net50), SimpleJob(RuntimeMoniker.Net471), SimpleJob(RuntimeMoniker.Net48)]
    //[MemoryDiagnoser]
    //[ThreadingDiagnoser]
    //[ConcurrencyVisualizerProfiler]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class Bench_WorkItem : BenchBase
    {
        private static Random rnd = new Random();

        public enum Enum_TestType
        {
            SendRecv_Buffer = 0,
            SendRecv_WorkItem = 1,
        }

        
        [Params(Enum_TestType.SendRecv_Buffer, Enum_TestType.SendRecv_WorkItem)]
        public Enum_TestType TestType;


        [Params(25000)]
        public int numRepititions;

        private int m_numThreads;
        [Params(1)]
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
            }
        }

        private ThreadSafeQueue<RentedBuffer<byte>> Queue_Buffer = new ThreadSafeQueue<RentedBuffer<byte>>();
        private ThreadSafeQueue<WorkItemBase<UOWBenchMark, UOWBenchMark>> Queue_WorkItems = new ThreadSafeQueue<WorkItemBase<UOWBenchMark, UOWBenchMark>>();

        private UOWBenchMark Payload;

        private int numReps = 1;
        protected override void OnGlobalSetup()
        {
            Payload = new UOWBenchMark().RandomizeData(rnd, arraySize);
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
                Thread.Sleep(1);
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
            Console.WriteLine(@"Cleanup Complete: Pending Size Check: {0} - ThreadsRunning: {1} - HasItems: {2}", 0, ThreadsIsRunning, Queue_Buffer.Count);
            Console.WriteLine(@"===================================================================================================");
        }

        protected override bool EnablePublisher
        {
            get { return true; }
        }

        protected override void Publish()
        {
            RentedBuffer<byte> buffer;
            WorkItemBase<UOWBenchMark, UOWBenchMark> BenchMarkWorkItem;
            UOWBenchMark PayloadInstance;
            BenchMarkWorkItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>(0, 0, 1, 0, 0, 1, MQPriority.System);
            PayloadInstance = Payload.Clone();
            Queue_Buffer.Clear();

            switch (TestType)
            {
                case Enum_TestType.SendRecv_Buffer:
                    for (int I = 0; I < numReps; I++)
                    {
                        while (Queue_Buffer.Count > 10000) { Thread.Sleep(0); }
                        buffer = ChillXSerializer<UOWBenchMark>.ReadToRentedBuffer(PayloadInstance.Clone());
                        Queue_Buffer.Enqueue(buffer);
                        BenchMarkWorkItem.Dispose();
                    }
                    break;
                case Enum_TestType.SendRecv_WorkItem:
                    for (int I = 0; I < numReps; I++)
                    {
                        while (Queue_Buffer.Count > 10000) { Thread.Sleep(0); }
                        BenchMarkWorkItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>(0, 0, 1, 0, 0, 1, MQPriority.System);
                        BenchMarkWorkItem.RequestDetail.WorkItemData = PayloadInstance.Clone();
                        buffer = ChillXSerializer<WorkItemBaseCore>.ReadToRentedBuffer(BenchMarkWorkItem);
                        Queue_Buffer.Enqueue(buffer);
                        BenchMarkWorkItem.Dispose();
                    }
                    break;
            }
        }

        protected override bool EnableSubscriber
        {
            get { return true; }
        }

        protected override bool SubscriberHasWork => Queue_Buffer.HasItems();

        protected override void Subscribe()
        {
            WorkItemBaseCore BenchMarkWorkItemCore;
            WorkItemBase<UOWBenchMark, UOWBenchMark> BenchMarkWorkItem = null;
            RentedBuffer<byte> buffer;
            UOWBenchMark payloadInstance;
            int bytesConsumed = 0;
            switch (TestType)
            {
                case Enum_TestType.SendRecv_Buffer:
                    while (true)
                    {
                        buffer = Queue_Buffer.DeQueue();
                        if (buffer != null)
                        {
                            payloadInstance = new UOWBenchMark();
                            if (ChillXSerializer<UOWBenchMark>.Write(payloadInstance, buffer._rawBufferInternal, out bytesConsumed))
                            {
                                payloadInstance.Dispose();
                            }
                            buffer.Return();
                        }
                        else
                        {
                            if ((!Queue_Buffer.HasItems()) && (!ThreadsIsRunning))
                            {
                                break;
                            }
                        }
                    }
                    break;
                case Enum_TestType.SendRecv_WorkItem:
                    while (true)
                    {
                        buffer = Queue_Buffer.DeQueue();
                        if (buffer != null)
                        {
                            BenchMarkWorkItemCore = new WorkItemBaseCore();
                            if (ChillXSerializer<WorkItemBaseCore>.Write(BenchMarkWorkItemCore, buffer._rawBufferInternal, out bytesConsumed))
                            {
                                BenchMarkWorkItem = new WorkItemBase<UOWBenchMark, UOWBenchMark>(BenchMarkWorkItemCore);
                            }
                            BenchMarkWorkItemCore.Dispose();
                            buffer.Return();
                        }
                        else
                        {
                            if ((!Queue_Buffer.HasItems()) && (!ThreadsIsRunning))
                            {
                                break;
                            }
                        }
                    }
                    break;
            }
            int Blah;
            if (bytesConsumed > 100)
            {
                Blah = bytesConsumed;
            }
        }
    }
}
