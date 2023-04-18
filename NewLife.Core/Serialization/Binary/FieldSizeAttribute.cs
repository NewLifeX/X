using System.Collections;
using System.Reflection;
using System.Text;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>字段大小特性。</summary>
/// <remarks>
/// 可以通过Size指定字符串或数组的固有大小，为0表示自动计算；
/// 也可以通过指定参考字段ReferenceName，然后从其中获取大小。
/// 支持_Header._Questions形式的多层次引用字段。
/// 
/// 支持针对单个成员使用多个FieldSize特性，各自指定不同Version版本，以支持不同版本协议的序列化。
/// 例如JT/T808协议，2011/2019版的相同字段使用不同长度。
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true)]
public class FieldSizeAttribute : Attribute
{
    /// <summary>大小。使用<see cref="ReferenceName"/>时，作为偏移量；0表示自动计算大小</summary>
    public Int32 Size { get; set; }

    /// <summary>大小宽度。特定个数的字节表示长度，自动计算时（Size=0）使用，可选0/1/2/4</summary>
    public Int32 SizeWidth { get; set; } = -1;

    /// <summary>参考大小字段名，其中存储了实际大小，使用时获取</summary>
    public String ReferenceName { get; set; }

    /// <summary>协议版本。用于支持多版本协议序列化。例如JT/T808的2011/2019</summary>
    public String Version { get; set; }

    /// <summary>通过Size指定字符串或数组的固有大小，为0表示自动计算</summary>
    /// <param name="size"></param>
    public FieldSizeAttribute(Int32 size) => Size = size;

    /// <summary>指定参考字段ReferenceName，然后从其中获取大小</summary>
    /// <param name="referenceName"></param>
    public FieldSizeAttribute(String referenceName) => ReferenceName = referenceName;

    /// <summary>指定参考字段ReferenceName，然后从其中获取大小</summary>
    /// <param name="referenceName"></param>
    /// <param name="size">在参考字段值基础上的增量，可以是正数负数</param>
    public FieldSizeAttribute(String referenceName, Int32 size) { ReferenceName = referenceName; Size = size; }

    /// <summary>指定大小，指定协议版本，用于支持多版本协议序列化</summary>
    /// <param name="size"></param>
    /// <param name="version"></param>
    public FieldSizeAttribute(Int32 size, String version)
    {
        Size = size;
        Version = version;
    }

    #region 方法
    /// <summary>找到所引用的参考字段</summary>
    /// <param name="target">目标对象</param>
    /// <param name="member">目标对象的成员</param>
    /// <param name="value">数值</param>
    /// <returns></returns>
    private MemberInfo FindReference(Object target, MemberInfo member, out Object value)
    {
        value = null;

        if (member == null) return null;
        if (String.IsNullOrEmpty(ReferenceName)) return null;

        // 考虑ReferenceName可能是圆点分隔的多重结构
        MemberInfo mi = null;
        var type = member.DeclaringType;
        value = target;
        var ss = ReferenceName.Split('.');
        for (var i = 0; i < ss.Length; i++)
        {
            var pi = type.GetPropertyEx(ss[i]);
            if (pi != null)
            {
                mi = pi;
                type = pi.PropertyType;
            }
            else
            {
                var fi = type.GetFieldEx(ss[i]);
                if (fi != null)
                {
                    mi = fi;
                    type = fi.FieldType;
                }
            }

            // 最后一个不需要计算
            if (i < ss.Length - 1)
            {
                if (mi != null) value = value.GetValue(mi);
            }
        }

        // 目标字段必须是整型
        var tc = Type.GetTypeCode(type);
        if (tc is >= TypeCode.SByte and <= TypeCode.UInt64) return mi;

        return null;
    }

    /// <summary>设置目标对象的引用大小值</summary>
    /// <param name="target">目标对象</param>
    /// <param name="member"></param>
    /// <param name="encoding"></param>
    internal void SetReferenceSize(Object target, MemberInfo member, Encoding encoding)
    {
        var mi = FindReference(target, member, out var v);
        if (mi == null) return;

        // 获取当前成员（加了特性）的值
        var value = target.GetValue(member);
        if (value == null) return;

        // 尝试计算大小
        var size = 0;
        if (value is String)
        {
            encoding ??= Encoding.UTF8;

            size = encoding.GetByteCount("" + value);
        }
        else if (value.GetType().IsArray)
        {
            size = (value as Array).Length;
        }
        else if (value is IEnumerable)
        {
            foreach (var item in value as IEnumerable)
            {
                size++;
            }
        }

        // 给参考字段赋值
        v.SetValue(mi, size - Size);
    }

    /// <summary>获取目标对象的引用大小值</summary>
    /// <param name="target">目标对象</param>
    /// <param name="member"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    internal Boolean TryGetReferenceSize(Object target, MemberInfo member, out Int32 size)
    {
        size = -1;

        var mi = FindReference(target, member, out var v);
        if (mi == null) return false;

        size = Convert.ToInt32(v.GetValue(mi)) + Size;

        return true;
    }
    #endregion
}