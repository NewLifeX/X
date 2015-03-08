using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using NewLife.Collections;

namespace NewLife.Reflection
{
    /// <summary>快速属性访问</summary>
    public class PropertyInfoX : MemberInfoX
    {
        #region 属性
        private PropertyInfo _Property;
        /// <summary>目标属性</summary>
        public PropertyInfo Property
        {
            get { return _Property; }
            set { _Property = value; }
        }

        private List<String> hasLoad = new List<String>();

        private MethodInfo _GetMethod;
        /// <summary>读取方法</summary>
        public MethodInfo GetMethod
        {
            get
            {
                if (_GetMethod == null && !hasLoad.Contains("GetMethod"))
                {
                    _GetMethod = Property.GetGetMethod();
                    if (_GetMethod == null) _GetMethod = Property.GetGetMethod(true);
                    hasLoad.Add("GetMethod");
                }
                return _GetMethod;
            }
            set { _GetMethod = value; }
        }

        private MethodInfo _SetMethod;
        /// <summary>设置方法</summary>
        public MethodInfo SetMethod
        {
            get
            {
                if (_SetMethod == null && !hasLoad.Contains("SetMethod"))
                {
                    _SetMethod = Property.GetSetMethod();
                    if (_SetMethod == null) _SetMethod = Property.GetSetMethod(true);
                    hasLoad.Add("SetMethod");
                }
                return _SetMethod;
            }
            set { _SetMethod = value; }
        }

        FastGetValueHandler _GetHandler;
        /// <summary>快速调用委托，延迟到首次使用才创建</summary>
        FastGetValueHandler GetHandler
        {
            get
            {
                //if (_GetHandler == null && GetMethod != null)
                //_GetHandler = CreateDelegate<FastGetValueHandler>(GetMethod, typeof(Object), new Type[] { typeof(Object) });
                if (_GetHandler == null && GetMethod != null) _GetHandler = GetValueInvoker(GetMethod);

                return _GetHandler;
            }
        }

        FastSetValueHandler _SetHandler;
        /// <summary>快速调用委托，延迟到首次使用才创建</summary>
        FastSetValueHandler SetHandler
        {
            get
            {
                //if (_SetHandler == null && SetMethod != null)
                //    _SetHandler = CreateDelegate<FastSetValueHandler>(SetMethod, null, new Type[] { typeof(Object), typeof(Object[]) });
                if (_SetHandler == null && SetMethod != null) _SetHandler = SetValueInvoker(SetMethod);

                return _SetHandler;
            }
        }
        #endregion

        #region 构造
        private PropertyInfoX(PropertyInfo property) : base(property) { Property = property; }

        private static DictionaryCache<PropertyInfo, PropertyInfoX> cache = new DictionaryCache<PropertyInfo, PropertyInfoX>();
        /// <summary>创建</summary>
        /// <param name="property">属性</param>
        /// <returns></returns>
        public static PropertyInfoX Create(PropertyInfo property)
        {
            if (property == null) return null;

            return cache.GetItem(property, key => new PropertyInfoX(key));
        }

        /// <summary>创建</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public new static PropertyInfoX Create(Type type, String name)
        {
            // name不判断0长度字符串，因为0长度字符串可以做标识符
            if (type == null || name == null) return null;

            var property = type.GetProperty(name);
            if (property == null) property = type.GetProperty(name, DefaultBinding);
            if (property == null) property = type.GetProperty(name, DefaultBinding | BindingFlags.IgnoreCase);
            if (property == null)
            {
                var ps = type.GetProperties();
                foreach (var item in ps)
                {
                    if (item.Name.EqualIgnoreCase(name))
                    {
                        property = item;
                        break;
                    }
                }
            }
            if (property == null && type.BaseType != null && type.BaseType != typeof(Object)) return Create(type.BaseType, name);
            if (property == null) return null;

            return Create(property);
        }
        #endregion

        #region 创建动态方法
        delegate Object FastGetValueHandler(Object obj);
        delegate void FastSetValueHandler(Object obj, Object value);

