using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Net;
using NewLife.Queue.Protocols;
using NewLife.Queue.Protocols.CenterServers.Requests;
using NewLife.Remoting;

namespace NewLife.Queue.Center.Controllers
{
    public class BrokerController : IApi
    {
        public IApiSession Session { get; set; }


        public RemotingResponse RegisterBroker(BrokerRegistrationRequest model)
        {
            var netsession = Session as INetSession;
            if (netsession != null)
            {
                var server = netsession.Server as CenterServer;
                server?.ClusterManager.RegisterBroker(Session,model);
            }
            return new RemotingResponse();
        }
    }

    class DemoController : IApi
    {
        public IApiSession Session { get; set; }

        public string Say(string msg)
        {
            var netsession = Session as INetSession;
            if (netsession != null)
            {
                var server = netsession.Server as NewLife.Queue.Center.CenterServer;
                return server?.Demo + msg;
            }
            return "不对" + msg;
        }
    }
}
