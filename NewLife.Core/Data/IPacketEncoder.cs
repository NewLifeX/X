using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Data;

/// <summary>数据包编码器接口</summary>
public interface IPacketEncoder
{
    /// <summary>数值转数据包</summary>
    /// <param name="value">数值对象</param>
    /// <returns></returns>
    IPacket? Encode(Object value);

    /// <summary>数据包转对象</summary>
    /// <param name="data">数据包</param>
    /// <param name="type">目标类型</param>
    /// <returns></returns>
    Object? Decode(IPacket data, Type type);
}

/// <summary>编码器扩展</summary>
public static class PackerEncoderExtensions
{
    /// <summary>数据包转对象</summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="encoder"></param>
    /// <param name="data">数据包</param>
    /// <returns></returns>
    public static T? Decode<T>(this IPacketEncoder encoder, IPacket data) => (T?)encoder.Decode(data, typeof(T));
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
    public virtual IPacket? Encode(Object value)
    {
        if (value == null) return null!;

        if (value is IPacket pk) return pk;
        if (value is Byte[] buf) return (ArrayPacket)buf;
        if (value is IAccessor acc) return acc.ToPacket();

        var str = OnEncode(value);

        return (ArrayPacket)str.GetBytes();
    }

    /// <summary>编码为字符串。复杂类型采用Json序列化</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    protected virtual String? OnEncode(Object value)
    {
        var type = value.GetType();
        return type.GetTypeCode() switch
        {
            TypeCode.Object => JsonHost.Write(value),
            TypeCode.String => value as String,
            TypeCode.DateTime => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff"),
            _ => value + "",
        };
    }

    /// <summary>数据包转对象</summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public virtual Object? Decode(IPacket data, Type type)
    {
        try
        {
            if (type == typeof(IPacket)) return data;
            if (type == typeof(Packet)) return data is Packet pk ? pk : data.ReadBytes();
            if (type == typeof(Byte[])) return data.ReadBytes();
            if (type.As<IAccessor>()) return type.AccessorRead(data);

            // 可空类型
            if (data.Length == 0 && type.IsNullable()) return null;

            var str = data.ToStr();
            return OnDecode(str, type);
        }
        catch
        {
            if (ThrowOnError) throw;

            return null;
        }
    }

    /// <summary>字符串解码为对象。复杂类型采用Json反序列化</summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    protected virtual Object? OnDecode(String value, Type type)
    {
        if (type.GetTypeCode() == TypeCode.String) return value;
        if (type.IsBaseType()) return value.ChangeType(type);

        return JsonHost.Read(value, type);
    }
}