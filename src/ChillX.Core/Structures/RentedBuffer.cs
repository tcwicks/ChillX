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

using ChillX.Core.CapabilityBase;
using ChillX.Core.CapabilityInterfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChillX.Core.Structures
{
    /// <summary>
    /// Pooled wrapper around <see cref="ArrayPool{T}"/>
    /// Note: please do NOT call <see cref="Dispose"/>() Instead call <see cref="Return"/>
    /// The framework will pool this object and internally dispose it when appropriate
    /// Note: Create new instances via RentedBuffer<T>.Shared.Rent(Length)
    /// </summary>
    public class RentedBuffer<T> : PoolingBase, IDisposable
    {
        public static readonly ManagedPool<RentedBuffer<T>> Shared = ManagedPool<RentedBuffer<T>>.Shared;

        //Todo: Remove this
        //public List<string> DebugText { get; } = DebugText_Create();
        //private static List<string> DebugText_Create()
        //{
        //    if (Common.EnableDebug)
        //    {
        //        return new List<string>();
        //    }
        //    return null;
        //}

        /// <summary>
        /// Constructor is used internaly by the framework. Instead use <see cref="Shared"/>
        /// Example: RentedBuffer<T>.Shared.Rent(Length)
        /// <code>RentedBuffer<int>.Shared.Rent(50);</code>
        /// </summary>
        public RentedBuffer()
        {
        }

        private WeakReference<RentedBufferContract<T>> Contract;

        //public RentedBuffer(int size)
        //{
        //    OnRented(size);
        //}

        /// <summary>
        /// Used internally by the framework during serialization. Please use <see cref="BufferSpan"/> instead
        /// If you need to do a fast array copy then please use <see cref="Length"/> instead of _rawBufferInternal.Length
        /// </summary>
        public T[] _rawBufferInternal { get; private set; }
        
        /// <summary>
        /// Length of the array
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Array as a Span&lt;<typeparamref name="T"/>&gt;
        /// </summary>
        public Span<T> BufferSpan { get { return _rawBufferInternal.AsSpan(0, Length); } }

        public T this[int index]
        {
            get { return _rawBufferInternal.AsSpan(0, Length)[index]; }
            set { _rawBufferInternal.AsSpan(0, Length)[index] = value; }
        }

        public int? OwnerID { get; set; }

        private bool m_isDisposed = false;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                m_isDisposed = true;
                if (_rawBufferInternal != null)
                {
                    Interlocked.Decrement(ref RentedCount);

                    RentedBufferContract<T> contractInstance;
                    if (Contract.TryGetTarget(out contractInstance))
                    {
                        contractInstance.CancelContract();
                    }

                    ArrayPool<T>.Shared.Return(_rawBufferInternal);
                    //Return(buffer);
                    _rawBufferInternal = null;
                }
                GC.SuppressFinalize(this);
            }
        }

        protected override void HandleOnRented(int capacity)
        {
            if (_rawBufferInternal != null)
            {
                Interlocked.Decrement(ref RentedCount);
                ArrayPool<T>.Shared.Return(_rawBufferInternal);
                _rawBufferInternal = null;
            }
            Interlocked.Increment(ref RentedCount);
            _rawBufferInternal = ArrayPool<T>.Shared.Rent(capacity);
            Length = capacity;
            RentedBufferContract<T> contractInstance;
            if (Contract == null)
            {
                contractInstance = RentedBufferContract<T>.Shared.Rent(0);
                contractInstance.BeginContract(this, _rawBufferInternal);
                Contract = new WeakReference<RentedBufferContract<T>>(contractInstance);
            }
            else if (!Contract.TryGetTarget(out contractInstance))
            {
                contractInstance = RentedBufferContract<T>.Shared.Rent(0);
                contractInstance.BeginContract(this, _rawBufferInternal);
                Contract.SetTarget(contractInstance);
            }
        }

        private int RentedCount = 0;
        public int RentedCountGet()
        {
            return Interlocked.CompareExchange(ref RentedCount, RentedCount, RentedCount);
        }

        public void Return()
        {
            if (IsRented)
            {
                lock (this)
                {
                    if (IsRented)
                    {
                        Shared.Return(this);
                        OwnerID = null;
                        //if (buffer != null)
                        //{
                        //    ArrayPool<T>.Shared.Return(buffer);
                        //    buffer = null;
                        //}
                    }
                }
            }
        }

        protected override void HandleOnReturned()
        {
            if (_rawBufferInternal != null)
            {
                Interlocked.Decrement(ref RentedCount);
                RentedBufferContract<T> contractInstance;
                if (Contract.TryGetTarget(out contractInstance))
                {
                    contractInstance.CancelContract();
                }
                ArrayPool<T>.Shared.Return(_rawBufferInternal);
                _rawBufferInternal = null;
            }
        }

        #region Way Too DANGEROUS
        // WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING 
        // The following four commented out implicit casts are way too dangerous.
        // Do not do something like this which is automatically allocating a buffer from the pool.
        // WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING 

        //public static implicit operator RentedBuffer<T>(T[] input)
        //{
        //    RentedBuffer<T> buffer = RentedBuffer<T>.Shared.Rent(input.Length);
        //    Array.Copy(input, 0, buffer._rawBufferInternal, 0, input.Length);
        //    return buffer;
        //}
        //public static implicit operator T[](RentedBuffer<T> input)
        //{
        //    return input.BufferSpan.ToArray();
        //}

        //public static implicit operator RentedBuffer<T>(Span<T> input)
        //{
        //    RentedBuffer<T> buffer = RentedBuffer<T>.Shared.Rent(input.Length);
        //    input.CopyTo(buffer.BufferSpan);
        //    return buffer;
        //}
        //public static implicit operator Span<T>(RentedBuffer<T> input)
        //{
        //    return input.BufferSpan;
        //}

        // End of dangerous examples
        #endregion

        public static RentedBuffer<T> operator +(RentedBuffer<T> Left, Span<T> Right)
        {
            int len = Math.Min(Left.Length, Right.Length);
            if (len > 0)
            {
                Left.BufferSpan.Clear();
                if (Right.Length > Left.Length)
                {
                    Right.Slice(0, Left.Length).CopyTo(Left.BufferSpan);
                }
                else
                {
                    Right.CopyTo(Left.BufferSpan);
                }
            }
            return Left;
        }

        public static RentedBuffer<T> operator +(RentedBuffer<T> Left, RentedBuffer<T> Right)
        {
            int len = Math.Min(Left.Length, Right.Length);
            if (len > 0)
            {
                Left.BufferSpan.Clear();
                if (Right.Length > Left.Length)
                {
                    Right._rawBufferInternal.AsSpan().Slice(0, Left.Length).CopyTo(Left.BufferSpan);
                }
                else
                {
                    Array.Copy(Right._rawBufferInternal, 0, Left._rawBufferInternal, 0, Right.Length);
                }
            }
            return Left;
        }
        public static Span<T> operator +(Span<T> Left, RentedBuffer<T> Right)
        {
            int len = Math.Min(Left.Length, Right.Length);
            if (len > 0)
            {
                Left.Clear();
                if (Right.Length > Left.Length)
                {
                    Right._rawBufferInternal.AsSpan().Slice(0, Left.Length).CopyTo(Left);
                }
                else
                {
                    Right.BufferSpan.CopyTo(Left);
                }
            }
            return Left;
        }


        public static RentedBuffer<T> operator +(RentedBuffer<T> Left, T[] Right)
        {
            int len = Math.Min(Left.Length, Right.Length);
            if (len > 0)
            {
                Left.BufferSpan.Clear();
                if (Right.Length > Left.Length)
                {
                    Array.Copy(Right, 0, Left._rawBufferInternal, 0, Left.Length);
                }
                else
                {
                    Array.Copy(Right, 0, Left._rawBufferInternal, 0, Right.Length);
                }
            }
            return Left;
        }

        
        public static Span<T> operator +(T[] Left, RentedBuffer<T> Right)
        {
            int len = Math.Min(Left.Length, Right.Length);
            if (len > 0)
            {
                Array.Clear(Left, 0, Left.Length);
                if (Right.Length > Left.Length)
                {
                    Array.Copy(Right._rawBufferInternal, 0, Left, 0, Left.Length);
                }
                else
                {
                    Array.Copy(Right._rawBufferInternal, 0, Left, 0, Right.Length);
                }
            }
            return Left;
        }
    }

}
