using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChillX.Core.Structures
{
    public class LockFreeRingBufferQueue<T>
        where T : IEquatable<T>
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
            public volatile int WriterID;
            public volatile int Head;
            public volatile int Tail;
            public BufferNode Next;
            private int BufferNodeSize;
            public int ID;
        }

        public LockFreeRingBufferQueue(int bufferNodeSize  = 8192)
        {
            if (bufferNodeSize < 2)
            {
                throw new ArgumentException(@"Capacity must be greater than 2");
            }
            BufferNodeSize = bufferNodeSize;
            BufferID = new ThreadsafeCounter(0, int.MaxValue - 1);
            Head = new BufferNode(bufferNodeSize, BufferID.NextID(), true);
            Tail = Head;
        }
        private readonly ThreadsafeCounter BufferID;
        private BufferNode Head;
        private BufferNode Tail;
        private int BufferNodeSize;
        private volatile int m_Count;
        public int Count { get { return m_Count; } }
        private readonly ReaderWriterLockSlim SyncLock = new ReaderWriterLockSlim();

        public void ClearNotThreadSafe()
        {
            SyncLock.EnterWriteLock();
            try
            {
                Head = new BufferNode(BufferNodeSize, BufferID.NextID(), true);
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
                SyncLock.EnterWriteLock();
                try
                {
                    if (current.ID != Head.ID)
                    {
                        throw new Exception(@"Buffer size is smaller than the number of concurrent threads enqueuing");
                        //return false;
                    }
                    BufferNode newHead;
                    newHead = new BufferNode(BufferNodeSize, BufferID.NextID());

                    Head = newHead;
                    Interlocked.Exchange(ref Head, newHead);
                    Interlocked.Exchange(ref current.Next, newHead);
                }
                finally
                {
                    SyncLock.ExitWriteLock();
                }
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
                if (Result.Equals(0))
                {

                }
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
                        // Do Nothing
                        //nextIndex = Interlocked.Decrement(ref current.Tail);
                        return Result;
                    }
                    else if (Tail.Next != null)
                    {
                        BufferNode newTail;
                        newTail = Tail.Next;
                        if (newTail.Tail <= newTail.Head)
                        {
                            Interlocked.Decrement(ref m_Count);
                            Result = newTail.Buffer[newTail.Tail];
                            //Thread.MemoryBarrier();
                            //newTail.Buffer[newTail.Tail] = default(T);
                            if (Result.Equals(0))
                            {

                            }
                            success = true;
                            Tail = newTail;
                        }
                        else
                        {
                            //Do Nothing
                            //nextIndex = Interlocked.Decrement(ref current.Tail);
                        }
                    }
                    else
                    {
                        //Do Nothing
                        //nextIndex = Interlocked.Decrement(ref current.Tail);
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
