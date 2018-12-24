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
        /// <summary>是否启用全局调试。默认为不启用</summary>
        [Description("全局调试")]
        public Boolean Debug { get; set; }

        /// <summary>服务名</summary>
        [Description("服务名")]
        public String ServiceName { get; set; } = "";

        /// <summary>显示名</summary>
        [Description("显示名")]
        public String DisplayName { get; set; } = "";

        /// <summary>服务描述</summary>
        [Description("服务描述")]
        public String Description { get; set; } = "";

        /// <summary>最大占用内存，单位： M。超过最大占用时，整个服务进程将会重启，以释放资源。默认0秒，表示无限</summary>
        [Description("最大占用内存，单位： M。超过最大占用时，整个服务进程将会重启，以释放资源。默认0秒，表示无限")]
        public Int32 MaxMemory { get; set; } = 8096;

        /// <summary>最大线程数，单位：个。超过最大占用时，整个服务进程将会重启，以释放资源。默认0个，表示无限</summary>
        [Description("最大线程数，单位：个。超过最大占用时，整个服务进程将会重启，以释放资源。默认0个，表示无限")]
        public Int32 MaxThread { get; set; } = 1000;

        /// <summary>最大句柄数，单位：个。超过最大占用时，整个服务进程将会重启，以释放资源。默认0个，表示无限</summary>
        [Description("最大句柄数，单位：个。超过最大占用时，整个服务进程将会重启，以释放资源。默认0个，表示无限")]
        public Int32 MaxHandle { get; set; } = 10000;

        /// <summary>自动重启时间，单位：分。到达自动重启时间时，整个服务进程将会重启，以释放资源。默认0分，表示无限</summary>
        [Description("自动重启时间，单位：分。到达自动重启时间时，整个服务进程将会重启，以释放资源。默认0分，表示无限")]
        public Int32 AutoRestart { get; set; }

        /// <summary>等待退出。停止服务时，等待每个线程完成当前工作的退出时间，默认5000ms</summary>
        [Description("等待退出。停止服务时，等待每个线程完成当前工作的退出时间，默认5000ms")]
        public Int32 WaitForExit { get; set; } = 5000;

        /// <summary>看门狗，保护其它服务，每分钟检查一次。多个服务名逗号分隔</summary>
        [Description("看门狗，保护其它服务，每分钟检查一次。多个服务名逗号分隔")]
        public String WatchDog { get; set; } = "";
        #endregion
    }
}