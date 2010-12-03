using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 协议数组元素
    /// </summary>
    /// <remarks>
    /// 该特性仅对数据及实现了IEnumerable接口的字段或属性有效
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ProtocolArrayAttribute : ProtocolAttribute
    {
        private Int32 _Size;
        /// <summary>大小</summary>
        /// <remarks>默认为0时，首先序列化一个压缩整数作为数组元素个数，再序列化每一个项；设置了大小后，不再压缩元素个数，而是以特性指定的大小为准</remarks>
        public Int32 Size
        {
            get { return _Size; }
            set { _Size = value; }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="size"></param>
        public ProtocolArrayAttribute(Int32 size)
        {
            Size = size;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="config"></param>
        public override void MergeTo(FormatterConfig config)
        {
            config.Size = Size;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public override bool Equals(FormatterConfig config)
        {
            return config.Size == Size;
        }
    }
}