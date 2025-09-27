using System.ComponentModel;
using System.Reflection;

namespace NewLife;

/// <summary>枚举类型助手类</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class EnumHelper
{
    /// <summary>枚举变量是否包含指定标识</summary>
    /// <param name="value">枚举变量</param>
    /// <param name="flag">要判断的标识</param>
    /// <returns>如果枚举变量包含指定标识则返回 true，否则返回 false</returns>
    /// <exception cref="ArgumentException">当两个枚举类型不匹配时</exception>
    public static Boolean Has(this Enum value, Enum flag)
    {
        if (value.GetType() != flag.GetType()) throw new ArgumentException("Enumeration identification judgment must be of the same type", nameof(flag));

        var num = Convert.ToUInt64(flag);
        // 特殊处理：当flag为0时，只有当value也为0时才返回true
        if (num == 0) return Convert.ToUInt64(value) == 0;

        return (Convert.ToUInt64(value) & num) == num;
    }

    /// <summary>设置枚举标识位</summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="source">源枚举值</param>
    /// <param name="flag">要设置的标识</param>
    /// <param name="value">是否设置该标识</param>
    /// <returns>设置后的枚举值</returns>
    /// <exception cref="ArgumentException">当枚举类型不匹配时</exception>
    public static T Set<T>(this Enum source, T flag, Boolean value)
    {
        if (source is not T) throw new ArgumentException("Enumeration identification judgment must be of the same type", nameof(source));

        var s = Convert.ToUInt64(source);
        var f = Convert.ToUInt64(flag);

        if (value)
            s |= f;
        else
            s &= ~f;

        return (T)Enum.ToObject(typeof(T), s);
    }

    /// <summary>获取枚举字段的描述</summary>
    /// <param name="value">枚举值</param>
    /// <returns>如果存在 DescriptionAttribute 则返回其描述，否则返回 null</returns>
    public static String? GetDescription(this Enum value)
    {
        if (value == null) return null;

        var type = value.GetType();
        var field = type.GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);

        // 云飞扬 2017-07-06 传入的枚举值可能并不存在，需要判断是否为 null
        if (field == null) return null;

        var description = field.GetCustomAttribute<DescriptionAttribute>(false);
        return description?.Description;
    }

    /// <summary>获取枚举类型的所有字段描述</summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>包含枚举值与其描述的字典</returns>
    public static Dictionary<TEnum, String> GetDescriptions<TEnum>() where TEnum : notnull
    {
        var result = new Dictionary<TEnum, String>();
        var descriptions = GetDescriptions(typeof(TEnum));

        foreach (var kvp in descriptions)
        {
            result.Add((TEnum)Enum.ToObject(typeof(TEnum), kvp.Key), kvp.Value);
        }

        return result;
    }

    /// <summary>获取枚举类型的所有字段描述</summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>包含枚举值（Int32）与其描述的字典</returns>
    public static Dictionary<Int32, String> GetDescriptions(Type enumType)
    {
        var result = new Dictionary<Int32, String>();

        // 检查是否为枚举类型
        if (!enumType.IsEnum) return result;

        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            if (!field.IsStatic) continue;

            var enumValue = Convert.ToInt32(field.GetValue(null));

            // 优先使用 DisplayNameAttribute
            var displayName = field.GetCustomAttribute<DisplayNameAttribute>(false);
            var description = displayName?.DisplayName;

            // 其次使用 DescriptionAttribute
            var descriptionAttr = field.GetCustomAttribute<DescriptionAttribute>(false);
            if (description.IsNullOrEmpty()) description = descriptionAttr?.Description;

            // 有些枚举可能不同名称有相同的值，使用索引器避免重复键异常
            if (description.IsNullOrEmpty()) description = field.Name;
            result[enumValue] = description;
        }

        return result;
    }
}