using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Remoting;

namespace NewLife.Queue.Broker.Controllers
{
    public class ProducerInfoController : IApi
    {
        public IApiSession Session { get; set; }

        public bool RegisterProducer(string user,string pass)
        {
            var host = Session.GetService<ApiServer>();
            var producerInfoDict = host["ProducerInfoDict"] as ConcurrentDictionary<string, ProducerInfo>;
            if (producerInfoDict == null) return false;
            producerInfoDict.AddOrUpdate(Session.Remote.EndPoint.ToAddress(), key =>
            {
                var producerInfo = new ProducerInfo
                {
                    ProducerId = Session.Remote.EndPoint.ToAddress(),
                    HeartbeatInfo = new ClientHeartbeatInfo(Session) { LastHeartbeatTime = DateTime.Now }
                };
                host.Log.Info("Producer registered, producerId: {0}, connectionId: {1}", Session.Remote.EndPoint.ToAddress(), key);
                return producerInfo;
            }, (key, existingProducerInfo) =>
            {
                existingProducerInfo.HeartbeatInfo.LastHeartbeatTime = DateTime.Now;
                return existingProducerInfo;
            });
            return true;
        }
    }
}
