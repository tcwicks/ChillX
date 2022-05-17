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

using ChillX.Core.Network;
using ChillX.Core.Structures;
using ChillX.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChillX.MQServer.UnitOfWork
{

    [SerializedEntity(0)]
    [Serializable]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public class WorkItemBaseCore : IWorkItem, IEqualityComparer<WorkItemBaseCore>
    {
        public WorkItemBaseCore()
        {

        }
        public WorkItemBaseCore(RentedBuffer<byte> buffer)
        {
            Deserialize(buffer);
        }
        public WorkItemBaseCore(byte[] buffer)
        {
            Deserialize(buffer);
        }
        public WorkItemBaseCore(
            WorkItemBaseCore source
            )
        {
            OriginID = source.OriginID;
            ReplyRequested = source.ReplyRequested;
            IsReply = source.IsReply;
            DestinationServiceType = source.DestinationServiceType;
            DestinationServiceModule = source.DestinationServiceModule;
            DestinationServiceFunction = source.DestinationServiceFunction;
            SourceServiceType = source.SourceServiceType;
            SourceServiceModule = source.SourceServiceModule;
            SourceServiceFunction = source.SourceServiceFunction;
            Priority = source.Priority;
            CreationDate = DateTime.UtcNow;
            ResponseStatus = source.ResponseStatus;
            UniqueID = CXMQUtility.WorkItemNextID(source.DestinationServiceType);
        }

        public WorkItemBaseCore(
            int destinationServiceType,
            int destinationServiceModule,
            int destinationServiceFunction,
            int sourceServiceType,
            int sourceServiceModule,
            int sourceServiceFunction,
            MQPriority priority,
            ResponseStatusCode responseStatus
            )
        {
            DestinationServiceType = destinationServiceType;
            DestinationServiceModule = destinationServiceModule;
            DestinationServiceFunction = destinationServiceFunction;
            SourceServiceType = sourceServiceType;
            SourceServiceModule = sourceServiceModule;
            SourceServiceFunction = sourceServiceFunction;
            Priority = priority;
            CreationDate = DateTime.UtcNow;
            ResponseStatus = responseStatus;
            UniqueID = CXMQUtility.WorkItemNextID(destinationServiceType);
        }

        public bool IsSystemRequest { get { return Priority == MQPriority.System; } }

        #region ISupportUniqueID
        [SerializedMember(0)]
        public int UniqueID { get; private set; }
        public void AssignUniqueID(int newUniqueID)
        {
            UniqueID = newUniqueID;
        }
        #endregion

        #region ISupportPriority<WorkItemPriority>
        [SerializedMember(1)]
        public MQPriority Priority { get; private set; }
        public void AssignPriority(MQPriority priority)
        {
            Priority = priority;
        }
        #endregion

        #region ISupportRouting
        [SerializedMember(2)]
        public bool ReplyRequested { get; private set; } = true;
        public void FireAndForget()
        {
            ReplyRequested = false;
            IsReply = false;
        }
        public void RequestReply()
        {
            ReplyRequested = true;
            IsReply = false;
        }

        [SerializedMember(3)]
        public bool IsReply { get; private set; }
        public void MarkAsReply()
        {
            IsReply = true;
        }

        public void MarkAsRequest()
        {
            IsReply = false;
        }

        [SerializedMember(4)]
        public int OriginID { get; private set; }
        public void AssignOrigin(int originID)
        {
            OriginID = originID;
        }

        [SerializedMember(5)]
        public DateTime CreationDate { get; protected set; }

        [SerializedMember(6)]
        public int DestinationServiceType { get; protected set; }
        [SerializedMember(7)]
        public int DestinationServiceModule { get; protected set; }
        [SerializedMember(8)]
        public int DestinationServiceFunction { get; protected set; }

        private int m_DestinationServiceKey = 0;
        private bool m_DestinationServiceKey_IsSet = false;
        public int DestinationServiceKey
        {
            get
            {
                if (!m_DestinationServiceKey_IsSet)
                {
                    m_DestinationServiceKey = WorkItemServiceKey.CreateKey(DestinationServiceType, DestinationServiceModule, DestinationServiceFunction);
                    m_DestinationServiceKey_IsSet = true;
                }
                return m_DestinationServiceKey;
            }
        }

        [SerializedMember(9)]
        public int SourceServiceType { get; protected set; }
        [SerializedMember(10)]
        public int SourceServiceModule { get; protected set; }
        [SerializedMember(11)]
        public int SourceServiceFunction { get; protected set; }

        private int m_SourceServiceKey = 0;
        private bool m_SourceServiceKey_IsSet = false;
        public int SourceServiceKey
        {
            get
            {
                if (!m_SourceServiceKey_IsSet)
                {
                    m_SourceServiceKey = WorkItemServiceKey.CreateKey(SourceServiceType, SourceServiceModule, SourceServiceFunction);
                    m_SourceServiceKey_IsSet = true;
                }
                return m_SourceServiceKey;
            }
        }

        [SerializedMember(12)]
        public ResponseStatusCode ResponseStatus { get; protected set; }

        [SerializedMember(13)]
        public RentedBuffer<byte> RequestData { get; protected set; } = null;

        [SerializedMember(14)]
        public RentedBuffer<byte> ResponseData { get; protected set; } = null;

        [SerializedMember(15)]
        public string MessageText { get; set; }

        private const int FixedFieldSize = (7 * 4) + 2 + (1 * 8) + (3 * 4);
        public RentedBuffer<byte> SerializeToRentedBuffer()
        {
            int dataSize = FixedFieldSize;
            int MessageTextSize;
            int RequestDataSize;
            int ResponseDataSize;
            PackToBytes();
            if (RequestData != null && RequestData.Length > 0)
            {
                dataSize += RequestData.Length;
                RequestDataSize = RequestData.Length;
            }
            else
            {
                RequestDataSize = 0;
            }
            if (ResponseData != null && ResponseData.Length > 0)
            {
                dataSize += ResponseData.Length;
                ResponseDataSize = ResponseData.Length;
            }
            else
            {
                ResponseDataSize = 0;
            }
            if (string.IsNullOrEmpty(MessageText))
            {
                MessageTextSize = 0;
            }
            else
            {
                MessageTextSize = BitConverterExtended.GetByteCountUTF8String(MessageText);
            }
            RentedBuffer<byte> buffer;
            byte[] bufferData;
            int startIndex = 0;
            buffer = RentedBuffer<byte>.Shared.Rent(dataSize);
            bufferData = buffer._rawBufferInternal;
            startIndex += BitConverterExtended.GetBytes(UniqueID, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes((byte)Priority, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes(CreationDate, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes(DestinationServiceType, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes(DestinationServiceModule, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes(DestinationServiceFunction, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes(SourceServiceType, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes(SourceServiceModule, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes(SourceServiceFunction, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes((byte)ResponseStatus, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes(MessageTextSize, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes(RequestDataSize, bufferData, startIndex);
            startIndex += BitConverterExtended.GetBytes(ResponseDataSize, bufferData, startIndex);
            if (MessageTextSize > 0)
            {
                startIndex += BitConverterExtended.GetBytesUTF8String(MessageText, bufferData, startIndex);
            }
            if (RequestDataSize > 0)
            {
                Array.Copy(RequestData._rawBufferInternal, 0, bufferData, startIndex, RequestData.Length);
                startIndex += RequestData.Length;
            }
            if (ResponseDataSize > 0)
            {
                Array.Copy(ResponseData._rawBufferInternal, 0, bufferData, startIndex, ResponseData.Length);
                startIndex += ResponseData.Length;
            }
            return buffer;
        }
        public byte[] Serialize()
        {
            int dataSize = FixedFieldSize;
            int MessageTextSize;
            int RequestDataSize;
            int ResponseDataSize;
            PackToBytes();
            if (RequestData != null && RequestData.Length > 0)
            {
                dataSize += RequestData.Length;
                RequestDataSize = RequestData.Length;
            }
            else
            {
                RequestDataSize = 0;
            }
            if (ResponseData != null && ResponseData.Length > 0)
            {
                dataSize += ResponseData.Length;
                ResponseDataSize = ResponseData.Length;
            }
            else
            {
                ResponseDataSize = 0;
            }
            if (string.IsNullOrEmpty(MessageText))
            {
                MessageTextSize = 0;
            }
            else
            {
                MessageTextSize = BitConverterExtended.GetByteCountUTF8String(MessageText);
            }
            byte[] buffer;
            int startIndex = 0;
            buffer = new byte[dataSize];
            startIndex += BitConverterExtended.GetBytes(UniqueID, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes((byte)Priority, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes(CreationDate, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes(DestinationServiceType, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes(DestinationServiceModule, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes(DestinationServiceFunction, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes(SourceServiceType, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes(SourceServiceModule, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes(SourceServiceFunction, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes((byte)ResponseStatus, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes(MessageTextSize, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes(RequestDataSize, buffer, startIndex);
            startIndex += BitConverterExtended.GetBytes(ResponseDataSize, buffer, startIndex);
            if (MessageTextSize > 0)
            {
                startIndex += BitConverterExtended.GetBytesUTF8String(MessageText, buffer, startIndex);
            }
            if (RequestDataSize > 0)
            {
                Array.Copy(RequestData._rawBufferInternal, 0, buffer, startIndex, RequestData.Length);
                startIndex += RequestData.Length;
            }
            if (ResponseDataSize > 0)
            {
                Array.Copy(ResponseData._rawBufferInternal, 0, buffer, startIndex, ResponseData.Length);
                startIndex += ResponseData.Length;
            }
            return buffer;
        }
        public void Deserialize(RentedBuffer<byte> buffer)
        {
            int startIndex = 0;
            int MessageTextSize;
            int RequestDataSize;
            int ResponseDataSize;
            byte[] bufferData = buffer._rawBufferInternal;
            UniqueID = BitConverterExtended.ToInt32(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            Priority = (MQPriority)bufferData[startIndex]; startIndex += BitConverterExtended.SizeOfByte;
            CreationDate = BitConverterExtended.ToDateTime(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfDateTime;
            DestinationServiceType = BitConverterExtended.ToInt32(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            DestinationServiceModule = BitConverterExtended.ToInt32(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            DestinationServiceFunction = BitConverterExtended.ToInt32(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            SourceServiceType = BitConverterExtended.ToInt32(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            SourceServiceModule = BitConverterExtended.ToInt32(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            SourceServiceFunction = BitConverterExtended.ToInt32(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            ResponseStatus = (ResponseStatusCode)bufferData[startIndex]; startIndex += BitConverterExtended.SizeOfByte;
            MessageTextSize = BitConverterExtended.ToInt32(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            RequestDataSize = BitConverterExtended.ToInt32(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            ResponseDataSize = BitConverterExtended.ToInt32(bufferData, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            if (MessageTextSize > 0)
            {
                MessageText = BitConverterExtended.ToString(bufferData, startIndex, MessageTextSize);
                startIndex += MessageTextSize;
            }
            else
            {
                MessageText = String.Empty;
            }
            if (RequestDataSize > 0)
            {
                RequestData = RentedBuffer<byte>.Shared.Rent(RequestDataSize);
                this.RentedBufferQueue.Enqueue(RequestData);
                Array.Copy(bufferData, startIndex, RequestData._rawBufferInternal, 0, RequestDataSize);
                startIndex += RequestDataSize;
            }
            else
            {
                RequestData = null;
            }
            if (ResponseDataSize > 0)
            {
                ResponseData = RentedBuffer<byte>.Shared.Rent(ResponseDataSize);
                this.RentedBufferQueue.Enqueue(ResponseData);
                Array.Copy(bufferData, startIndex, ResponseData._rawBufferInternal, 0, ResponseDataSize);
                startIndex += ResponseDataSize;
            }
            else
            {
                ResponseData = null;
            }
        }
        public void Deserialize(byte[] buffer)
        {
            int startIndex = 0;
            int MessageTextSize;
            int RequestDataSize;
            int ResponseDataSize;
            UniqueID = BitConverterExtended.ToInt32(buffer, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            Priority = (MQPriority)buffer[startIndex]; startIndex += BitConverterExtended.SizeOfByte;
            CreationDate = BitConverterExtended.ToDateTime(buffer, startIndex); startIndex += BitConverterExtended.SizeOfDateTime;
            DestinationServiceType = BitConverterExtended.ToInt32(buffer, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            DestinationServiceModule = BitConverterExtended.ToInt32(buffer, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            DestinationServiceFunction = BitConverterExtended.ToInt32(buffer, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            SourceServiceType = BitConverterExtended.ToInt32(buffer, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            SourceServiceModule = BitConverterExtended.ToInt32(buffer, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            SourceServiceFunction = BitConverterExtended.ToInt32(buffer, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            ResponseStatus = (ResponseStatusCode)buffer[startIndex]; startIndex += BitConverterExtended.SizeOfByte;
            MessageTextSize = BitConverterExtended.ToInt32(buffer, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            RequestDataSize = BitConverterExtended.ToInt32(buffer, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            ResponseDataSize = BitConverterExtended.ToInt32(buffer, startIndex); startIndex += BitConverterExtended.SizeOfInt32;
            if (MessageTextSize > 0)
            {
                MessageText = BitConverterExtended.ToString(buffer, startIndex, MessageTextSize);
                startIndex += MessageTextSize;
            }
            else
            {
                MessageText = String.Empty;
            }
            if (RequestDataSize > 0)
            {
                RequestData = RentedBuffer<byte>.Shared.Rent(RequestDataSize);
                this.RentedBufferQueue.Enqueue(RequestData);
                Array.Copy(buffer, startIndex, RequestData._rawBufferInternal, 0, RequestDataSize);
                startIndex += RequestDataSize;
            }
            else
            {
                RequestData = null;
            }
            if (ResponseDataSize > 0)
            {
                ResponseData = RentedBuffer<byte>.Shared.Rent(ResponseDataSize);
                this.RentedBufferQueue.Enqueue(ResponseData);
                Array.Copy(buffer, startIndex, ResponseData._rawBufferInternal, 0, ResponseDataSize);
                startIndex += ResponseDataSize;
            }
            else
            {
                ResponseData = null;
            }
        }

        public ConnectivityEndPoint SourceConnectivityEndPoint { get; internal set; }

        public virtual bool HasRequest { get { throw new NotImplementedException(@"HasRequest{get;} Must be implemented in concrete class. Cannot make this abstract due to serialization issues with abstract classes."); } }

        public virtual bool HasResponse { get { throw new NotImplementedException(@"HasResponse{get;} Must be implemented in concrete class. Cannot make this abstract due to serialization issues with abstract classes."); } }

        public virtual void UnPackBytes() { }
        public virtual void PackToBytes() { }

        public void CreationDateAssign(DateTime _creationDate)
        {
            CreationDate = _creationDate;
        }

        public void DestinationServiceTypeAssign(int _destinationServiceType)
        {
            DestinationServiceType = _destinationServiceType;
        }

        public void DestinationServiceModuleAssign(int _destinationServiceModule)
        {
            DestinationServiceModule = _destinationServiceModule;
        }

        public void DestinationServiceFunctionAssign(int _destinationServiceFunction)
        {
            DestinationServiceFunction = _destinationServiceFunction;
        }

        public void SourceServiceTypeAssign(int _sourceServiceType)
        {
            SourceServiceType = _sourceServiceType;
        }

        public void SourceServiceModuleAssign(int _sourceServiceModule)
        {
            SourceServiceModule = _sourceServiceModule;
        }

        public void SourceServiceFunctionAssign(int _sourceServiceFunction)
        {
            SourceServiceFunction = _sourceServiceFunction;
        }

        public void SourceConnectivityEndPointAssign(ConnectivityEndPoint _connectivityEndPoint)
        {
            SourceConnectivityEndPoint = _connectivityEndPoint;
        }

        public void ResponseStatusAssign(ResponseStatusCode _responseStatus)
        {
            ResponseStatus = _responseStatus;
        }
        #endregion

        public WorkItemBaseCore CreateUnprocessedErrorReply(ResponseStatusCode _responseStatus, string _message = null)
        {
            WorkItemBaseCore Response;
            Response = (WorkItemBaseCore)this.Clone();
            Response.CreationDateAssign(DateTime.UtcNow);
            Response.SourceServiceType = DestinationServiceType;
            Response.SourceServiceModule = DestinationServiceModule;
            Response.SourceServiceFunction = DestinationServiceFunction;
            Response.DestinationServiceType = SourceServiceType;
            Response.DestinationServiceModule = SourceServiceModule;
            Response.DestinationServiceFunction = SourceServiceFunction;
            Response.ResponseStatus = _responseStatus;
            Response.MessageText = _message;
            PackToBytes();
            Response.RequestData = RequestData;
            Response.ResponseData = ResponseData;
            return Response;

        }

        #region IDisposable Members

        private bool m_IsDisposed = false;
        public bool IsDisposed
        {
            get { return m_IsDisposed; }
        }


        protected virtual void DoDispose()
        {
        }

        protected readonly ThreadSafeQueue<RentedBuffer<byte>> RentedBufferQueue = new ThreadSafeQueue<RentedBuffer<byte>>();

        protected static volatile int RentedCounter = 0;

        public void Dispose()
        {
            if (!IsDisposed)
            {
                RentedBuffer<byte> buffer;
                bool success;
                buffer= RentedBufferQueue.DeQueue(out success);
                while (success)
                {
                    buffer.Return();
                    Interlocked.Decrement(ref RentedCounter);
                    buffer = RentedBufferQueue.DeQueue(out success);
                }
                m_IsDisposed = true;
                DoDispose();
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region ICloneable
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion

        #region IEquality

        public bool Equals(WorkItemBaseCore x, WorkItemBaseCore y)
        {
            if (x == null && y == null) { return true; }
            if (x == null || y == null) { return false; }
            return x.UniqueID.Equals(UniqueID) && y.UniqueID.Equals(y.UniqueID);
        }

        public int GetHashCode(WorkItemBaseCore obj)
        {
            return obj.UniqueID.GetHashCode();
        }

        public bool Equals(IWorkItem x, IWorkItem y)
        {
            if (x == null && y == null) { return true; }
            if (x == null || y == null) { return false; }
            return x.UniqueID.Equals(UniqueID) && y.UniqueID.Equals(y.UniqueID);
        }

        public int GetHashCode(IWorkItem obj)
        {
            return obj.UniqueID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            WorkItemBaseCore target = obj as WorkItemBaseCore;
            if (target == null)
            {
                return base.Equals(obj);
            }
            return target.UniqueID.Equals(UniqueID);
        }

        public override int GetHashCode()
        {
            return UniqueID.GetHashCode();
        }
        #endregion
    }
}
