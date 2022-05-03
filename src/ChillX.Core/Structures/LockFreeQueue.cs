using ChillX.Core.CapabilityBase;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChillX.Core.Structures
{
    /// <summary>
    /// <see cref="ThreadSafeQueue{T}"/> is a faster and creates lower GC pressure than this implementation
    /// This implementation is only provided as a reference example to compare the performance of lock free structures versus locking structures
    /// This LockFreeQueueNode<T> is not used in any way by the framework as it is inferior in every measurable aspect as compared to <see cref="ThreadSafeQueue{T}"/>
    /// </summary>
    /// <typeparam name="T">Work item type which you would be scheduling for processing</typeparam>
    public class LockFreeQueue<T>
    {
        

        private static bool CompareExchange(ref LockFreeQueueNode<T> location, LockFreeQueueNode<T> newNode, LockFreeQueueNode<T> originalNode)
        {
            return
                object.ReferenceEquals(originalNode, Interlocked.CompareExchange<LockFreeQueueNode<T>>(ref location, newNode, originalNode));
        }

        private volatile int m_Count = 0;
        public int Count { get { return m_Count; } }

        private LockFreeQueueNode<T> head;
        private LockFreeQueueNode<T> tail;

        private bool m_UsePooledNodes = true;

        public LockFreeQueue(bool usePooledNodes = true)
        {
            m_UsePooledNodes = usePooledNodes;
            head = new LockFreeQueueNode<T>();
            tail = head;
        }

        public void Enqueue(T item)
        {
            LockFreeQueueNode<T> originalTail = null;
            LockFreeQueueNode<T> originalTailNext;

            LockFreeQueueNode<T> newNodeToInsert;
            if (m_UsePooledNodes)
            {
                newNodeToInsert = ManagedPool<LockFreeQueueNode<T>>.Shared.Rent(0);
            }
            else
            {
                newNodeToInsert = new LockFreeQueueNode<T>();
            }
            newNodeToInsert.Item = item;

            bool newNodeWasAdded = false;
            while (!newNodeWasAdded)
            {
                originalTail = tail;

                originalTailNext = originalTail.Next;
                if (tail == originalTail)
                {
                    if (originalTailNext == null)
                        newNodeWasAdded = CompareExchange(ref tail.Next, newNodeToInsert, null);
                    else
                        CompareExchange(ref tail, originalTailNext, originalTail);
                }
            }
            Interlocked.Increment(ref m_Count);
            CompareExchange(ref tail, newNodeToInsert, originalTail);
        }

        public void Clear()
        {
            // This would work but we cannot then guarantee that QueueSize would be accurate.
            // Two Interlocked statements is never atomic and the last thing we want to do here is to add locks
            //Interlocked.Exchange(ref tail, head);
            //Interlocked.Exchange(ref m_QueueSize, 0);

            //next best alternative. Spin in a tight loop and dequeue
            bool mightHaveMore = true;
            while (mightHaveMore) { DeQueue(out mightHaveMore); }
        }

        public bool HasItems()
        {
            return m_Count > 0;
        }

        public bool IsEmpty()
        {
            return m_Count == 0;
        }

        public T GetDefault()
        {
            return default(T);
        }

        public T DeQueue(out bool Success)
        {
            LockFreeQueueNode<T> NodeToReturn;
            bool deQueueSuccess = false;
            LockFreeQueueNode<T> originalHead = null;
            T deQueuedNode = default(T);

            while (!deQueueSuccess)
            {

                originalHead = head;
                LockFreeQueueNode<T> originalTail = tail;
                LockFreeQueueNode<T> originalHeadNext = originalHead.Next;

                if (originalHead == head)
                {
                    if (originalHead == originalTail)
                    {
                        if (originalHeadNext == null)
                        {
                            Success = false;
                            return default(T);
                        }
                        CompareExchange(ref tail, originalHeadNext, originalTail);
                    }

                    else
                    {
                        deQueuedNode = originalHeadNext.Item;
                        deQueueSuccess = CompareExchange(ref head, originalHeadNext, originalHead);
                    }
                }
            }
            Interlocked.Decrement(ref m_Count);
            if (m_UsePooledNodes)
            {
                ManagedPool<LockFreeQueueNode<T>>.Shared.Return(originalHead);
            }
            Success = true;
            return deQueuedNode;
        }

        public T DeQueue()
        {
            bool success;
            return DeQueue(out success);
        }
    }

    internal class LockFreeQueueNode<T> : PoolingBase
    {
        //public static readonly ManagedPool<Node<N>> Shared = ManagedPool<Node<N>>.Shared;
        public LockFreeQueueNode()
        {

        }
        public LockFreeQueueNode<T> Next;
        public T Item;

        protected override void HandleOnRented(int capacity)
        {
            Next = null;
        }
        protected override void HandleOnReturned()
        {
            Next = null;
        }

    }


}
