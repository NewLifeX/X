using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>序列化种类</summary>
    public enum RWKinds
    {
        /// <summary>二进制</summary>
        Binary,

        /// <summary>Xml</summary>
        Xml,

        /// <summary>Json</summary>
        Json,

        /// <summary>名值</summary>
        NameValue
    }
}