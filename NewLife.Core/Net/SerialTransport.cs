using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>串口传输</summary>
    /// <example>
    /// 标准例程：
    /// <code>
    /// var st = new SerialTransport();
    /// st.PortName = "COM65";  // 通讯口
    /// st.FrameSize = 16;      // 数据帧大小
    /// 
    /// st.Received += (s, e) =>
    /// {
    ///     Console.WriteLine("收到 {0}", e.ToHex());
    ///     // 返回null表示没有数据需要返回给对方
    ///     return null;
    /// };
    /// // 开始异步操作
    /// st.ReceiveAsync();
    /// 
    /// //var buf = "01080000801A".ToHex();
    /// var buf = "0111C02C".ToHex();
    /// for (int i = 0; i &lt; 100; i++)
    /// {
    ///     Console.WriteLine("发送 {0}", buf.ToHex());
    ///     st.Send(buf);
    /// 
    ///     Thread.Sleep(1000);
    /// }
    /// </code>
    /// </example>
    public class SerialTransport : ITransport, IDisposable
    {
        #region 属性
        private SerialPort _Serial;
        /// <summary>串口对象</summary>
        public SerialPort Serial
        {
            get { return _Serial; }
            set
            {
                _Serial = value;
                if (_Serial != null)
                {
                    PortName = _Serial.PortName;
                    BaudRate = _Serial.BaudRate;
                    Parity = _Serial.Parity;
                    DataBits = _Serial.DataBits;
                    StopBits = _Serial.StopBits;
                }
            }
        }

        private String _PortName = "COM1";
        /// <summary>端口名称。默认COM1</summary>
        public String PortName { get { return _PortName; } set { _PortName = value; } }

        private Int32 _BaudRate = 115200;
        /// <summary>波特率。默认115200</summary>
        public Int32 BaudRate { get { return _BaudRate; } set { _BaudRate = value; } }

        private Parity _Parity = Parity.None;
        /// <summary>奇偶校验位。默认None</summary>
        public Parity Parity { get { return _Parity; } set { _Parity = value; } }

        private Int32 _DataBits = 8;
        /// <summary>数据位。默认8</summary>
        public Int32 DataBits { get { return _DataBits; } set { _DataBits = value; } }

        private StopBits _StopBits = StopBits.One;
        /// <summary>停止位。默认One</summary>
        public StopBits StopBits { get { return _StopBits; } set { _StopBits = value; } }

        private Int32 _ExpectedFrame = 1;
        /// <summary>读取的期望帧长度，小于该长度为未满一帧，读取不做返回</summary>
        /// <remarks>如果读取超时，也有可能返回</remarks>
        public Int32 FrameSize { get { return _ExpectedFrame; } set { _ExpectedFrame = value; } }

        private String _Description;
        /// <summary>描述信息</summary>
        public String Description
        {
            get
            {
                if (_Description == null)
                {
                    var dic = GetNames();
                    if (!dic.TryGetValue(PortName, out _Description))
                        _Description = "";
                }
                return _Description;
            }
        }
        #endregion

        #region 构造
#if !MF
        /// <summary>串口传输</summary>
        public SerialTransport()
        {
            // 每隔一段时间检查一次串口是否已经关闭，如果串口已经不存在，则关闭该传输口
            timer = new TimerX(CheckDisconnect, null, 3000, 3000);
        }
#endif

        /// <summary>析构</summary>
        ~SerialTransport() { Dispose(false); }

        /// <summary>销毁</summary>
        public void Dispose() { Dispose(true); }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing) GC.SuppressFinalize(this);

            if (Serial != null) Close();
#if !MF
            if (timer != null) timer.Dispose();
#endif
        }
        #endregion

        #region 方法
        /// <summary>确保创建</summary>
        public virtual void EnsureCreate()
        {
            if (Serial == null) Serial = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
        }

        /// <summary>打开</summary>
        public virtual void Open()
        {
            EnsureCreate();

            if (!Serial.IsOpen) Serial.Open();

            _Description = null;
        }

        /// <summary>关闭</summary>
        public virtual void Close()
        {
            // 关闭时必须清空，否则更换属性后再次打开也无法改变属性
            var sp = Serial;
            if (sp != null)
            {
                Serial = null;
                if (sp.IsOpen)
                {
#if MF
                    // 关闭的时候不向外抛出错误，以确保完成关闭
                    try
                    {
                        sp.Close();
                    }
                    catch { }
#else
                    ThreadPoolX.QueueUserWorkItem(() => sp.Close());
#endif
                }
                //Serial = null;

#if !MF
                OnDisconnect();
#endif
            }
        }

        /// <summary>写入数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        public virtual void Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

#if !MF && DEBUG
            WriteLog("Write:{0}", BitConverter.ToString(buffer));
#endif

            if (count < 0) count = buffer.Length - offset;

            var sp = Serial;
            lock (sp)
            {
                sp.Write(buffer, offset, count);
            }
        }

        /// <summary>从串口中读取指定长度的数据，一般是一帧</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public virtual Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            if (count < 0) count = buffer.Length - offset;

            // 读取数据
            var bufstart = offset;
            var bufend = offset + count;
            var sp = Serial;
            lock (sp)
            {
                WaitMore();

                try
                {
                    var size = sp.BytesToRead;
                    // 计算还有多少可用空间
                    if (offset + size > bufend) size = bufend - offset;
                    var data = new Byte[size];
                    size = sp.Read(data, 0, data.Length);
                    if (size > 0)
                    {
                        buffer.Write(offset, data, 0, size);
                        offset += size;
                    }
                }
                catch { }
            }

