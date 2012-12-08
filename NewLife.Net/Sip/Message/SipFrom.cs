using System;

namespace NewLife.Net.Sip.Message
{
    /// <summary></summary>
    public class SipFrom : SipValueWithParams
    {
        #region 属性
        private SipNameAddress _Address;
        /// <summary>地址</summary>
        public SipNameAddress Address { get { return _Address; } set { _Address = value; } }
        #endregion

        #region 扩展属性
        private String _Tag;
        /// <summary>tag</summary>
        public String Tag { get { return _Tag; } set { _Tag = value; } }
        #endregion
    }
}