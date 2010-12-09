using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NewLife.Collections;
using System.Reflection.Emit;

namespace NewLife.Reflection
{
    /// <summary>
    /// 快速调用构造函数。基于DynamicMethod和Emit实现。
    /// </summary>
    public class ConstructorInfoX : MemberInfoX
    {
        #region 属性
        private ConstructorInfo _Constructor;
        /// <summary>目标方法</summary>
        public ConstructorInfo Constructor
        {
            get { return _Constructor; }
            private set { _Constructor = value; }
        }

        FastCreateInstanceHandler _Handler;
        /// <summary>
        /// 快速调用委托，延迟到首次使用才创建
        /// </summary>
        FastCreateInstanceHandler Handler
        {
            get
            {
                if (_Handler == null) _Handler = CreateDelegate<FastCreateInstanceHandler>(Constructor);
                return _Handler;
            }
        }
        #endregion

        #region 扩展属性
        //private Type[] _ParamTypes;
        ///// <summary>参数类型数组</summary>
        //public Type[] ParamTypes
        //{
        //    get
        //    {
        //        if (_ParamTypes == null)
        //        {
        //            _ParamTypes = Type.EmptyTypes;

        //            ParameterInfo[] pis = Constructor.GetParameters();
        //            if (pis != null && pis.Length > 0)
        //            {
        //                List<Type> list = new List<Type>();
        //                foreach (ParameterInfo item in pis)
        //                {
        //                    list.Add(item.ParameterType);
        //                }
        //                _ParamTypes = list.ToArray();
        //            }
        //        }
        //        return _ParamTypes;
        //    }
        //}
        #endregion

        #region 构造
        private ConstructorInfoX(ConstructorInfo constructor) : base(constructor) { Constructor = constructor; }

        private static DictionaryCache<ConstructorInfo, ConstructorInfoX> cache = new DictionaryCache<ConstructorInfo, ConstructorInfoX>();
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="constructor"></param>
        /// <returns></returns>
        public static ConstructorInfoX Create(ConstructorInfo constructor)
        {
            if (constructor == null) return null;

            return cache.GetItem(constructor, delegate(ConstructorInfo key)
            {
                return new ConstructorInfoX(key);
            });
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="type"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public static ConstructorInfoX Create(Type type, Type[] types)
        {
            ConstructorInfo constructor = type.GetConstructor(types);
            if (constructor == null) constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
            if (constructor == null) return null;

            return Create(constructor);
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ConstructorInfoX Create(Type type)
        {
            return Create(type, Type.EmptyTypes);
        }
        #endregion

        #region 创建动态方法
        //private FastCreateInstanceHandler GetConstructorInvoker()
        //{
        //    // 定义一个没有名字的动态方法。
        //    // 关联到模块，并且跳过JIT可见性检查，可以访问所有类型的所有成员
        //    DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object), typeof(Object[]) }, Constructor.DeclaringType.Module, true);
        //    ILGenerator il = dynamicMethod.GetILGenerator();

        //    GetMethodInvoker(il);

        //    FastCreateInstanceHandler invoder = (FastCreateInstanceHandler)dynamicMethod.CreateDelegate(typeof(FastCreateInstanceHandler));
        //    return invoder;
        //}

        internal static void GetMethodInvoker(ILGenerator il, ConstructorInfo method)
        {
            Type targetType = method.DeclaringType;

            //准备参数
            ParameterInfo[] ps = method.GetParameters();

            if (targetType.IsValueType || ps == null || ps.Length < 1)
            {
                // 值类型和无参数类型

                // 声明目标类型的本地变量
                il.DeclareLocal(targetType);
                // 加载地址
                il.Emit(OpCodes.Ldloca_S, 0);
                // 创建对象
                il.Emit(OpCodes.Initobj, targetType);
                // 加载对象
                il.Emit(OpCodes.Ldloc_0);
            }
            else if (targetType.IsArray)
            {
                // 数组类型

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldelem_Ref);
                il.Emit(OpCodes.Unbox_Any, typeof(Int32));
                il.Emit(OpCodes.Newarr, targetType.GetElementType());
            }
            else
            {
                // 其它类型
                EmitHelper help = new EmitHelper(il);

                help.PushParams(method);

                // 创建对象
                il.Emit(OpCodes.Initobj, targetType);
            }

            // 是否需要装箱
            if (targetType.IsValueType) il.Emit(OpCodes.Box, targetType);

            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// 快速调用委托
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        delegate Object FastCreateInstanceHandler(Object[] parameters);
        #endregion

        #region 调用
        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override Object CreateInstance(Object[] parameters)
        {
            return Handler.Invoke(parameters);
        }
        #endregion

        #region 类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator ConstructorInfo(ConstructorInfoX obj)
        {
            return obj != null ? obj.Constructor : null;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator ConstructorInfoX(ConstructorInfo obj)
        {
            return obj != null ? Create(obj) : null;
        }
        #endregion
    }
}
