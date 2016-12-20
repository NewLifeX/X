using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            return new RemotingResponse();
        }
    }
}
