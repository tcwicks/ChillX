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

using ChillX.MQServer.Server.SystemMessage;
using ChillX.MQServer.Service;
using ChillX.MQServer.Transport;
using ChillX.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ChillX.Core.Structures;
using ChillX.MQServer.UnitOfWork;
using ChillX.Serialization;
using ChillX.Core.Network;
using ChillX.Core.Schedulers;

namespace ChillX.MQServer.Server
{
    internal class Dispatcher
    {
        private int m_OriginID;
        private int OriginID { get { return m_OriginID; } }
        private double m_DroppedConnectionRetryTimeoutSeconds;
        private double DroppedConnectionRetryTimeoutSeconds { get { return m_DroppedConnectionRetryTimeoutSeconds; } }
        private bool m_LockFreeCopyAndSwap;
        private bool LockFreeCopyAndSwap { get { return m_LockFreeCopyAndSwap; } }

        /// <summary>
        /// Unit of work dispatcher
        /// </summary>
        /// <param name="_lockFreeCopyAndSwap">Single Writer multiple reader pattern.
        /// Only set this to true if using a single writer thread which will call:
        /// <see cref="AddConnection(ConnectionTCPSocket{MQPriority}, UOWDiscovery)"/>, <see cref="DropConnection(ConnectionTCPSocket{MQPriority})"/> etc...</param>
        /// <param name="_droppedConnectionRetryTimeoutSeconds">Number of seconds to keep messages from dropped connections</param>
        public Dispatcher(int originID, double droppedConnectionRetryTimeoutSeconds, bool lockFreeCopySwap)
        {
            m_OriginID = originID;
            m_DroppedConnectionRetryTimeoutSeconds = droppedConnectionRetryTimeoutSeconds;
            m_LockFreeCopyAndSwap = lockFreeCopySwap;
        }

        private Dictionary<int, RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>>> m_PathOriginConnectionDict = new Dictionary<int, RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>>>();
        private Dictionary<int, RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>>> PathOriginConnectionDict { get { return m_PathOriginConnectionDict; } }


        private Dictionary<int, RoundRobinScheduler<int, int>> m_PathFunctionOriginDict = new Dictionary<int, RoundRobinScheduler<int, int>>();
        private Dictionary<int, RoundRobinScheduler<int, int>> PathFunctionOriginDict { get { return m_PathFunctionOriginDict; } }


        private Dictionary<ConnectionTCPSocket<MQPriority>, int> m_PathConnectionOriginDict = new Dictionary<ConnectionTCPSocket<MQPriority>, int>();
        private Dictionary<ConnectionTCPSocket<MQPriority>, int> PathConnectionOriginDict { get { return m_PathConnectionOriginDict; } }


        private Dictionary<ConnectionTCPSocket<MQPriority>, int[]> m_PathConnectionFunctionDict = new Dictionary<ConnectionTCPSocket<MQPriority>, int[]>();
        private Dictionary<ConnectionTCPSocket<MQPriority>, int[]> PathConnectionFunctionDict { get { return m_PathConnectionFunctionDict; } }

        
        private Dictionary<int, ConnectionTCPSocket<MQPriority>> m_ActiveConnectionDict = new Dictionary<int, ConnectionTCPSocket<MQPriority>>();
        private Dictionary<int, ConnectionTCPSocket<MQPriority>> ActiveConnectionDict { get { return m_ActiveConnectionDict; } }


        private Dictionary<int, IMQServiceBase> LocalServiceDict { get; } = new Dictionary<int, IMQServiceBase>();

        private readonly ReaderWriterLockSlim SyncLock = new ReaderWriterLockSlim();

        internal UOWDiscovery DiscoveryData()
        {
            UOWDiscovery response;
            List<int> Keys;
            response = new UOWDiscovery();
            response.OriginID = OriginID;
            Keys = new List<int>();
            if (LockFreeCopyAndSwap)
            {
                Keys.AddRange(LocalServiceDict.Keys);
                Keys.AddRange(PathFunctionOriginDict.Keys);
            }
            else
            {
                SyncLock.EnterReadLock();
                try
                {
                    Keys.AddRange(LocalServiceDict.Keys);
                    Keys.AddRange(PathFunctionOriginDict.Keys);
                }
                finally
                {
                    SyncLock.ExitReadLock();
                }
            }
            response.ServiceKeys = RentedBuffer<int>.Shared.Rent(Keys.Count);
            response.ServiceKeys += Keys.ToArray();
            return response;
        }

