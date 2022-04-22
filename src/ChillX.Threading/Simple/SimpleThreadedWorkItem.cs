using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ChillX.Threading.Simple
{
    public class SimpleThreadedWorkItem<TRequest,TResponse, TClientID> : IEqualityComparer<SimpleThreadedWorkItem<TRequest, TResponse, TClientID>>
        where TRequest : class, new()
        where TResponse: class, new()
        where TClientID : struct, IComparable, IFormattable, IConvertible
    {
        public SimpleThreadedWorkItem(TClientID _ClientID)
        {
            ClientID = _ClientID;
        }
        public int ID { get; } = IdentitySequence.NextID();
        public TClientID ClientID { get; private set; }
        private TRequest m_Request = default(TRequest);
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
                lock(this)
                {
                    m_Request = value;
                }
            }
        } 
        private TResponse m_Response = default(TResponse);
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

        public TimeSpan ResponseAge
        {
            get
            {
                DateTime CompleteTime;
                lock(this)
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

        public bool Equals(SimpleThreadedWorkItem<TRequest, TResponse, TClientID> x, SimpleThreadedWorkItem<TRequest, TResponse, TClientID> y)
        {
            if ((x == null) && (y == null)) { return true; }
            if ((x == null) || (y == null)) { return false; }
            return x.ID == y.ID;
        }

        public int GetHashCode(SimpleThreadedWorkItem<TRequest, TResponse, TClientID> obj)
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
            SimpleThreadedWorkItem<TRequest, TResponse, TClientID> TypedInstance;
            TypedInstance = obj as SimpleThreadedWorkItem<TRequest, TResponse, TClientID>;
            if (TypedInstance == null) { return false; }
            return ID == TypedInstance.ID;
        }
    }
}
