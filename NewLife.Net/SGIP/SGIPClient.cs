using System;
using System.IO;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;

namespace NewLife.Net.SGIP
{
    /// <summary>SGIP客户端</summary>
    public class SGIPClient : Netbase
    {
        #region 属性
        #region 基本属性
        private ISocketClient _Client;
        /// <summary>TCP客户端</summary>
        public ISocketClient Client { get { return _Client; } set { _Client = value; } }

        private String _IP;
        /// <summary>IP地址</summary>
        public String IP { get { return _IP; } set { _IP = value; } }

        private Int32 _Port;
        /// <summary>端口</summary>
        public Int32 Port { get { return _Port; } set { _Port = value; } }

        private UInt32 _SrcNodeSequence;
        /// <summary>源节点</summary>
        public UInt32 SrcNodeSequence { get { return _SrcNodeSequence; } set { _SrcNodeSequence = value; } }
        #endregion

        #region 登录属性
        private String _SystemID;
        /// <summary>帐号名</summary>
        public String SystemID { get { return _SystemID; } set { _SystemID = value; } }

        private String _Password;
        /// <summary>密码</summary>
        public String Password { get { return _Password; } set { _Password = value; } }

        private Boolean _Logined = false;
        /// <summary>是否已登录</summary>
        public Boolean Logined { get { return _Logined; } set { _Logined = value; } }
        #endregion

        #region 发信息属性
        private String _SPNumber;
        /// <summary>SP的接入号码。</summary>
        public String SPNumber { get { return _SPNumber; } set { _SPNumber = value; } }

        private String _CorpID;
        /// <summary>企业代码。取值范围：0～99999。</summary>
        public String CorpID { get { return _CorpID; } set { _CorpID = value; } }

        private String _ServiceType;
        /// <summary>业务代码</summary>
        public String ServiceType { get { return _ServiceType; } set { _ServiceType = value; } }
        #endregion
        #endregion

        #region 登录
        /// <summary>登录。发送Bind指令，接收Bind_Resp响应</summary>
        /// <returns></returns>
        public Boolean Login()
        {
            WriteLog(String.Format("正在连接服务器…… {0}:{1}", IP, Port));
            var client = new TcpSession();
            Client = client;
            try
            {
                //client.Connect(IP, Port);
                client.Connect(IP, Port);
            }
            catch (Exception ex)
            {
                String str = IP + ":" + Port.ToString();
                throw new NetException("连接网关服务器" + str + "出错，请确定网络是否畅通！" + ex.Message, ex);
            }

            var cmd = new SGIPBind();
            cmd.LoginName = SystemID;
            cmd.LoginPassowrd = Password;
            cmd.LoginType = LoginTypes.SpToSmg;

            WriteLog("正在登录……");

            var session = client as ISocketSession;
            session.Send(cmd.GetStream());
            var data = client.Receive();
            var resp = SGIPEntity.Read(new MemoryStream(data)) as SGIPResponse;

            if (resp == null) throw new Exception("登录失败！服务器没有响应！");
            if (resp.Result != SGIPErrorCodes.Success) throw new Exception("登录失败！" + resp.Result.GetDescription());

            //登录完成，开始读取指令
            client.Received += Client_Received;
            client.ReceiveAsync();

            Logined = true;

            WriteLog("登录成功！");

            return true;
        }

        void Client_Received(object sender, ReceivedEventArgs e)
        {
        }
        #endregion

