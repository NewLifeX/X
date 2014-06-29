using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using System.Xml.Serialization;
using NewLife.Xml;

namespace XCom
{
    [XmlConfigFile("XCom.config")]
    public class SerialConfig : XmlConfig<SerialConfig>
    {
        private String _PortName = "COM1";
        /// <summary>串口名</summary>
        [Description("串口名")]
        public String PortName { get { return _PortName; } set { _PortName = value; } }

        private Int32 _BaudRate = 115200;
        /// <summary>波特率</summary>
        [Description("波特率")]
        public Int32 BaudRate { get { return _BaudRate; } set { _BaudRate = value; } }

        private Int32 _DataBits = 8;
        /// <summary>数据位</summary>
        [Description("数据位")]
        public Int32 DataBits { get { return _DataBits; } set { _DataBits = value; } }

        private StopBits _StopBits = StopBits.One;
        /// <summary>停止位</summary>
        [Description("停止位 None/One/Two/OnePointFive")]
        public StopBits StopBits { get { return _StopBits; } set { _StopBits = value; } }

        private Parity _Parity = Parity.None;
        /// <summary>奇偶校验</summary>
        [Description("奇偶校验 None/Odd/Even/Mark/Space")]
        public Parity Parity { get { return _Parity; } set { _Parity = value; } }

        private Encoding _Encoding = Encoding.Default;
        [XmlIgnore]
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }

        /// <summary>编码</summary>
        [Description("编码 gb2312/us-ascii/utf-8")]
        public String WebEncoding { get { return _Encoding.WebName; } set { _Encoding = Encoding.GetEncoding(value); } }

        private Boolean _HexShow;
        /// <summary>十六进制显示</summary>
        [Description("十六进制显示")]
        public Boolean HexShow { get { return _HexShow; } set { _HexShow = value; } }

        private Boolean _HexSend;
        /// <summary>十六进制发送</summary>
        [Description("十六进制发送")]
        public Boolean HexSend { get { return _HexSend; } set { _HexSend = value; } }

        private DateTime _LastUpdate;
        /// <summary>最后更新时间</summary>
        [Description("最后更新时间")]
        public DateTime LastUpdate { get { return _LastUpdate; } set { _LastUpdate = value; } }
    }
}