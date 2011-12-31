using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Sip.Message
{
    /// <summary>ac-value</summary>
    /// <remarks>
    /// <code>
    /// RFC 3841 Syntax:
    ///     ac-value       = "*" *(SEMI ac-params)
    ///     ac-params      = feature-param / req-param / explicit-param / generic-param
    ///                      ;;feature param from RFC 3840
    ///                      ;;generic-param from RFC 3261
    ///     req-param      = "require"
    ///     explicit-param = "explicit"
    /// </code>
    /// </remarks>
    public class SipACValue : SipValueWithParams
    {
        #region 属性
        private Boolean _Require;
        /// <summary>属性说明</summary>
        public Boolean Require { get { return _Require; } set { _Require = value; } }

        private Boolean _Explicit;
        /// <summary>属性说明</summary>
        public Boolean Explicit { get { return _Explicit; } set { _Explicit = value; } }
        #endregion
    }
}