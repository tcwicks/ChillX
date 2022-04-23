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
using System.Diagnostics;
using System.Text;

namespace ChillX.Threading.Complete
{
    internal class WorkThread<TRequest, TResponse, TClientID, TPriority> : IDisposable
        where TPriority : struct, IComparable, IFormattable, IConvertible
        where TClientID : IComparable, IConvertible
    {
        private static object SyncRoot { get; } = new object();

        public int ID { get; } = IdentitySequence.NextID();

        private ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>.Handler_GetNextPendingWorkItem OnGetNextPendingWorkItem;
        private ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>.Handler_ProcessRequest OnProcessRequest;
        private ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>.Handler_OnRequestProcessed OnRequestProcessed;
        private ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>.Handler_OnThreadExit OnThreadExit;
        private ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>.Handler_LogError OnLogError;

        private int ExitTime;
        public WorkThread(ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>.Handler_GetNextPendingWorkItem _OnGetNextPendingWorkItem
            , ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>.Handler_ProcessRequest _OnProcessRequest
            , ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>.Handler_OnRequestProcessed _OnRequestProcessed
            , ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>.Handler_OnThreadExit _OnThreadExit
            , ThreadedWorkItemProcessor<TRequest, TResponse, TClientID, TPriority>.Handler_LogError _OnLogError
            , int _ExitTime = 100)
        {
            OnGetNextPendingWorkItem = _OnGetNextPendingWorkItem;
            OnProcessRequest = _OnProcessRequest;
            OnRequestProcessed = _OnRequestProcessed;
            OnThreadExit = _OnThreadExit;
            ExitTime = Math.Max(_ExitTime, 1000);
        }

        private System.Threading.Thread m_WorkerThread;
        public System.Threading.Thread WorkerThread
        {
            get
            {
                lock (SyncRoot)
                {
                    return m_WorkerThread;
                }
            }
        }
        public void Start()
        {
            lock (SyncRoot)
            {
                m_WorkerThread = new System.Threading.Thread(new System.Threading.ThreadStart(DoWork));
                m_WorkerThread.Start();
                m_IsRunning = true;
            }
        }
        private bool m_IsRunning = false;
        public bool IsRunning
        {
            get
            {
                lock (SyncRoot)
                {
                    return m_IsRunning && m_WorkerThread.IsAlive;
                }
            }
        }

        public bool IsAlive
        {
            get
            {
                lock (SyncRoot)
                {
                    return m_WorkerThread.IsAlive;
                }
            }
        }

        private bool m_ExitSignal = false;
        public void Exit()
        {
            lock (SyncRoot)
            {
                m_ExitSignal = true;
            }
        }
        private bool ShouldExit()
        {
            lock (SyncRoot)
            {
                if (m_ExitSignal)
                {
                    return true;
                }
            }
            return false;
        }
        //For profiling only
        //public Stopwatch SWActive { get; } = new Stopwatch();
        //public Stopwatch SWIdle { get; } = new Stopwatch();
        private void DoWork()
        {
            try
            {
                bool Continue;
                Stopwatch SWSleepTimer;
                Continue = true;
                SWSleepTimer = new Stopwatch();
                //For profiling only
                //SWActive.Reset();
                //SWIdle.Reset();
                SWSleepTimer.Reset();
                while ((SWSleepTimer.ElapsedMilliseconds < ExitTime) && Continue)
                {
                    //For profiling only
                    //SWActive.Start();
                    ThreadWorkItem<TRequest, TResponse, TClientID> workItem;
                    bool hasRequest;
                    hasRequest = OnGetNextPendingWorkItem(out workItem);

                    if (!hasRequest)
                    {
                        //For profiling only
                        //SWActive.Stop();
                        //SWIdle.Start();
                        SWSleepTimer.Start();
                        while (!hasRequest)
                        {
                            //For profiling only
                            //SWIdle.Start();

                            
                            if (ShouldExit())
                            {
                                Continue = false;
                                break;
                            }
                            if (SWSleepTimer.ElapsedMilliseconds > 10)
                            {
                                System.Threading.Thread.Sleep(1);
                            }
                            else
                            {
                                System.Threading.Thread.Sleep(0);
                            }
                            hasRequest = OnGetNextPendingWorkItem(out workItem);
                            if (!hasRequest)
                            {
                                if (SWSleepTimer.ElapsedMilliseconds > ExitTime) { break; }
                            }
                        }
                        SWSleepTimer.Stop();

                        //For profiling only
                        //SWIdle.Stop();
                        //SWActive.Start();
                    }
                    if (hasRequest)
                    {
                        SWSleepTimer.Reset();
                        try
                        {
                            workItem.Response = OnProcessRequest(workItem);
                        }
                        catch (Exception ex2)
                        {
                            try
                            {
                                workItem.Response = default(TResponse);
                                workItem.ErrorException = ex2;
                                workItem.IsError = true;
                                OnRequestProcessed(workItem);
                                OnLogError(new Exception(@"Error calling OnProcessRequest() handler for work item request. See inner exception.", ex2));
                                try
                                {

                                }
                                catch (Exception ex3)
                                {
                                    OnLogError(new Exception(@"Unknown error In work thread controller. See inner exception.", ex3));
                                }
                            }
                            catch
                            {

                            }
                        }
                        try
                        {
                            OnRequestProcessed(workItem);
                        }
                        catch (Exception ex2)
                        {
                            try
                            {
                                OnLogError(new Exception(@"Error int OnRequestProcessed() handler for work item request. See inner exception.", ex2));
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    OnLogError(new Exception(@"Unknown error In work thread controller. See inner exception.", ex));
                }
                catch
                {

                }
            }
            finally
            {
                lock (SyncRoot)
                {
                    m_IsRunning = false;
                }
                OnThreadExit(ID);
            }
            //For profiling only
            //SWIdle.Stop();
            //SWActive.Stop();
            //Console.WriteLine(@"Thread Controller Thread Exited : Active: {0} - Idle: {1}", SWActive.Elapsed.ToString(), SWIdle.Elapsed.ToString());
            Dispose();
        }

        private bool IsDisposed = false;
        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                OnGetNextPendingWorkItem = null;
                OnProcessRequest = null;
                OnRequestProcessed = null;
                OnThreadExit = null;
                OnLogError = null;
                m_WorkerThread = null;
                GC.SuppressFinalize(this);
            }
        }

    }
}
