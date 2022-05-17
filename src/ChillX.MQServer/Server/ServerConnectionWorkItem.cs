using ChillX.MQServer.Transport;
using ChillX.MQServer.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer.Server
{
    internal class ServerConnectionWorkItem : IDisposable
    {
        public ServerConnectionWorkItem(MQServer _server, ConnectionTCPSocket<MQPriority> _connection, WorkItemBaseCore _workItem)
        {
            Server = _server;
            Connection = _connection;
            WorkItem = _workItem;
            m_IsDisposed = false;
        }

        public MQServer Server;
        public ConnectionTCPSocket<MQPriority> Connection;
        public WorkItemBaseCore WorkItem;

        private bool m_IsDisposed;
        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                if (WorkItem != null)
                {
                    WorkItem.Dispose();
                }
                Server = null;
                Connection = null;
                WorkItem = null;
            }
        }
    }
}
