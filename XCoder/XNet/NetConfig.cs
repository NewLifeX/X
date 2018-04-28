using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NewLife.Xml;

namespace XNet
{
    /// <summary>网络口配置</summary>
    [XmlConfigFile("Config\\Net.config")]
    public class NetConfig : XmlConfig<NetConfig>
    {
        #region 属性
        /// <summary>模式</summary>
        [Description("模式")]
        public Int32 Mode { get; set; } = 1;

        /// <summary>本地地址</summary>
        [Description("本地地址")]
        public String Local { get; set; }

        /// <summary>远程地址</summary>
        [Description("远程地址")]
        public String Address { get; set; } = "";

        /// <summary>端口</summary>
        [Description("端口")]
        public Int32 Port { get; set; }

        /// <summary>文本编码</summary>
        [XmlIgnore]
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>编码</summary>
        [Description("编码 gb2312/us-ascii/utf-8")]
        public String WebEncoding { get { return Encoding.WebName; } set { Encoding = Encoding.GetEncoding(value); } }

        /// <summary>十六进制显示</summary>
        [Description("十六进制显示")]
        public Boolean HexShow { get; set; }

        /// <summary>十六进制发送</summary>
        [Description("十六进制发送")]
        public Boolean HexSend { get; set; }

        /// <summary>发送内容</summary>
        [Description("发送内容")]
        public String SendContent { get; set; } = "新生命开发团队，学无先后达者为师";

        /// <summary>发送次数</summary>
        [Description("发送次数")]
        public Int32 SendTimes { get; set; } = 1;

        /// <summary>发送间隔。毫秒</summary>
        [Description("发送间隔。毫秒")]
        public Int32 SendSleep { get; set; } = 1000;

        /// <summary>发送用户数</summary>
        [Description("发送用户数")]
        public Int32 SendUsers { get; set; } = 1;

        /// <summary>显示应用日志</summary>
        [Description("显示应用日志")]
        public Boolean ShowLog { get; set; } = true;

        /// <summary>显示网络日志</summary>
        [Description("显示网络日志")]
        public Boolean ShowSocketLog { get; set; } = true;

        /// <summary>显示接收字符串</summary>
        [Description("显示接收字符串")]
        public Boolean ShowReceiveString { get; set; } = true;

        /// <summary>显示发送数据</summary>
        [Description("显示发送数据")]
        public Boolean ShowSend { get; set; } = true;

        /// <summary>显示接收数据</summary>
        [Description("显示接收数据")]
        public Boolean ShowReceive { get; set; } = true;

        /// <summary>显示统计信息</summary>
        [Description("显示统计信息")]
        public Boolean ShowStat { get; set; } = true;

        /// <summary>日志着色</summary>
        [Description("日志着色")]
        public Boolean ColorLog { get; set; } = true;
        #endregion

        #region 方法
        public NetConfig()
        {
        }

        public void AddAddress(String addr)
        {
            var addrs = (Address + "").Split(";").Distinct().ToList();
            addrs.Insert(0, addr);
            addrs = addrs.Distinct().ToList();
            while (addrs.Count > 10) addrs.RemoveAt(addrs.Count - 1);
            Address = addrs.Join(";");
        }
        #endregion
    }
}