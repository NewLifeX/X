using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using NewLife.Buffers;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>高性能Span二进制序列化器</summary>
/// <remarks>
/// 基于 <see cref="SpanReader"/>/<see cref="SpanWriter"/> 的极速二进制序列化框架，
/// 无需Stream、无处理器链、类型元数据一次缓存，适用于RPC通信和文件读写。
/// <list type="bullet">
/// <item>实现 <see cref="ISpanSerializable"/> 接口的类型获得零反射序列化性能</item>
/// <item>普通类型通过缓存反射自动序列化，支持基础类型、String、DateTime、Guid、Byte[]、枚举和嵌套类</item>
/// <item>不支持列表、字典等集合类型，如需请实现 <see cref="ISpanSerializable"/> 自行处理</item>
/// </list>
/// </remarks>
public static class SpanSerializer
{
    #region 类型缓存
    private static readonly ConcurrentDictionary<Type, PropertySchema[]> _cache = new();

    private static PropertySchema[] GetSchema(Type type) => _cache.GetOrAdd(type, BuildSchema);

    private static PropertySchema[] BuildSchema(Type type)
    {
        var props = type.GetProperties(true);
        var schemas = new PropertySchema[props.Count];
        for (var i = 0; i < props.Count; i++)
        {
            var p = props[i];
            var pType = p.PropertyType;

            // 解析Nullable<T>
            var underlyingType = Nullable.GetUnderlyingType(pType);
            var isNullable = underlyingType != null;
            var actualType = underlyingType ?? pType;

            // 解析Enum，取底层整数类型
            var isEnum = actualType.IsEnum;
            var enumType = isEnum ? actualType : null;
            if (isEnum) actualType = actualType.GetEnumUnderlyingType();

            // 编译属性访问委托，替代反射GetValue/SetValue
            var getter = BuildGetter(type, p);
            var setter = BuildSetter(type, p);

            schemas[i] = new PropertySchema(p, pType, actualType, Type.GetTypeCode(actualType), isNullable, isEnum, enumType, getter, setter);
        }
        return schemas;
    }

    /// <summary>编译属性取值委托：(Object target) => (Object)((T)target).Prop</summary>
    private static Func<Object, Object?> BuildGetter(Type type, PropertyInfo property)
    {
        var target = Expression.Parameter(typeof(Object), "target");
        Expression body = Expression.Property(Expression.Convert(target, type), property);
        if (property.PropertyType.IsValueType)
            body = Expression.Convert(body, typeof(Object));
        return Expression.Lambda<Func<Object, Object?>>(body, target).Compile();
    }

    /// <summary>编译属性赋值委托：(Object target, Object value) => ((T)target).Prop = (TProp)value</summary>
    private static Action<Object, Object?> BuildSetter(Type type, PropertyInfo property)
    {
        var target = Expression.Parameter(typeof(Object), "target");
        var value = Expression.Parameter(typeof(Object), "value");
        // 值类型用Unbox获取可写引用，引用类型用Convert
        var instance = type.IsValueType
            ? Expression.Unbox(target, type)
            : (Expression)Expression.Convert(target, type);
        var assign = Expression.Assign(
            Expression.Property(instance, property),
            Expression.Convert(value, property.PropertyType));
        return Expression.Lambda<Action<Object, Object?>>(assign, target, value).Compile();
    }

