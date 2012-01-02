using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.SGIP
{
    /// <summary>MO指令</summary>
    public class SGIPDeliver : SGIPEntity
    {
        #region 属性
        [FieldSize(21)]
        private String _UserNumber;
        /// <summary>发送短消息的用户手机号，手机号码前加“86”国别标志</summary>
        public String UserNumber { get { return _UserNumber; } set { _UserNumber = value; } }

        private String _SPNumber;
        /// <summary>SP的接入号码</summary>
        public String SPNumber { get { return _SPNumber; } set { _SPNumber = value; } }

        private Byte _TP_pid;
        /// <summary>GSM协议类型。详细解释请参考GSM03.40中的9.2.3.9</summary>
        public Byte TP_pid { get { return _TP_pid; } set { _TP_pid = value; } }

        private Byte _TP_udhi;
        /// <summary>GSM协议类型。详细解释请参考GSM03.40中的9.2.3.23,仅使用1位，右对齐</summary>
        public Byte TP_udhi { get { return _TP_udhi; } set { _TP_udhi = value; } }

        private SGIPMessageCodings _MessageCoding;
        /// <summary>短消息的编码格式。</summary>
        public SGIPMessageCodings MessageCoding { get { return _MessageCoding; } set { _MessageCoding = value; } }

        private UInt32 _MessageLength;
        /// <summary>短消息的长度</summary>
        public UInt32 MessageLength { get { return _MessageLength; } set { _MessageLength = value; } }

        private String _MessageContent;
        /// <summary>短消息的内容</summary>
        public String MessageContent { get { return _MessageContent; } set { _MessageContent = value; } }

        [FieldSize(8)]
        private String _Reserve;
        /// <summary>保留，扩展用</summary>
        public String Reserve { get { return _Reserve; } set { _Reserve = value; } }
        #endregion
    
        #region 构造
        /// <summary>实例化</summary>
        public SGIPDeliver() : base(SGIPCommands.Deliver) { }
        #endregion
}

    /// <summary>
    /// 短消息的编码格式。
    /// </summary>
    public enum SGIPMessageCodings : byte
    {
        /// <summary>
        /// 0：纯ASCII字符串
        /// </summary>
        Ascii = 0,
        /// <summary>
        /// 3：写卡操作
        /// </summary>
        WriteCard = 3,
        /// <summary>
        /// 4：二进制编码
        /// </summary>
        Binary = 4,
        /// <summary>
        /// 8：UCS2编码
        /// </summary>
        Ucs2 = 8,
        /// <summary>
        /// 15: GBK编码
        /// </summary>
        Gbk = 15,
        /// <summary>
        /// 其它参见GSM3.38第4节：SMS Data Coding Scheme
        /// </summary>
        Others = 99,
    }
}