using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NewLife.Collections;

namespace NewLife.Reflection
{
    /// <summary>
    /// 快速字段访问
    /// </summary>
    public class FieldInfoX : MemberInfoX
    {
        #region 属性
        private FieldInfo _Field;
        /// <summary>目标字段</summary>
        public FieldInfo Field
        {
            get { return _Field; }
            set { _Field = value; }
        }

        FastGetValueHandler _GetHandler;
        /// <summary>
        /// 快速调用委托，延迟到首次使用才创建
        /// </summary>
        FastGetValueHandler GetHandler
        {
            get
            {
                if (_GetHandler == null) _GetHandler = GetValueInvoker(Field);

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
                if (_SetHandler == null) _SetHandler = SetValueInvoker(Field);

                return _SetHandler;
            }
        }
        #endregion

        #region 构造
        private FieldInfoX(FieldInfo field) : base(field) { Field = field; }

        private static DictionaryCache<FieldInfo, FieldInfoX> cache = new DictionaryCache<FieldInfo, FieldInfoX>();
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static FieldInfoX Create(FieldInfo field)
        {
            if (field == null) return null;

            return cache.GetItem(field, delegate(FieldInfo key)
            {
                return new FieldInfoX(key);
            });
            //if (cache.ContainsKey(field)) return cache[field];
            //lock (cache)
            //{
            //    if (cache.ContainsKey(field)) return cache[field];

            //    FieldInfoX entity = new FieldInfoX(field);

            //    //entity.Field = field;
            //    entity.gethandler = GetValueInvoker(field);
            //    entity.sethandler = SetValueInvoker(field);

            //    cache.Add(field, entity);

            //    return entity;
            //}
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FieldInfoX Create(Type type, String name)
        {
            FieldInfo field = type.GetField(name);
            if (field == null) field = type.GetField(name, DefaultBinding);
            if (field == null) field = type.GetField(name, DefaultBinding | BindingFlags.IgnoreCase);
            if (field == null) return null;

            return Create(field);
        }
        #endregion

        #region 创建动态方法
        private static FastGetValueHandler GetValueInvoker(FieldInfo field)
        {
            //定义一个没有名字的动态方法
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object) }, field.DeclaringType.Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();
            EmitHelper help = new EmitHelper(il);

            // 必须考虑对象是值类型的情况，需要拆箱
            // 其它地方看到的程序从来都没有人处理
            help.Ldarg(0)
                .CastFromObject(field.DeclaringType)
                .Ldfld(field)
                .BoxIfValueType(field.FieldType)
                .Ret();

            //il.Emit(OpCodes.Ldarg_0);
            ////il.Emit(OpCodes.Isinst, field.DeclaringType);
            ////il.Emit(OpCodes.Castclass, field.DeclaringType);

            //il.Emit(OpCodes.Ldfld, field);
            //if (field.FieldType.IsValueType) il.Emit(OpCodes.Box, field.FieldType);
            //il.Emit(OpCodes.Ret);
            //if (field.Name == "key")
            //{
            //    SaveIL(dynamicMethod, delegate(ILGenerator il2)
            //    {
            //        il2.Emit(OpCodes.Ldarg_0);
            //        il2.Emit(OpCodes.Ldfld, field);
            //        if (field.FieldType.IsValueType) il2.Emit(OpCodes.Box, field.FieldType);
            //        il2.Emit(OpCodes.Ret);
            //    });
            //}
            return (FastGetValueHandler)dynamicMethod.CreateDelegate(typeof(FastGetValueHandler));
        }

        private static DynamicMethod GetValueInvoker2(FieldInfo field)
        {
            //定义一个没有名字的动态方法
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, field.FieldType, new Type[] { typeof(Object) }, field.DeclaringType.Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();
            EmitHelper help = new EmitHelper(il);

            // 必须考虑对象是值类型的情况，需要拆箱
            // 其它地方看到的程序从来都没有人处理
            help.Ldarg(0)
                .CastFromObject(field.DeclaringType)
                .Ldfld(field)
                //.BoxIfValueType(field.FieldType)
                .Ret();

            return dynamicMethod;
        }

        private static FastSetValueHandler SetValueInvoker(FieldInfo field)
        {
            //定义一个没有名字的动态方法
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, null, new Type[] { typeof(Object), typeof(Object) }, field.DeclaringType.Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();
            EmitHelper help = new EmitHelper(il);

            // 必须考虑对象是值类型的情况，需要拆箱
            // 其它地方看到的程序从来都没有人处理
            // 值类型是不支持这样子赋值的，暂时没有找到更好的替代方法
            help.Ldarg(0)
                .CastFromObject(field.DeclaringType)
                .Ldarg(1);

            MethodInfo method = GetMethod(field.FieldType);
            if (method != null)
                help.Call(method);
            else
                help.CastFromObject(field.FieldType);

            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);

            return (FastSetValueHandler)dynamicMethod.CreateDelegate(typeof(FastSetValueHandler));
        }

        static MethodInfo GetMethod(Type type)
        {
            String name = "To" + type.Name;
            return typeof(Convert).GetMethod(name, new Type[] { typeof(Object) });
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
            return GetHandler.Invoke(obj);
        }

        DynamicMethod _GetMethod;
        /// <summary>
        /// 快速调用委托，延迟到首次使用才创建
        /// </summary>
        DynamicMethod GetMethod2
        {
            get
            {
                if (_GetMethod == null) _GetMethod = GetValueInvoker2(Field);

                return _GetMethod;
            }
        }

        /// <summary>
        /// 取值
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Object GetValue2(Object obj)
        {
            return GetMethod2.Invoke(null, new Object[] { obj });
        }

        /// <summary>
        /// 赋值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public override void SetValue(Object obj, Object value)
        {
            // SetHandler不支持值类型
            if (Field.DeclaringType.IsValueType)
            {
                // 不相等才赋值
                Object v = GetValue(obj);
                if (!Object.Equals(value, v)) Field.SetValue(obj, value);
            }
            else
                SetHandler.Invoke(obj, value);
        }

        /// <summary>
        /// 静态快速取值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetValue<T>(Object target, String name)
        {
            if (target == null || String.IsNullOrEmpty(name)) return default(T);

            FieldInfoX fix = Create(target.GetType(), name);
            if (fix == null) return default(T);

            return (T)fix.GetValue(target);
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

            FieldInfoX fix = Create(target.GetType(), name);
            if (fix == null) return;

            fix.SetValue(target, value);
        }

        delegate Object FastGetValueHandler(Object obj);
        delegate void FastSetValueHandler(Object obj, Object value);
        #endregion

        #region 类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator FieldInfo(FieldInfoX obj)
        {
            return obj != null ? obj.Field : null;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator FieldInfoX(FieldInfo obj)
        {
            return obj != null ? Create(obj) : null;
        }
        #endregion
    }
}