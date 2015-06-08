using System;

namespace NewLife.Serialization
{
    /// <summary>Xml基础类型处理器</summary>
    public class XmlGeneral : XmlHandlerBase
    {
        /// <summary>实例化</summary>
        public XmlGeneral()
        {
            Priority = 10;
        }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns>是否处理成功</returns>
        public override Boolean Write(Object value, Type type)
        {
            if (value == null && type != typeof(String)) return false;

            var writer = Host.GetWriter();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    writer.WriteValue((Boolean)value);
                    return true;
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Char:
                    writer.WriteValue((Char)value);
                    return true;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                    writer.WriteValue((Byte)0);
                    return true;
                case TypeCode.DateTime:
                    writer.WriteValue((DateTime)value);
                    return true;
                case TypeCode.Decimal:
                    writer.WriteValue((Decimal)value);
                    return true;
                case TypeCode.Double:
                    writer.WriteValue((Double)value);
                    return true;
                case TypeCode.Single:
                    writer.WriteValue((Single)value);
                    return true;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    writer.WriteValue((Int32)value);
                    return true;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    writer.WriteValue((Int64)value);
                    return true;
                case TypeCode.String:
                    writer.WriteValue(value + "");
                    return true;
                case TypeCode.Object:
                    break;
                default:
                    break;
            }

            if (type == typeof(Guid))
            {
                writer.WriteValue(((Guid)value).ToString());
                return true;
            }

            if (type == typeof(DateTimeOffset))
            {
                writer.WriteValue((DateTimeOffset)value);
                return true;
            }

            if (type == typeof(TimeSpan))
            {
                writer.WriteValue((TimeSpan)value);
                return true;
            }

            if (type == typeof(Byte[]))
            {
                var buf = value as Byte[];
                writer.WriteBase64(buf, 0, buf.Length);
                return true;
            }

            if (type == typeof(Char[]))
            {
                writer.WriteValue(new String((Char[])value));
                return true;
            }

            return false;
        }

        /// <summary>尝试读取</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TryRead(Type type, ref object value)
        {
            throw new NotImplementedException();
        }
    }
}