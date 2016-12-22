using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Queue.Broker.Controllers;
using NewLife.Remoting;

namespace NewLife.Queue.Broker
{
    public class BrokerService: DisposeBase
    {
        #region 属性
        /// <summary>接口服务器</summary>
        public ApiServer ProducerServer { get; private set; }
        /// <summary>
        /// 发布者集合
        /// </summary>
        private readonly ConcurrentDictionary<string /*connectionId*/, ProducerInfo> _producerInfoDict = new ConcurrentDictionary<string, ProducerInfo>();
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public BrokerService(int port = 2234)
        {
            ProducerServer = new ApiServer(port);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Stop();
        }
        #endregion

        #region 主要方法
        /// <summary>开始服务</summary>
        public void Start()
        {
            if (ProducerServer.Active) return;

            // 编码器
            if (ProducerServer.Encoder == null) ProducerServer.Encoder = new JsonEncoder();

            // 注册控制器
            ProducerServer.Register<ProducerInfoController>();
            //Server.Register<TopicController>();
            //Server.Register<MessageController>();

            // 建立引用
            ProducerServer["ProducerInfoDict"] = _producerInfoDict;

            ProducerServer.Start();
        }

        /// <summary>停止服务</summary>
        public void Stop() { ProducerServer.Stop(); }
        #endregion
    }

    internal class ProducerInfo
    {
        public string ProducerId;
        public ClientHeartbeatInfo HeartbeatInfo;
    }

    public class ClientHeartbeatInfo
    {
        public IApiSession Session { get; private set; }
        public DateTime LastHeartbeatTime { get; set; }

        public ClientHeartbeatInfo(IApiSession session)
        {
            Session = session;
        }

        public bool IsTimeout(double timeoutMilliseconds)
        {
            return (DateTime.Now - LastHeartbeatTime).TotalMilliseconds >= timeoutMilliseconds;
        }
    }
}
