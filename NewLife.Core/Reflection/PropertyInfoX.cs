using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NewLife.Collections;

namespace NewLife.Reflection
{
    /// <summary>
    /// 快速属性访问
    /// </summary>
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
        /// <summary>
        /// 快速调用委托，延迟到首次使用才创建
        /// </summary>
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
        /// <summary>
        /// 快速调用委托，延迟到首次使用才创建
        /// </summary>
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
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static PropertyInfoX Create(PropertyInfo property)
        {
            ////PropertyInfoX obj = new PropertyInfoX(property);
            ////FastGetValueHandler h1 = obj.GetValue;
            ////FastSetValueHandler h2 = obj.SetValue;

            //Type t1 = typeof(FastSetValueHandler);
            //Type t2 = typeof(FastSetValueHandler);
            //TypeX tt1 = TypeX.Create(t1);
            //TypeX tt2 = TypeX.Create(t2);



            if (property == null) return null;

            return cache.GetItem(property, delegate(PropertyInfo key)
            {
                return new PropertyInfoX(key);
            });
            //if (cache.ContainsKey(property)) return cache[property];
            //lock (cache)
            //{
            //    if (cache.ContainsKey(property)) return cache[property];

            //    PropertyInfoX entity = new PropertyInfoX(property);

            //    //entity.Property = property;
            //    entity.gethandler = GetValueInvoker(property);
            //    entity.sethandler = SetValueInvoker(property);

            //    cache.Add(property, entity);

            //    return entity;
            //}
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static PropertyInfoX Create(Type type, String name)
        {
            PropertyInfo property = type.GetProperty(name);
            if (property == null) property = type.GetProperty(name, DefaultBinding);
            if (property == null) property = type.GetProperty(name, DefaultBinding | BindingFlags.IgnoreCase);
            if (property == null)
            {
                PropertyInfo[] ps = type.GetProperties();
                foreach (PropertyInfo item in ps)
                {
                    if (String.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        property = item;
                        break;
                    }
                }
            }
            if (property == null && type.BaseType != typeof(Object)) return Create(type.BaseType, name);
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
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object) }, method.DeclaringType.Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();

            EmitHelper help = new EmitHelper(il);
            //if (!method.IsStatic) il.Emit(OpCodes.Ldarg_0);
            if (!method.IsStatic) help.Ldarg(0).CastFromObject(method.DeclaringType);
            // 目标方法没有参数
            help.Call(method)
                .BoxIfValueType(method.ReturnType)
                .Ret();

            return (FastGetValueHandler)dynamicMethod.CreateDelegate(typeof(FastGetValueHandler));
        }

        private static FastSetValueHandler SetValueInvoker(MethodInfo method)
        {
            //定义一个没有名字的动态方法
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, null, new Type[] { typeof(Object), typeof(Object) }, method.DeclaringType.Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();

            EmitHelper help = new EmitHelper(il);
            //if (!method.IsStatic) il.Emit(OpCodes.Ldarg_0);
            if (!method.IsStatic) help.Ldarg(0).CastFromObject(method.DeclaringType);
            // 目标方法只有一个参数
            help.Ldarg(1)
                .CastFromObject(method.GetParameters()[0].ParameterType)
                .Call(method)
                .Ret();

            return (FastSetValueHandler)dynamicMethod.CreateDelegate(typeof(FastSetValueHandler));
        }
        #endregion

        #region 调用
        /// <summary>
        /// 取值
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Object GetValue(Object obj)
        {
            if (GetHandler == null) throw new InvalidOperationException("不支持GetValue操作！");
            return GetHandler.Invoke(obj);
        }

        /// <summary>
        /// 赋值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public override void SetValue(Object obj, Object value)
        {
            if (SetHandler == null) throw new InvalidOperationException("不支持SetValue操作！");
            SetHandler.Invoke(obj, value);
        }

        /// <summary>
        /// 静态快速取值
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TResult GetValue<TResult>(Object target, String name)
        {
            if (target == null || String.IsNullOrEmpty(name)) return default(TResult);

            PropertyInfoX pix = Create(target.GetType(), name);
            if (pix == null) return default(TResult);

            return (TResult)pix.GetValue(target);
        }

        /// <summary>
        /// 静态快速赋值
        /// </summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void SetValue(Object target, String name, Object value)
        {
            if (target == null || String.IsNullOrEmpty(name)) return;

            PropertyInfoX pix = Create(target.GetType(), name);
            if (pix == null) return;

            pix.SetValue(target, value);
        }
        #endregion

        #region 类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator PropertyInfo(PropertyInfoX obj)
        {
            return obj != null ? obj.Property : null;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator PropertyInfoX(PropertyInfo obj)
        {
            return obj != null ? Create(obj) : null;
        }
        #endregion
    }
}