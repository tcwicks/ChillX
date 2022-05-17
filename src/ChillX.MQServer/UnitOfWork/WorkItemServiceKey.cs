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

using ChillX.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer.UnitOfWork
{

    public class WorkItemServiceKey : IEqualityComparer<WorkItemServiceKey>
    {
        public const int MaxServiceTypes = 1000;
        public const int MaxServiceModules = 1000;
        public const int MaxServiceFunctions = 1000;
        //                                             2147 483 647
        public const int ServiceTypeShift = 1000000; //   1 000 000;
        public const int ServiceModuleShift = 1000;  //       1 000;
        public WorkItemServiceKey(int _serviceType, int _serviceModule, int _serviceFunction)
        {
            ServiceType = _serviceType;
            ServiceModule = _serviceModule;
            ServiceFunction = _serviceFunction;
        }

        private int m_ServiceType = 1;
        public int ServiceType
        {
            get
            {
                return m_ServiceType;
            }
            set
            {
                value = Math.Min(Math.Max(value, 1), MaxServiceTypes);
                m_ServiceType = value;
            }
        }
        private int m_ServiceModule = 1;
        public int ServiceModule
        {
            get
            {
                return m_ServiceModule;
            }
            set
            {
                value = Math.Min(Math.Max(value, 1), MaxServiceModules);
                m_ServiceModule = value;
            }
        }
        private int m_ServiceFunction = 1;
        public int ServiceFunction
        {
            get
            {
                return m_ServiceFunction;
            }
            set
            {
                value = Math.Min(Math.Max(value, 1), MaxServiceFunctions);
                m_ServiceFunction = value;
            }
        }
        public int UniqueKey()
        {
            return (ServiceType * ServiceTypeShift) + (ServiceModule * ServiceModuleShift) + ServiceFunction;
        }

        public override int GetHashCode()
        {
            return UniqueKey().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            WorkItemServiceKey other = obj as WorkItemServiceKey;
            if (other == null)
            {
                return base.Equals(obj);
            }
            return (ServiceType == other.ServiceType) && (ServiceModule == other.ServiceModule) && (ServiceFunction == other.ServiceFunction);
        }

        public bool Equals(WorkItemServiceKey x, WorkItemServiceKey y)
        {
            if (x == null && y == null) { return true; }
            if (x == null || y == null) { return false; }
            return (x.ServiceType == y.ServiceType) && (x.ServiceModule == y.ServiceModule) && (x.ServiceFunction == y.ServiceFunction);
        }

        public int GetHashCode(WorkItemServiceKey obj)
        {
            return obj.GetHashCode();
        }

        public static int CreateKey(int _serviceType, int _serviceModule, int _serviceFunction)
        {
            return (Math.Min(Math.Max(_serviceType, 1), MaxServiceTypes) * ServiceTypeShift) + (Math.Min(Math.Max(_serviceModule, 1), MaxServiceModules) * ServiceModuleShift) + Math.Min(Math.Max(_serviceFunction, 1), MaxServiceFunctions);
        }

        internal static int CreateKeyUnChecked(int _serviceType, int _serviceModule, int _serviceFunction)
        {
            return (_serviceType * ServiceTypeShift) + (_serviceModule * ServiceModuleShift) + _serviceFunction;
        }

    }

}
