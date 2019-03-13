#if __WIN__
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NewLife.Data;
using NewLife.Log;
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
    /// };
    /// // 开始异步操作
    /// st.Open();
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
    public class SerialTransport : DisposeBase, ITransport, IDisposable
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

        /// <summary>端口名称。默认COM1</summary>
        public String PortName { get; set; } = "COM1";

        /// <summary>波特率。默认115200</summary>
        public Int32 BaudRate { get; set; } = 115200;

        /// <summary>奇偶校验位。默认None</summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>数据位。默认8</summary>
        public Int32 DataBits { get; set; } = 8;

        /// <summary>停止位。默认One</summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>超时时间。超过该大小未收到数据，说明是另一帧。默认10ms</summary>
        public Int32 Timeout { get; set; } = 10;

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

        ///// <summary>粘包处理接口</summary>
        //public IPacket Packet { get; set; }

        /// <summary>字节超时。数据包间隔，默认20ms</summary>
        public Int32 ByteTimeout { get; set; } = 20;
        #endregion

        #region 构造
        /// <summary>串口传输</summary>
        public SerialTransport()
        {
            // 每隔一段时间检查一次串口是否已经关闭，如果串口已经不存在，则关闭该传输口
            timer = new TimerX(CheckDisconnect, null, 3000, 3000);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            try
            {
                if (Serial != null) Close();
                if (timer != null) timer.Dispose();
            }
            catch { }
        }
        #endregion

        #region 方法
        /// <summary>确保创建</summary>
        public virtual void EnsureCreate()
        {
            if (Serial == null)
            {
                Serial = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);

                _Description = null;
            }
        }

        /// <summary>打开</summary>
        public virtual Boolean Open()
        {
            EnsureCreate();

            if (!Serial.IsOpen)
            {
                Serial.Open();
                if (Received != null) Serial.DataReceived += DataReceived;
            }

            return true;
        }

        /// <summary>关闭</summary>
        public virtual Boolean Close()
        {
            // 关闭时必须清空，否则更换属性后再次打开也无法改变属性
            var sp = Serial;
            if (sp != null)
            {
                Serial = null;
                if (Received != null) sp.DataReceived -= DataReceived;
                if (sp.IsOpen) sp.Close();

                OnDisconnect();
            }

            return true;
        }
        #endregion

        #region 发送
        /// <summary>写入数据</summary>
        /// <param name="pk">数据包</param>
        public virtual Boolean Send(Packet pk)
        {
            if (!Open()) return false;

            WriteLog("Send:{0}", pk.ToHex());

            var sp = Serial;
            lock (sp)
            {
                sp.Write(pk.Data, pk.Offset, pk.Count);
            }

            return true;
        }

        /// <summary>异步发送数据并等待响应</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public virtual async Task<Packet> SendAsync(Packet pk)
        {
            if (!Open()) return null;

            //if (Packet == null) Packet = new PacketProvider();

            //var task = Packet.Add(pk, null, Timeout);

            _Source = new TaskCompletionSource<Packet>();

            if (pk != null)
            {
                WriteLog("SendAsync:{0}", pk.ToHex());

                // 发送数据
                Serial.Write(pk.Data, pk.Offset, pk.Count);
            }

            return await _Source.Task;
        }

        /// <summary>接收数据</summary>
        /// <returns></returns>
        public virtual Packet Receive()
        {
            if (!Open()) return null;

            var task = SendAsync(null);
            if (Timeout > 0 && !task.Wait(Timeout)) return null;

            return task.Result;
        }
        #endregion

        #region 异步接收
        void DataReceived(Object sender, SerialDataReceivedEventArgs e)
        {
            // 发送者必须保持一定间隔，每个报文不能太大，否则会因为粘包拆包而出错
            try
            {
                var sp = sender as SerialPort;
                WaitMore();
                if (sp.BytesToRead > 0)
                {
                    var buf = new Byte[sp.BytesToRead];

                    var count = sp.Read(buf, 0, buf.Length);
                    //if (count != buf.Length) buf = buf.ReadBytes(0, count);
                    //var ms = new MemoryStream(buf, 0, count, false);
                    var pk = new Packet(buf, 0, count);

                    ProcessReceive(pk);
                }
            }
            catch (Exception ex)
            {
                //WriteLog("Error " + ex.Message);
                if (Log != null) Log.Error("DataReceived Error {0}", ex.Message);
            }
        }

        void WaitMore()
        {
            var sp = Serial;

            var ms = ByteTimeout;
            var end = DateTime.Now.AddMilliseconds(ms);
            var count = sp.BytesToRead;
            while (sp.IsOpen && end > DateTime.Now)
            {
                //Thread.SpinWait(1);
                Thread.Sleep(ms);
                if (count != sp.BytesToRead)
                {
                    end = DateTime.Now.AddMilliseconds(ms);
                    count = sp.BytesToRead;
                }
            }
        }

        void ProcessReceive(Packet pk)
        {
            try
            {
                //if (Packet == null)
                OnReceive(pk);
                //else
                //{
                //    // 拆包，多个包多次调用处理程序
                //    foreach (var msg in Packet.Parse(pk))
                //    {
                //        OnReceive(msg);
                //    }
                //}
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed()) Log.Error("{0}.OnReceive {1}", PortName, ex.Message);
            }
        }

        private TaskCompletionSource<Packet> _Source;
        /// <summary>处理收到的数据。默认匹配同步接收委托</summary>
        /// <param name="pk"></param>
        internal virtual void OnReceive(Packet pk)
        {
            //// 同步匹配
            //if (Packet != null && Packet.Match(pk, null)) return;

            if (_Source != null)
            {
                _Source.SetResult(pk);
                _Source = null;
                return;
            }

            // 触发事件
            Received?.Invoke(this, new ReceivedEventArgs { Packet = pk });
        }

        /// <summary>数据到达事件</summary>
        public event EventHandler<ReceivedEventArgs> Received;
        #endregion

        #region 自动检测串口断开
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
                WriteLog("串口{0}已经不存在，准备关闭！", PortName);

                //OnDisconnect();
                Close();
            }
        }
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

        /// <summary>获取串口列表，名称和描述</summary>
        /// <returns></returns>
        public static Dictionary<String, String> GetNames()
        {
            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM", false))
            using (var usb = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB", false))
            {
                if (key != null)
                {
                    foreach (var item in key.GetValueNames())
                    {
                        var name = key.GetValue(item) + "";
                        var des = "";

                        // 尝试枚举USB串口
                        foreach (var vid in usb.GetSubKeyNames())
                        {
                            var usbvid = usb.OpenSubKey(vid);
                            foreach (var elm in usbvid.GetSubKeyNames())
                            {
                                var sub = usbvid.OpenSubKey(elm);
                                //if (sub.GetValue("Class") + "" == "Ports")
                                {
                                    var FriendlyName = sub.GetValue("FriendlyName") + "";
                                    if (FriendlyName.Contains("({0})".F(name)))
                                    {
                                        des = FriendlyName.TrimEnd("({0})".F(name)).Trim();
                                        break;
                                    }
                                }
                            }
                            if (!des.IsNullOrEmpty()) break;
                        }

                        // 最后选择设备映射的串口名
                        if (des.IsNullOrEmpty())
                        {
                            des = item;
                            var p = item.LastIndexOf('\\');
                            if (p >= 0) des = des.Substring(p + 1);
                        }

                        //dic.Add(name, des);
                        // 某台机器上发现，串口有重复
                        dic[name] = des;
                    }
                }
            }
            return dic;
        }

        /// <summary>从串口列表选择串口，支持自动选择关键字</summary>
        /// <param name="keyWord">串口名称或者描述符的关键字</param>
        /// <returns></returns>
        public static SerialTransport Choose(String keyWord = null)
        {
            var ns = GetNames();
            if (ns.Count == 0)
            {
                Console.WriteLine("没有可用串口！");
                return null;
            }

            var name = "";
            var des = "";

            Console.WriteLine("可用串口：");
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (var item in ns)
            {
                if (item.Value == "Serial0") continue;

                if (keyWord != null && (item.Key.EqualIgnoreCase(keyWord) || item.Value.Contains(keyWord)))
                {
                    name = item.Key;
                    des = item.Value;
                }

                //Console.WriteLine(item);
                Console.WriteLine("{0,5}({1})", item.Key, item.Value);
            }
            // 没有自动选择，则默认最后一个
            if (name.IsNullOrEmpty())
            {
                var item = ns.Last();
                name = item.Key;
                des = item.Value;
            }
            while (true)
            {
                Console.ResetColor();
                Console.Write("请输入串口名称（默认 ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("{0}", name);
                Console.ResetColor();
                Console.Write("）：");

                var str = Console.ReadLine();
                if (str.IsNullOrEmpty()) break;

                // 只有输入有效串口名称才行
                if (ns.ContainsKey(str))
                {
                    name = str;
                    des = ns[str];
                    break;
                }
            }

            Console.WriteLine();
            Console.Write("正在打开串口 ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("{0}({1})", name, des);

            Console.ResetColor();

            var sp = new SerialTransport
            {
                PortName = name
            };

            return sp;
        }
        #endregion

        #region 日志
        /// <summary>日志对象</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log != null && Log.Enable) Log.Info(format, args);
        }

        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (!String.IsNullOrEmpty(PortName))
                return PortName;
            else
                return "(SerialPort)";
        }
        #endregion
    }
}
#endif