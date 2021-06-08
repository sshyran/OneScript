﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the
Mozilla Public License, v.2.0. If a copy of the MPL
was not distributed with this file, You can obtain one
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/

using System;
using System.Runtime.InteropServices;
using OneScript.Commons;
using OneScript.Contexts;
using OneScript.Contexts.Reflection;
using OneScript.Types;
using OneScript.Values;
using ScriptEngine.Machine;

namespace OneScript.StandardLibrary.NativeApi
{
    /// <summary>
    /// Экземпляр внешней компоненты Native API
    /// </summary>
    public class NativeApiComponent : NativeApiValue, IRuntimeContextInstance, IValue
    {
        private readonly IntPtr _object;

        public NativeApiComponent()
        {
        }
        
        public override IRuntimeContextInstance AsObject()
        {
            return this;
        }

        public event OnComponentEvent OnComponentEvent;

        public event OnComponentError OnComponentError;

        public event OnComponentStatusText OnComponentStatusText;
        
        private enum ErrorCodes
        {
            ADDIN_E_NONE = 1000,
            ADDIN_E_ORDINARY = 1001,
            ADDIN_E_ATTENTION = 1002,
            ADDIN_E_IMPORTANT = 1003,
            ADDIN_E_VERY_IMPORTANT = 1004,
            ADDIN_E_INFO = 1005,
            ADDIN_E_FAIL = 1006,
            ADDIN_E_MSGBOX_ATTENTION = 1007,
            ADDIN_E_MSGBOX_INFO = 1008,
            ADDIN_E_MSGBOX_FAIL = 1009,
        }

        private static string S(IntPtr ptr)
        {
            return NativeApiProxy.Str(ptr);
        }

        private MessageStatusEnum Status(ushort wcode)
        {
            switch ((ErrorCodes)wcode)
            {
                case ErrorCodes.ADDIN_E_NONE:
                    return MessageStatusEnum.WithoutStatus;
                case ErrorCodes.ADDIN_E_ORDINARY:
                    return MessageStatusEnum.Ordinary;
                case ErrorCodes.ADDIN_E_IMPORTANT:
                    return MessageStatusEnum.Important;
                case ErrorCodes.ADDIN_E_INFO:
                    return MessageStatusEnum.Information;
                case ErrorCodes.ADDIN_E_VERY_IMPORTANT:
                case ErrorCodes.ADDIN_E_FAIL:
                    return MessageStatusEnum.VeryImportant;
                case ErrorCodes.ADDIN_E_ATTENTION:
                case ErrorCodes.ADDIN_E_MSGBOX_ATTENTION:
                case ErrorCodes.ADDIN_E_MSGBOX_INFO:
                case ErrorCodes.ADDIN_E_MSGBOX_FAIL:
                    return MessageStatusEnum.Attention;
                default:
                    return MessageStatusEnum.Ordinary;
            }
        }

        public NativeApiComponent(object host, NativeApiLibrary library, TypeDescriptor typeDef, string componentName)
        {
            _object = NativeApiProxy.GetClassObject(library.Module, componentName,
                (wcode, source, descr, scode) =>
                {
                    OnComponentError?.Invoke(Status(wcode), scode, S(source), S(descr));
                },
                (source, message, data) => {
                    OnComponentEvent?.Invoke(S(source), S(message), S(data));
                },
                (status) => {
                    OnComponentStatusText?.Invoke(S(status));
                }
            );
            DefineType(typeDef);
        }

        public void Dispose()
        {
            try { NativeApiProxy.DestroyObject(_object); } catch (Exception) { }
        }

        public bool IsIndexed => false;

        public bool DynamicMethodSignatures => false;

        public IValue GetIndexedValue(IValue index)
        {
            throw new NotImplementedException();
        }

        public void SetIndexedValue(IValue index, IValue val)
        {
            throw new NotImplementedException();
        }

        public int FindProperty(string name)
        {
            var propNumber = NativeApiProxy.FindProp(_object, name);
            if (propNumber < 0)
                throw OldRuntimeException.PropNotFoundException(name);
            return propNumber;
        }

        public bool IsPropReadable(int propNum)
        {
            return NativeApiProxy.IsPropReadable(_object, propNum);
        }

        public bool IsPropWritable(int propNum)
        {
            return NativeApiProxy.IsPropWritable(_object, propNum);
        }

        public int GetPropCount()
        {
            return NativeApiProxy.GetNProps(_object);
        }

        public string GetPropName(int propNum)
        {
            var name = string.Empty;
            NativeApiProxy.GetPropName(_object, propNum, 0,
                n => name = NativeApiProxy.Str(n)
            );
            return name;
        }

