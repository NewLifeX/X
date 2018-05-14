using System;
using System.ServiceProcess;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Agent
{
    /// <summary>服务程序基类</summary>
    public abstract class AgentServiceBase : ServiceBase
    {
        #region 属性
        /// <summary>显示名</summary>
        public virtual String DisplayName { get; set; }

        /// <summary>描述</summary>
        public virtual String Description { get; set; }

        /// <summary>线程数</summary>
        public virtual Int32 ThreadCount { get; set; } = 1;

        /// <summary>线程名</summary>
        public virtual String[] ThreadNames { get; set; }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        public AgentServiceBase()
        {
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = true;
            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
            AutoLog = true;
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
                            var obj = item.CreateInstance();
                            if ((last == null || last is AgentService) && obj != null && obj is AgentServiceBase)
                            {
                                last = obj as AgentServiceBase;
                            }
                        }
                        catch (Exception ex) { XTrace.WriteException(ex); }
                    }
                    if (_Instance == null) _Instance = last;
                }
                return _Instance;
            }
            set { _Instance = value; }
        }
        #endregion

        private static Int32[] _Intervals;
        /// <summary>间隔数组。默认60秒</summary>
        public static Int32[] Intervals
        {
            get
            {
                if (_Intervals != null) return _Intervals;

                //_Intervals = Config.GetConfigSplit<Int32>("XAgent.Interval", null, Config.GetConfigSplit<Int32>("Interval", null, new Int32[] { 60 }));
                _Intervals = Setting.Current.Intervals.SplitAsInt();
                return _Intervals;
            }
            set { _Intervals = value; }
        }

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [Obsolete("=>WriteLog")]
        public static void WriteLine(String format, params Object[] args)
        {
            var set = Setting.Current;
            if (set.Debug) XTrace.WriteLine(format, args);
        }

        ///// <summary>写日志</summary>
        ///// <param name="msg"></param>
        //public static void WriteLine(String msg)
        //{
        //    if (XTrace.Debug) XTrace.WriteLine(msg);
        //}
        #endregion

        #region 运行UI
        //internal static void RunUI()
        //{
        //    FreeConsole();

        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);
        //    Application.Run(new FrmMain());
        //}

        //[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        //internal static extern bool FreeConsole();
        #endregion
    }
}