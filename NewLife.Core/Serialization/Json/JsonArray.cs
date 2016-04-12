using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Reflection;
using System.Linq;

namespace NewLife.Serialization
{
    /// <summary>列表数据编码</summary>
    public class JsonArray : JsonHandlerBase
    {
        /// <summary>初始化</summary>
        public JsonArray()
        {
            // 优先级
            Priority = 20;
        }

        /// <summary>获取对象的Json字符串表示形式。</summary>
        /// <param name="value"></param>
        /// <returns>返回null表示不支持</returns>
        public override String GetString(Object value)
        {
            if (value == null) return String.Empty;

            var type = value.GetType();
            if (type == typeof(Guid)) return ((Guid)value).ToString();
            if (type == typeof(Byte[])) return Convert.ToBase64String((Byte[])value);
            if (type == typeof(Char[])) return new String((Char[])value);

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Boolean:
                    return value + "";
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Char:
                    return value + "";
                case TypeCode.DBNull:
                case TypeCode.Empty:
                    return String.Empty;
                case TypeCode.DateTime:
                    return value + "";
                case TypeCode.Decimal:
                    return value + "";
                case TypeCode.Single:
                case TypeCode.Double:
                    return value + "";
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return value + "";
                case TypeCode.String:
                    if (((String)value).IsNullOrEmpty()) return String.Empty;
                    return "\"{0}\"".F(value);
                case TypeCode.Object:
                default:
                    return null;
            }
        }

        /// <summary>写入</summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override bool Write(object value, Type type)
        {
            if (!typeof(IList).IsAssignableFrom(type)) return false;

            var list = value as IList;

            Host.Write("[");
            if (list != null && list.Count > 0)
            {
                // 循环写入数据
                foreach (var item in list)
                {
                    Host.Write(item);
                }
            }
            Host.Write("]");

            return true;
        }

        /// <summary>读取</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TryRead(Type type, ref object value)
        {
            if (!typeof(IList).IsAssignableFrom(type)) return false;

            // 先读取
            if (!Host.Read("[")) return false;

            // 子元素类型
            var elmType = type.GetElementTypeEx();

            var list = typeof(IList<>).MakeGenericType(elmType).CreateInstance() as IList;
            while (!Host.Read("]"))
            {
                Object obj = null;
                if (!Host.TryRead(elmType, ref obj)) return false;

                list.Add(obj);
            }

            // 数组的创建比较特别
            if (typeof(Array).IsAssignableFrom(type))
            {
                value = Array.CreateInstance(type.GetElementTypeEx(), list.Count);
                list.CopyTo((Array)value, 0);
            }
            else
                value = list;

            return true;
        }
    }
}