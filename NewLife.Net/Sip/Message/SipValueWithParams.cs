using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Sip.Message
{
    /// <summary>带参数Sip实体基类</summary>
    public abstract class SipValueWithParams : SipValue
    {
        #region 属性
        private Dictionary<String, String> _Parameters;
        /// <summary>参数集合</summary>
        public IDictionary<String, String> Parameters { get { return _Parameters ?? (_Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)); } }
        #endregion
    }
}