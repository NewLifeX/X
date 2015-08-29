using System;
using System.ComponentModel;
using NewLife.Configuration;
using NewLife.Xml;

namespace XAgent
{
    /// <summary>服务设置</summary>
    [DisplayName("服务设置")]
    [XmlConfigFile(@"Config\XAgent.config", 15000)]
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        private Boolean _Debug;
        /// <summary>是否启用全局调试。默认为不启用</summary>
        [Description("全局调试")]
        public Boolean Debug { get { return _Debug; } set { _Debug = value; } }

        private String _ServiceName;
        /// <summary>服务名</summary>
        [Description("服务名")]
        public String ServiceName { get { return _ServiceName; } set { _ServiceName = value; } }

        private String _DisplayName;
        /// <summary>显示名</summary>
        [Description("显示名")]
        public String DisplayName { get { return _DisplayName; } set { _DisplayName = value; } }

        private String _Description;
        /// <summary>服务描述</summary>
        [Description("服务描述")]
        public String Description { get { return _Description; } set { _Description = value; } }

        private String _Intervals = "3";
        /// <summary>工作线程间隔，单位：秒。不同工作线程的时间间隔用逗号或分号隔开。可以通过设置任务的时间间隔小于0来关闭指定任务</summary>
        [Description("工作线程间隔，单位：秒。不同工作线程的时间间隔用逗号或分号隔开。可以通过设置任务的时间间隔小于0来关闭指定任务")]
        public String Intervals { get { return _Intervals; } set { _Intervals = value; } }

        private Int32 _MaxActive;
        /// <summary>最大活动时间，单位：秒。超过最大活动时间都还没有响应的线程将会被重启，防止线程执行时间过长。默认0秒，表示无限</summary>
        [Description("最大活动时间，单位：秒。超过最大活动时间都还没有响应的线程将会被重启，防止线程执行时间过长。默认0秒，表示无限")]
        public Int32 MaxActive { get { return _MaxActive; } set { _MaxActive = value; } }

        private Int32 _MaxMemory;
        /// <summary>最大占用内存，单位： M。超过最大占用时，整个服务进程将会重启，以释放资源。默认0秒，表示无限</summary>
        [Description("最大占用内存，单位： M。超过最大占用时，整个服务进程将会重启，以释放资源。默认0秒，表示无限")]
        public Int32 MaxMemory { get { return _MaxMemory; } set { _MaxMemory = value; } }

        private Int32 _MaxThread;
        /// <summary>最大总线程数，单位：个。超过最大占用时，整个服务进程将会重启，以释放资源。默认0个，表示无限</summary>
        [Description("最大总线程数，单位：个。超过最大占用时，整个服务进程将会重启，以释放资源。默认0个，表示无限")]
        public Int32 MaxThread { get { return _MaxThread; } set { _MaxThread = value; } }

        private Int32 _AutoRestart;
        /// <summary>自动重启时间，单位：分。到达自动重启时间时，整个服务进程将会重启，以释放资源。默认0分，表示无限</summary>
        [Description("自动重启时间，单位：分。到达自动重启时间时，整个服务进程将会重启，以释放资源。默认0分，表示无限")]
        public Int32 AutoRestart { get { return _AutoRestart; } set { _AutoRestart = value; } }

        private String _WatchDog;
        /// <summary>看门狗，保护其它服务，每分钟检查一次。多个服务名逗号分隔</summary>
        [Description("看门狗，保护其它服务，每分钟检查一次。多个服务名逗号分隔")]
        public String WatchDog { get { return _WatchDog; } set { _WatchDog = value; } }
        #endregion

        #region 方法
        /// <summary>新建时调用</summary>
        protected override void OnNew()
        {
            Debug = Config.GetConfig<Boolean>("XAgent.Debug", false);
            ServiceName = Config.GetConfig<String>("XAgent.ServiceName");
            DisplayName = Config.GetConfig<String>("XAgent.DisplayName");
            Description = Config.GetConfig<String>("XAgent.Description");
            Intervals = Config.GetConfig<String>("XAgent.Interval", Config.GetConfig<String>("Interval", "60"));
            MaxActive = Config.GetConfig<Int32>("XAgent.MaxActive");
            MaxMemory = Config.GetConfig<Int32>("XAgent.MaxMemory");
            MaxThread = Config.GetConfig<Int32>("XAgent.MaxThread");
            AutoRestart = Config.GetConfig<Int32>("XAgent.AutoRestart");
            WatchDog = Config.GetConfig<String>("XAgent.WatchDog");
        }
        #endregion
    }
}