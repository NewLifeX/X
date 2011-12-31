using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace NewLife.Net.Stun
{
    /// <summary>Stun结果</summary>
    public class StunResult
    {
        private StunNetType _Type;
        /// <summary>类型</summary>
        public StunNetType Type { get { return _Type; } set { _Type = value; } }

        private IPEndPoint _Public;
        /// <summary>公共地址</summary>
        public IPEndPoint Public { get { return _Public; } set { _Public = value; } }

        /// <summary>实例化Stun结果</summary>
        /// <param name="type"></param>
        /// <param name="ep"></param>
        public StunResult(StunNetType type, IPEndPoint ep) { Type = type; Public = ep; }
    }
}