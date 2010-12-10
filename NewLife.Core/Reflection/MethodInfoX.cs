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
                if (_Handler == null) _Handler = CreateDelegate<FastInvokeHandler>(Method, typeof(Object), new Type[] { typeof(Object), typeof(Object[]) });
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
            if (method == null) method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (method == null) return null;

            return Create(method);
        }
        #endregion

        #region 创建动态方法
        //private FastInvokeHandler GetMethodInvoker(MethodInfo method)
        //{
        //    // 定义一个没有名字的动态方法。
        //    // 关联到模块，并且跳过JIT可见性检查，可以访问所有类型的所有成员
        //    DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object), typeof(Object[]) }, method.DeclaringType.Module, true);
        //    ILGenerator il = dynamicMethod.GetILGenerator();

        //    GetMethodInvoker(il, method);

        //    FastInvokeHandler invoder = (FastInvokeHandler)dynamicMethod.CreateDelegate(typeof(FastInvokeHandler));
        //    return invoder;
        //}

        //internal static void GetMethodInvoker(ILGenerator il, MethodInfo method)
        //{
        //    EmitHelper help = new EmitHelper(il);

        //    // 为引用类型建立本地变量
        //    //Int32 refParams = 0;
        //    //if (HasRefParam(method))
        //    //{
        //    //    refParams = helper.CreateLocalsForByRefParams(method);
        //    //}

        //    if (!method.IsStatic) il.Emit(OpCodes.Ldarg_0);

        //    //准备参数
        //    //ParameterInfo[] ps = method.GetParameters();
        //    //for (int i = 0; i < ps.Length; i++)
        //    //{
        //    //    il.Emit(OpCodes.Ldarg_1);
        //    //    EmitFastInt(il, i);
        //    //    il.Emit(OpCodes.Ldelem_Ref);
        //    //    if (ps[i].ParameterType.IsValueType)
        //    //        il.Emit(OpCodes.Unbox_Any, ps[i].ParameterType);
        //    //    else
        //    //        il.Emit(OpCodes.Castclass, ps[i].ParameterType);
        //    //}
        //    help.PushParams(method)
        //        .Call(method)
        //        .Ret(method);

        //    //调用目标方法
        //    //if (method.IsVirtual)
        //    //    il.EmitCall(OpCodes.Callvirt, method, null);
        //    //else
        //    //    il.EmitCall(OpCodes.Call, method, null);

        //    ////处理返回值
        //    //if (method.ReturnType == typeof(void))
        //    //    il.Emit(OpCodes.Ldnull);
        //    //else if (method.ReturnType.IsValueType)
        //    //    il.Emit(OpCodes.Box, method.ReturnType);

        //    //il.Emit(OpCodes.Ret);
        //}

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
