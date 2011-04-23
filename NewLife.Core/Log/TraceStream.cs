using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Log
{
    /// <summary>
    /// 跟踪流。继承自MemoryStream，主要用于重写Read/Write行为，跟踪程序操作数据流的过程
    /// </summary>
    public class TraceStream : MemoryStream
    {
        #region 方法
        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            RaiseAction("Write", buffer, offset, count);

            base.Write(buffer, offset, count);
        }

        /// <summary>
        /// 写入一个字节
        /// </summary>
        /// <param name="value"></param>
        public override void WriteByte(byte value)
        {
            RaiseAction("WriteByte", value);

            base.WriteByte(value);
        }

        /// <summary>
        /// 读取
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Int32 n = base.Read(buffer, offset, count);

            RaiseAction("Read", buffer, offset, count, n);

            return n;
        }

        /// <summary>
        /// 读取一个字节
        /// </summary>
        /// <returns></returns>
        public override int ReadByte()
        {
            Int32 n = base.ReadByte();

            RaiseAction("ReadByte", n);

            return n;
        }
        #endregion

        #region 事件
        /// <summary>
        /// 操作时触发
        /// </summary>
        public event EventHandler<EventArgs<String, Object[]>> OnAction;

        void RaiseAction(String action, params Object[] args)
        {
            if (OnAction != null) OnAction(this, new EventArgs<string, object[]>(action, args));
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
                if (value)
                    OnAction += new EventHandler<EventArgs<string, object[]>>(TraceStream_OnAction);
                else
                    OnAction -= new EventHandler<EventArgs<string, object[]>>(TraceStream_OnAction);

                _UseConsole = value;
            }
        }

        private Encoding _Encoding = Encoding.UTF8;
        /// <summary>编码</summary>
        public Encoding Encoding
        {
            get { return _Encoding; }
            set { _Encoding = value; }
        }

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
                buffer = (Byte[])e.Arg2[0];
                offset = (Int32)e.Arg2[1];
                count = (Int32)e.Arg2[e.Arg2.Length - 1];
            }

            if (e.Arg2.Length == 1)
                Console.Write("{0:X2} ({0})", e.Arg2[0]);
            else
            {
                if (count == 1)
                    Console.Write("{0:X2} ({0})", Convert.ToInt32(buffer[0]));
                else
                    Console.Write(BitConverter.ToString(buffer, offset, count));
            }

            // 黄色内容
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\t");
            if (e.Arg2.Length == 1)
            {
                Int32 n = Convert.ToInt32(e.Arg2[0]);
                if (n >= 0) Console.Write(Convert.ToChar(n));
            }
            else
            {
                if (count == 1)
                    Console.Write("{0} ({1})", Convert.ToChar(buffer[0]), Convert.ToInt32(buffer[0]));
                else
                    Console.Write(Encoding.GetString(buffer, offset, count));
            }

            Console.ForegroundColor = color;
            Console.WriteLine();
        }
        #endregion
    }
}