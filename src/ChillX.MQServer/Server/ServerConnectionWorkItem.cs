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
