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
