using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Net;
using NewLife.Remoting;

namespace NewLife.Queue.Clients
{
    public class ProducerClient : DisposeBase
    {
        #region 属性

        /// <summary>名称</summary>
        public string Name { get; set; } = "laoqiu";

        /// <summary>远程地址</summary>
        public NetUri Remote { get; set; }

        /// <summary>网络客户端</summary>
        public ApiClient Client { get; set; }

        /// <summary>已登录</summary>
        public bool Logined { get; private set; }
        #endregion

        #region 构造函数
        /// <summary>实例化</summary>
        public ProducerClient(int port=2234)
        {
            Remote = new NetUri(NetType.Tcp, NetHelper.MyIP(), port);

            // 还未上消息格式，暂时用Udp替代Tcp，避免粘包问题
            //Remote = new NetUri(ProtocolType.Udp, NetHelper.MyIP(), 2234);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Close();

            Client.TryDispose();
        }
        #endregion

        #region 登录验证
        /// <summary>登录</summary>
        /// <returns></returns>
        public async Task<bool> RegisterProducer()
        {
            Open();

            var rs = await Client.InvokeAsync<bool>("ProducerInfo/RegisterProducer", new { user = Name, pass = Name.MD5() });
            Logined = rs;

            return rs;
        }
        #endregion

        #region 打开关闭
        /// <summary>打开</summary>
        public void Open()
        {
            var ac = Client;
            if (ac != null && !ac.Disposed) return;
            ac = new ApiClient(Remote)
            {
                Encoder = new JsonEncoder(),
                Log = Log
            };

            var ss = ac.Client as IApiSession;
            if (ss != null) ss["user"] = Name;

            ac.Open();

            Client = ac;

            //// 异步登录
            //if (!Name.IsNullOrEmpty()) Task.Run(Login);
        }

        /// <summary>关闭</summary>
        public void Close()
        {
            Client.Close("关闭");
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}
