using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChillX.Core.Structures
{
    public class ThreadSafeQueue<T>
    {
        private class Node<N>
        {
            public Node<N> Next;
            public N Item;
        }

        private static bool CompareExchange(ref Node<T> location, Node<T> newNode, Node<T> originalNode)
        {
            return
                object.ReferenceEquals(originalNode,Interlocked.CompareExchange<Node<T>>(ref location, newNode, originalNode));
        }

        private volatile int m_QueueSize = 0;
        public int QueueSize { get { return m_QueueSize; } }

        private Node<T> head;
        private Node<T> tail;

        public ThreadSafeQueue()
        {
            head = new Node<T>();
            tail = head;
        }

        public void Enqueue(T item)
        {
            Node<T> originalTail = null;
            Node<T> originalTailNext;

            Node<T> newNodeToInsert = new Node<T>();
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
            Interlocked.Increment(ref m_QueueSize);
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
            return m_QueueSize > 0;
        }

        public bool IsEmpty()
        {
            return m_QueueSize == 0;
        }

        public T GetDefault()
        {
            return default(T);
        }

        public T DeQueue(out bool Success)
        {
            bool deQueueSuccess = false;
            Node<T> originalHead = null;
            T deQueuedNode = default(T);

            while (!deQueueSuccess)
            {

                originalHead = head;
                Node<T> originalTail = tail;
                Node<T> originalHeadNext = originalHead.Next;

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
                        deQueueSuccess =
                        CompareExchange(ref head, originalHeadNext, originalHead);
                    }
                }
            }
            Interlocked.Decrement(ref m_QueueSize);
            Success = true;
            return deQueuedNode;
        }

        public T DeQueue()
        {
            bool deQueueSuccess = false;
            Node<T> originalHead = null;
            T deQueuedNode = default(T);

            while (!deQueueSuccess)
            {

                originalHead = head;
                Node<T> originalTail = tail;
                Node<T> originalHeadNext = originalHead.Next;

                if (originalHead == head)
                {
                    if (originalHead == originalTail)
                    {
                        if (originalHeadNext == null)
                        {
                            return default(T);
                        }
                        CompareExchange(ref tail, originalHeadNext, originalTail);
                    }

                    else
                    {
                        deQueuedNode = originalHeadNext.Item;
                        deQueueSuccess =
                        CompareExchange(ref head, originalHeadNext, originalHead);
                    }
                }
            }
            Interlocked.Decrement(ref m_QueueSize);
            return deQueuedNode;
        }
    }
}
