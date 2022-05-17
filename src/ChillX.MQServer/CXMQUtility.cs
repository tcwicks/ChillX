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

using ChillX.Core.Structures;
using ChillX.MQServer.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer
{
    internal static class CXMQUtility
    {


        private static ThreadsafeCounter m_WorkItemIDCounter = new ThreadsafeCounter(0, UnitOfWork.WorkItemServiceKey.ServiceTypeShift - 1);
        public static int WorkItemNextID(int ServiceTypeID)
        {
            return m_WorkItemIDCounter.NextID() + (ServiceTypeID * UnitOfWork.WorkItemServiceKey.ServiceTypeShift);
        }

        private static ThreadSafeUniqueIDPool m_ConnectionUniqueID = new ThreadSafeUniqueIDPool(0, int.MaxValue - 2);
        public static int ConnectionUniqueIDNext()
        {
            return m_ConnectionUniqueID.NextID();
        }
        public static void ConnectionUniqueIDReturn(int _iD)
        {
            m_ConnectionUniqueID.ReturnID(_iD);
        }

        private static ThreadsafeCounter m_PingPongIDCounter = new ThreadsafeCounter(0, int.MaxValue - 2);
        public static int PingPongNextID()
        {
            return m_PingPongIDCounter.NextID();
        }

        public enum LogSeverity
        {
            info = 0,
            warning = 1,
            error = 2,
            fatal = 3,
        }

        public static void LogEntry(this LogSeverity _severity, string _message)
        {
            Log(_severity, _message);
        }

        public static void LogEntry(this string _message, LogSeverity _severity)
        {
            Log(_severity, _message);
        }

        public static void Log(LogSeverity _severity, string _message)
        {

        }

        public static int SourceUniqueKey<TPriorityEnum>(this IWorkItem target)
            where TPriorityEnum : struct, IComparable, IFormattable, IConvertible
        {
            return WorkItemServiceKey.CreateKeyUnChecked(target.SourceServiceType, target.SourceServiceModule, target.SourceServiceFunction);
        }

        public static int DestinationUniqueKey<TPriorityEnum>(this IWorkItem target)
            where TPriorityEnum : struct, IComparable, IFormattable, IConvertible
        {
            return WorkItemServiceKey.CreateKeyUnChecked(target.DestinationServiceType, target.DestinationServiceModule, target.DestinationServiceFunction);
        }

        public static int SourceUniqueKey<TPriorityEnum>(this WorkItemBaseCore target)
            where TPriorityEnum : struct, IComparable, IFormattable, IConvertible
        {
            return WorkItemServiceKey.CreateKeyUnChecked(target.SourceServiceType, target.SourceServiceModule, target.SourceServiceFunction);
        }

        public static int DestinationUniqueKey<TPriorityEnum>(this WorkItemBaseCore target)
            where TPriorityEnum : struct, IComparable, IFormattable, IConvertible
        {
            return WorkItemServiceKey.CreateKeyUnChecked(target.DestinationServiceType, target.DestinationServiceModule, target.DestinationServiceFunction);
        }
    }
}
