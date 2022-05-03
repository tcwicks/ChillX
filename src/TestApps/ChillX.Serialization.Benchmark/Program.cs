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

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using ChillX.Core.Structures;
using System;
using System.Diagnostics;

namespace ChillX.Serialization.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //string TestString = @"The quick brown fox jumped over the lazy dog";
            //byte[] buffer;
            //char[] charArray = TestString.ToCharArray();
            //int numBytes = charArray.Length * 2;
            //buffer = new byte[numBytes];
            //for (int I = 0; I < 10; I++)
            //{
            //    numBytes = BitConverterExtended.GetBytes(charArray, buffer, 0);
            //}
            //string Deserialized;
            //charArray = TestString.ToCharArray();
            //numBytes = charArray.Length * 2;
            //buffer = new byte[numBytes];
            //numBytes = BitConverterExtended.GetBytes(charArray, buffer, 0);
            //for (int I = 0; I < 10; I++)
            //{
            //    charArray = BitConverterExtended.ToCharArray(buffer, 0, numBytes);
            //    Deserialized = new string(charArray);
            //}


            //SanityCheck();
            //var summary = BenchmarkRunner.Run(typeof(Bench_QueueOverhead),
            //    DefaultConfig.Instance.AddDiagnoser(MemoryDiagnoser.Default)
            //    .WithOptions(ConfigOptions.DisableOptimizationsValidator));
            try
            {
                var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args,
                    DefaultConfig.Instance.AddDiagnoser(MemoryDiagnoser.Default)
                    //.WithOptions(ConfigOptions.DisableOptimizationsValidator)
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        //static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

        private static void SanityCheck()
        {
            for (int i = 0; i < 25; i++)
            {
                Stopwatch sw = new Stopwatch();
                ChillXEntity.Bench_ChillXSerializeEntity Debug = new ChillXEntity.Bench_ChillXSerializeEntity();
                Debug.numThreads = 2;
                Debug.arraySize = 64;
                Debug.GlobalSetup();
                sw.Start();
                Debug.Bench_ChillXSerializer();
                sw.Stop();
                Console.WriteLine(@"Sanity Check 1 Time: {0}", sw.Elapsed.ToString());
                Debug.GlobalCleanup();
                Debug.numThreads = 2;
                Debug.GlobalSetup();
                sw.Start();
                Debug.Bench_ChillXSerializer();
                sw.Stop();
                Console.WriteLine(@"Sanity Check 2 Time: {0}", sw.Elapsed.ToString());
                Debug.GlobalCleanup();
                Debug.GlobalCleanup();
                Debug.numThreads = 2;
                Debug.GlobalSetup();
                sw.Start();
                Debug.Bench_ChillXSerializer();
                sw.Stop();
                Console.WriteLine(@"Sanity Check 2 Time: {0}", sw.Elapsed.ToString());
                Debug.GlobalCleanup();
            }
        }

    }

}
