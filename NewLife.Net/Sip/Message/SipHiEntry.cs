using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Sip.Message
{
    /// <summary></summary>
    public class SipHiEntry : SipValueWithParams
    {
        #region 属性
        private SipNameAddress _Address;
        /// <summary>地址</summary>
        public SipNameAddress Address { get { return _Address; } set { _Address = value; } }
        #endregion

        #region 扩展属性
        private Double _Index;
        /// <summary>index</summary>
        public Double Index { get { return _Index; } set { _Index = value; } }
        #endregion
    }
}