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
        /// <param name="type">类型</param>
        /// <param name="ep"></param>
        public StunResult(StunNetType type, IPEndPoint ep) { Type = type; Public = ep; }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0} {1}", Type, Public);
        }
    }
}