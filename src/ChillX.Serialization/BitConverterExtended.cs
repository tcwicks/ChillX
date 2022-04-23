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

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChillX.Serialization
{
    /// <summary>
    /// Replacement for System.BitConverter with greatly extended functionality.
    /// If System.BitConverter is ever updated to provide equivalent functionality
    /// then this implementation will be dropped in favour of System.BitConverter
    /// </summary>
    public static class BitConverterExtended
    {

        /// <summary>
        /// This field indicates the "endianess" of the architecture.
        /// The value is set to true if the architecture is little endian;
        /// false if it is big endian.
        /// </summary>
        public static readonly bool IsLittleEndian = FetchIsLitteEndian();
        /// <summary>
        /// Get the correct value for the platform as defined in System.BitConverter
        /// </summary>
        /// <returns>True if Is Little Endian</returns>
        private static bool FetchIsLitteEndian() { return BitConverter.IsLittleEndian; }

        // Booleans will be converted from 4 bytes per bool to 1 byte per bool.
        // We could pack this tighter but the complexity is not worth it
        // That is unless frequently serializing arrays of booleans

        private const byte ByteTrue = (byte)1;
        private const byte ByteFalse = (byte)0;

        /// <summary>
        /// Serializes a bool into a byte and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static int GetBytes(bool value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 1))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            buffer[startIndex] = (value ? ByteTrue : ByteFalse);
            return 1;
        }


        /// <summary>
        /// Copies a byte into buffer at offset specified by startIndex
        /// Just a convenience method provided for generic use.
        /// This is just doing buffer[startIndex] = value;
        /// with range / overflow checking
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(byte value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 1))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //byte[] bytes = new byte[2];
            fixed (byte* b = &buffer[startIndex])
                *((byte*)b) = (byte)value;
            return 1;
        }

        /// <summary>
        /// Serializes a char into two bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(char value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 2))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //byte[] bytes = new byte[2];
            fixed (byte* b = &buffer[startIndex])
                *((short*)b) = (short)value;
            return 2;
        }

        /// <summary>
        /// Serializes a short / Int16 into two bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(short value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 2))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //byte[] bytes = new byte[2];
            fixed (byte* b = &buffer[startIndex])
                *((short*)b) = value;
            return 2;
        }


        /// <summary>
        /// Serializes a int into four bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(int value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 4))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //byte[] bytes = new byte[4];
            fixed (byte* b = &buffer[startIndex])
                *((int*)b) = value;
            return 4;
        }


        /// <summary>
        /// Serializes a long into eight bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(long value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 8))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //byte[] bytes = new byte[8];
            fixed (byte* b = &buffer[startIndex])
                *((long*)b) = value;
            return 8;
        }


        /// <summary>
        /// Serializes a ushort / UInt16 into two bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(ushort value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 2))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //return GetBytes((short)value);
            fixed (byte* b = &buffer[startIndex])
                *((short*)b) = (short)value;
            return 2;
        }


        /// <summary>
        /// Serializes a uint / UInt32 into four bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(uint value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 4))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //return GetBytes((int)value);
            fixed (byte* b = &buffer[startIndex])
                *((int*)b) = (int)value;
            return 4;
        }


        /// <summary>
        /// Serializes a ulong / UInt64 into eight bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(ulong value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 8))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //return GetBytes((long)value);

            fixed (byte* b = &buffer[startIndex])
                *((long*)b) = (long)value;
            return 8;
        }


        /// <summary>
        /// Serializes a single / float into four bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(float value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 4))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //return GetBytes(*(int*)&value);
            fixed (byte* b = &buffer[startIndex])
                *((int*)b) = *(int*)&value;
            return 4;
        }


        /// <summary>
        /// Serializes a double into eight bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(double value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 8))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //return GetBytes(*(long*)&value);
            fixed (byte* b = &buffer[startIndex])
                *((long*)b) = *(long*)&value;
            return 8;
        }


        /// <summary>
        /// Serializes a decimal into sixteen bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(decimal value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 16))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //return GetBytes(*(long*)&value);
            fixed (byte* b = &buffer[startIndex])
                *((decimal*)b) = *(decimal*)&value;
            return 16;
        }

        /// <summary>
        /// Serializes a TimeSpan into eight bytes and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(TimeSpan value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 8))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //return GetBytes(*(long*)&value);

            fixed (byte* b = &buffer[startIndex])
                *((long*)b) = value.Ticks;
            return 8;
        }

        /// <summary>
        /// Serializes a DateTime into eight bytes and assigns it to buffer at offset specified by startIndex
        /// Note: datetime value will be converted to UTC before serializing.
        /// Correseponding de-serializer <see cref="ToDateTime(byte[], int)"/> will also expect the same.
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(DateTime value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 8))
            {
                throw new ArgumentOutOfRangeException(@"startIndex is out of range");
            }
            Contract.EndContractBlock();

            //return GetBytes(*(long*)&value);
            fixed (byte* b = &buffer[startIndex])
                *((long*)b) = value.ToUniversalTime().Ticks;
            return 8;
        }


        /// <summary>
        /// Serializes a DateTime into eight bytes and assigns it to buffer at offset specified by startIndex
        /// Note: datetime value will be converted to UTC before serializing.
        /// Correseponding de-serializer <see cref="ToDateTime(byte[], int)"/> will also expect the same.
        /// </summary>
        /// <param name="value">value to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBytesUTF8String(string value, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (value == null ? 0 : value.Length)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (value == null) { return 0; }

            int len = Encoding.UTF8.GetByteCount(value);
            Array.Copy(Encoding.UTF8.GetBytes(value), 0, buffer, startIndex, len);
            return len;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetByteCountUTF8String(string value)
        {
            if (value == null) { return 0; }
            return Encoding.UTF8.GetByteCount(value);
        }


        /// <summary>
        /// Copies a byte into buffer at offset specified by startIndex
        /// Just a convenience method provided for generic use.
        /// This is just doing Array.Copy(array, 0, buffer, startIndex, array.Length);
        /// with range / overflow checking
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static int GetBytes(byte[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            Array.Copy(array, 0, buffer, startIndex, array.Length);
            return array.Length;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(byte[] array)
        {
            if (array == null) { return 0; }
            return array.Length;
        }

        /// <summary>
        /// Serializes a bool array into a byte array and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(bool[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + array.Length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (byte* b = &buffer[startIndex])
            {
                for (int I = 0; I < len; I++)
                {
                    *((byte*)b + I) = (array[I] ? ByteTrue : ByteFalse);
                }
            }
            return len;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(bool[] array)
        {
            if (array == null) { return 0; }
            return array.Length;
        }

        /// <summary>
        /// Serializes a char array into a byte array and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(char[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 2)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (char* pArr = &array[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < len; I++)
                    {
                        *((short*)b + I) = *((short*)pArr + I);
                    }
                }
            }
            return len * 2;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(char[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 2;
        }


        /// <summary>
        /// Serializes a short / UInt16 array into a byte array and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(short[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 2)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (short* pArr = &array[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < len; I++)
                    {
                        *((short*)b + I) = *((short*)pArr + I);
                    }
                }
            }
            return len * 2;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(short[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 2;
        }


        /// <summary>
        /// Serializes an int / Int32 array into a byte array and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(int[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 4)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (int* pArr = &array[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < len; I++)
                    {
                        *((int*)b + I) = *((int*)pArr + I);
                    }
                }
            }
            return len * 4;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(int[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 4;
        }


        /// <summary>
        /// Serializes a long / Int64 array into a byte array and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(long[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 8)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (long* pArr = &array[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < len; I++)
                    {
                        *((long*)b + I) = *((long*)pArr + I);
                    }
                }
            }
            return len * 8;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(long[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 8;
        }


        /// <summary>
        /// Serializes a ushort / UInt16 array into a byte array and assigns it to buffer at offset specified by startIndex
        /// Note: short[] and ushort[] are compatible therefore if using the same method name reflection invoke fails as it cannot differentiate betweent the two
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytesUShortArray(ushort[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 2)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (ushort* pArr = &array[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < len; I++)
                    {
                        *((short*)b + I) = *((short*)pArr + I);
                    }
                }
            }
            return len * 2;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// Note: short[] and ushort[] are compatible therefore if using the same method name reflection invoke fails as it cannot differentiate betweent the two
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCountUShortArray(ushort[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 2;
        }


        /// <summary>
        /// Serializes a uint / UInt32 array into a byte array and assigns it to buffer at offset specified by startIndex
        /// Note: int[] and uint[] are compatible therefore if using the same method name reflection invoke fails as it cannot differentiate betweent the two
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytesUIntArray(uint[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 4)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (uint* pArr = &array[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < len; I++)
                    {
                        *((int*)b + I) = *((int*)pArr + I);
                    }
                }
            }
            return len * 4;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// Note: int[] and uint[] are compatible therefore if using the same method name reflection invoke fails as it cannot differentiate betweent the two
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCountUIntArray(uint[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 4;
        }


        /// <summary>
        /// Serializes a ulong / UInt64 array into a byte array and assigns it to buffer at offset specified by startIndex
        /// Note: long[] and ulong[] are compatible therefore if using the same method name reflection invoke fails as it cannot differentiate betweent the two
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytesULongArray(ulong[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 8)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (ulong* pArr = &array[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < len; I++)
                    {
                        *((long*)b + I) = *((long*)pArr + I);
                    }
                }
            }
            return len * 8;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// Note: long[] and ulong[] are compatible therefore if using the same method name reflection invoke fails as it cannot differentiate betweent the two
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCountULongArray(ulong[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 8;
        }


        /// <summary>
        /// Serializes a Single / float array into a byte array and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(float[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 4)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (float* pArr = &array[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < len; I++)
                    {
                        *((int*)b + I) = *((int*)pArr + I);
                    }
                }
            }
            return len * 4;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(float[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 4;
        }


        /// <summary>
        /// Serializes a double array into a byte array and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(double[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 8)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (double* pArr = &array[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < len; I++)
                    {
                        *((long*)b + I) = *((long*)pArr + I);
                    }
                }
            }
            return len * 8;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(double[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 8;
        }


        /// <summary>
        /// Serializes a decimal array into a byte array and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(decimal[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 16)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (decimal* pArr = &array[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < len; I++)
                    {
                        *((decimal*)b + I) = *((decimal*)pArr + I);
                    }
                }
            }
            return len * 16;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(decimal[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 16;
        }


        /// <summary>
        /// Serializes a TimeSpan array into a byte array and assigns it to buffer at offset specified by startIndex
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(TimeSpan[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 8)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (byte* b = &buffer[startIndex])
            {
                for (int I = 0; I < len; I++)
                {
                    *((long*)b + I) = (long)array[I].Ticks;
                }
            }
            return len * 8;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(TimeSpan[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 8;
        }


        /// <summary>
        /// Serializes a DateTime array into a byte array and assigns it to buffer at offset specified by startIndex
        /// Note: datetime value will be converted to UTC before serializing.
        /// Correseponding de-serializer <see cref="ToDateTimeArray(byte[], int, int)"/> will also expect the same.
        /// </summary>
        /// <param name="array">array to be serialized</param>
        /// <param name="buffer">byte array buffer to write to</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static int GetBytes(DateTime[] array, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + (array == null ? 0 : array.Length * 8)))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (array == null) { return 0; }

            int len = array.Length;
            fixed (byte* b = &buffer[startIndex])
            {
                for (int I = 0; I < len; I++)
                {
                    *((long*)b + I) = (long)array[I].ToUniversalTime().Ticks;
                }
            }
            return len * 8;
        }

        /// <summary>
        /// Get size in bytes when serialized
        /// </summary>
        /// <param name="array">Array for which the serialized byte count / size is requested</param>
        /// <returns>number of bytes required to serialize specified array</returns>
        public static int GetByteCount(DateTime[] array)
        {
            if (array == null) { return 0; }
            return array.Length * 8;
        }

        /// <summary>
        /// De-Serializes a bool from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static bool ToBoolean(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 1))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            return (buffer[startIndex] == ByteFalse) ? false : true;
        }

        /// <summary>
        /// De-Serializes a bool array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result array</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe bool[] ToBooleanArray(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            bool[] result = new bool[length];

            fixed (bool* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((bool*)pArr + I) = (*((byte*)b + I) == ByteFalse) ? false : true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a byte from buffer at offset specified by startIndex
        /// Just a convenience method provided for generic use.
        /// This is just doing return buffer[startIndex];
        /// with range / overflow checking
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result array</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe byte ToByte(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length <= (startIndex + 1))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            fixed (byte* pbyte = &buffer[startIndex])
            {
                return *((byte*)pbyte);
            }
        }


        /// <summary>
        /// Returns a byte from buffer at offset specified by startIndex
        /// Just a convenience method provided for generic use.
        /// This is just doing byte[] result = new byte[length]; return Array.Copy(buffer, startIndex, result, 0, length);
        /// with range / overflow checking
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result array</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe byte[] ToByteArray(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            byte[] result = new byte[length];

            Array.Copy(buffer, startIndex, result, 0, length);
            return result;
        }


        /// <summary>
        /// De-Serializes a char from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static char ToChar(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 2))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            return (char)ToInt16(buffer, startIndex);
        }


        /// <summary>
        /// De-Serializes a char array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe char[] ToCharArray(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 2 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 2 for char");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 2;
            char[] result = new char[length];

            fixed (char* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((short*)pArr + I) = *((short*)b + I);
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// De-Serializes a short / Int16 from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe short ToInt16(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 2))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            fixed (byte* pbyte = &buffer[startIndex])
            {
                return *((short*)pbyte);
                //if (startIndex % 2 == 0)
                //{ // data is aligned 
                //    return *((short*)pbyte);
                //}
                //else
                //{
                //    if (IsLittleEndian)
                //    {
                //        return (short)((*pbyte) | (*(pbyte + 1) << 8));
                //    }
                //    else
                //    {
                //        return (short)((*pbyte << 8) | (*(pbyte + 1)));
                //    }
                //}
            }
        }


        /// <summary>
        /// De-Serializes a short / Int16 array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe short[] ToInt16Array(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 2 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 2 for short");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 2;
            short[] result = new short[length];

            fixed (short* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((short*)pArr + I) = *((short*)b + I);
                        //    if (IsLittleEndian)
                        //    {
                        //        return (short)((*pbyte) | (*(pbyte + 1) << 8));
                        //    }
                        //    else
                        //    {
                        //        return (short)((*pbyte << 8) | (*(pbyte + 1)));
                        //    }
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// De-Serializes an int / Int32 from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe int ToInt32(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 4))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            fixed (byte* pbyte = &buffer[startIndex])
            {
                return *((int*)pbyte);
                //if (startIndex % 4 == 0)
                //{ // data is aligned 
                //    return *((int*)pbyte);
                //}
                //else
                //{
                //    if (IsLittleEndian)
                //    {
                //        return (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                //    }
                //    else
                //    {
                //        return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                //    }
                //}
            }
        }


        /// <summary>
        /// De-Serializes an int / Int32 array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe int[] ToInt32Array(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 4 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 4 for int");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 4;
            int[] result = new int[length];

            fixed (int* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((int*)pArr + I) = *((int*)b + I);
                        //    if (IsLittleEndian)
                        //    {
                        //        return (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                        //    }
                        //    else
                        //    {
                        //        return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                        //    }
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// De-Serializes a long / Int64 from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe long ToInt64(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 8))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            fixed (byte* pbyte = &buffer[startIndex])
            {
                return *((long*)pbyte);
                //if (startIndex % 8 == 0)
                //{ // data is aligned 
                //    return *((long*)pbyte);
                //}
                //else
                //{
                //    if (IsLittleEndian)
                //    {
                //        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                //        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                //        return (uint)i1 | ((long)i2 << 32);
                //    }
                //    else
                //    {
                //        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                //        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                //        return (uint)i2 | ((long)i1 << 32);
                //    }
                //}
            }
        }


        /// <summary>
        /// De-Serializes a long / Int64 array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe long[] ToInt64Array(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 8 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 8 for long");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 8;
            long[] result = new long[length];

            fixed (long* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((long*)pArr + I) = *((long*)b + I);
                        //    if (IsLittleEndian)
                        //    {
                        //        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                        //        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                        //        return (uint)i1 | ((long)i2 << 32);
                        //    }
                        //    else
                        //    {
                        //        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                        //        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                        //        return (uint)i2 | ((long)i1 << 32);
                        //    }
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// De-Serializes a ushort / UInt16 from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static ushort ToUInt16(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 2))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            //return (ushort)ToInt16(value, startIndex);
            fixed (byte* pbyte = &buffer[startIndex])
            {
                return (ushort)(*((short*)pbyte));
                //if (startIndex % 2 == 0)
                //{ // data is aligned 
                //    return (ushort)(*((short*)pbyte));
                //}
                //else
                //{
                //    if (IsLittleEndian)
                //    {
                //        return (ushort)((*pbyte) | (*(pbyte + 1) << 8));
                //    }
                //    else
                //    {
                //        return (ushort)((*pbyte << 8) | (*(pbyte + 1)));
                //    }
                //}
            }
        }


        /// <summary>
        /// De-Serializes a ushort / UInt16 array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static ushort[] ToUInt16Array(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 2 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 2 for UInt16");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 2;
            ushort[] result = new ushort[length];

            fixed (ushort* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((short*)pArr + I) = *((short*)b + I);
                        //    if (IsLittleEndian)
                        //    {
                        //        return (ushort)((*pbyte) | (*(pbyte + 1) << 8));
                        //    }
                        //    else
                        //    {
                        //        return (ushort)((*pbyte << 8) | (*(pbyte + 1)));
                        //    }
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// De-Serializes a uint / UInt32 from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static uint ToUInt32(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 4))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            //return (uint)ToInt32(value, startIndex);
            fixed (byte* pbyte = &buffer[startIndex])
            {
                return (uint)(*((int*)pbyte));
                //if (startIndex % 4 == 0)
                //{ // data is aligned 
                //    return (uint)(*((int*)pbyte));
                //}
                //else
                //{
                //    if (IsLittleEndian)
                //    {
                //        return (uint)((*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24));
                //    }
                //    else
                //    {
                //        return (uint)((*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3)));
                //    }
                //}
            }

        }


        /// <summary>
        /// De-Serializes a uint / UInt32 array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static uint[] ToUInt32Array(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 4 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 4 for UInt32");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 4;
            uint[] result = new uint[length];

            fixed (uint* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((int*)pArr + I) = *((int*)b + I);
                        //    if (IsLittleEndian)
                        //    {
                        //        return (uint)((*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24));
                        //    }
                        //    else
                        //    {
                        //        return (uint)((*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3)));
                        //    }
                        //}
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// De-Serializes a ulong / UInt64 from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static ulong ToUInt64(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 8))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            //return (ulong)ToInt64(value, startIndex);

            fixed (byte* pbyte = &buffer[startIndex])
            {
                return (ulong)(*((long*)pbyte));
                //if (startIndex % 8 == 0)
                //{ // data is aligned 
                //    return (ulong)(*((long*)pbyte));
                //}
                //else
                //{
                //    if (IsLittleEndian)
                //    {
                //        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                //        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                //        return (ulong)((uint)i1 | ((long)i2 << 32));
                //    }
                //    else
                //    {
                //        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                //        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                //        return (ulong)((uint)i2 | ((long)i1 << 32));
                //    }
                //}
            }
        }


        /// <summary>
        /// De-Serializes a ulong / UInt64 array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static ulong[] ToUInt64Array(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 8 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 8 for UInt64");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 8;
            ulong[] result = new ulong[length];

            fixed (ulong* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((long*)pArr + I) = *((long*)b + I);
                        //    if (IsLittleEndian)
                        //    {
                        //        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                        //        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                        //        return (ulong)((uint)i1 | ((long)i2 << 32));
                        //    }
                        //    else
                        //    {
                        //        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                        //        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                        //        return (ulong)((uint)i2 | ((long)i1 << 32));
                        //    }
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// De-Serializes a single / float from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        unsafe public static float ToSingle(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 4))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            int val = ToInt32(buffer, startIndex);
            return *(float*)&val;
        }


        /// <summary>
        /// De-Serializes a Single / float array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        unsafe public static float[] ToSingleArray(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 4 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 4 for float / single");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 4;
            float[] result = new float[length];

            fixed (float* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((int*)pArr + I) = *((int*)b + I);
                        //    if (IsLittleEndian)
                        //    {
                        //        return (uint)((*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24));
                        //    }
                        //    else
                        //    {
                        //        return (uint)((*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3)));
                        //    }
                        //}
                    }
                }
            }
            return result;
        }



        /// <summary>
        /// De-Serializes a double from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        unsafe public static double ToDouble(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 8))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            long val = ToInt64(buffer, startIndex);
            return *(double*)&val;
        }


        /// <summary>
        /// De-Serializes a double array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        unsafe public static double[] ToDoubleArray(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 4 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 4 for float / single");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 8;
            double[] result = new double[length];

            fixed (double* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((long*)pArr + I) = *((long*)b + I);
                        //    if (IsLittleEndian)
                        //    {
                        //        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                        //        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                        //        return (ulong)((uint)i1 | ((long)i2 << 32));
                        //    }
                        //    else
                        //    {
                        //        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                        //        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                        //        return (ulong)((uint)i2 | ((long)i1 << 32));
                        //    }
                    }
                }
            }
            return result;
        }



        /// <summary>
        /// De-Serializes a decimal from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        unsafe public static decimal ToDecimal(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 16))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            fixed (byte* pbyte = &buffer[startIndex])
            {
                return (decimal)(*((decimal*)pbyte));
            }
        }


        /// <summary>
        /// De-Serializes a decimal array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static decimal[] ToDecimalArray(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 16 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 16 for decimal");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 16;
            decimal[] result = new decimal[length];

            fixed (decimal* pArr = &result[0])
            {
                fixed (byte* b = &buffer[startIndex])
                {
                    for (int I = 0; I < length; I++)
                    {
                        *((decimal*)pArr + I) = *((decimal*)b + I);
                        //    For each int in decimal get bits
                        //    if (IsLittleEndian)
                        //    {
                        //        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                        //        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                        //        return (ulong)((uint)i1 | ((long)i2 << 32));
                        //    }
                        //    else
                        //    {
                        //        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                        //        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                        //        return (ulong)((uint)i2 | ((long)i1 << 32));
                        //    }
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// De-Serializes a TimeSpan from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static TimeSpan ToTimeSpan(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 8))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            //return (ulong)ToInt64(value, startIndex);

            fixed (byte* pbyte = &buffer[startIndex])
            {
                return TimeSpan.FromTicks((*((long*)pbyte)));

                //    if (IsLittleEndian)
                //    {
                //        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                //        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                //        return TimeSpan.FromTicks((ulong)((uint)i1 | ((long)i2 << 32)));
                //    }
                //    else
                //    {
                //        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                //        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                //        return TimeSpan.FromTicks((ulong)((uint)i2 | ((long)i1 << 32)));
                //    }
                //}
            }
        }


        /// <summary>
        /// De-Serializes a TimeSpan array from byte array buffer at offset specified by startIndex
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe TimeSpan[] ToTimeSpanArray(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 8 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 8 for long");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 8;
            TimeSpan[] result = new TimeSpan[length];

            fixed (byte* b = &buffer[startIndex])
            {
                for (int I = 0; I < length; I++)
                {
                    result[I] = TimeSpan.FromTicks(*((long*)b + I));
                    //    if (IsLittleEndian)
                    //    {
                    //        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                    //        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                    //        result[I] = TimeSpan.FromTicks((uint)i1 | ((long)i2 << 32));
                    //    }
                    //    else
                    //    {
                    //        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                    //        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                    //        result[I] = TimeSpan.FromTicks((uint)i2 | ((long)i1 << 32));
                    //    }
                }
            }
            return result;
        }


        /// <summary>
        /// De-Serializes a DateTime from byte array buffer at offset specified by startIndex
        /// Note: Datetime returned will be DateTimeKind.Utc
        /// It is assumed that the previously serialized bytes <see cref="GetBytes(DateTime, byte[], int)"/> contain a UTC date time.
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public unsafe static DateTime ToDateTime(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(@"startIndex must be >= 0");
            }
            if (buffer.Length < (startIndex + 8))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();

            //return (ulong)ToInt64(value, startIndex);
            fixed (byte* pbyte = &buffer[startIndex])
            {
                return new DateTime((*((long*)pbyte)), DateTimeKind.Utc);

                //    if (IsLittleEndian)
                //    {
                //        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                //        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                //        return TimeSpan.FromTicks((ulong)((uint)i1 | ((long)i2 << 32)));
                //    }
                //    else
                //    {
                //        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                //        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                //        return TimeSpan.FromTicks((ulong)((uint)i2 | ((long)i1 << 32)));
                //    }
                //}
            }
        }


        /// <summary>
        /// De-Serializes a DateTime array from byte array buffer at offset specified by startIndex
        /// Note: Datetime returned will be DateTimeKind.Utc
        /// It is assumed that the previously serialized bytes <see cref="GetBytes(DateTime[], byte[], int)"/> contain a UTC date time.
        /// </summary>
        /// <param name="buffer">byte array buffer to deserialize from</param>
        /// <param name="startIndex">buffer offset</param>
        /// <param name="length">number of bytes to deserialize from buffer into returned result array</param>
        /// <returns>De-Serialized result</returns>
        /// <exception cref="ArgumentNullException">Buffer cannot be null</exception>
        /// <exception cref="ArgumentOutOfRangeException">start index and length of data must fit in range of buffer byte array</exception>
        public static unsafe DateTime[] ToDateTimeArray(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            if (length % 8 != 0)
            {
                throw new InvalidOperationException(@"length must be a divisble by 8 for long");
            }
            Contract.EndContractBlock();

            if (length == 0) { return null; }

            length = length / 8;
            DateTime[] result = new DateTime[length];

            fixed (byte* b = &buffer[startIndex])
            {
                for (int I = 0; I < length; I++)
                {
                    result[I] = new DateTime(*((long*)b + I), DateTimeKind.Utc);
                    //    if (IsLittleEndian)
                    //    {
                    //        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                    //        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                    //        result[I] = new DateTime((uint)i1 | ((long)i2 << 32), DateTimeKind.Utc);
                    //    }
                    //    else
                    //    {
                    //        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                    //        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                    //        result[I] = new DateTime((uint)i2 | ((long)i1 << 32), DateTimeKind.Utc);
                    //    }
                }
            }
            return result;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("value cannot be null");
            }
            if ((startIndex < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException(@"startIndex or length is out of range");
            }
            if (buffer.Length < (startIndex + length))
            {
                throw new ArgumentOutOfRangeException(@"startIndex + length is out of range");
            }
            Contract.EndContractBlock();
            return Encoding.UTF8.GetString(buffer.AsSpan().Slice(startIndex, length).ToArray());
        }


        /// <summary>
        /// LIttle Endian bitwise conversion of double to long / Int64
        /// </summary>
        /// <param name="value">double value to be converted</param>
        /// <returns>long / Int64 representation of value</returns>
        /// <exception cref="NotSupportedException">Only implemented for little endian</exception>
        public static unsafe long DoubleToInt64Bits(double value)
        {
            if (!IsLittleEndian)
            {
                throw new NotSupportedException(@"This method is implemented assuming little endian");
            }
            Contract.EndContractBlock();

            return *((long*)&value);
        }

        /// <summary>
        /// LIttle Endian bitwise conversion of long / Int64 to double
        /// </summary>
        /// <param name="value">long / Int64 value to be converted</param>
        /// <returns>double representation of value</returns>
        /// <exception cref="NotSupportedException">Only implemented for little endian</exception>
        public static unsafe double Int64BitsToDouble(long value)
        {
            if (!IsLittleEndian)
            {
                throw new NotSupportedException(@"This method is implemented assuming little endian");
            }
            Contract.EndContractBlock();

            return *((double*)&value);
        }


        /// <summary>
        /// LIttle Endian bitwise conversion of Single / float to int / Int32
        /// </summary>
        /// <param name="value">Single / float value to be converted</param>
        /// <returns>int / Int32 representation of value</returns>
        /// <exception cref="NotSupportedException">Only implemented for little endian</exception>
        public static unsafe int SingleToInt32Bits(float value)
        {
            if (!IsLittleEndian)
            {
                throw new NotSupportedException(@"This method is implemented assuming little endian");
            }
            Contract.EndContractBlock();

            return *((int*)&value);
        }

        /// <summary>
        /// LIttle Endian bitwise conversion of int / Int32 to Single / float
        /// </summary>
        /// <param name="value">int / Int32 value to be converted</param>
        /// <returns>Single / float representation of value</returns>
        /// <exception cref="NotSupportedException">Only implemented for little endian</exception>
        public static unsafe float Int32BitsToSingle(int value)
        {
            if (!IsLittleEndian)
            {
                throw new NotSupportedException(@"This method is implemented assuming little endian");
            }
            Contract.EndContractBlock();

            return *((float*)&value);
        }
    }

}
