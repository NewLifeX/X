using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Messaging;
using NewLife.Serialization;

namespace NewLife.Net.CoAP
{
    /// <summary>受限制的应用协议(Constrained Application Protocol)</summary>
    /// <remarks>用于物联网M2M</remarks>
    public class CoAPMessage : Message<CoAPMessage>
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
        #endregion
    }
}