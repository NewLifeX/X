using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using NewLife.Collections;
using NewLife.Exceptions;

namespace NewLife.Reflection
{
    /// <summary>
    /// 快速调用。基于DynamicMethod和Emit实现。
    /// </summary>
    public class MethodInfoX : MemberInfoX
    {
        #region 属性
        private MethodInfo _Method;
        /// <summary>目标方法</summary>
        public MethodInfo Method
        {
            get { return _Method; }
            private set { _Method = value; }
        }

        FastInvokeHandler _Handler;
        /// <summary>
        /// 快速调用委托，延迟到首次使用才创建
        /// </summary>
        FastInvokeHandler Handler
        {
            get
            {
                //if (_Handler == null) _Handler = CreateDelegate<FastInvokeHandler>(Method, typeof(Object), new Type[] { typeof(Object), typeof(Object[]) });
                if (_Handler == null) _Handler = GetMethodInvoker(Method);
                return _Handler;
            }
        }
        #endregion

        #region 构造
        private MethodInfoX(MethodInfo method) : base(method) { Method = method; }

        private static DictionaryCache<MethodInfo, MethodInfoX> cache = new DictionaryCache<MethodInfo, MethodInfoX>();
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static MethodInfoX Create(MethodInfo method)
        {
            if (method == null) return null;

            return cache.GetItem(method, delegate(MethodInfo key)
            {
                return new MethodInfoX(key);
            });
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public new static MethodInfoX Create(Type type, String name)
        {
            var method = type.GetMethod(name);
            if (method == null) method = type.GetMethod(name, DefaultBinding);
            if (method == null) method = type.GetMethod(name, DefaultBinding | BindingFlags.IgnoreCase);
            if (method == null && type.BaseType != typeof(Object)) return Create(type.BaseType, name);
            if (method == null) return null;

            return Create(method);
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="paramTypes">参数类型</param>
        /// <returns></returns>
        public static MethodInfoX Create(Type type, String name, Type[] paramTypes)
        {
            var method = type.GetMethod(name, paramTypes);
            if (method == null) method = type.GetMethod(name, DefaultBinding, null, paramTypes, null);
            if (method == null) method = type.GetMethod(name, DefaultBinding | BindingFlags.IgnoreCase, null, paramTypes, null);
            if (method == null && type.BaseType != typeof(Object)) return Create(type.BaseType, name, paramTypes);
            if (method == null) return null;

            return Create(method);
        }
        #endregion

        #region 创建动态方法
        private FastInvokeHandler GetMethodInvoker(MethodInfo method)
        {
            // 定义一个没有名字的动态方法。
            // 关联到模块，并且跳过JIT可见性检查，可以访问所有类型的所有成员
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object), typeof(Object[]) }, method.DeclaringType.Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();

            GetMethodInvoker(il, method);
#if DEBUG
            //SaveIL(dynamicMethod, delegate(ILGenerator il2)
            //{
            //    GetMethodInvoker(il2, method);
            //});
#endif

            return (FastInvokeHandler)dynamicMethod.CreateDelegate(typeof(FastInvokeHandler));
        }

