using ChillX.Core.CapabilityInterfaces;
using ChillX.Core.Structures;
using ChillX.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChillX.MQServer.UnitOfWork
{
    [SerializedEntity(1)]
    [Serializable]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public class WorkItemBase<TRequest, TResponse> : WorkItemBaseCore, IWorkItemTyped<TRequest, TResponse>
        where TRequest : new()
        where TResponse : new()
    {
        public WorkItemBase(
            int _destinationServiceType,
            int _destinationServiceModule,
            int _destinationServiceFunction,
            int _sourceServiceType,
            int _sourceServiceModule,
            int _sourceServiceFunction,
            MQPriority _priority = MQPriority.NormalPriority,
            ResponseStatusCode _responseStatus = ResponseStatusCode.Pending
            )
        : base(
              _destinationServiceType,
              _destinationServiceModule,
              _destinationServiceFunction,
              _sourceServiceType,
              _sourceServiceModule,
              _sourceServiceFunction,
              _priority,
              _responseStatus
            )
        {
        }
        public WorkItemBase(WorkItemBaseCore _source)
            : base(
              _source
                 )
        {
            base.AssignUniqueID(_source.UniqueID);
            MessageText = _source.MessageText;
            if (_source.RequestData != null)
            {
                WorkItemDetail<TRequest> requestEntity = new WorkItemDetail<TRequest>();
                if (ChillXSerializer<WorkItemDetail<TRequest>>.Write(requestEntity, _source.RequestData._rawBufferInternal))
                {
                    m_RequestDetail = requestEntity;
                }
                base.RequestData = null;
            }
            if (_source.ResponseData != null)
            {
                WorkItemDetail<TResponse> responseEntity = new WorkItemDetail<TResponse>();
                if (ChillXSerializer<WorkItemDetail<TResponse>>.Write(responseEntity, _source.ResponseData._rawBufferInternal))
                {
                    m_ResponseDetail = responseEntity;
                }
                base.ResponseData = null;
            }
        }

        private WorkItemDetail<TRequest> m_RequestDetail = new WorkItemDetail<TRequest>();
        /// <summary>
        /// This object property will be disposed once the relevant Process Work Item method completes
        /// If you wish to keep a copy of any data in this property beyond the lifecycle of the relevant Process Work Item method
        /// Please create a separate copy of such data
        /// </summary>
        public WorkItemDetail<TRequest> RequestDetail
        {
            get
            {
                if ((m_RequestDetail == null) && (base.RequestData != null))
                {
                    WorkItemDetail<TRequest> requestEntity = new WorkItemDetail<TRequest>();
                    if (ChillXSerializer<WorkItemDetail<TRequest>>.Write(requestEntity, base.RequestData._rawBufferInternal))
                    {
                        m_RequestDetail = requestEntity;
                    }
                }
                return m_RequestDetail;
            }
        }

        private WorkItemDetail<TResponse> m_ResponseDetail = new WorkItemDetail<TResponse>();
        /// <summary>
        /// This object property will be disposed once the relevant Process Work Item method completes
        /// If you wish to keep a copy of any data in this property beyond the lifecycle of the relevant Process Work Item method
        /// Please create a separate copy of such data
        /// </summary>
        public WorkItemDetail<TResponse> ResponseDetail
        {
            get
            {
                if ((m_ResponseDetail == null) && (base.ResponseData != null))
                {
                    WorkItemDetail<TResponse> responseEntity = new WorkItemDetail<TResponse>();
                    if (ChillXSerializer<WorkItemDetail<TResponse>>.Write(responseEntity, base.ResponseData._rawBufferInternal))
                    {
                        m_ResponseDetail = responseEntity;
                    }
                }
                return m_ResponseDetail;
            }
        }

        public override void UnPackBytes()
        {
            if (base.RequestData != null)
            {
                WorkItemDetail<TRequest> requestEntity = new WorkItemDetail<TRequest>();
                if (ChillXSerializer<WorkItemDetail<TRequest>>.Write(requestEntity, base.RequestData._rawBufferInternal))
                {
                    m_RequestDetail = requestEntity;
                }
                base.RequestData = null;
            }
            if (base.ResponseData != null)
            {
                WorkItemDetail<TResponse> responseEntity = new WorkItemDetail<TResponse>();
                if (ChillXSerializer<WorkItemDetail<TResponse>>.Write(responseEntity, base.ResponseData._rawBufferInternal))
                {
                    m_ResponseDetail = responseEntity;
                }
                base.ResponseData = null;
            }
        }

        public override void PackToBytes()
        {
            RentedBuffer<byte> buffer;

            if (base.RequestData == null)
            {
                //buffer = ChillXSerializer<WorkItemDetail<TRequest>>.ReadToRentedBuffer(m_RequestDetail);
                //base.RequestData = buffer._rawBufferInternal;

                Interlocked.Increment(ref RentedCounter);
                buffer = ChillXSerializer<WorkItemDetail<TRequest>>.ReadToRentedBuffer(m_RequestDetail);
                base.RequestData = buffer;
                RentedBufferQueue.Enqueue(buffer);
            }

            if (base.ResponseData == null)
            {
                //buffer = ChillXSerializer<WorkItemDetail<TResponse>>.ReadToRentedBuffer(m_ResponseDetail);
                //Interlocked.Increment(ref RentedCounter);
                //base.ResponseData = buffer._rawBufferInternal;

                Interlocked.Increment(ref RentedCounter);
                buffer = ChillXSerializer<WorkItemDetail<TResponse>>.ReadToRentedBuffer(m_ResponseDetail);
                base.ResponseData = buffer;
                RentedBufferQueue.Enqueue(buffer);
            }

        }

        //Testing purposes only
        //public override void UnPackRequestResponse()
        //{
        //    if (base.RequestData != null)
        //    {
        //        m_RequestDetail = MessagePackSerializer.Deserialize<WorkItemDetail<TRequest>>(base.RequestData, CXMQUtility.SerializationOptions);
        //        base.RequestData = null;
        //    }
        //    if (base.ResponseData != null)
        //    {
        //        m_ResponseDetail = MessagePackSerializer.Deserialize<WorkItemDetail<TResponse>>(base.ResponseData, CXMQUtility.SerializationOptions);
        //    }
        //}

        public override bool HasRequest => RequestDetail.IsValueAssigned;
        public override bool HasResponse => ResponseDetail.IsValueAssigned;

        /// <summary>
        /// Creates a reply WorkItem based on this request and assigns the response data object
        /// </summary>
        /// <param name="_responseData">Response data object. Note: this object will be disposed once transmission is complete.</param>
        /// <param name="_responseStatus">Status of response from target service module function</param>
        /// <param name="_message">Generic Plain text message. Most usefull for sending back processing error exception messages.</param>
        /// <returns>Ready to transmit reply</returns>
        public WorkItemBase<TRequest, TResponse> CreateReply(TResponse _responseData, ResponseStatusCode _responseStatus = ResponseStatusCode.Success, string _message = null)
        {
            WorkItemBase<TRequest, TResponse> Response;
            Response = (WorkItemBase<TRequest, TResponse>)this.Clone();
            Response.MarkAsReply();
            Response.SourceServiceType = DestinationServiceType;
            Response.SourceServiceModule = DestinationServiceModule;
            Response.SourceServiceFunction = DestinationServiceFunction;
            Response.DestinationServiceType = SourceServiceType;
            Response.DestinationServiceModule = SourceServiceModule;
            Response.DestinationServiceFunction = SourceServiceFunction;
            Response.RequestDetail.WorkItemData = RequestDetail.WorkItemData;
            m_RequestIsCopied = true;
            Response.ResponseDetail.WorkItemData = _responseData;
            Response.ResponseStatus = _responseStatus;
            Response.MessageText = _message;
            return Response;
        }

        /// <summary>
        /// Creates a reply WorkItem based on this request and assigns the response data object impersonating a different responding service endpoint
        /// </summary>
        /// <param name="_responseData">Response data object.  Note: this object will be disposed once transmission is complete.</param>
        /// <param name="_fromServiceType">Responding service type to impersonate. Ignored if value <= 0</param>
        /// <param name="_fromServiceModule">Responding service module to impersonate. Ignored if value <= 0</param>
        /// <param name="_fromServiceFunction">Responding service function to impersonate. Ignored if value <= 0</param>
        /// <param name="_responseStatus">Status of response from target service module function</param>
        /// <param name="_message">Generic Plain text message. Most usefull for sending back processing error exception messages.</param>
        /// <returns>Ready to transmit reply</returns>
        public WorkItemBase<TRequest, TResponse> CreateImpersonatedReply(TResponse _responseData, int _fromServiceType = 0, int _fromServiceModule = 0, int _fromServiceFunction = 0,
            ResponseStatusCode _responseStatus = ResponseStatusCode.Success, string _message = null)
        {
            WorkItemBase<TRequest, TResponse> Response;
            Response = (WorkItemBase<TRequest, TResponse>)this.Clone();
            Response.MarkAsReply();
            Response.SourceServiceType = _fromServiceType <= 0 ? DestinationServiceType : _fromServiceType;
            Response.SourceServiceModule = _fromServiceModule <= 0 ? DestinationServiceModule : _fromServiceModule;
            Response.SourceServiceFunction = _fromServiceFunction <= 0 ? DestinationServiceFunction : _fromServiceFunction;
            Response.DestinationServiceType = SourceServiceType;
            Response.DestinationServiceModule = SourceServiceModule;
            Response.DestinationServiceFunction = SourceServiceFunction;
            Response.RequestDetail.WorkItemData = RequestDetail.WorkItemData;
            m_RequestIsCopied = true;
            Response.ResponseDetail.WorkItemData = _responseData;
            Response.ResponseStatus = _responseStatus;
            Response.MessageText = _message;
            return Response;
        }

        /// <summary>
        /// Creates a forwarding WorkItem based on this request
        /// </summary>
        /// <param name="_toServiceType">New destination service type</param>
        /// <param name="_toServiceModule">New destination service module</param>
        /// <param name="_toServiceFunction">New destination service function</param>
        /// <param name="_newRequest">replacement request data object. If null will retain the original request. else will replace the original request. Note: this object will be disposed once transmission is complete.</param>
        /// <param name="_responseStatus">Status of response from target service module function</param>
        /// <param name="_message">Generic Plain text message. Most usefull for sending back processing error exception messages.</param>
        /// <returns></returns>
        public WorkItemBase<TRequest, TResponse> CreateForward(int _toServiceType, int _toServiceModule, int _toServiceFunction, TRequest _newRequest = default(TRequest),
            ResponseStatusCode _responseStatus = ResponseStatusCode.Success, string _message = null)
        {
            WorkItemBase<TRequest, TResponse> Response;
            Response = (WorkItemBase<TRequest, TResponse>)this.Clone();
            Response.DestinationServiceType = _toServiceType;
            Response.DestinationServiceModule = _toServiceModule;
            Response.DestinationServiceFunction = _toServiceFunction;
            Response.ResponseStatus = _responseStatus;
            if (!EqualityComparer<TRequest>.Default.Equals(_newRequest, default(TRequest)))
            {
                Response.RequestDetail.WorkItemData = _newRequest;
            }
            else
            {
                Response.RequestDetail.WorkItemData = RequestDetail.WorkItemData;
                m_RequestIsCopied = true;
            }
            Response.ResponseDetail.WorkItemData = ResponseDetail.WorkItemData;
            m_ResponseIsCopied = true;
            Response.MessageText = _message;
            return Response;
        }

        /// <summary>
        /// Creates a forwarding WorkItem based on this request impersonating a different responding service endpoint
        /// </summary>
        /// <param name="_fromServiceType">New source service type to impersonate</param>
        /// <param name="_fromServiceModule">New source service module to impersonate</param>
        /// <param name="_fromServiceFunction">New source service function to impersonate</param>
        /// <param name="_toServiceType">New destination service type</param>
        /// <param name="_toServiceModule">New destination service module</param>
        /// <param name="_toServiceFunction">New destination service function</param>
        /// <param name="_newRequest">replacement request data object. If null will retain the original request. else will replace the original request. Note: this object will be disposed once transmission is complete.</param>
        /// <param name="_responseStatus">Status of response from target service module function</param>
        /// <param name="_message">Generic Plain text message. Most usefull for sending back processing error exception messages.</param>
        /// <returns></returns>
        public WorkItemBase<TRequest, TResponse> CreateImpersonatedForward(int _fromServiceType, int _fromServiceModule, int _fromServiceFunction, int _toServiceType, int _toServiceModule, int _toServiceFunction, TRequest _newRequest = default(TRequest),
            ResponseStatusCode _responseStatus = ResponseStatusCode.Success, string _message = null)
        {
            WorkItemBase<TRequest, TResponse> Response;
            Response = (WorkItemBase<TRequest, TResponse>)this.Clone();
            Response.SourceServiceType = _fromServiceType;
            Response.SourceServiceModule = _fromServiceModule;
            Response.SourceServiceFunction = _fromServiceFunction;
            Response.DestinationServiceType = _toServiceType;
            Response.DestinationServiceModule = _toServiceModule;
            Response.DestinationServiceFunction = _toServiceFunction;
            Response.ResponseStatus = _responseStatus;
            if (!EqualityComparer<TRequest>.Default.Equals(_newRequest, default(TRequest)))
            {
                Response.RequestDetail.WorkItemData = _newRequest;
            }
            else
            {
                Response.RequestDetail.WorkItemData = RequestDetail.WorkItemData;
                m_RequestIsCopied = true;
            }
            Response.ResponseDetail.WorkItemData = ResponseDetail.WorkItemData;
            m_ResponseIsCopied = true;
            Response.MessageText = _message;
            return Response;
        }

        private bool m_RequestIsCopied = false;
        private bool m_ResponseIsCopied = false;
        protected override void DoDispose()
        {
            if (RequestDetail != null)
            {
                lock (this)
                {
                    if (RequestDetail != null)
                    {
                        if (!m_RequestIsCopied)
                        {
                            RequestDetail.Dispose();
                            m_RequestDetail = null;
                            base.RequestData = null;
                        }
                    }
                    if (ResponseDetail != null)
                    {
                        if (!m_ResponseIsCopied)
                        {
                            ResponseDetail.Dispose();
                            m_ResponseDetail = null;
                            base.ResponseData = null;
                        }
                    }
                }
            }
            else if (ResponseDetail != null)
            {
                lock (this)
                {
                    if (ResponseDetail != null)
                    {
                        if (!m_ResponseIsCopied)
                        {
                            ResponseDetail.Dispose();
                            m_ResponseDetail = null;
                            base.ResponseData = null;
                        }
                    }
                }
            }
            base.DoDispose();
        }
    }
}
