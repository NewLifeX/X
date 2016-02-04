using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using System.Xml.Serialization;
using NewLife.Xml;

namespace XNet
{
    /// <summary>网络口配置</summary>
    [XmlConfigFile("Config\\Net.config")]
    public class NetConfig : XmlConfig<NetConfig>
    {
        private String _Address;
        /// <summary>目的地址</summary>
        [Description("目的地址")]
        public String Address { get { return _Address; } set { _Address = value; } }

        private Int32 _Port;
        /// <summary>端口</summary>
        [Description("端口")]
        public Int32 Port { get { return _Port; } set { _Port = value; } }

        private Encoding _Encoding = Encoding.Default;
        /// <summary>文本编码</summary>
        [XmlIgnore]
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }

        /// <summary>编码</summary>
        [Description("编码 gb2312/us-ascii/utf-8")]
        public String WebEncoding { get { return _Encoding.WebName; } set { _Encoding = Encoding.GetEncoding(value); } }

        private Boolean _HexShow;
        /// <summary>十六进制显示</summary>
        [Description("十六进制显示")]
        public Boolean HexShow { get { return _HexShow; } set { _HexShow = value; } }

        /// <summary>显示接收字符串</summary>
        [Description("显示接收字符串")]
        public Boolean ShowReceiveString { get; set; }

        /// <summary>显示发送数据</summary>
        [Description("显示发送数据")]
        public Boolean ShowSend { get; set; }

        /// <summary>显示接收数据</summary>
        [Description("显示接收数据")]
        public Boolean ShowReceive { get; set; }

        /// <summary>显示统计信息</summary>
        [Description("显示统计信息")]
        public Boolean ShowStat { get; set; }
    }
}