        #region 发信息
        /// <summary>发信息</summary>
        /// <param name="target">目标对象</param>
        /// <param name="content"></param>
        public void SendMessage(String target, String content)
        {
            if (String.IsNullOrEmpty(target) || String.IsNullOrEmpty(content)) return;

            if (!target.StartsWith("86")) target = "86" + target;

            var cmd = new SGIPSubmit();
            String id = SystemID;
            if (id.Length > 4) id = SystemID.Substring(4);
            cmd.SPNumber = SPNumber + id + "4888";
            cmd.ChargeNumber = new String('0', 21);
            cmd.UserCount = 1;
            cmd.UserNumber = target;
            cmd.CorpId = CorpID;
            cmd.ServiceType = ServiceType;
            cmd.FeeType = FeeTypes.FreeSend;
            cmd.FeeValue = "0";
            cmd.GivenValue = "0";
            cmd.AgentFlag = SubmitAgentFlags.SouldIncome;
            cmd.MorelatetoMTFlag = SubmitMorelatetoMTFlags.NormalFirst;
            cmd.Priority = 0;
            cmd.ExpireTime = "";
            cmd.ScheduleTime = "";
            cmd.ReportFlag = SubmitReportFlags.Always;
            cmd.TP_pid = 0;
            cmd.TP_udhi = 0;
            cmd.MessageCoding = SGIPMessageCodings.Gbk;
            //cmd.MessageCoding = MessageCodings.Ascii;
            cmd.MessageType = 0;
            //cmd.MessageLength = (UInt32)content.Length;
            cmd.MessageContent = content;

            WriteLog("正在向" + target + "发信息……");
            //Submit_Resp resp = Send(cmd) as Submit_Resp;
            Send(cmd);
        }

        /// <summary>传递信息</summary>
        /// <param name="target">目标对象</param>
        /// <param name="content"></param>
        /// <returns></returns>
        public void DeliverMessage(String target, String content)
        {
            if (String.IsNullOrEmpty(target) || String.IsNullOrEmpty(content)) return;

            if (!target.StartsWith("86")) target = "86" + target;

            var cmd = new SGIPDeliver();
            cmd.UserNumber = target;
            cmd.SPNumber = SPNumber + SystemID.Substring(4) + "4888";
            cmd.TP_pid = 0;
            cmd.TP_udhi = 0;
            cmd.MessageCoding = SGIPMessageCodings.Gbk;
            cmd.MessageLength = (UInt32)content.Length;
            cmd.MessageContent = content;

            WriteLog("正在向" + target + "分发信息……");
            //Deliver_Resp resp = Send(cmd) as Deliver_Resp;
            Send(cmd);
        }
        #endregion

        #region 退出
        /// <summary>退出</summary>
        /// <returns></returns>
        public Boolean Logout()
        {
            if (Logined)
            {
                WriteLog("正在注销……");

                if (Client != null && Client.Socket.Connected)
                {
                    try
                    {
                        Write(new SGIPUnbind());
                        WriteLog("注销成功！");
                    }
                    catch (Exception ex)
                    {
                        WriteLog("注销失败！" + ex.Message);
                    }
                }

                Logined = false;
            }

            if (Client != null) Client.Close();

            return true;
        }
        #endregion

        #region 发命令
        /// <summary>发送指令，返回响应</summary>
        /// <param name="command">指令</param>
        /// <returns>响应</returns>
        private SGIPEntity Send(SGIPEntity command)
        {
            Write(command);

            return null;
        }
        #endregion

        #region 读写命令
        /// <summary>读命令</summary>
        /// <returns></returns>
        private SGIPEntity Read()
        {
            if (Client == null || !Client.Socket.Connected) throw new InvalidOperationException("没有连接到服务器！");

            try
            {
                var cmd = SGIPEntity.Read(new MemoryStream(Client.Receive()));
                if (cmd == null) throw new Exception("获取命令失败！");

                WriteLog("收包：" + cmd.ToString());
                return cmd;
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("读命令失败！", ex);
            }
        }

        /// <summary>写命令</summary>
        /// <param name="cmd"></param>
        private void Write(SGIPEntity cmd)
        {
            if (Client == null || !Client.Socket.Connected) throw new InvalidOperationException("没有连接到服务器！");

            if (cmd.SrcNodeSequence < 1) cmd.SrcNodeSequence = SrcNodeSequence;

            try
            {
                try
                {
                    Client.Send(cmd.GetStream());
                }
                catch (Exception ex)
                {
                    throw new Exception("发送" + cmd.Command + "指令失败！", ex);
                }

                WriteLog("发包：" + cmd);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("写" + cmd.Command + "命令失败！", ex);
            }
        }
        #endregion
    }
}