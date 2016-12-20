using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Queue.Protocols.Brokers;
using NewLife.Queue.Protocols.CenterServers.Requests;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Net;

namespace NewLife.Queue.Center
{
    public class ClusterManager: IClusterManager
    {
        private readonly ConcurrentDictionary<string /*clusterName*/, Cluster> _clusterDict;
        private readonly CenterServer _centerServer;
        private readonly object _lockObj = new object();
        private TimerX _time;
        public ClusterManager(CenterServer centerServer)
        {
            _centerServer = centerServer;
            _clusterDict = new ConcurrentDictionary<string, Cluster>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiSession"></param>
        /// <param name="request"></param>
        public void RegisterBroker(IApiSession apiSession, BrokerRegistrationRequest request)
        {
            lock (_lockObj)
            {
                var brokerInfo = request.BrokerInfo;
                var cluster = _clusterDict.GetOrAdd(brokerInfo.ClusterName, x => new Cluster { ClusterName = x });
                var brokerGroup = cluster.BrokerGroups.GetOrAdd(brokerInfo.GroupName, x => new BrokerGroup { GroupName = x });
                Broker broker;
                if (!brokerGroup.Brokers.TryGetValue(brokerInfo.BrokerName, out broker))
                {
                    var netSession = apiSession as INetSession;
                    if (netSession != null)
                    {
                        var connectionId = netSession.Remote.EndPoint.ToAddress();
                        broker = new Broker
                        {
                            BrokerInfo = request.BrokerInfo,
                            TotalSendThroughput = request.TotalSendThroughput,
                            TotalConsumeThroughput = request.TotalConsumeThroughput,
                            TotalUnConsumedMessageCount = request.TotalUnConsumedMessageCount,
                            TopicQueueInfoList = request.TopicQueueInfoList,
                            TopicConsumeInfoList = request.TopicConsumeInfoList,
                            ProducerList = request.ProducerList,
                            ConsumerList = request.ConsumerList,
                            ApiSession = apiSession,
                            ConnectionId = connectionId,
                            LastActiveTime = DateTime.Now,
                            FirstRegisteredTime = DateTime.Now,
                            Group = brokerGroup
                        };
                        if (brokerGroup.Brokers.TryAdd(brokerInfo.BrokerName, broker))
                        {
                            _centerServer.Log.Info("Registered new broker, brokerInfo: {0}", brokerInfo.ToJson());
                        }
                    }
                    else
                    {
                        _centerServer.Log.Error("Registered broker Erro, brokerInfo: {0}", brokerInfo.ToJson());
                    }
                   
                }
                else
                {
                    broker.LastActiveTime = DateTime.Now;
                    broker.TotalSendThroughput = request.TotalSendThroughput;
                    broker.TotalConsumeThroughput = request.TotalConsumeThroughput;
                    broker.TotalUnConsumedMessageCount = request.TotalUnConsumedMessageCount;

                    if (!broker.BrokerInfo.IsEqualsWith(request.BrokerInfo))
                    {
                        _centerServer.Log.Info("Broker basicInfo changed, old: {0}, new: {1}", broker.BrokerInfo, request.BrokerInfo);
                        broker.BrokerInfo = request.BrokerInfo;
 
                    }

                    broker.TopicQueueInfoList = request.TopicQueueInfoList;
                    broker.TopicConsumeInfoList = request.TopicConsumeInfoList;
                    broker.ProducerList = request.ProducerList;
                    broker.ConsumerList = request.ConsumerList;
                }
            }
        }

        public void Start()
        {
            lock (_lockObj)
            {
                _clusterDict.Clear();
            }
            _time = new TimerX(s => { ScanNotActiveBroker(); }, null, 1000, 1000);
        }
        public void Shutdown()
        {
            lock (_lockObj)
            {
                _clusterDict.Clear();
            }
            _time?.Dispose();
        }

        private void ScanNotActiveBroker()
        {
            lock (_lockObj)
            {
                foreach (var cluster in _clusterDict.Values)
                {
                    foreach (var brokerGroup in cluster.BrokerGroups.Values)
                    {
                        var notActiveBrokers = new List<Broker>();
                        foreach (var broker in brokerGroup.Brokers.Values)
                        {
                            if (broker.IsTimeout(_centerServer.CurrentCfg.BrokerInactiveMaxMilliseconds))
                            {
                                notActiveBrokers.Add(broker);
                            }
                        }
                        if (notActiveBrokers.Count > 0)
                        {
                            foreach (var broker in notActiveBrokers)
                            {
                                Broker removed;
                                if (brokerGroup.Brokers.TryRemove(broker.BrokerInfo.BrokerName, out removed))
                                {
                                    //_logger.InfoFormat("Removed timeout broker, brokerInfo: {0}, lastActiveTime: {1}", _jsonSerializer.Serialize(removed.BrokerInfo), removed.LastActiveTime);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    class Broker
    {
        public BrokerInfo BrokerInfo { get; set; }
        public long TotalSendThroughput { get; set; }
        public long TotalConsumeThroughput { get; set; }
        public long TotalUnConsumedMessageCount { get; set; }
        public IList<TopicQueueInfo> TopicQueueInfoList = new List<TopicQueueInfo>();
        public IList<TopicConsumeInfo> TopicConsumeInfoList = new List<TopicConsumeInfo>();
        public IList<string> ProducerList = new List<string>();
        public IList<ConsumerInfo> ConsumerList = new List<ConsumerInfo>();
        public string ConnectionId { get; set; }
        public IApiSession ApiSession { get; set; }
        public DateTime LastActiveTime { get; set; }
        public BrokerGroup Group { get; set; }
        public DateTime FirstRegisteredTime { get; set; }

        public bool IsTimeout(double timeoutMilliseconds)
        {
            return (DateTime.Now - LastActiveTime).TotalMilliseconds >= timeoutMilliseconds;
        }
    }

    class BrokerGroup
    {
        public string GroupName { get; set; }
        public ConcurrentDictionary<string /*brokerName*/, Broker> Brokers = new ConcurrentDictionary<string, Broker>();
    }
    class Cluster
    {
        public string ClusterName { get; set; }
        public ConcurrentDictionary<string /*groupName*/, BrokerGroup> BrokerGroups = new ConcurrentDictionary<string, BrokerGroup>();
    }

}
