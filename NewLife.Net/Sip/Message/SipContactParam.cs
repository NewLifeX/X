using System;

namespace NewLife.Net.Sip.Message
{
    /// <summary></summary>
    public class SipContactParam : SipValueWithParams
    {
        #region 属性
        private SipNameAddress _Address;
        /// <summary>地址</summary>
        public SipNameAddress Address { get { return _Address; } set { _Address = value; } }
        #endregion

        #region 扩展属性
        /// <summary>是否*联系人</summary>
        public Boolean IsStarContact { get { return Address != null && Address.Uri != null && Address.Uri.Host.StartsWith("*"); } }

        private Double _QValue;
        /// <summary>qvalue</summary>
        public Double QValue { get { return _QValue; } set { _QValue = value; } }

        private Int32 _Expires;
        /// <summary>expires</summary>
        public Int32 Expires { get { return _Expires; } set { _Expires = value; } }
        #endregion
    }
}