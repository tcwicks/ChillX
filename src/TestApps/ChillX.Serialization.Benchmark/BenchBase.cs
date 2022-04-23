/*
ChillX Framework Test Application
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

/*
Notice: This bencmark app uses Messagepack purely for performance comparison
 */

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChillX.Serialization.Benchmark
{
    public abstract class BenchBase
    {
        protected abstract bool EnablePublisher { get; }
        protected abstract void Publish();
        private void PublishMethod()
        {
            while (ThreadsIsRunning)
            {
                ThreadsGo.WaitOne();
                if (ThreadsIsRunning)
                {
                    NumPublishers_Inc();
                    NumThreadsRunning_Inc();
                    try
                    {
                        Publish();
                    }
                    finally
                    {
                        NumThreadsRunning_Dec();
                        NumPublishers_Dec();
                        ThreadsGo.Reset();
                    }
                }
                ThreadsComplete.Set();
            }
        }

        private object SyncLock_NumPublishers = new object();
        private int m_NumPublishers = 0;
        private int NumPublishers
        {
            get
            {
                lock (SyncLock_NumPublishers)
                {
                    return m_NumPublishers;
                }
            }
            set
            {
                lock (SyncLock_NumPublishers)
                {
                    m_NumPublishers = value;
                }
            }
        }
        private void NumPublishers_Inc()
        {
            lock (SyncLock_NumPublishers)
            {
                m_NumPublishers++;
            }
        }
        private void NumPublishers_Dec()
        {
            lock (SyncLock_NumPublishers)
            {
                m_NumPublishers--;
            }
        }

        protected abstract bool EnableSubscriber { get; }
        protected abstract void Subscribe();
        protected abstract bool SubscriberHasWork { get; }
        private void SubscribeMethod()
        {
            while (ThreadsIsRunning || SubscriberHasWork)
            {
                NumThreadsRunning_Inc();
                try
                {
                    Subscribe();
                }
                finally
                {
                    NumThreadsRunning_Dec();
                }
            }
        }

        private object NumThreadsRunning_SyncLock = new object();
        private volatile int m_NumThreadsRunning = 0;
        public int NumThreadsRunning
        {
            get
            {
                lock (NumThreadsRunning_SyncLock) { return m_NumThreadsRunning; }
            }
        }
        private void NumThreadsRunning_Inc()
        {
            lock (NumThreadsRunning_SyncLock)
            {
                Interlocked.Increment(ref m_NumThreadsRunning);
            }
        }
        private void NumThreadsRunning_Dec()
        {
            lock (NumThreadsRunning_SyncLock)
            {
                Interlocked.Decrement(ref m_NumThreadsRunning);
            }
        }

        private ManualResetEvent ThreadsGo = new ManualResetEvent(false);
        private ManualResetEvent ThreadsComplete = new ManualResetEvent(false);
        protected List<Thread> PublisherThreadsList { get; } = new List<Thread>();
        protected List<Thread> SubscriberThreadsList { get; } = new List<Thread>();

        protected object SyncRoot = new object();
        protected volatile bool ThreadsIsRunning = false;
        protected void ThreadStartup()
        {
            lock (SyncRoot)
            {
                ThreadsIsRunning = false;
            }
            ThreadsGo.Set();
            foreach (Thread T in PublisherThreadsList)
            {
                T.Join();
            }
            foreach (Thread T in SubscriberThreadsList)
            {
                T.Join();
            }
            PublisherThreadsList.Clear();
            SubscriberThreadsList.Clear();
            ThreadsGo.Reset();

            lock (SyncRoot)
            {
                ThreadsIsRunning = true;
            }
            for (int I = 0; I < numThreads; I++)
            {
                Thread T;
                if (EnablePublisher)
                {
                    T = new Thread(new ThreadStart(PublishMethod));
                    PublisherThreadsList.Add(T);
                    T.Start();
                }
                if (EnableSubscriber)
                {
                    T = new Thread(new ThreadStart(SubscribeMethod));
                    SubscriberThreadsList.Add(T);
                    T.Start();
                }
            }
        }

        protected void ThreadRunOneItteration()
        {
            ThreadsComplete.Reset();
            ThreadsGo.Set();
            ThreadsComplete.WaitOne();
            while (SubscriberHasWork || (NumPublishers > 0))
            {
                Thread.Sleep(0);
            }
        }

        protected void ThreadShutdown()
        {
            lock (SyncRoot)
            {
                ThreadsIsRunning = false;
            }
            ThreadsGo.Set();
            if (EnablePublisher)
            {
                foreach (Thread T in PublisherThreadsList)
                {
                    T.Join();
                }
                foreach (Thread T in SubscriberThreadsList)
                {
                    T.Join();
                }
                PublisherThreadsList.Clear();
                SubscriberThreadsList.Clear();

            }
            else if (EnableSubscriber)
            {
                foreach (Thread T in SubscriberThreadsList)
                {
                    T.Join();
                }
                SubscriberThreadsList.Clear();
            }
            ThreadsGo.Reset();
        }

        public abstract int numThreads { get; set; }

        protected abstract void OnGlobalSetup();

        [GlobalSetup]
        public void GlobalSetup()
        {
            OnGlobalSetup();
            ThreadStartup();
        }

        protected abstract void OnGlobalCleanup();

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            lock (SyncRoot)
            {
                ThreadsIsRunning = false;
            }
            OnGlobalCleanup();
            ThreadShutdown();
        }

    }

}
