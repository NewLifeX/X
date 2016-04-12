using System;
using NewLife.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace NewLife.Serialization
{
    /// <summary>Json基础类型处理器</summary>
    public class JsonGeneral : JsonHandlerBase
    {
        /// <summary>实例化</summary>
        public JsonGeneral()
        {
            Priority = 10;
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

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type == null)
            {
                if (value == null) return false;
                type = value.GetType();
            }

            //if (type == typeof(Guid))
            //{
            //    value = new Guid(ReadBytes(16));
            //    return true;
            //}
            //else if (type == typeof(Byte[]))
            //{
            //    value = ReadBytes(-1);
            //    return true;
            //}
            //else if (type == typeof(Char[]))
            //{
            //    value = ReadChars(-1);
            //    return true;
            //}

            return false;
        }
    }
}