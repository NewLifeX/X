using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 自定义序列化接口
    /// </summary>
    /// <remarks>
    /// 该接口支持多层实现，向下延伸，下层优先。
    /// 某个类实现该接口后，该类的所有属性，以及所有子孙类属性都将有效
    /// </remarks>
    public interface IProtocolSerializable
    {
        /// <summary>
        /// 序列化前触发
        /// </summary>
        /// <param name="context"></param>
        /// <returns>是否允许序列化当前字段或属性</returns>
        Boolean OnSerializing(WriteContext context);

        /// <summary>
        /// 序列化后触发
        /// </summary>
        /// <param name="context"></param>
        void OnSerialized(WriteContext context);

        /// <summary>
        /// 反序列化前触发
        /// </summary>
        /// <param name="context"></param>
        /// <returns>是否允许反序列化当前字段或属性</returns>
        Boolean OnDeserializing(ReadContext context);

        /// <summary>
        /// 反序列化后触发
        /// </summary>
        /// <param name="context"></param>
        void OnDeserialized(ReadContext context);

        /// <summary>
        /// 为指定类型创建实例时触发
        /// </summary>
        /// <param name="context"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Object OnCreateInstance(ReadContext context, Type type);
    }
}
