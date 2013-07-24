using System;
using System.IO.Ports;
using System.Threading;

namespace NewLife.Net.Modbus
{
    /// <summary>串口传输</summary>
    public class SerialTransport : ITransport, IDisposable
    {
        #region 属性
        private SerialPort _Serial;
        /// <summary>串口对象</summary>
        public SerialPort Serial { get { return _Serial; } set { _Serial = value; } }

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
        #endregion

        #region 构造
        /// <summary>析构</summary>
        ~SerialTransport() { Dispose(false); }

        /// <summary>销毁</summary>
        public void Dispose() { Dispose(true); }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing) GC.SuppressFinalize(this);

            if (Serial != null) Serial.Dispose();
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
        }

        /// <summary>关闭</summary>
        public virtual void Close()
        {
            if (Serial != null && Serial.IsOpen) Serial.Close();
        }

        /// <summary>写入数据</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public virtual void Write(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

#if !MF
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
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public virtual Int32 Read(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            if (count < 0) count = buffer.Length - offset;

            // 读取数据
            var bufstart = offset;
            var bufend = offset + count;
            var sp = Serial;
            lock (sp)
            {
                // 等待1秒，直到有数据为止
                var timeout = sp.ReadTimeout;
#if MF
                if (timeout <= 0) timeout = 500;
#else
                if (timeout <= 0) timeout = 100;
#endif
                var end = DateTime.Now.AddMilliseconds(timeout);
#if MF
                while (sp.BytesToRead <= 0 && sp.IsOpen && end > DateTime.Now) Thread.Sleep(1);
#else
                while (sp.BytesToRead <= 0 && sp.IsOpen && end > DateTime.Now) Thread.SpinWait(1);
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

#if !MF
            WriteLog("Read:{0}", BitConverter.ToString(buffer, bufstart, offset - bufstart));
#endif

            return offset - bufstart;
        }
        #endregion

        #region 异步接收
        /// <summary>开始监听</summary>
        public virtual void Listen()
        {
            Open();

            Serial.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
            //serial.ErrorReceived += new SerialErrorReceivedEventHandler(port_ErrorReceived);
        }

        void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 发送者必须保持一定间隔，每个报文不能太大，否则会因为粘包拆包而出错
            try
            {
                var sp = sender as SerialPort;
                var count = 0;
                while (sp.BytesToRead > count)
                {
                    count = sp.BytesToRead;
                    // 暂停一会，可能还有数据
                    Thread.Sleep(10);
                }
                if (sp.BytesToRead >= ModbusEntity.NO_DATA_LENGTH)
                {
                    var buf = new byte[sp.BytesToRead];
                    count = sp.Read(buf, 0, buf.Length);
                    if (count != buf.Length) buf = buf.ReadBytes(count);

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
            if (Received != null) buf = Received(this, buf);
        }

        /// <summary>数据到达事件，事件里调用<see cref="Read"/>读取数据</summary>
        public event TransportEventHandler Received;
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
        #endregion
    }
}