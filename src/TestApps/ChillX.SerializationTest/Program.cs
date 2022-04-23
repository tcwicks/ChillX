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
Notice: This test app uses Messagepack purely for performance comparison
 */

using ChillX.Core.Structures;
using System.Buffers;
using System.Diagnostics;
using System.Reflection;

namespace ChillX.Serialization.Test
{
    internal class Program
    {
        #region Micro Optimization
        private static int InterlockedValue = 0;
        private static ManualResetEvent InterlockedWaitHandle = new ManualResetEvent(false);
        private static void Inc()
        {
            InterlockedWaitHandle.WaitOne();
            for (int i = 0; i < 50000001; i++)
            {
                Interlocked.Increment(ref InterlockedValue);
            }
        }
        private static void Dec()
        {
            InterlockedWaitHandle.WaitOne();
            for (int i = 0; i < 50000000; i++)
            {
                Interlocked.Decrement(ref InterlockedValue);
            }
        }
        private static void Check()
        {
            int val;
            InterlockedWaitHandle.WaitOne();
            for (int i = 0; i < 50000000; i++)
            {
                val = InterlockedValue;
            }
        }
        private static object Lock = new object();
        private static ReaderWriterLockSlim RWLock = new ReaderWriterLockSlim();
        private static int LockedValue = 0;
        private static int Lock_Slim()
        {
            RWLock.EnterReadLock();
            try
            {
                return LockedValue;
            }
            finally
            {
                RWLock.ExitReadLock();
            }
        }
        private static void LockTest_Slim()
        {
            int Result = 0;
            InterlockedWaitHandle.WaitOne();
            for (int I = 0; I < 10000000; I++)
            {
                Result = Lock_Slim();
            }
            if (Result > 0)
            {
                Result = Lock_Slim();
            }
        }
        private static int Lock_Monitor()
        {
            lock (Lock)
            {
                return LockedValue;
            }
        }
        private static void LockTest_Monitor()
        {
            int Result = 0;
            InterlockedWaitHandle.WaitOne();
            for (int I = 0; I < 10000000; I++)
            {
                Result = Lock_Monitor();
            }
            if (Result > 0)
            {
                Result = Lock_Monitor();
            }
        }

        private static void TestInterlocked()
        {
            List<Thread> threads = new List<Thread>();
            InterlockedWaitHandle.Reset();
            for (int I = 0; I < 8; I++)
            {
                threads.Add(new Thread(new ThreadStart(Inc)));
            }
            for (int I = 0; I < 8; I++)
            {
                threads.Add(new Thread(new ThreadStart(Dec)));
            }
            for (int I = 0; I < 8; I++)
            {
                threads.Add(new Thread(new ThreadStart(Check)));
            }
            foreach (Thread thread in threads)
            {
                thread.Start();
            }
            InterlockedWaitHandle.Set();
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            Console.WriteLine(@"Interlocked Counter: {0}", InterlockedValue);
            Console.ReadLine();
        }

        private static void Test_Locks()
        {
            Console.WriteLine(@"Press Enter to continue.");
            Console.ReadLine();
            Stopwatch sw = new Stopwatch();
            List<Thread> threads = new List<Thread>();
            InterlockedWaitHandle.Reset();
            for (int I = 0; I < 8; I++)
            {
                threads.Add(new Thread(new ThreadStart(LockTest_Slim)));
            }
            LockedValue = 5;
            foreach (Thread thread in threads)
            {
                thread.Start();
            }
            sw.Start();
            InterlockedWaitHandle.Set();
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            sw.Stop();
            Console.WriteLine(@"LockTest_Slim: {0}", sw.Elapsed);

            threads.Clear();
            InterlockedWaitHandle.Reset();
            for (int I = 0; I < 8; I++)
            {
                threads.Add(new Thread(new ThreadStart(LockTest_Monitor)));
            }
            LockedValue = 5;
            foreach (Thread thread in threads)
            {
                thread.Start();
            }
            sw.Restart();
            InterlockedWaitHandle.Set();
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            sw.Stop();
            Console.WriteLine(@"LockTest_Monitor: {0}", sw.Elapsed);
            Console.ReadLine();
        }

        #endregion

        static void Main(string[] args)
        {
            //Test_Locks();
            //TestInterlocked();

            short[] blah;
            blah = new short[2] { 2, 5 };
            BitConverterExtended.GetBytes(blah, new byte[100], 10);

            TestIntegrity ValidateIntegrity = new TestIntegrity();
            ValidateIntegrity.Validate();

            EnableCalculateOverheads = true;
            EnableReadOnlyProcessNoQueue = true;
            EnableWriteOnlyProcessNoQueue = true;
            EnableReadOnlyProcessWithQueue = true;
            EnableWriteOnlyProcessWithQueue = true;
            EnableRoundTripProcessWithQueue = true;
            EnableRoundTripProcessIntegrity = true;

            //Console.WriteLine(@"Press Enter To Continue");
            //Console.ReadLine();
            Console.Clear();
            ChillXSerializerPerformanceTest(200000, 64, 1, 2, 4, 9);
            MessagePackPerformanceTest(200000, 64, 1, 2, 4, 9);

            Console.WriteLine(@"");

            //ChillXLightspeedBenchmark(10000000, 64, 1, 2, 4, false);
            //MessagePackBenchmark(10000000, 64, 1, 2, 4);


            //Console.WriteLine(@"Performance gain using Marshal and UnSafe pointer copy is not worth it is UTF16 which means double the data size on predominantly ASCII data sets.");
            //StringPerfTest(10000000, 255);

            //Console.WriteLine(@"-------------------------------------------------------------------------------------");
            //StringSerializePerfTest(10000000, 255);
            //Console.WriteLine(@"-------------------------------------------------------------------------------------");


            //BufferPerfTest(1000000, 16, 64);
            //BufferPerfTest(1000000, 64, 16);
        }

        private class TestIntegrity
        {
            private Random rnd = new Random();
            public void Validate()
            {
                Validate(true);
                Validate(false);
                Validate((char)(byte)rnd.Next(32, 125));
                Validate((short)rnd.Next(short.MinValue, short.MaxValue));
                Validate((int)rnd.Next(int.MinValue, int.MaxValue));
                Validate((long)rnd.Next(int.MinValue, int.MaxValue));
                Validate((UInt16)rnd.Next(1, UInt16.MaxValue));
                Validate((UInt32)rnd.Next(1, int.MaxValue));
                Validate((UInt64)rnd.Next(1, int.MaxValue));
                Validate(((Single)rnd.Next(1, int.MaxValue)) / ((Single)rnd.Next(1, int.MaxValue)));
                Validate(((Single)rnd.Next(1, int.MaxValue)) / ((Single)rnd.Next(1, int.MaxValue)));
                Validate((Single)Math.PI);
                Validate(((double)rnd.Next(1, int.MaxValue)) / ((double)rnd.Next(1, int.MaxValue)));
                Validate(((double)rnd.Next(1, int.MaxValue)) / ((double)rnd.Next(1, int.MaxValue)));
                Validate((double)Math.PI);
                Validate(((decimal)rnd.Next(1, int.MaxValue)) / ((decimal)rnd.Next(1, int.MaxValue)));
                Validate(((decimal)rnd.Next(1, int.MaxValue)) / ((decimal)rnd.Next(1, int.MaxValue)));
                Validate((decimal)Math.PI);
                Validate(TimeSpan.FromTicks(rnd.Next(int.MinValue, int.MaxValue)));
                Validate(TimeSpan.FromTicks(int.MaxValue));
                Validate(TimeSpan.FromTicks(long.MaxValue));
                Validate(TimeSpan.MinValue);
                Validate(TimeSpan.MaxValue);
                Validate(new DateTime(DateTime.UtcNow.Ticks + (rnd.Next(1, int.MaxValue)), DateTimeKind.Unspecified));
                Validate(new DateTime(DateTime.UtcNow.Ticks + (rnd.Next(1, int.MaxValue)), DateTimeKind.Utc));
                Validate(new DateTime(DateTime.UtcNow.Ticks + (rnd.Next(1, int.MaxValue)), DateTimeKind.Local));
                Validate(DateTime.MinValue.ToUniversalTime());
                Validate(DateTime.MinValue.ToUniversalTime().AddDays(1));
                Validate(DateTime.MaxValue);

                int ArraySize = 20;

                bool[] ArrayTest_bool = new bool[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_bool[I] = rnd.NextDouble() > 0.5d;
                }
                Validate(ArrayTest_bool);

                short[] ArrayTest_short = new short[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_short[I] = (short)rnd.Next(short.MinValue, short.MaxValue);
                }
                ArrayTest_short[0] = short.MinValue; ArrayTest_short[1] = short.MaxValue;
                Validate(ArrayTest_short);

                int[] ArrayTest_int = new int[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_int[I] = (int)rnd.Next(int.MinValue, int.MaxValue);
                }
                ArrayTest_int[0] = int.MinValue; ArrayTest_int[1] = int.MaxValue;
                Validate(ArrayTest_int);

                long[] ArrayTest_long = new long[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_long[I] = (long)rnd.Next(int.MinValue, int.MaxValue);
                }
                ArrayTest_long[0] = long.MinValue; ArrayTest_long[1] = long.MaxValue;
                Validate(ArrayTest_long);

                UInt16[] ArrayTest_UInt16 = new UInt16[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_UInt16[I] = (UInt16)rnd.Next(UInt16.MinValue, UInt16.MaxValue);
                }
                ArrayTest_UInt16[0] = UInt16.MinValue; ArrayTest_UInt16[1] = UInt16.MaxValue;
                Validate(ArrayTest_UInt16);

                UInt32[] ArrayTest_UInt32 = new UInt32[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_UInt32[I] = (UInt32)rnd.Next(1, int.MaxValue);
                }
                ArrayTest_UInt32[0] = UInt32.MinValue; ArrayTest_UInt32[1] = UInt32.MaxValue;
                Validate(ArrayTest_UInt32);

                UInt64[] ArrayTest_UInt64 = new UInt64[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_UInt64[I] = (UInt64)rnd.Next(1, int.MaxValue);
                }
                ArrayTest_UInt64[0] = UInt64.MinValue; ArrayTest_UInt64[1] = UInt64.MaxValue;
                Validate(ArrayTest_UInt64);

                Single[] ArrayTest_Single = new Single[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_Single[I] = (Single)rnd.Next(1, int.MaxValue);
                }
                ArrayTest_Single[0] = Single.MinValue; ArrayTest_Single[1] = Single.MaxValue; ArrayTest_Single[2] = (float)Math.PI;
                Validate(ArrayTest_Single);

                ArrayTest_Single = new Single[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_Single[I] = ((Single)rnd.Next(1, int.MaxValue)) / ((Single)rnd.Next(1, int.MaxValue));
                }
                Validate(ArrayTest_Single);

                double[] ArrayTest_double = new double[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_double[I] = (double)rnd.Next(1, int.MaxValue);
                }
                ArrayTest_double[0] = double.MinValue; ArrayTest_double[1] = double.MaxValue; ArrayTest_double[2] = Math.PI;
                Validate(ArrayTest_double);

                ArrayTest_double = new double[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_double[I] = ((double)rnd.Next(1, int.MaxValue)) / ((double)rnd.Next(1, int.MaxValue));
                }
                Validate(ArrayTest_double);

                decimal[] ArrayTest_decimal = new decimal[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_decimal[I] = (decimal)rnd.Next(1, int.MaxValue);
                }
                ArrayTest_decimal[0] = decimal.MinValue; ArrayTest_decimal[1] = decimal.MaxValue; ArrayTest_decimal[2] = (decimal)Math.PI;
                Validate(ArrayTest_decimal);

                ArrayTest_decimal = new decimal[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_decimal[I] = ((decimal)rnd.Next(1, int.MaxValue)) / ((decimal)rnd.Next(1, int.MaxValue));
                }
                Validate(ArrayTest_decimal);

                TimeSpan[] ArrayTest_TimeSpan = new TimeSpan[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_TimeSpan[I] = new TimeSpan(DateTime.Now.Ticks + rnd.Next(1, int.MaxValue));
                }
                ArrayTest_TimeSpan[0] = TimeSpan.MinValue; ArrayTest_TimeSpan[1] = TimeSpan.MaxValue;
                Validate(ArrayTest_TimeSpan);


                DateTime[] ArrayTest_DateTime = new DateTime[ArraySize];
                for (int I = 0; I < ArraySize; I++)
                {
                    ArrayTest_DateTime[I] = new DateTime(DateTime.Now.Ticks + rnd.Next(1, int.MaxValue), DateTimeKind.Utc);
                }
                ArrayTest_DateTime[0] = DateTime.MinValue.ToUniversalTime(); ArrayTest_DateTime[1] = DateTime.MaxValue.ToUniversalTime();
                Validate(ArrayTest_DateTime);
            }

