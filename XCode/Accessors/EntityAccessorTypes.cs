using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.Accessors
{
    /// <summary>实体访问器种类</summary>
    public enum EntityAccessorTypes
    {
        /// <summary>Http，只读不写。</summary>
        Http,

        /// <summary>WebForm</summary>
        WebForm,

        /// <summary>WinForm</summary>
        WinForm,

        /// <summary>二进制</summary>
        Binary,

        /// <summary>Xml</summary>
        Xml,

        /// <summary>Json</summary>
        Json
    }
}