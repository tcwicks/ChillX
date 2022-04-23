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
    public class Bench_ChillXSerializePrimitives : BenchBase
    {
        [Params(500000)]
        public int numRepititions;

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


        private int m_stringSize;
        [Params(8192)]
        public int stringSize
        {
            get { return m_stringSize; }
            set
            {
                m_stringSize = value;
            }
        }

        protected override bool EnablePublisher => true;

        protected override bool EnableSubscriber => false;

        protected override bool SubscriberHasWork => false;

        private int numReps;
        private string TestString;
        private byte[] TestBuffer;
        private Enum_TestType TestType;

        private enum Enum_TestType
        {
            StringToByte = 0,
            ByteToString = 1,
        }

        protected override void OnGlobalSetup()
        {
            if (rnd == null)
            {
                rnd = new Random();
            }
            numReps = numRepititions / numThreads;
            TestString = RandomString(stringSize);
            TestBuffer = Encoding.UTF8.GetBytes(TestString);
            GC.Collect();
            Thread.Sleep(1);
        }


        [Benchmark]
        public void BenchSerialize_StringToByte()
        {
            TestType = Enum_TestType.StringToByte;
            ThreadRunOneItteration();
            while (NumThreadsRunning > 0)
            {
                Thread.Sleep(1);
            }
        }

        [Benchmark]
        public void BenchSerialize_ByteToString()
        {
            TestType = Enum_TestType.ByteToString;
            ThreadRunOneItteration();
            while (NumThreadsRunning > 0)
            {
                Thread.Sleep(1);
            }
        }

        protected override void OnGlobalCleanup()
        {
            GC.Collect();
            Thread.Sleep(1);
        }

        protected override void Publish()
        {
            switch (TestType)
            {
                case Enum_TestType.StringToByte:
                    Test_StringToByte();
                    break;
                case Enum_TestType.ByteToString:
                    Test_ByteToString();
                    break;
            }
        }
        private void Test_StringToByte()
        {
            byte[] buffer;
            string StringInstance;
            StringInstance = TestString;
            for (int I = 0; I < numReps; I++)
            {
                buffer = Encoding.UTF8.GetBytes(StringInstance);
            }
        }
        private void Test_ByteToString()
        {
            byte[] buffer;
            string Deserialized;
            buffer = new List<byte>(TestBuffer).ToArray();
            for (int I = 0; I < numReps; I++)
            {
                Deserialized = Encoding.UTF8.GetString(buffer);
            }
        }

        protected override void Subscribe()
        {
        }

        private static Random rnd;
        private static string RandomString(int stringSize)
        {
            System.Text.StringBuilder sb;
            sb = new System.Text.StringBuilder();
            for (int i = 0; i < stringSize; i++)
            {
                sb.Append((char)(byte)rnd.Next(32, 125));
            }
            return sb.ToString();
        }
    }

}
