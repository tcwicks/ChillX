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
