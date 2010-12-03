using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Attributes
{
    /// <summary>
    /// 反序列化前执行该方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnProtocolDeserializingAttribute : Attribute
    {
    }
}
