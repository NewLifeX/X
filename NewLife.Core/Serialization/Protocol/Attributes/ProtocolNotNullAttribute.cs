using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 是否不为空
    /// </summary>
    public class ProtocolNotNullAttribute : ProtocolAttribute
    {
        private Boolean _NotNull;
        /// <summary>是否不为空</summary>
        /// <remarks>序列化引用对象时，默认会先写入一个字节表示该对象是否为空，对于该特性标识的非空对象，不再写入该标识字节</remarks>
        public Boolean NotNull
        {
            get { return _NotNull; }
            set { _NotNull = value; }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="notNull"></param>
        public ProtocolNotNullAttribute(Boolean notNull)
        {
            NotNull = notNull;
        }

        /// <summary>
        /// 构造
        /// </summary>
        public ProtocolNotNullAttribute() : this(true) { }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="config"></param>
        public override void MergeTo(FormatterConfig config)
        {
            //config.SetFlag(ConfigFlags.NotNull, NotNull);
            config.NotNull = NotNull;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public override bool Equals(FormatterConfig config)
        {
            return config.NotNull == NotNull;
        }
    }
}
