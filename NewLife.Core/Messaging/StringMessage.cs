using System;
using System.Xml.Serialization;

namespace NewLife.Messaging
{
    /// <summary>字符串消息。封装一个字符串，UTF8编码。</summary>
    public class StringMessage : Message
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.String; } }

        //private Int32 _Length;
        ///// <summary>长度</summary>
        //public Int32 Length { get { return _Length; } set { _Length = value; } }

        private String _Value;
        /// <summary>字符串</summary>
        public String Value { get { return _Value; } set { _Value = value; } }

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1}", base.ToString(), Value);
        }
        #endregion
    }
}