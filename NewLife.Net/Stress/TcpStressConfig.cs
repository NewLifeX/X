using System;
using System.ComponentModel;
using NewLife.Xml;

namespace NewLife.Net.Stress
{
    /// <summary>Tcp压力测试配置文件</summary>
    [XmlConfigFile("TcpStress.xml")]
    public class TcpStressConfig : XmlConfig<TcpStressConfig>
    {
        private String _Address = "127.0.0.1";
        /// <summary>测试地址</summary>
        [Description("测试地址")]
        public String Address { get { return _Address; } set { _Address = value; } }

        private Int32 _Port = 80;
        /// <summary>测试端口</summary>
        [Description("测试端口")]
        public Int32 Port { get { return _Port; } set { _Port = value; } }

        private Int32 _Connections = 10000;
        /// <summary>连接数</summary>
        [Description("连接数")]
        public Int32 Connections { get { return _Connections; } set { _Connections = value; } }

        private Int32 _Interval = 0;
        /// <summary>连接间隔，单位毫秒</summary>
        [Description("连接间隔，单位毫秒")]
        public Int32 Interval { get { return _Interval; } set { _Interval = value; } }

        private String _Data = "我是大石头！";
        /// <summary>发送的数据，十六进制数据使用0x开头</summary>
        [Description("发送的数据")]
        public String Data { get { return _Data; } set { _Data = value; } }

        private Boolean _UseLength;
        /// <summary>使用前缀长度</summary>
        [Description("使用前缀长度")]
        public Boolean UseLength { get { return _UseLength; } set { _UseLength = value; } }

        private Int32 _SendInterval = 1000;
        /// <summary>发送数据间隔，单位毫秒</summary>
        [Description("发送数据间隔，单位毫秒")]
        public Int32 SendInterval { get { return _SendInterval; } set { _SendInterval = value; } }

        private Int32 _Times = 100;
        /// <summary>每个连接发送数据次数</summary>
        [Description("每个连接发送数据次数")]
        public Int32 Times { get { return _Times; } set { _Times = value; } }

        /// <summary>显示参数</summary>
        public void Show()
        {
            var cfg = this;

            var pis = cfg.GetType().GetProperties();
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