    /// <summary>属性元数据缓存结构</summary>
    internal readonly struct PropertySchema(PropertyInfo property, Type propertyType, Type actualType, TypeCode code, Boolean isNullable, Boolean isEnum, Type? enumType, Func<Object, Object?> getter, Action<Object, Object?> setter)
    {
        /// <summary>属性信息</summary>
        public readonly PropertyInfo Property = property;

        /// <summary>原始属性类型</summary>
        public readonly Type PropertyType = propertyType;

        /// <summary>实际值类型（解除Nullable和Enum后）</summary>
        public readonly Type ActualType = actualType;

        /// <summary>类型编码</summary>
        public readonly TypeCode Code = code;

        /// <summary>是否Nullable值类型</summary>
        public readonly Boolean IsNullable = isNullable;

        /// <summary>是否枚举</summary>
        public readonly Boolean IsEnum = isEnum;

        /// <summary>枚举类型（用于Enum.ToObject）</summary>
        public readonly Type? EnumType = enumType;

        /// <summary>编译属性取值委托，替代反射</summary>
        public readonly Func<Object, Object?> Getter = getter;

        /// <summary>编译属性赋值委托，替代反射</summary>
        public readonly Action<Object, Object?> Setter = setter;
    }
    #endregion

    private static readonly DateTime _dt1970 = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>协议头部预留空间。序列化时在缓冲区前方预留该字节数，方便各种协议向前扩展头部，默认32</summary>
    public static Int32 HeaderReserve { get; set; } = 32;

    #region 快捷方法
    /// <summary>序列化对象到池化数据包，全程零分配。调用方负责 Dispose 归还内存</summary>
    /// <remarks>
    /// 缓冲区前方预留 <see cref="HeaderReserve"/> 字节空间，方便协议层通过 <c>new OwnerPacket(owner, expandSize)</c> 向前扩展头部。
    /// 返回的数据包有效区域仅包含对象数据，不含预留空间。
    /// </remarks>
    /// <param name="value">目标对象</param>
    /// <param name="bufferSize">初始缓冲区大小，默认4096（含预留空间）</param>
    /// <returns>拥有管理权的数据包，使用完毕后需 Dispose</returns>
    public static IOwnerPacket Serialize(Object value, Int32 bufferSize = 4096)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        var reserve = HeaderReserve;
        var buf = ArrayPool<Byte>.Shared.Rent(bufferSize);
        var writer = new SpanWriter(buf, reserve);
        WriteObject(ref writer, value, value.GetType());

