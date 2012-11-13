using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using NewLife.Linq;
using NewLife.Reflection;
using XCode.Configuration;
using NewLife.Log;

namespace XCode.Common
{
    /// <summary>助手类</summary>
    static class Helper
    {
        public static Boolean IsIntType(this Type type)
        {
            var code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    break;
            }

            return false;
        }

        /// <summary>指定键是否为空。一般业务系统设计不允许主键为空，包括自增的0和字符串的空</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Boolean IsNullKey(Object key)
        {
            if (key == null) return true;

            var type = key.GetType();

            //由于key的实际类型是由类型推倒而来，所以必须根据实际传入的参数类型分别进行装箱操作
            //如果不根据类型分别进行会导致类型转换失败抛出异常
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16: return ((Int16)key) <= 0;
                case TypeCode.Int32: return ((Int32)key) <= 0;
                case TypeCode.Int64: return ((Int64)key) <= 0;
                case TypeCode.UInt16: return ((UInt16)key) <= 0;
                case TypeCode.UInt32: return ((UInt32)key) <= 0;
                case TypeCode.UInt64: return ((UInt64)key) <= 0;
                case TypeCode.String: return String.IsNullOrEmpty((String)key);
                default: break;
            }

            if (type == typeof(Guid)) return ((Guid)key) == Guid.Empty;
            if (type == typeof(Byte[])) return ((Byte[])key).Length <= 0;

            return false;
        }

        /// <summary>是否空主键的实体</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Boolean IsEntityNullKey(IEntity entity)
        {
            IEntityOperate eop = EntityFactory.CreateOperate(entity.GetType());
            foreach (var item in eop.Fields)
            {
                if ((item.PrimaryKey || item.IsIdentity) && IsNullKey(entity[item.Name])) return true;
            }

            //List<FieldItem> pks = eop.Fields.Where(e => e.PrimaryKey).ToList();
            //if (pks != null && pks.Count > 0)
            //{
            //    foreach (FieldItem item in pks)
            //    {
            //        // 任何一个不为空，则表明整体不为空
            //        if (!IsNullKey(entity[item.Name])) return false;
            //    }
            //    return true;
            //}

            //FieldItem field = eop.Fields.FirstOrDefault(e => e.IsIdentity);
            //if (field.IsIdentity)
            //{
            //    return IsNullKey(entity[field.Name]);
            //}

            return false;
        }

        /// <summary>判断两个对象是否相当，特别处理整型</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Boolean EqualTo(this Object left, Object right)
        {
            // 空判断
            if (left == null) return right == null;
            if (right == null) return false;

            // 如果已经相等，不用做别的处理了
            if (Object.Equals(left, right)) return true;

            // 特殊处理整型
            return left.GetType().IsIntType() && right.GetType().IsIntType() && (Int64)left == (Int64)right;
        }
    }
}