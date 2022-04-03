﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ChillXThreading.Complete
{
    public class ThreadedWorkItem<TRequest, TResponse, TClientID> : IEqualityComparer<ThreadedWorkItem<TRequest, TResponse, TClientID>>
        where TClientID : struct, IComparable, IFormattable, IConvertible
    {
        public ThreadedWorkItem(TClientID _ClientID)
        {
            ClientID = _ClientID;
        }
        public int ID { get; } = IdentitySequence.NextID();
        public TClientID ClientID { get; private set; }
        private TRequest m_Request = default(TRequest);
        /// <summary>
        /// Request work item Unit of Work
        /// This is the work to be processed. Or the API request data etc...
        /// </summary>
        public TRequest Request
        {
            get
            {
                lock (this)
                {
                    return m_Request;
                }
            }
            set
            {
                lock (this)
                {
                    m_Request = value;
                }
            }
        }
        private TResponse m_Response = default(TResponse);
        /// <summary>
        /// Response work item Unit of Work
        /// This processed result. Or the API response data etc...
        /// </summary>
        public TResponse Response
        {
            get
            {
                lock (this)
                {
                    return m_Response;
                }
            }
            set
            {
                lock (this)
                {
                    m_Response = value;
                    m_IsComplete = true;
                    m_ResponseCompleteTime = DateTime.Now;
                }
            }
        }

        private bool m_IsComplete = false;
        /// <summary>
        /// True of the work item request has been processed and a response has been assigned. <see cref="Response"/>
        /// </summary>
        public bool IsComplete
        {
            get
            {
                lock (this)
                {
                    return m_IsComplete;
                }
            }
        }

        private DateTime m_ResponseCompleteTime = DateTime.MinValue;
        /// <summary>
        /// Timestamp in local server time of when the processed response was assigned
        /// </summary>
        public DateTime ResponseCompleteTime
        {
            get
            {
                lock (this)
                {
                    return m_ResponseCompleteTime;
                }
            }
        }

        /// <summary>
        /// Age of response. This is the difference betweent he current time and <see cref="ResponseCompleteTime"/>
        /// </summary>
        public TimeSpan ResponseAge
        {
            get
            {
                DateTime CompleteTime;
                lock (this)
                {
                    if (m_IsComplete)
                    {
                        CompleteTime = m_ResponseCompleteTime;
                    }
                    else
                    {
                        return TimeSpan.Zero;
                    }
                }
                return DateTime.Now.Subtract(CompleteTime);
            }
        }

        public bool Equals(ThreadedWorkItem<TRequest, TResponse, TClientID> x, ThreadedWorkItem<TRequest, TResponse, TClientID> y)
        {
            if ((x == null) && (y == null)) { return true; }
            if ((x == null) || (y == null)) { return false; }
            return x.ID == y.ID;
        }

        public int GetHashCode(ThreadedWorkItem<TRequest, TResponse, TClientID> obj)
        {
            return obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }
            ThreadedWorkItem<TRequest, TResponse, TClientID> TypedInstance;
            TypedInstance = obj as ThreadedWorkItem<TRequest, TResponse, TClientID>;
            if (TypedInstance == null) { return false; }
            return ID == TypedInstance.ID;
        }
    }
}
