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
 * *****************************************************************************************************************************************
 * Purpose of this benchmark test is to validate that Span<T> does not cause any extra allocations or GC overhead or performance penalties *
 * *****************************************************************************************************************************************
 * 
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1645 (21H2)
AMD Ryzen Threadripper 3970X, 1 CPU, 64 logical and 32 physical cores
.NET SDK=6.0.202
  [Host]   : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT  [AttachedDebugger]
  .NET 6.0 : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT

Job=.NET 6.0  Runtime=.NET 6.0

|                Method |     m_TestMode | numRepititions | numThreads |      Mean |    Error |   StdDev | Completed Work Items | Lock Contentions | Allocated |
|---------------------- |--------------- |--------------- |----------- |----------:|---------:|---------:|---------------------:|-----------------:|----------:|
| Bench_LockPerformance | ArrayByteIndex |      100000000 |          1 |  53.15 ms | 0.983 ms | 0.919 ms |                    - |                - |      48 B |
| Bench_LockPerformance | ArrayByteIndex |      100000000 |          4 |  13.40 ms | 0.083 ms | 0.074 ms |                    - |                - |       8 B |
| Bench_LockPerformance |  SpanByteIndex |      100000000 |          1 | 101.20 ms | 0.089 ms | 0.079 ms |                    - |                - |     725 B |
| Bench_LockPerformance |  SpanByteIndex |      100000000 |          4 |  25.30 ms | 0.288 ms | 0.240 ms |                    - |                - |      15 B |
| Bench_LockPerformance |  ArrayIntIndex |      100000000 |          1 |  77.34 ms | 0.948 ms | 0.886 ms |                    - |                - |      69 B |
| Bench_LockPerformance |  ArrayIntIndex |      100000000 |          4 |  19.17 ms | 0.209 ms | 0.175 ms |                    - |                - |      15 B |
| Bench_LockPerformance |   SpanIntIndex |      100000000 |          1 | 101.46 ms | 0.359 ms | 0.318 ms |                    - |                - |     250 B |
| Bench_LockPerformance |   SpanIntIndex |      100000000 |          4 |  25.34 ms | 0.503 ms | 0.445 ms |                    - |                - |      16 B |

 */

namespace ChillX.MQServer.Benchmark
{
    //[SimpleJob(RuntimeMoniker.NetCoreApp20), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.Net60), SimpleJob(RuntimeMoniker.Net50), SimpleJob(RuntimeMoniker.Net471), SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    //[ConcurrencyVisualizerProfiler]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class Bench_ArrayVsSpan : BenchBase
    {
        public enum TestMode
        {
            ArrayByteIndex = 0,
            SpanByteIndex = 1,
            ArrayIntIndex = 2,
            SpanIntIndex = 3,
        }

        [Params(TestMode.ArrayByteIndex, TestMode.SpanByteIndex, TestMode.ArrayIntIndex, TestMode.SpanIntIndex)]
        public TestMode m_TestMode = TestMode.ArrayByteIndex;

        private static Random rnd = new Random();
        [Params(100000000)]
        public int numRepititions;

        private int m_numThreads;
        [Params(1,4)]
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

        private byte[] m_BufferBytes = BufferBytesCreate();
        private static byte[] BufferBytesCreate()
        {
            byte[] buffer = new byte[1024];
            for (int I = 0; I < buffer.Length; I++)
            {
                buffer[I] = (byte)rnd.Next(byte.MinValue, byte.MaxValue);
            }
            return buffer;
        }

        public byte[] BufferByteArray { get { return m_BufferBytes; } }
        public Span<byte> BufferByteSpan { get { return m_BufferBytes.AsSpan(128,256); } }



        private int[] m_BufferInt = BufferIntCreate();
        private static int[] BufferIntCreate()
        {
            int[] buffer = new int[1024];
            for (int I = 0; I < buffer.Length; I++)
            {
                buffer[I] = (int)rnd.Next(int.MinValue, int.MaxValue);
            }
            return buffer;
        }

        public int[] BufferintArray { get { return m_BufferInt; } }
        public Span<int> BufferIntSpan { get { return m_BufferInt.AsSpan(128, 256); } }

        private int numReps = 1;

        protected override void OnGlobalSetup()
        {
            numReps = numRepititions / numThreads;
            m_BufferBytes = BufferBytesCreate();
            m_BufferInt = BufferIntCreate();
            Console.WriteLine(@"==============================================================================================");
            Console.WriteLine(@"Setup is run: Num Threads: {0}  -  numReps: {1} - Test Type: {2}", numThreads, numReps, Enum.GetName(typeof(TestMode), m_TestMode));
            Console.WriteLine(@"==============================================================================================");
        }

        [Benchmark]
        public void Bench_LockPerformance()
        {
            ThreadRunOneItteration();
        }

        protected override void OnGlobalCleanup()
        {
            Console.WriteLine(@"===================================================================================================");
            Console.WriteLine(@"Cleaning up.");
            Console.WriteLine(@"===================================================================================================");
        }

        protected override bool EnablePublisher
        {
            get { return true; }
        }

        private int FinalValue;
        protected override void Publish()
        {
            int counter = 0;
            long sum = 0;
            switch (m_TestMode)
            {
                case TestMode.ArrayByteIndex:
                    for (int I = 0; I < numReps; I++)
                    {
                        counter ++;
                        if (counter >= 250)
                        {
                            counter = 0;
                            sum = 0;
                        }
                        sum += BufferByteArray[counter];
                    }
                    break;
                case TestMode.SpanByteIndex:
                    for (int I = 0; I < numReps; I++)
                    {
                        counter++;
                        if (counter >= 250)
                        {
                            counter = 0;
                            sum = 0;
                        }
                        sum += BufferByteSpan[counter];
                    }
                    break;
                case TestMode.ArrayIntIndex:
                    for (int I = 0; I < numReps; I++)
                    {
                        counter++;
                        if (counter >= 250)
                        {
                            counter = 0;
                            sum = 0;
                        }
                        sum += BufferintArray[counter];
                    }
                    break;
                case TestMode.SpanIntIndex:
                    for (int I = 0; I < numReps; I++)
                    {
                        counter++;
                        if (counter >= 250)
                        {
                            counter = 0;
                            sum = 0;
                        }
                        sum += BufferIntSpan[counter];
                    }
                    break;
            }

        }
        protected override bool EnableSubscriber
        {
            get { return false; }
        }

        protected override bool SubscriberHasWork => false;

        protected override void Subscribe()
        {

        }
    }
}
