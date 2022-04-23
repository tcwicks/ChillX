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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChillX.Serialization
{
    /// <summary>
    /// Creates wrappers for BitConverterExtended using expression trees for performance.
    /// Choosing to use expression trees instead of Reflection emit of IL code because of:
    /// 1) Better readability
    /// 2) Same performance
    /// </summary>
    public static class Helpers
    {
        public static Func<TObject, byte[], int, int> BuildSerializeGetter<TObject, TProperty>(PropertyInfo property, string BitConverterMethod = @"GetBytes")
        {
            Type targetType = typeof(TObject);
            MethodInfo getterMethodInfo = property.GetGetMethod();
            ParameterExpression entityParameterExpression = Expression.Parameter(targetType);
            MethodCallExpression getterCall = Expression.Call(entityParameterExpression, getterMethodInfo);

            ParameterExpression expressionBitConverterParameter = Expression.Parameter(typeof(TProperty));
            ParameterExpression expressionBitConverterParameterBuffer = Expression.Parameter(typeof(byte[]));
            ParameterExpression expressionBitConverterParameterStartIndex = Expression.Parameter(typeof(int));
            MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(BitConverterExtended),
                                BitConverterMethod,
                                null, /* no generic type arguments */
                                expressionBitConverterParameter, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex);

            var invoker = Expression.Call(expressionCallBitConverter.Method, getterCall, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex);
            var invokerCompiled = Expression.Lambda<Func<TObject, byte[], int, int>>(invoker, entityParameterExpression, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex).Compile();
            return invokerCompiled;
        }
        public static Func<TObject, int> BuildSerializeSizeGetter<TObject, TProperty>(PropertyInfo property, string BitConverterMethod = @"GetByteCount")
        {
            Type targetType = typeof(TObject);
            MethodInfo getterMethodInfo = property.GetGetMethod();
            ParameterExpression entityParameterExpression = Expression.Parameter(targetType);
            MethodCallExpression getterCall = Expression.Call(entityParameterExpression, getterMethodInfo);

            ParameterExpression expressionBitConverterParameter = Expression.Parameter(typeof(TProperty));
            MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(BitConverterExtended),
                                BitConverterMethod,
                                null, /* no generic type arguments */
                                expressionBitConverterParameter);

            var invoker = Expression.Call(expressionCallBitConverter.Method, getterCall);
            var invokerCompiled = Expression.Lambda<Func<TObject, int>>(invoker, entityParameterExpression).Compile();
            return invokerCompiled;
        }
        public static Func<TObject, byte[], int, int> BuildSerializeGetter<TObject, TProperty>(FieldInfo field, string BitConverterMethod = @"GetBytes")
        {
            Type targetType = typeof(TObject);
            ParameterExpression entityParameterExpression = Expression.Parameter(targetType);

            MemberExpression getterCall = Expression.Field(entityParameterExpression, field);


            ParameterExpression expressionBitConverterParameter = Expression.Parameter(typeof(TProperty));
            ParameterExpression expressionBitConverterParameterBuffer = Expression.Parameter(typeof(byte[]));
            ParameterExpression expressionBitConverterParameterStartIndex = Expression.Parameter(typeof(int));
            MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(BitConverterExtended),
                                BitConverterMethod,
                                null, /* no generic type arguments */
                                expressionBitConverterParameter, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex);

            var invoker = Expression.Call(expressionCallBitConverter.Method, getterCall, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex);
            var invokerCompiled = Expression.Lambda<Func<TObject, byte[], int, int>>(invoker, entityParameterExpression, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex).Compile();
            return invokerCompiled;
        }

        public static Func<TObject, int> BuildSerializeSizeGetter<TObject, TProperty>(FieldInfo field, string BitConverterMethod = @"GetByteCount")
        {
            Type targetType = typeof(TObject);
            ParameterExpression entityParameterExpression = Expression.Parameter(targetType);

            MemberExpression getterCall = Expression.Field(entityParameterExpression, field);


            ParameterExpression expressionBitConverterParameter = Expression.Parameter(typeof(TProperty));
            MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(BitConverterExtended),
                                BitConverterMethod,
                                null, /* no generic type arguments */
                                expressionBitConverterParameter);

            var invoker = Expression.Call(expressionCallBitConverter.Method, getterCall);
            var invokerCompiled = Expression.Lambda<Func<TObject, int>>(invoker, entityParameterExpression).Compile();
            return invokerCompiled;
        }

        public static Action<TObject, byte[], int> BuildSerializeSetter<TObject>(PropertyInfo property, string BitConverterMethod)
        {
            Type targetType = typeof(TObject);
            ParameterExpression expressionBitConverterValueParameter = Expression.Parameter(typeof(byte[]));
            ParameterExpression expressionBitConverterStartParameter = Expression.Parameter(typeof(int));

            MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(BitConverterExtended),
                                BitConverterMethod,
                                null, /* no generic type arguments */
                                expressionBitConverterValueParameter, expressionBitConverterStartParameter);


            MethodInfo setterMethodInfo = property.GetSetMethod();
            ParameterExpression entityParameterExpression = Expression.Parameter(targetType);
            MemberExpression propertySetterExpression = Expression.Property(entityParameterExpression, setterMethodInfo);

            BinaryExpression expressionAssign = Expression.Assign(propertySetterExpression, expressionCallBitConverter);

            Action<TObject, byte[], int> invokerCompiled = Expression.Lambda<Action<TObject, byte[], int>>(expressionAssign, entityParameterExpression, expressionBitConverterValueParameter, expressionBitConverterStartParameter).Compile();
            return invokerCompiled;
        }
        public static Action<TObject, byte[], int> BuildSerializeSetter<TObject>(FieldInfo field, string BitConverterMethod)
        {
            Type targetType = typeof(TObject);
            ParameterExpression expressionBitConverterValueParameter = Expression.Parameter(typeof(byte[]));
            ParameterExpression expressionBitConverterStartParameter = Expression.Parameter(typeof(int));

            MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(BitConverterExtended),
                                BitConverterMethod,
                                null, /* no generic type arguments */
                                expressionBitConverterValueParameter, expressionBitConverterStartParameter);


            ParameterExpression entityParameterExpression = Expression.Parameter(targetType);

            MemberExpression setterCall = Expression.Field(entityParameterExpression, field);

            BinaryExpression expressionAssign = Expression.Assign(setterCall, expressionCallBitConverter);

            Action<TObject, byte[], int> invokerCompiled = Expression.Lambda<Action<TObject, byte[], int>>(expressionAssign, entityParameterExpression, expressionBitConverterValueParameter, expressionBitConverterStartParameter).Compile();
            return invokerCompiled;
        }

        public static Action<TObject, byte[], int, int> BuildSerializeSetterArray<TObject>(PropertyInfo property, string BitConverterMethod)
        {
            Type targetType = typeof(TObject);
            ParameterExpression expressionBitConverterValueParameter = Expression.Parameter(typeof(byte[]));
            ParameterExpression expressionBitConverterStartParameter = Expression.Parameter(typeof(int));
            ParameterExpression expressionBitConverterLengthParameter = Expression.Parameter(typeof(int));

            MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(BitConverterExtended),
                                BitConverterMethod,
                                null, /* no generic type arguments */
                                expressionBitConverterValueParameter, expressionBitConverterStartParameter, expressionBitConverterLengthParameter);


            MethodInfo setterMethodInfo = property.GetSetMethod();
            ParameterExpression entityParameterExpression = Expression.Parameter(targetType);
            MemberExpression propertySetterExpression = Expression.Property(entityParameterExpression, setterMethodInfo);

            BinaryExpression expressionAssign = Expression.Assign(propertySetterExpression, expressionCallBitConverter);

            Action<TObject, byte[], int, int> invokerCompiled = Expression.Lambda<Action<TObject, byte[], int, int>>(expressionAssign, entityParameterExpression, expressionBitConverterValueParameter, expressionBitConverterStartParameter, expressionBitConverterLengthParameter).Compile();
            return invokerCompiled;
        }
        public static Action<TObject, byte[], int, int> BuildSerializeSetterArray<TObject>(FieldInfo field, string BitConverterMethod)
        {
            Type targetType = typeof(TObject);
            ParameterExpression expressionBitConverterValueParameter = Expression.Parameter(typeof(byte[]));
            ParameterExpression expressionBitConverterStartParameter = Expression.Parameter(typeof(int));
            ParameterExpression expressionBitConverterLengthParameter = Expression.Parameter(typeof(int));

            MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(BitConverterExtended),
                                BitConverterMethod,
                                null, /* no generic type arguments */
                                expressionBitConverterValueParameter, expressionBitConverterStartParameter, expressionBitConverterLengthParameter);


            ParameterExpression entityParameterExpression = Expression.Parameter(targetType);

            MemberExpression setterCall = Expression.Field(entityParameterExpression, field);

            BinaryExpression expressionAssign = Expression.Assign(setterCall, expressionCallBitConverter);

            Action<TObject, byte[], int, int> invokerCompiled = Expression.Lambda<Action<TObject, byte[], int, int>>(expressionAssign, entityParameterExpression, expressionBitConverterValueParameter, expressionBitConverterStartParameter, expressionBitConverterLengthParameter).Compile();
            return invokerCompiled;
        }



        #region Unused

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static int GetBytesUTF8(string s, byte[] buffer, int startIndex)
        //{
        //    int len = Encoding.UTF8.GetByteCount(s);
        //    Array.Copy(Encoding.UTF8.GetBytes(s), 0, buffer, startIndex, len);
        //    return len;
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static int GetByteCount(string s)
        //{
        //    return Encoding.UTF8.GetByteCount(s);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static string GetString(byte[] buffer, int startIndex)
        //{
        //    if (startIndex != 0)
        //    {
        //        throw new NotSupportedException(@"Provided only for parameter compatibility with BitConverter. It is too risky to autodetect the desired length based on null terminator as this could occur naturally in another data type. Call only with the exact byte buffer for the string.");
        //    }
        //    return Encoding.UTF8.GetString(buffer);
        //}

        //public static Func<TObject, byte[], int, int> BuildSerializeGetterString<TObject>(PropertyInfo property)
        //{
        //    Type targetType = typeof(TObject);
        //    MethodInfo getterMethodInfo = property.GetGetMethod();
        //    ParameterExpression entityParameterExpression = Expression.Parameter(targetType);
        //    MethodCallExpression getterCall = Expression.Call(entityParameterExpression, getterMethodInfo);

        //    ParameterExpression expressionBitConverterParameter = Expression.Parameter(typeof(string));
        //    ParameterExpression expressionBitConverterParameterBuffer = Expression.Parameter(typeof(byte[]));
        //    ParameterExpression expressionBitConverterParameterStartIndex = Expression.Parameter(typeof(int));
        //    MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(Helpers),
        //                        "GetBytesUTF8",
        //                        null, /* no generic type arguments */
        //                        expressionBitConverterParameter, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex);

        //    var invoker = Expression.Call(expressionCallBitConverter.Method, getterCall, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex);
        //    var invokerCompiled = Expression.Lambda<Func<TObject, byte[], int, int>>(invoker, entityParameterExpression, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex).Compile();
        //    return invokerCompiled;
        //}
        //public static Func<TObject, int> BuildSerializeSizeGetterString<TObject>(PropertyInfo property)
        //{
        //    Type targetType = typeof(TObject);
        //    MethodInfo getterMethodInfo = property.GetGetMethod();
        //    ParameterExpression entityParameterExpression = Expression.Parameter(targetType);
        //    MethodCallExpression getterCall = Expression.Call(entityParameterExpression, getterMethodInfo);

        //    ParameterExpression expressionBitConverterParameter = Expression.Parameter(typeof(string));
        //    MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(Helpers),
        //                        "GetByteCount",
        //                        null, /* no generic type arguments */
        //                        expressionBitConverterParameter);

        //    var invoker = Expression.Call(expressionCallBitConverter.Method, getterCall);
        //    var invokerCompiled = Expression.Lambda<Func<TObject, int>>(invoker, entityParameterExpression).Compile();
        //    return invokerCompiled;
        //}

        //public static Func<TObject, byte[], int, int> BuildSerializeGetterString<TObject>(FieldInfo field)
        //{
        //    Type targetType = typeof(TObject);
        //    ParameterExpression entityParameterExpression = Expression.Parameter(targetType);

        //    MemberExpression getterCall = Expression.Field(entityParameterExpression, field);


        //    ParameterExpression expressionBitConverterParameter = Expression.Parameter(typeof(string));
        //    ParameterExpression expressionBitConverterParameterBuffer = Expression.Parameter(typeof(byte[]));
        //    ParameterExpression expressionBitConverterParameterStartIndex = Expression.Parameter(typeof(int));
        //    MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(Helpers),
        //                        "GetBytesUTF8",
        //                        null, /* no generic type arguments */
        //                        expressionBitConverterParameter, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex);

        //    var invoker = Expression.Call(expressionCallBitConverter.Method, getterCall, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex);
        //    var invokerCompiled = Expression.Lambda<Func<TObject, byte[], int, int>>(invoker, entityParameterExpression, expressionBitConverterParameterBuffer, expressionBitConverterParameterStartIndex).Compile();
        //    return invokerCompiled;
        //}
        //public static Func<TObject, int> BuildSerializeSizeGetterString<TObject>(FieldInfo field)
        //{
        //    Type targetType = typeof(TObject);
        //    ParameterExpression entityParameterExpression = Expression.Parameter(targetType);

        //    MemberExpression getterCall = Expression.Field(entityParameterExpression, field);


        //    ParameterExpression expressionBitConverterParameter = Expression.Parameter(typeof(string));
        //    MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(Helpers),
        //                        "GetByteCountUTF8",
        //                        null, /* no generic type arguments */
        //                        expressionBitConverterParameter);

        //    var invoker = Expression.Call(expressionCallBitConverter.Method, getterCall);
        //    var invokerCompiled = Expression.Lambda<Func<TObject, int>>(invoker, entityParameterExpression).Compile();
        //    return invokerCompiled;
        //}

        //public static Action<TObject, byte[], int> BuildSerializeSetterString<TObject>(PropertyInfo property)
        //{
        //    Type targetType = typeof(TObject);
        //    ParameterExpression expressionBitConverterValueParameter = Expression.Parameter(typeof(byte[]));
        //    ParameterExpression expressionBitConverterStartParameter = Expression.Parameter(typeof(int));

        //    MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(Helpers),
        //                        @"GetString",
        //                        null, /* no generic type arguments */
        //                        expressionBitConverterValueParameter, expressionBitConverterStartParameter);


        //    MethodInfo setterMethodInfo = property.GetSetMethod();
        //    ParameterExpression entityParameterExpression = Expression.Parameter(targetType);
        //    ParameterExpression propertyParameterExpression = Expression.Parameter(typeof(string));
        //    MemberExpression propertySetterExpression = Expression.Property(entityParameterExpression, setterMethodInfo);

        //    BinaryExpression expressionAssign = Expression.Assign(propertySetterExpression, expressionCallBitConverter);

        //    Action<TObject, byte[], int> invokerCompiled = Expression.Lambda<Action<TObject, byte[], int>>(expressionAssign, entityParameterExpression, expressionBitConverterValueParameter, expressionBitConverterStartParameter).Compile();
        //    return invokerCompiled;
        //}
        //public static Action<TObject, byte[], int> BuildSerializeSetterString<TObject>(FieldInfo field)
        //{
        //    Type targetType = typeof(TObject);
        //    ParameterExpression expressionBitConverterValueParameter = Expression.Parameter(typeof(byte[]));
        //    ParameterExpression expressionBitConverterStartParameter = Expression.Parameter(typeof(int));

        //    MethodCallExpression expressionCallBitConverter = Expression.Call(typeof(Helpers),
        //                        @"GetString",
        //                        null, /* no generic type arguments */
        //                        expressionBitConverterValueParameter, expressionBitConverterStartParameter);


        //    ParameterExpression entityParameterExpression = Expression.Parameter(targetType);

        //    MemberExpression setterCall = Expression.Field(entityParameterExpression, field);

        //    BinaryExpression expressionAssign = Expression.Assign(setterCall, expressionCallBitConverter);

        //    Action<TObject, byte[], int> invokerCompiled = Expression.Lambda<Action<TObject, byte[], int>>(expressionAssign, entityParameterExpression, expressionBitConverterValueParameter, expressionBitConverterStartParameter).Compile();
        //    return invokerCompiled;
        //}
        #endregion


    }

}
