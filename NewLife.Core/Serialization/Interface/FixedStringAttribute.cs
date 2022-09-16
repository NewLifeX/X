using System;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>定长字符串序列化特性</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class FixedStringAttribute : AccessorAttribute
{
    /// <summary>长度</summary>
    public Int32 Length { get; set; }

    /// <summary>定长字符串序列化</summary>
    /// <param name="length"></param>
    public FixedStringAttribute(Int32 length) => Length = length;

    /// <summary>从数据流中读取消息</summary>
    /// <param name="formatter">序列化</param>
    /// <param name="context">上下文</param>
    /// <returns>是否成功</returns>
    public override Boolean Read(IFormatterX formatter, AccessorContext context)
    {
        if (formatter is Binary bn)
        {
            var str = bn.ReadFixedString(Length);
            context.Value.SetValue(context.Member, str);

            return true;
        }

        return false;
    }

    /// <summary>把消息写入到数据流中</summary>
    /// <param name="formatter">序列化</param>
    /// <param name="context">上下文</param>
    public override Boolean Write(IFormatterX formatter, AccessorContext context)
    {
        if (formatter is Binary bn)
        {
            var str = context.Value.GetValue(context.Member) as String;
            bn.WriteFixedString(str, Length);

            return true;
        }

        return false;
    }

}