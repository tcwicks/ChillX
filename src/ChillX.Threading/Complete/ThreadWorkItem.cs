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

using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.Threading.Complete
{
    public class ThreadWorkItem<TRequest, TResponse, TClientID> : IEqualityComparer<ThreadWorkItem<TRequest, TResponse, TClientID>>
        where TClientID : IComparable, IConvertible
    {
        public ThreadWorkItem(TClientID _ClientID)
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

        /// <summary>
        /// Custom variables can be attached here
        /// </summary>
        public object[] Parameters { get; set; }

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

        private bool m_IsError = false;
        public bool IsError
        {
            get 
            {
                lock (this)
                {
                    return m_IsError;
                }
            }
            internal set
            {
                lock(this)
                {
                    m_IsError = value;
                }

            }
        }

        private Exception m_ErrorException = null;
        public Exception ErrorException
        {
            get
            {
                lock( this)
                {
                    return m_ErrorException;
                }
            }
            internal set
            {
                lock(this)
                {
                    m_ErrorException = value;
                    m_IsError = (value != null);
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

        public bool Equals(ThreadWorkItem<TRequest, TResponse, TClientID> x, ThreadWorkItem<TRequest, TResponse, TClientID> y)
        {
            if ((x == null) && (y == null)) { return true; }
            if ((x == null) || (y == null)) { return false; }
            return x.ID == y.ID;
        }

        public int GetHashCode(ThreadWorkItem<TRequest, TResponse, TClientID> obj)
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
            ThreadWorkItem<TRequest, TResponse, TClientID> TypedInstance;
            TypedInstance = obj as ThreadWorkItem<TRequest, TResponse, TClientID>;
            if (TypedInstance == null) { return false; }
            return ID == TypedInstance.ID;
        }
    }
}
