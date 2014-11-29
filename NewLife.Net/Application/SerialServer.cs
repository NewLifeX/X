using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>串口服务器。把收发数据映射到本地的指定串口</summary>
    /// <remarks>
    /// 一定要注意：串口编程最大的问题在于硬件设备可能极为不稳定，响应速度不可估计，所以需要尽可能的等待。
    /// 
    /// 串口读取的学问非常的大，影响响应比较慢，要等一会才能得到数据，并且可能只是得到一部分，而无限制的等待，又会让整个读取变得相当漫长。
    /// 一般来说，串口传输的数据，是指定长度有头有尾的帧数据。因此，只需要设定好等待时间和帧大小，就能很好的读取数据。
    /// </remarks>
    public class SerialServer : NetServer<SerialServer.Session>
    {
        #region 属性
        private String _PortName = "COM1";
        /// <summary>串口名。默认COM1</summary>
        public String PortName { get { return _PortName; } set { _PortName = value; } }

        private Boolean _AutoClose = true;
        /// <summary>每次收发完数据之后自动关闭</summary>
        public Boolean AutoClose { get { return _AutoClose; } set { _AutoClose = value; } }

        private SerialPort _Serial;
        /// <summary>串口对象</summary>
        public SerialPort Serial { get { return _Serial; } set { _Serial = value; } }

        private Int32 _Timeout = 1000;
        /// <summary>超时时间</summary>
        public Int32 Timeout { get { return _Timeout; } set { _Timeout = value; } }

        private Int32 _ReceivedBytesThreshold = 1;
        /// <summary>每一帧长度</summary>
        public Int32 ReceivedBytesThreshold { get { return _ReceivedBytesThreshold; } set { _ReceivedBytesThreshold = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个串口服务器</summary>
        public SerialServer()
        {
            Port = 24;
            Name = "串口服务";
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            var sp = Serial;
            Serial = null;
            if (sp != null) sp.Dispose();
        }
        #endregion

        #region 事件
        /// <summary>创建窗口对象时触发</summary>
        public event EventHandler OnCreate;

        void RaiseCreate()
        {
            if (OnCreate != null) OnCreate(this, EventArgs.Empty);
        }
        #endregion

        #region 业务
        #endregion

        #region 会话
        /// <summary>串口服务会话</summary>
        public class Session : NetSession<SerialServer>
        {
            static readonly Byte[] vspStart = new Byte[] { 0xFF, 0xFA, 0x2C };
            static readonly Byte[] vspEnd = new Byte[] { 0xFF, 0xF0 };

            /// <summary>收到客户端发来的数据</summary>
            /// <param name="e"></param>
            protected override void OnReceive(ReceivedEventArgs e)
            {
                var ms = e.Stream;
                if (ms == null || ms.Length <= 0) return;

                var sp = Host.Serial;
                if (sp == null)
                {
                    sp = new SerialPort(Host.PortName);
                    Host.Serial = sp;

                    //sp.BaudRate = 9600;
                    //sp.DataBits = 8;
                    //sp.Parity = Parity.None;
                    //sp.StopBits = StopBits.One;
                    if (Host.Timeout > 0) sp.ReadTimeout = sp.WriteTimeout = Host.Timeout;
                    //sp.ReadBufferSize = sp.WriteBufferSize = 1024;
                    //sp.Handshake = Handshake.None;

                    //sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                    if (Host.ReceivedBytesThreshold > 0) sp.ReceivedBytesThreshold = Host.ReceivedBytesThreshold;

                    // 如果不设置，有时候可能读不到数据
                    sp.RtsEnable = true;
                    sp.DtrEnable = true;

                    Host.RaiseCreate();
                }

                //var ms = e.GetStream();

                #region 特殊处理VSP格式
                // 特殊处理VSP格式。有可能是先开始FFF0才到FFFA
                while (ms.Position < ms.Length && (ms.StartsWith(vspStart) || ms.StartsWith(vspEnd) && ms.StartsWith(vspStart)))
                {
                    var data = ms.ReadTo(vspEnd);
                    if (data != null)
                    {
                        WriteLog("VSP: {0}", data.ToHex());
                        switch (data[0])
                        {
                            case 0x01:
                                Array.Reverse(data, 1, 4);
                                var rate = BitConverter.ToInt32(data, 1);
                                if (rate > 0) sp.BaudRate = rate;
                                break;
                            case 0x02:
                                if (data[1] > 0) sp.DataBits = data[1];
                                break;
                            case 0x03:
                                if (data[1] > 0) sp.Parity = (Parity)data[1];
                                break;
                            case 0x04:
                                if (data[1] > 0) sp.StopBits = (StopBits)data[1];
                                break;
                            case 0x05:
                                if (data[1] > 0) sp.Handshake = (Handshake)data[1];
                                break;
                            default:
                                break;
                        }
                    }
                }
                #endregion

                if (ms.Position >= ms.Length) return;

                // 必须加锁，防止多线程冲突
                lock (sp)
                {
                    if (!sp.IsOpen) sp.Open();

                    // 写数据
                    var data2 = ms.ReadBytes();
                    WriteLog("Net=>SerialPort: {0}", data2.ToHex());
                    sp.Write(data2, 0, data2.Length);

                    // 读数据
                    ReadAndSend(sp, Session);

                    if (Host.AutoClose) sp.Close();
                }

                //base.OnReceive(e);
            }

            void ReadAndSend(SerialPort sp, ISocketSession session)
            {
                // 读取数据
                var data = Read(sp, Host.ReceivedBytesThreshold);

                if (data != null && data.Length > 0)
                {
                    WriteLog("SerialPort=>Net: {0}", data.ToHex());
                    session.Send(data, 0, data.Length);
                }
            }
        }
        #endregion

        #region 辅助
        /// <summary>从串口中读取指定长度的数据，一般是一帧</summary>
        /// <param name="sp"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Byte[] Read(SerialPort sp, Int32 length = 0)
        {
            // 读取数据
            var ms = new MemoryStream();
            if (sp.ReadTimeout < 1) sp.ReadTimeout = 1000;

            while (sp.IsOpen)
            {
                // 等待1秒，直到有数据为止
                var timeout = sp.ReadTimeout;
                if (timeout < 1) timeout = 500;
                var end = DateTime.Now.AddMilliseconds(timeout);
                while (sp.BytesToRead <= 0 && sp.IsOpen && end > DateTime.Now) Thread.SpinWait(1);

                if (!sp.IsOpen || sp.BytesToRead <= 0) break;

                try
                {
                    var data = new Byte[sp.BytesToRead];
                    var count = sp.Read(data, 0, data.Length);
                    if (count > 0) ms.Write(data, 0, count);
                }
                catch { break; }

                // 如果已经足够一帧，结束
                if (length > 0 && ms.Length >= length) break;
            }

            ms.Position = 0;
            return ms.ReadBytes();
        }
        #endregion
    }
}