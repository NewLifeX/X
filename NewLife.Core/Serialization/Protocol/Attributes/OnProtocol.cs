using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 创建实例时调用的方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnProtocolCreateInstanceAttribute : Attribute { }

    /// <summary>
    /// 反序列化后执行该方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnProtocolDeserializedAttribute : Attribute { }

    /// <summary>
    /// 反序列化前执行该方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnProtocolDeserializingAttribute : Attribute { }

    /// <summary>
    /// 序列化后执行该方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnProtocolSerializedAttribute : Attribute { }
    /// <summary>
    /// 序列化前执行该方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnProtocolSerializingAttribute : Attribute { }
}