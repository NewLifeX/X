namespace NewLife.Reflection;

/// <summary>类型编码助手。提供 TypeCode 与 Type 之间的 AOT 安全映射</summary>
internal static class TypeCodeHelper
{
    /// <summary>根据类型编码获取运行时类型。为 Type.GetType("System." + code) 的 AOT 安全等价实现</summary>
    /// <param name="code">类型编码</param>
    /// <returns>对应的运行时类型；无法映射时返回 null</returns>
    public static Type? GetType(TypeCode code) => code switch
    {
        TypeCode.Boolean => typeof(Boolean),
        TypeCode.Char => typeof(Char),
        TypeCode.SByte => typeof(SByte),
        TypeCode.Byte => typeof(Byte),
        TypeCode.Int16 => typeof(Int16),
        TypeCode.UInt16 => typeof(UInt16),
        TypeCode.Int32 => typeof(Int32),
        TypeCode.UInt32 => typeof(UInt32),
        TypeCode.Int64 => typeof(Int64),
        TypeCode.UInt64 => typeof(UInt64),
        TypeCode.Single => typeof(Single),
        TypeCode.Double => typeof(Double),
        TypeCode.Decimal => typeof(Decimal),
        TypeCode.DateTime => typeof(DateTime),
        TypeCode.String => typeof(String),
        TypeCode.Object => typeof(Object),
        TypeCode.DBNull => typeof(DBNull),
        _ => null,
    };
}