            #region Non Array
            public void Validate(bool Value)
            {
                bool Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToBoolean(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on bool");
                }
            }
            public void Validate(char Value)
            {
                char Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToChar(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on char");
                }
            }
            public void Validate(short Value)
            {
                short Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToInt16(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on short");
                }
            }
            public void Validate(int Value)
            {
                int Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToInt32(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on int");
                }
            }
            public void Validate(long Value)
            {
                long Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToInt64(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on long");
                }
            }
            public void Validate(UInt16 Value)
            {
                UInt16 Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToUInt16(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on UInt16");
                }
            }
            public void Validate(UInt32 Value)
            {
                UInt32 Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToUInt32(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on UInt32");
                }
            }
            public void Validate(UInt64 Value)
            {
                UInt64 Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToUInt64(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on UInt64");
                }
            }
            public void Validate(Single Value)
            {
                Single Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToSingle(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on Single");
                }
            }
            public void Validate(double Value)
            {
                double Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToDouble(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on double");
                }
            }
            public void Validate(decimal Value)
            {
                decimal Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToDecimal(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on decimal");
                }
            }
            public void Validate(TimeSpan Value)
            {
                TimeSpan Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToTimeSpan(buffer, 5);
                if (Value != Result)
                {
                    throw new Exception(@"Validate failed on TimeSpan");
                }
            }
            public void Validate(DateTime Value)
            {
                DateTime Result;
                byte[] buffer = new byte[100];
                int NumBytes = BitConverterExtended.GetBytes(Value, buffer, 5);
                Result = BitConverterExtended.ToDateTime(buffer, 5);
                if (Value.ToUniversalTime() != Result)
                {
                    throw new Exception(@"Validate failed on DateTime");
                }
            }
            #endregion

