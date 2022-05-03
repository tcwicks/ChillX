﻿/*
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
    public class Bench_ChillXSerializePrimitives : BenchBase
    {
        [Params(10000000)]
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


        private int m_stringSize;
        [Params(64)]
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
        [Params(Enum_TestType.StringToByte, Enum_TestType.ByteToString, Enum_TestType.StringToByteViaChar, Enum_TestType.ByteToStringViaChar)]
        public Enum_TestType TestType;

        public enum Enum_TestType
        {
            StringToByte = 0,
            ByteToString = 1,
            StringToByteViaChar = 2,
            ByteToStringViaChar = 3,
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
        public void BenchSerialize_String()
        {
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
                case Enum_TestType.StringToByteViaChar:
                    Test_StringToByteViaChar();
                    break;
                case Enum_TestType.ByteToStringViaChar:
                    Test_ByteToStringViaChar();
                    break;
            }
        }
        private void Test_StringToByte()
        {
            byte[] buffer;
            string StringInstance;
            int numBytes;
            StringInstance = TestString;
            numBytes = BitConverterExtended.GetByteCountUTF8String(StringInstance);
            buffer= new byte[numBytes];
            for (int I = 0; I < numReps; I++)
            {
                //buffer = Encoding.UTF8.GetBytes(StringInstance);
                numBytes = BitConverterExtended.GetByteCountUTF8String(StringInstance);
                BitConverterExtended.GetBytesUTF8String(StringInstance, buffer, 0);
            }
            buffer= new byte[numBytes];
        }

        private void Test_StringToByteViaChar()
        {
            byte[] buffer;
            char[] charArray = TestString.ToCharArray();
            int numBytes = charArray.Length * 2;
            buffer = new byte[numBytes];
            for (int I = 0; I < numReps; I++)
            {
                charArray = TestString.ToCharArray();
                numBytes = BitConverterExtended.GetBytes(charArray, buffer, 0);
            }
            buffer= new byte[numBytes];
        }
        private void Test_ByteToString()
        {
            byte[] buffer;
            string Deserialized = String.Empty;
            string StringInstance;
            int numBytes;
            StringInstance = TestString;
            numBytes = BitConverterExtended.GetByteCountUTF8String(StringInstance);
            buffer = new byte[numBytes];
            BitConverterExtended.GetBytesUTF8String(StringInstance, buffer, 0);
            for (int I = 0; I < numReps; I++)
            {
                //Deserialized = Encoding.UTF8.GetString(buffer);
                Deserialized = BitConverterExtended.ToString(buffer, 0, numBytes);
            }
            numBytes = Deserialized.Length;
        }

        private void Test_ByteToStringViaChar()
        {
            byte[] buffer;
            string Deserialized = String.Empty;
            char[] charArray;
            charArray = TestString.ToCharArray();
            int numBytes = charArray.Length * 2;
            buffer = new byte[numBytes];
            numBytes = BitConverterExtended.GetBytes(charArray, buffer, 0);
            for (int I = 0; I < numReps; I++)
            {
                charArray = BitConverterExtended.ToCharArray(buffer, 0, numBytes);
                Deserialized = new string(charArray);
            }
            numBytes = Deserialized.Length * 2;
            buffer = new byte[numBytes];
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
