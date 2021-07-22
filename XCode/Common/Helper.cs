using System;
using System.Collections.Generic;
using System.Data;
using NewLife.Reflection;

namespace XCode.Common
{
    /// <summary>助手类</summary>
    static class Helper
    {
        /// <summary>指定键是否为空。一般业务系统设计不允许主键为空，包括自增的0和字符串的空</summary>
        /// <param name="key">键值</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static Boolean IsNullKey(Object key, Type type)
        {
            if (key == null) return true;

            if (type == null) type = key.GetType();

            key = key.ChangeType(type);

            //由于key的实际类型是由类型推倒而来，所以必须根据实际传入的参数类型分别进行装箱操作
            //如果不根据类型分别进行会导致类型转换失败抛出异常
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16: return ((Int16)key) == 0;
                case TypeCode.Int32: return ((Int32)key) == 0;
                case TypeCode.Int64: return ((Int64)key) == 0;
                case TypeCode.UInt16: return ((UInt16)key) == 0;
                case TypeCode.UInt32: return ((UInt32)key) == 0;
                case TypeCode.UInt64: return ((UInt64)key) == 0;
                case TypeCode.String: return String.IsNullOrEmpty((String)key);
                default: break;
            }

            if (type == typeof(Guid)) return ((Guid)key) == Guid.Empty;
            if (type == typeof(Byte[])) return ((Byte[])key).Length == 0;

            return false;
        }

        /// <summary>是否空主键的实体</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Boolean IsEntityNullKey(IEntity entity)
        {
            var eop = entity.GetType().AsFactory();
            foreach (var item in eop.Fields)
            {
                if ((item.PrimaryKey || item.IsIdentity) && IsNullKey(entity[item.Name], item.Type)) return true;
            }

            return false;
        }

        public static DataRow[] ToArray(this DataRowCollection collection)
        {
            if (collection == null) return new DataRow[0];

            var list = new List<DataRow>();
            foreach (var item in collection)
            {
                if (item is DataRow dr) list.Add(dr);
            }

            return list.ToArray();
        }
    }
}