            #region Array
            public void Validate(bool[] Array)
            {
                bool[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytes(Array, buffer, 5);
                Result = BitConverterExtended.ToBooleanArray(buffer, 5, NumBytes);
                if (!IsArrayEqual<bool>(Array, Result))
                {
                    throw new Exception(@"Validate failed on bool[]");
                }
            }
            public void Validate(short[] Array)
            {
                short[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytes(Array, buffer, 5);
                Result = BitConverterExtended.ToInt16Array(buffer, 5, NumBytes);
                if (!IsArrayEqual<short>(Array, Result))
                {
                    throw new Exception(@"Validate failed on short[]");
                }
            }
            public void Validate(int[] Array)
            {
                int[] Result;
                byte[] buffer;
                buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytes(Array, buffer, 5);
                Result = BitConverterExtended.ToInt32Array(buffer, 5, NumBytes);
                if (!IsArrayEqual<int>(Array, Result))
                {
                    throw new Exception(@"Validate failed on int[]");
                }
            }
            public void Validate(long[] Array)
            {
                long[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytes(Array, buffer, 5);
                Result = BitConverterExtended.ToInt64Array(buffer, 5, NumBytes);
                if (!IsArrayEqual<long>(Array, Result))
                {
                    throw new Exception(@"Validate failed on long[]");
                }
            }
            public void Validate(UInt16[] Array)
            {
                UInt16[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytesUShortArray(Array, buffer, 5);
                Result = BitConverterExtended.ToUInt16Array(buffer, 5, NumBytes);
                if (!IsArrayEqual<UInt16>(Array, Result))
                {
                    throw new Exception(@"Validate failed on UInt16[]");
                }
            }
            public void Validate(UInt32[] Array)
            {
                UInt32[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytesUIntArray(Array, buffer, 5);
                Result = BitConverterExtended.ToUInt32Array(buffer, 5, NumBytes);
                if (!IsArrayEqual<UInt32>(Array, Result))
                {
                    throw new Exception(@"Validate failed on UInt32[]");
                }
            }
            public void Validate(UInt64[] Array)
            {
                UInt64[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytesULongArray(Array, buffer, 5);
                Result = BitConverterExtended.ToUInt64Array(buffer, 5, NumBytes);
                if (!IsArrayEqual<UInt64>(Array, Result))
                {
                    throw new Exception(@"Validate failed on UInt64[]");
                }
            }
            public void Validate(Single[] Array)
            {
                Single[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytes(Array, buffer, 5);
                Result = BitConverterExtended.ToSingleArray(buffer, 5, NumBytes);
                if (!IsArrayEqual<Single>(Array, Result))
                {
                    throw new Exception(@"Validate failed on Single[]");
                }
            }
            public void Validate(double[] Array)
            {
                double[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytes(Array, buffer, 5);
                Result = BitConverterExtended.ToDoubleArray(buffer, 5, NumBytes);
                if (!IsArrayEqual<double>(Array, Result))
                {
                    throw new Exception(@"Validate failed on double[]");
                }
            }
            public void Validate(decimal[] Array)
            {
                decimal[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytes(Array, buffer, 5);
                Result = BitConverterExtended.ToDecimalArray(buffer, 5, NumBytes);
                if (!IsArrayEqual<decimal>(Array, Result))
                {
                    throw new Exception(@"Validate failed on decimal[]");
                }
            }
            public void Validate(TimeSpan[] Array)
            {
                TimeSpan[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytes(Array, buffer, 5);
                Result = BitConverterExtended.ToTimeSpanArray(buffer, 5, NumBytes);
                if (!IsArrayEqual<TimeSpan>(Array, Result))
                {
                    throw new Exception(@"Validate failed on TimeSpan[]");
                }
            }
            public void Validate(DateTime[] Array)
            {
                DateTime[] Result;
                byte[] buffer = new byte[Array.Length * 16 + 16];
                int NumBytes = BitConverterExtended.GetBytes(Array, buffer, 5);
                Result = BitConverterExtended.ToDateTimeArray(buffer, 5, NumBytes);
                if (!IsArrayEqual<DateTime>(Array, Result))
                {
                    throw new Exception(@"Validate failed on DateTime[]");
                }
            }

            #endregion

            public bool IsArrayEqual<T>(T[] x, T[] y)
                where T : struct
            {
                if (x.Length != y.Length) { return false; }
                for (int I = 0; I < x.Length; I++)
                {
                    if (!x[I].Equals(y[I])) { return false; }
                }
                return true;
            }

        }


        private static bool EnableCalculateOverheads = true;
        private static bool EnableReadOnlyProcessNoQueue = true;
        private static bool EnableWriteOnlyProcessNoQueue = true;
        private static bool EnableReadOnlyProcessWithQueue = true;
        private static bool EnableWriteOnlyProcessWithQueue = true;
        private static bool EnableRoundTripProcessWithQueue = true;
        private static bool EnableRoundTripProcessIntegrity = true;


        private static object SyncRoot = new object();
        private static volatile bool ThreadsIsRunning = false;
        private static void ThreadRun()
        {
            lock (SyncRoot)
            {
                ThreadsIsRunning = true;
            }
        }
        private static void ThreadExit()
        {
            lock (SyncRoot)
            {
                ThreadsIsRunning = false;
            }
        }
        private static ManualResetEvent BenchWaitHandle = new ManualResetEvent(false);

        private static ThreadSafeQueue<byte[]> Queue_Buffer = new ThreadSafeQueue<byte[]>();
        private static ThreadSafeQueue<RentedBuffer> Queue_RentedBuffer = new ThreadSafeQueue<RentedBuffer>();
        private static ThreadSafeQueue<ChillXEntity.TestClassVariantA> Queue_ChillX = new ThreadSafeQueue<ChillXEntity.TestClassVariantA>();
        private static ThreadSafeQueue<MessagePackEntity.TestClassVariantA> Queue_MsgPack = new ThreadSafeQueue<MessagePackEntity.TestClassVariantA>();
        private static TypedSerializer<ChillXEntity.TestClassVariantA> Serializer = TypedSerializer<ChillXEntity.TestClassVariantA>.Create();
        private static byte[] TestBuffer;
        private static int numRepititions;


        private static void ChillXLightspeedBench_ReadOverhead()
        {
            byte[] buffer;
            bool success;
            ChillXEntity.TestClassVariantA TestClassOne;
            BenchWaitHandle.WaitOne();
            while (ThreadsIsRunning)
            {
                TestClassOne = Queue_ChillX.DeQueue(out success);
                if (success)
                {
                    Queue_Buffer.Enqueue(TestBuffer);
                }
            }
        }

        private static void ChillXLightspeedBench_WriteOverhead()
        {
            byte[] buffer;
            bool success;
            int bytesConsumed = 1;
            ChillXEntity.TestClassVariantA TestClassTwo;
            TestClassTwo = new ChillXEntity.TestClassVariantA();
            BenchWaitHandle.WaitOne();
            while (ThreadsIsRunning) // Might miss a couple (< numthreads) but close enough
            {
                buffer = Queue_Buffer.DeQueue(out success);
                if (success)
                {
                    // Do Nothing;
                    bytesConsumed = buffer.Length;
                }
            }
            buffer = new byte[bytesConsumed];// Prevent compiler stripping out bytesConsumed = buffer.Length;
        }
        private static void ChillXLightspeedBench_ReadNoQueue()
        {
            byte[] buffer;
            ChillXEntity.TestClassVariantA TestClassOne;
            TypedSerializer<ChillXEntity.TestClassVariantA> SerializerLocal = TypedSerializer<ChillXEntity.TestClassVariantA>.Create();
            TestClassOne = Queue_ChillX.DeQueue();
            buffer = new byte[TestBuffer.Length];
            RentedBuffer bufferRented;
            BenchWaitHandle.WaitOne();
            for (int I = 0; I < numRepititions; I++)
            {
                bufferRented = SerializerLocal.ReadToRentedBuffer(TestClassOne);
                bufferRented.Dispose();
            }
        }
        private static void ChillXLightspeedBench_WriteNoQueue()
        {
            byte[] buffer;
            int bytesConsumed;
            ChillXEntity.TestClassVariantA TestClassTwo;
            TypedSerializer<ChillXEntity.TestClassVariantA> SerializerLocal = TypedSerializer<ChillXEntity.TestClassVariantA>.Create();
            TestClassTwo = new ChillXEntity.TestClassVariantA();
            buffer = new List<byte>(TestBuffer).ToArray();
            BenchWaitHandle.WaitOne();
            for (int I = 0; I < numRepititions; I++)
            {
                SerializerLocal.Write(TestClassTwo, buffer, out bytesConsumed);
            }
        }

        private static void ChillXLightspeedBench_Read()
        {
            RentedBuffer buffer;
            ChillXEntity.TestClassVariantA TestClassOne;
            //TypedSerializer<ChillXEntity.TestClassVariantA> SerializerLocal = TypedSerializer<ChillXEntity.TestClassVariantA>.Create();
            BenchWaitHandle.WaitOne();
            while (ThreadsIsRunning)
            {
                TestClassOne = Queue_ChillX.DeQueue();
                if (TestClassOne != null)
                {
                    buffer = Serializer.ReadToRentedBuffer(TestClassOne);
                    Queue_RentedBuffer.Enqueue(buffer);
                }
            }
        }
        private static void ChillXLightspeedBench_Write()
        {
            RentedBuffer buffer;
            int bytesConsumed;
            ChillXEntity.TestClassVariantA TestClassTwo;
            TestClassTwo = new ChillXEntity.TestClassVariantA();
            BenchWaitHandle.WaitOne();
            while (ThreadsIsRunning) // Might miss a couple (< numthreads) but close enough
            {
                //Queue_Buffer.WaitHandle.WaitOne();
                buffer = Queue_RentedBuffer.DeQueue();
                if (buffer != null)
                {
                    Serializer.Write(TestClassTwo, buffer.buffer, out bytesConsumed);
                    buffer.Dispose();
                }
            }
        }
        private static void ChillXLightspeedBench_Dispose()
        {
            RentedBuffer buffer;
            int bytesConsumed;
            ChillXEntity.TestClassVariantA TestClassTwo;
            TestClassTwo = new ChillXEntity.TestClassVariantA();
            BenchWaitHandle.WaitOne();
            while (ThreadsIsRunning) // Might miss a couple (< numthreads) but close enough
            {
                //Queue_Buffer.WaitHandle.WaitOne();
                buffer = Queue_RentedBuffer.DeQueue();
                if (buffer != null)
                {
                    buffer.Dispose();
                }
            }
        }

        private static volatile int IntegrityCount = 0;
        private static object SyncLock_Integrity = new object();
        private static void IntegrityCount_Increment()
        {
            lock (SyncLock_Integrity)
            {
                Interlocked.Increment(ref IntegrityCount);
            }
        }
        private static void IntegrityCount_Decrement()
        {
            lock (SyncLock_Integrity)
            {
                Interlocked.Decrement(ref IntegrityCount);
            }
        }
        private static int IntegrityCount_Get()
        {
            lock (SyncLock_Integrity)
            {
                return IntegrityCount;
            }
        }
        private static void ChillXLightspeedBench_ReadIntegrity()
        {
            RentedBuffer buffer;
            ChillXEntity.TestClassVariantA TestClassOne;
            //TypedSerializer<ChillXEntity.TestClassVariantA> SerializerLocal = TypedSerializer<ChillXEntity.TestClassVariantA>.Create();
            BenchWaitHandle.WaitOne();
            while (ThreadsIsRunning || Queue_ChillX.HasItems())
            {
                TestClassOne = Queue_ChillX.DeQueue();
                if (TestClassOne != null)
                {
                    buffer = Serializer.ReadToRentedBuffer(TestClassOne);
                    Queue_RentedBuffer.Enqueue(buffer);
                }
            }
        }

        private static void ChillXLightspeedBench_WriteIntegrity()
        {
            RentedBuffer buffer;
            int bytesConsumed;
            ChillXEntity.TestClassVariantA TestClassTwo;
            TestClassTwo = new ChillXEntity.TestClassVariantA();
            int Counter = 0;
            BenchWaitHandle.WaitOne();
            while (ThreadsIsRunning || Queue_RentedBuffer.HasItems())
            {
                //Queue_Buffer.WaitHandle.WaitOne();
                buffer = Queue_RentedBuffer.DeQueue();
                if (buffer != null)
                {
                    IntegrityCount_Decrement();
                    Serializer.Write(TestClassTwo, buffer.buffer, out bytesConsumed);
                    buffer.Dispose();
                    Counter--;
                }
            }
            //IntegrityCount_Adjust(Counter);
        }

        private static void ChillXSerializerPerformanceTest(int numReps, int stringSize, int numThreadsA, int numThreadsB, int numThreadsC, int numBestOf)
        {
            Console.WriteLine(@"");
            Console.WriteLine(@"----------------------------------------------------------------------------------");
            Console.WriteLine(@"Benchmarking LIghtSpeed Serializer: Test Object: Data class with 31 properties / fields of different types inlcuding multiple arrays of different types");
            Console.WriteLine(@"Num Reps: {0}  -  Array Size: {1}", numReps, stringSize);
            Console.WriteLine(@"----------------------------------------------------------------------------------");
            Random rnd = new Random();
            ChillXEntity.TestClassVariantA TestClassOne = new ChillXEntity.TestClassVariantA();
            ChillXEntity.TestClassVariantA TestClassTwo = new ChillXEntity.TestClassVariantA();

            TypedSerializer<ChillXEntity.TestClassVariantA> Serializer = TypedSerializer<ChillXEntity.TestClassVariantA>.Create();

            bool isEqual;
            isEqual = TestClassOne.Equals(TestClassTwo);

            TestClassOne.RandomizeData(rnd, stringSize);

            isEqual = TestClassOne.Equals(TestClassTwo);

            byte[] buffer;
            int bytesConsumed;
            buffer = Serializer.Read(TestClassOne);
            Serializer.Write(TestClassTwo, buffer, out bytesConsumed);
            isEqual = TestClassOne.EqualsDebug(TestClassTwo);
            if (!isEqual)
            {
                Console.WriteLine(@"ChillXLightning Serializer itegrity test failed. Deserialized entity does not match Original");
            }
            TestClassTwo = TestClassOne.Clone();
            isEqual = TestClassOne.EqualsDebug(TestClassTwo);
            if (!isEqual)
            {
                Console.WriteLine(@"ChillXLightning Serializer itegrity test failed. Deserialized entity does not match Original");
            }
            Stopwatch sw = Stopwatch.StartNew();
            long ElapsedTicks;
            double SerializeSeconds;
            double SerializeRatePerSecond;
            double SerializeMbps;

            if (false)
            {
                #region Single Threaded Version

                BenchWaitHandle.Reset();

                double DeSerializeSeconds;
                ElapsedTicks = long.MaxValue;
                for (int bestOf = 0; bestOf < 3; bestOf++)
                {
                    sw.Restart();
                    for (int I = 0; I < numReps; I++)
                    {
                        buffer = Serializer.Read(TestClassOne);
                    }
                    sw.Stop();
                    if (sw.ElapsedTicks < ElapsedTicks)
                    {
                        ElapsedTicks = sw.ElapsedTicks;
                    }
                }
                Console.WriteLine(@"  Serialize Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Time: {2}", buffer.Length, numReps, TimeSpan.FromTicks(ElapsedTicks).ToString());
                SerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;

                ElapsedTicks = long.MaxValue;
                for (int bestOf = 0; bestOf < 3; bestOf++)
                {
                    sw.Restart();
                    for (int I = 0; I < numReps; I++)
                    {
                        Serializer.Write(TestClassTwo, buffer, out bytesConsumed);
                    }
                    sw.Stop();
                    if (sw.ElapsedTicks < ElapsedTicks)
                    {
                        ElapsedTicks = sw.ElapsedTicks;
                    }
                }
                DeSerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds; ;
                Console.WriteLine(@"DeSerialize Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Time: {2}", buffer.Length, numReps, TimeSpan.FromTicks(ElapsedTicks).ToString());


                SerializeRatePerSecond = ((double)numReps) / SerializeSeconds;
                double DeSerializeRatePerSecond = ((double)numReps) / DeSerializeSeconds;
                SerializeMbps = (SerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));
                double DeSerializeMbps = (DeSerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));

                Console.WriteLine(@"Serializer Performance:");
                if (SerializeRatePerSecond > 1000000d)
                {
                    Console.WriteLine(@"  Serialize Entities Per Second: {0:00,000,000}  - Mbps: {1:0.00}", SerializeRatePerSecond, SerializeMbps);
                }
                else
                {
                    Console.WriteLine(@"     Serialize Entities Per Second: {0:000,000}  - Mbps: {1:0.00}", SerializeRatePerSecond, SerializeMbps);

                }
                if (DeSerializeRatePerSecond > 1000000d)
                {
                    Console.WriteLine(@"DeSerialize Entities Per Second: {0:00,000,000}  - Mbps: {1:0.00}", DeSerializeRatePerSecond, DeSerializeMbps);
                }
                else
                {
                    Console.WriteLine(@"   DeSerialize Entities Per Second: {0:000,000}  - Mbps: {1:0.00}", DeSerializeRatePerSecond, DeSerializeMbps);
                }
                #endregion
            }
            Console.WriteLine(@"----------------------------------------------------------------------------------");
            Console.WriteLine(@"Multi Threaded Tests:");
            Console.WriteLine(@"----------------------------------------------------------------------------------");

            List<Thread> runningTHreadList = new List<Thread>();
            List<long> ElapsedTicksList = new List<long>();

            TestClassOne = new ChillXEntity.TestClassVariantA();
            TestClassOne.RandomizeData(rnd, stringSize);
            TestBuffer = Serializer.Read(TestClassOne);
            if (EnableCalculateOverheads)
            {
                for (int I = 0; I < 10; I++)
                {
                    Queue_ChillX.Enqueue(TestClassOne);
                }

                buffer = Serializer.Read(TestClassOne);
                for (int I = 0; I < 10; I++)
                {
                    Queue_Buffer.Enqueue(buffer);
                }

                TestClassOne = new ChillXEntity.TestClassVariantA();
                TestClassOne.RandomizeData(rnd, stringSize);
                int QueuSize;
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    Queue_ChillX.Enqueue(TestClassOne);
                    Queue_ChillX.DeQueue();
                    Queue_Buffer.Enqueue(buffer);
                    Queue_Buffer.DeQueue();
                }
                sw.Stop();
                ElapsedTicks = sw.ElapsedTicks;
                Console.WriteLine(@"  Calculating Raw Overheads (WaitHandle): Count: {1:00,000,000}  -  Time: {2}", buffer.Length, numReps, TimeSpan.FromTicks(ElapsedTicks).ToString());
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    Queue_ChillX.Enqueue(TestClassOne);
                    Queue_ChillX.DeQueue();
                    Queue_Buffer.Enqueue(buffer);
                    Queue_Buffer.DeQueue();
                }
                sw.Stop();
                ElapsedTicks = sw.ElapsedTicks;
                Console.WriteLine(@"  Calculating Raw Overheads (QueueSize) : Count: {1:00,000,000}  -  Time: {2}", buffer.Length, numReps, TimeSpan.FromTicks(ElapsedTicks).ToString());

                buffer = Serializer.Read(TestClassOne);
                for (int I = 0; I < 3; I++)
                {
                    int numThreads = 1;
                    switch (I)
                    {
                        case 0: numThreads = numThreadsA; break;
                        case 1: numThreads = numThreadsB; break;
                        case 2: numThreads = numThreadsC; break;
                    }
                    ElapsedTicks = long.MaxValue;
                    ElapsedTicksList.Clear();
                    for (int bestOf = 0; bestOf < numBestOf; bestOf++)
                    {
                        GC.Collect();
                        Thread.Sleep(100);
                        Queue_ChillX.Clear();
                        Queue_Buffer.Clear();
                        BenchWaitHandle.Reset();
                        ThreadRun();
                        for (int N = 0; N < numReps; N++)
                        {
                            Queue_ChillX.Enqueue(TestClassOne);
                        }
                        for (int N = 0; N < numThreads; N++)
                        {
                            Thread T;
                            T = new Thread(new ThreadStart(ChillXLightspeedBench_ReadOverhead));
                            runningTHreadList.Add(T);
                            T.Start();
                            T = new Thread(new ThreadStart(ChillXLightspeedBench_WriteOverhead));
                            runningTHreadList.Add(T);
                            T.Start();
                        }
                        Thread.Sleep(500);
                        sw.Restart();
                        BenchWaitHandle.Set();
                        bool BenchRunning = true;
                        while (BenchRunning)
                        {
                            Thread.Sleep(1);
                            BenchRunning = Queue_ChillX.HasItems();
                        }
                        BenchRunning = true;
                        while (BenchRunning)
                        {
                            Thread.Sleep(1);
                            BenchRunning = Queue_Buffer.HasItems();
                        }
                        ThreadExit();
                        foreach (Thread T in runningTHreadList)
                        {
                            T.Join();
                        }
                        sw.Stop();
                        if (sw.ElapsedTicks < ElapsedTicks)
                        {
                            ElapsedTicks = sw.ElapsedTicks;
                        }
                        ElapsedTicksList.Add(sw.ElapsedTicks);
                        Thread.Sleep(100);
                    }
                    if (numBestOf >= 7)
                    {
                        ElapsedTicksList.Sort();
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicks = 0;
                        foreach (long Ticks in ElapsedTicksList)
                        {
                            ElapsedTicks += Ticks;
                        }
                        ElapsedTicks = ElapsedTicks / ElapsedTicksList.Count;
                    }
                    SerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;
                    SerializeRatePerSecond = ((double)numReps) / SerializeSeconds;
                    SerializeMbps = (SerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));
                    Console.WriteLine(@"Round Trip No Processing: Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Threads: {2:00}  -  Entities Per Second: {3:00,000,000}  -  Mbps: {4:0.00}  -  Time: {5}", buffer.Length, numReps, numThreads, SerializeRatePerSecond, SerializeMbps, TimeSpan.FromTicks(ElapsedTicks).ToString());
                }
            }

            Console.WriteLine(@"");

            buffer = Serializer.Read(TestClassOne);
            numRepititions = numReps;
            if (EnableReadOnlyProcessNoQueue)
            {
                for (int I = 0; I < 3; I++)
                {
                    int numThreads = 1;
                    switch (I)
                    {
                        case 0: numThreads = numThreadsA; break;
                        case 1: numThreads = numThreadsB; break;
                        case 2: numThreads = numThreadsC; break;
                    }
                    ElapsedTicks = long.MaxValue;
                    ElapsedTicksList.Clear();
                    for (int bestOf = 0; bestOf < numBestOf; bestOf++)
                    {
                        GC.Collect();
                        Thread.Sleep(100);
                        Queue_ChillX.Clear();
                        Queue_Buffer.Clear();
                        BenchWaitHandle.Reset();
                        ThreadRun();
                        for (int N = 0; N < 10; N++)
                        {
                            Queue_ChillX.Enqueue(TestClassOne);
                        }
                        for (int N = 0; N < numThreads; N++)
                        {
                            Thread T;
                            T = new Thread(new ThreadStart(ChillXLightspeedBench_ReadNoQueue));
                            runningTHreadList.Add(T);
                            T.Start();
                            //T = new Thread(new ThreadStart(ChillXLightspeedBench_WriteNoQueue));
                            //runningTHreadList.Add(T);
                            //T.Start();
                        }
                        Thread.Sleep(500);
                        sw.Restart();
                        BenchWaitHandle.Set();
                        bool BenchRunning = true;
                        foreach (Thread T in runningTHreadList)
                        {
                            T.Join();
                        }
                        sw.Stop();
                        if (sw.ElapsedTicks < ElapsedTicks)
                        {
                            ElapsedTicks = sw.ElapsedTicks;
                        }
                        ElapsedTicksList.Add(sw.ElapsedTicks);
                        ThreadExit();

                        Thread.Sleep(100);
                    }
                    if (numBestOf >= 7)
                    {
                        ElapsedTicksList.Sort();
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicks = 0;
                        foreach (long Ticks in ElapsedTicksList)
                        {
                            ElapsedTicks += Ticks;
                        }
                        ElapsedTicks = ElapsedTicks / ElapsedTicksList.Count;
                    }
                    SerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;
                    SerializeRatePerSecond = ((double)(numReps * numThreads)) / SerializeSeconds;
                    SerializeMbps = (SerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));
                    Console.WriteLine(@"Read Only Process No Q  : Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Threads: {2:00}  -  Entities Per Second: {3:00,000,000}  -  Mbps: {4:0.00}  -  Time: {5}", buffer.Length, numReps, numThreads, SerializeRatePerSecond, SerializeMbps, TimeSpan.FromTicks(ElapsedTicks).ToString());
                }
                Console.WriteLine(@"");
            }


            buffer = Serializer.Read(TestClassOne);
            numRepititions = numReps;
            if (EnableWriteOnlyProcessNoQueue)
            {
                for (int I = 0; I < 3; I++)
                {
                    int numThreads = 1;
                    switch (I)
                    {
                        case 0: numThreads = numThreadsA; break;
                        case 1: numThreads = numThreadsB; break;
                        case 2: numThreads = numThreadsC; break;
                    }
                    ElapsedTicks = long.MaxValue;
                    ElapsedTicksList.Clear();
                    for (int bestOf = 0; bestOf < numBestOf; bestOf++)
                    {
                        GC.Collect();
                        Thread.Sleep(100);
                        Queue_ChillX.Clear();
                        Queue_Buffer.Clear();
                        BenchWaitHandle.Reset();
                        ThreadRun();
                        for (int N = 0; N < 10; N++)
                        {
                            Queue_ChillX.Enqueue(TestClassOne);
                        }
                        for (int N = 0; N < numThreads; N++)
                        {
                            Thread T;
                            //T = new Thread(new ThreadStart(ChillXLightspeedBench_ReadNoQueue));
                            //runningTHreadList.Add(T);
                            //T.Start();
                            T = new Thread(new ThreadStart(ChillXLightspeedBench_WriteNoQueue));
                            runningTHreadList.Add(T);
                            T.Start();
                        }
                        Thread.Sleep(500);
                        sw.Restart();
                        BenchWaitHandle.Set();
                        bool BenchRunning = true;
                        foreach (Thread T in runningTHreadList)
                        {
                            T.Join();
                        }
                        sw.Stop();
                        if (sw.ElapsedTicks < ElapsedTicks)
                        {
                            ElapsedTicks = sw.ElapsedTicks;
                        }
                        ElapsedTicksList.Add(sw.ElapsedTicks);
                        ThreadExit();

                        Thread.Sleep(100);
                    }
                    if (numBestOf >= 7)
                    {
                        ElapsedTicksList.Sort();
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicks = 0;
                        foreach (long Ticks in ElapsedTicksList)
                        {
                            ElapsedTicks += Ticks;
                        }
                        ElapsedTicks = ElapsedTicks / ElapsedTicksList.Count;
                    }
                    SerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;
                    SerializeRatePerSecond = ((double)(numReps * numThreads)) / SerializeSeconds;
                    SerializeMbps = (SerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));
                    Console.WriteLine(@"Write Only Process No Q : Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Threads: {2:00}  -  Entities Per Second: {3:00,000,000}  -  Mbps: {4:0.00}  -  Time: {5}", buffer.Length, numReps, numThreads, SerializeRatePerSecond, SerializeMbps, TimeSpan.FromTicks(ElapsedTicks).ToString());
                }
                Console.WriteLine(@"");
            }


            buffer = Serializer.Read(TestClassOne);
            if (EnableReadOnlyProcessWithQueue)
            {
                for (int I = 0; I < 3; I++)
                {
                    int numThreads = 1;
                    switch (I)
                    {
                        case 0: numThreads = numThreadsA; break;
                        case 1: numThreads = numThreadsB; break;
                        case 2: numThreads = numThreadsC; break;
                    }
                    ElapsedTicks = long.MaxValue;
                    ElapsedTicksList.Clear();
                    for (int bestOf = 0; bestOf < numBestOf; bestOf++)
                    {
                        GC.Collect();
                        Thread.Sleep(100);
                        Queue_ChillX.Clear();
                        Queue_Buffer.Clear();
                        BenchWaitHandle.Reset();
                        ThreadRun();
                        for (int N = 0; N < numReps; N++)
                        {
                            Queue_ChillX.Enqueue(TestClassOne);
                        }
                        for (int N = 0; N < numThreads; N++)
                        {
                            Thread T;
                            T = new Thread(new ThreadStart(ChillXLightspeedBench_Read));
                            runningTHreadList.Add(T);
                            T.Start();
                            T = new Thread(new ThreadStart(ChillXLightspeedBench_Dispose));
                            runningTHreadList.Add(T);
                            T.Start();
                        }
                        GC.Collect();
                        Thread.Sleep(500);
                        sw.Restart();
                        BenchWaitHandle.Set();
                        bool BenchRunning = true;
                        while (BenchRunning)
                        {
                            Thread.Sleep(1);
                            BenchRunning = Queue_ChillX.HasItems();
                        }
                        ElapsedTicksList.Add(sw.ElapsedTicks);
                        ThreadExit();
                        foreach (Thread T in runningTHreadList)
                        {
                            T.Join();
                        }
                        sw.Stop();
                        if (sw.ElapsedTicks < ElapsedTicks)
                        {
                            ElapsedTicks = sw.ElapsedTicks;
                        }
                        Thread.Sleep(100);
                    }
                    if (numBestOf >= 7)
                    {
                        ElapsedTicksList.Sort();
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicks = 0;
                        foreach (long Ticks in ElapsedTicksList)
                        {
                            ElapsedTicks += Ticks;
                        }
                        ElapsedTicks = ElapsedTicks / ElapsedTicksList.Count;
                    }
                    SerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;
                    SerializeRatePerSecond = ((double)numReps) / SerializeSeconds;
                    SerializeMbps = (SerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));
                    Console.WriteLine(@"Read Only Process Queue : Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Threads: {2:00}  -  Entities Per Second: {3:00,000,000}  -  Mbps: {4:0.00}  -  Time: {5}", buffer.Length, numReps, numThreads, SerializeRatePerSecond, SerializeMbps, TimeSpan.FromTicks(ElapsedTicks).ToString());
                }
                Console.WriteLine(@"");
            }

            buffer = Serializer.Read(TestClassOne);
            if (EnableWriteOnlyProcessWithQueue)
            {
                for (int I = 0; I < 3; I++)
                {
                    int numThreads = 1;
                    switch (I)
                    {
                        case 0: numThreads = numThreadsA; break;
                        case 1: numThreads = numThreadsB; break;
                        case 2: numThreads = numThreadsC; break;
                    }
                    ElapsedTicks = long.MaxValue;
                    ElapsedTicksList.Clear();
                    for (int bestOf = 0; bestOf < numBestOf; bestOf++)
                    {
                        GC.Collect();
                        Thread.Sleep(100);
                        Queue_ChillX.Clear();
                        Queue_Buffer.Clear();
                        BenchWaitHandle.Reset();
                        ThreadRun();
                        for (int N = 0; N < numReps; N++)
                        {
                            Queue_RentedBuffer.Enqueue(Serializer.ReadToRentedBuffer(TestClassOne));
                        }
                        for (int N = 0; N < numThreads; N++)
                        {
                            Thread T;
                            //T = new Thread(new ThreadStart(ChillXLightspeedBench_Read));
                            //runningTHreadList.Add(T);
                            //T.Start();
                            T = new Thread(new ThreadStart(ChillXLightspeedBench_Write));
                            runningTHreadList.Add(T);
                            T.Start();
                        }
                        GC.Collect();
                        Thread.Sleep(500);
                        sw.Restart();
                        BenchWaitHandle.Set();
                        bool BenchRunning = true;
                        while (BenchRunning)
                        {
                            Thread.Sleep(1);
                            BenchRunning = Queue_RentedBuffer.HasItems();
                        }
                        ThreadExit();
                        foreach (Thread T in runningTHreadList)
                        {
                            T.Join();
                        }
                        sw.Stop();
                        if (sw.ElapsedTicks < ElapsedTicks)
                        {
                            ElapsedTicks = sw.ElapsedTicks;
                        }
                        ElapsedTicksList.Add(sw.ElapsedTicks);
                        Thread.Sleep(100);
                    }
                    if (numBestOf >= 7)
                    {
                        ElapsedTicksList.Sort();
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicks = 0;
                        foreach (long Ticks in ElapsedTicksList)
                        {
                            ElapsedTicks += Ticks;
                        }
                        ElapsedTicks = ElapsedTicks / ElapsedTicksList.Count;
                    }
                    SerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;
                    SerializeRatePerSecond = ((double)numReps) / SerializeSeconds;
                    SerializeMbps = (SerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));
                    Console.WriteLine(@"Write Only Process Queue: Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Threads: {2:00}  -  Entities Per Second: {3:00,000,000}  -  Mbps: {4:0.00}  -  Time: {5}", buffer.Length, numReps, numThreads, SerializeRatePerSecond, SerializeMbps, TimeSpan.FromTicks(ElapsedTicks).ToString());
                }
                Console.WriteLine(@"");
            }

            buffer = Serializer.Read(TestClassOne);
            if (EnableRoundTripProcessWithQueue)
            {
                for (int I = 0; I < 3; I++)
                {
                    int numThreads = 1;
                    switch (I)
                    {
                        case 0: numThreads = numThreadsA; break;
                        case 1: numThreads = numThreadsB; break;
                        case 2: numThreads = numThreadsC; break;
                    }
                    ElapsedTicks = long.MaxValue;
                    ElapsedTicksList.Clear();
                    for (int bestOf = 0; bestOf < numBestOf; bestOf++)
                    {
                        GC.Collect();
                        Thread.Sleep(100);
                        Queue_ChillX.Clear();
                        Queue_Buffer.Clear();
                        BenchWaitHandle.Reset();
                        ThreadRun();
                        for (int N = 0; N < numReps; N++)
                        {
                            Queue_ChillX.Enqueue(TestClassOne.Clone());
                        }
                        for (int N = 0; N < numThreads; N++)
                        {
                            Thread T;
                            T = new Thread(new ThreadStart(ChillXLightspeedBench_Read));
                            runningTHreadList.Add(T);
                            T.Start();
                            T = new Thread(new ThreadStart(ChillXLightspeedBench_Write));
                            runningTHreadList.Add(T);
                            T.Start();
                        }
                        GC.Collect();
                        Thread.Sleep(500);
                        sw.Restart();
                        BenchWaitHandle.Set();
                        bool BenchRunning = true;
                        while (BenchRunning)
                        {
                            Thread.Sleep(1);
                            BenchRunning = Queue_ChillX.HasItems();
                        }
                        BenchRunning = true;
                        while (BenchRunning)
                        {
                            Thread.Sleep(1);
                            BenchRunning = Queue_Buffer.HasItems();
                        }
                        ThreadExit();
                        foreach (Thread T in runningTHreadList)
                        {
                            T.Join();
                        }
                        sw.Stop();
                        if (sw.ElapsedTicks < ElapsedTicks)
                        {
                            ElapsedTicks = sw.ElapsedTicks;
                        }
                        ElapsedTicksList.Add(sw.ElapsedTicks);
                        Thread.Sleep(100);
                    }
                    if (numBestOf >= 7)
                    {
                        ElapsedTicksList.Sort();
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(0);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                        ElapsedTicks = 0;
                        foreach (long Ticks in ElapsedTicksList)
                        {
                            ElapsedTicks += Ticks;
                        }
                        ElapsedTicks = ElapsedTicks / ElapsedTicksList.Count;
                    }
                    SerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;
                    SerializeRatePerSecond = ((double)numReps) / SerializeSeconds;
                    SerializeMbps = (SerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));
                    Console.WriteLine(@"Round Trip Processing Q : Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Threads: {2:00}  -  Entities Per Second: {3:00,000,000}  -  Mbps: {4:0.00}  -  Time: {5}", buffer.Length, numReps, numThreads, SerializeRatePerSecond, SerializeMbps, TimeSpan.FromTicks(ElapsedTicks).ToString());
                }
            }

