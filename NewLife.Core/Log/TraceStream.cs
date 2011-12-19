using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Collections;

namespace NewLife.Log
{
    /// <summary>跟踪流。包装一个基础数据流，主要用于重写Read/Write等行为，跟踪程序操作数据流的过程</summary>
    public class TraceStream : Stream
    {
        private Stream _BaseStream;
        /// <summary>基础流</summary>
        public Stream BaseStream
        {
            get { return _BaseStream; }
            set { _BaseStream = value; }
        }

        private ICollection<String> _TraceMembers;
        /// <summary>跟踪的成员</summary>
        public ICollection<String> TraceMembers
        {
            get { return _TraceMembers ?? (_TraceMembers = new HashSet<String>(DefaultTraceMembers, StringComparer.OrdinalIgnoreCase)); }
            set { _TraceMembers = value; }
        }

        static readonly String[] DefaultTraceMembers = new String[] { "Write", "WriteByte", "Read", "ReadByte", "BeginRead", "BeginWrite", "EndRead", "EndWrite", "Seek", "Close", "Flush", "SetLength", "SetPosition" };

        #region 基本读写方法
        /// <summary>写入</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            RaiseAction("Write", buffer, offset, count);

            BaseStream.Write(buffer, offset, count);
        }

        /// <summary>写入一个字节</summary>
        /// <param name="value"></param>
        public override void WriteByte(byte value)
        {
            RaiseAction("WriteByte", value);

            BaseStream.WriteByte(value);
        }

        /// <summary>读取</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Int32 n = BaseStream.Read(buffer, offset, count);

            RaiseAction("Read", buffer, offset, count, n);

            return n;
        }

        /// <summary>读取一个字节</summary>
        /// <returns></returns>
        public override int ReadByte()
        {
            Int32 n = BaseStream.ReadByte();

            RaiseAction("ReadByte", n);

            return n;
        }
        #endregion

        #region 异步读写方法
        /// <summary>异步开始读</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            RaiseAction("BeginRead", offset, count);

            return BaseStream.BeginRead(buffer, offset, count, callback, state);
        }

        /// <summary>异步开始写</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            RaiseAction("BeginWrite", offset, count);

            return BaseStream.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <summary>异步读结束</summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        public override int EndRead(IAsyncResult asyncResult)
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
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
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
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            RaiseAction("SetLength", value);

            BaseStream.SetLength(value);
        }
        #endregion

        #region 属性
        /// <summary>可读</summary>
        public override bool CanRead { get { return BaseStream.CanRead; } }

        /// <summary>可搜索</summary>
        public override bool CanSeek { get { return BaseStream.CanSeek; } }

        /// <summary>可超时</summary>
        public override bool CanTimeout { get { return BaseStream.CanTimeout; } }

        /// <summary>可写</summary>
        public override bool CanWrite { get { return BaseStream.CanWrite; } }

        /// <summary>可读</summary>
        public override int ReadTimeout { get { return BaseStream.ReadTimeout; } set { BaseStream.ReadTimeout = value; } }

        /// <summary>读写超时</summary>
        public override int WriteTimeout { get { return base.WriteTimeout; } set { base.WriteTimeout = value; } }

        /// <summary>长度</summary>
        public override long Length { get { return BaseStream.Length; } }

        /// <summary>位置</summary>
        public override long Position
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
            if (stream == null) stream = new MemoryStream();
            BaseStream = stream;
            UseConsole = true;
        }
        #endregion

        #region 事件
        /// <summary>
        /// 操作时触发
        /// </summary>
        public event EventHandler<EventArgs<String, Object[]>> OnAction;

        void RaiseAction(String action, params Object[] args)
        {
            if (OnAction != null)
            {
                if (_TraceMembers != null && !_TraceMembers.Contains(action)) return;

                OnAction(this, new EventArgs<string, object[]>(action, args));
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
                //if (value && !Runtime.IsConsole) throw new InvalidOperationException("非控制台程序无法使用该功能！");
                if (value && !Runtime.IsConsole) return;
                if (value == _UseConsole) return;

                if (value)
                    OnAction += new EventHandler<EventArgs<string, object[]>>(TraceStream_OnAction);
                else
                    OnAction -= new EventHandler<EventArgs<string, object[]>>(TraceStream_OnAction);

                _UseConsole = value;
            }
        }

        private Encoding _Encoding = Encoding.UTF8;
        /// <summary>编码</summary>
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }

        void TraceStream_OnAction(object sender, EventArgs<string, object[]> e)
        {
            ConsoleColor color = Console.ForegroundColor;

            // 红色动作
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(e.Arg1);

            // 白色十六进制
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\t");

            Byte[] buffer = null;
            Int32 offset = 0;
            Int32 count = 0;
            if (e.Arg2.Length > 1)
            {
                if (e.Arg2[0] is Byte[]) buffer = (Byte[])e.Arg2[0];
                offset = (Int32)e.Arg2[1];
                count = (Int32)e.Arg2[e.Arg2.Length - 1];
            }

            if (e.Arg2.Length == 1)
            {
                Int32 n = Convert.ToInt32(e.Arg2[0]);
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
                    //Console.Write("{0:X2} ({0})", Convert.ToInt32(buffer[0]));
                    Int32 n = Convert.ToInt32(buffer[0]);
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
                //Int32 n = Convert.ToInt32(e.Arg2[0]);
                ////if (n >= 0) Console.Write(Convert.ToChar(n));
                //// 只显示可见字符
                //if (n >= '0') Console.Write(Convert.ToChar(n));

                if (e.Arg2[0] != null)
                {
                    TypeCode tc = Type.GetTypeCode(e.Arg2[0].GetType());
                    //if (tc >= TypeCode.Int16 && tc <= TypeCode.UInt64)
                    if (tc != TypeCode.Object)
                    {
                        Console.WriteLine(e.Arg2[0]);
                    }
                }
            }
            else if (buffer != null)
            {
                if (count == 1)
                {
                    //Console.Write("{0} ({1})", Convert.ToChar(buffer[0]), Convert.ToInt32(buffer[0]));
                    // 只显示可见字符
                    if (buffer[0] >= '0') Console.Write("{0} ({1})", Convert.ToChar(buffer[0]), Convert.ToInt32(buffer[0]));
                }
                else if (count == 2)
                    Console.Write(BitConverter.ToInt16(buffer, offset));
                else if (count == 4)
                    Console.Write(BitConverter.ToInt32(buffer, offset));
                else if (count < 50)
                    Console.Write(Encoding.GetString(buffer, offset, count));
            }

            Console.ForegroundColor = color;
            Console.WriteLine();
        }
        #endregion
    }
}