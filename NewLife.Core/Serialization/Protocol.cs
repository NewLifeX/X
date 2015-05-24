using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Messaging;

namespace NewLife.Serialization
{
    /// <summary>协议基类</summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Protocol<T> : MessageBase where T : Protocol<T>, new()
    {
        #region 读写方法
        #endregion
    }
}