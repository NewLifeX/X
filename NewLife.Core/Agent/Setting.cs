using System;
using System.ComponentModel;
using NewLife.Xml;

namespace NewLife.Agent
{
    /// <summary>服务设置</summary>
    [DisplayName("服务设置")]
    [XmlConfigFile(@"Config\Agent.config", 15000)]
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        /// <summary>服务名</summary>
        [Description("服务名")]
        public String ServiceName { get; set; } = "";

        /// <summary>显示名</summary>
        [Description("显示名")]
        public String DisplayName { get; set; } = "";

        /// <summary>服务描述</summary>
        [Description("服务描述")]
        public String Description { get; set; } = "";

        /// <summary>最大占用内存。超过最大占用时，整个服务进程将会重启，以释放资源。默认8096M</summary>
        [Description("最大占用内存。超过最大占用时，整个服务进程将会重启，以释放资源。默认8096M")]
        public Int32 MaxMemory { get; set; } = 8096;

        /// <summary>最大线程数。超过最大占用时，整个服务进程将会重启，以释放资源。默认1000个</summary>
        [Description("最大线程数。超过最大占用时，整个服务进程将会重启，以释放资源。默认1000个")]
        public Int32 MaxThread { get; set; } = 1000;

        /// <summary>最大句柄数。超过最大占用时，整个服务进程将会重启，以释放资源。默认10000</summary>
        [Description("最大句柄数。超过最大占用时，整个服务进程将会重启，以释放资源。默认10000个")]
        public Int32 MaxHandle { get; set; } = 10000;

        /// <summary>自动重启时间。到达自动重启时间时，整个服务进程将会重启，以释放资源。默认0分，表示无限</summary>
        [Description("自动重启时间。到达自动重启时间时，整个服务进程将会重启，以释放资源。默认0分，表示无限")]
        public Int32 AutoRestart { get; set; }

        /// <summary>看门狗，保护其它服务，每分钟检查一次。多个服务名逗号分隔</summary>
        [Description("看门狗，保护其它服务，每分钟检查一次。多个服务名逗号分隔")]
        public String WatchDog { get; set; } = "";
        #endregion
    }
}