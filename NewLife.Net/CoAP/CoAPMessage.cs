using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.CoAP
{
    /// <summary>受限制的应用协议(Constrained Application Protocol)</summary>
    /// <remarks>用于物联网M2M</remarks>
    public class CoAPMessage
    {
        #region 协议字段
        [BitSize(2)]
        private Byte _Ver;
        /// <summary>版本</summary>
        public Byte Ver { get { return _Ver; } set { _Ver = value; } }

        [BitSize(2)]
        private Byte _Type;
        /// <summary>类型</summary>
        public Byte Type { get { return _Type; } set { _Type = value; } }

        [BitSize(4)]
        private Byte _OptionCount;
        /// <summary>可选选项数量</summary>
        public Byte OptionCount { get { return _OptionCount; } set { _OptionCount = value; } }

        private Byte _Code;
        /// <summary>指令码</summary>
        public Byte Code { get { return _Code; } set { _Code = value; } }

        private Int32 _MessageID;
        /// <summary>消息编号</summary>
        public Int32 MessageID { get { return _MessageID; } set { _MessageID = value; } }
        #endregion

        #region 读写
        /// <summary>从流中读取消息</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static CoAPMessage Read(Stream stream)
        {
            var binary = new Binary();
            //binary.IsLittleEndian = true;
            binary.Stream = stream;

            return binary.Read<CoAPMessage>();
        }

        /// <summary>从字节数组中读取消息</summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static CoAPMessage Read(Byte[] buf) { return Read(new MemoryStream(buf)); }

        /// <summary>把消息写入流中</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            var binary = new Binary();
            //binary.IsLittleEndian = true;
            binary.Stream = stream;

            binary.Write(this);
        }

        /// <summary>转为字节数组</summary>
        /// <returns></returns>
        public Byte[] ToArray()
        {
            var ms = new MemoryStream();
            Write(ms);

            return ms.ToArray();
        }
        #endregion
    }
}