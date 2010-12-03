using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 是否使用压缩编码的整数进行序列号
    /// </summary>
    public class ProtocolEncodeIntAttribute : ProtocolAttribute
    {
        private Boolean _EncodeInt;
        /// <summary>是否使用压缩编码整数</summary>
        public Boolean EncodeInt
        {
            get { return _EncodeInt; }
            set { _EncodeInt = value; }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="encodeInt"></param>
        public ProtocolEncodeIntAttribute(Boolean encodeInt)
        {
            EncodeInt = encodeInt;
        }

        /// <summary>
        /// 构造
        /// </summary>
        public ProtocolEncodeIntAttribute() : this(true) { }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="config"></param>
        public override void MergeTo(FormatterConfig config)
        {
            //config.SetFlag(ConfigFlags.EncodeInt, EncodeInt);
            config.EncodeInt = EncodeInt;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public override bool Equals(FormatterConfig config)
        {
            return config.EncodeInt == EncodeInt;
        }
    }
}
