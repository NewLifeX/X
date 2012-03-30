using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using NewLife.Collections;

namespace NewLife.Reflection
{
    /// <summary>快速调用构造函数。基于DynamicMethod和Emit实现。</summary>
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

        FastHandler _Handler;
        /// <summary>快速调用委托，延迟到首次使用才创建</summary>
        FastHandler Handler
        {
            get
            {
                //if (_Handler == null) _Handler = CreateDelegate<FastCreateInstanceHandler>(Constructor, typeof(Object), new Type[] { typeof(Object[]) });
                if (_Handler == null) _Handler = GetConstructorInvoker(Constructor);
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
        /// <summary>创建</summary>
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

        /// <summary>创建</summary>
        /// <param name="type"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public static ConstructorInfoX Create(Type type, Type[] types)
        {
            ConstructorInfo constructor = type.GetConstructor(types);
            if (constructor == null) constructor = type.GetConstructor(DefaultBinding, null, types, null);
            if (constructor != null) return Create(constructor);

            //ListX<ConstructorInfo> list = TypeX.Create(type).Constructors;
            //if (list != null && list.Count > 0)
            //{
            //    if (types == null || types.Length <= 1) return list[0];

            //    ListX<ConstructorInfo> list2 = new ListX<ConstructorInfo>();
            //    foreach (ConstructorInfo item in list)
            //    {
            //        ParameterInfo[] ps = item.GetParameters();
            //        if (ps == null || ps.Length < 1 || ps.Length != types.Length) continue;

            //        for (int i = 0; i < ps.Length; i++)
            //        {

            //        }
            //    }
            //}

            //// 基本不可能的错误，因为每个类都会有构造函数
            //throw new Exception("无法找到构造函数！");

            return null;
        }

        /// <summary>创建</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ConstructorInfoX Create(Type type)
        {
            return Create(type, Type.EmptyTypes);
        }
        #endregion

        #region 创建动态方法
        delegate Object FastHandler(Object[] parameters);

        private static FastHandler GetConstructorInvoker(ConstructorInfo constructor)
        {
            // 定义一个没有名字的动态方法。
            // 关联到模块，并且跳过JIT可见性检查，可以访问所有类型的所有成员
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object[]) }, constructor.DeclaringType.Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();

            EmitHelper help = new EmitHelper(il);
            Type target = constructor.DeclaringType;
            if (target.IsValueType)
                help.NewValueType(target).BoxIfValueType(target).Ret();
            else if (target.IsArray)
                help.PushParams(0, new Type[] { typeof(Int32) }).NewArray(target.GetElementType()).Ret();
            else
                help.PushParams(0, constructor).NewObj(constructor).Ret();

            return (FastHandler)dynamicMethod.CreateDelegate(typeof(FastHandler));
        }
        #endregion

        #region 调用
        /// <summary>创建实例</summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public override Object CreateInstance(params Object[] parameters)
        {
            return Handler.Invoke(parameters);
        }
        #endregion

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator ConstructorInfo(ConstructorInfoX obj)
        {
            return obj != null ? obj.Constructor : null;
        }

        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator ConstructorInfoX(ConstructorInfo obj)
        {
            return obj != null ? Create(obj) : null;
        }
        #endregion
    }
}
