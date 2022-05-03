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

using ChillX.Core.CapabilityInterfaces;
using ChillX.Core.Structures;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ChillX.Serialization
{
    internal class TypedSerializer<TObject> : SerializerBase
    {
        internal static TypedSerializer<TObject> Create()
        {
            return new TypedSerializer<TObject>().LoadDefinition();
        }

        public int EntityIndex { get; private set; }
        private bool HasDynamicData = false;
        private int DataSizeFixed;
        private int NumMembers;

        private SerializationMemberInfo[] SerializationDefinition;
        private SerializationMemberInfo[] SerializationDefinitionDynamicSize;
        private Dictionary<ushort, SerializationMemberInfo> SerializationDefinitionDict = new Dictionary<ushort, SerializationMemberInfo>();
        private int ByteSizeData;
        private int ByteSizeHeaderExplicit;
        private int ByteSizeHeaderImplicit;
        private byte[] BufferHeaderExplicit;
        private byte[] BufferHeaderImplicit;

        private UInt16 NumFields;
        private UInt16 NumFieldsFixed;
        private UInt16 NumFieldsVariable;
        private TypedSerializer()
        {

        }
        internal TypedSerializer<TObject> LoadDefinition()
        {
            return LoadDefinition(typeof(TObject));
        }
        internal TypedSerializer<TObject> LoadDefinition(Type targetType)
        {
            PropertyInfo[] propertyList = targetType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo[] fieldList = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            List<SerializationMemberInfo> serializationMemberList = new List<SerializationMemberInfo>();
            List<PropertyInfo> childObjectPropertyList = new List<PropertyInfo>();
            List<FieldInfo> childObjectFieldList = new List<FieldInfo>();

            SerializationMemberInfo serializationMember;
            SerializedMemberAttribute SerializationAttribute;
            SerializedEntityAttribute SerializabeEntityAttribute;
            bool canSerialize;
            Func<TObject, byte[]> Reader;
            Action<TObject, byte[], int> Writer;

            SerializabeEntityAttribute = targetType.GetCustomAttribute<SerializedEntityAttribute>();
            if (SerializabeEntityAttribute == null)
            {
                throw new InvalidOperationException(String.Format(@"Class or struct type not tagged with LightSpeedEntityAttribute {0} ", targetType.FullName));
            }
            EntityIndex = SerializabeEntityAttribute.Index;

            foreach (PropertyInfo property in propertyList)
            {
                SerializationAttribute = property.GetCustomAttribute<SerializedMemberAttribute>(false);
                if (SerializationAttribute != null)
                {
                    if ((!property.CanRead) || (!property.CanWrite))
                    {
                        throw new InvalidOperationException(String.Format(@"Property tagged for serialization must have both getter and setter. {0}.{1} ", targetType.FullName, property.Name));
                    }
                    //if (property.PropertyType.IsEnum)
                    //{
                    //    throw new InvalidOperationException(String.Format(@"Property tagged for serialization is an Enum. Please serialize it as an int32 value instead. {0}.{1} ", targetType.FullName, property.Name));
                    //}
                    TypeCode typeCode = Type.GetTypeCode(property.PropertyType);
                    canSerialize = true;
                    serializationMember = new SerializationMemberInfo();
                    serializationMember.SerializationAttribute = SerializationAttribute;
                    serializationMember.Name = property.Name;
                    serializationMember.IsDynamic = false;
                    switch (typeCode)
                    {
                        case TypeCode.Byte:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, byte>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToByte");
                            serializationMember.NumBytes = sizeof(byte);
                            break;
                        case TypeCode.Boolean:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, bool>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToBoolean");
                            serializationMember.NumBytes = sizeof(bool);
                            break;
                        case TypeCode.Char:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, char>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToChar");
                            serializationMember.NumBytes = sizeof(char);
                            break;
                        case TypeCode.Int16:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, short>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToInt16");
                            serializationMember.NumBytes = sizeof(short);
                            break;
                        case TypeCode.Int32:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, int>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToInt32");
                            serializationMember.NumBytes = sizeof(int);
                            break;
                        case TypeCode.Int64:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, long>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToInt64");
                            serializationMember.NumBytes = sizeof(long);
                            break;
                        case TypeCode.UInt16:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, ushort>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToUInt16");
                            serializationMember.NumBytes = sizeof(ushort);
                            break;
                        case TypeCode.UInt32:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, uint>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToUInt32");
                            serializationMember.NumBytes = sizeof(uint);
                            break;
                        case TypeCode.UInt64:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, ulong>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToUInt64");
                            serializationMember.NumBytes = sizeof(ulong);
                            break;
                        case TypeCode.Single:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, float>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToSingle");
                            serializationMember.NumBytes = sizeof(float);
                            break;
                        case TypeCode.Double:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, double>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToDouble");
                            serializationMember.NumBytes = sizeof(double);
                            break;
                        case TypeCode.Decimal:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, decimal>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToDecimal");
                            serializationMember.NumBytes = sizeof(decimal);
                            break;
                        case TypeCode.DateTime:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, DateTime>(property);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToDateTime");
                            serializationMember.NumBytes = sizeof(long);
                            break;
                        case TypeCode.String:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, string>(property, @"GetBytesUTF8String");
                            serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToString");
                            serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, string>(property, @"GetByteCountUTF8String");
                            serializationMember.NumBytes = 0;
                            serializationMember.IsDynamic = true;
                            break;
                        default:
                            if (property.PropertyType == typeof(TimeSpan))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, TimeSpan>(property);
                                serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(property, @"ToTimeSpan");
                                serializationMember.NumBytes = sizeof(long);
                            }
                            else if (property.PropertyType == typeof(byte[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, byte[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToByteArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, byte[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<byte>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<byte>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToByteRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<byte>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(bool[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, bool[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToBooleanArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, bool[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<bool>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<bool>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToBooleanRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<bool>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(char[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, char[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToCharArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, char[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<char>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<char>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToCharRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<char>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(short[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, short[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToInt16Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, short[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<short>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<short>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToInt16RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<short>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(int[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, int[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToInt32Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, int[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<int>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<int>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToInt32RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<int>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(long[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, long[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToInt64Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, long[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<long>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<long>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToInt64RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<long>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(ushort[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, ushort[]>(property, @"GetBytesUShortArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToUInt16Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, ushort[]>(property, @"GetByteCountUShortArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<ushort>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<ushort>>(property, @"GetBytesUShortArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToUInt16RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<ushort>>(property, @"GetByteCountUShortArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(uint[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, uint[]>(property, @"GetBytesUIntArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToUInt32Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, uint[]>(property, @"GetByteCountUIntArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<uint>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<uint>>(property, @"GetBytesUIntArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToUInt32RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<uint>>(property, @"GetByteCountUIntArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(ulong[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, ulong[]>(property, @"GetBytesULongArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToUInt64Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, ulong[]>(property, @"GetByteCountULongArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<ulong>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<ulong>>(property, @"GetBytesULongArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToUInt64RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<ulong>> (property, @"GetByteCountULongArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(float[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, float[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToSingleArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, float[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<float>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<float>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToSingleRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<float>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(double[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, double[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToDoubleArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, double[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<double>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<double>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToDoubleRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<double>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(decimal[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, decimal[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToDecimalArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, decimal[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<decimal>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<decimal>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToDecimalRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<decimal>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(TimeSpan[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, TimeSpan[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToTimeSpanArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, TimeSpan[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<TimeSpan>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<TimeSpan>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToTimeSpanRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<TimeSpan>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(DateTime[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, DateTime[]>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToDateTimeArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, DateTime[]>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (property.PropertyType == typeof(RentedBuffer<DateTime>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<DateTime>>(property);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(property, @"ToDateTimeRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<DateTime>>(property);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            //else if ((property.PropertyType.IsValueType && !property.PropertyType.IsPrimitive) || (property.PropertyType.IsClass))
                            //{
                            //    serializationMember.IsDynamic = true;
                            //    isStructOrClass = true;
                            //    canSerialize = false;
                            //}
                            else
                            {
                                canSerialize = false;
                            }
                            break;

                    }
                    if (!canSerialize)
                    {
                        // Todo add additional serializers here
                    }
                    if (!canSerialize)
                    {
                        throw new NotSupportedException(String.Format(@"Property tagged for serialization is not a supported type. Currently only native primitive types and arrays of primitive types are supported. {0}.{1} ", targetType.FullName, property.Name));
                    }
                    else
                    {
                        serializationMemberList.Add(serializationMember);
                    }
                }
            }
            foreach (FieldInfo field in fieldList)
            {
                SerializationAttribute = field.GetCustomAttribute<SerializedMemberAttribute>(false);
                if (SerializationAttribute != null)
                {
                    if (field.IsInitOnly)
                    {
                        throw new InvalidOperationException(String.Format(@"Field tagged for serialization must not be read only. {0}.{1} ", targetType.FullName, field.Name));
                    }

                    TypeCode typeCode = Type.GetTypeCode(field.FieldType);
                    canSerialize = true;
                    serializationMember = new SerializationMemberInfo();
                    serializationMember.SerializationAttribute = SerializationAttribute;
                    serializationMember.Name = field.Name;
                    serializationMember.IsDynamic = false;
                    switch (typeCode)
                    {
                        case TypeCode.Byte:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, byte>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToByte");
                            serializationMember.NumBytes = sizeof(byte);
                            break;
                        case TypeCode.Boolean:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, bool>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToBoolean");
                            serializationMember.NumBytes = sizeof(bool);
                            break;
                        case TypeCode.Char:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, char>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToChar");
                            serializationMember.NumBytes = sizeof(char);
                            break;
                        case TypeCode.Int16:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, short>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToInt16");
                            serializationMember.NumBytes = sizeof(short);
                            break;
                        case TypeCode.Int32:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, int>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToInt32");
                            serializationMember.NumBytes = sizeof(int);
                            break;
                        case TypeCode.Int64:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, long>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToInt64");
                            serializationMember.NumBytes = sizeof(long);
                            break;
                        case TypeCode.UInt16:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, ushort>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToUInt16");
                            serializationMember.NumBytes = sizeof(ushort);
                            break;
                        case TypeCode.UInt32:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, uint>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToUInt32");
                            serializationMember.NumBytes = sizeof(uint);
                            break;
                        case TypeCode.UInt64:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, ulong>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToUInt64");
                            serializationMember.NumBytes = sizeof(ulong);
                            break;
                        case TypeCode.Single:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, float>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToSingle");
                            serializationMember.NumBytes = sizeof(float);
                            break;
                        case TypeCode.Double:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, double>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToDouble");
                            serializationMember.NumBytes = sizeof(double);
                            break;
                        case TypeCode.Decimal:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, decimal>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToDecimal");
                            serializationMember.NumBytes = sizeof(decimal);
                            break;
                        case TypeCode.DateTime:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, DateTime>(field);
                            serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToDateTime");
                            serializationMember.NumBytes = sizeof(long);
                            break;
                        case TypeCode.String:
                            serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, string>(field, @"GetBytesUTF8String");
                            serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToString");
                            serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, string>(field, @"GetByteCountUTF8String");
                            serializationMember.NumBytes = 0;
                            serializationMember.IsDynamic = true;
                            break;
                        default:
                            if (field.FieldType == typeof(TimeSpan))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, TimeSpan>(field);
                                serializationMember.Writer = Helpers.BuildSerializeSetter<TObject>(field, @"ToTimeSpan");
                                serializationMember.NumBytes = sizeof(long);
                            }
                            else if (field.FieldType == typeof(byte[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, byte[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToByteArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, byte[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<byte>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<byte>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToByteRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<byte>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(bool[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, bool[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToBooleanArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, bool[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<bool>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<bool>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToBooleanRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<bool>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(char[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, char[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToCharArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, char[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<char>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<char>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToCharRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<char>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(short[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, short[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToInt16Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, short[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<short>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<short>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToInt16RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<short>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(int[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, int[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToInt32Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, int[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<int>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<int>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToInt32RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<int>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(long[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, long[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToInt64Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, long[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<long>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<long>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToInt64RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<long>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(ushort[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, ushort[]>(field, @"GetBytesUShortArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToUInt16Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, ushort[]>(field, @"GetByteCountUShortArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<ushort>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<ushort>>(field, @"GetBytesUShortArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToUInt16RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<ushort>>(field, @"GetByteCountUShortArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(uint[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, uint[]>(field, @"GetBytesUIntArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToUInt32Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, uint[]>(field, @"GetByteCountUIntArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<uint>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<uint>>(field, @"GetBytesUIntArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToUInt32RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<uint>>(field, @"GetByteCountUIntArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(ulong[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, ulong[]>(field, @"GetBytesULongArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToUInt64Array");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, ulong[]>(field, @"GetByteCountULongArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<ulong>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<ulong>>(field, @"GetBytesULongArray");
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToUInt64RentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<ulong>>(field, @"GetByteCountULongArray");
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(float[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, float[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToSingleArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, float[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<float>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<float>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToSingleRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<float>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(double[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, double[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToDoubleArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, double[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<double>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<double>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToDoubleRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<double>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(decimal[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, decimal[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToDecimalArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, decimal[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<decimal>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<decimal>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToDecimalRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<decimal>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(TimeSpan[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, TimeSpan[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToTimeSpanArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, TimeSpan[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<TimeSpan>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<TimeSpan>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToTimeSpanRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<TimeSpan>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(DateTime[]))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, DateTime[]>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToDateTimeArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, DateTime[]>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            else if (field.FieldType == typeof(RentedBuffer<DateTime>))
                            {
                                serializationMember.Reader = Helpers.BuildSerializeGetter<TObject, RentedBuffer<DateTime>>(field);
                                serializationMember.WriterDynamic = Helpers.BuildSerializeSetterArray<TObject>(field, @"ToDateTimeRentedArray");
                                serializationMember.ReaderGetSize = Helpers.BuildSerializeSizeGetter<TObject, RentedBuffer<DateTime>>(field);
                                serializationMember.NumBytes = 0;
                                serializationMember.IsDynamic = true;
                            }
                            //else if ((field.FieldType.IsValueType && !field.FieldType.IsPrimitive) || (field.FieldType.IsClass))
                            //{
                            //    serializationMember.IsDynamic = true;
                            //    isStructOrClass = true;
                            //    canSerialize = false;
                            //}
                            else
                            {
                                canSerialize = false;
                            }
                            break;

                    }
                    if (!canSerialize)
                    {
                        // Toto Build additional primitive type serializers
                    }
                    if (!canSerialize)
                    {
                        throw new NotSupportedException(String.Format(@"Field tagged for serialization is not a supported type. Currently only native primitive types and arrays of primitive types are supported. {0}.{1} ", targetType.FullName, field.Name));
                    }
                    else
                    {
                        serializationMemberList.Add(serializationMember);
                    }
                }
            }
            serializationMemberList.Sort(SerializationMemberSortAscending);
            SerializationDefinition = serializationMemberList.ToArray();
            ByteSizeData = 0;
            NumFieldsFixed = 0;
            NumFieldsVariable = 0;
            NumFields = 0;
            foreach (SerializationMemberInfo SerializationInfo in SerializationDefinition)
            {
                ByteSizeData += SerializationInfo.NumBytes;
                if (SerializationDefinitionDict.ContainsKey(SerializationInfo.SerializationAttribute.Index))
                {
                    throw new InvalidOperationException(String.Format(@"Duplicate LightSpeedSerializeAttribute with same index {0}.Index == {1}.Index == {2}", SerializationInfo.Name, SerializationDefinitionDict[SerializationInfo.SerializationAttribute.Index].Name, SerializationInfo.SerializationAttribute.Index));
                }
                else
                {
                    SerializationDefinitionDict.Add(SerializationInfo.SerializationAttribute.Index, SerializationInfo);
                }
                NumFields += 1;
                if (SerializationInfo.IsDynamic)
                {
                    NumFieldsVariable += 1;
                }
                else
                {
                    NumFieldsFixed += 1;
                }
            }
            if (SerializationDefinition.Length > (UInt16.MaxValue - 1))
            {
                throw new NotSupportedException(String.Format(@"Total number of serializable properties {0} exceeds the maximum limit of {1}. If more is really needed then change the data header block for number of properties to UInt32 instead of UInt16", SerializationDefinition.Length, (UInt16.MaxValue - 1)));
            }
            NumFields = (UInt16)SerializationDefinition.Length;
            ByteSizeHeaderExplicit = sizeof(int) + sizeof(bool) + sizeof(UInt16) + sizeof(UInt16) + (sizeof(UInt16) * NumFields) + (sizeof(int) * NumFieldsVariable);
            ByteSizeHeaderImplicit = sizeof(int) + sizeof(bool) + sizeof(UInt16) + sizeof(UInt16) + (sizeof(int) * NumFieldsVariable);

            BufferHeaderExplicit = new byte[ByteSizeHeaderExplicit];
            BufferHeaderImplicit = new byte[ByteSizeHeader];

            BufferHeaderExplicit[ByteSizeHeaderBlockSize] = DataTrueFlag;
            BufferHeaderImplicit[ByteSizeHeaderBlockSize] = DataFalseFlag;

            Array.Copy(BitConverter.GetBytes(EntityIndex), 0, BufferHeaderExplicit, ByteSizeHeaderBlockSize + ByteSizeHeaderBlockFlag, ByteSizeHeaderBlockAttribute);
            Array.Copy(BitConverter.GetBytes(EntityIndex), 0, BufferHeaderImplicit, ByteSizeHeaderBlockSize + ByteSizeHeaderBlockFlag, ByteSizeHeaderBlockAttribute);

            Array.Copy(BitConverter.GetBytes(NumFields), 0, BufferHeaderExplicit, ByteSizeHeaderBlockSize + ByteSizeHeaderBlockFlag + ByteSizeHeaderBlockAttribute, ByteSizeHeaderBlockAttribute);
            Array.Copy(BitConverter.GetBytes(NumFields), 0, BufferHeaderImplicit, ByteSizeHeaderBlockSize + ByteSizeHeaderBlockFlag + ByteSizeHeaderBlockAttribute, ByteSizeHeaderBlockAttribute);

            int Index;
            Index = ByteSizeHeader;
            HasDynamicData = false;
            DataSizeFixed = 0;
            serializationMemberList.Clear();
            foreach (SerializationMemberInfo SerializationInfo in SerializationDefinition)
            {
                Array.Copy(BitConverter.GetBytes(SerializationInfo.SerializationAttribute.Index), 0, BufferHeaderExplicit, Index, ByteSizeHeaderBlockAttribute);
                Index += ByteSizeHeaderBlockAttribute;
                if (SerializationInfo.IsDynamic)
                {
                    Index += ByteSizeHeaderBlockSize;
                }
                if (SerializationInfo.IsDynamic) { HasDynamicData = true; DataSizeFixed += 32; serializationMemberList.Add(SerializationInfo); }
                else { DataSizeFixed += SerializationInfo.NumBytes; }
            }
            SerializationDefinitionDynamicSize = serializationMemberList.ToArray();
            NumMembers = SerializationDefinition.Length;
            return this;
        }

        private static int SerializationMemberSortAscending(SerializationMemberInfo x, SerializationMemberInfo y)
        {
            return x.SerializationAttribute.Index.CompareTo(y.SerializationAttribute.Index);
        }

        public byte[] Read(TObject target, bool headerExplicit = true)
        {
            byte[] buffer;
            int HeaderIndex;

            ISupportSerialization AutoSerializeEntity = target as ISupportSerialization;
            if (AutoSerializeEntity != null)
            {
                AutoSerializeEntity.PackToBytes();
            }

            int dataSize = DataSizeFixed;
            if (HasDynamicData)
            {
                foreach (SerializationMemberInfo SerializationInfo in SerializationDefinitionDynamicSize)
                {
                    dataSize += SerializationInfo.ReaderGetSize(target);
                }
            }
            if (headerExplicit)
            {
                dataSize += ByteSizeHeaderExplicit;
            }
            else
            {
                dataSize += ByteSizeHeaderImplicit;
            }
            buffer = new byte[dataSize];


            HeaderIndex = ByteSizeHeader;
            if (headerExplicit)
            {
                Array.Copy(BufferHeaderExplicit, 0, buffer, 0, ByteSizeHeaderExplicit);
                int dataIndex = ByteSizeHeaderExplicit;
                for (int I = 0; I < NumMembers; I++)
                {
                    SerializationMemberInfo SerializationInfo = SerializationDefinition[I];
                    int dataLength = SerializationInfo.Reader(target, buffer, dataIndex);
                    dataIndex += dataLength;
                    HeaderIndex += ByteSizeHeaderBlockAttribute;
                    if (SerializationInfo.IsDynamic)
                    {
                        BitConverterExtended.GetBytes(dataLength, buffer, HeaderIndex);
                        HeaderIndex += ByteSizeHeaderBlockSize;
                    }
                }
            }
            else
            {
                Array.Copy(BufferHeaderImplicit, 0, buffer, 0, ByteSizeHeader);
                int dataIndex = ByteSizeHeaderImplicit;
                for (int I = 0; I < NumMembers; I++)
                {
                    SerializationMemberInfo SerializationInfo = SerializationDefinition[I];
                    int dataLength = SerializationInfo.Reader(target, buffer, dataIndex);
                    dataIndex += dataLength;
                    if (SerializationInfo.IsDynamic)
                    {
                        BitConverterExtended.GetBytes(dataLength, buffer, HeaderIndex);
                        HeaderIndex += ByteSizeHeaderBlockSize;
                    }
                }
            }
            BitConverterExtended.GetBytes(dataSize, buffer, 0);
            return buffer;
        }

        public byte[] Read(TObject target, byte[] buffer, bool headerExplicit = true)
        {
            int HeaderIndex;

            ISupportSerialization AutoSerializeEntity = target as ISupportSerialization;
            if (AutoSerializeEntity != null)
            {
                AutoSerializeEntity.PackToBytes();
            }

            int dataSize = DataSizeFixed;
            if (HasDynamicData)
            {
                foreach (SerializationMemberInfo SerializationInfo in SerializationDefinitionDynamicSize)
                {
                    dataSize += SerializationInfo.ReaderGetSize(target);
                }
            }
            if (headerExplicit)
            {
                dataSize += ByteSizeHeaderExplicit;
            }
            else
            {
                dataSize += ByteSizeHeaderImplicit;
            }


            HeaderIndex = ByteSizeHeader;
            if (headerExplicit)
            {
                Array.Copy(BufferHeaderExplicit, 0, buffer, 0, ByteSizeHeaderExplicit);
                int dataIndex = ByteSizeHeaderExplicit;
                for (int I = 0; I < NumMembers; I++)
                {
                    SerializationMemberInfo SerializationInfo = SerializationDefinition[I];
                    int dataLength = SerializationInfo.Reader(target, buffer, dataIndex);
                    dataIndex += dataLength;
                    HeaderIndex += ByteSizeHeaderBlockAttribute;
                    if (SerializationInfo.IsDynamic)
                    {
                        BitConverterExtended.GetBytes(dataLength, buffer, HeaderIndex);
                        HeaderIndex += ByteSizeHeaderBlockSize;
                    }
                }
            }
            else
            {
                Array.Copy(BufferHeaderImplicit, 0, buffer, 0, ByteSizeHeader);
                int dataIndex = ByteSizeHeaderImplicit;
                for (int I = 0; I < NumMembers; I++)
                {
                    SerializationMemberInfo SerializationInfo = SerializationDefinition[I];
                    int dataLength = SerializationInfo.Reader(target, buffer, dataIndex);
                    dataIndex += dataLength;
                    if (SerializationInfo.IsDynamic)
                    {
                        BitConverterExtended.GetBytes(dataLength, buffer, HeaderIndex);
                        HeaderIndex += ByteSizeHeaderBlockSize;
                    }
                }
            }
            BitConverterExtended.GetBytes(dataSize, buffer, 0);
            return buffer;
        }
        public RentedBuffer<byte> ReadToRentedBuffer(TObject target, bool headerExplicit = true)
        {
            RentedBuffer<byte> bufferRented;
            byte[] buffer;
            int HeaderIndex;

            ISupportSerialization AutoSerializeEntity = target as ISupportSerialization;
            if (AutoSerializeEntity != null)
            {
                AutoSerializeEntity.PackToBytes();
            }

            int dataSize = DataSizeFixed;
            if (HasDynamicData)
            {
                foreach (SerializationMemberInfo SerializationInfo in SerializationDefinitionDynamicSize)
                {
                    dataSize += SerializationInfo.ReaderGetSize(target);
                }
            }
            if (headerExplicit)
            {
                dataSize += ByteSizeHeaderExplicit;
            }
            else
            {
                dataSize += ByteSizeHeaderImplicit;
            }
            bufferRented = RentedBuffer<byte>.Shared.Rent(dataSize);
            //bufferRented = new RentedBuffer<byte>(dataSize);
            buffer = bufferRented._rawBufferInternal;


            HeaderIndex = ByteSizeHeader;
            if (headerExplicit)
            {
                Array.Copy(BufferHeaderExplicit, 0, buffer, 0, ByteSizeHeaderExplicit);
                int dataIndex = ByteSizeHeaderExplicit;
                //foreach (SerializationMemberInfo SerializationInfo in SerializationDefinition)
                //{
                //    int dataLength = SerializationInfo.Reader(target, buffer, dataIndex);
                //    dataIndex += dataLength;
                //    HeaderIndex += ByteSizeHeaderBlockAttribute;
                //    if (SerializationInfo.IsDynamic)
                //    {
                //        CustomBitConverter.GetBytes(dataLength,buffer, HeaderIndex);
                //        HeaderIndex += ByteSizeHeaderBlockSize;
                //    }
                //}
                for (int I = 0; I < NumMembers; I++)
                {
                    SerializationMemberInfo SerializationInfo = SerializationDefinition[I];
                    int dataLength = SerializationInfo.Reader(target, buffer, dataIndex);
                    dataIndex += dataLength;
                    HeaderIndex += ByteSizeHeaderBlockAttribute;
                    if (SerializationInfo.IsDynamic)
                    {
                        BitConverterExtended.GetBytes(dataLength, buffer, HeaderIndex);
                        HeaderIndex += ByteSizeHeaderBlockSize;
                    }
                }
            }
            else
            {
                Array.Copy(BufferHeaderImplicit, 0, buffer, 0, ByteSizeHeader);
                int dataIndex = ByteSizeHeaderImplicit;
                //foreach (SerializationMemberInfo SerializationInfo in SerializationDefinition)
                //{
                //    int dataLength = SerializationInfo.Reader(target,buffer, dataIndex);
                //    dataIndex += dataLength;
                //    if (SerializationInfo.IsDynamic)
                //    {
                //        CustomBitConverter.GetBytes(dataLength,buffer, HeaderIndex);
                //        HeaderIndex += ByteSizeHeaderBlockSize;
                //    }
                //}
                for (int I = 0; I < NumMembers; I++)
                {
                    SerializationMemberInfo SerializationInfo = SerializationDefinition[I];
                    int dataLength = SerializationInfo.Reader(target, buffer, dataIndex);
                    dataIndex += dataLength;
                    if (SerializationInfo.IsDynamic)
                    {
                        BitConverterExtended.GetBytes(dataLength, buffer, HeaderIndex);
                        HeaderIndex += ByteSizeHeaderBlockSize;
                    }
                }
            }
            BitConverterExtended.GetBytes(dataSize, buffer, 0);
            return bufferRented;
        }

        //It is NOT straight forward to create an overload with Span<T> buffer because Span<T> cannot be used in type parameters
        //And we need to use Type parameters with this strategy as we are using expression trees to create dynamic delegates
        public bool Write(TObject target, byte[] buffer, out int bytesConsumed, int startIndex = 0)
        {
            int MessageSize;
            if (buffer.Length < ByteSizeHeaderBlockSize + startIndex) { bytesConsumed = 0; return false; }
            MessageSize = BitConverter.ToInt32(buffer, startIndex);
            if (buffer.Length < MessageSize + startIndex) { bytesConsumed = 0; return false; }

            bool isExplicit;
            isExplicit = BitConverter.ToBoolean(buffer, ByteSizeHeaderBlockSize);
            if (!isExplicit)
            {
                throw new InvalidOperationException(String.Format(@"Header layout byte buffer from previous message must be provided when de-serializing data messages which do not explicit header layout. Consider using WriteImplicit() instead."));
            }
            ushort entityTypeID;
            int FieldCount;
            entityTypeID = BitConverter.ToUInt16(buffer, ByteSizeHeaderBlockSize + ByteSizeHeaderBlockFlag);
            FieldCount = BitConverter.ToUInt16(buffer, ByteSizeHeaderBlockSize + ByteSizeHeaderBlockFlag + ByteSizeHeaderBlockAttribute);

            bytesConsumed = MessageSize;

            int headerIndex = ByteSizeHeader + startIndex;
            int dataIndex = ByteSizeHeaderExplicit + startIndex;
            ushort fieldID;
            int fieldSize;
            SerializationMemberInfo serializationInfo;
            for (int fieldIndex = 0; fieldIndex < FieldCount; fieldIndex++)
            {
                fieldID = BitConverter.ToUInt16(buffer, headerIndex);
                headerIndex += ByteSizeHeaderBlockAttribute;
                if (!SerializationDefinitionDict.TryGetValue(fieldID, out serializationInfo))
                {
                    throw new MissingMemberException(String.Format(@"Property or Field with index {0} is present in the data but not found on the entity", fieldID));
                }
                if (serializationInfo.IsDynamic)
                {
                    fieldSize = BitConverterExtended.ToInt32(buffer, headerIndex);
                    headerIndex += ByteSizeHeaderBlockSize;
                    //serializationInfo.WriterArray(target, buffer.AsSpan().Slice(dataIndex, fieldSize).ToArray(), 0);
                    serializationInfo.WriterDynamic(target, buffer, dataIndex, fieldSize);
                    dataIndex += fieldSize;
                }
                else
                {
                    serializationInfo.Writer(target, buffer, dataIndex);
                    dataIndex += serializationInfo.NumBytes;
                }
            }
            ISupportSerialization AutoSerializeEntity = target as ISupportSerialization;
            if (AutoSerializeEntity != null)
            {
                AutoSerializeEntity.UnPackBytes();
            }
            return true;
        }

        private struct SerializationMemberInfo : IEqualityComparer<SerializationMemberInfo>
        {
            public string Name;
            public SerializedMemberAttribute SerializationAttribute;
            public Func<TObject, byte[], int, int> Reader;
            public Func<TObject, int> ReaderGetSize;
            public Action<TObject, byte[], int> Writer;
            public Action<TObject, byte[], int, int> WriterDynamic;
            public int NumBytes;
            public bool IsDynamic;

            public override bool Equals(object obj)
            {
                if (obj is SerializationMemberInfo)
                {
                    return SerializationAttribute.Index == ((SerializationMemberInfo)obj).SerializationAttribute.Index;
                }
                return base.Equals(obj);
            }

            public bool Equals(SerializationMemberInfo other)
            {
                return SerializationAttribute.Index == other.SerializationAttribute.Index;
            }

            public bool Equals(SerializationMemberInfo x, SerializationMemberInfo y)
            {
                return x.SerializationAttribute.Index == y.SerializationAttribute.Index;
            }

            public int GetHashCode(SerializationMemberInfo obj)
            {
                return obj.SerializationAttribute.Index.GetHashCode();
            }
            public override int GetHashCode()
            {
                return SerializationAttribute.Index.GetHashCode();
            }
        }
    }

}
