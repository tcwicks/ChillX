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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChillX.Core.Structures
{
    public class ThreadSafeQueue<T> : IDisposable
    {
        private Queue<T> m_Queue = new Queue<T>();
        private ReaderWriterLockSlim m_Lock = new ReaderWriterLockSlim();
        public ThreadSafeQueue()
        {
        }

        public void Enqueue(T item)
        {
            m_Lock.EnterWriteLock();
            try
            {
                m_Queue.Enqueue(item);
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            m_Lock.EnterWriteLock();
            try
            {
                m_Queue.Clear();
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }
        public int Count 
        {
            get 
            {
                m_Lock.EnterReadLock();
                try
                {
                    return m_Queue.Count;
                }
                finally
                {
                    m_Lock.ExitReadLock();
                }
            } 
        }

        public bool HasItems()
        {
            m_Lock.EnterReadLock();
            try
            {
                return m_Queue.Count > 0;
            }
            finally
            {
                m_Lock.ExitReadLock();
            }
        }
        public bool IsEmpty()
        {
            m_Lock.EnterReadLock();
            try
            {
                return m_Queue.Count == 0;
            }
            finally
            {
                m_Lock.ExitReadLock();
            }
        }
        public T GetDefault()
        {
            return default(T);
        }

        public T DeQueue(out bool Success)
        {
            Success = true;
            m_Lock.EnterWriteLock();
            try
            {
                if (m_Queue.Count > 0)
                {
                    return m_Queue.Dequeue();
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
            Success = false;
            return default(T);
        }

        public int DeQueue(int requestedCount, Queue<T> destinationQueue , out bool success)
        {
            success = false;
            int counter = 0;
            m_Lock.EnterWriteLock();
            try
            {
                while (m_Queue.Count > 0 && destinationQueue.Count < requestedCount)
                {
                    counter++;
                    destinationQueue.Enqueue(m_Queue.Dequeue());
                    success = true;
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
            return counter;
        }
        public int DeQueue(int requestedCount, ThreadSafeQueue<T> destinationQueue, out bool success)
        {
            success = false;
            int counter = 0;
            m_Lock.EnterWriteLock();
            try
            {
                while (m_Queue.Count > 0 && destinationQueue.Count < requestedCount)
                {
                    counter++;
                    destinationQueue.Enqueue(m_Queue.Dequeue());
                    success = true;
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
            return counter;
        }

        public int DeQueue(ThreadSafeQueue<T> destinationQueue)
        {
            int counter = 0;
            m_Lock.EnterWriteLock();
            try
            {
                while (m_Queue.Count > 0)
                {
                    counter++;
                    destinationQueue.Enqueue(m_Queue.Dequeue());
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
            return counter;
        }

        public int DeQueue(Queue<T> destinationQueue)
        {
            int counter = 0;
            m_Lock.EnterWriteLock();
            try
            {
                while (m_Queue.Count > 0)
                {
                    counter++;
                    destinationQueue.Enqueue(m_Queue.Dequeue());
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
            return counter;
        }

        public T DeQueue()
        {
            m_Lock.EnterWriteLock();
            try
            {
                if (m_Queue.Count > 0)
                {
                    return m_Queue.Dequeue();
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
            return default(T);
        }

        private bool m_IsDisposed = false;
        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                DoDispose(true);
            }
        }
        private void DoDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                m_Lock.Dispose();
                m_Lock = null;
            }
        }
    }
    
}
