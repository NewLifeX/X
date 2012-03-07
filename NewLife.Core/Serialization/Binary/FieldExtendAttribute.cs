using System;
using System.Collections;
using System.Reflection;
using System.Text;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>字段扩展特性。</summary>
    /// <remarks>
    /// 该特性只用于整型表示长度的字段。
    /// 读取器遇到该特性后，读取指定长度的数据流，然后切换为新数据流，完成后续字段的读取，完成后恢复备份；
    /// 写入器遇到该特性后，切换为新数据流，使用新数据流写入后续字段，完成后写入数据流长度，再写新数据流数据。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class FieldExtendAttribute : Attribute { }
}