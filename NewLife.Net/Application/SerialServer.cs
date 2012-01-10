using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.IO.Ports;
using System.Net;
using NewLife.IO;
using System.IO;
using NewLife.Security;

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

        #region 扩展属性
        private SerialPort _SerialPort;
        /// <summary>串口</summary>
        public SerialPort SerialPort
        {
            get
            {
                if (_SerialPort == null)
                {
                    var sp = new SerialPort(PortName);
                    //sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                    sp.ReadTimeout = 1000;
                    sp.Open();

                    _SerialPort = sp;
                }
                return _SerialPort;
            }
            set { _SerialPort = value; }
        }
        #endregion

        #region 构造
        /// <summary>实例化一个串口服务器</summary>
        public SerialServer()
        {
            Port = 23;
        }

        /// <summary>子类重载实现资源释放逻辑</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (_SerialPort != null)
            {
                _SerialPort.Dispose();
                _SerialPort = null;
            }
        }
        #endregion

        #region 业务
        /// <summary>接受连接时，对于Udp是收到数据时（同时触发OnReceived）</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnAccepted(object sender, NetEventArgs e)
        {
            base.OnAccepted(sender, e);

            var session = e.Socket as ISocketSession;
            ReadAndSend(session, e.RemoteEndPoint);
        }

        /// <summary>收到数据时</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            base.OnReceived(sender, e);

            var session = e.Socket as ISocketSession;
            if (e.BytesTransferred > 0)
            {
                WriteLog("Net=>SerialPort: {0}", e.Buffer.ToHex(e.Offset, e.BytesTransferred));
                SerialPort.Write(e.Buffer, e.Offset, e.BytesTransferred);
            }

            ReadAndSend(session, e.RemoteEndPoint);
        }

        void ReadAndSend(ISocketSession session, EndPoint remote)
        {
            // 读取数据
            var ms = new MemoryStream();
            while (SerialPort.BytesToRead > 0)
            {
                Int32 d = SerialPort.ReadByte();
                if (d != -1) break;

                ms.WriteByte((Byte)d);
            }

            if (ms.Length > 0)
            {
                ms.Position = 0;
                WriteLog("SerialPort=>Net: {0}", ms.ReadBytes().ToHex());
                ms.Position = 0;
                session.Send(ms, remote);
            }
        }

        //private ReadWriteMemoryStream stream = new ReadWriteMemoryStream();

        //void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    Int32 d = SerialPort.ReadByte();
        //    if (d != -1) stream.WriteByte((Byte)d);
        //}
        #endregion
    }
}