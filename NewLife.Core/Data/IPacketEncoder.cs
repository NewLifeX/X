using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Data;

/// <summary>数据包编码器接口</summary>
public interface IPacketEncoder
{
    /// <summary>数值转数据包</summary>
    /// <param name="value">数值对象</param>
    /// <returns></returns>
    Packet Encode(Object value);

    /// <summary>数据包转对象</summary>
    /// <param name="data">数据包</param>
    /// <param name="type">目标类型</param>
    /// <returns></returns>
    Object? Decode(Packet data, Type type);
}

/// <summary>编码器扩展</summary>
public static class PackerEncoderExtensions
{
    /// <summary>数据包转对象</summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="encoder"></param>
    /// <param name="data">数据包</param>
    /// <returns></returns>
    public static T? Decode<T>(this IPacketEncoder encoder, Packet data) => (T?)encoder.Decode(data, typeof(T));
}

/// <summary>默认数据包编码器。基础类型直接转，复杂类型Json序列化</summary>
public class DefaultPacketEncoder : IPacketEncoder
{
    #region 属性
    /// <summary>Json序列化主机</summary>
    public IJsonHost JsonHost { get; set; } = JsonHelper.Default;

    /// <summary>解码出错时抛出异常。默认false不抛出异常，仅返回默认值</summary>
    public Boolean ThrowOnError { get; set; }
    #endregion

    /// <summary>数值转数据包</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual Packet Encode(Object value)
    {
        if (value == null) return new Byte[0];

        if (value is Packet pk) return pk;
        if (value is Byte[] buf) return buf;
        if (value is IAccessor acc) return acc.ToPacket();

        var type = value.GetType();
        return (type.GetTypeCode()) switch
        {
            TypeCode.Object => JsonHost.Write(value).GetBytes(),
            TypeCode.String => (value as String).GetBytes(),
            TypeCode.DateTime => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss").GetBytes(),
            _ => (value + "").GetBytes(),
        };
    }

    /// <summary>数据包转对象</summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public virtual Object? Decode(Packet data, Type type)
    {
        try
        {
            if (type == typeof(Packet)) return data;
            if (type == typeof(Byte[])) return data.ReadBytes();
            if (type.As<IAccessor>()) return type.AccessorRead(data);

            // 可空类型
            if (data.Total == 0 && type.IsNullable()) return null;

            var str = data.ToStr();
            if (type.GetTypeCode() == TypeCode.String) return str;
            if (type.IsBaseType()) return str.ChangeType(type);

            return JsonHost.Read(str, type);
        }
        catch
        {
            if (ThrowOnError) throw;

            return null;
        }
    }
}