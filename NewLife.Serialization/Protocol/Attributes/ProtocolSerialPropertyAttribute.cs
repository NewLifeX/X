using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 是否把属性作为主要序列化对象
    /// </summary>
    public class ProtocolSerialPropertyAttribute : ProtocolAttribute
    {
        private Boolean _SerialProperty;
        /// <summary>
        /// 序列化属性
        /// </summary>
        /// <remarks>
        /// 默认情况下只序列化字段，但是由特性控制的是否序列化优先级更高，该标识仅对没有任何特性的字段或属性有效
        /// </remarks>
        public Boolean SerialProperty
        {
            get { return _SerialProperty; }
            set { _SerialProperty = value; }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="serialProperty"></param>
        public ProtocolSerialPropertyAttribute(Boolean serialProperty)
        {
            SerialProperty = serialProperty;
        }

        /// <summary>
        /// 构造
        /// </summary>
        public ProtocolSerialPropertyAttribute() : this(true) { }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="config"></param>
        public override void MergeTo(FormatterConfig config)
        {
            config.SerialProperty = SerialProperty;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public override bool Equals(FormatterConfig config)
        {
            return config.SerialProperty == SerialProperty;
        }
    }
}