        internal bool RegisterLocalService(IMQServiceBase service)
        {
            bool hasFunctions = false;
            if (service.Startup())
            {
                foreach (int key in service.FunctionKeys)
                {
                    hasFunctions = true;
                    if (!LocalServiceDict.ContainsKey(key))
                    {
                        LocalServiceDict.Add(key, service);
                    }
                    else
                    {
                        string.Format(@"Service {0} function key {1} is already registered", service.GetType().FullName, key);
                    }
                }
            }
            return hasFunctions;

            //Access pattern does not require locking
            //SyncLock.EnterWriteLock();
            //try
            //{
            //    foreach (int key in service.FunctionKeys)
            //    {
            //        if (!LocalServiceDict.ContainsKey(key))
            //        {
            //            LocalServiceDict.Add(key, service);
            //        }
            //        else
            //        {
            //            string.Format(@"Service {0} function key {1} is already registered", service.GetType().FullName, key);
            //        }
            //    }
            //}
            //finally
            //{
            //    SyncLock.ExitWriteLock();
            //}
        }

        internal void AddConnection(ConnectionTCPSocket<MQPriority> client, UOWDiscovery functionList)
        {
            RoundRobinScheduler<int, int> schedulerOrigin;
            RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>> schedulerConnection;
            bool hasServiceKeys;
            hasServiceKeys = functionList.ServiceKeys != null && functionList.ServiceKeys.Length > 0;
            if (LockFreeCopyAndSwap)
            {
                Dictionary<int, ConnectionTCPSocket<MQPriority>> Clone_ActiveConnectionDict;
                Dictionary<int, RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>>> Clone_PathOriginConnectionDict;
                Dictionary<int, RoundRobinScheduler<int, int>> Clone_PathFunctionOriginDict;
                Dictionary<ConnectionTCPSocket<MQPriority>, int> Clone_PathConnectionOriginDict;
                Dictionary<ConnectionTCPSocket<MQPriority>, int[]> Clone_PathConnectionFunctionDict;

                Clone_ActiveConnectionDict = new Dictionary<int, ConnectionTCPSocket<MQPriority>>(ActiveConnectionDict);
                Clone_PathOriginConnectionDict = new Dictionary<int, RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>>>(PathOriginConnectionDict);
                Clone_PathFunctionOriginDict = new Dictionary<int, RoundRobinScheduler<int, int>>(PathFunctionOriginDict);
                Clone_PathConnectionOriginDict = new Dictionary<ConnectionTCPSocket<MQPriority>, int>(PathConnectionOriginDict);
                Clone_PathConnectionFunctionDict = new Dictionary<ConnectionTCPSocket<MQPriority>, int[]>(PathConnectionFunctionDict);

                if (Clone_ActiveConnectionDict.ContainsKey(client.UniqueID))
                {
                    string.Format(@"TCP Connection being added to dispatcher already exists. Remote IP: {0}, RemotePort: {1}", client.ConnectionEndPoint.IPAddress, client.ConnectionEndPoint.Listening_Port)
                        .Log(LogSeverity.error);
                    Clone_ActiveConnectionDict[client.UniqueID] = client;
                    Clone_PathConnectionOriginDict[client] = functionList.OriginID;
                    if (hasServiceKeys)
                    {
                        Clone_PathConnectionFunctionDict[client] = functionList.ServiceKeys.BufferSpan.ToArray();
                    }
                }
                else
                {
                    Clone_ActiveConnectionDict.Add(client.UniqueID, client);
                    Clone_PathConnectionOriginDict.Add(client, functionList.OriginID);
                    if (hasServiceKeys)
                    {
                        Clone_PathConnectionFunctionDict.Add(client, functionList.ServiceKeys.BufferSpan.ToArray());
                    }
                }
                if (Clone_PathOriginConnectionDict.TryGetValue(functionList.OriginID, out schedulerConnection))
                {
                    schedulerConnection.RegisterTarget(client);
                }
                else
                {
                    schedulerConnection = new RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>>(functionList.OriginID);
                    schedulerConnection.RegisterTarget(client);
                    Clone_PathOriginConnectionDict.Add(schedulerConnection.Key, schedulerConnection);
                }

                if (hasServiceKeys)
                {
                    foreach (int key in functionList.ServiceKeys.BufferSpan)
                    {
                        if (!LocalServiceDict.ContainsKey(key))
                        {
                            if (Clone_PathFunctionOriginDict.TryGetValue(key, out schedulerOrigin))
                            {
                                schedulerOrigin.RegisterTarget(functionList.OriginID);
                            }
                            else
                            {
                                schedulerOrigin = new RoundRobinScheduler<int, int>(key);
                                schedulerOrigin.RegisterTarget(functionList.OriginID);
                                Clone_PathFunctionOriginDict.Add(schedulerOrigin.Key, schedulerOrigin);
                            }
                        }
                    }
                }
                Interlocked.Exchange(ref m_ActiveConnectionDict, Clone_ActiveConnectionDict).Clear();
                Interlocked.Exchange(ref m_PathOriginConnectionDict, Clone_PathOriginConnectionDict).Clear();
                Interlocked.Exchange(ref m_PathFunctionOriginDict, Clone_PathFunctionOriginDict).Clear();
                Interlocked.Exchange(ref m_PathConnectionOriginDict, Clone_PathConnectionOriginDict).Clear();
                Interlocked.Exchange(ref m_PathConnectionFunctionDict, Clone_PathConnectionFunctionDict).Clear();
            }
            else
            {
                SyncLock.EnterWriteLock();
                try
                {
                    if (ActiveConnectionDict.ContainsKey(client.UniqueID))
                    {
                        string.Format(@"TCP Connection being added to dispatcher already exists. Remote IP: {0}, RemotePort: {1}", client.ConnectionEndPoint.IPAddress, client.ConnectionEndPoint.Listening_Port)
                            .Log(LogSeverity.error);
                        ActiveConnectionDict[client.UniqueID] = client;
                        PathConnectionOriginDict[client] = functionList.OriginID;
                        if (hasServiceKeys)
                        {
                            PathConnectionFunctionDict[client] = functionList.ServiceKeys.BufferSpan.ToArray();
                        }
                    }
                    else
                    {
                        ActiveConnectionDict.Add(client.UniqueID, client);
                        PathConnectionOriginDict.Add(client, functionList.OriginID);
                        if (hasServiceKeys)
                        {
                            PathConnectionFunctionDict.Add(client, functionList.ServiceKeys.BufferSpan.ToArray());
                        }
                    }
                    if (PathOriginConnectionDict.TryGetValue(functionList.OriginID, out schedulerConnection))
                    {
                        schedulerConnection.RegisterTarget(client);
                    }
                    else
                    {
                        schedulerConnection = new RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>>(functionList.OriginID);
                        schedulerConnection.RegisterTarget(client);
                        PathOriginConnectionDict.Add(schedulerConnection.Key, schedulerConnection);
                    }

                    if (hasServiceKeys)
                    {
                        foreach (int key in functionList.ServiceKeys.BufferSpan)
                        {
                            if (PathFunctionOriginDict.TryGetValue(key, out schedulerOrigin))
                            {
                                schedulerOrigin.RegisterTarget(functionList.OriginID);
                            }
                            else
                            {
                                schedulerOrigin = new RoundRobinScheduler<int, int>(key);
                                schedulerOrigin.RegisterTarget(functionList.OriginID);
                                PathFunctionOriginDict.Add(schedulerOrigin.Key, schedulerOrigin);
                            }
                        }
                    }
                }
                finally
                {
                    SyncLock.ExitWriteLock();
                }
            }
        }

