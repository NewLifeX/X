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

        /// <summary>工作线程间隔，单位：秒。不同工作线程的时间间隔用逗号或分号隔开。可以通过设置任务的时间间隔小于0来关闭指定任务</summary>
        [Description("工作线程间隔，单位：秒。不同工作线程的时间间隔用逗号或分号隔开。可以通过设置任务的时间间隔小于0来关闭指定任务")]
        public String Intervals { get; set; } = "3";

        /// <summary>最大活动时间，单位：秒。超过最大活动时间都还没有响应的线程将会被重启，防止线程执行时间过长。默认0秒，表示无限</summary>
        [Description("最大活动时间，单位：秒。超过最大活动时间都还没有响应的线程将会被重启，防止线程执行时间过长。默认0秒，表示无限")]
        public Int32 MaxActive { get; set; }

        /// <summary>最大占用内存，单位： M。超过最大占用时，整个服务进程将会重启，以释放资源。默认0秒，表示无限</summary>
        [Description("最大占用内存，单位： M。超过最大占用时，整个服务进程将会重启，以释放资源。默认0秒，表示无限")]
        public Int32 MaxMemory { get; set; }

        /// <summary>最大总线程数，单位：个。超过最大占用时，整个服务进程将会重启，以释放资源。默认0个，表示无限</summary>
        [Description("最大总线程数，单位：个。超过最大占用时，整个服务进程将会重启，以释放资源。默认0个，表示无限")]
        public Int32 MaxThread { get; set; }

        /// <summary>自动重启时间，单位：分。到达自动重启时间时，整个服务进程将会重启，以释放资源。默认0分，表示无限</summary>
        [Description("自动重启时间，单位：分。到达自动重启时间时，整个服务进程将会重启，以释放资源。默认0分，表示无限")]
        public Int32 AutoRestart { get; set; }

        /// <summary>等待退出。停止服务时，等待每个线程完成当前工作的退出时间，默认3000ms</summary>
        [Description("等待退出。停止服务时，等待每个线程完成当前工作的退出时间，默认3000ms")]
        public Int32 WaitForExit { get; set; } = 3000;

        /// <summary>看门狗，保护其它服务，每分钟检查一次。多个服务名逗号分隔</summary>
        [Description("看门狗，保护其它服务，每分钟检查一次。多个服务名逗号分隔")]
        public String WatchDog { get; set; } = "";

        /// <summary>依赖服务</summary>
        [Description("依赖服务")]
        public String ServicesDependedOn { get; set; } = "";
        #endregion

        #region 方法
        /// <summary>新建时调用</summary>
        protected override void OnNew()
        {
        }
        #endregion
    }
}