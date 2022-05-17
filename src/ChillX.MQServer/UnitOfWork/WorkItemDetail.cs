﻿/*
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

using ChillX.Core.CapabilityBase;
using ChillX.Core.CapabilityInterfaces;
using ChillX.Core.Structures;
using ChillX.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer.UnitOfWork
{
    [SerializedEntity(2)]
    [Serializable]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public class WorkItemDetail<T> : ISupportSerialization, IDisposable, ICloneable
        where T : new()
    {
        //public static readonly ManagedPool<WorkItemDetail<T>> Shared = ManagedPool<WorkItemDetail<T>>.Shared;

        public WorkItemDetail()
        {

        }

        private T m_WorkItemData;
        public T WorkItemData
        {
            get { return m_WorkItemData; }
            set { m_WorkItemData = value; IsValueAssigned = m_WorkItemData == null ? false : true; }
        }

        [SerializedMember(0)]
        private RentedBuffer<byte> WorkItemDataBytes = null;


        [SerializedMember(1)]
        public bool IsValueAssigned { get; private set; } = false;



        public void PackToBytes()
        {
            if (IsValueAssigned)
            {
                if (WorkItemDataBytes == null)
                {
                    WorkItemDataBytes = Serialization.ChillXSerializer<T>.ReadToRentedBuffer(WorkItemData);
                }
            }
        }

        public void UnPackBytes()
        {
            if (WorkItemDataBytes != null)
            {
                T instance = new T();
                if (Serialization.ChillXSerializer<T>.Write(instance, WorkItemDataBytes._rawBufferInternal))
                {
                    WorkItemData = instance;
                    WorkItemDataBytes = null;
                }
            }
        }

        #region IDisposable Members

        private bool m_IsDisposed = false;
        public bool IsDisposed
        {
            get { return m_IsDisposed; }
        }

        protected virtual void DoDispose()
        {
            IDisposable DisposableWorkItemData;
            lock (this)
            {
                DisposableWorkItemData = WorkItemData as IDisposable;
                if (DisposableWorkItemData != null)
                {
                    if (DisposableWorkItemData != null)
                    {
                        DisposableWorkItemData.Dispose();
                    }
                }
                if (WorkItemDataBytes != null)
                {
                    WorkItemDataBytes.Return();
                    WorkItemDataBytes = null;
                }
            }
            WorkItemData = default(T);
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                DoDispose();
                GC.SuppressFinalize(this);
                m_IsDisposed = true;
            }
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            //WorkItemDetail<T> Result;
            //Result = new WorkItemDetail<T>();
            //Result.m_WorkItemData = m_WorkItemData;
            //return Result;
            return this.MemberwiseClone();
        }


        #endregion
    }

}
