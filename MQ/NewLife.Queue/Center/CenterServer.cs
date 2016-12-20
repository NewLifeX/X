using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Net;
using NewLife.Queue.Center.Controllers;
using NewLife.Remoting;

namespace NewLife.Queue.Center
{
    /// <summary>
    /// 中心服务器
    /// </summary>
    public class CenterServer : ApiServer
    {
        public readonly Setting CurrentCfg = Setting.Current;
        public ClusterManager ClusterManager { get; }

        public string Demo => "My Info";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public CenterServer(int port = 0)
        {
            if (port == 0) port = CurrentCfg.Port;
            Encoder = new JsonEncoder();
            if (CurrentCfg.Debug) Log = XTrace.Log;
            Add(new NetUri(NetType.Unknown, "", port));
            ClusterManager = new ClusterManager(this);
            //// Register<BrokerController>();
             Register<DemoController>();
        }
    }
}
