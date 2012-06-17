using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>通过委托对常见方法进行快速方法调用</summary>
    class FastMethod
    {
        #region 属性
        private MethodInfo _Method;
        /// <summary>方法</summary>
        public MethodInfo Method { get { return _Method; } set { _Method = value; } }

        private Boolean _Supported;
        /// <summary>是否支持</summary>
        public Boolean Supported { get { return _Supported; } }

        IFastHandler _Handler;
        #endregion

        #region 方法
        public FastMethod(MethodInfo method)
        {
            Method = method;
            _Supported = Check();
        }

        static IFastProvider _provider = new FastProvider();
        Boolean Check()
        {
            var rt = Method.ReturnType;
            var ps = Method.GetParameters();

            //if (ps == null || ps.Length < 1)
            //{
            //    if (CheckGetInt16()) return true;
            //}
            //else if (ps.Length == 1)
            //{

            //}
            if ((_Handler = _provider.Create(Method, ps, rt)) != null) return true;

            return false;
        }

        public Object Invoke(Object obj, params Object[] parameters)
        {
            if (_Handler == null) return null;

            return _Handler.Invoke(obj, parameters);
        }
        #endregion

        #region 接口
        /// <summary>快速调用委托</summary>
        interface IFastHandler
        {
            Object Invoke(Object obj, params Object[] parameters);
        }

        /// <summary>提供者。如果支持该方法，则创建实际IFastHandler</summary>
        interface IFastProvider
        {
            IFastHandler Create(MethodInfo method, ParameterInfo[] pis, Type retType);
        }
        #endregion

        #region 整型
        class FastProvider : IFastProvider
        {
            public IFastHandler Create(MethodInfo method, ParameterInfo[] pis, Type retType)
            {
                if (pis == null || pis.Length < 1)
                {
                    var tc = Type.GetTypeCode(retType);
                    switch (tc)
                    {
                        case TypeCode.Boolean:
                            return new FastHandlerGet<Boolean>(method);
                        case TypeCode.Byte:
                            return new FastHandlerGet<Byte>(method);
                        case TypeCode.Char:
                            return new FastHandlerGet<Char>(method);
                        case TypeCode.DBNull:
                            break;
                        case TypeCode.DateTime:
                            return new FastHandlerGet<DateTime>(method);
                        case TypeCode.Decimal:
                            return new FastHandlerGet<Decimal>(method);
                        case TypeCode.Double:
                            return new FastHandlerGet<Double>(method);
                        case TypeCode.Empty:
                            break;
                        case TypeCode.Int16:
                            return new FastHandlerGet<Int16>(method);
                        case TypeCode.Int32:
                            return new FastHandlerGet<Int32>(method);
                        case TypeCode.Int64:
                            return new FastHandlerGet<Int64>(method);
                        case TypeCode.SByte:
                            return new FastHandlerGet<SByte>(method);
                        case TypeCode.Single:
                            return new FastHandlerGet<Single>(method);
                        case TypeCode.String:
                            return new FastHandlerGet<String>(method);
                        case TypeCode.UInt16:
                            return new FastHandlerGet<UInt16>(method);
                        case TypeCode.UInt32:
                            return new FastHandlerGet<UInt32>(method);
                        case TypeCode.UInt64:
                            return new FastHandlerGet<UInt64>(method);
                        default:
                            break;
                    }
                }
                else if (pis.Length == 1)
                {
                    var tc = Type.GetTypeCode(retType);
                    switch (tc)
                    {
                        case TypeCode.Boolean:
                            return new FastHandlerSet<Boolean>(method);
                        case TypeCode.Byte:
                            return new FastHandlerSet<Byte>(method);
                        case TypeCode.Char:
                            return new FastHandlerSet<Char>(method);
                        case TypeCode.DBNull:
                            break;
                        case TypeCode.DateTime:
                            return new FastHandlerSet<DateTime>(method);
                        case TypeCode.Decimal:
                            return new FastHandlerSet<Decimal>(method);
                        case TypeCode.Double:
                            return new FastHandlerSet<Double>(method);
                        case TypeCode.Empty:
                            break;
                        case TypeCode.Int16:
                            return new FastHandlerSet<Int16>(method);
                        case TypeCode.Int32:
                            return new FastHandlerSet<Int32>(method);
                        case TypeCode.Int64:
                            return new FastHandlerSet<Int64>(method);
                        case TypeCode.SByte:
                            return new FastHandlerSet<SByte>(method);
                        case TypeCode.Single:
                            return new FastHandlerSet<Single>(method);
                        case TypeCode.String:
                            return new FastHandlerSet<String>(method);
                        case TypeCode.UInt16:
                            return new FastHandlerSet<UInt16>(method);
                        case TypeCode.UInt32:
                            return new FastHandlerSet<UInt32>(method);
                        case TypeCode.UInt64:
                            return new FastHandlerSet<UInt64>(method);
                        default:
                            break;
                    }
                }

                return null;
            }

            class FastHandlerGet<T> : IFastHandler
            {
                public Func<T> Handler;
                private MethodInfo _Method;

                public FastHandlerGet(MethodInfo method)
                {
                    _Method = method;
                    //Handler = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), method);
                }

                public Object Invoke(Object obj, params Object[] parameters)
                {
                    var h = Handler;
                    if (obj != null)
                        h = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), obj, _Method);
                    else if (h == null)
                        h = Handler = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), _Method);
                    if (h == null) return null;

                    return h();
                }
            }

            class FastHandlerSet<T> : IFastHandler
            {
                public Action<T> Handler;
                private MethodInfo _Method;

                public FastHandlerSet(MethodInfo method)
                {
                    _Method = method;
                    //Handler = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), method);
                }

                public Object Invoke(Object obj, params Object[] parameters)
                {
                    var h = Handler;
                    if (obj != null)
                        h = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), obj, _Method);
                    else if (h == null)
                        h = Handler = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), _Method);
                    if (h == null) return null;

                    h((T)parameters[0]);
                    return null;
                }
            }
        }
        #endregion
    }
}