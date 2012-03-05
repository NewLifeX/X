using System;
using System.Xml.Serialization;

namespace NewLife.Messaging
{
    /// <summary>异常消息</summary>
    public class ExceptionMessage : EntityMessage
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Exception; } }

        /// <summary>异常对象</summary>
        public new Exception Value { get { return base.Value as Exception; } set { base.Value = value; } }

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var err = Value;
            if (err != null)
                return String.Format("{0} {1}", base.ToString(), err.Message);
            else
                return base.ToString();
        }
        #endregion
    }
}