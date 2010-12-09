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
                if (_GetHandler == null && GetMethod != null) _GetHandler = CreateDelegate<FastGetValueHandler>(GetMethod);

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
                if (_SetHandler == null && SetMethod != null) _SetHandler = CreateDelegate<FastSetValueHandler>(SetMethod);
                //if (_SetHandler == null && SetMethod != null) _SetHandler = SetValueInvoker(Property);

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
            if (property == null) property = type.GetProperty(name, BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null) property = type.GetProperty(name, BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
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
            if (property == null) return null;

            return Create(property);
        }
        #endregion

        #region 创建动态方法
        //private static FastGetValueHandler GetValueInvoker(PropertyInfo property)
        //{
        //    MethodInfo method = property.GetGetMethod();
        //    if (method == null) method = property.GetGetMethod(true);
        //    if (method == null) return null;

        //    //定义一个没有名字的动态方法
        //    DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object) }, property.DeclaringType.Module, true);
        //    ILGenerator il = dynamicMethod.GetILGenerator();

        //    GetMethodInvoker(il, method);

        //    FastGetValueHandler invoder = (FastGetValueHandler)dynamicMethod.CreateDelegate(typeof(FastGetValueHandler));
        //    return invoder;
        //}

        //private static FastSetValueHandler SetValueInvoker(PropertyInfo property)
        //{
        //    MethodInfo method = property.GetSetMethod();
        //    if (method == null) method = property.GetSetMethod(true);
        //    if (method == null) return null;

        //    //定义一个没有名字的动态方法
        //    DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, null, new Type[] { typeof(Object), typeof(Object[]) }, property.DeclaringType.Module, true);
        //    ILGenerator il = dynamicMethod.GetILGenerator();

        //    GetMethodInvoker(il, method);

        //    FastSetValueHandler invoder = (FastSetValueHandler)dynamicMethod.CreateDelegate(typeof(FastSetValueHandler));
        //    return invoder;
        //}

        //internal static void GetMethodInvoker(ILGenerator il, MethodInfo method)
        //{
        //    EmitHelper help = new EmitHelper(il);

        //    if (!method.IsStatic) il.Emit(OpCodes.Ldarg_0);

        //    help.PushParams(method)
        //        .Call(method)
        //        .Ret(method);

        //    ////准备参数
        //    //ParameterInfo[] ps = method.GetParameters();
        //    //for (int i = 0; i < ps.Length; i++)
        //    //{
        //    //    il.Emit(OpCodes.Ldarg_1);
        //    //    //EmitFastInt(il, i);
        //    //    help.Ldc_I4(i);
        //    //    il.Emit(OpCodes.Ldelem_Ref);
        //    //    if (ps[i].ParameterType.IsValueType)
        //    //        il.Emit(OpCodes.Unbox_Any, ps[i].ParameterType);
        //    //    else
        //    //        il.Emit(OpCodes.Castclass, ps[i].ParameterType);
        //    //}

        //    ////调用目标方法
        //    //if (method.IsVirtual)
        //    //    il.EmitCall(OpCodes.Callvirt, method, null);
        //    //else
        //    //    il.EmitCall(OpCodes.Call, method, null);

        //    ////处理返回值
        //    //if (method.ReturnType != typeof(void) && method.ReturnType.IsValueType) il.Emit(OpCodes.Box, method.ReturnType);

        //    //il.Emit(OpCodes.Ret);
        //}
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
            SetHandler.Invoke(obj, new Object[] { value });
        }

        delegate Object FastGetValueHandler(Object obj);
        delegate void FastSetValueHandler(Object obj, Object[] parameters);
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