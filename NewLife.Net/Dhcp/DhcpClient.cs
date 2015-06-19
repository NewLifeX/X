using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Net.Dhcp
{
    /// <summary>DHCP客户端</summary>
    public class DhcpClient : DisposeBase
    {
        #region 属性
        private UdpServer _Client;
        /// <summary>网络客户端</summary>
        public UdpServer Client { get { return _Client; } set { _Client = value; } }

        private IPAddress _Address = IPAddress.Any;
        /// <summary>地址</summary>
        public IPAddress Address { get { return _Address; } set { _Address = value; } }

        private Byte[] _Mac;
        /// <summary>物理地址</summary>
        public Byte[] Mac { get { return _Mac; } set { _Mac = value; } }

        /// <summary>事务ID</summary>
        private Int32 TransID;

        private TimerX _Timer;
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (_Timer != null) _Timer.Dispose();

            Stop();
        }
        #endregion

        #region 主要方法
        /// <summary>开始</summary>
        public void Start()
        {
            if (Client == null)
            {
                Client = new UdpServer();
                Client.Received += Client_Received;
                Client.Open();

                _Timer = new TimerX(s =>
                {
                    try
                    {
                        SendDiscover();
                    }
                    catch (Exception ex) { WriteLog(ex.ToString()); }
                }, null, 0, 1000);
            }

            var rnd = new Random();
            TransID = rnd.Next();
            if (_Mac == null || _Mac.Length == 0)
            {
                _Mac = new Byte[6];
                rnd.NextBytes(_Mac);
            }
        }

        /// <summary>停止</summary>
        public void Stop()
        {
            var client = Client;
            if (client != null)
            {
                Client = null;
                client.Dispose();
            }
        }

        void Client_Received(object sender, ReceivedEventArgs e)
        {
            var dhcp = new DhcpEntity();
            dhcp.Read(e.Stream);

            var kind = dhcp.Kind;
            WriteLog("收到：{0}", dhcp);
            switch (kind)
            {
                case DhcpMessageType.Offer:
                    OnOffer(dhcp, e.UserState);
                    break;
                case DhcpMessageType.Ack:
                    OnAck(dhcp, e.UserState);
                    break;
                case DhcpMessageType.Nak:
                    OnNak(dhcp, e.UserState);
                    break;
                default:
                    WriteLog("未知消息：\r\n{0}", dhcp);
                    break;
            }
        }

        /// <summary>发送DHCP消息</summary>
        /// <param name="dhcp"></param>
        protected void Send(DhcpEntity dhcp)
        {
            dhcp.ClientMac = Mac;

            var opt = new DhcpOption();
            opt.SetClientId(Mac);
            dhcp.Options.Add(opt);

            if (Address != IPAddress.Any)
            {
                opt = new DhcpOption();
                opt.SetData(DhcpOptions.RequestedIP, Address.GetAddressBytes());
                dhcp.Options.Add(opt);
            }

            opt = new DhcpOption();
            opt.SetData(DhcpOptions.HostName, ("WSWL_SmartOS_").GetBytes());
            dhcp.Options.Add(opt);

            opt = new DhcpOption();
            opt.SetData(DhcpOptions.Vendor, "http://www.NewLifeX.com".GetBytes());
            dhcp.Options.Add(opt);

            opt = new DhcpOption();
            opt.SetData(DhcpOptions.ParameterList, new Byte[] { 0x01, 0x06, 0x03, 0x2b });
            dhcp.Options.Add(opt);

            dhcp.TransactionID = TransID;

            WriteLog("发送：{0}", dhcp);

            var buf = dhcp.ToArray();
            Client.Client.Broadcast(buf, 68);
        }

        /// <summary>广播发现</summary>
        public void SendDiscover()
        {
            var dhcp = new DhcpEntity();
            dhcp.Kind = DhcpMessageType.Discover;

            Send(dhcp);
        }

        /// <summary>提供IP</summary>
        /// <param name="dhcp"></param>
        /// <param name="state"></param>
        protected virtual void OnOffer(DhcpEntity dhcp, Object state)
        {
            if (dhcp.TransactionID != TransID)
            {
                XTrace.Log.Error("响应消息事务不等于本地事务 0x{0:X8} != 0x{1:X8}", dhcp.TransactionID, TransID);
                return;
            }

            Address = new IPAddress(dhcp.YourIP);
            WriteLog("得到地址 {0} 来自 {1}", Address, state);
            foreach (var item in dhcp.Options)
            {
                WriteLog("{0,-16}: {1}", item.Option, item.ToStr());
            }

            // 发送确认
            SendRequest();
        }

        /// <summary>发送请求</summary>
        public void SendRequest()
        {
            var dhcp = new DhcpEntity();
            dhcp.Kind = DhcpMessageType.Request;

            Send(dhcp);
        }

        /// <summary>确认</summary>
        /// <param name="dhcp"></param>
        /// <param name="state"></param>
        protected virtual void OnAck(DhcpEntity dhcp, Object state)
        {
            WriteLog("已确认地址 {0} 来自 {1}", Address, state);

            // 查找租约时间
            var opt = dhcp.Get(DhcpOptions.IPLeaseTime);
            if (opt != null)
            {
                var buf = opt.Data.ReadBytes();
                buf.Reverse();
                var ts = new TimeSpan(0, 0, buf.ToInt());
                WriteLog("租约到期：{0}", ts);
            }
        }

        /// <summary>拒绝</summary>
        /// <param name="dhcp"></param>
        /// <param name="state"></param>
        protected virtual void OnNak(DhcpEntity dhcp, Object state)
        {
            WriteLog("被拒绝 {0} 来自 {1}", Address, state);

            // 查找拒绝原因
            var opt = dhcp.Get(DhcpOptions.Message);
            if (opt != null)
            {
                WriteLog("拒绝原因：{0}", opt.ToStr());
            }
        }
        #endregion

        #region 日志
        private ILog _Log = Logger.Null;
        /// <summary>日志</summary>
        public ILog Log { get { return _Log; } set { _Log = value; } }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log.Info(format, args);
        }
        #endregion
    }
}