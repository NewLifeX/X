using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>完全字符串序列化特性。指示数据流剩下部分全部作为字符串读写</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class FullStringAttribute : AccessorAttribute
{
    /// <summary>从数据流中读取消息</summary>
    /// <param name="formatter">序列化</param>
    /// <param name="context">上下文</param>
    /// <returns>是否成功</returns>
    public override Boolean Read(IFormatterX formatter, AccessorContext context)
    {
        if (formatter is Binary bn)
        {
            var buf = bn.Stream.ReadBytes(-1);
            var str = bn.Encoding.GetString(buf);
            if (bn.TrimZero && str != null) str = str.Trim('\0');

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
            if (!str.IsNullOrEmpty())
            {
                var buf = bn.Encoding.GetBytes(str);
                bn.Write(buf, 0, buf.Length);
            }

            return true;
        }

        return false;
    }

}