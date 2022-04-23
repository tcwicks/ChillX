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

using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillX.Serialization.Benchmark.MessagePackEntity
{
    [MessagePackObject()]
    public class TestClassVariantA : IEqualityComparer<TestClassVariantA>
    {
        public TestClassVariantA()
        {

        }
        public TestClassVariantA(string independentPrivateFieldString)
        {
            m_IndependentPuplicFieldStringOne = independentPrivateFieldString;
        }
        private int m_VariantABackingPropertyOne = 1;
        [MessagePack.Key(1)]
        public int VariantABackingPropertyOne { get { return m_VariantABackingPropertyOne; } set { m_VariantABackingPropertyOne = value; } }

        [MessagePack.Key(2)]
        private int m_IndependentPrivateFieldOne = 2;

        [MessagePack.Key(3)]
        public int m_IndependentPublicFieldOne = 3;
        [MessagePack.Key(4)]
        public int VariantAPropertyOne { get; set; } = 4;

        [MessagePack.Key(5)]
        public short VariantAPropertyShort { get; set; } = 5;

        [MessagePack.Key(6)]
        public long VariantAPropertyLong { get; set; } = 6;

        [MessagePack.Key(7)]
        public UInt16 VariantAPropertyUInt16 { get; set; } = 7;

        [MessagePack.Key(8)]
        public UInt32 VariantAPropertyUInt32 { get; set; } = 8;

        [MessagePack.Key(9)]
        public UInt64 VariantAPropertyUInt64 { get; set; } = 9;

        [MessagePack.Key(10)]
        public float VariantAPropertyHalf { get; set; } = 1.1f;

        [MessagePack.Key(11)]
        public float VariantAPropertyFloat { get; set; } = 1.1f;

        [MessagePack.Key(12)]
        public Single VariantAPropertySingle { get; set; } = 1.1f;

        [MessagePack.Key(13)]
        public double VariantAPropertyDouble { get; set; } = 1.2d;

        [MessagePack.Key(14)]
        public bool VariantAPropertyBool { get; set; } = true;

        [MessagePack.Key(15)]
        public string VariantAPropertyString { get; set; } = @"VariantAPropertyString";

        [MessagePack.Key(16)]
        public char VariantAPropertyChar { get; set; } = 'C';

        [MessagePack.Key(17)]
        public string m_IndependentPuplicFieldStringOne = @"m_IndependentPuplicFieldStringOne";

        [MessagePack.Key(18)]
        public string m_IndependentPublicFieldStringTwo = @"m_IndependentPublicFieldStringTwo";

        [MessagePack.Key(19)]
        public char[] ArrayProperty_Char { get; set; } = null;

        [MessagePack.Key(20)]
        public byte[] ArrayProperty_Byte { get; set; } = null;

        [MessagePack.Key(21)]
        public short[] ArrayProperty_Short { get; set; } = null;

        [MessagePack.Key(22)]
        public int[] ArrayProperty_Int { get; set; } = null;

        [MessagePack.Key(23)]
        public long[] ArrayProperty_Long { get; set; } = null;

        [MessagePack.Key(24)]
        public ushort[] ArrayProperty_UShort { get; set; } = null;

        [MessagePack.Key(25)]
        public uint[] ArrayProperty_UInt { get; set; } = null;

        [MessagePack.Key(26)]
        public ulong[] ArrayProperty_ULong { get; set; } = null;

        [MessagePack.Key(27)]
        public float[] ArrayProperty_Single { get; set; } = null;

        [MessagePack.Key(28)]
        public double[] ArrayProperty_Double { get; set; } = null;

        [MessagePack.Key(29)]
        public decimal[] ArrayProperty_Decimal { get; set; } = null;

        [MessagePack.Key(30)]
        public TimeSpan[] ArrayProperty_TimeSpan { get; set; } = null;

        [MessagePack.Key(31)]
        public DateTime[] ArrayProperty_DateTime { get; set; } = null;

        public TestClassVariantA RandomizeData(Random rnd, int arraySize)
        {
            VariantAPropertyBool = false;
            VariantAPropertyChar = (char)(byte)rnd.Next(32, 125);
            VariantAPropertyDouble = rnd.NextDouble() * 1000d;
            VariantAPropertyFloat = (float)(rnd.NextDouble() * 1000d);
            VariantAPropertyLong = rnd.Next(int.MinValue, int.MaxValue);
            VariantAPropertyOne = rnd.Next(int.MinValue, int.MaxValue);
            VariantAPropertyShort = (short)rnd.Next(short.MinValue, short.MaxValue);
            VariantAPropertySingle = (float)(rnd.NextDouble() * 1000d);
            VariantAPropertyUInt16 = (UInt16)rnd.Next(UInt16.MinValue, UInt16.MaxValue);
            VariantAPropertyUInt32 = (UInt32)rnd.Next(0, int.MaxValue);
            VariantAPropertyUInt64 = (UInt64)rnd.Next(0, int.MaxValue);
            System.Text.StringBuilder sb;
            sb = new System.Text.StringBuilder();
            for (int i = 0; i < arraySize; i++)
            {
                sb.Append((char)(byte)rnd.Next(32, 125));
            }
            VariantAPropertyString = sb.ToString();
            sb = new System.Text.StringBuilder();
            for (int i = 0; i < arraySize; i++)
            {
                sb.Append((char)(byte)rnd.Next(32, 125));
            }
            m_IndependentPuplicFieldStringOne = sb.ToString();
            sb = new System.Text.StringBuilder();
            for (int i = 0; i < arraySize; i++)
            {
                sb.Append((char)(byte)rnd.Next(32, 125));
            }
            m_IndependentPublicFieldStringTwo = sb.ToString();

            ArrayProperty_Char = new char[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Char[i] = (char)(byte)rnd.Next(32, 125);
            }
            ArrayProperty_Byte = new byte[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Byte[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue);
            }
            ArrayProperty_Short = new short[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Short[i] = (short)rnd.Next(short.MinValue, short.MaxValue);
            }
            ArrayProperty_Int = new int[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Int[i] = rnd.Next(int.MinValue, int.MaxValue);
            }
            ArrayProperty_Long = new long[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Long[i] = (long)rnd.Next(int.MinValue, int.MaxValue);
            }
            ArrayProperty_UShort = new ushort[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_UShort[i] = (ushort)rnd.Next(ushort.MinValue, ushort.MaxValue);
            }
            ArrayProperty_UInt = new uint[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_UInt[i] = (uint)rnd.Next(1, int.MaxValue);
            }
            ArrayProperty_ULong = new ulong[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_ULong[i] = (ulong)rnd.Next(1, int.MaxValue);
            }
            ArrayProperty_Single = new float[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Single[i] = (float)rnd.NextDouble() * (float)rnd.Next(1, int.MaxValue);
            }
            ArrayProperty_Single[0] = (float)Math.PI;
            ArrayProperty_Double = new double[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Double[i] = rnd.NextDouble() * ((double)rnd.Next(1, int.MaxValue));
            }
            ArrayProperty_Double[0] = Math.PI;
            ArrayProperty_Decimal = new decimal[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Decimal[i] = (decimal)(rnd.NextDouble() * ((double)rnd.Next(1, int.MaxValue)));
            }
            if (arraySize > 1) { ArrayProperty_Decimal[0] = (decimal)Math.PI; }
            ArrayProperty_TimeSpan = new TimeSpan[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_TimeSpan[i] = TimeSpan.FromTicks(rnd.Next(int.MinValue, int.MaxValue));
            }
            if (arraySize > 2) { ArrayProperty_TimeSpan[0] = TimeSpan.MinValue; ArrayProperty_TimeSpan[1] = TimeSpan.MaxValue; }
            ArrayProperty_DateTime = new DateTime[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_DateTime[i] = new DateTime(DateTime.UtcNow.Ticks + (rnd.Next(int.MinValue, int.MaxValue)), DateTimeKind.Utc);
            }
            if (arraySize > 2) { ArrayProperty_DateTime[0] = DateTime.MinValue.ToUniversalTime().AddDays(1); ArrayProperty_DateTime[1] = DateTime.MaxValue.ToUniversalTime().AddDays(-11); }
            return this;
        }
        public TestClassVariantA Clone()
        {
            TestClassVariantA result;
            result = new TestClassVariantA();
            result.VariantABackingPropertyOne = VariantABackingPropertyOne;
            result.m_IndependentPrivateFieldOne = m_IndependentPrivateFieldOne;
            result.m_IndependentPublicFieldOne = m_IndependentPublicFieldOne;
            result.VariantAPropertyOne = VariantAPropertyOne;
            result.VariantAPropertyShort = VariantAPropertyShort;
            result.VariantAPropertyLong = VariantAPropertyLong;
            result.VariantAPropertyUInt16 = VariantAPropertyUInt16;
            result.VariantAPropertyUInt32 = VariantAPropertyUInt32;
            result.VariantAPropertyUInt64 = VariantAPropertyUInt64;
            result.VariantAPropertyHalf = VariantAPropertyHalf;
            result.VariantAPropertyFloat = VariantAPropertyFloat;
            result.VariantAPropertySingle = VariantAPropertySingle;
            result.VariantAPropertyDouble = VariantAPropertyDouble;
            result.VariantAPropertyBool = VariantAPropertyBool;
            result.VariantAPropertyString = VariantAPropertyString;
            result.VariantAPropertyChar = VariantAPropertyChar;
            result.m_IndependentPuplicFieldStringOne = m_IndependentPuplicFieldStringOne;
            result.m_IndependentPublicFieldStringTwo = m_IndependentPublicFieldStringTwo;
            result.ArrayProperty_Char = new char[ArrayProperty_Char.Length]; Array.Copy(ArrayProperty_Char, result.ArrayProperty_Char, ArrayProperty_Char.Length);
            result.ArrayProperty_Byte = new byte[ArrayProperty_Byte.Length]; Array.Copy(ArrayProperty_Byte, result.ArrayProperty_Byte, ArrayProperty_Byte.Length);
            result.ArrayProperty_Short = new short[ArrayProperty_Short.Length]; Array.Copy(ArrayProperty_Short, result.ArrayProperty_Short, ArrayProperty_Short.Length);
            result.ArrayProperty_Int = new int[ArrayProperty_Int.Length]; Array.Copy(ArrayProperty_Int, result.ArrayProperty_Int, ArrayProperty_Int.Length);
            result.ArrayProperty_Long = new long[ArrayProperty_Long.Length]; Array.Copy(ArrayProperty_Long, result.ArrayProperty_Long, ArrayProperty_Long.Length);
            result.ArrayProperty_UShort = new ushort[ArrayProperty_UShort.Length]; Array.Copy(ArrayProperty_UShort, result.ArrayProperty_UShort, ArrayProperty_UShort.Length);
            result.ArrayProperty_UInt = new uint[ArrayProperty_UInt.Length]; Array.Copy(ArrayProperty_UInt, result.ArrayProperty_UInt, ArrayProperty_UInt.Length);
            result.ArrayProperty_ULong = new ulong[ArrayProperty_ULong.Length]; Array.Copy(ArrayProperty_ULong, result.ArrayProperty_ULong, ArrayProperty_ULong.Length);
            result.ArrayProperty_Single = new float[ArrayProperty_Single.Length]; Array.Copy(ArrayProperty_Single, result.ArrayProperty_Single, ArrayProperty_Single.Length);
            result.ArrayProperty_Double = new double[ArrayProperty_Double.Length]; Array.Copy(ArrayProperty_Double, result.ArrayProperty_Double, ArrayProperty_Double.Length);
            result.ArrayProperty_Decimal = new decimal[ArrayProperty_Decimal.Length]; Array.Copy(ArrayProperty_Decimal, result.ArrayProperty_Decimal, ArrayProperty_Decimal.Length);
            result.ArrayProperty_TimeSpan = new TimeSpan[ArrayProperty_TimeSpan.Length]; Array.Copy(ArrayProperty_TimeSpan, result.ArrayProperty_TimeSpan, ArrayProperty_TimeSpan.Length);
            result.ArrayProperty_DateTime = new DateTime[ArrayProperty_DateTime.Length]; Array.Copy(ArrayProperty_DateTime, result.ArrayProperty_DateTime, ArrayProperty_DateTime.Length);
            return result;
        }
        public override bool Equals(object? obj)
        {
            if (obj is TestClassVariantA)
            {
                TestClassVariantA other;
                other = (TestClassVariantA)obj;
                if (other == null) { return false; }
                return m_VariantABackingPropertyOne == other.m_VariantABackingPropertyOne
                    && m_IndependentPrivateFieldOne == other.m_IndependentPrivateFieldOne
                    && m_IndependentPublicFieldOne == other.m_IndependentPublicFieldOne
                    && VariantAPropertyOne.Equals(other.VariantAPropertyOne)
                    && VariantAPropertyShort.Equals(other.VariantAPropertyShort)
                    && VariantAPropertyLong.Equals(other.VariantAPropertyLong)
                    && VariantAPropertyUInt16.Equals(other.VariantAPropertyUInt16)
                    && VariantAPropertyUInt32.Equals(other.VariantAPropertyUInt32)
                    && VariantAPropertyUInt64.Equals(other.VariantAPropertyUInt64)
                    && VariantAPropertyHalf.Equals(other.VariantAPropertyHalf)
                    && VariantAPropertyFloat.Equals(other.VariantAPropertyFloat)
                    && VariantAPropertySingle.Equals(other.VariantAPropertySingle)
                    && VariantAPropertyDouble.Equals(other.VariantAPropertyDouble)
                    && VariantAPropertyBool.Equals(other.VariantAPropertyBool)
                    && VariantAPropertyString.Equals(other.VariantAPropertyString)
                    && VariantAPropertyChar.Equals(other.VariantAPropertyChar)
                    && m_IndependentPuplicFieldStringOne.Equals(other.m_IndependentPuplicFieldStringOne)
                    && m_IndependentPublicFieldStringTwo.Equals(other.m_IndependentPublicFieldStringTwo)
                    && IsArrayEqual(ArrayProperty_Char, other.ArrayProperty_Char)
                    && IsArrayEqual(ArrayProperty_Byte, other.ArrayProperty_Byte)
                    && IsArrayEqual(ArrayProperty_Short, other.ArrayProperty_Short)
                    && IsArrayEqual(ArrayProperty_Int, other.ArrayProperty_Int)
                    && IsArrayEqual(ArrayProperty_Long, other.ArrayProperty_Long)
                    && IsArrayEqual(ArrayProperty_UShort, other.ArrayProperty_UShort)
                    && IsArrayEqual(ArrayProperty_UInt, other.ArrayProperty_UInt)
                    && IsArrayEqual(ArrayProperty_ULong, other.ArrayProperty_ULong)
                    && IsArrayEqual(ArrayProperty_Single, other.ArrayProperty_Single)
                    && IsArrayEqual(ArrayProperty_Double, other.ArrayProperty_Double)
                    && IsArrayEqual(ArrayProperty_Decimal, other.ArrayProperty_Decimal)
                    && IsArrayEqual(ArrayProperty_TimeSpan, other.ArrayProperty_TimeSpan)
                    && IsArrayEqual(ArrayProperty_DateTime, other.ArrayProperty_DateTime);
            }
            return base.Equals(obj);
        }

        public bool EqualsDebug(TestClassVariantA other)
        {
            if (other == null) { return false; }
            bool result = true;
            if (m_VariantABackingPropertyOne != other.m_VariantABackingPropertyOne) { result = false; }
            if (m_IndependentPrivateFieldOne != other.m_IndependentPrivateFieldOne) { result = false; }
            if (m_IndependentPublicFieldOne != other.m_IndependentPublicFieldOne) { result = false; }
            if (VariantAPropertyOne != other.VariantAPropertyOne) { result = false; }
            if (VariantAPropertyShort != other.VariantAPropertyShort) { result = false; }
            if (VariantAPropertyLong != other.VariantAPropertyLong) { result = false; }
            if (VariantAPropertyUInt16 != other.VariantAPropertyUInt16) { result = false; }
            if (VariantAPropertyUInt32 != other.VariantAPropertyUInt32) { result = false; }
            if (VariantAPropertyUInt64 != other.VariantAPropertyUInt64) { result = false; }
            if (VariantAPropertyHalf != other.VariantAPropertyHalf) { result = false; }
            if (VariantAPropertyFloat != other.VariantAPropertyFloat) { result = false; }
            if (VariantAPropertySingle != other.VariantAPropertySingle) { result = false; }
            if (VariantAPropertyDouble != other.VariantAPropertyDouble) { result = false; }
            if (VariantAPropertyBool != other.VariantAPropertyBool) { result = false; }
            if (VariantAPropertyString != other.VariantAPropertyString) { result = false; }
            if (VariantAPropertyChar != other.VariantAPropertyChar) { result = false; }
            if (m_IndependentPuplicFieldStringOne != other.m_IndependentPuplicFieldStringOne) { result = false; }
            if (m_IndependentPublicFieldStringTwo != other.m_IndependentPublicFieldStringTwo) { result = false; }
            if (!IsArrayEqual(ArrayProperty_Char, other.ArrayProperty_Char)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_Byte, other.ArrayProperty_Byte)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_Short, other.ArrayProperty_Short)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_Int, other.ArrayProperty_Int)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_Long, other.ArrayProperty_Long)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_UShort, other.ArrayProperty_UShort)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_UInt, other.ArrayProperty_UInt)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_ULong, other.ArrayProperty_ULong)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_Single, other.ArrayProperty_Single)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_Double, other.ArrayProperty_Double)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_Decimal, other.ArrayProperty_Decimal)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_TimeSpan, other.ArrayProperty_TimeSpan)) { result = false; }
            if (!IsArrayEqual(ArrayProperty_DateTime, other.ArrayProperty_DateTime)) { result = false; }
            return result;
        }
        public bool IsArrayEqual<T>(T[] x, T[] y)
            where T : struct
        {
            if ((x == null) && (y == null)) { return true; }
            if ((x == null) || (y == null)) { return false; }
            if (x.Length != y.Length) { return false; }
            for (int I = 0; I < x.Length; I++)
            {
                if (!x[I].Equals(y[I])) { return false; }
            }
            return true;
        }

        public bool Equals(TestClassVariantA? x, TestClassVariantA? y)
        {
            if (x == null && y == null) { return true; }
            if (x == null || y == null) { return false; }
            return x.m_VariantABackingPropertyOne.Equals(y.m_VariantABackingPropertyOne)
            && x.m_IndependentPrivateFieldOne.Equals(y.m_IndependentPrivateFieldOne)
            && x.m_IndependentPublicFieldOne.Equals(Equals(y.m_IndependentPublicFieldOne))
            && x.VariantAPropertyOne.Equals(y.VariantAPropertyOne)
            && x.VariantAPropertyShort.Equals(y.VariantAPropertyShort)
            && x.VariantAPropertyLong.Equals(y.VariantAPropertyLong)
            && x.VariantAPropertyUInt16.Equals(y.VariantAPropertyUInt16)
            && x.VariantAPropertyUInt32.Equals(y.VariantAPropertyUInt32)
            && x.VariantAPropertyUInt64.Equals(y.VariantAPropertyUInt64)
            && x.VariantAPropertyHalf.Equals(y.VariantAPropertyHalf)
            && x.VariantAPropertyFloat.Equals(y.VariantAPropertyFloat)
            && x.VariantAPropertySingle.Equals(y.VariantAPropertySingle)
            && x.VariantAPropertyDouble.Equals(y.VariantAPropertyDouble)
            && x.VariantAPropertyBool.Equals(y.VariantAPropertyBool)
            && x.VariantAPropertyString.Equals(y.VariantAPropertyString)
            && x.VariantAPropertyChar.Equals(y.VariantAPropertyChar)
            && x.m_IndependentPuplicFieldStringOne.Equals(y.m_IndependentPuplicFieldStringOne)
            && x.m_IndependentPublicFieldStringTwo.Equals(y.m_IndependentPublicFieldStringTwo)
            && x.IsArrayEqual(ArrayProperty_Char, y.ArrayProperty_Char)
            && x.IsArrayEqual(ArrayProperty_Byte, y.ArrayProperty_Byte)
            && x.IsArrayEqual(ArrayProperty_Short, y.ArrayProperty_Short)
            && x.IsArrayEqual(ArrayProperty_Int, y.ArrayProperty_Int)
            && x.IsArrayEqual(ArrayProperty_Long, y.ArrayProperty_Long)
            && x.IsArrayEqual(ArrayProperty_UShort, y.ArrayProperty_UShort)
            && x.IsArrayEqual(ArrayProperty_UInt, y.ArrayProperty_UInt)
            && x.IsArrayEqual(ArrayProperty_ULong, y.ArrayProperty_ULong)
            && x.IsArrayEqual(ArrayProperty_Single, y.ArrayProperty_Single)
            && x.IsArrayEqual(ArrayProperty_Double, y.ArrayProperty_Double)
            && x.IsArrayEqual(ArrayProperty_Decimal, y.ArrayProperty_Decimal)
            && x.IsArrayEqual(ArrayProperty_TimeSpan, y.ArrayProperty_TimeSpan);
        }

        public int GetHashCode(TestClassVariantA obj)
        {
            return obj.GetHashCode();
        }
    }
}