        private static FastGetValueHandler GetValueInvoker(MethodInfo method)
        {
            //定义一个没有名字的动态方法
            var dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object) }, method.DeclaringType.Module, true);
            var il = dynamicMethod.GetILGenerator();

            //if (!method.IsStatic) il.Emit(OpCodes.Ldarg_0);
            if (!method.IsStatic) il.Ldarg(0).CastFromObject(method.DeclaringType);
            // 目标方法没有参数
            il.Call(method)
                .BoxIfValueType(method.ReturnType)
                .Ret();

            return (FastGetValueHandler)dynamicMethod.CreateDelegate(typeof(FastGetValueHandler));
        }

        private static FastSetValueHandler SetValueInvoker(MethodInfo method)
        {
            //定义一个没有名字的动态方法
            var dynamicMethod = new DynamicMethod(String.Empty, null, new Type[] { typeof(Object), typeof(Object) }, method.DeclaringType.Module, true);
            var il = dynamicMethod.GetILGenerator();

            //if (!method.IsStatic) il.Emit(OpCodes.Ldarg_0);
            if (!method.IsStatic) il.Ldarg(0).CastFromObject(method.DeclaringType);
            // 目标方法只有一个参数
            il.Ldarg(1)
                .CastFromObject(method.GetParameters()[0].ParameterType)
                .Call(method)
                .Ret();

            return (FastSetValueHandler)dynamicMethod.CreateDelegate(typeof(FastSetValueHandler));
        }
        #endregion

        #region 调用
        /// <summary>取值</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [DebuggerHidden]
        [DebuggerStepThrough]
        public override Object GetValue(Object obj)
        {
            if (GetHandler == null) throw new InvalidOperationException("不支持GetValue操作！");
            return GetHandler.Invoke(obj);
        }

        // 快速委托没有什么性能优势，Emit已经足够快
        //public Object GetValue2(Object obj)
        //{
        //    var fg = FastGet;
        //    if (fg != null) return fg.Invoke(obj);

        //    if (GetHandler == null) throw new InvalidOperationException("不支持GetValue操作！");
        //    return GetHandler.Invoke(obj);
        //}

        /// <summary>赋值</summary>
        /// <param name="obj"></param>
        /// <param name="value">数值</param>
        [DebuggerHidden]
        [DebuggerStepThrough]
        public override void SetValue(Object obj, Object value)
        {
            if (SetHandler == null) throw new InvalidOperationException("不支持SetValue操作！");

            // 如果类型不匹配，先做类型转换
            if (value != null && !Type.IsAssignableFrom(value.GetType())) value = TypeX.ChangeType(value, Type);

            SetHandler.Invoke(obj, value);
        }

        /// <summary>快速获取静态属性。若属性不存在，会抛出异常。不确定属性是否存在时，建议使用Create方法</summary>
        /// <param name="type">类型</param>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        internal static Object GetValue(Type type, Object target, String name)
        {
            if (type == null && target != null) type = target.GetType();
            if (type == null) throw new ArgumentNullException("type");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            PropertyInfoX pix = Create(type, name);
            if (pix == null) throw new XException("类{0}中无法找到{1}属性！", type.Name, name);

            return pix.GetValue(target);
        }

        /// <summary>静态快速赋值。若属性不存在，会抛出异常。不确定属性是否存在时，建议使用Create方法</summary>
        /// <param name="type">类型</param>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        internal static void SetValue(Type type, Object target, String name, Object value)
        {
            if (type == null && target != null) type = target.GetType();
            if (type == null) throw new ArgumentNullException("type");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            PropertyInfoX pix = Create(type, name);
            if (pix == null) throw new XException("类{0}中无法找到{1}属性！", type.Name, name);

            pix.SetValue(target, value);
        }

        /// <summary>快速获取静态属性。若属性不存在，会抛出异常。不确定属性是否存在时，建议使用Create方法</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Object GetValue(Type type, String name) { return GetValue(type, null, name); }

        /// <summary>静态属性快速赋值。若属性不存在，会抛出异常。不确定属性是否存在时，建议使用Create方法</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        public static void SetValue(Type type, String name, Object value) { SetValue(type, null, name, value); }

        /// <summary>静态快速取值。若属性不存在，会抛出异常。不确定属性是否存在时，建议使用Create方法</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TResult GetValue<TResult>(Object target, String name)
        {
            //if (target == null || String.IsNullOrEmpty(name)) return default(TResult);
            if (target == null) throw new ArgumentNullException("target");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            return (TResult)GetValue(target.GetType(), target, name);
        }

        /// <summary>快速获取静态属性。若属性不存在，会抛出异常。不确定属性是否存在时，建议使用Create方法</summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TResult GetValue<TTarget, TResult>(String name) { return (TResult)GetValue(typeof(TTarget), null, name); }

        /// <summary>成员属性快速赋值。若属性不存在，会抛出异常。不确定属性是否存在时，建议使用Create方法</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        public static void SetValue(Object target, String name, Object value) { SetValue(target.GetType(), target, name, value); }

        /// <summary>快速设置静态属性。若属性不存在，会抛出异常。不确定属性是否存在时，建议使用Create方法</summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        public static void SetValue<TTarget>(String name, Object value) { SetValue(typeof(TTarget), null, name, value); }
        #endregion

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator PropertyInfo(PropertyInfoX obj)
        {
            return obj != null ? obj.Property : null;
        }

        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator PropertyInfoX(PropertyInfo obj)
        {
            return obj != null ? Create(obj) : null;
        }
        #endregion

        #region 快速委托调用
        // 快速委托没有什么性能优势，Emit已经足够快

        //private Boolean initFastGet;
        //private FastMethod _FastGet;
        ///// <summary>快速获取</summary>
        //FastMethod FastGet
        //{
        //    get
        //    {
        //        if (!initFastGet)
        //        {
        //            var fm = new FastMethod(GetMethod);
        //            if (fm.Supported) _FastGet = fm;

        //            initFastGet = true;
        //        }
        //        return _FastGet;
        //    }
        //}
        #endregion
    }
}