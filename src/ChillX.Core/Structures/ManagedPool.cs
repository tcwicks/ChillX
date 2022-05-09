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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ChillX.Core.Structures
{
    
    public class ManagedPool<T>
        where T: ISupportExpiry, ISupportPooling, new()
    {
        private static ManagedPool<T> m_Shared = null;
        private static object m_Shared_Lock = new object();
        public static ManagedPool<T> Shared
        {
            get
            {
                if (m_Shared == null)
                {
                    lock (m_Shared_Lock)
                    {
                        if (m_Shared == null)
                        {
                            m_Shared = new ManagedPool<T>();
                        }
                    }
                }
                return m_Shared;
            }
        }

        private ManagedPool()
        {
            //T instance = new T();
            //IDisposable disposableT = instance as IDisposable;
            //if (disposableT != null)
            //{
            //    m_IsDisposableT = true;
            //    disposableT.Dispose();
            //}
            //else
            //{
            //    m_IsDisposableT = false;
            //}

            m_IsDisposableT = (typeof(T).GetInterface(@"IDisposable") != null);

            //TrimPoolStopWatch.Start();
            //for (int I = 0; I < 1024; I++)
            //{
            //    ObjectPool.Enqueue(new T());
            //}

            Core.BackgroundTaskSchduler.Schedule(TrimCallBack, 15);
        }

        private static double m_LifetimeSeconds = 60d;
        public static double LifetimeSeconds
        {
            get { return m_LifetimeSeconds; }
            set
            {
                if (value < 1d) { value = 1d; }
                else if (value > 60d) { value = 60d; }
                Interlocked.Exchange(ref m_LifetimeSeconds, value);
            }
        }

        private bool m_IsDisposableT = false;
        public bool IsDisposableT { get { return m_IsDisposableT; } }
        //private readonly Stopwatch TrimPoolStopWatch = new Stopwatch();
        //private int TrimPoolCountDown = 128000;
        //private int IsTrimPoolStarting = 0;

        private ThreadSafeQueue<T> ObjectPool = new ThreadSafeQueue<T>();

        /// <summary>
        /// Todo: Debug purposes only. Remove this
        /// But first why are we not getting back all the items we rent ???
        /// </summary>
        private volatile int m_RentedCount = 0;

        public T Rent(int capacity)
        {
            bool success;
            T result;
            result = ObjectPool.DeQueue(out success);
            if (success)
            {
                result.OnRented(capacity);
                Interlocked.Increment(ref m_RentedCount);
                ////Todo: Remove Debug Code
                //if (Common.EnableDebug)
                //{
                //    RentedBuffer<char> DebugOne;
                //    DebugOne = result as RentedBuffer<char>;
                //    if (DebugOne != null)
                //    {
                //        DebugOne.DebugText.Add(@"Rented: From Cache.");
                //    }
                //}
                return result;
            }
            result = new T();
            result.OnRented(capacity);
            Interlocked.Increment(ref m_RentedCount);
            //Todo: Remove Debug Code
            //if (Common.EnableDebug)
            //{
            //    RentedBuffer<char> DebugTwo;
            //    DebugTwo = result as RentedBuffer<char>;
            //    if (DebugTwo != null)
            //    {
            //        DebugTwo.DebugText.Add(@"Rented: New.");
            //    }
            //}

            //if ((TrimPoolStopWatch.ElapsedMilliseconds > 60000) || (Interlocked.Decrement(ref TrimPoolCountDown) < 0))
            //{
            //    if (Interlocked.CompareExchange(ref IsTrimPoolStarting, 1, 0) == 0)
            //    {
            //        if ((TrimPoolStopWatch.ElapsedMilliseconds > 60000) || (TrimPoolCountDown < 0))
            //        {
            //            Interlocked.Exchange(ref TrimPoolCountDown, 128000);
            //            TrimPoolStopWatch.Reset();
            //            try
            //            {
            //                ThreadPool.QueueUserWorkItem(new WaitCallback(TrimCallBack));
            //            }
            //            catch
            //            {
            //                // This should not happen but just in case
            //                // Could wrao this in a try catch and silence any exception but lets not.
            //                Thread thread = new Thread(new ThreadStart(Trim));
            //                thread.Start();
            //            }
            //        }
            //        Interlocked.Exchange(ref IsTrimPoolStarting, 0);
            //    }
            //}
            return result;
        }

        public void Return(T item)
        {
            //Todo: Remove Debug Code
            //if (Common.EnableDebug)
            //{
            //    RentedBuffer<char> DebugOne;
            //    DebugOne = item as RentedBuffer<char>;
            //    if (DebugOne != null)
            //    {
            //        if (DebugOne.IsRented)
            //        {
            //            if (DebugOne._rawBufferInternal != null)
            //            {
            //                DebugOne.DebugText.Add(String.Concat(@"Returned: ", new String(DebugOne.BufferSpan.ToArray())));
            //            }
            //            else
            //            {
            //                DebugOne.DebugText.Add(@"_rawBufferInternal is Null !!!!!");
            //            }
            //        }
            //        else
            //        {
            //            DebugOne.DebugText.Add(@"Duplicate Return!!!!!");
            //        }
            //    }
            //}
            if (item.IsRented)
            {
                Interlocked.Decrement(ref m_RentedCount);
                item.OnReturned();
                ObjectPool.Enqueue(item);
            }
        }

        private volatile int IsTrimming = 0;
        private void TrimCallBack(object argument)
        {
            Trim();
        }
        private void Trim()
        {
            double TTLSeconds;
            if (Interlocked.CompareExchange(ref IsTrimming, 1, 0) == 0)
            {
                try
                {
                    TTLSeconds = m_LifetimeSeconds;
                    T item;
                    bool success = true;
                    DateTime CurrentTime = DateTime.UtcNow;
                    //int transferSize = Math.Min(ObjectPool.Count / 3, 32);
                    int numToCheck = ObjectPool.Count / 2;
                    item = ObjectPool.DeQueue(out success);
                    while (success && numToCheck > 0)
                    {
                        numToCheck--;
                        if (Math.Abs(CurrentTime.Subtract(item.LastUsedTimeUTC).TotalSeconds) < TTLSeconds)
                        {
                            ObjectPool.Enqueue(item);
                        }
                        else if (IsDisposableT)
                        {
                            ((IDisposable)item).Dispose();
                        }
                        item = ObjectPool.DeQueue(out success);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref IsTrimming, 0);
                }
            }
        }
    }
}
