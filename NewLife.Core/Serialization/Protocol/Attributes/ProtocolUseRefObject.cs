using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 是否使用对象引用
    /// </summary>
    public class ProtocolUseRefObjectAttribute : ProtocolAttribute
    {
        private Boolean _UseRefObject;
        /// <summary>是否使用对象引用</summary>
        public Boolean UseRefObject
        {
            get { return _UseRefObject; }
            set { _UseRefObject = value; }
        }
   
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="useRefObject"></param>
        public ProtocolUseRefObjectAttribute(Boolean useRefObject)
        {
            UseRefObject = useRefObject;
        }

        /// <summary>
        /// 构造
        /// </summary>
        public ProtocolUseRefObjectAttribute() : this(true) { }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="config"></param>
        public override void MergeTo(FormatterConfig config)
        {
            config.UseRefObject = UseRefObject;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public override bool Equals(FormatterConfig config)
        {
            return config.UseRefObject == UseRefObject;
        } }
}
