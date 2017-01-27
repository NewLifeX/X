using System;
using System.ComponentModel;
using System.Reflection;
using NewLife.Reflection;
using NewLife.Xml;

namespace NewLife.Net.Stress
{
    /// <summary>Tcp压力测试配置文件</summary>
    [XmlConfigFile("TcpStress.xml")]
    public class TcpStressConfig : XmlConfig<TcpStressConfig>
    {
        /// <summary>测试地址</summary>
        [Description("测试地址")]
        public String Address { get; set; } = "";

        /// <summary>测试端口</summary>
        [Description("测试端口")]
        public Int32 Port { get; set; } = 80;

        /// <summary>连接数</summary>
        [Description("连接数")]
        public Int32 Connections { get; set; } = 10000;

        /// <summary>连接间隔，单位毫秒</summary>
        [Description("连接间隔，单位毫秒")]
        public Int32 Interval { get; set; }

        /// <summary>发送的数据，十六进制数据使用0x开头</summary>
        [Description("发送的数据")]
        public String Data { get; set; } = "我是大石头！";

        /// <summary>使用前缀长度</summary>
        [Description("使用前缀长度")]
        public Boolean UseLength { get; set; }

        /// <summary>发送数据间隔，单位毫秒</summary>
        [Description("发送数据间隔，单位毫秒")]
        public Int32 SendInterval { get; set; } = 1000;

        /// <summary>每个连接发送数据次数</summary>
        [Description("每个连接发送数据次数")]
        public Int32 Times { get; set; } = 100;

        /// <summary>实例化</summary>
        public TcpStressConfig()
        {
        }

        /// <summary>新建配置</summary>
        protected override void OnNew()
        {
            Address = NetHelper.MyIP().ToString();
        }

        /// <summary>显示参数</summary>
        public void Show()
        {
            var cfg = this;

            var pis = cfg.GetType().GetProperties(true);
            var len = 0;
            foreach (var item in pis)
            {
                if (item.Name.Length > len) len = item.Name.Length;
            }
            var color = Console.ForegroundColor;
            foreach (var item in pis)
            {
                var des = item.GetCustomAttribute<DescriptionAttribute>();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("{0," + len + "}:", item.Name);

                Console.ForegroundColor = ConsoleColor.Red;
                var v = item.GetValue(cfg, null) + "";
                if (des != null && des.Description.IndexOf("毫秒") >= 0)
                {
                    Console.Write("{0,6}", v);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("ms");
                }
                else
                    Console.Write("{0,8}", v);

                Console.ForegroundColor = ConsoleColor.DarkGray;
                if (des != null) Console.Write("\t" + des.Description);
                Console.WriteLine();

                Console.ForegroundColor = color;
            }
        }
    }
}