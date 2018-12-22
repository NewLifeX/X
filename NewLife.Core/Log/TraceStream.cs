using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewLife.Log
{
    /// <summary>跟踪流。包装一个基础数据流，主要用于重写Read/Write等行为，跟踪程序操作数据流的过程</summary>
    public class TraceStream : Stream
    {
        #region 属性
        /// <summary>基础流</summary>
        public Stream BaseStream { get; set; }

        /// <summary>跟踪的成员</summary>
        public ICollection<String> TraceMembers { get; set; }

        /// <summary>是否小端字节序。x86系列则采用Little-Endian方式存储数据；网络协议都是Big-Endian；</summary>
        /// <remarks>
        /// 网络协议都是Big-Endian；
        /// Java编译的都是Big-Endian；
        /// Motorola的PowerPC是Big-Endian；
        /// x86系列则采用Little-Endian方式存储数据；
        /// ARM同时支持 big和little，实际应用中通常使用Little-Endian。
        /// </remarks>
        public Boolean IsLittleEndian { get; set; }

        static readonly String[] DefaultTraceMembers = new String[] { "Write", "WriteByte", "Read", "ReadByte", "BeginRead", "BeginWrite", "EndRead", "EndWrite", "Seek", "Close", "Flush", "SetLength", "SetPosition" };

        /// <summary>显示位置的步长，位移超过此长度后输出位置。默认16，设为0不输出位置</summary>
        public Int32 ShowPositionStep { get; set; }
        #endregion

        #region 基本读写方法
        /// <summary>写入</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        public override void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            RaiseAction("Write", buffer, offset, count);

            BaseStream.Write(buffer, offset, count);
        }

        /// <summary>写入一个字节</summary>
        /// <param name="value">数值</param>
        public override void WriteByte(Byte value)
        {
            RaiseAction("WriteByte", value);

            BaseStream.WriteByte(value);
        }

        /// <summary>读取</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            var n = BaseStream.Read(buffer, offset, count);

            RaiseAction("Read", buffer, offset, count, n);

            return n;
        }

        /// <summary>读取一个字节</summary>
        /// <returns></returns>
        public override Int32 ReadByte()
        {
            var n = BaseStream.ReadByte();

            RaiseAction("ReadByte", n);

            return n;
        }
        #endregion

        #region 异步读写方法
        /// <summary>异步开始读</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginRead(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state)
        {
            RaiseAction("BeginRead", offset, count);

            return BaseStream.BeginRead(buffer, offset, count, callback, state);
        }

        /// <summary>异步开始写</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginWrite(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state)
        {
            RaiseAction("BeginWrite", offset, count);

            return BaseStream.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <summary>异步读结束</summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        public override Int32 EndRead(IAsyncResult asyncResult)
        {
            RaiseAction("EndRead");

            return BaseStream.EndRead(asyncResult);
        }

        /// <summary>异步写结束</summary>
        /// <param name="asyncResult"></param>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            RaiseAction("EndWrite");

            BaseStream.EndWrite(asyncResult);
        }
        #endregion

        #region 其它方法
        /// <summary>设置流位置</summary>
        /// <param name="offset">偏移</param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            RaiseAction("Seek", offset, origin);

            return BaseStream.Seek(offset, origin);
        }

        /// <summary>关闭数据流</summary>
        public override void Close()
        {
            RaiseAction("Close");

            BaseStream.Close();
        }

        /// <summary>刷新缓冲区</summary>
        public override void Flush()
        {
            RaiseAction("Flush");

            BaseStream.Flush();
        }

        /// <summary>设置长度</summary>
        /// <param name="value">数值</param>
        public override void SetLength(Int64 value)
        {
            RaiseAction("SetLength", value);

            BaseStream.SetLength(value);
        }
        #endregion

        #region 属性
        /// <summary>可读</summary>
        public override Boolean CanRead { get { return BaseStream.CanRead; } }

        /// <summary>可搜索</summary>
        public override Boolean CanSeek { get { return BaseStream.CanSeek; } }

        /// <summary>可超时</summary>
        public override Boolean CanTimeout { get { return BaseStream.CanTimeout; } }

        /// <summary>可写</summary>
        public override Boolean CanWrite { get { return BaseStream.CanWrite; } }

        /// <summary>可读</summary>
        public override Int32 ReadTimeout { get { return BaseStream.ReadTimeout; } set { BaseStream.ReadTimeout = value; } }

        /// <summary>读写超时</summary>
        public override Int32 WriteTimeout { get { return base.WriteTimeout; } set { base.WriteTimeout = value; } }

        /// <summary>长度</summary>
        public override Int64 Length { get { return BaseStream.Length; } }

        /// <summary>位置</summary>
        public override Int64 Position
        {
            get { return BaseStream.Position; }
            set
            {
                RaiseAction("SetPosition", value);

                BaseStream.Position = value;
            }
        }
        #endregion

        #region 构造
        /// <summary>实例化跟踪流</summary>
        public TraceStream() : this(null) { }

        /// <summary>实例化跟踪流</summary>
        /// <param name="stream"></param>
        public TraceStream(Stream stream)
        {
            TraceMembers = new HashSet<String>(DefaultTraceMembers, StringComparer.OrdinalIgnoreCase);
            IsLittleEndian = true;
            ShowPositionStep = 16;
            Encoding = Encoding.UTF8;

            if (stream == null) stream = new MemoryStream();
            BaseStream = stream;
            UseConsole = true;

            if (!UseConsole) OnAction += XTrace_OnAction;
        }
        #endregion

        #region 事件
        /// <summary>操作时触发</summary>
        public event EventHandler<EventArgs<String, Object[]>> OnAction;

        Int64 lastPosition = -1;
        void RaiseAction(String action, params Object[] args)
        {
            if (OnAction != null)
            {
                if (!TraceMembers.Contains(action)) return;

                if (ShowPositionStep > 0)
                {
                    var cp = Position;
                    if (lastPosition < 0)
                    {
                        lastPosition = cp;
                        OnAction(this, new EventArgs<String, Object[]>("BeginPosition", new Object[] { lastPosition }));
                    }

                    if (cp > lastPosition + ShowPositionStep)
                    {
                        lastPosition = cp;
                        OnAction(this, new EventArgs<String, Object[]>("Position", new Object[] { lastPosition }));
                    }
                }

                OnAction(this, new EventArgs<String, Object[]>(action, args));
            }
        }
        #endregion

        #region 控制台
        private Boolean _UseConsole;
        /// <summary>是否使用控制台</summary>
        public Boolean UseConsole
        {
            get { return _UseConsole; }
            set
            {
                if (value && !Runtime.IsConsole) return;
                if (value == _UseConsole) return;

                if (value)
                    OnAction += TraceStream_OnAction;
                else
                    OnAction -= TraceStream_OnAction;

                _UseConsole = value;
            }
        }

        /// <summary>编码</summary>
        public Encoding Encoding { get; set; }

        void TraceStream_OnAction(Object sender, EventArgs<String, Object[]> e)
        {
            var color = Console.ForegroundColor;

            // 红色动作
            Console.ForegroundColor = ConsoleColor.Red;
            var act = e.Arg1;
            if (act.Length < 8) act += "\t";
            Console.Write(act);

            // 白色十六进制
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\t");

            Byte[] buffer = null;
            var offset = 0;
            var count = 0;
            if (e.Arg2.Length > 1)
            {
                if (e.Arg2[0] is Byte[]) buffer = (Byte[])e.Arg2[0];
                offset = (Int32)e.Arg2[1];
                count = (Int32)e.Arg2[e.Arg2.Length - 1];
            }

            if (e.Arg2.Length == 1)
            {
                var n = Convert.ToInt32(e.Arg2[0]);
                // 大于10才显示十进制
                if (n >= 10)
                    Console.Write("{0:X2} ({0})", n);
                else
                    Console.Write("{0:X2}", n);
            }
            else if (buffer != null)
            {
                if (count == 1)
                {
                    var n = Convert.ToInt32(buffer[0]);
                    // 大于10才显示十进制
                    if (n >= 10)
                        Console.Write("{0:X2} ({0})", n);
                    else
                        Console.Write("{0:X2}", n);
                }
                else
                    Console.Write(BitConverter.ToString(buffer, offset, count <= 50 ? count : 50) + (count <= 50 ? "" : "...（共" + count + "）"));
            }
            // 黄色内容
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\t");
            if (e.Arg2.Length == 1)
            {
                if (e.Arg2[0] != null)
                {
                    var tc = Type.GetTypeCode(e.Arg2[0].GetType());
                    if (tc != TypeCode.Object) Console.Write(e.Arg2[0]);
                }
            }
            else if (buffer != null)
            {
                if (count == 1)
                {
                    // 只显示可见字符
                    if (buffer[0] >= '0') Console.Write("{0} ({1})", Convert.ToChar(buffer[0]), Convert.ToInt32(buffer[0]));
                }
                else if (count == 2)
                    Console.Write(BitConverter.ToInt16(Format(buffer), offset));
                else if (count == 4)
                    Console.Write(BitConverter.ToInt32(Format(buffer), offset));
                else if (count < 50)
                    Console.Write(Encoding.GetString(buffer, offset, count));
            }
            Console.ForegroundColor = color;
            Console.WriteLine();
        }

        Byte[] Format(Byte[] buffer)
        {
            if (buffer == null || buffer.Length < 1) return buffer;

            if (IsLittleEndian) return buffer;

            // 不要改变原来的数组
            var bts = new Byte[buffer.Length];
            Buffer.BlockCopy(buffer, 0, bts, 0, bts.Length);
            Array.Reverse(bts);

            return bts;
        }
        #endregion

        #region 日志
        void XTrace_OnAction(Object sender, EventArgs<String, Object[]> e)
        {
            var sb = new StringBuilder();

            // 红色动作
            var act = e.Arg1;
            if (act.Length < 8) act += "\t";
            sb.AppendFormat(act);

            // 白色十六进制
            sb.AppendFormat("\t");

            Byte[] buffer = null;
            var offset = 0;
            var count = 0;
            if (e.Arg2.Length > 1)
            {
                if (e.Arg2[0] is Byte[]) buffer = (Byte[])e.Arg2[0];
                offset = (Int32)e.Arg2[1];
                count = (Int32)e.Arg2[e.Arg2.Length - 1];
            }

            if (e.Arg2.Length == 1)
            {
                var n = Convert.ToInt32(e.Arg2[0]);
                // 大于10才显示十进制
                if (n >= 10)
                    sb.AppendFormat("{0:X2} ({0})", n);
                else
                    sb.AppendFormat("{0:X2}", n);
            }
            else if (buffer != null)
            {
                if (count == 1)
                {
                    var n = Convert.ToInt32(buffer[0]);
                    // 大于10才显示十进制
                    if (n >= 10)
                        sb.AppendFormat("{0:X2} ({0})", n);
                    else
                        sb.AppendFormat("{0:X2}", n);
                }
                else
                    sb.AppendFormat(BitConverter.ToString(buffer, offset, count <= 50 ? count : 50) + (count <= 50 ? "" : "...（共" + count + "）"));
            }

            // 黄色内容
            sb.AppendFormat("\t");
            if (e.Arg2.Length == 1)
            {
                if (e.Arg2[0] != null)
                {
                    var tc = Type.GetTypeCode(e.Arg2[0].GetType());
                    if (tc != TypeCode.Object) sb.AppendFormat(e.Arg2[0] + "");
                }
            }
            else if (buffer != null)
            {
                if (count == 1)
                {
                    // 只显示可见字符
                    if (buffer[0] >= '0') sb.AppendFormat("{0} ({1})", Convert.ToChar(buffer[0]), Convert.ToInt32(buffer[0]));
                }
                else if (count == 2)
                    sb.AppendFormat(BitConverter.ToInt16(Format(buffer), offset) + "");
                else if (count == 4)
                    sb.AppendFormat(BitConverter.ToInt32(Format(buffer), offset) + "");
                else if (count < 50)
                    sb.AppendFormat(Encoding.GetString(buffer, offset, count));
            }

            XTrace.WriteLine(sb.ToString());
        }
        #endregion
    }
}