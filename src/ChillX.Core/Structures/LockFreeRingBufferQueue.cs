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
using System.Text;
using System.Threading;

namespace ChillX.Core.Structures
{
    /// <summary>
    /// <see cref="ThreadSafeQueue{T}"/> is much faster and creates lower GC pressure than this implementation
    /// This implementation is only provided as a reference example to compare the performance of lock free structures versus locking structures
    /// This LockFreeQueueNode<T> is not used in any way by the framework as it is inferior in every measurable aspect as compared to <see cref="ThreadSafeQueue{T}"/>
    /// </summary>
    /// <typeparam name="T">Work item type which you would be scheduling for processing</typeparam>
    public class LockFreeRingBufferQueue<T>
    {
        private class BufferNode
        {
            public BufferNode(int capacity, int id, bool isFirst = false)
            {
                Buffer = new T[capacity];
                WriterID = -1;
                Head = -1;
                Tail = isFirst ? -1 : 0;
                BufferNodeSize = capacity;
                ID = id;
            }
            public T[] Buffer;
            public int WriterID;
            public int Head;
            public int Tail;
            public BufferNode Next;
            private int BufferNodeSize;
            public int ID;
            public void Clear(int id)
            {
                Array.Clear(Buffer,0, Buffer.Length);
                WriterID = -1;
                Head = -1;
                Tail = 0;
                ID = id;
            }
        }

        private const byte TrueValue = (byte)1;
        private const byte FalseValue = (byte)0;

        public LockFreeRingBufferQueue(int bufferNodeSize  = 8192)
        {
            if (bufferNodeSize < 2)
            {
                throw new ArgumentException(@"Capacity must be greater than 2");
            }
            BufferNodeSize = bufferNodeSize;
            Head = new BufferNode(bufferNodeSize, BufferIDNext, true);
            Tail = Head;
            newHead = new BufferNode(BufferNodeSize, BufferIDNext);
            NewHeadValid = TrueValue;
            OldTailSlot1 = null;
        }
        private int m_BufferID = 0;
        private int BufferIDNext 
        {
            get
            {
                m_BufferID++;
                if (m_BufferID >= int.MaxValue) { m_BufferID = 1; }
                return m_BufferID;
            }
        }
        private BufferNode Head;
        private BufferNode Tail;
        private int BufferNodeSize;
        private int m_Count;

        private BufferNode newHead;
        private byte NewHeadValid;

        private BufferNode OldTailSlot1;
        private BufferNode OldTailSlot2;

        public int Count { get { return m_Count; } }
        private readonly ReaderWriterLockSlim SyncLock = new ReaderWriterLockSlim();

        public void ClearNotThreadSafe()
        {
            SyncLock.EnterWriteLock();
            try
            {
                Head = new BufferNode(BufferNodeSize, BufferIDNext, true);
                Tail = Head;
                Interlocked.Exchange(ref m_Count, 0);
            }
            finally
            {
                SyncLock.ExitWriteLock();
            }
        }

        private static bool CompareExchange(ref int locationIndex, int newIndex, int originalIndex)
        {
            return originalIndex == Interlocked.CompareExchange(ref locationIndex, newIndex, originalIndex);
        }
        public void Enqueue(T item)
        {
            while (!DoEnqueue(item))
            {
                //Just spin loop
            }
        }

