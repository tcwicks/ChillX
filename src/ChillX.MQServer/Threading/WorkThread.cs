using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;


namespace ChillX.MQServer.Threading
{
    internal class WorkThread<TRequest, TResponse, TClientID, TPriority> : IDisposable
     where TPriority : struct, IComparable, IFormattable, IConvertible
     where TClientID : struct, IComparable, IFormattable, IConvertible
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
                m_WorkerThread.Name = @"ThreadController WorkThread";
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
        //private void DoWork_Old()
        //{
        //    try
        //    {
        //        bool Continue;
        //        Stopwatch SWSleepTimer;
        //        Continue = true;
        //        SWSleepTimer = new Stopwatch();
        //        //For profiling only
        //        //SWActive.Reset();
        //        //SWIdle.Reset();
        //        SWSleepTimer.Reset();
        //        while ((SWSleepTimer.ElapsedMilliseconds < ExitTime) && Continue)
        //        {
        //            //For profiling only
        //            //SWActive.Start();
        //            ThreadWorkItem<TRequest, TResponse, TClientID> workItem;
        //            bool hasRequest;
        //            hasRequest = OnGetNextPendingWorkItem(out workItem);

        //            if (!hasRequest)
        //            {
        //                //For profiling only
        //                //SWActive.Stop();
        //                //SWIdle.Start();
        //                SWSleepTimer.Start();
        //                while (!hasRequest)
        //                {
        //                    //For profiling only
        //                    //SWIdle.Start();


        //                    if (ShouldExit())
        //                    {
        //                        Continue = false;
        //                        break;
        //                    }
        //                    if (SWSleepTimer.ElapsedMilliseconds > 100)
        //                    {
        //                        System.Threading.Thread.Sleep(1);
        //                    }
        //                    else
        //                    {
        //                        System.Threading.Thread.Sleep(0);
        //                    }
        //                    hasRequest = OnGetNextPendingWorkItem(out workItem);
        //                    if (!hasRequest)
        //                    {
        //                        if (SWSleepTimer.ElapsedMilliseconds > ExitTime) { break; }
        //                    }
        //                }
        //                SWSleepTimer.Stop();

        //                //For profiling only
        //                //SWIdle.Stop();
        //                //SWActive.Start();
        //            }
        //            if (hasRequest)
        //            {
        //                SWSleepTimer.Reset();
        //                try
        //                {
        //                    workItem.Response = OnProcessRequest(workItem.Request);
        //                }
        //                catch (Exception ex2)
        //                {
        //                    try
        //                    {
        //                        workItem.Response = default(TResponse);
        //                        workItem.ErrorException = ex2;
        //                        workItem.IsError = true;
        //                        OnRequestProcessed(workItem);
        //                        OnLogError(new Exception(@"Error calling OnProcessRequest() handler for work item request. See inner exception.", ex2));
        //                        try
        //                        {

        //                        }
        //                        catch (Exception ex3)
        //                        {
        //                            OnLogError(new Exception(@"Unknown error In work thread controller. See inner exception.", ex3));
        //                        }
        //                    }
        //                    catch
        //                    {

        //                    }
        //                }
        //                try
        //                {
        //                    OnRequestProcessed(workItem);
        //                }
        //                catch (Exception ex2)
        //                {
        //                    try
        //                    {
        //                        OnLogError(new Exception(@"Error int OnRequestProcessed() handler for work item request. See inner exception.", ex2));
        //                    }
        //                    catch
        //                    {

        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex2)
        //    {
        //        try
        //        {
        //            OnLogError(new Exception(@"Unknown error In work thread controller. See inner exception.", ex2));
        //        }
        //        catch
        //        {

        //        }
        //    }
        //    finally
        //    {
        //        lock (SyncRoot)
        //        {
        //            m_IsRunning = false;
        //        }
        //        OnThreadExit(ID);
        //    }
        //    //For profiling only
        //    //SWIdle.Stop();
        //    //SWActive.Stop();
        //    //Console.WriteLine(@"Thread Controller Thread Exited : Active: {0} - Idle: {1}", SWActive.Elapsed.ToString(), SWIdle.Elapsed.ToString());
        //    Dispose();
        //}

        private void DoWork()
        {
            try
            {
                bool Continue;
                Stopwatch SWSleepTimer;
                int IdleCount;
                Continue = true;
                SWSleepTimer = new Stopwatch();
                SWSleepTimer.Reset();
                IdleCount = 0;
                while (Continue)
                {
                    ThreadWorkItem<TRequest, TResponse, TClientID> workItem;
                    bool hasRequest;
                    hasRequest = OnGetNextPendingWorkItem(out workItem);

                    if (!hasRequest)
                    {
                        SWSleepTimer.Start();
                        while (!hasRequest)
                        {
                            IdleCount += 1;

                            if (ShouldExit())
                            {
                                Continue = false;
                                break;
                            }
                            if (IdleCount > 10)
                            {
                                System.Threading.Thread.Sleep(1);
                            }
                            hasRequest = OnGetNextPendingWorkItem(out workItem);
                            if (!hasRequest)
                            {
                                if (SWSleepTimer.ElapsedMilliseconds > ExitTime) { break; }
                            }
                        }
                        SWSleepTimer.Stop();

                    }
                    if (hasRequest)
                    {
                        IdleCount = 0;
                        SWSleepTimer.Reset();
                        try
                        {
                            workItem.Response = OnProcessRequest(workItem.Request);
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
