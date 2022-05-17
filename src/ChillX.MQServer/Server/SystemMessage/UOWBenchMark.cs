/*
ChillX Framework Library
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

using ChillX.Core.CapabilityInterfaces;
using ChillX.Core.Structures;
using ChillX.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer.Server.SystemMessage
{
    [SerializedEntity(9)]
    public class UOWBenchMark : IDisposable
    {
        public UOWBenchMark()
        {

        }

        private int m_VariantABackingPropertyOne = 1;
        [SerializedMemberAttribute(1)]
        public int VariantABackingPropertyOne { get { return m_VariantABackingPropertyOne; } set { m_VariantABackingPropertyOne = value; } }

        [SerializedMemberAttribute(2)]
        private int m_IndependentPrivateFieldOne = 2;

        [SerializedMemberAttribute(3)]
        public int m_IndependentPublicFieldOne = 3;
        [SerializedMemberAttribute(4)]
        public int VariantAPropertyOne { get; set; } = 4;

        [SerializedMemberAttribute(5)]
        public short VariantAPropertyShort { get; set; } = 5;

        [SerializedMemberAttribute(6)]
        public long VariantAPropertyLong { get; set; } = 6;

        [SerializedMemberAttribute(7)]
        public UInt16 VariantAPropertyUInt16 { get; set; } = 7;

        [SerializedMemberAttribute(8)]
        public UInt32 VariantAPropertyUInt32 { get; set; } = 8;

        [SerializedMemberAttribute(9)]
        public UInt64 VariantAPropertyUInt64 { get; set; } = 9;

        [SerializedMemberAttribute(10)]
        public float VariantAPropertyHalf { get; set; } = 1.1f;

        [SerializedMemberAttribute(11)]
        public float VariantAPropertyFloat { get; set; } = 1.1f;

        [SerializedMemberAttribute(12)]
        public Single VariantAPropertySingle { get; set; } = 1.1f;

        [SerializedMemberAttribute(13)]
        public double VariantAPropertyDouble { get; set; } = 1.2d;

        [SerializedMemberAttribute(14)]
        public bool VariantAPropertyBool { get; set; } = true;

        [SerializedMemberAttribute(15)]
        public string VariantAPropertyString { get; set; } = @"VariantAPropertyString";

        [SerializedMemberAttribute(16)]
        public char VariantAPropertyChar { get; set; } = 'C';

        [SerializedMemberAttribute(17)]
        private string m_IndependentPrivateFieldString = @"m_IndependentPrivateFieldString";

        [SerializedMemberAttribute(18)]
        public string m_IndependentPublicFieldString = @"m_IndependentPublicFieldString";

        [SerializedMemberAttribute(19)]
        public RentedBuffer<char> ArrayProperty_Char { get; set; } = null;

        [SerializedMemberAttribute(20)]
        public RentedBuffer<byte> ArrayProperty_Byte { get; set; } = null;

        [SerializedMemberAttribute(21)]
        public RentedBuffer<short> ArrayProperty_Short { get; set; } = null;

        [SerializedMemberAttribute(22)]
        public RentedBuffer<int> ArrayProperty_Int { get; set; } = null;

        [SerializedMemberAttribute(23)]
        public RentedBuffer<long> ArrayProperty_Long { get; set; } = null;

        [SerializedMemberAttribute(24)]
        public RentedBuffer<ushort> ArrayProperty_UShort { get; set; } = null;

        [SerializedMemberAttribute(25)]
        public RentedBuffer<uint> ArrayProperty_UInt { get; set; } = null;

        [SerializedMemberAttribute(26)]
        public RentedBuffer<ulong> ArrayProperty_ULong { get; set; } = null;

        [SerializedMemberAttribute(27)]
        public RentedBuffer<float> ArrayProperty_Single { get; set; } = null;

        [SerializedMemberAttribute(28)]
        public RentedBuffer<double> ArrayProperty_Double { get; set; } = null;

        [SerializedMemberAttribute(29)]
        public RentedBuffer<decimal> ArrayProperty_Decimal { get; set; } = null;

        [SerializedMemberAttribute(30)]
        public RentedBuffer<TimeSpan> ArrayProperty_TimeSpan { get; set; } = null;

        [SerializedMemberAttribute(31)]
        public RentedBuffer<DateTime> ArrayProperty_DateTime { get; set; } = null;

        public UOWBenchMark RandomizeData(Random rnd, int arraySize)
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
            m_IndependentPrivateFieldString = sb.ToString();
            sb = new System.Text.StringBuilder();
            for (int i = 0; i < arraySize; i++)
            {
                sb.Append((char)(byte)rnd.Next(32, 125));
            }
            m_IndependentPublicFieldString = sb.ToString();

            ArrayProperty_Char = ArrayProperty_Char ?? RentedBuffer<char>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Char[i] = (char)(byte)rnd.Next(32, 125);
            }
            ArrayProperty_Byte = ArrayProperty_Byte ?? RentedBuffer<byte>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Byte[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue);
            }
            ArrayProperty_Short = ArrayProperty_Short ?? RentedBuffer<short>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Short[i] = (short)rnd.Next(short.MinValue, short.MaxValue);
            }
            ArrayProperty_Int = ArrayProperty_Int ?? RentedBuffer<int>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Int[i] = rnd.Next(int.MinValue, int.MaxValue);
            }
            ArrayProperty_Long = ArrayProperty_Long ?? RentedBuffer<long>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Long[i] = (long)rnd.Next(int.MinValue, int.MaxValue);
            }
            ArrayProperty_UShort = ArrayProperty_UShort ?? RentedBuffer<ushort>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_UShort[i] = (ushort)rnd.Next(ushort.MinValue, ushort.MaxValue);
            }
            ArrayProperty_UInt = ArrayProperty_UInt ?? RentedBuffer<uint>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_UInt[i] = (uint)rnd.Next(1, int.MaxValue);
            }
            ArrayProperty_ULong = ArrayProperty_ULong ?? RentedBuffer<ulong>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_ULong[i] = (ulong)rnd.Next(1, int.MaxValue);
            }
            ArrayProperty_Single = ArrayProperty_Single ?? RentedBuffer<float>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Single[i] = (float)rnd.NextDouble() * (float)rnd.Next(1, int.MaxValue);
            }
            ArrayProperty_Single[0] = (float)Math.PI;
            ArrayProperty_Double = ArrayProperty_Double ?? RentedBuffer<double>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Double[i] = rnd.NextDouble() * ((double)rnd.Next(1, int.MaxValue));
            }
            ArrayProperty_Double[0] = Math.PI;
            ArrayProperty_Decimal = ArrayProperty_Decimal ?? RentedBuffer<decimal>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_Decimal[i] = (decimal)(rnd.NextDouble() * ((double)rnd.Next(1, int.MaxValue)));
            }
            if (arraySize > 1) { ArrayProperty_Decimal[0] = (decimal)Math.PI; }
            ArrayProperty_TimeSpan = ArrayProperty_TimeSpan ?? RentedBuffer<TimeSpan>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_TimeSpan[i] = TimeSpan.FromTicks(rnd.Next(int.MinValue, int.MaxValue));
            }
            if (arraySize > 2) { ArrayProperty_TimeSpan[0] = TimeSpan.MinValue; ArrayProperty_TimeSpan[1] = TimeSpan.MaxValue; }
            ArrayProperty_DateTime = ArrayProperty_DateTime ?? RentedBuffer<DateTime>.Shared.Rent(arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                ArrayProperty_DateTime[i] = new DateTime(DateTime.UtcNow.Ticks + (rnd.Next(int.MinValue, int.MaxValue)), DateTimeKind.Utc);
            }
            if (arraySize > 2) { ArrayProperty_DateTime[0] = DateTime.MinValue.ToUniversalTime().AddDays(1); ArrayProperty_DateTime[1] = DateTime.MaxValue.ToUniversalTime().AddDays(-11); }
            return this;
        }

        public UOWBenchMark Clone()
        {
            UOWBenchMark result;
            result = new UOWBenchMark();
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
            result.m_IndependentPrivateFieldString = m_IndependentPrivateFieldString;
            result.m_IndependentPublicFieldString = m_IndependentPublicFieldString;
            
            result.ArrayProperty_Char = result.ArrayProperty_Char ?? RentedBuffer<char>.Shared.Rent(ArrayProperty_Char.Length);
            result.ArrayProperty_Char += ArrayProperty_Char;

            result.ArrayProperty_Byte = result.ArrayProperty_Byte ?? RentedBuffer<byte>.Shared.Rent(ArrayProperty_Byte.Length);
            result.ArrayProperty_Byte += ArrayProperty_Byte;

            result.ArrayProperty_Short = result.ArrayProperty_Short ?? RentedBuffer<short>.Shared.Rent(ArrayProperty_Short.Length);
            result.ArrayProperty_Short += ArrayProperty_Short;

            result.ArrayProperty_Int = result.ArrayProperty_Int ?? RentedBuffer<int>.Shared.Rent(ArrayProperty_Int.Length);
            result.ArrayProperty_Int += ArrayProperty_Int;

            result.ArrayProperty_Long = result.ArrayProperty_Long ?? RentedBuffer<long>.Shared.Rent(ArrayProperty_Long.Length);
            result.ArrayProperty_Long += ArrayProperty_Long;

            result.ArrayProperty_UShort = result.ArrayProperty_UShort ?? RentedBuffer<ushort>.Shared.Rent(ArrayProperty_UShort.Length);
            result.ArrayProperty_UShort += ArrayProperty_UShort;

            result.ArrayProperty_UInt = result.ArrayProperty_UInt ?? RentedBuffer<uint>.Shared.Rent(ArrayProperty_UInt.Length);
            result.ArrayProperty_UInt += ArrayProperty_UInt;

            result.ArrayProperty_ULong = result.ArrayProperty_ULong ?? RentedBuffer<ulong>.Shared.Rent(ArrayProperty_ULong.Length);
            result.ArrayProperty_ULong += ArrayProperty_ULong;

            result.ArrayProperty_Single = result.ArrayProperty_Single ?? RentedBuffer<float>.Shared.Rent(ArrayProperty_Single.Length);
            result.ArrayProperty_Single += ArrayProperty_Single;

            result.ArrayProperty_Double = result.ArrayProperty_Double ?? RentedBuffer<double>.Shared.Rent(ArrayProperty_Double.Length);
            result.ArrayProperty_Double += ArrayProperty_Double;

            result.ArrayProperty_Decimal = result.ArrayProperty_Decimal ?? RentedBuffer<decimal>.Shared.Rent(ArrayProperty_Decimal.Length);
            result.ArrayProperty_Decimal += ArrayProperty_Decimal;

            result.ArrayProperty_TimeSpan = result.ArrayProperty_TimeSpan ?? RentedBuffer<TimeSpan>.Shared.Rent(ArrayProperty_TimeSpan.Length);
            result.ArrayProperty_TimeSpan += ArrayProperty_TimeSpan;

            result.ArrayProperty_DateTime = result.ArrayProperty_DateTime ?? RentedBuffer<DateTime>.Shared.Rent(ArrayProperty_DateTime.Length);
            result.ArrayProperty_DateTime += ArrayProperty_DateTime;
            return result;
        }


        public override bool Equals(object obj)
        {
            if (obj is UOWBenchMark)
            {
                UOWBenchMark other;
                other = (UOWBenchMark)obj;
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
                    && m_IndependentPrivateFieldString.Equals(other.m_IndependentPrivateFieldString)
                    && m_IndependentPublicFieldString.Equals(other.m_IndependentPublicFieldString)
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

        public bool EqualsDebug(UOWBenchMark other)
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
            if (m_IndependentPrivateFieldString != other.m_IndependentPrivateFieldString) { result = false; }
            if (m_IndependentPublicFieldString != other.m_IndependentPublicFieldString) { result = false; }
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

        public bool IsArrayEqual<T>(RentedBuffer<T> x, RentedBuffer<T> y)
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


        public bool Equals(UOWBenchMark x, UOWBenchMark y)
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
            && x.m_IndependentPrivateFieldString.Equals(y.m_IndependentPrivateFieldString)
            && x.m_IndependentPublicFieldString.Equals(y.m_IndependentPublicFieldString)
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
            && x.IsArrayEqual(ArrayProperty_TimeSpan, y.ArrayProperty_TimeSpan)
            && x.IsArrayEqual(ArrayProperty_DateTime, y.ArrayProperty_DateTime);
        }

        public int GetHashCode(UOWBenchMark obj)
        {
            return obj.GetHashCode();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }




        private bool m_IsDisposed = false;
        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                if (ArrayProperty_Char != null)
                {
                    ArrayProperty_Char.Return(); ArrayProperty_Char = null;
                }
                if (ArrayProperty_Byte != null)
                {
                    ArrayProperty_Byte.Return(); ArrayProperty_Byte = null;
                }
                if (ArrayProperty_Short != null)
                {
                    ArrayProperty_Short.Return(); ArrayProperty_Short = null;
                }
                if (ArrayProperty_Int != null)
                {
                    ArrayProperty_Int.Return(); ArrayProperty_Int = null;
                }
                if (ArrayProperty_Long != null)
                {
                    ArrayProperty_Long.Return(); ArrayProperty_Long = null;
                }
                if (ArrayProperty_UShort != null)
                {
                    ArrayProperty_UShort.Return(); ArrayProperty_UShort = null;
                }
                if (ArrayProperty_UInt != null)
                {
                    ArrayProperty_UInt.Return(); ArrayProperty_UInt = null;
                }
                if (ArrayProperty_ULong != null)
                {
                    ArrayProperty_ULong.Return(); ArrayProperty_ULong = null;
                }
                if (ArrayProperty_Single != null)
                {
                    ArrayProperty_Single.Return(); ArrayProperty_Single = null;
                }
                if (ArrayProperty_Double != null)
                {
                    ArrayProperty_Double.Return(); ArrayProperty_Double = null;
                }
                if (ArrayProperty_Decimal != null)
                {
                    ArrayProperty_Decimal.Return(); ArrayProperty_Decimal = null;
                }
                if (ArrayProperty_TimeSpan != null)
                {
                    ArrayProperty_TimeSpan.Return(); ArrayProperty_TimeSpan = null;
                }
                if (ArrayProperty_DateTime != null)
                {
                    ArrayProperty_DateTime.Return(); ArrayProperty_DateTime = null;
                }
                GC.SuppressFinalize(this);
            }
        }

       
    }
}
