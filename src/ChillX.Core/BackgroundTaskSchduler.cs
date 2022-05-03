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
using System.Threading;

namespace ChillX.Core
{
    public static class BackgroundTaskSchduler
    {
        private class CallbackDetail
        {
            public CallbackDetail(WaitCallback _CallBack, int _NumSecondsPerCall)
            {
                CallBack = _CallBack;
                NumSecondsPerCall = _NumSecondsPerCall;
                Countdown = _NumSecondsPerCall;
            }
            public int NumSecondsPerCall;
            public int Countdown;
            public WaitCallback CallBack;
        }
        public const double MinimumPollFrequenceSeconds = 4d;
        private static Dictionary<WaitCallback, CallbackDetail> TaskList = new Dictionary<WaitCallback, CallbackDetail>();

        private static System.Threading.Timer TaskTimer = TaskTimerCreate();
        private static ReaderWriterLockSlim TaskLock = new ReaderWriterLockSlim();
        private static int ProcessExited = 0;
        private static System.Threading.Timer TaskTimerCreate()
        {
            System.Diagnostics.Process currentProcess;
            currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            currentProcess.EnableRaisingEvents = true;
            //currentProcess.Exited += CurrentProcess_Exited;
            return new System.Threading.Timer(RunTasks, new object(), 1000, 1000);
        }

        private static void CurrentProcess_Exited(object sender, EventArgs e)
        {
            TaskTimer.Dispose();
            TaskLock.Dispose();
            Interlocked.Exchange(ref ProcessExited, 1);
            Dictionary<WaitCallback, CallbackDetail> EmptyList = new Dictionary<WaitCallback, CallbackDetail>();
            EmptyList = Interlocked.Exchange(ref TaskList, EmptyList);
            EmptyList.Clear();
        }

        private static void RunTasks(object argument)
        {
            List<CallbackDetail> tasks = new List<CallbackDetail>();
            TaskLock.EnterReadLock();
            try
            {
                if (ProcessExited != 0) { return; }
                tasks.AddRange(TaskList.Values);
            }
            finally
            {
                TaskLock.ExitReadLock();
            }
            foreach (CallbackDetail task in tasks)
            {
                if (task.Countdown == 0)
                {
                    task.Countdown = task.NumSecondsPerCall;
                    ThreadPool.QueueUserWorkItem(task.CallBack);
                }
                else
                {
                    task.Countdown -= 1;
                }
            }
        }

        public static void Schedule(WaitCallback handler, int numSecondsPerCall)
        {
            TaskLock.EnterWriteLock();
            try
            {
                if (ProcessExited != 0) { return; }
                TaskList.Add(handler, new CallbackDetail(handler, numSecondsPerCall));
            }
            finally
            {
                TaskLock.ExitWriteLock();
            }
        }
        public static void Cancel(WaitCallback handler)
        {
            TaskLock.EnterWriteLock();
            try
            {
                if (ProcessExited != 0) { return; }
                TaskList.Remove(handler);
            }
            finally
            {
                TaskLock.ExitWriteLock();
            }
        }
    }
}
