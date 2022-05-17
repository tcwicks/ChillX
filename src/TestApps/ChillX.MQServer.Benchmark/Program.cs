//// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Running;
using ChillX.Core.Structures;
using ChillX.MQServer.Server.SystemMessage;
using ChillX.MQServer.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillX.MQServer.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //RentedBuffer<int> SourceData = RentedBuffer<int>.Shared.Rent(10);
            //for (int I = 0; I < SourceData.Length; I++)
            //{
            //    SourceData[I] = int.MaxValue - I;
            //}
            //RentedBuffer<byte> buffer = RentedBuffer<byte>.Shared.Rent(Serialization.BitConverterExtended.GetByteCount(SourceData));
            //Serialization.BitConverterExtended.GetBytes(SourceData, buffer.buffer, 0);
            //RentedBuffer<int> DestinationData = Serialization.BitConverterExtended.ToInt32RentedArray(buffer.buffer, 0, buffer.Length);

            //LockFreeRingBufferQueue<int> TestQueue = new LockFreeRingBufferQueue<int>(4);
            //for (int I = 0; I < 11; I++)
            //{
            //    TestQueue.Enqueue(I);
            //}
            //for (int I = 0; I < 11; I++)
            //{
            //    bool success;
            //    int result;
            //    result = TestQueue.DeQueue(out success);
            //    if (result != I)
            //    {

            //    }
            //}

            //TimeSpan RingBufferQueueTime;
            //Stopwatch sw;
            //sw = new Stopwatch();
            //Bench_Queue Bench = new Bench_Queue();
            //Bench.numThreads = 4;
            //Bench.numRepititions = 1000000;
            //Bench.m_TestMode = Bench_Queue.TestMode.RingBufferQueue;
            //for (int i = 0; i < 3; i++)
            //{
            //    Bench.GlobalSetup();
            //    sw.Start();
            //    //for (int j = 0; j < 1000; j++)
            //    //{
            //    //    Bench.Bench_QueuePerformance();
            //    //    Console.WriteLine(@"{0} - {1}", i, j);
            //    //}
            //    Bench.Bench_QueuePerformance();
            //    sw.Stop();
            //    Bench.GlobalCleanup();
            //}
            //RingBufferQueueTime = sw.Elapsed;
            //Console.WriteLine(@"RingBufferQueue: {0}", sw.Elapsed.ToString());
            //Bench.m_TestMode = Bench_Queue.TestMode.ThreadSafeQueue;
            //sw.Reset();
            //for (int i = 0; i < 3; i++)
            //{
            //    Bench.GlobalSetup();
            //    sw.Start();
            //    //for (int j = 0; j < 1000; j++)
            //    //{
            //    //    Bench.Bench_QueuePerformance();
            //    //    Console.WriteLine(@"{0} - {1}", i, j);
            //    //}
            //    Bench.Bench_QueuePerformance();
            //    sw.Stop();
            //    Bench.GlobalCleanup();
            //}
            //Console.WriteLine(@"RingBufferQueue: {0}", RingBufferQueueTime.ToString());
            //Console.WriteLine(@"ThreadSafeQueue: {0}", sw.Elapsed.ToString());

            bool Continue = true;
            try
            {
                while (Continue)
                {
                    ConsoleKeyInfo response;
                    int Choice = 0;
                    while (Choice < 1 || Choice > 6)
                    {
                        Console.WriteLine(@"1: For Default Profiler");
                        Console.WriteLine(@"2: For Memory & Threading Diagnoser Profiler");
                        Console.WriteLine(@"3: For ETW Profiler");
                        Console.WriteLine(@"4: For Concurrency Visualizer Profiler");
                        Console.WriteLine(@"5: For Inlining Diagnoser Profiler");
                        Console.WriteLine(@"6: For TailCall Diagnoser Profiler");
                        response = Console.ReadKey();
                        if (!int.TryParse(response.KeyChar.ToString(), out Choice))
                        {
                            Choice = 0;
                        }
                        Console.Clear();
                    }
                    IEnumerable<BenchmarkDotNet.Reports.Summary> summary;
                    switch (Choice)
                    {
                        case 1:
                            summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args,
                                DefaultConfig.Instance
                                 //.WithOptions(ConfigOptions.DisableOptimizationsValidator)
                                 );
                            break;
                        case 2:
                            summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args,
                                DefaultConfig.Instance
                                .AddDiagnoser(MemoryDiagnoser.Default)
                                .AddDiagnoser(ThreadingDiagnoser.Default)
                                 //.WithOptions(ConfigOptions.DisableOptimizationsValidator)
                                 );
                            break;
                        case 3:
                            summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args,
                                DefaultConfig.Instance
                                //.AddDiagnoser(MemoryDiagnoser.Default)
                                .AddDiagnoser(new EtwProfiler())
                                 );
                            break;
                        case 4:
                            summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args,
                                DefaultConfig.Instance
                                //.AddDiagnoser(MemoryDiagnoser.Default)
                                .AddDiagnoser(new ConcurrencyVisualizerProfiler())
                                 );
                            break;
                        case 5:
                            summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args,
                                DefaultConfig.Instance
                                .AddDiagnoser(MemoryDiagnoser.Default)
                                .AddDiagnoser(new BenchmarkDotNet.Diagnostics.Windows.InliningDiagnoser())
                                 );
                            break;
                        case 6:
                            summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args,
                                DefaultConfig.Instance
                                .AddDiagnoser(MemoryDiagnoser.Default)
                                .AddDiagnoser(new BenchmarkDotNet.Diagnostics.Windows.TailCallDiagnoser())
                                 );
                            break;
                       
                    }
                    
                    Console.WriteLine(@"");
                    Console.WriteLine(@"Press x to quit or any other key to repeat.");
                    response = Console.ReadKey();
                    if (response.Key == ConsoleKey.X)
                    {
                        Continue = false;
                    }
                    else
                    {
                        Console.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