        static void GetMethodInvoker(ILGenerator il, MethodInfo method)
        {
            EmitHelper help = new EmitHelper(il);
            Type retType = method.ReturnType;

            //if (!method.IsStatic) il.Emit(OpCodes.Ldarg_0);
            if (!method.IsStatic) help.Ldarg(0).CastFromObject(method.DeclaringType);

            // 方法的参数数组放在动态方法的第二位，所以是1
            help.PushParams(1, method)
                .Call(method)
                .BoxIfValueType(retType);

            //处理返回值，如果调用的方法没有返回值，则需要返回一个空
            if (retType == null || retType == typeof(void))
                help.Ldnull().Ret();
            else
                help.Ret();

            //调用目标方法
            //if (method.IsVirtual)
            //    il.EmitCall(OpCodes.Callvirt, method, null);
            //else
            //    il.EmitCall(OpCodes.Call, method, null);

            ////处理返回值
            //if (method.ReturnType == typeof(void))
            //    il.Emit(OpCodes.Ldnull);
            //else if (method.ReturnType.IsValueType)
            //    il.Emit(OpCodes.Box, method.ReturnType);

            //il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// 快速调用委托
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        delegate Object FastInvokeHandler(Object obj, Object[] parameters);
        #endregion

        #region 调用
        /// <summary>
        /// 参数调用
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public override Object Invoke(Object obj, params  Object[] parameters)
        {
            if (parameters == null || parameters.Length <= 0)
                return Handler.Invoke(obj, null);
            else
            {
                // 预处理参数类型
                ParameterInfo[] pis = Method.GetParameters();
                for (int i = 0; i < parameters.Length && i < pis.Length; i++)
                {
                    parameters[i] = TypeX.ChangeType(parameters[i], pis[i].ParameterType);
                }

                return Handler.Invoke(obj, parameters);
            }
        }

        /// <summary>
        /// 通过字典参数调用
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Object InvokeByDictionaryParameter(Object obj, IDictionary parameters)
        {
            //// 没有传入参数
            //if (parameters == null || parameters.Count < 1) return Invoke(obj, null);
            //! 注意：有可能没有传入参数，但是该方法是需要参数的，这个时候采用全部null的方法

            // 该方法没有参数，无视外部传入参数
            ParameterInfo[] pis = Method.GetParameters();
            if (pis == null || pis.Length < 1) return Invoke(obj, null);

            Object[] ps = new Object[pis.Length];
            for (int i = 0; i < pis.Length; i++)
            {
                Object v = null;
                if (parameters != null && parameters.Contains(pis[i].Name)) v = parameters[pis[i].Name];
                ps[i] = TypeX.ChangeType(v, pis[i].ParameterType);
            }

            return Handler.Invoke(obj, ps);
        }

        /// <summary>
        /// 快速调用方法成员
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static TResult Invoke<TResult>(Object target, String name, params  Object[] parameters)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            MethodInfoX mix = Create(target.GetType(), name, TypeX.GetTypeArray(parameters));
            if (mix == null) throw new XException("类{0}中无法找到{1}方法！", target.GetType().Name, name);

            return (TResult)mix.Invoke(target, parameters);
        }

        /// <summary>
        /// 快速调用静态方法
        /// </summary>
        /// <typeparam name="TTarget">目标类型</typeparam>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static TResult Invoke<TTarget, TResult>(String name, params  Object[] parameters)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            MethodInfoX mix = Create(typeof(TTarget), name, TypeX.GetTypeArray(parameters));
            if (mix == null) throw new XException("类{0}中无法找到{1}方法！", typeof(TTarget).Name, name);

            return (TResult)mix.Invoke(null, parameters);
        }

        /// <summary>
        /// 通过传入参数字典快速调用方法
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static TResult InvokeByDictionaryParameter<TResult>(Object target, String name, IDictionary parameters)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            MethodInfoX mix = Create(target.GetType(), name);
            if (mix == null) throw new XException("类{0}中无法找到{1}方法！", target.GetType().Name, name);

            return (TResult)mix.InvokeByDictionaryParameter(target, parameters);
        }

        /// <summary>
        /// 通过传入参数字典快速调用静态方法
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static TResult InvokeByDictionaryParameter<TTarget, TResult>(String name, IDictionary parameters)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            MethodInfoX mix = Create(typeof(TTarget), name);
            if (mix == null) throw new XException("类{0}中无法找到{1}方法！", typeof(TTarget).Name, name);

            return (TResult)mix.InvokeByDictionaryParameter(null, parameters);
        }
        #endregion

        #region 类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator MethodInfo(MethodInfoX obj)
        {
            return obj != null ? obj.Method : null;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator MethodInfoX(MethodInfo obj)
        {
            return obj != null ? Create(obj) : null;
        }
        #endregion
    }
}