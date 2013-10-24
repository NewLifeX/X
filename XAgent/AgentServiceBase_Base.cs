using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Windows.Forms;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;

namespace XAgent
{
    /// <summary>服务程序基类</summary>
    public abstract class AgentServiceBase : ServiceBase
    {
        #region 属性
        /// <summary>显示名</summary>
        public virtual String DisplayName { get { return AgentServiceName; } }

        /// <summary>描述</summary>
        public virtual String Description { get { return AgentServiceName + "服务"; } }

        /// <summary>线程数</summary>
        public virtual Int32 ThreadCount { get { return 1; } }

        /// <summary>线程名</summary>
        public virtual String[] ThreadNames { get { return null; } }
        #endregion

        #region 静态属性
        /// <summary>服务名</summary>
        public static String AgentServiceName { get { return Instance.ServiceName; } }

        /// <summary>显示名</summary>
        public static String AgentDisplayName { get { return Config.GetConfig<String>("XAgent.DisplayName", Instance.DisplayName); } }

        /// <summary>服务描述</summary>
        public static String AgentDescription { get { return Config.GetConfig<String>("XAgent.Description", Instance.Description); } }

        /// <summary>Exe程序名</summary>
        internal static String ExeName
        {
            get
            {
                //String filename= AppDomain.CurrentDomain.FriendlyName.Replace(".vshost.", ".");
                //if (filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return filename;

                //filename = Assembly.GetExecutingAssembly().Location;
                //return filename;
                //String filename = Assembly.GetEntryAssembly().Location;
                Process p = Process.GetCurrentProcess();
                String filename = p.MainModule.FileName;
                filename = Path.GetFileName(filename);
                filename = filename.Replace(".vshost.", ".");
                return filename;
            }
        }

        private static AgentServiceBase _Instance;
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

        /// <summary>是否已安装</summary>
        public static Boolean? IsInstalled { get { return IsServiceInstalled(AgentServiceName); } }

        /// <summary>是否已启动</summary>
        public static Boolean? IsRunning { get { return IsServiceRunning(AgentServiceName); } }
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
                //if (_Intervals == null) _Intervals = new Int32[] { 60 };

                //String str = Config.GetConfig<String>("XAgent.Interval", Config.GetConfig<String>("Interval"));
                //if (String.IsNullOrEmpty(str))
                //{
                //    _Intervals = new Int32[] { 60 };
                //}
                //else
                //{
                //    String[] ss = str.Split(new Char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                //    List<Int32> list = new List<Int32>(ss.Length);
                //    foreach (String item in ss)
                //    {
                //        str = item.Trim();
                //        if (String.IsNullOrEmpty(str)) continue;

                //        Int32 result = 60;
                //        if (!Int32.TryParse(str, out result)) result = 60;
                //        if (result <= 0) result = 60;
                //        list.Add(result);
                //    }
                //    _Intervals = list.ToArray();
                //}
                return _Intervals;
            }
            set { _Intervals = value; }
        }

        private static Int32? _MaxActive;
        /// <summary>最大活动时间。超过最大活动时间都还没有响应的线程将会被重启，防止线程执行时间过长。默认0秒，表示无限</summary>
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
        /// <summary>最大占用内存。超过最大占用时，整个服务进程将会重启，以释放资源。默认0秒，表示无限</summary>
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
        /// <summary>最大总线程数。超过最大占用时，整个服务进程将会重启，以释放资源。默认0秒，表示无限</summary>
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
        /// <summary>自动重启时间，单位：分钟。到达自动重启时间时，整个服务进程将会重启，以释放资源。默认0秒，表示无限</summary>
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

        #region 服务安装和启动
        /// <summary>安装、卸载 服务</summary>
        /// <param name="isinstall">是否安装</param>
        public static void Install(Boolean isinstall)
        {
            var name = AgentServiceName.Replace(" ", "_");
            WriteLine("在win7/win2008及更高系统中，可能需要管理员权限执行才能安装/卸载服务。");
            if (isinstall)
            {
                RunSC("create " + name + " BinPath= \"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExeName) + " -s\" start= auto DisplayName= \"" + AgentDisplayName + "\"");
                RunSC("description " + name + " \"" + AgentDescription + "\"");
            }
            else
            {
                ControlService(false);

                RunSC("Delete " + name);
            }
        }

        /// <summary>启动、停止 服务</summary>
        /// <param name="isstart"></param>
        public static void ControlService(Boolean isstart)
        {
            if (isstart)
                RunCmd("net start " + AgentServiceName, false, true);
            else
                RunCmd("net stop " + AgentServiceName, false, true);
        }

        /// <summary>执行一个命令</summary>
        /// <param name="cmd"></param>
        /// <param name="showWindow"></param>
        /// <param name="waitForExit"></param>
        protected static void RunCmd(String cmd, Boolean showWindow, Boolean waitForExit)
        {
            WriteLine("RunCmd " + cmd);

            Process p = new Process();
            ProcessStartInfo si = new ProcessStartInfo();
            String path = Environment.SystemDirectory;
            path = Path.Combine(path, @"cmd.exe");
            si.FileName = path;
            if (!cmd.StartsWith(@"/")) cmd = @"/c " + cmd;
            si.Arguments = cmd;
            si.UseShellExecute = false;
            si.CreateNoWindow = !showWindow;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            p.StartInfo = si;

            p.Start();
            if (waitForExit)
            {
                p.WaitForExit();

                String str = p.StandardOutput.ReadToEnd();
                if (!String.IsNullOrEmpty(str)) WriteLine(str.Trim(new Char[] { '\r', '\n', '\t' }).Trim());
                str = p.StandardError.ReadToEnd();
                if (!String.IsNullOrEmpty(str)) WriteLine(str.Trim(new Char[] { '\r', '\n', '\t' }).Trim());
            }
        }

        /// <summary>执行SC命令</summary>
        /// <param name="cmd"></param>
        protected static void RunSC(String cmd)
        {
            String path = Environment.SystemDirectory;
            path = Path.Combine(path, @"sc.exe");
            if (!File.Exists(path)) path = "sc.exe";
            if (!File.Exists(path)) return;
            RunCmd(path + " " + cmd, false, true);
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

        #region 辅助
        /// <summary>取得服务</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static ServiceController GetService(String name)
        {
            var list = new List<ServiceController>(ServiceController.GetServices());
            if (list == null || list.Count < 1) return null;

            //return list.Find(delegate(ServiceController item) { return item.ServiceName == name; });
            foreach (ServiceController item in list)
            {
                if (item.ServiceName == name) return item;
            }
            return null;
        }

        /// <summary>是否已安装</summary>
        public static Boolean? IsServiceInstalled(String name)
        {
            ServiceController control = null;
            try
            {
                // 取的时候就抛异常，是不知道是否安装的
                control = GetService(name);
                if (control == null) return false;
                try
                {
                    //尝试访问一下才知道是否已安装
                    Boolean b = control.CanShutdown;
                    return true;
                }
                catch { return false; }
            }
            catch { return null; }
            finally { if (control != null)control.Dispose(); }
        }

        /// <summary>是否已启动</summary>
        public static Boolean? IsServiceRunning(String name)
        {
            ServiceController control = null;
            try
            {
                control = GetService(name);
                if (control == null) return false;
                try
                {
                    //尝试访问一下才知道是否已安装
                    Boolean b = control.CanShutdown;
                }
                catch { return false; }

                control.Refresh();
                if (control.Status == ServiceControllerStatus.Running) return true;
                if (control.Status == ServiceControllerStatus.Stopped) return false;
                return null;
            }
            catch { return null; }
            finally { if (control != null)control.Dispose(); }
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