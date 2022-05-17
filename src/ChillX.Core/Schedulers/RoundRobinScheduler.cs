using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChillX.Core.Schedulers
{
    public class RoundRobinScheduler<TKey,TTarget>
    {
        private TKey m_Key;
        public TKey Key { get { return m_Key; } }

        private readonly HashSet<TTarget> m_Targets = new HashSet<TTarget>();
        private HashSet<TTarget> Targets { get { return m_Targets; } }
        private readonly List<TTarget> m_TargetsList = new List<TTarget>();
        private List<TTarget> TargetsList { get { return m_TargetsList; } }
        private readonly object SyncRoot = new object();
        private int Robin = 0;
        public RoundRobinScheduler(TKey key)
        {
            m_Key = key;
        }

        public int Count
        {
            get { return m_TargetsList.Count; }
        }
        public void RegisterTarget(TTarget target, ReaderWriterLockSlim lockInstance = null)
        {
            if (lockInstance == null)
            {
                if (!Targets.Contains(target))
                {
                    Targets.Add(target);
                    TargetsList.Clear();
                    TargetsList.AddRange(Targets);
                }
            }
            else
            {
                lockInstance.EnterWriteLock();
                try
                {
                    if (!Targets.Contains(target))
                    {
                        Targets.Add(target);
                        TargetsList.Clear();
                        TargetsList.AddRange(Targets);
                    }
                }
                finally
                {
                    lockInstance.ExitWriteLock();
                }
            }
        }

        public void DeregisterTarget(TTarget target, ReaderWriterLockSlim lockInstance = null)
        {
            if (lockInstance == null)
            {
                if (Targets.Contains(target))
                {
                    Targets.Remove(target);
                    TargetsList.Clear();
                    TargetsList.AddRange(Targets);
                }
            }
            else
            {
                lockInstance.EnterWriteLock();
                try
                {
                    if (Targets.Contains(target))
                    {
                        Targets.Remove(target);
                        TargetsList.Clear();
                        TargetsList.AddRange(Targets);
                    }
                }
                finally
                {
                    lockInstance.ExitWriteLock();
                }
            }
        }

        public bool NextTarget(out TTarget target, ReaderWriterLockSlim lockInstance = null)
        {
            if (lockInstance == null)
            {
                if (TargetsList.Count == 0)
                {
                    target = default(TTarget);
                    return false;
                }
                else
                {
                    int nextID = Interlocked.Increment(ref Robin);
                    if (nextID > TargetsList.Count)
                    {
                        lock(SyncRoot)
                        {
                            nextID = Robin;
                            if (nextID > TargetsList.Count)
                            {
                                Interlocked.Exchange(ref Robin, -1);
                            }
                        }
                        nextID = Interlocked.Increment(ref Robin);
                    }
                    target = TargetsList[nextID];
                    return true;
                }
            }
            else
            {
                lockInstance.EnterReadLock();
                try
                {
                    if (TargetsList.Count == 0)
                    {
                        target = default(TTarget);
                        return false;
                    }
                    else
                    {
                        int nextID = Interlocked.Increment(ref Robin);
                        if (nextID > TargetsList.Count)
                        {
                            lock (SyncRoot)
                            {
                                nextID = Robin;
                                if (nextID > TargetsList.Count)
                                {
                                    Interlocked.Exchange(ref Robin, -1);
                                }
                            }
                            nextID = Interlocked.Increment(ref Robin);
                        }
                        target = TargetsList[nextID];
                        return true;
                    }
                }
                finally
                {
                    lockInstance.ExitReadLock();
                }
            }
        }
    }
}
