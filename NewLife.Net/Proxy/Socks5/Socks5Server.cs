
namespace NewLife.Net.Proxy
{
    /// <summary>Socks5代理</summary>
    /// <remarks>
    /// 1，Socks5Request。协商认证方法
    /// 2，Socks5Answer。确定认证方法
    /// 3，Socks5Entity2。请求命令
    /// 4，Socks5Entity2。响应命令
    /// 5，Socks5Auth。请求认证
    /// 6，Socks5Answer。响应认证
    /// 7，开始传输
    /// </remarks>
    public class Socks5 : ProxyBase<Socks5.Session>
    {
        ///// <summary>创建会话</summary>
        ///// <param name="e"></param>
        ///// <returns></returns>
        //protected override INetSession CreateSession(NetEventArgs e)
        //{
        //    return new Session();
        //}

        #region 会话
        /// <summary>Socks5代理会话</summary>
        public class Session : ProxySession<Socks5, Session>
        {
            ///// <summary>代理对象</summary>
            //public new HttpReverseProxy Proxy { get { return base.Proxy as HttpReverseProxy; } set { base.Proxy = value; } }

            ///// <summary>收到客户端发来的数据。子类可通过重载该方法来修改数据</summary>
            ///// <param name="e"></param>
            ///// <param name="stream">数据</param>
            ///// <returns>修改后的数据</returns>
            //protected override Stream OnReceive(NetEventArgs e, Stream stream)
            //{
            //    return stream;
            //}
        }
        #endregion
    }
}