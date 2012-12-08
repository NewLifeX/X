using System;

namespace NewLife.Net.Sip.Message
{
    /// <summary></summary>
    public class SipEvent : SipValueWithParams
    {
        #region 属性
        private String _EventType;
        /// <summary>属性说明</summary>
        public String EventType { get { return _EventType; } set { _EventType = value; } }
        #endregion

        #region 扩展属性
        private String _ID;
        /// <summary>id</summary>
        public String ID { get { return _ID; } set { _ID = value; } }
        #endregion
    }
}