        private bool DoEnqueue(T item)
        {
            int currentIndex;
            int nextIndex;
            int epoch;
            BufferNode current;

            current = Head;
            nextIndex = Interlocked.Increment(ref current.WriterID);
            currentIndex = nextIndex - 1;
            if (nextIndex > BufferNodeSize)
            {
                return false;
            }
            else if (nextIndex < BufferNodeSize)
            {
                //Thread.MemoryBarrier();
                current.Buffer[nextIndex] = item;
                while (Interlocked.CompareExchange(ref current.Head, nextIndex, currentIndex) != currentIndex)
                {
                    //Just Loop
                }
                Interlocked.Increment(ref m_Count);

                return true;
            }
            else if (nextIndex == BufferNodeSize)
            {
                #region previous lock based method
                //This can be done without a lock
                SyncLock.EnterWriteLock();
                try
                {
                    //This cannot happen
                    //if (current.ID != Head.ID)
                    //{
                    //    throw new Exception(@"Buffer size is smaller than the number of concurrent threads enqueuing");
                    //    //return false;
                    //}
                    Head = newHead;
                    current.Next = newHead;
                    if (OldTailSlot1 != null)
                    {
                        newHead = OldTailSlot1;
                        OldTailSlot1 = null;
                    }
                    else
                    {
                        newHead = new BufferNode(BufferNodeSize, BufferIDNext);
                    }
                }
                finally
                {
                    SyncLock.ExitWriteLock();
                }
                #endregion

                #region Lock free method
                //Lock free method 
                //BufferNode newHeadTemp = null;

                //while (newHeadTemp == null)
                //{
                //    newHeadTemp = Interlocked.Exchange(ref newHead, null);
                //}

                ////Interlocked is not needed anymore because the final Interlocked.CompareExchange will flush store operations of this thread.
                ////Interlocked.Exchange(ref Head, newHeadTemp);
                ////Interlocked.Exchange(ref current.Next, newHeadTemp);
                //Head = newHeadTemp;
                //current.Next = newHeadTemp;

                ////newHeadTemp = Interlocked.Exchange(ref OldTailSlot1, null);
                //newHeadTemp = OldTailSlot1;
                //OldTailSlot1 = null;

                //if (newHeadTemp == null)
                //{
                //    //newHeadTemp = new BufferNode(BufferNodeSize, BufferID.NextID());
                //    //Interlocked.Exchange(ref newHead, newHeadTemp);
                //    newHeadTemp = new BufferNode(BufferNodeSize, BufferID.NextID());
                //}
                //Interlocked.Exchange(ref newHead, newHeadTemp);
                #endregion

                while (Interlocked.CompareExchange(ref current.Head, nextIndex, currentIndex) != currentIndex)
                {
                    //Just Loop
                }
            }
            return false;
        }
        public T DeQueue(out bool success)
        {
            int currentIndex;
            int nextIndex;
            T Result;
            BufferNode current;
            bool indexIsGood;
            success = false;
            indexIsGood = false;
            Result = default(T);
            current = Tail;
            if (m_Count == 0)
            {
                return Result;
            }
            currentIndex = current.Tail;
            nextIndex = currentIndex + 1;
            if (nextIndex <= BufferNodeSize)
            {
                while (nextIndex <= current.Head)
                {
                    if (Interlocked.CompareExchange(ref current.Tail, nextIndex, currentIndex) == currentIndex)
                    {
                        indexIsGood = true;
                        break;
                    }
                    current = Tail;
                    if (m_Count == 0)
                    {
                        return Result;
                    }
                    currentIndex = current.Tail;
                    nextIndex = currentIndex + 1;
                }
                if (!indexIsGood)
                {
                    return Result;
                }
            }
            if (nextIndex < BufferNodeSize)
            {
                Interlocked.Decrement(ref m_Count);
                Result = current.Buffer[nextIndex];
                //Thread.MemoryBarrier();
                //current.Buffer[nextIndex] = default(T);
                success = true;
                return Result;
            }
            else if (nextIndex >= BufferNodeSize)
            {
                SyncLock.EnterWriteLock();
                try
                {
                    if (current.ID != Tail.ID)
                    {
                        return Result;
                    }
                    else if (Tail.Next != null)
                    {
                        BufferNode newTail;
                        BufferNode oldTailTemp = null;
                        newTail = Tail.Next;
                        if (newTail.Tail <= newTail.Head)
                        {
                            Interlocked.Decrement(ref m_Count);
                            Result = newTail.Buffer[newTail.Tail];
                            //Thread.MemoryBarrier();
                            //newTail.Buffer[newTail.Tail] = default(T);
                            success = true;
                            oldTailTemp = Tail;

                            Interlocked.Exchange(ref Tail, newTail);
                            if (OldTailSlot1 == null)
                            {
                                if (OldTailSlot2 != null)
                                {
                                    OldTailSlot2.Clear(BufferIDNext);
                                    OldTailSlot1 = OldTailSlot2;
                                    OldTailSlot2 = oldTailTemp;
                                }
                            }
                            //Tail = newTail;
                        }
                        else
                        {
                            //Do Nothing
                        }
                    }
                    else
                    {
                        //Do Nothing
                    }
                }
                finally
                {
                    SyncLock.ExitWriteLock();
                }
            }
            return Result;
        }
    }
}
