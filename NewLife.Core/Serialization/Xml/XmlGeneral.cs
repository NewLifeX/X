using System;
using System.Globalization;
using System.Xml;
using NewLife.Reflection;

namespace NewLife.Serialization;

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
    public override Boolean Write(Object? value, Type type)
    {
        //if (value == null && type != typeof(String)) return false;

        var writer = Host.GetWriter();

        // 枚举 写入字符串
        if (type.IsEnum)
        {
            if (Host is Xml xml && xml.EnumString)
                writer.WriteValue(value + "");
            else
                writer.WriteValue(value.ToLong());

            return true;
        }

        switch (type.GetTypeCode())
        {
            case TypeCode.Boolean:
                writer.WriteValue((Boolean)(value ?? false));
                return true;
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Char:
                writer.WriteValue(Convert.ToChar(value));
                return true;
            case TypeCode.DBNull:
            case TypeCode.Empty:
                writer.WriteValue(0);
                return true;
            case TypeCode.DateTime:
                writer.WriteValue(((DateTime)(value ?? DateTime.MinValue)).ToFullString());
                return true;
            case TypeCode.Decimal:
                writer.WriteValue((Decimal)(value ?? 0m));
                return true;
            case TypeCode.Double:
                writer.WriteValue((Double)(value ?? 0d));
                return true;
            case TypeCode.Single:
                writer.WriteValue((Single)(value ?? 0f));
                return true;
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
                writer.WriteValue(Convert.ToInt32(value));
                return true;
            case TypeCode.Int64:
            case TypeCode.UInt64:
                writer.WriteValue(Convert.ToInt64(value));
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
            if (value is Guid guid) writer.WriteValue((guid).ToString());
            return true;
        }

        if (type == typeof(DateTimeOffset))
        {
            //writer.WriteValue((DateTimeOffset)value);
            if (value is DateTimeOffset dto) writer.WriteValue(dto + "");
            return true;
        }

        if (type == typeof(TimeSpan))
        {
            if (value is TimeSpan ts) writer.WriteValue(ts + "");
            return true;
        }

        if (type == typeof(Byte[]))
        {
            if (value is Byte[] buf) writer.WriteBase64(buf, 0, buf.Length);
            return true;
        }

        if (type == typeof(Char[]))
        {
            if (value is Char[] cs) writer.WriteValue(new String(cs));
            return true;
        }

        // 支持格式化的类型，有去有回
        if (type.As<IFormattable>())
        {
            if (value is IFormattable ft) writer.WriteValue(ft + "");
            return true;
        }

        return false;
    }

    /// <summary>尝试读取</summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override Boolean TryRead(Type type, ref Object? value)
    {
        if (type == null)
        {
            if (value == null) return false;
            type = value.GetType();
        }

        var reader = Host.GetReader();

        if (type == typeof(Guid))
        {
            value = new Guid(reader.ReadContentAsString());
            return true;
        }
        else if (type == typeof(Byte[]))
        {
            // 用字符串长度作为预设缓冲区的长度
            var buf = new Byte[reader.Value.Length];
            var count = reader.ReadContentAsBase64(buf, 0, buf.Length);
            value = buf.ReadBytes(0, count);
            return true;
        }
        else if (type == typeof(Char[]))
        {
            value = reader.ReadContentAsString().ToCharArray();
            return true;
        }
        else if (type == typeof(DateTimeOffset))
        {
            //value = reader.ReadContentAs(type, null);
            value = DateTimeOffset.Parse(reader.ReadContentAsString());
            return true;
        }
        else if (type == typeof(TimeSpan))
        {
            value = TimeSpan.Parse(reader.ReadContentAsString());
            return true;
        }

        type = Nullable.GetUnderlyingType(type) ?? type;
        if (!type.IsBaseType()) return false;

        // 读取异构Xml时可能报错
        var v = (reader.NodeType == XmlNodeType.Element ? reader.ReadElementContentAsString() : reader.ReadContentAsString()) + "";

        // 枚举
        if (type.IsEnum)
        {
            value = Enum.Parse(type, v);
            return true;
        }

        switch (type.GetTypeCode())
        {
            case TypeCode.Boolean:
                value = v.ToBoolean();
                return true;
            case TypeCode.Byte:
                value = Byte.Parse(v, NumberStyles.HexNumber);
                return true;
            case TypeCode.Char:
                if (v.Length > 0) value = v[0];
                return true;
            case TypeCode.DBNull:
                value = DBNull.Value;
                return true;
            case TypeCode.DateTime:
                value = v.ToDateTime();
                return true;
            case TypeCode.Decimal:
                value = (Decimal)v.ToDouble();
                return true;
            case TypeCode.Double:
                value = v.ToDouble();
                return true;
            case TypeCode.Empty:
                value = null;
                return true;
            case TypeCode.Int16:
                value = (Int16)v.ToInt();
                return true;
            case TypeCode.Int32:
                value = v.ToInt();
                return true;
            case TypeCode.Int64:
                value = Int64.Parse(v);
                return true;
            case TypeCode.Object:
                break;
            case TypeCode.SByte:
                value = SByte.Parse(v, NumberStyles.HexNumber);
                return true;
            case TypeCode.Single:
                value = (Single)v.ToDouble();
                return true;
            case TypeCode.String:
                value = v;
                return true;
            case TypeCode.UInt16:
                value = (UInt16)v.ToInt();
                return true;
            case TypeCode.UInt32:
                value = (UInt32)v.ToInt();
                return true;
            case TypeCode.UInt64:
                value = UInt64.Parse(v);
                return true;
            default:
                break;
        }

#if NET7_0_OR_GREATER
        if (type.GetInterfaces().Any(e => e.IsGenericType && e.GetGenericTypeDefinition() == typeof(IParsable<>)))
        {
            value = reader.ReadContentAsString().ChangeType(type);
            return true;
        }
#endif

        return false;
    }
}