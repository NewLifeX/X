using System;
using System.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>通用NAT代理</summary>
    public class XProxy : ProxyBase
    {
        #region 属性
        private NATFilter _nat;

        /// <summary>服务器地址</summary>
        public String ServerAddress { get { return _nat.Address; } set { _nat.Address = value; } }

        /// <summary>服务器端口</summary>
        public Int32 ServerPort { get { return _nat.Port; } set { _nat.Port = value; } }

        /// <summary>服务器协议</summary>
        public ProtocolType ServerProtocolType { get { return _nat.ProtocolType; } set { _nat.ProtocolType = value; } }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        public XProxy()
        {
            _nat = new NATFilter();
            _nat.Proxy = this;
            Filters.Add(_nat);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 开始
        /// </summary>
        protected override void OnStart()
        {
            if (ServerProtocolType == 0) ServerProtocolType = ProtocolType;

            base.OnStart();
        }
        #endregion
    }
}