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

using ChillX.Core.Helpers;
using ChillX.MQServer.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer.Service
{
    public interface IMQServiceModule : IDisposable
    {
        int ModuleType { get; }
        IEnumerable<int> CreateServiceFunctionList();
        WorkItemBaseCore ProcessWorkItem(WorkItemBaseCore workItem);
    }
    public abstract class MQServiceModuleBase<TFunctionEnum> : IMQServiceModule
        where TFunctionEnum : Enum, IComparable, IFormattable, IConvertible
    {
        public abstract int ModuleType { get; }

        public abstract IEnumerable<int> CreateServiceFunctionList();

        public WorkItemBaseCore ProcessWorkItem(WorkItemBaseCore workItem)
        {
            return ProcessWorkItem(TypeCaster<int,TFunctionEnum>.Convert(workItem.DestinationServiceFunction), workItem);
        }
        protected abstract WorkItemBaseCore ProcessWorkItem(TFunctionEnum functionType, WorkItemBaseCore workItem);

        private bool m_IsDisposed = false;
        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                DoDispose(true);
            }
        }

        private void DoDispose(bool isDisposing)
        {
            if (isDisposing)
            {
                OnDispose();
            }
        }
        protected abstract void OnDispose();
    }
}
