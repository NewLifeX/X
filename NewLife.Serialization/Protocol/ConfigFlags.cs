using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 序列化标识
    /// </summary>
    [Flags]
    public enum ConfigFlags
    {
        /// <summary>
        /// 没有头部
        /// </summary>
        /// <remarks>
        /// 没有头部的序列化只能采用默认配置
        /// </remarks>
        NoHead = 1,

        /// <summary>
        /// 序列化属性
        /// </summary>
        /// <remarks>
        /// 默认情况下只序列化字段，但是由特性控制的是否序列化优先级更高，该标识仅对没有任何特性的字段或属性有效
        /// </remarks>
        SerialProperty = 2,

        /// <summary>
        /// 压缩整数
        /// </summary>
        /// <remarks>
        /// 使用压缩编码的整数，更节省空间
        /// </remarks>
        EncodeInt = 4,

        /// <summary>
        /// 非空
        /// <remarks>
        /// 序列化引用对象时，默认会先写入一个字节表示该对象是否为空，对于该特性标识的非空对象，不再写入该标识字节
        /// </remarks>
        /// </summary>
        NotNull = 8,
    }
}
