using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using NewLife.Collections;
using NewLife.Exceptions;

namespace NewLife.Reflection
{
    /// <summary>快速字段访问</summary>
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
        /// <summary>快速调用委托，延迟到首次使用才创建</summary>
        FastGetValueHandler GetHandler
        {
            get
            {
                if (_GetHandler == null) _GetHandler = GetValueInvoker(Field);

                return _GetHandler;
            }
        }

        FastSetValueHandler _SetHandler;
        /// <summary>快速调用委托，延迟到首次使用才创建</summary>
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
        /// <summary>创建</summary>
        /// <param name="field">字段</param>
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

        /// <summary>创建</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public new static FieldInfoX Create(Type type, String name)
        {
            FieldInfo field = type.GetField(name);
            if (field == null) field = type.GetField(name, DefaultBinding);
            if (field == null) field = type.GetField(name, DefaultBinding | BindingFlags.IgnoreCase);
            if (field == null && type.BaseType != typeof(Object)) return Create(type.BaseType, name);
            if (field == null) return null;

            return Create(field);
        }
        #endregion

        #region 创建动态方法
        private static FastGetValueHandler GetValueInvoker(FieldInfo field)
        {
            //定义一个没有名字的动态方法
            var dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object) }, field.DeclaringType.Module, true);
            var il = dynamicMethod.GetILGenerator();

            // 必须考虑对象是值类型的情况，需要拆箱
            // 其它地方看到的程序从来都没有人处理
            il.Ldarg(0)
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

        //private static DynamicMethod GetValueInvoker2(FieldInfo field)
        //{
        //    //定义一个没有名字的动态方法
        //    DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, field.FieldType, new Type[] { typeof(Object) }, field.DeclaringType.Module, true);
        //    ILGenerator il = dynamicMethod.GetILGenerator();
        //    EmitHelper help = new EmitHelper(il);

        //    // 必须考虑对象是值类型的情况，需要拆箱
        //    // 其它地方看到的程序从来都没有人处理
        //    help.Ldarg(0)
        //        .CastFromObject(field.DeclaringType)
        //        .Ldfld(field)
        //        //.BoxIfValueType(field.FieldType)
        //        .Ret();

        //    return dynamicMethod;
        //}

        private static FastSetValueHandler SetValueInvoker(FieldInfo field)
        {
            //定义一个没有名字的动态方法
            var dynamicMethod = new DynamicMethod(String.Empty, null, new Type[] { typeof(Object), typeof(Object) }, field.DeclaringType.Module, true);
            var il = dynamicMethod.GetILGenerator();

            // 必须考虑对象是值类型的情况，需要拆箱
            // 其它地方看到的程序从来都没有人处理
            // 值类型是不支持这样子赋值的，暂时没有找到更好的替代方法
            il.Ldarg(0)
                .CastFromObject(field.DeclaringType)
                .Ldarg(1);

            var method = GetMethod(field.FieldType);
            if (method != null)
                il.Call(method);
            else
                il.CastFromObject(field.FieldType);

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
        /// <summary>取值</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public override Object GetValue(Object obj)
        {
            // 在编译时写入并且不能更改的字段，不能快速反射，主要因为取不到FieldHandle。枚举中的静态字段。
            if (Field.IsLiteral) return Field.GetValue(obj);

            return GetHandler.Invoke(obj);
        }

        /// <summary>赋值</summary>
        /// <param name="obj"></param>
        /// <param name="value">数值</param>
        [DebuggerStepThrough]
        public override void SetValue(Object obj, Object value)
        {
            // 如果类型不匹配，先做类型转换
            if (value != null && !Type.IsAssignableFrom(value.GetType())) value = TypeX.ChangeType(value, Type);

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

        internal static Object GetValue(Type type, Object target, String name)
        {
            if (type == null && target != null) type = target.GetType();
            if (type == null) throw new ArgumentNullException("type");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            FieldInfoX fix = Create(type, name);
            if (fix == null) throw new XException("类{0}中无法找到{1}字段！", type.Name, name);

            return fix.GetValue(target);
        }

        internal static void SetValue(Type type, Object target, String name, Object value)
        {
            if (type == null && target != null) type = target.GetType();
            if (type == null) throw new ArgumentNullException("type");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            FieldInfoX fix = Create(type, name);
            if (fix == null) throw new XException("类{0}中无法找到{1}字段！", type.Name, name);

            fix.SetValue(target, value);
        }

        /// <summary>静态快速取值。若字段不存在，会抛出异常。不确定字段是否存在时，建议使用Create方法</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TResult GetValue<TResult>(Object target, String name) { return (TResult)GetValue(target.GetType(), target, name); }

        /// <summary>快速获取静态字段。若字段不存在，会抛出异常。不确定字段是否存在时，建议使用Create方法</summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TResult GetValue<TTarget, TResult>(String name) { return (TResult)GetValue(typeof(TTarget), null, name); }

        /// <summary>静态快速赋值。若字段不存在，会抛出异常。不确定字段是否存在时，建议使用Create方法</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        public static void SetValue(Object target, String name, Object value) { SetValue(target.GetType(), target, name, value); }

        /// <summary>快速设置静态字段。若字段不存在，会抛出异常。不确定字段是否存在时，建议使用Create方法</summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        public static void SetValue<TTarget>(String name, Object value) { SetValue(typeof(TTarget), null, name, value); }

        delegate Object FastGetValueHandler(Object obj);
        delegate void FastSetValueHandler(Object obj, Object value);
        #endregion

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator FieldInfo(FieldInfoX obj)
        {
            return obj != null ? obj.Field : null;
        }

        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator FieldInfoX(FieldInfo obj)
        {
            return obj != null ? Create(obj) : null;
        }
        #endregion
    }
}