using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Windows.Forms;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;

namespace XAgent
{
    /// <summary>服务程序基类</summary>
    public abstract class AgentServiceBase : ServiceBase, IAgentService
    {
        #region 属性
        /// <summary>显示名</summary>
        public virtual String DisplayName { get { return ServiceName; } }

        /// <summary>描述</summary>
        public virtual String Description { get { return ServiceName + "服务"; } }

        /// <summary>线程数</summary>
        public virtual Int32 ThreadCount { get { return 1; } }

        /// <summary>线程名</summary>
        public virtual String[] ThreadNames { get { return null; } }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        public AgentServiceBase()
        {
            // 指定默认服务名
            if (String.IsNullOrEmpty(ServiceName)) ServiceName = this.GetType().Name;
        }
        #endregion

        #region 静态属性
        /// <summary></summary>
        internal protected static AgentServiceBase _Instance;
        /// <summary>服务实例。每个应用程序域只有一个服务实例</summary>
        public static AgentServiceBase Instance
        {
            get
            {
                // 如果用户代码直接访问当前类静态属性，就无法触发AgentServiceBase<TService>的类型构造函数，无法为Instance赋值，从而报错
                // 我们可以采用反射来进行处理
                if (_Instance == null)
                {
                    AgentServiceBase last = null;
                    foreach (var item in AssemblyX.FindAllPlugins(typeof(AgentServiceBase), true))
                    {
                        try
                        {
                            // 这里实例化一次，按理应该可以除非AgentServiceBase<TService>的类型构造函数了，如果还是没有赋值，则这里赋值
                            var obj = TypeX.CreateInstance(item);
                            if ((last == null || last is AgentService) && obj != null && obj is AgentServiceBase)
                            {
                                last = obj as AgentServiceBase;
                            }
                        }
                        catch { }
                    }
                    if (_Instance == null) _Instance = last;
                }
                return _Instance;
            }
            set { _Instance = value; }
        }
        #endregion

        #region 辅助函数及属性
        private static Int32[] _Intervals;
        /// <summary>间隔数组。默认60秒</summary>
        public static Int32[] Intervals
        {
            get
            {
                if (_Intervals != null) return _Intervals;

                _Intervals = Config.GetConfigSplit<Int32>("XAgent.Interval", null, Config.GetConfigSplit<Int32>("Interval", null, new Int32[] { 60 }));
                return _Intervals;
            }
            set { _Intervals = value; }
        }

        private static Int32? _MaxActive;
        /// <summary>最大活动时间。超过最大活动时间都还没有响应的线程将会被重启，防止线程执行时间过长。默认0，表示无限</summary>
        public static Int32 MaxActive
        {
            get
            {
                if (_MaxActive == null) _MaxActive = Config.GetConfig<Int32>("XAgent.MaxActive", Config.GetConfig<Int32>("MaxActive", 0));
                return _MaxActive.Value;
            }
            set { _MaxActive = value; }
        }

        private static Int32? _MaxMemory;
        /// <summary>最大占用内存。超过最大占用时，整个服务进程将会重启，以释放资源。默认0，表示无限</summary>
        public static Int32 MaxMemory
        {
            get
            {
                if (_MaxMemory == null) _MaxMemory = Config.GetConfig<Int32>("XAgent.MaxMemory", Config.GetConfig<Int32>("MaxMemory", 0));
                return _MaxMemory.Value;
            }
            set { _MaxMemory = value; }
        }

        private static Int32? _MaxThread;
        /// <summary>最大总线程数。超过最大占用时，整个服务进程将会重启，以释放资源。默认0，表示无限</summary>
        public static Int32 MaxThread
        {
            get
            {
                if (_MaxThread == null) _MaxThread = Config.GetConfig<Int32>("XAgent.MaxThread", Config.GetConfig<Int32>("MaxThread", 0));
                return _MaxThread.Value;
            }
            set { _MaxThread = value; }
        }

        private static Int32? _AutoRestart;
        /// <summary>自动重启时间，单位：分钟。到达自动重启时间时，整个服务进程将会重启，以释放资源。默认0，表示无限</summary>
        public static Int32 AutoRestart
        {
            get
            {
                if (_AutoRestart == null) _AutoRestart = Config.GetConfig<Int32>("XAgent.AutoRestart", Config.GetConfig<Int32>("AutoRestart", 0));
                return _AutoRestart.Value;
            }
            set { _AutoRestart = value; }
        }
        #endregion

        #region 日志
        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object[] args)
        {
            if (XTrace.Debug) XTrace.WriteLine(format, args);
        }

        /// <summary>写日志</summary>
        /// <param name="msg"></param>
        public static void WriteLine(String msg)
        {
            if (XTrace.Debug) XTrace.WriteLine(msg);
        }

        /// <summary>写日志</summary>
        /// <param name="msg"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("请改用WriteLine")]
        public static void WriteLog(String msg)
        {
            if (XTrace.Debug) XTrace.WriteLine(msg);
        }
        #endregion

        #region 运行UI
        internal static void RunUI()
        {
            FreeConsole();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool FreeConsole();
        #endregion
    }
}