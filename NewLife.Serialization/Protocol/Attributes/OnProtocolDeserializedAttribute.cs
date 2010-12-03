using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Attributes
{
    /// <summary>
    /// 反序列化后执行该方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnProtocolDeserializedAttribute : Attribute
    {
    }
}
