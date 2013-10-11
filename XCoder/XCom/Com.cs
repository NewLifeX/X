using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using NewLife;
using NewLife.Log;

namespace XCom
{
    class Com : DisposeBase
    {
        #region 属性
        private SerialPort _Serial;
        /// <summary>串口</summary>
        public SerialPort Serial { get { return _Serial; } set { _Serial = value; } }

        private Int32 _BytesOfReceived;
        /// <summary>收到字节数</summary>
        public Int32 BytesOfReceived { get { return _BytesOfReceived; } set { _BytesOfReceived = value; } }

        private Int32 _BytesOfSent;
        /// <summary>已发送字节数</summary>
        public Int32 BytesOfSent { get { return _BytesOfSent; } set { _BytesOfSent = value; } }

        private Encoding _Encoding = Encoding.Default;
        /// <summary>编码</summary>
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }
        #endregion

        #region 构造
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (_Serial != null && _Serial.IsOpen)
            {
                try
                {
                    _Serial.Close();
                    _Serial.Dispose();
                    _Serial = null;
                }
                catch { }
            }
        }
        #endregion

        #region 方法
        public Com Open()
        {
            if (!Serial.IsOpen) Serial.Open();

            return this;
        }

        public void Write(String str) { Write(Encoding.GetBytes(str)); }

        /// <summary>写入数据</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Write(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            WriteLog("Write:{0}", BitConverter.ToString(buffer));

            if (count < 0) count = buffer.Length - offset;

            var sp = Serial;
            lock (sp)
            {
                _BytesOfSent += count;

                sp.Write(buffer, offset, count);
            }
        }

        public String ReadString()
        {
            var buf = new Byte[10240];
            var size = Read(buf);
            if (size <= 0) return null;

            return Encoding.GetString(buf, 0, size);
        }

        /// <summary>从串口中读取指定长度的数据，一般是一帧</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Int32 Read(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
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
                if (timeout <= 0) timeout = 100;

                var end = DateTime.Now.AddMilliseconds(timeout);
                while (sp.BytesToRead <= 0 && sp.IsOpen && end > DateTime.Now) Thread.SpinWait(1);

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

                    _BytesOfReceived += size;
                }
                catch { }
            }

            WriteLog("Read:{0}", BitConverter.ToString(buffer, bufstart, offset - bufstart));

            return offset - bufstart;
        }
        #endregion

        #region 异步接收
        /// <summary>开始监听</summary>
        public void Listen()
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
                    Thread.Sleep(1);
                }
                if (sp.BytesToRead > 0)
                {
                    var buf = new byte[sp.BytesToRead];
                    count = sp.Read(buf, 0, buf.Length);
                    if (count != buf.Length) buf = buf.ReadBytes(0, count);

                    if (Received != null)
                    {
                        _BytesOfReceived += buf.Length;

                        var e2 = new DataReceivedEventArgs { Data = buf };
                        Received(this, e2);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("Error " + ex.Message);
            }
        }

        /// <summary>数据到达事件，事件里调用<see cref="Read"/>读取数据</summary>
        public event EventHandler<DataReceivedEventArgs> Received;
        #endregion

        #region 日志
        /// <summary>输出日志</summary>
        /// <param name="formart"></param>
        /// <param name="args"></param>
        public static void WriteLog(String formart, params Object[] args)
        {
            XTrace.WriteLine(formart, args);
        }
        #endregion
    }
}