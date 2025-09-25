using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Data;

/// <summary>数据包编码器接口</summary>
/// <remarks>
/// 提供对象与数据包之间的双向转换能力，支持多种类型的数据序列化。
/// 设计目标：统一数据包处理，支持基础类型直接转换和复杂类型序列化。
/// </remarks>
public interface IPacketEncoder
{
    /// <summary>将对象编码为数据包</summary>
    /// <param name="value">要编码的对象，支持基础类型、数据包、字节数组和访问器</param>
    /// <returns>编码后的数据包，输入为null时返回null</returns>
    IPacket? Encode(Object? value);

    /// <summary>将数据包解码为指定类型的对象</summary>
    /// <param name="data">要解码的数据包</param>
    /// <param name="type">目标对象类型</param>
    /// <returns>解码后的对象，失败时根据配置返回null或抛出异常</returns>
    Object? Decode(IPacket data, Type type);
}

/// <summary>数据包编码器扩展方法</summary>
public static class PacketEncoderExtensions
{
    /// <summary>将数据包解码为指定类型的对象（泛型版本）</summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="encoder">编码器实例</param>
    /// <param name="data">要解码的数据包</param>
    /// <returns>解码后的强类型对象</returns>
    public static T? Decode<T>(this IPacketEncoder encoder, IPacket data) => (T?)encoder.Decode(data, typeof(T));
}

/// <summary>默认数据包编码器</summary>
/// <remarks>
/// <para>编码策略：</para>
/// <list type="bullet">
/// <item>基础类型：直接转换为字符串再转字节数组</item>
/// <item>字符串类型：直接转字节数组</item>
/// <item>DateTime：格式化为"yyyy-MM-dd HH:mm:ss.fff"</item>
/// <item>复杂类型：使用JSON序列化</item>
/// <item>数据包类型：直接返回或适配</item>
/// <item>字节数组：包装为ArrayPacket</item>
/// <item>访问器类型：调用ToPacket()方法</item>
/// </list>
/// </remarks>
public class DefaultPacketEncoder : IPacketEncoder
{
    #region 属性
    /// <summary>JSON序列化主机，用于处理复杂对象类型</summary>
    /// <remarks>可配置不同的JSON序列化器以满足特定需求</remarks>
    public IJsonHost JsonHost { get; set; } = JsonHelper.Default;

    /// <summary>解码出错时是否抛出异常</summary>
    /// <remarks>
    /// <para>默认false：出错时返回null，适用于容错性要求高的场景</para>
    /// <para>设为true：出错时抛出异常，便于调试和严格的数据校验</para>
    /// </remarks>
    public Boolean ThrowOnError { get; set; }
    #endregion

    #region 编码方法
    /// <summary>将对象编码为数据包</summary>
    /// <param name="value">要编码的对象</param>
    /// <returns>编码后的数据包</returns>
    public virtual IPacket? Encode(Object? value)
    {
        // 空值处理
        if (value == null) return null;

        // 已经是数据包类型，直接返回
        if (value is IPacket packet) return packet;

        // 字节数组，包装为数据包
        if (value is Byte[] buffer) return new ArrayPacket(buffer);

        // 访问器类型，调用专用转换方法
        if (value is IAccessor accessor) return accessor.ToPacket();

        // 其他类型先编码为字符串
        var stringValue = OnEncode(value);
        if (stringValue == null) return null;

        return new ArrayPacket(stringValue.GetBytes());
    }

    /// <summary>将对象编码为字符串</summary>
    /// <param name="value">要编码的对象，已确保非null</param>
    /// <returns>编码后的字符串</returns>
    /// <remarks>
    /// 子类可重写此方法以实现自定义的字符串编码逻辑
    /// </remarks>
    protected virtual String? OnEncode(Object value)
    {
        var type = value.GetType();
        var typeCode = type.GetTypeCode();

        return typeCode switch
        {
            // 复杂对象使用JSON序列化
            TypeCode.Object => JsonHost.Write(value),

            // 字符串直接返回
            TypeCode.String => value as String,

            // 时间类型使用标准格式
            TypeCode.DateTime => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff"),

            // 其他基础类型转字符串
            _ => value.ToString(),
        };
    }
    #endregion

    #region 解码方法
    /// <summary>将数据包解码为指定类型的对象</summary>
    /// <param name="data">要解码的数据包</param>
    /// <param name="type">目标类型</param>
    /// <returns>解码后的对象</returns>
    public virtual Object? Decode(IPacket data, Type type)
    {
        try
        {
            return DecodeInternal(data, type);
        }
        catch
        {
            // 根据配置决定是否抛出异常
            if (ThrowOnError) throw;
            return null;
        }
    }

    /// <summary>内部解码实现</summary>
    /// <param name="data">数据包</param>
    /// <param name="type">目标类型</param>
    /// <returns>解码后的对象</returns>
    protected virtual Object? DecodeInternal(IPacket data, Type type)
    {
        // 目标就是数据包类型
        if (type == typeof(IPacket)) return data;

        // 目标是具体的Packet类型
        if (type == typeof(Packet))
            return data is Packet existingPacket ? existingPacket : new Packet(data.ReadBytes());

        // 目标是字节数组
        if (type == typeof(Byte[])) return data.ReadBytes();

        // 目标是访问器类型
        if (type.As<IAccessor>()) return type.AccessorRead(data);

        // 空数据且目标是可空类型
        if (data.Total == 0 && type.IsNullable()) return null;

        // 转换为字符串进行进一步解析
        var stringValue = data.ToStr();
        return OnDecode(stringValue, type);
    }

    /// <summary>将字符串解码为指定类型的对象</summary>
    /// <param name="value">字符串值</param>
    /// <param name="type">目标类型</param>
    /// <returns>解码后的对象</returns>
    /// <remarks>
    /// 子类可重写此方法以实现自定义的字符串解码逻辑
    /// </remarks>
    protected virtual Object? OnDecode(String value, Type type)
    {
        var typeCode = type.GetTypeCode();

        // 字符串类型直接返回
        if (typeCode == TypeCode.String) return value;

        // 基础类型使用类型转换
        if (type.IsBaseType()) return value.ChangeType(type);

        // 复杂类型使用JSON反序列化
        return JsonHost.Read(value, type);
    }
    #endregion
}