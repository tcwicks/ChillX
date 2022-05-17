using ChillX.MQServer.UnitOfWork;
using ChillX.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ChillX.Core.Structures;

namespace ChillX.MQServer.Service
{
    public interface IMQServiceBase : IDisposable
    {
        int ServiceType { get; }
        bool IsRunning { get; }
        bool Startup();
        void ShutDown();
        IEnumerable<int> FunctionKeys { get; }
        WorkItemBaseCore ProcessRequest(WorkItemBaseCore workItem);
    }

    public abstract class MQServiceBase : IMQServiceBase
    {
        private readonly object SyncRoot = new object();
        public abstract int ServiceType { get; }

        //Lock Free
        private int m_IsRunning = 0;
        public bool IsRunning 
        {
            get { return m_IsRunning == 1; }
            private set { Interlocked.Exchange(ref m_IsRunning, value ? 1:0 ); } 
        }

        public bool Startup()
        {
            if (!IsRunning)
            {
                try
                {
                    if (OnStartup())
                    {
                        IEnumerable<IMQServiceModule> serviceModuleList = CreateServiceModules();
                        foreach (IMQServiceModule serviceModule in serviceModuleList)
                        {
                            IEnumerable<int> ServiceFunctionList;
                            ServiceFunctionList = serviceModule.CreateServiceFunctionList();
                            foreach (int serviceFunction in ServiceFunctionList)
                            {
                                int serviceKey;
                                serviceKey = WorkItemServiceKey.CreateKey(ServiceType, serviceModule.ModuleType, serviceFunction);
                                if (!ServiceModuleDict.ContainsKey(serviceKey))
                                {
                                    ServiceModuleDict.Add(serviceKey, serviceModule);
                                }
                                else
                                {
                                    string.Format(@"Service type: {0} is using service function type ID {1} twice. Second instance will be ignored", serviceModule.GetType().FullName, serviceFunction.ToString())
                                        .Log(LogSeverity.error);
                                }
                            }
                        }
                        IsRunning = true;
                    }
                }
                catch (Exception ex)
                {
                    ex.Log(String.Format(@"Unknown error starting up service module {0}", this.GetType().FullName), LogSeverity.fatal);
                }
            }
            return IsRunning;
        }

        protected abstract bool OnStartup();

        public void ShutDown()
        {
            if (IsRunning)
            {
                OnShutdown();
                foreach (IMQServiceModule serviceModule in ServiceModuleDict.Values)
                {
                    serviceModule.Dispose();
                }
                ServiceModuleDict.Clear();
                IsRunning = false;
            }
        }

        public abstract void OnShutdown();

        protected abstract IEnumerable<IMQServiceModule> CreateServiceModules();

        private Dictionary<int, IMQServiceModule> ServiceModuleDict { get; } = new Dictionary<int, IMQServiceModule>();

        public IEnumerable<int> FunctionKeys
        {
            get
            {
                return ServiceModuleDict.Keys;
            }
        }

        public WorkItemBaseCore ProcessRequest(WorkItemBaseCore workItem)
        {
            if (workItem.DestinationServiceType != ServiceType)
            {
                string errrorMessage = string.Format(@"Framework Error!!! MQ Service Base recieved work item with wrong service type! Expected: {0} recieved: {1}", ServiceType, workItem.DestinationServiceType);
                errrorMessage.Log(LogSeverity.fatal);
                return workItem.CreateUnprocessedErrorReply(ResponseStatusCode.ProcessingError, errrorMessage);
            }
            IMQServiceModule serviceModule;
            if (!ServiceModuleDict.TryGetValue(workItem.DestinationServiceKey, out serviceModule))
            {
                string errrorMessage = string.Format(@"Framework Error!!! MQ Service Base recieved work item with unregistered service module or function! recieved: {0}.{1}", workItem.DestinationServiceModule, workItem.DestinationServiceFunction);
                errrorMessage.Log(LogSeverity.fatal);
                return workItem.CreateUnprocessedErrorReply(ResponseStatusCode.ProcessingError, errrorMessage);
            }
            return serviceModule.ProcessWorkItem(workItem);
        }

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
                if (IsRunning)
                {
                    ShutDown();
                }
                OnDispose();
            }
        }
        protected abstract void OnDispose();

    }
}
