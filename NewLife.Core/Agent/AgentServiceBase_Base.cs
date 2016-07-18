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
        public virtual String DisplayName { get { return ServiceName; } }

        /// <summary>描述</summary>
        public virtual String Description { get { return ServiceName + "服务"; } }

        /// <summary>线程数</summary>
        public virtual Int32 ThreadCount { get; set; }

        /// <summary>线程名</summary>
        public virtual String[] ThreadNames { get { return null; } }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        public AgentServiceBase()
        {
            // 指定默认服务名
            if (String.IsNullOrEmpty(ServiceName)) ServiceName = GetType().Name;

            ThreadCount = 1;
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
                        catch { }
                    }
                    if (_Instance == null) _Instance = last;
                }
                return _Instance;
            }
            set { _Instance = value; }
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