            List<Thread> publisherThreadList = new List<Thread>();
            List<Thread> subscriberThreadList = new List<Thread>();
            buffer = Serializer.Read(TestClassOne);
            if (EnableRoundTripProcessIntegrity)
            {
                bool BenchRunning = true;
                int numThreads = numThreadsC;
                ElapsedTicks = long.MaxValue;
                ElapsedTicksList.Clear();

                GC.Collect();
                Thread.Sleep(100);
                Queue_ChillX.Clear();
                Queue_Buffer.Clear();
                BenchWaitHandle.Reset();
                ThreadRun();
                for (int N = 0; N < numReps; N++)
                {
                    Queue_ChillX.Enqueue(TestClassOne.Clone());
                    IntegrityCount_Increment();
                }
                //IntegrityCount_Adjust(numReps);

                for (int N = 0; N < numThreads; N++)
                {
                    Thread T;
                    T = new Thread(new ThreadStart(ChillXLightspeedBench_ReadIntegrity));
                    publisherThreadList.Add(T);
                    T.Start();
                    T = new Thread(new ThreadStart(ChillXLightspeedBench_WriteIntegrity));
                    subscriberThreadList.Add(T);
                    T.Start();
                }
                GC.Collect();
                Thread.Sleep(500);
                sw.Restart();
                BenchWaitHandle.Set();
                BenchRunning = true;
                for (int I = 0; I < numBestOf; I++)
                {
                    for (int N = 0; N < numReps; N++)
                    {
                        Queue_ChillX.Enqueue(TestClassOne.Clone());
                        IntegrityCount_Increment();
                    }
                    //Console.WriteLine(@"Inflight {0}", IntegrityCount_Get());
                    //BenchRunning = true;
                    //while (BenchRunning)
                    //{
                    //    Thread.Sleep(1);
                    //    BenchRunning = Queue_ChillX.HasItems();
                    //}
                    //BenchRunning = true;
                    //while (BenchRunning)
                    //{
                    //    Thread.Sleep(1);
                    //    BenchRunning = Queue_Buffer.HasItems();
                    //}
                    //Console.WriteLine(@"Integrity {0}", IntegrityCount_Get());
                }
                BenchRunning = true;
                while (BenchRunning)
                {
                    Thread.Sleep(1);
                    BenchRunning = Queue_ChillX.HasItems();
                }
                for (int N = 0; N < numReps; N++)
                {
                    Queue_ChillX.Enqueue(TestClassOne.Clone());
                    IntegrityCount_Increment();
                }
                //IntegrityCount_Adjust(numReps);
                //Console.WriteLine(@"Inflight {0}", IntegrityCount_Get());

                BenchRunning = true;
                while (BenchRunning)
                {
                    Thread.Sleep(1);
                    BenchRunning = Queue_ChillX.HasItems();
                }
                BenchRunning = true;
                while (BenchRunning)
                {
                    Thread.Sleep(1);
                    BenchRunning = Queue_Buffer.HasItems();
                }
                //Console.WriteLine(@"Integrity {0}", IntegrityCount_Get());
                ThreadExit();
                foreach (Thread T in publisherThreadList)
                {
                    T.Join();
                }
                foreach (Thread T in subscriberThreadList)
                {
                    T.Join();
                }
                sw.Stop();
                ElapsedTicks = sw.ElapsedTicks;

                ElapsedTicksList.Add(sw.ElapsedTicks);
                Thread.Sleep(100);

                SerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;
                SerializeRatePerSecond = ((double)(numReps * (numBestOf + 2))) / SerializeSeconds;
                SerializeMbps = (SerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));
                Console.WriteLine(@"Round Trip integrity W  : Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Threads: {2:00}  -  Entities Per Second: {3:00,000,000}  -  Mbps: {4:0.00}  -  Integrity: {5}  -  Time: {6}", buffer.Length, numReps, numThreads, SerializeRatePerSecond, SerializeMbps, IntegrityCount_Get(), TimeSpan.FromTicks(ElapsedTicks).ToString());
            }
        }

