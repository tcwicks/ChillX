using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ChillX.Serialization
{
    internal static class SerializerCache
    {
        private static readonly Dictionary<Type, SerializerBase> Cache = new Dictionary<Type, SerializerBase>();
        private static readonly ReaderWriterLockSlim SerializerCache_Lock = new ReaderWriterLockSlim();

        internal static bool TryGetCached<TObject>(Type EntityType, out TypedSerializer<TObject> serializer)
        {
            serializer = null;
            SerializerBase serializerBase;
            SerializerCache_Lock.EnterReadLock();
            try
            {
                if (Cache.TryGetValue(EntityType, out serializerBase))
                {
                    serializer = serializerBase as TypedSerializer<TObject>;
                }
            }
            finally
            {
                SerializerCache_Lock.ExitReadLock();
            }
            return serializer != null;
        }

        internal static void TryAddCached<TObject>(Type EntityType, TypedSerializer<TObject> serializer)
        {
            SerializerCache_Lock.EnterWriteLock();
            try
            {
                if (!Cache.ContainsKey(EntityType))
                {
                    Cache.Add(EntityType, serializer);
                }
            }
            finally
            {
                SerializerCache_Lock.ExitWriteLock();
            }
        }
    }
}