#if !MF && DEBUG
            WriteLog("Read:{0} Expected/True={1}/{2}", BitConverter.ToString(buffer, bufstart, offset - bufstart), FrameSize, offset - bufstart);
#endif

            return offset - bufstart;
        }

        void WaitMore()
        {
            var sp = Serial;

            // 等待1秒，直到有数据为止
            var timeout = sp.ReadTimeout;
#if MF
            if (timeout <= 0) timeout = 500;
#else
            if (timeout <= 0) timeout = 200;
#endif
            var end = DateTime.Now.AddMilliseconds(timeout);
#if MF
            while (sp.BytesToRead < FrameSize && sp.IsOpen && end > DateTime.Now) Thread.Sleep(1);
#else
            while (sp.BytesToRead < FrameSize && sp.IsOpen && end > DateTime.Now) Thread.SpinWait(1);
#endif

#if MF
            // 注释下面这个代码后，性能提升15%
            var n = 0;
            // 暂停一会，可能还有数据
            while (sp.BytesToRead > n)
            {
                n = sp.BytesToRead;
                // 暂停一会，可能还有数据
                Thread.Sleep(10);
            }
#endif
        }
        #endregion

        #region 异步接收
        /// <summary>开始监听</summary>
        public virtual void ReceiveAsync()
        {
            Open();

            Serial.DataReceived += DataReceived;
            //Serial.ErrorReceived += Serial_ErrorReceived;
        }

        //void Serial_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        //{
        //    XTrace.WriteLine("串口{0}错误 {1}", PortName, e.EventType);
        //}

        void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 发送者必须保持一定间隔，每个报文不能太大，否则会因为粘包拆包而出错
            try
            {
                var sp = sender as SerialPort;
                WaitMore();
                if (sp.BytesToRead > 0)
                {
                    var buf = new byte[sp.BytesToRead];

                    var count = sp.Read(buf, 0, buf.Length);
                    if (count != buf.Length) buf = buf.ReadBytes(0, count);

                    OnReceive(buf);
                }
            }
            catch (Exception ex)
            {
                WriteLog("Error " + ex.Message);
            }
        }

        /// <summary>收到数据时触发</summary>
        /// <param name="buf"></param>
        protected virtual void OnReceive(Byte[] buf)
        {
            if (Received != null)
            {
                buf = Received(this, buf);

                // 数据发回去
                if (buf != null) Serial.Write(buf, 0, buf.Length);
            }
        }

        /// <summary>数据到达事件，事件里调用<see cref="Receive"/>读取数据</summary>
        public event TransportEventHandler Received;
        #endregion

        #region 自动检测串口断开
#if !MF
        /// <summary>断开时触发，可能是人为断开，也可能是串口链路断开</summary>
        public event EventHandler Disconnected;

        Boolean isInEvent;
        void OnDisconnect()
        {
            if (Disconnected != null)
            {
                // 判断是否在事件中，避免外部在断开时间中调用Close造成死循环
                if (!isInEvent)
                {
                    isInEvent = true;

                    Disconnected(this, EventArgs.Empty);

                    isInEvent = false;
                }
            }
        }

        TimerX timer;
        /// <summary>检查串口是否已经断开</summary>
        /// <remarks>
        /// FX串口异步操作有严重的泄漏缺陷，如果外部硬件长时间断开，
        /// SerialPort.IsOpen检测不到，并且会无限大占用内存。
        /// </remarks>
        /// <param name="state"></param>
        void CheckDisconnect(Object state)
        {
            if (String.IsNullOrEmpty(PortName) || Serial == null || !Serial.IsOpen) return;

            // 如果端口已经不存在，则断开吧
            if (!SerialPort.GetPortNames().Contains(PortName))
            {
#if DEBUG
                NewLife.Log.XTrace.WriteLine("串口{0}已经不存在，准备关闭！", PortName);
#endif

                //OnDisconnect();
                Close();
            }
        }
#endif
        #endregion

        #region 辅助
        /// <summary>获取带有描述的串口名，没有时返回空数组</summary>
        /// <returns></returns>
        public static String[] GetPortNames()
        {
            var list = new List<String>();
            foreach (var item in GetNames())
            {
                list.Add(String.Format("{0}({1})", item.Key, item.Value));
            }
            return list.ToArray();
        }

        static Dictionary<String, String> GetNames()
        {
            var dic = new Dictionary<String, String>();
            using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM", false))
            {
                if (key != null)
                {
                    foreach (var item in key.GetValueNames())
                    {
                        var value = key.GetValue(item) + "";
                        var name = item;
                        var p = item.LastIndexOf('\\');
                        if (p >= 0) name = name.Substring(p + 1);

                        //list.Add(String.Format("{0}({1})", value, name));
                        dic.Add(value, name);
                    }
                }
            }
            return dic;
        }
        #endregion

        #region 日志
        /// <summary>输出日志</summary>
        /// <param name="formart"></param>
        /// <param name="args"></param>
        public static void WriteLog(String formart, params Object[] args)
        {
#if !MF
            NewLife.Log.XTrace.WriteLine(formart, args);
#endif
        }

        /// <summary>已重载</summary>
        /// <returns></returns>
        public override string ToString()
        {
            //return PortName + "(SerialPort)";
            // MF中没有String.IsNullOrEmpty
            if (PortName != null && PortName != String.Empty)
                return PortName;
            else
                return "(SerialPort)";
        }
        #endregion
    }
}