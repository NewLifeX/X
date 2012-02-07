using System;
using System.IO;
using System.IO.Ports;
using System.Net;
using NewLife.IO;
using NewLife.Net.Sockets;
using NewLife.Security;
using System.Threading;

namespace NewLife.Net.Application
{
    /// <summary>串口服务器。把收发数据映射到本地的指定串口</summary>
    public class SerialServer : NetServer
    {
        #region 属性
        private String _PortName = "COM1";
        /// <summary>串口名。默认COM1</summary>
        public String PortName { get { return _PortName; } set { _PortName = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个串口服务器</summary>
        public SerialServer() { Port = 24; }
        #endregion

        #region 业务
        /// <summary>接受连接时，对于Udp是收到数据时（同时触发OnReceived）</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnAccepted(object sender, NetEventArgs e)
        {
            base.OnAccepted(sender, e);

            var session = e.Socket as ISocketSession;
            using (var sp = Open(session))
            {
                if (sp != null) ReadAndSend(sp, session, e.RemoteEndPoint);
            }
        }

        /// <summary>收到数据时</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            base.OnReceived(sender, e);

            var session = e.Socket as ISocketSession;
            using (var sp = Open(session))
            {
                if (sp == null) return;

                if (e.BytesTransferred > 0)
                {
                    WriteLog("Net=>SerialPort: {0}", e.Buffer.ToHex(e.Offset, e.BytesTransferred));
                    sp.Write(e.Buffer, e.Offset, e.BytesTransferred);
                }

                Thread.Sleep(1000);

                ReadAndSend(sp, session, e.RemoteEndPoint);
            }
        }

        SerialPort Open(ISocketSession session)
        {
            var sp = new SerialPort(PortName);
            try
            {
                sp.Open();
            }
            //catch (UnauthorizedAccessException)
            catch
            {
                session.Disconnect();
                return null;
            }
            return sp;
        }

        void ReadAndSend(SerialPort sp, ISocketSession session, EndPoint remote)
        {
            // 读取数据
            var ms = new MemoryStream();
            sp.ReadTimeout = 500;
            //while (sp.BytesToRead > 0)
            //{
            //    try
            //    {
            //        Int32 d = sp.ReadByte();
            //        if (d != -1) ms.WriteByte((Byte)d);
            //    }
            //    catch { break; }
            //}
            WriteLog("可读取数据：{0}", sp.BytesToRead);
            var str = sp.ReadExisting();
            if (!String.IsNullOrEmpty(str))
            {
                var buffer = sp.Encoding.GetBytes(str);
                ms.Write(buffer, 0, buffer.Length);
            }

            if (ms.Length > 0)
            {
                ms.Position = 0;
                WriteLog("SerialPort=>Net: {0}", ms.ReadBytes().ToHex());
                ms.Position = 0;
                session.Send(ms, remote);
            }
        }
        #endregion
    }
}