        public IValue GetPropValue(int propNum)
        {
            var result = ValueFactory.Create();
            NativeApiProxy.GetPropVal(_object, propNum,
                variant => result = NativeApiVariant.GetValue(variant)
            );
            return result;
        }

        public void SetPropValue(int propNum, IValue newVal)
        {
            var variant = new NativeApiVariant();
            variant.SetValue(newVal);
            NativeApiProxy.SetPropVal(_object, propNum, ref variant);
            variant.Clear();
        }

        public int GetMethodsCount()
        {
            return NativeApiProxy.GetNMethods(_object);
        }

        public int FindMethod(string name)
        {
            var methodNumber = NativeApiProxy.FindMethod(_object, name);
            if (methodNumber < 0)
                throw RuntimeException.MethodNotFoundException(name);
            return methodNumber;
        }

        public BslMethodInfoBase GetMethodInfo(int methodNumber)
        {
            if (methodNumber < 0)
                throw new RuntimeException("Метод не найден");
            var name = string.Empty;
            var alias = string.Empty;
            NativeApiProxy.GetMethodName(_object, methodNumber, 0,
                str => name = NativeApiProxy.Str(str)
            );
            NativeApiProxy.GetMethodName(_object, methodNumber, 1,
                str => alias = NativeApiProxy.Str(str)
            );
            var paramCount = NativeApiProxy.GetNParams(_object, methodNumber);
            var paramArray = new BslParameterInfo[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                var localCopyOfIndex = i;
                NativeApiProxy.GetParamDefValue(_object, methodNumber, i, variant =>
                {
                    if (NativeApiVariant.NotEmpty(variant))
                    {
                        paramArray[localCopyOfIndex].SetDefaultValue(default);
                    }
                });
            }

            var mi = new NativeApiMethodInfo();
            mi.SetName(name);
            mi.SetAlias(alias);
            if(NativeApiProxy.HasRetVal(_object, methodNumber))
                mi.SetReturnType(typeof(BslValue));
            mi.SetPrivate(false);
            mi.Parameters.AddRange(paramArray);

            return mi;
        }

        public BslPropertyInfoBase GetPropertyInfo(int propertyNumber)
        {
            var propName = GetPropName(propertyNumber);
            var isReadable = IsPropReadable(propertyNumber);
            var isWritable = IsPropWritable(propertyNumber);

            var propInfo = new CustomizablePropertyInfo();
            propInfo.SetName(propName);
            propInfo.SetAccessibility(isReadable, isWritable);
            propInfo.SetDispatchId(propertyNumber);

            return propInfo;
        }

        private void SetDefValues(int methodNumber, int paramCount, IValue[] arguments)
        {
            for (int i = 0; i < paramCount; i++)
                if (arguments[i] == null)
                    NativeApiProxy.GetParamDefValue(_object, methodNumber, i,
                        variant => arguments[i] = NativeApiVariant.GetValue(variant)
                    );
        }

        public void CallAsProcedure(int methodNumber, IValue[] arguments)
        {
            var paramArray = IntPtr.Zero;
            int paramCount = NativeApiProxy.GetNParams(_object, methodNumber);
            if (paramCount > 0)
                paramArray = Marshal.AllocHGlobal(NativeApiVariant.Size * paramCount);
            SetDefValues(methodNumber, paramCount, arguments);
            NativeApiVariant.SetValue(paramArray, arguments, paramCount);
            NativeApiProxy.CallAsProc(_object, methodNumber, paramArray);
            NativeApiVariant.GetValue(arguments, paramArray, paramCount);
            NativeApiVariant.Clear(paramArray, paramCount);
            Marshal.FreeHGlobal(paramArray);
        }

        public void CallAsFunction(int methodNumber, IValue[] arguments, out IValue retValue)
        {
            var paramArray = IntPtr.Zero;
            int paramCount = NativeApiProxy.GetNParams(_object, methodNumber);
            if (paramCount > 0)
                paramArray = Marshal.AllocHGlobal(NativeApiVariant.Size * paramCount);
            SetDefValues(methodNumber, paramCount, arguments);
            NativeApiVariant.SetValue(paramArray, arguments, paramCount);
            IValue result = retValue = ValueFactory.Create();
            bool ok = NativeApiProxy.CallAsFunc(_object, methodNumber, paramArray,
                variant => result = NativeApiVariant.GetValue(variant)
            );
            NativeApiVariant.GetValue(arguments, paramArray, paramCount);
            NativeApiVariant.Clear(paramArray, paramCount);
            Marshal.FreeHGlobal(paramArray);
            if (ok)
                retValue = result;
        }
    }
}
