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

namespace ChillX.Threading.Simple
{
    internal class SimpleWorkThread<TRequest, TResponse, TClientID> :IDisposable
        where TRequest : class, new()
        where TResponse : class, new()
        where TClientID : struct, IComparable, IFormattable, IConvertible
    {
        private static object SyncRoot { get; } = new object();
       
        public int ID { get; } = IdentitySequence.NextID();

        private SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>.Handler_GetNextPendingWorkItem OnGetNextPendingWorkItem;
        private SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>.Handler_ProcessRequest OnProcessRequest;
        private SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>.Handler_OnRequestProcessed OnRequestProcessed;
        private SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>.Handler_OnThreadExit OnThreadExit;
        private SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>.Handler_LogError OnLogError;

        private int ExitTime;
        public SimpleWorkThread(SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>.Handler_GetNextPendingWorkItem _OnGetNextPendingWorkItem
            , SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>.Handler_ProcessRequest _OnProcessRequest
            , SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>.Handler_OnRequestProcessed _OnRequestProcessed
            , SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>.Handler_OnThreadExit _OnThreadExit
            , SimpleThreadedWorkItemProcessor<TRequest, TResponse, TClientID>.Handler_LogError _OnLogError
            , int _ExitTime = 1000)
        {
            OnGetNextPendingWorkItem = _OnGetNextPendingWorkItem;
            OnProcessRequest = _OnProcessRequest;
            OnRequestProcessed = _OnRequestProcessed;
            OnThreadExit = _OnThreadExit;
            ExitTime = Math.Max(_ExitTime,1000);
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
                lock(SyncRoot)
                {
                    return m_IsRunning && m_WorkerThread.IsAlive;
                }
            }
        }

        private void DoWork()
        {
            try
            {
                int IdleCounter = 0;
                while (IdleCounter < ExitTime)
                {
                    SimpleThreadedWorkItem<TRequest, TResponse, TClientID> workItem;
                    bool hasRequest;
                    hasRequest = OnGetNextPendingWorkItem(out workItem);
                    int SleepTime;
                    SleepTime = 0;
                    while (!hasRequest)
                    {
                        SleepTime++;
                        if (SleepTime > 10)
                        {
                            SleepTime = 10;
                        }
                        System.Threading.Thread.Sleep(SleepTime);
                        hasRequest = OnGetNextPendingWorkItem(out workItem);
                        if (!hasRequest)
                        {
                            IdleCounter+= SleepTime;
                            if (IdleCounter > ExitTime) { break; }
                        }
                    }
                    if (hasRequest)
                    {
                        try
                        {
                            workItem.Response = OnProcessRequest(workItem.Request);
                            try
                            {
                                OnRequestProcessed(workItem);
                            }
                            catch(Exception ex2)
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
                        catch (Exception ex)
                        {
                            try
                            {
                                OnLogError(new Exception(@"Error calling OnProcessRequest() handler for work item request. See inner exception.", ex));
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            }
            catch (Exception ex2)
            {
                try
                {
                    OnLogError(new Exception(@"Unknown error In work thread controller. See inner exception.", ex2));
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
