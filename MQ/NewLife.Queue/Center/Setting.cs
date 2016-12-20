using System.ComponentModel;
using NewLife.Xml;

namespace NewLife.Queue.Center
{
    /// <summary>
    /// 控制中心配置
    /// </summary>
    [DisplayName("控制中心配置")]
#if !__MOBILE__
    [XmlConfigFile(@"Config\CenterServer.config", 15000)]
#endif
    public class Setting : XmlConfig<Setting>
    {
        /// <summary>调试</summary>
        [Description("调试")]
        public bool Debug { get; set; } = true;
        /// <summary>网络端口</summary>
        [Description("网络端口")]
        public int Port { get; set; } = 3344;

        /// <summary>Broker不活跃最大允许时间，如果一个Broker超过此时间未发送心跳，则认为此Broker挂掉了；默认超时时间为10s;</summary>
        [Description("Broker不活跃最大允许时间，如果一个Broker超过此时间未发送心跳，则认为此Broker挂掉了；默认超时时间为10s;")]
        public int BrokerInactiveMaxMilliseconds { get; set; } = 10*1000;
    }
}