        private static void MessagePackBenchmark_Read()
        {
            byte[] buffer;
            MessagePackEntity.TestClassVariantA TestClassOne;
            BenchWaitHandle.WaitOne();
            while (ThreadsIsRunning)
            {
                TestClassOne = Queue_MsgPack.DeQueue();
                if (TestClassOne != null)
                {
                    buffer = MessagePack.MessagePackSerializer.Serialize<MessagePackEntity.TestClassVariantA>(TestClassOne);
                    //Queue_Buffer.Enqueue(buffer);
                }
            }
        }
        private static void MessagePackBenchmark_Write()
        {
            byte[] buffer;
            int bytesConsumed;
            MessagePackEntity.TestClassVariantA TestClassTwo;
            BenchWaitHandle.WaitOne();
            while (ThreadsIsRunning) // Might miss a couple (< numthreads) but close enough
            {
                //Queue_Buffer.WaitHandle.WaitOne();
                buffer = Queue_Buffer.DeQueue();
                if (buffer != null)
                {
                    TestClassTwo = MessagePack.MessagePackSerializer.Deserialize<MessagePackEntity.TestClassVariantA>(buffer);
                }
            }
        }

        private static void MessagePackPerformanceTest(int numReps, int stringSize, int numThreadsA, int numThreadsB, int numThreadsC, int numBestOf)
        {
            Console.WriteLine(@"");
            Console.WriteLine(@"----------------------------------------------------------------------------------");
            Console.WriteLine(@"Benchmarking LIghtSpeed Serializer: Test Object: Data class with 31 properties / fields of different types inlcuding multiple arrays of different types");
            Console.WriteLine(@"Num Reps: {0}  -  Array Size: {1}", numReps, stringSize);
            Console.WriteLine(@"----------------------------------------------------------------------------------");
            Random rnd = new Random();
            MessagePackEntity.TestClassVariantA TestClassOne = new MessagePackEntity.TestClassVariantA();
            MessagePackEntity.TestClassVariantA TestClassTwo = new MessagePackEntity.TestClassVariantA();

            bool isEqual;
            isEqual = TestClassOne.Equals(TestClassTwo);

            TestClassOne.RandomizeData(rnd, stringSize);

            isEqual = TestClassOne.Equals(TestClassTwo);

            byte[] buffer;
            buffer = MessagePack.MessagePackSerializer.Serialize<MessagePackEntity.TestClassVariantA>(TestClassOne);
            TestClassTwo = MessagePack.MessagePackSerializer.Deserialize<MessagePackEntity.TestClassVariantA>(buffer);
            isEqual = TestClassOne.EqualsDebug(TestClassTwo);
            if (!isEqual)
            {
                Console.WriteLine(@"Messagepack Serializer itegrity test failed. Deserialized entity does not match Original");
            }
            TestClassTwo = TestClassOne.Clone();
            isEqual = TestClassOne.EqualsDebug(TestClassTwo);
            if (!isEqual)
            {
                Console.WriteLine(@"Messagepack Serializer itegrity test failed. Deserialized entity does not match Original");
            }


            Stopwatch sw = Stopwatch.StartNew();
            double DeSerializeSeconds;
            long ElapsedTicks;
            List<long> ElapsedTicksList = new List<long>();
            double SerializeSeconds;
            double SerializeRatePerSecond;
            double SerializeMbps;

            if (false)
            {
                #region Single Threaded Version
                ElapsedTicks = long.MaxValue;
                for (int bestOf = 0; bestOf < 3; bestOf++)
                {
                    sw.Restart();
                    for (int I = 0; I < numReps; I++)
                    {
                        buffer = MessagePack.MessagePackSerializer.Serialize<MessagePackEntity.TestClassVariantA>(TestClassOne);
                    }
                    sw.Stop();
                    if (sw.ElapsedTicks < ElapsedTicks)
                    {
                        ElapsedTicks = sw.ElapsedTicks;
                    }
                }
                Console.WriteLine(@"  Serialize Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Time: {2}", buffer.Length, numReps, TimeSpan.FromTicks(ElapsedTicks).ToString());
                SerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;

                ElapsedTicks = long.MaxValue;
                for (int bestOf = 0; bestOf < 3; bestOf++)
                {
                    sw.Restart();
                    for (int I = 0; I < numReps; I++)
                    {
                        TestClassTwo = MessagePack.MessagePackSerializer.Deserialize<MessagePackEntity.TestClassVariantA>(buffer);
                    }
                    sw.Stop();
                    if (sw.ElapsedTicks < ElapsedTicks)
                    {
                        ElapsedTicks = sw.ElapsedTicks;
                    }
                }
                DeSerializeSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds; ;
                Console.WriteLine(@"DeSerialize Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Time: {2}", buffer.Length, numReps, TimeSpan.FromTicks(ElapsedTicks).ToString());


                SerializeRatePerSecond = ((double)numReps) / SerializeSeconds;
                double DeSerializeRatePerSecond = ((double)numReps) / DeSerializeSeconds;
                SerializeMbps = (SerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));
                double DeSerializeMbps = (DeSerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));

