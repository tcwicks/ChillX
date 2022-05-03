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
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ChillX.Core.Structures
{
    public abstract class RentedBufferContractBase : ISupportPooling
    {
        private static Queue<RentedBufferContractBase> Active = new Queue<RentedBufferContractBase>();
        private static readonly ReaderWriterLockSlim ActiveLock = new ReaderWriterLockSlim();
        private static Stopwatch PruneStopWatch = Stopwatch.StartNew();
        private static bool IsPruning = false;
        private static int IsPruneTaskScheduled = SchedulePruneTask();
        private static int SchedulePruneTask()
        {
            if (Interlocked.CompareExchange(ref IsPruneTaskScheduled, 1, 0) == 0)
            {
                bool DoCreateTimer = false;
                ActiveLock.EnterWriteLock();
                try
                {
                    Core.BackgroundTaskSchduler.Schedule(TriggerPrune, 1);
                }
                finally
                {
                    ActiveLock.ExitWriteLock();
                }
            }
            return 1;
        }

        protected static void ActiveAdd(RentedBufferContractBase instance)
        {
            ActiveLock.EnterWriteLock();
            try
            {
                Active.Enqueue(instance);
            }
            finally
            {
                ActiveLock.ExitWriteLock();
            }
        }
        

        private static void TriggerPrune(object argument)
        {
            bool maybeCanPrune = false;
            bool CanPrune = false;
            ActiveLock.EnterReadLock();
            try
            {
                maybeCanPrune = !IsPruning && PruneStopWatch.Elapsed.TotalSeconds > 1d /*Core.BackgroundTaskSchduler.MinimumPollFrequenceSeconds*/;
            }
            finally
            {
                ActiveLock.ExitReadLock();
            }
            if (maybeCanPrune)
            {
                ActiveLock.EnterWriteLock();
                try
                {
                    if (!IsPruning && PruneStopWatch.Elapsed.TotalSeconds > 1d /*Core.BackgroundTaskSchduler.MinimumPollFrequenceSeconds*/)
                    {
                        PruneStopWatch.Restart();
                        CanPrune = true;
                        //ThreadPool.QueueUserWorkItem(new WaitCallback(DoPrune));
                    }
                }
                finally
                {
                    ActiveLock.ExitWriteLock();
                }
            }
            if (CanPrune)
            {
                DoPrune();
            }
        }
        private static void DoPrune()
        {
            bool maybeCanPrune = false;
            ActiveLock.EnterReadLock();
            try
            {
                maybeCanPrune = !IsPruning;
            }
            finally
            {
                ActiveLock.ExitReadLock();
            }
            if (maybeCanPrune)
            {
                bool CanPrune = false;
                Queue<RentedBufferContractBase> maybePendingQueue = null;
                Queue<RentedBufferContractBase> PendingQueue = null;
                RentedBufferContractBase bufferContract;
                ActiveLock.EnterWriteLock();
                try
                {
                    if (!IsPruning)
                    {
                        IsPruning = true;
                        PruneStopWatch.Restart();
                        CanPrune = true;
                        maybePendingQueue = Active;
                        Active = new Queue<RentedBufferContractBase>();
                    }
                }
                finally
                {
                    ActiveLock.ExitWriteLock();
                }
                if (CanPrune)
                {
                    try
                    {
                        PendingQueue = new Queue<RentedBufferContractBase>();
                        bool ShouldGC = maybePendingQueue.Count > 5000;
                        while (maybePendingQueue.Count > 0)
                        {
                            bufferContract = maybePendingQueue.Dequeue();
                            if (!bufferContract.IsRented)
                            {
                                bufferContract.CancelContract();
                            }
                            else if (!bufferContract.ReturnRentedArray())
                            {
                                PendingQueue.Enqueue(bufferContract);
                            }
                        }
                        if (ShouldGC) { GC.Collect(); }

                    }
                    finally
                    {
                        ActiveLock.EnterWriteLock();
                        try
                        {
                            IsPruning = false;
                            while (PendingQueue.Count > 0)
                            {
                                bufferContract = PendingQueue.Dequeue();
                                Active.Enqueue(bufferContract);
                            }
                            while (maybePendingQueue.Count > 0)
                            {
                                bufferContract = maybePendingQueue.Dequeue();
                                Active.Enqueue(bufferContract);
                            }
                            PruneStopWatch.Restart();
                        }
                        finally
                        {
                            ActiveLock.ExitWriteLock();
                        }
                    }
                }
            }
        }

        public RentedBufferContractBase()
        {
        }

        public virtual DateTime LastUsedTimeUTC { get; protected set; }

        public virtual void ExtendLastUsedTimeUTC(DateTime newLastUsedTimeUTC)
        {
            LastUsedTimeUTC = newLastUsedTimeUTC;
        }

        private bool m_IsRented = false;
        public bool IsRented
        {
            get { return m_IsRented; }
        }

       

        public void OnRented(int capacity)
        {
            m_IsRented = true;
            LastUsedTimeUTC = DateTime.UtcNow;
            //if (IsPruneTaskScheduled == 0)
            //{
            //    TriggerPrune(this);
            //}
        }

        public void Return()
        {
            throw new NotImplementedException(@"Rented Buffer Contract is a transparent background class maintained by the framework");
        }

        public void OnReturned()
        {
            m_IsRented = false;
            LastUsedTimeUTC = DateTime.UtcNow;
        }
        protected WeakReference bufferWeakReference;

        public void CancelContract()
        {
            lock (this)
            {
                bufferWeakReference = null;
                DoCancelContract();
            }
        }
        protected abstract void DoCancelContract();

        public bool ReturnRentedArray()
        {
            lock (this)
            {
                if (bufferWeakReference == null || !bufferWeakReference.IsAlive)
                {
                    DoReturnRentedArray();
                    return true;
                }
            }
            return false;
        }
        protected abstract void DoReturnRentedArray();
    }

    public class RentedBufferContract<T> : RentedBufferContractBase
    {
        public static readonly ManagedPool<RentedBufferContract<T>> Shared = ManagedPool<RentedBufferContract<T>>.Shared;
        public RentedBufferContract()
            :base()
        {

        }

        public void BeginContract(RentedBuffer<T> rentedBufferInstance, T[] rentedArray)
        {
            bufferWeakReference = new WeakReference(rentedBufferInstance);
            rentedArrayInstance = rentedArray;
            ActiveAdd(this);
        }
        private T[] rentedArrayInstance;

        protected override void DoReturnRentedArray()
        {
            if (rentedArrayInstance != null)
            {
                ArrayPool<T>.Shared.Return(rentedArrayInstance);
            }
            if (IsRented)
            {
                Shared.Return(this);
            }
        }

        protected override void DoCancelContract()
        {
            rentedArrayInstance = null;
            if (IsRented)
            {
                Shared.Return(this);
            }
        }
    }
}