        internal void DropConnection(ConnectionTCPSocket<MQPriority> client)
        {
            int[] keys;
            int originID;
            RoundRobinScheduler<int, int> schedulerOrigin;
            RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>> schedulerConnection;
            if (LockFreeCopyAndSwap)
            {
                Dictionary<int, ConnectionTCPSocket<MQPriority>> Clone_ActiveConnectionDict;
                Dictionary<int, RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>>> Clone_PathOriginConnectionDict;
                Dictionary<int, RoundRobinScheduler<int, int>> Clone_PathFunctionOriginDict;
                Dictionary<ConnectionTCPSocket<MQPriority>, int> Clone_PathConnectionOriginDict;
                Dictionary<ConnectionTCPSocket<MQPriority>, int[]> Clone_PathConnectionFunctionDict;


                if (ActiveConnectionDict.ContainsKey(client.UniqueID))
                {
                    Clone_ActiveConnectionDict = new Dictionary<int, ConnectionTCPSocket<MQPriority>>(ActiveConnectionDict);
                    Clone_ActiveConnectionDict.Remove(client.UniqueID);
                    Interlocked.Exchange(ref m_ActiveConnectionDict, Clone_ActiveConnectionDict).Clear();
                }
                if (PathConnectionOriginDict.ContainsKey(client))
                {
                    Clone_PathConnectionOriginDict = new Dictionary<ConnectionTCPSocket<MQPriority>, int>(PathConnectionOriginDict);
                    if (Clone_PathConnectionOriginDict.TryGetValue(client, out originID))
                    {
                        Clone_PathConnectionOriginDict.Remove(client);
                        Clone_PathOriginConnectionDict = new Dictionary<int, RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>>>(PathOriginConnectionDict);
                        Clone_PathConnectionFunctionDict = new Dictionary<ConnectionTCPSocket<MQPriority>, int[]>(PathConnectionFunctionDict);
                        Clone_PathFunctionOriginDict = new Dictionary<int, RoundRobinScheduler<int, int>>(PathFunctionOriginDict);
                        if (Clone_PathOriginConnectionDict.TryGetValue(originID, out schedulerConnection))
                        {
                            schedulerConnection.DeregisterTarget(client);
                            if (schedulerConnection.Count == 0)
                            {
                                Clone_PathOriginConnectionDict.Remove(originID);
                            }
                        }
                        if (Clone_PathConnectionFunctionDict.TryGetValue(client, out keys))
                        {
                            Clone_PathConnectionFunctionDict.Remove(client);
                            foreach (int key in keys)
                            {
                                if (Clone_PathFunctionOriginDict.TryGetValue(key, out schedulerOrigin))
                                {
                                    schedulerOrigin.DeregisterTarget(originID);
                                    if (schedulerOrigin.Count == 0)
                                    {
                                        Clone_PathFunctionOriginDict.Remove(key);
                                    }
                                }
                            }
                        }
                        Interlocked.Exchange(ref m_PathConnectionOriginDict, Clone_PathConnectionOriginDict).Clear();
                        Interlocked.Exchange(ref m_PathOriginConnectionDict, Clone_PathOriginConnectionDict).Clear();
                        Interlocked.Exchange(ref m_PathConnectionFunctionDict, Clone_PathConnectionFunctionDict).Clear();
                        Interlocked.Exchange(ref m_PathFunctionOriginDict, Clone_PathFunctionOriginDict).Clear();
                    }
                }

            }
            else
            {
                SyncLock.EnterWriteLock();
                try
                {
                    ActiveConnectionDict.Remove(client.UniqueID);
                    if (PathConnectionOriginDict.TryGetValue(client, out originID))
                    {
                        PathConnectionOriginDict.Remove(client);
                        if (PathOriginConnectionDict.TryGetValue(originID, out schedulerConnection))
                        {
                            schedulerConnection.DeregisterTarget(client);
                            if (schedulerConnection.Count == 0)
                            {
                                PathOriginConnectionDict.Remove(originID);
                            }
                        }
                        if (PathConnectionFunctionDict.TryGetValue(client, out keys))
                        {
                            PathConnectionFunctionDict.Remove(client);
                            foreach (int key in keys)
                            {
                                if (PathFunctionOriginDict.TryGetValue(key, out schedulerOrigin))
                                {
                                    schedulerOrigin.DeregisterTarget(originID);
                                    if (schedulerOrigin.Count == 0)
                                    {
                                        PathFunctionOriginDict.Remove(key);
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    SyncLock.ExitWriteLock();
                }
            }
        }

        private bool ConnectionForDestination(int destinationServiceKey, out ConnectionTCPSocket<MQPriority> client)
        {
            RoundRobinScheduler<int, int> schedulerOrigin;
            RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>> schedulerConnection;
            int originID;
            bool success;
            if (LockFreeCopyAndSwap)
            {
                if (PathFunctionOriginDict.TryGetValue(destinationServiceKey, out schedulerOrigin))
                {
                    if (schedulerOrigin.NextTarget(out originID))
                    {
                        if (PathOriginConnectionDict.TryGetValue(originID, out schedulerConnection))
                        {
                            if (schedulerConnection.NextTarget(out client))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            client = null;
            return false;
        }
        private bool ConnectionForOrigin(int originID, out ConnectionTCPSocket<MQPriority> client)
        {
            RoundRobinScheduler<int, ConnectionTCPSocket<MQPriority>> schedulerConnection;
            bool success;
            if (LockFreeCopyAndSwap)
            {
                if (PathOriginConnectionDict.TryGetValue(originID, out schedulerConnection))
                {
                    if (schedulerConnection.NextTarget(out client))
                    {
                        return true;
                    }
                }
            }
            client = null;
            return false;
        }

        private ConnectionTCPSocket<MQPriority> ConnectionForID(int uniqueID)
        {
            ConnectionTCPSocket<MQPriority> result;
            if (LockFreeCopyAndSwap)
            {
                if (ActiveConnectionDict.TryGetValue(uniqueID, out result))
                {
                    return result;
                }
            }
            else
            {
                SyncLock.EnterReadLock();
                try
                {
                    if (ActiveConnectionDict.TryGetValue(uniqueID, out result))
                    {
                        return result;
                    }
                }
                finally
                {
                    SyncLock.ExitReadLock();
                }
            }
            return null;
        }

        private ThreadSafeQueue<WorkItemBaseCore> ReadWorkItemQueue { get; } = new ThreadSafeQueue<WorkItemBaseCore>();
        private ThreadSafeQueue<WorkItemBaseCore> ProcessWorkItemQueue { get; } = new ThreadSafeQueue<WorkItemBaseCore>();

        private ThreadSafeQueue<WorkItemBaseCore> SendWorkItemQueue { get; } = new ThreadSafeQueue<WorkItemBaseCore>();
        private void OutboundEnqueue(WorkItemBaseCore workItem)
        {
            workItem.CreationDateAssign(DateTime.UtcNow);
            SendWorkItemQueue.Enqueue(workItem);
        }

        private readonly Dictionary<int, WorkItemBaseCore> PendingRequestDict = new Dictionary<int, WorkItemBaseCore>();
        private readonly Dictionary<int, WorkItemBaseCore> PendingResponseDict = new Dictionary<int, WorkItemBaseCore>();
        private readonly ReaderWriterLockSlim PendingRequestResponseLock = new ReaderWriterLockSlim();

        internal void ScheduleRequest(WorkItemBaseCore workItem)
        {
            WorkItemBaseCore existingWorkItem;
            workItem.AssignOrigin(OriginID);
            workItem.MarkAsRequest();
            if (workItem.ReplyRequested)
            {
                PendingRequestResponseLock.EnterWriteLock();
                try
                {
                    if (PendingRequestDict.TryGetValue(workItem.UniqueID, out existingWorkItem))
                    {
                        PendingRequestDict[workItem.UniqueID] = workItem;
                    }
                    else
                    {
                        existingWorkItem = null;
                        PendingRequestDict.Add(workItem.UniqueID, workItem);
                    }
                }
                finally
                {
                    PendingRequestResponseLock.ExitWriteLock();
                }
                if (existingWorkItem != null)
                {
                    if (!object.ReferenceEquals(existingWorkItem, workItem))
                    {
                        existingWorkItem.Dispose();
                    }
                }
            }
            Dispatch(workItem);
        }
        internal void RecieveResponse(WorkItemBaseCore workItem)
        {
            WorkItemBaseCore existingRequestWorkItem;
            WorkItemBaseCore existingResponseWorkItem;
            if (workItem.IsReply)
            {
                PendingRequestResponseLock.EnterWriteLock();
                try
                {
                    if (PendingRequestDict.TryGetValue(workItem.UniqueID, out existingRequestWorkItem))
                    {
                        PendingRequestDict.Remove(workItem.UniqueID);
                    }
                    if (PendingResponseDict.TryGetValue(workItem.UniqueID, out existingResponseWorkItem))
                    {
                        PendingResponseDict[workItem.UniqueID] = workItem;
                    }
                    else
                    {
                        PendingResponseDict.Add(workItem.UniqueID, workItem);
                    }
                }
                finally
                {
                    PendingRequestResponseLock.ExitWriteLock();
                }
                if (existingRequestWorkItem != null)
                {
                    if (!object.ReferenceEquals(existingRequestWorkItem, workItem))
                    {
                        existingRequestWorkItem.Dispose();
                    }
                }
                if (existingResponseWorkItem != null)
                {
                    if (!object.ReferenceEquals(existingResponseWorkItem, workItem))
                    {
                        existingResponseWorkItem.Dispose();
                    }
                }
            }
        }
        internal bool GetProcessedResponse(int uniqueID, out WorkItemBaseCore responseWorkItem)
        {
            PendingRequestResponseLock.EnterWriteLock();
            try
            {
                if (PendingResponseDict.TryGetValue(uniqueID, out responseWorkItem))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                PendingRequestResponseLock.ExitWriteLock();
            }
        }

        internal void Dispatch(WorkItemBaseCore workItem)
        {
            IMQServiceBase workItemProcessor;
            WorkItemBaseCore processedWorkItem;
            try
            {
                if (workItem.IsReply)
                {
                    if (workItem.OriginID == OriginID)
                    {
                        RecieveResponse(workItem);
                    }
                    else
                    {
                        OutboundEnqueue(workItem);
                    }
                }
                else
                {
                    if (LocalServiceDict.TryGetValue(workItem.DestinationServiceKey, out workItemProcessor))
                    {
                        if (workItem.ReplyRequested)
                        {
                            processedWorkItem = workItemProcessor.ProcessRequest(workItem);
                            if (processedWorkItem != null)
                            {
                                OutboundEnqueue(processedWorkItem);
                                if (!object.ReferenceEquals(processedWorkItem, workItem))
                                {
                                    workItem.Dispose();
                                }
                            }
                            else
                            {
                                string.Format(@"Service {0} returned null response where reply was requested for work item type: {1} - module: {2} - function: {3}", workItemProcessor.GetType().FullName, workItem.DestinationServiceType, workItem.DestinationServiceModule, workItem.DestinationServiceFunction)
                                    .Log(LogSeverity.error);
                                workItem.Dispose();
                            }
                        }
                        else
                        {
                            processedWorkItem = workItemProcessor.ProcessRequest(workItem);
                            if (!object.ReferenceEquals(processedWorkItem, workItem))
                            {
                                processedWorkItem.Dispose();
                            }
                            workItem.Dispose();
                        }
                        ProcessWorkItemQueue.Enqueue(workItem);
                    }
                    else
                    {
                        OutboundEnqueue(workItem);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log(@"Dispatcher Error", LogSeverity.debug);
            }
        }

        public void SendWorkItems()
        {
            Queue<WorkItemBaseCore> pendingQueue;
            WorkItemBaseCore workItem;
            RentedBuffer<byte> buffer;
            ConnectionTCPSocket<MQPriority> client;
            pendingQueue = new Queue<WorkItemBaseCore>();
            SendWorkItemQueue.DeQueue(pendingQueue);
            while (pendingQueue.Count > 0)
            {
                workItem = pendingQueue.Dequeue();
                if (workItem.IsReply)
                {
                    if (ConnectionForOrigin(workItem.OriginID, out client))
                    {
                        client.SendWorkItemQueue.Enqueue(workItem);
                    }
                    else
                    {
                        if (DateTime.UtcNow.Subtract(workItem.CreationDate).TotalSeconds < DroppedConnectionRetryTimeoutSeconds)
                        {
                            SendWorkItemQueue.Enqueue(workItem);
                        }
                        else
                        {
                            workItem.Dispose();
                        }
                    }
                }
                else
                {
                    if (ConnectionForDestination(workItem.DestinationServiceKey, out client))
                    {
                        client.SendWorkItemQueue.Enqueue(workItem);
                    }
                    else
                    {
                        if (ConnectionForOrigin(workItem.OriginID, out client))
                        {
                            client.SendWorkItemQueue.Enqueue(workItem.CreateUnprocessedErrorReply(ResponseStatusCode.TransmissionError, @"Destination service not found"));
                        }
                        else
                        {
                            SendWorkItemQueue.Enqueue(workItem.CreateUnprocessedErrorReply(ResponseStatusCode.TransmissionError, @"Destination service not found"));
                        }
                        workItem.Dispose();
                    }
                }
            }
        }

    }
}