                Console.WriteLine(@"Serializer Performance:");
                if (SerializeRatePerSecond > 1000000d)
                {
                    Console.WriteLine(@"  Serialize Entities Per Second: {0:00,000,000}  - Mbps: {1:0.00}", SerializeRatePerSecond, SerializeMbps);
                }
                else
                {
                    Console.WriteLine(@"     Serialize Entities Per Second: {0:000,000}  - Mbps: {1:0.00}", SerializeRatePerSecond, SerializeMbps);

                }
                if (DeSerializeRatePerSecond > 1000000d)
                {
                    Console.WriteLine(@"DeSerialize Entities Per Second: {0:00,000,000}  - Mbps: {1:0.00}", DeSerializeRatePerSecond, DeSerializeMbps);
                }
                else
                {
                    Console.WriteLine(@"   DeSerialize Entities Per Second: {0:000,000}  - Mbps: {1:0.00}", DeSerializeRatePerSecond, DeSerializeMbps);
                }
                Console.WriteLine(@"----------------------------------------------------------------------------------");
                #endregion
            }



            Console.WriteLine(@"----------------------------------------------------------------------------------");
            Console.WriteLine(@"Multi Threaded Version:");
            Console.WriteLine(@"----------------------------------------------------------------------------------");

            List<Thread> runningTHreadList = new List<Thread>();

            TestClassOne = new MessagePackEntity.TestClassVariantA();
            TestClassOne.RandomizeData(rnd, stringSize);
            for (int I = 0; I < 10; I++)
            {
                Queue_MsgPack.Enqueue(TestClassOne);
            }

            TestBuffer = MessagePack.MessagePackSerializer.Serialize<MessagePackEntity.TestClassVariantA>(TestClassOne);
            buffer = MessagePack.MessagePackSerializer.Serialize<MessagePackEntity.TestClassVariantA>(TestClassOne);
            for (int I = 0; I < 10; I++)
            {
                Queue_Buffer.Enqueue(buffer);
            }

            TestClassOne = new MessagePackEntity.TestClassVariantA();
            TestClassOne.RandomizeData(rnd, stringSize);
            int QueuSize;
            sw.Restart();
            for (int I = 0; I < numReps; I++)
            {
                Queue_MsgPack.Enqueue(TestClassOne);
                Queue_MsgPack.DeQueue();
                Queue_Buffer.Enqueue(buffer);
                Queue_Buffer.DeQueue();
            }
            sw.Stop();
            ElapsedTicks = sw.ElapsedTicks;
            Console.WriteLine(@"  Calculating Raw Overheads (WaitHandle): Count: {1:00,000,000}  -  Time: {2}", buffer.Length, numReps, TimeSpan.FromTicks(ElapsedTicks).ToString());
            sw.Restart();
            for (int I = 0; I < numReps; I++)
            {
                Queue_MsgPack.Enqueue(TestClassOne);
                Queue_MsgPack.DeQueue();
                Queue_Buffer.Enqueue(buffer);
                Queue_Buffer.DeQueue();
            }
            sw.Stop();
            ElapsedTicks = sw.ElapsedTicks;
            Console.WriteLine(@"  Calculating Raw Overheads (QueueSize) : Count: {1:00,000,000}  -  Time: {2}", buffer.Length, numReps, TimeSpan.FromTicks(ElapsedTicks).ToString());


            for (int I = 0; I < 3; I++)
            {
                int numThreads = 1;
                switch (I)
                {
                    case 0: numThreads = numThreadsA; break;
                    case 1: numThreads = numThreadsB; break;
                    case 2: numThreads = numThreadsC; break;
                }
                ElapsedTicks = long.MaxValue;
                ElapsedTicksList.Clear();
                for (int bestOf = 0; bestOf < numBestOf; bestOf++)
                {
                    GC.Collect();
                    Thread.Sleep(100);
                    Queue_MsgPack.Clear();
                    Queue_Buffer.Clear();
                    BenchWaitHandle.Reset();
                    ThreadRun();
                    for (int N = 0; N < numReps; N++)
                    {
                        Queue_MsgPack.Enqueue(TestClassOne.Clone());
                    }
                    for (int N = 0; N < numThreads; N++)
                    {
                        Thread T;
                        T = new Thread(new ThreadStart(MessagePackBenchmark_Read));
                        runningTHreadList.Add(T);
                        T.Start();
                        T = new Thread(new ThreadStart(MessagePackBenchmark_Write));
                        runningTHreadList.Add(T);
                        T.Start();
                    }
                    sw.Restart();
                    BenchWaitHandle.Set();
                    bool BenchRunning = true;
                    while (BenchRunning)
                    {
                        Thread.Sleep(1);
                        BenchRunning = Queue_MsgPack.HasItems();
                    }
                    BenchRunning = true;
                    while (BenchRunning)
                    {
                        Thread.Sleep(1);
                        BenchRunning = Queue_Buffer.HasItems();
                    }
                    sw.Stop();
                    if (sw.ElapsedTicks < ElapsedTicks)
                    {
                        ElapsedTicks = sw.ElapsedTicks;
                    }
                    ElapsedTicksList.Add(sw.ElapsedTicks);
                    ThreadExit();
                    BenchRunning = true;
                    while (BenchRunning)
                    {
                        Thread.Sleep(100);
                        BenchRunning = false;
                        foreach (Thread T in runningTHreadList)
                        {
                            if (T.IsAlive)
                            {
                                BenchRunning = true;
                                break;
                            }
                        }
                    }
                    Thread.Sleep(100);
                }
                if (numBestOf >= 7)
                {
                    ElapsedTicksList.Sort();
                    ElapsedTicksList.RemoveAt(0);
                    ElapsedTicksList.RemoveAt(0);
                    ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                    ElapsedTicksList.RemoveAt(ElapsedTicksList.Count - 1);
                    ElapsedTicks = 0;
                    foreach (long Ticks in ElapsedTicksList)
                    {
                        ElapsedTicks += Ticks;
                    }
                    ElapsedTicks = ElapsedTicks / ElapsedTicksList.Count;
                }
                SerializeSeconds = sw.Elapsed.TotalSeconds;
                SerializeRatePerSecond = ((double)numReps) / SerializeSeconds;
                SerializeMbps = (SerializeRatePerSecond * ((double)buffer.Length)) / ((double)(1024 * 1024));
                Console.WriteLine(@"Round Trip Processing   : Entity Size bytes: {0:00,000}  -  Count: {1:00,000,000}  -  Threads: {2:00}  -  Entities Per Second: {3:00,000,000}  -  Mbps: {4:0.00}  -  Time: {5}", buffer.Length, numReps, numThreads, SerializeRatePerSecond, SerializeMbps, TimeSpan.FromTicks(ElapsedTicks).ToString());
            }
        }

        private static void SerializerIntegrityTest()
        {
            Random rnd = new Random();
            ChillXEntity.TestClassVariantA TestClassOne = new ChillXEntity.TestClassVariantA();
            ChillXEntity.TestClassVariantA TestClassTwo = new ChillXEntity.TestClassVariantA();

            TypedSerializer<ChillXEntity.TestClassVariantA> Serializer = TypedSerializer<ChillXEntity.TestClassVariantA>.Create();

            bool isEqual;
            isEqual = TestClassOne.Equals(TestClassTwo);

            TestClassOne.RandomizeData(rnd, 64);

            isEqual = TestClassOne.Equals(TestClassTwo);

            byte[] buffer;
            int bytesConsumed;
            buffer = Serializer.Read(TestClassOne);
            Serializer.Write(TestClassTwo, buffer, out bytesConsumed);
            isEqual = TestClassOne.EqualsDebug(TestClassTwo);
            if (!isEqual)
            {
                throw new Exception(@"Serializer itegrity test failed. Deserialized entity does not match Original");
            }
        }

        private static void StringSerializePerfTest(int numReps, int StringSize)
        {
            Random rnd = new Random();
            Func<ChillXEntity.TestClassVariantA, byte[], int, int> Reader;
            Action<ChillXEntity.TestClassVariantA, byte[], int> Writer;
            PropertyInfo property = typeof(ChillXEntity.TestClassVariantA).GetProperty(@"VariantAPropertyString");

            //MethodInfo blah = property.GetGetMethod();

            Reader = Helpers.BuildSerializeGetter<ChillXEntity.TestClassVariantA, string>(property);
            Writer = Helpers.BuildSerializeSetter<ChillXEntity.TestClassVariantA>(property, @"ToString");

            ChillXEntity.TestClassVariantA TestClassOne = new ChillXEntity.TestClassVariantA();
            ChillXEntity.TestClassVariantA TestClassTwo = new ChillXEntity.TestClassVariantA();
            string TestString;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < StringSize; i++)
            {
                sb.Append((char)(byte)rnd.Next(32, 125));
            }
            TestString = sb.ToString();
            TestClassOne.VariantAPropertyString = TestString;

            byte[] buffer;
            byte[] bufferTwo;

            buffer = new byte[StringSize * 3];
            Reader(TestClassOne, buffer, 0);

            bufferTwo = System.Text.Encoding.UTF8.GetBytes(TestClassOne.VariantAPropertyString);
            for (int I = 0; I < buffer.Length; I++)
            {
                if (buffer[I] != bufferTwo[I])
                {
                    throw new Exception(@"Serialized bytes do not match System.Text.Encoding.UTF8.GetBytes()");
                }
            }
            bool isEqual = TestClassOne.VariantAPropertyString == TestString;

            TestClassOne.VariantAPropertyString = @"The quick brown fox jumped over the lazy dog";
            isEqual = TestClassOne.VariantAPropertyString == TestString;
            Writer(TestClassOne, buffer, 0);
            if (TestClassOne.VariantAPropertyString != TestString)
            {
                throw new Exception(@"DeSerialized string does not match original");
            }

            Stopwatch sw = Stopwatch.StartNew();
            long ElapsedTicks;


            buffer = new byte[StringSize * 3];
            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    Reader(TestClassOne, buffer, 0);
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"Serialize                        : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());
            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    Writer(TestClassOne, buffer, 0);
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"DeSerialize                      : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());
        }

        unsafe private static void StringPerfTest(int numReps, int StringSize)
        {
            Random rnd = new Random();

            string TestStringOne;
            string TestStringTwo;
            byte[] buffer;
            int bufferSize;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < StringSize; i++)
            {
                sb.Append((char)(byte)rnd.Next(32, 125));
            }
            TestStringOne = sb.ToString();

            bufferSize = System.Text.Encoding.UTF8.GetByteCount(TestStringOne);
            bufferSize = System.Text.Encoding.Unicode.GetByteCount(TestStringOne);

            buffer = new byte[bufferSize];
            fixed (void* ptr = TestStringOne)
            {
                System.Runtime.InteropServices.Marshal.Copy(new IntPtr(ptr), buffer, 0, bufferSize);
            }
            fixed (byte* bptr = buffer)
            {
                char* cptr = (char*)(bptr + 0);
                TestStringTwo = new string(cptr, 0, bufferSize / 2);
            }
            bool IsEqual;
            if (TestStringOne != TestStringTwo)
            {
                throw new Exception(@"Marshal Copy method does not de-serialize to the same string");
            }

            Stopwatch sw = Stopwatch.StartNew();
            long ElapsedTicks;
            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    buffer = new byte[bufferSize];
                    fixed (void* ptr = TestStringOne)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(new IntPtr(ptr), buffer, 0, bufferSize);
                    }
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"Marshal Copy String To Bytes     : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());
            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    fixed (byte* bptr = buffer)
                    {
                        char* cptr = (char*)(bptr + 0);
                        TestStringTwo = new string(cptr, 0, bufferSize / 2);
                    }
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"Bytes to String Unsafe Pointer   : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());
            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    buffer = System.Text.Encoding.Unicode.GetBytes(TestStringOne);
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"Unicode.GetBytes() Serializer    : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());
            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    TestStringTwo = System.Text.Encoding.Unicode.GetString(buffer);
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"Unicode.GetString() DeSerializer : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());
            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    buffer = System.Text.Encoding.UTF8.GetBytes(TestStringOne);
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"UTF8.GetBytes() Serializer       : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());
            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    TestStringTwo = System.Text.Encoding.UTF8.GetString(buffer);
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"UTF8.GetString() DeSerializer    : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());
        }

        private static void BufferPerfTest(int numReps, int numSegments, int segmentSize)
        {
            Random rnd = new Random();
            long ElapsedTicks;
            int bufferSize = numSegments * segmentSize;
            byte[] buffer = new byte[bufferSize];
            byte[] segment = new byte[segmentSize];
            Console.WriteLine(@"----------------------------------------------------------------------------------");
            Console.WriteLine(@"Buffers Test: NumReps: {0}  -  NumSegments: {1}  -  SegmentSize: {2}", numReps, numSegments, segmentSize);
            Console.WriteLine(@"----------------------------------------------------------------------------------");
            for (int I = 0; I < segmentSize; I++)
            {
                segment[I] = (byte)rnd.Next(0, 255);
            }
            ElapsedTicks = long.MaxValue;
            Stopwatch sw = Stopwatch.StartNew();
            //for (int bestOf = 0; bestOf < 3; bestOf++)
            //{
            //    sw.Restart();
            //    for (int I = 0; I < numReps; I++)
            //    {
            //        for (int j = 0; j < numSegments; j++)
            //        {
            //            for (int k = 0; k < segmentSize; k++)
            //            {
            //                buffer[(j * segmentSize) + k] = segment[k];
            //            }
            //        }
            //    }
            //    sw.Stop();
            //    if (sw.ElapsedTicks < ElapsedTicks)
            //    {
            //        ElapsedTicks = sw.ElapsedTicks;
            //    }
            //}
            //Console.WriteLine(@"Inline Copy        : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());

            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    buffer = new byte[bufferSize];
                    for (int j = 0; j < numSegments; j++)
                    {
                        Array.Copy(segment, 0, buffer, j * segmentSize, segmentSize);
                    }
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"Array.copy         : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());

            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    buffer = new byte[bufferSize];
                    for (int j = 0; j < numSegments; j++)
                    {
                        Buffer.BlockCopy(segment, 0, buffer, j * segmentSize, segmentSize);
                    }
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"Buffer.BlockCopy   : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());

            ElapsedTicks = long.MaxValue;
            using (MemoryStream ms = new MemoryStream(bufferSize))
            {
                for (int bestOf = 0; bestOf < 3; bestOf++)
                {
                    sw.Restart();
                    for (int I = 0; I < numReps; I++)
                    {
                        ms.Position = 0;
                        for (int j = 0; j < numSegments; j++)
                        {
                            ms.Write(segment);
                        }
                        buffer = ms.ToArray();
                    }
                    sw.Stop();
                    if (sw.ElapsedTicks < ElapsedTicks)
                    {
                        ElapsedTicks = sw.ElapsedTicks;
                    }
                }
            }
            Console.WriteLine(@"MemStream.ToArray  : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());

            ElapsedTicks = long.MaxValue;
            using (MemoryStream ms = new MemoryStream(bufferSize))
            {
                for (int bestOf = 0; bestOf < 3; bestOf++)
                {
                    sw.Restart();
                    for (int I = 0; I < numReps; I++)
                    {
                        ms.Position = 0;
                        for (int j = 0; j < numSegments; j++)
                        {
                            ms.Write(segment);
                        }
                        buffer = ms.GetBuffer();
                    }
                    sw.Stop();
                    if (sw.ElapsedTicks < ElapsedTicks)
                    {
                        ElapsedTicks = sw.ElapsedTicks;
                    }
                }
            }
            Console.WriteLine(@"MemStream.GetBuffer: {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());

            ElapsedTicks = long.MaxValue;
            ReadOnlySpan<byte> segmentSpan = new ReadOnlySpan<byte>(segment);
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    buffer = new byte[bufferSize];
                    Span<byte> bufferSpan = new Span<byte>(buffer);
                    for (int j = 0; j < numSegments; j++)
                    {
                        segmentSpan.CopyTo(bufferSpan.Slice(j * segmentSize, segmentSize));
                    }
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"Span.CopyTo(Slice) : {0}", TimeSpan.FromTicks(ElapsedTicks).ToString());

            ArrayPool<byte> bufferPool;
            bufferPool = ArrayPool<byte>.Create(bufferSize + 10, 25);
            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    buffer = bufferPool.Rent(bufferSize);
                    try
                    {
                        for (int j = 0; j < numSegments; j++)
                        {
                            Array.Copy(segment, 0, buffer, j * segmentSize, segmentSize);
                        }
                    }
                    finally
                    {
                        bufferPool.Return(buffer);
                    }
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"Array.Copy Pooled 1 :{0}", TimeSpan.FromTicks(ElapsedTicks).ToString());

            ElapsedTicks = long.MaxValue;
            for (int bestOf = 0; bestOf < 3; bestOf++)
            {
                sw.Restart();
                for (int I = 0; I < numReps; I++)
                {
                    buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                    try
                    {
                        for (int j = 0; j < numSegments; j++)
                        {
                            Array.Copy(segment, 0, buffer, j * segmentSize, segmentSize);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                sw.Stop();
                if (sw.ElapsedTicks < ElapsedTicks)
                {
                    ElapsedTicks = sw.ElapsedTicks;
                }
            }
            Console.WriteLine(@"Array.Copy Pooled 2 :{0}", TimeSpan.FromTicks(ElapsedTicks).ToString());
        }
    }
}