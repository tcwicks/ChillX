// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using ChillX.Core.Structures;
using ChillX.Logging;
using ChillX.MQServer;
using ChillX.MQServer.Server.SystemMessage;
using ChillX.MQServer.UnitOfWork;
using ChillX.Serialization;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace ChillX.MQServer.Test
{
    internal class Program
    {
        private const int BenchmarkArraySize = 64;
        private static readonly Random rnd = new Random();

        private static ManualResetEvent BenchWaitHandle = new ManualResetEvent(false);


        static void Main(string[] args)
        {

            ILogHandler ConsoleLogHandler;
            ConsoleLogHandler = new Logging.Handlers.LogHandlerConsole();

            Logging.Logger.BatchSize = 2;
            Logging.Logger.RegisterHandler(@"Console", ConsoleLogHandler);

            bool Exit = false;
            int numThreadsProducer = 1;
            int numThreadsConsumer = 1;
            while (!Exit)
            {
                Console.WriteLine(@"");
                Console.WriteLine(@"------------------------------------------------------------------------");
                Console.WriteLine(@"Q : Exit");
                Console.WriteLine(@"0 : Sanity Test");
                Console.WriteLine(@"1 : Micro Optimization Benchmark List vs Array vs Dictionary");
                Console.WriteLine(@"2 : Test / Benchmark Transparent Rented Buffers");
                Console.WriteLine(@"3 : Test / Benchmark Rented Buffers");
                Console.WriteLine(@"4 : Test / Benchmark Queue");
                Console.WriteLine(@"5 : Test / Benchmark Message Queue With WorkItem Payload - Produce Process Consume");
                Console.WriteLine(@"6 : Test / Benchmark Message Queue With WorkItem Payload - NetworkStream One Way Fully Unrolled");
                Console.WriteLine(@"8 : Test / Benchmark Message Queue Server");
                Console.WriteLine(@"9 : Test / Benchmark Message Queue Server in Benchmark Mode");
                Console.WriteLine(@"------------------------------------------------------------------------");
                ConsoleKeyInfo KeyPress = Console.ReadKey();
                Console.Clear();
                BenchWaitHandle.Reset();
                switch (KeyPress.KeyChar)
                {
                    case '0':
                        SanityTest();
                        break;
                    case '1':
                        MicroBenchmarkListVsArrayvsDict();
                        break;
                    case '2':
                        Bench_TransparentRentedBuffers();
                        break;
                    case '3':
                        Console.WriteLine(@"Enter number of Producer threads");
                        KeyPress = Console.ReadKey();
                        if (int.TryParse(KeyPress.KeyChar.ToString(), out numThreadsProducer))
                        {
                            Console.WriteLine(@"");
                            Console.WriteLine(@"Enter number of Consumer threads");
                            KeyPress = Console.ReadKey();
                            if (int.TryParse(KeyPress.KeyChar.ToString(), out numThreadsConsumer))
                            {
                                Console.WriteLine(@"");
                                RentedBuffer_Produce_Consume(numThreadsProducer, numThreadsConsumer);

                            }
                            else
                            {
                                RentedBuffer_Produce_Consume(numThreadsProducer, numThreadsProducer);
                            }
                        }
                        else
                        {
                            RentedBuffer_Produce_Consume(1, 1);
                        }
                        break;
                    case '4':
                        TestQueue();
                        break;
                    case '5':
                        Console.WriteLine(@"Enter number of Producer threads");
                        KeyPress = Console.ReadKey();
                        if (int.TryParse(KeyPress.KeyChar.ToString(),out numThreadsProducer))
                        {
                            Console.WriteLine(@"");
                            Console.WriteLine(@"Enter number of Consumer threads");
                            KeyPress = Console.ReadKey();
                            if (int.TryParse(KeyPress.KeyChar.ToString(), out numThreadsConsumer))
                            {
                                Console.WriteLine(@"");
                                MessageQueue_Produce_Process_Consume(numThreadsProducer, numThreadsConsumer);

                            }
                            else
                            {
                                MessageQueue_Produce_Process_Consume(numThreadsProducer, numThreadsProducer);
                            }
                        }
                        else
                        {
                            MessageQueue_Produce_Process_Consume(1, 1);
                        }
                        break;
                    case '6':
                        ThreadControllerTest_FullyUnrolled(1,1);
                        break;
                    case '8':
                        MQServerTest(false);
                        break;
                    case '9':
                        MQServerTest(true);
                        break;
                    default:
                        switch(KeyPress.Key)
                        {
                            case ConsoleKey.Q:
                                System.Environment.Exit(1);
                                return;
                        }
                        break;
                }
            }
            
            Console.ReadLine();
        }

        private static void MicroBenchmarkListVsArrayvsDict()
        {
            Dictionary<int, int> TestDict = new Dictionary<int, int>();
            List<int> TestList = new List<int>();
            int[] TestArray;
            for (int I = 0; I < 10; I++)
            {
                TestDict.Add(I, I);
                TestList.Add(I);
            }
            TestArray = TestList.ToArray();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int TestValue = 0;
            sw.Restart();
            for (int I = 0; I < 100000000; I++)
            {
                TestValue = TestDict[I % 10];
            }
            sw.Stop();
            Console.WriteLine(@"Dict  Num Items: {0} - Time: {1}", TestValue, sw.Elapsed);

            sw.Restart();
            for (int I = 0; I < 100000000; I++)
            {
                TestValue = TestList[I % 10];
            }
            sw.Stop();
            Console.WriteLine(@"List  Num Items: {0} - Time: {1}", TestValue, sw.Elapsed);

            sw.Restart();
            for (int I = 0; I < 100000000; I++)
            {
                TestValue = TestArray[I % 10];
            }
            sw.Stop();
            Console.WriteLine(@"Array Num Items: {0} - Time: {1}", TestValue, sw.Elapsed);
            Console.WriteLine(@"");
        }

        private static void SanityTest()
        {
            WorkItemBaseCore SanityTest;
            WorkItemBase<UOWBenchMark, UOWBenchMark> SanityTestOne = new WorkItemBase<UOWBenchMark, UOWBenchMark>(1, 2, 3,
                    1, 2, 3, MQPriority.System);
            WorkItemBase<UOWBenchMark, UOWBenchMark> SanityTestTwo;
            byte[] buffer;

            SanityTestOne.RequestDetail.WorkItemData = new UOWBenchMark().RandomizeData(rnd, BenchmarkArraySize);

            SanityTest = SanityTestOne;
            buffer = ChillXSerializer<WorkItemBaseCore>.Read(SanityTest);
            SanityTest = new WorkItemBaseCore();
            ChillXSerializer<WorkItemBaseCore>.Write(SanityTest, buffer);
            SanityTestTwo = new WorkItemBase<UOWBenchMark, UOWBenchMark>(SanityTest);

            bool IsEqual;
            IsEqual = SanityTestOne.RequestDetail.WorkItemData.EqualsDebug(SanityTestTwo.RequestDetail.WorkItemData);
            
            if (IsEqual)
            {
                Console.WriteLine(@"Sanity Test Passed");
            }
            else
            {
                Console.WriteLine(@"Sanity Test FAILED !!!");
            }
        }

        #region Benchmark Transparent Rented Buffers
        private static Queue<Queue<WorkItemBase<UOWBenchMark, UOWBenchMark>>> BenchMarkQueue = new Queue<Queue<WorkItemBase<UOWBenchMark, UOWBenchMark>>>();
        private static string BenchMarkPayload;

        private static volatile int ThreadCounter = 0;
        private static object ThreadCounterLocked = new object();
        private static void AddRemoveStuff()
        {
            UOWBenchMark PayloadCopy = null;
            Payload = Payload ?? new UOWBenchMark().RandomizeData(rnd, 128);

            int ThreadNum;
            lock (ThreadCounterLocked)
            {
                Interlocked.Increment(ref ThreadCounter);
                ThreadNum = ThreadCounter;
            }
            Payload.ArrayProperty_Char = Payload.ArrayProperty_Char + string.Concat(@"Thread: ", ThreadNum.ToString(), @" - The quick brown fox jumped over the lazy dog").ToCharArray();
            BenchWaitHandle.WaitOne();
            for (int I = 0; I < 100000; I++)
            {
                PayloadCopy = PayloadCopy ?? Payload.Clone();
                PayloadCopy.Dispose();
                PayloadCopy = null;
            }
            if (Payload != null)
            {
                Payload.Dispose();
                Payload = null;
            }
        }

        [ThreadStatic]
        private static UOWBenchMark m_Payload = null;
        private static UOWBenchMark Payload
        {
            get
            {
                return m_Payload;
            }
            set
            {
                m_Payload = value;
            }
        }

        private static void Bench_TransparentRentedBuffers()
        {
            BenchWaitHandle.Reset();
            List<Thread> testThreads = new List<Thread>();
            for (int I = 0; I < 10; I++)
            {
                Thread T = new Thread(new ThreadStart(AddRemoveStuff));
                testThreads.Add(T);
                T.Start();
            }
            Stopwatch swTest = new Stopwatch();
            swTest.Start();
            BenchWaitHandle.Set();
            //while (swTest.ElapsedMilliseconds < 5000)
            //{
            //    Thread.Sleep(0);
            //}
            foreach (Thread t in testThreads)
            {
                t.Join();
            }
            swTest.Stop();
            Console.WriteLine(@"Tight Loop over RentedBuffer creation and assignment:");
            Console.WriteLine(@"Object with 18 array properties / fields cloned 1000000 times across 10 threads.");
            Console.WriteLine(@"In Total 18 X 1000000 = 18 Million repetitions. Time: {0}", swTest.Elapsed);

            RentedBuffer<char>.Shared.Rent(50).Return();
            Payload = Payload ?? new UOWBenchMark().RandomizeData(rnd, 128);

            RentedBuffer<char>.Shared.Rent(50).Return();

            if (Payload != null)
            {
                Payload.Dispose();
                Payload = null;
            }
        }
        #endregion
        private static void RentedBuffer_Produce_Consume(int numProducers, int numConsumers)
        {
            RentedBufferTest Tester;
            bool TestRunning = true;
            double RunSeconds;
            int stats_MessagesProduced = 0;
            int stats_MessagesConsumed = 0;
            int stats_DoneQueue = 0;
            int Warmup = 3;
            RunningAverage stats_MessagesProduced_Avg = new RunningAverage(100);
            Queue<int> stats_MessagesProducedList = new Queue<int>();
            numProducers = Math.Max(numProducers, 1);
            numConsumers = Math.Max(numConsumers, 1);
            Console.WriteLine(@"Producers: {0}, Consumers: {1}", numProducers, numConsumers);
            Console.WriteLine(@"Press X to quit this test.");
            Tester = new RentedBufferTest();
            Tester.Run(numProducers, numConsumers);
            Stopwatch sw = Stopwatch.StartNew();
            while (TestRunning)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.X)
                    {
                        TestRunning = false;
                        break;
                    }
                }
                RunSeconds = sw.Elapsed.TotalSeconds;
                if (RunSeconds > 1d)
                {
                    Tester.CalcStatsWindow(RunSeconds, out stats_MessagesProduced, out stats_MessagesConsumed, out stats_DoneQueue);
                    if (Warmup > 0)
                    {
                        Warmup -= 1;
                    }
                    else
                    {
                        stats_MessagesProduced_Avg.AddResult(stats_MessagesProduced);
                        Console.WriteLine(@"Produced: {0:000000}  -  Consumed: {1:000000}  -  Processed Queue: {2:000000}", stats_MessagesProduced_Avg.ComputeAverageAsInt(), stats_MessagesConsumed, stats_DoneQueue);
                    }
                    sw.Restart();
                }
            }
            Tester.ShutDown();
            Console.WriteLine(@"Exiting");
        }

        private static void MessageQueue_Produce_Process_Consume(int numProducers, int numConsumers)
        {
            ThreadControllerTest_Produce_Process_Consume Tester;
            bool TestRunning = true;
            double RunSeconds;
            int stats_MessagesProduced = 0;
            int stats_MessagesProcessed = 0;
            int stats_MessagesConsumed = 0;
            int stats_ThreadControllerQueue = 0;
            int stats_DoneQueue = 0;
            int Warmup = 3;
            RunningAverage stats_MessagesProduced_Avg = new RunningAverage(100);
            Queue<int> stats_MessagesProducedList = new Queue<int>();
            numProducers = Math.Max(numProducers, 1);
            numConsumers = Math.Max(numConsumers, 1);
            Console.WriteLine(@"Producers: {0}, Consumers: {1}", numProducers, numConsumers);
            Console.WriteLine(@"Press X to quit this test.");
            Tester = new ThreadControllerTest_Produce_Process_Consume();
            Tester.Run(numProducers, numConsumers);
            Stopwatch sw = Stopwatch.StartNew();
            while (TestRunning)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.X)
                    {
                        TestRunning = false;
                        break;
                    }
                }
                RunSeconds = sw.Elapsed.TotalSeconds;
                if (RunSeconds > 1d)
                {
                    Tester.CalcStatsWindow(RunSeconds, out stats_MessagesProduced, out stats_MessagesProcessed, out stats_MessagesConsumed, out stats_ThreadControllerQueue, out stats_DoneQueue);
                    if (Warmup > 0)
                    {
                        Warmup -= 1;
                    }
                    else
                    {
                        stats_MessagesProduced_Avg.AddResult(stats_MessagesProduced);
                        Console.WriteLine(@"Produced: {0:000000}  -  Processed: {1:000000}  -  Consumed: {2:000000}  -  ThreadController Queue: {3:000000}  -  Processed Queue: {4:000000}", stats_MessagesProduced_Avg.ComputeAverageAsInt(), stats_MessagesProcessed, stats_MessagesConsumed, stats_ThreadControllerQueue, stats_DoneQueue);
                    }
                    sw.Restart();
                }
            }
            Tester.ShutDown();
            Console.WriteLine(@"Exiting");
        }

        private static void ThreadControllerTest_FullyUnrolled(int numProducers, int numConsumers)
        {
            ThreadingTestFullyUnrolled Tester;
            bool TestRunning = true;
            double RunSeconds;
            int stats_MessagesProduced = 0;
            int stats_MessagesProcessed = 0;
            int stats_MessagesConsumed = 0;
            int QueueProducerSendWorkItemsSize;
            int QueueProducerSendByteDataSize;
            int QueueConsumerRecieveByteDataSize;
            int QueueConsumerRecieveWorkItemsBaseSize;
            int QueueThreadControllerSize = 0;
            int QueueProcessedSize = 0;
            RunningAverage stats_MessagesProduced_Avg = new RunningAverage(100);
            numProducers = Math.Max(numProducers, 1);
            numConsumers = Math.Max(numConsumers, 1);
            Console.WriteLine(@"Producers: {0}, Consumers: {1}", numProducers, numConsumers);
            Console.WriteLine(@"Press X to quit this test.");
            Tester = new ThreadingTestFullyUnrolled();
            Tester.Run(numProducers, numConsumers);
            Stopwatch sw = Stopwatch.StartNew();
            while (TestRunning)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.X)
                    {
                        TestRunning = false;
                        break;
                    }
                }
                RunSeconds = sw.Elapsed.TotalSeconds;
                if (RunSeconds > 1d)
                {
                    Tester.CalcStatsWindow(out stats_MessagesProduced, out stats_MessagesProcessed, out stats_MessagesConsumed,
                        out QueueProducerSendWorkItemsSize, out QueueProducerSendByteDataSize, 
                        out QueueConsumerRecieveByteDataSize, out QueueConsumerRecieveWorkItemsBaseSize,
                        out QueueThreadControllerSize, out QueueProcessedSize);
                    stats_MessagesProduced_Avg.AddResult(stats_MessagesProduced);
                    Console.WriteLine(@"Produced: {0:000000}  -  Consumed: {2:000000}  -  Processed: {1:000000}  -  Queues::- SendWorkItems: {3:000000}  -  SendByteData: {4:000000}  -  RecieveByteData: {5:000000}  -  RecieveWorkItems: {6:000000}  -  ThreadController: {7:000000}  -  Processed: {8:000000}",
                        stats_MessagesProduced_Avg.ComputeAverageAsInt(), stats_MessagesConsumed, stats_MessagesProcessed,
                        QueueProducerSendWorkItemsSize, QueueProducerSendByteDataSize,
                        QueueConsumerRecieveByteDataSize, QueueConsumerRecieveWorkItemsBaseSize,
                        QueueThreadControllerSize, QueueProcessedSize
                        );
                    sw.Restart();
                }
            }
            Tester.ShutDown();
            Console.WriteLine(@"Exiting");
        }



        private static void TestQueue()
        {
            QueueTest.TestBench_Queue();
        }

        private static Server.MQServer ServerA;
        private static Server.MQServer ServerB;
        private static TestService ServiceTest;

        private static void MQServerTest(bool benchMarkMode)
        {
            bool TestRunning = true;



            ServerA = new Server.MQServer(1, @"192.168.1.25:8000", @"192.168.1.25:9000", runBenchMark: benchMarkMode);
            ServerB = new Server.MQServer(2, @"192.168.1.25:9000", @"192.168.1.25:8000", runBenchMark: benchMarkMode);
            ServiceTest = new TestService();

            ServerA.RegisterLocalService(ServiceTest);

            //ServerA = new ChillXMQ.Server.TCPServerBase(@"127.0.0.1:2000", @"127.0.0.1:3000");
            //ServerB = new ChillXMQ.Server.TCPServerBase(@"127.0.0.1:3000", @"127.0.0.1:2000");
            ServerA.Start();
            ServerB.Start();
            Stopwatch SW;
            SW = new Stopwatch();
            SW.Start();
            Console.WriteLine(@"Press X to quit this test.");
            RunningAverage AvgAMessageCountIn = new RunningAverage(25);
            RunningAverage AvgAMessageCountOut = new RunningAverage(25);
            RunningAverage AvgASendWorkSize = new RunningAverage(25);
            RunningAverage AvgASendDataSize = new RunningAverage(25);
            RunningAverage AvgARecieveDataSize = new RunningAverage(25);
            RunningAverage AvgARecieveWorkSize = new RunningAverage(25);

            int requestID = 0;
            while (TestRunning)
            {
                WorkItemBase<TestUOW, TestUOW> workItem;
                WorkItemBaseCore workItemBase;
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.X)
                    {
                        TestRunning = false;
                        break;
                    }
                }
                if (SW.ElapsedMilliseconds > 1000)
                {
                    //Console.WriteLine(@"ServerA In: {0} Msgs {1:0.00} Mbps  -  ServerA Out: {2} Msgs {3:0.00} Mbps", ServerA.MessageCountIn, ServerA.BandwidthInMbps, ServerA.MessageCountOut, ServerA.BandwidthOutMbps);
                    //Console.WriteLine(@"ServerB In: {0} Msgs {1:0.00} Mbps  -  ServerB Out: {2} Msgs {3:0.00} Mbps", ServerB.MessageCountIn, ServerB.BandwidthInMbps, ServerB.MessageCountOut, ServerB.BandwidthOutMbps);
                    AvgAMessageCountIn.AddResult(ServerA.MessageCountIn);
                    AvgAMessageCountOut.AddResult(ServerA.MessageCountOut);
                    AvgASendWorkSize.AddResult(ServerA.SendWorkSize);
                    AvgASendDataSize.AddResult(ServerA.SendDataSize);
                    AvgARecieveDataSize.AddResult(ServerA.RecieveDataSize);
                    AvgARecieveWorkSize.AddResult(ServerA.RecieveWorkSize);
                    Console.WriteLine(@"ServerA In: {0:00000} Msgs {1:000.00} Mbps  -  ServerA Out: {2:00000} Msgs {3:000.00} Mbps : Send: {4:0000} / {5:0000} : Recv {6:0000} / {7:0000}  -  ServerB In: {8:00000} Msgs {9:000.00} Mbps  -  ServerB Out: {10:00000} Msgs {11:000.00} Mbps : Send: {12:0000} / {13:0000} : Recv {14:0000} / {15:0000}  -  ProcessingCount: {16:00000} / {17:00000}  -  TransitCount: {18:00000} / {19:00000}  -  Latency: {20}:{21:000} / {22}:{23:000}  -- TotalCount {24} / {25}",
                        AvgAMessageCountIn.ComputeAverageAsInt(), ServerA.BandwidthInMbps, AvgAMessageCountOut.ComputeAverageAsInt(), ServerA.BandwidthOutMbps, AvgASendWorkSize.ComputeAverageAsInt(), AvgASendDataSize.ComputeAverageAsInt(), AvgARecieveDataSize.ComputeAverageAsInt(), AvgARecieveWorkSize.ComputeAverageAsInt(),
                        ServerB.MessageCountIn, ServerB.BandwidthInMbps, ServerB.MessageCountOut, ServerB.BandwidthOutMbps, ServerB.SendWorkSize, ServerB.SendDataSize, ServerB.RecieveDataSize, ServerB.RecieveWorkSize,
                        ServerA.ProcessingSize, ServerB.ProcessingSize,
                        ServerA.BenchTransit, ServerB.BenchTransit, 
                        TimeSpan.FromTicks(ServerA.PingTimeTicks).ToString(), TimeSpan.FromTicks(ServerA.PingTimeTicks).Milliseconds,
                        TimeSpan.FromTicks(ServerB.PingTimeTicks).ToString(), TimeSpan.FromTicks(ServerB.PingTimeTicks).Milliseconds,
                        ServerA.BenchMarkMessageCount, ServerB.BenchMarkMessageCount);
                    SW.Restart();
                    if (requestID == 1)
                    {
                        workItem = new WorkItemBase<TestUOW, TestUOW>( (int)ServiceTypes.Test, (int)ModuleTypes.BenchMark, (int)TestFunctions.Benchmark,
                            (int)ServiceTypes.Test, (int)ModuleTypes.BenchMark, (int)TestFunctions.Benchmark);
                        workItem.RequestDetail.WorkItemData = new TestUOW();
                        workItem.RequestDetail.WorkItemData.PrimeTarget = rnd.Next(100, 1000);
                        //requestID = workItem.UniqueID;

                        //requestID = ServerB.TransmitRequest(workItem);
                        workItem = null;
                    }
                    else
                    {
                        if (ServerB.GetProcessedResponse(requestID, out workItem))
                        {
                            requestID = 0;
                        }
                    }
                }
            }
            ServerA.ShutDown();
            ServerB.ShutDown();
        }

        private static Random random = new Random();

        private static string RandomText(int length = 25)
        {
            // creating a StringBuilder object()
            StringBuilder sb = new StringBuilder();

            char letter;

            for (int i = 0; i < length; i++)
            {
                double flt = random.NextDouble();
                int shift = Convert.ToInt32(Math.Floor(25 * flt));
                if (random.NextDouble() > 0.5d)
                {
                    letter = Convert.ToChar(shift + 65);
                }
                else
                {
                    letter = Convert.ToChar(shift + 97);

                }
                sb.Append(letter);
            }
            return sb.ToString();
        }
    }

}
