using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace XCode.Common
{
    /// <summary>
    /// 快速调用。基于DynamicMethod和Emit实现。
    /// </summary>
    class MethodInfoEx
    {
        #region 属性
        private MethodInfo _Method;
        /// <summary>目标方法</summary>
        public MethodInfo Method
        {
            get { return _Method; }
            private set { _Method = value; }
        }

        FastInvokeHandler handler;
        #endregion

        #region 构造
        private static Dictionary<MethodInfo, MethodInfoEx> cache = new Dictionary<MethodInfo, MethodInfoEx>();
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static MethodInfoEx Create(MethodInfo method)
        {
            if (method == null) return null;

            if (cache.ContainsKey(method)) return cache[method];
            lock (cache)
            {
                if (cache.ContainsKey(method)) return cache[method];

                MethodInfoEx entity = new MethodInfoEx();

                entity.Method = method;
                entity.handler = GetMethodInvoker(method);

                cache.Add(method, entity);

                return entity;
            }
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static MethodInfoEx Create(Type type, String name)
        {
            MethodInfo method = type.GetMethod(name);
            if (method == null) method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (method == null) return null;

            return Create(method);
        }
        #endregion

        #region 创建动态方法
        private static FastInvokeHandler GetMethodInvoker(MethodInfo method)
        {
            // 定义一个没有名字的动态方法。
            // 关联到模块，并且跳过JIT可见性检查，可以访问所有类型的所有成员
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object), typeof(Object[]) }, method.DeclaringType.Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();

            GetMethodInvoker(il, method);

            FastInvokeHandler invoder = (FastInvokeHandler)dynamicMethod.CreateDelegate(typeof(FastInvokeHandler));
            return invoder;
        }

        internal static void GetMethodInvoker(ILGenerator il, MethodInfo method)
        {
            if (!method.IsStatic) il.Emit(OpCodes.Ldarg_0);

            //准备参数
            ParameterInfo[] ps = method.GetParameters();
            for (int i = 0; i < ps.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                EmitFastInt(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                if (ps[i].ParameterType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, ps[i].ParameterType);
                else
                    il.Emit(OpCodes.Castclass, ps[i].ParameterType);
            }

            //调用目标方法
            if (method.IsVirtual)
                il.EmitCall(OpCodes.Callvirt, method, null);
            else
                il.EmitCall(OpCodes.Call, method, null);

            //处理返回值
            if (method.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else if (method.ReturnType.IsValueType)
                il.Emit(OpCodes.Box, method.ReturnType);

            il.Emit(OpCodes.Ret);
        }

        private static void EmitFastInt(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
                il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
            else
                il.Emit(OpCodes.Ldc_I4, value);
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
        /// 有参数调用
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Object Invoke(Object obj, Object[] parameters)
        {
            return handler.Invoke(obj, parameters);
        }
        #endregion

        #region 转换
        /// <summary>
        /// 把指定方法转为快速调用方法
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static implicit operator MethodInfoEx(MethodInfo method)
        {
            return Create(method);
        }
        #endregion
    }
}
