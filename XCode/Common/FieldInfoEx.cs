using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace XCode.Common
{
    /// <summary>
    /// 快速字段访问
    /// </summary>
    class FieldInfoEx
    {
        #region 属性
        private FieldInfo _Field;
        /// <summary>目标字段</summary>
        public FieldInfo Field
        {
            get { return _Field; }
            set { _Field = value; }
        }

        FastGetValueHandler gethandler;
        FastSetValueHandler sethandler;
        #endregion

        #region 构造
        private static Dictionary<FieldInfo, FieldInfoEx> cache = new Dictionary<FieldInfo, FieldInfoEx>();
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static FieldInfoEx Create(FieldInfo field)
        {
            if (field == null) return null;

            if (cache.ContainsKey(field)) return cache[field];
            lock (cache)
            {
                if (cache.ContainsKey(field)) return cache[field];

                FieldInfoEx entity = new FieldInfoEx();

                entity.Field = field;
                entity.gethandler = GetValueInvoker(field);
                entity.sethandler = SetValueInvoker(field);

                cache.Add(field, entity);

                return entity;
            }
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FieldInfoEx Create(Type type, String name)
        {
            FieldInfo field = type.GetField(name);
            if (field == null) field = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) field = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
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

            il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Isinst, field.DeclaringType);
            il.Emit(OpCodes.Ldfld, field);
            if (field.FieldType.IsValueType) il.Emit(OpCodes.Box, field.FieldType);
            il.Emit(OpCodes.Ret);

            FastGetValueHandler invoder = (FastGetValueHandler)dynamicMethod.CreateDelegate(typeof(FastGetValueHandler));
            return invoder;
        }

        private static FastSetValueHandler SetValueInvoker(FieldInfo field)
        {
            //定义一个没有名字的动态方法
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, null, new Type[] { typeof(Object), typeof(Object) }, field.DeclaringType.Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Isinst, field.DeclaringType);
            il.Emit(OpCodes.Ldarg_1);

            MethodInfo method = GetMethod(field.FieldType);
            if (method != null)
            {
                // 使用Convert.ToInt32(value)
                il.EmitCall(OpCodes.Call, method, null);
            }
            else
            {
                if (field.FieldType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, field.FieldType);
                else
                    il.Emit(OpCodes.Castclass, field.FieldType);
            }

            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);

            FastSetValueHandler invoder = (FastSetValueHandler)dynamicMethod.CreateDelegate(typeof(FastSetValueHandler));
            return invoder;
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
        public Object GetValue(Object obj)
        {
            //return Field.GetValue(obj);

            return gethandler.Invoke(obj);
        }

        /// <summary>
        /// 赋值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public void SetValue(Object obj, Object value)
        {
            //Field.SetValue(obj, value);

            sethandler.Invoke(obj, value);
        }

        delegate Object FastGetValueHandler(Object obj);
        delegate void FastSetValueHandler(Object obj, Object value);
        #endregion
    }
}