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
        /// <summary>目的地址</summary>
        [Description("目的地址")]
        public String Address { get; set; }

        /// <summary>端口</summary>
        [Description("端口")]
        public Int32 Port { get; set; }

        /// <summary>文本编码</summary>
        [XmlIgnore]
        public Encoding Encoding { get; set; }

        /// <summary>编码</summary>
        [Description("编码 gb2312/us-ascii/utf-8")]
        public String WebEncoding { get { return Encoding.WebName; } set { Encoding = Encoding.GetEncoding(value); } }

        /// <summary>十六进制显示</summary>
        [Description("十六进制显示")]
        public Boolean HexShow { get; set; }

        /// <summary>发送内容</summary>
        [Description("发送内容")]
        public String SendContent { get; set; }

        /// <summary>发送次数</summary>
        [Description("发送次数")]
        public Int32 SendTimes { get; set; }

        /// <summary>发送间隔。毫秒</summary>
        [Description("发送间隔。毫秒")]
        public Int32 SendSleep { get; set; }

        /// <summary>发送用户数</summary>
        [Description("发送用户数")]
        public Int32 SendUsers { get; set; }

        /// <summary>显示应用日志</summary>
        [Description("显示应用日志")]
        public Boolean ShowLog { get; set; }

        /// <summary>显示网络日志</summary>
        [Description("显示网络日志")]
        public Boolean ShowSocketLog { get; set; }

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

        public NetConfig()
        {
            Encoding = Encoding.Default;

            SendContent = "新生命开发团队，学无先后达者为师";
            SendTimes = 1;
            SendSleep = 1000;
            SendUsers = 1;

            ShowLog = true;
            ShowSocketLog = true;
            ShowReceiveString = true;
            ShowSend = true;
            ShowReceive = true;
            ShowStat = true;
        }
    }
}