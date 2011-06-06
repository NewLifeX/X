using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NewLife.Collections;

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
        public static MethodInfoX Create(Type type, String name)
        {
            MethodInfo method = type.GetMethod(name);
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
            MethodInfo method = type.GetMethod(name, paramTypes);
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
        public override Object Invoke(Object obj, params  Object[] parameters)
        {
            if (parameters != null && parameters.Length == 0)
                return Handler.Invoke(obj, null);
            else
                return Handler.Invoke(obj, parameters);
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
            if (target == null || String.IsNullOrEmpty(name)) return default(TResult);

            MethodInfoX mix = Create(target.GetType(), name);
            if (mix == null) return default(TResult);

            return (TResult)mix.Invoke(target, parameters);
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