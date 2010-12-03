using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 协议元素特性
    /// </summary>
    /// <remarks>
    /// 设置了该特性的字段或属性一定会被序列化
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ProtocolElementAttribute : ProtocolAttribute
    {
    }
}