        return new OwnerPacket(buf, reserve, writer.WrittenCount, true);
    }

    /// <summary>序列化对象到指定Span，返回实际写入字节数</summary>
    /// <param name="value">目标对象</param>
    /// <param name="buffer">目标缓冲区</param>
    /// <returns>实际写入的字节数</returns>
    public static Int32 Serialize(Object value, Span<Byte> buffer)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        var writer = new SpanWriter(buffer);
        WriteObject(ref writer, value, value.GetType());
        return writer.WrittenCount;
    }

    /// <summary>反序列化字节数组为对象</summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="data">字节数据</param>
    /// <returns>反序列化的对象实例</returns>
    public static T Deserialize<T>(ReadOnlySpan<Byte> data)
    {
        var reader = new SpanReader(data);
        return (T)ReadObject(ref reader, typeof(T));
    }

    /// <summary>反序列化字节数组为对象</summary>
    /// <param name="type">目标类型</param>
    /// <param name="data">字节数据</param>
    /// <returns>反序列化的对象实例</returns>
    public static Object Deserialize(Type type, ReadOnlySpan<Byte> data)
    {
        var reader = new SpanReader(data);
        return ReadObject(ref reader, type);
    }
    #endregion

    #region 写入
    /// <summary>写入引用类型对象（含null标记）</summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="writer">Span写入器</param>
    /// <param name="value">目标对象，可为null</param>
    public static void Write<T>(ref SpanWriter writer, T? value) where T : class
    {
        if (value == null)
        {
            writer.Write((Byte)0);
            return;
        }
        writer.Write((Byte)1);
        WriteObject(ref writer, value, typeof(T));
    }

    /// <summary>写入对象成员到SpanWriter（不含null标记）</summary>
    /// <param name="writer">Span写入器</param>
    /// <param name="value">目标对象，不可为null</param>
    /// <param name="type">对象类型</param>
    public static void WriteObject(ref SpanWriter writer, Object value, Type type)
    {
        // ISpanSerializable 零反射快速路径
        if (value is ISpanSerializable ss)
        {
            ss.Write(ref writer);
            return;
        }

        var schemas = GetSchema(type);
        for (var i = 0; i < schemas.Length; i++)
        {
            var val = schemas[i].Getter(value);
            WriteValue(ref writer, val, in schemas[i]);
        }
    }

    private static void WriteValue(ref SpanWriter writer, Object? value, in PropertySchema schema)
    {
        // Nullable值类型：先写标记
        if (schema.IsNullable)
        {
            if (value == null)
            {
                writer.Write((Byte)0);
                return;
            }
            writer.Write((Byte)1);
        }

        switch (schema.Code)
        {
            case TypeCode.Boolean:
                writer.Write((Byte)(value is Boolean bv && bv ? 1 : 0));
                break;
            case TypeCode.SByte:
                writer.Write(unchecked((Byte)(value is SByte sbv ? sbv : Convert.ToSByte(value))));
                break;
            case TypeCode.Byte:
                writer.Write(value is Byte byv ? byv : Convert.ToByte(value));
                break;
            case TypeCode.Char:
                writer.Write(Convert.ToByte(value));
                break;
            case TypeCode.Int16:
                writer.Write(value is Int16 i16 ? i16 : Convert.ToInt16(value));
                break;
            case TypeCode.UInt16:
                writer.Write(value is UInt16 u16 ? u16 : Convert.ToUInt16(value));
                break;
            case TypeCode.Int32:
                writer.Write(value is Int32 i32 ? i32 : Convert.ToInt32(value));
                break;
            case TypeCode.UInt32:
                writer.Write(value is UInt32 u32 ? u32 : Convert.ToUInt32(value));
                break;
            case TypeCode.Int64:
                writer.Write(value is Int64 i64 ? i64 : Convert.ToInt64(value));
                break;
            case TypeCode.UInt64:
                writer.Write(value is UInt64 u64 ? u64 : Convert.ToUInt64(value));
                break;
            case TypeCode.Single:
                writer.Write(value is Single fv ? fv : Convert.ToSingle(value));
                break;
            case TypeCode.Double:
                writer.Write(value is Double dv ? dv : Convert.ToDouble(value));
                break;
            case TypeCode.Decimal:
                var dcBits = Decimal.GetBits(value is Decimal dcv ? dcv : Convert.ToDecimal(value));
                for (var j = 0; j < 4; j++)
                    writer.Write(dcBits[j]);
                break;
            case TypeCode.DateTime:
                var dt = value is DateTime dtv ? dtv : Convert.ToDateTime(value);
                writer.Write(dt > DateTime.MinValue ? (UInt32)(dt - _dt1970).TotalSeconds : (UInt32)0);
                break;
            case TypeCode.String:
                writer.Write(value as String, 0);
                break;
            case TypeCode.Object:
                WriteComplexValue(ref writer, value, schema.PropertyType);
                break;
        }
    }

    private static void WriteComplexValue(ref SpanWriter writer, Object? value, Type type)
    {
        // Guid - 16字节结构体
        if (type == typeof(Guid))
        {
            writer.Write(value is Guid gv ? gv : default);
            return;
        }

        // Byte[] - 7位长度前缀 + 数据
        if (type == typeof(Byte[]))
        {
            if (value is not Byte[] buf)
            {
                writer.WriteEncodedInt(0);
            }
            else
            {
                writer.WriteEncodedInt(buf.Length);
                if (buf.Length > 0) writer.Write(buf);
            }
            return;
        }

        // 引用类型（嵌套对象） - 与Binary兼容，不写null标记
        if (!type.IsValueType)
        {
            if (value != null)
                WriteObject(ref writer, value, type);
            return;
        }

        // 值类型结构体 - 直接写成员
        if (value != null)
            WriteObject(ref writer, value, type);
    }
    #endregion

    #region 读取
    /// <summary>读取引用类型对象（含null标记）</summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="reader">Span读取器</param>
    /// <returns>反序列化的对象，可能为null</returns>
    public static T? Read<T>(ref SpanReader reader) where T : class
    {
        var flag = reader.ReadByte();
        if (flag == 0) return null;

        return (T)ReadObject(ref reader, typeof(T));
    }

    /// <summary>从SpanReader读取对象成员（不含null标记）</summary>
    /// <param name="reader">Span读取器</param>
    /// <param name="type">对象类型</param>
    /// <returns>反序列化的对象实例</returns>
    public static Object ReadObject(ref SpanReader reader, Type type)
    {
        var obj = type.CreateInstance()!;

        // ISpanSerializable 零反射快速路径
        if (obj is ISpanSerializable ss)
        {
            if (reader.Available > 0)
                ss.Read(ref reader);
            return obj;
        }

        var schemas = GetSchema(type);
        for (var i = 0; i < schemas.Length; i++)
        {
            // 数据流不足时放弃读取，与Binary的EndOfStream行为一致
            if (reader.Available <= 0) break;

            var val = ReadValue(ref reader, in schemas[i]);
            schemas[i].Setter(obj, val);
        }

        return obj;
    }

    private static Object? ReadValue(ref SpanReader reader, in PropertySchema schema)
    {
        // Nullable值类型：先读标记
        if (schema.IsNullable)
        {
            if (reader.ReadByte() == 0) return null;
        }

        Object? result;
        switch (schema.Code)
        {
            case TypeCode.Boolean:
                result = reader.ReadByte() != 0;
                break;
            case TypeCode.SByte:
                result = unchecked((SByte)reader.ReadByte());
                break;
            case TypeCode.Byte:
                result = reader.ReadByte();
                break;
            case TypeCode.Char:
                result = Convert.ToChar(reader.ReadByte());
                break;
            case TypeCode.Int16:
                result = reader.ReadInt16();
                break;
            case TypeCode.UInt16:
                result = reader.ReadUInt16();
                break;
            case TypeCode.Int32:
                result = reader.ReadInt32();
                break;
            case TypeCode.UInt32:
                result = reader.ReadUInt32();
                break;
            case TypeCode.Int64:
                result = reader.ReadInt64();
                break;
            case TypeCode.UInt64:
                result = reader.ReadUInt64();
                break;
            case TypeCode.Single:
                result = reader.ReadSingle();
                break;
            case TypeCode.Double:
                result = reader.ReadDouble();
                break;
            case TypeCode.Decimal:
                var rdBits = new Int32[4];
                for (var j = 0; j < 4; j++)
                    rdBits[j] = reader.ReadInt32();
                result = new Decimal(rdBits);
                break;
            case TypeCode.DateTime:
                var sec = reader.ReadUInt32();
                result = sec == 0 ? DateTime.MinValue : _dt1970.AddSeconds(sec);
                break;
            case TypeCode.String:
                result = reader.ReadString();
                break;
            case TypeCode.Object:
                result = ReadComplexValue(ref reader, schema.PropertyType);
                break;
            default:
                result = null;
                break;
        }

        // 枚举类型还原
        if (schema.IsEnum && result != null)
            result = Enum.ToObject(schema.EnumType!, result);

        return result;
    }

    private static Object? ReadComplexValue(ref SpanReader reader, Type type)
    {
        // Guid - 16字节结构体
        if (type == typeof(Guid))
            return reader.Read<Guid>();

        // Byte[] - 7位长度前缀 + 数据
        if (type == typeof(Byte[]))
        {
            var len = reader.ReadEncodedInt();
            if (len <= 0) return len == 0 ? new Byte[0] : null;
            return reader.ReadBytes(len).ToArray();
        }

        // 引用类型（嵌套对象） - 与Binary兼容，不读null标记
        if (!type.IsValueType)
            return ReadObject(ref reader, type);

        // 值类型结构体
        return ReadObject(ref reader, type);
    }
    #endregion
}
