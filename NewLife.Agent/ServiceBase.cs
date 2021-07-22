using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Agent
{
    /// <summary>服务程序基类</summary>
    public abstract class ServiceBase : IHostedService
    {
        #region 属性
        /// <summary>主机</summary>
        public IHost Host { get; set; }

        /// <summary>服务名</summary>
        public String ServiceName { get; set; }

        /// <summary>显示名</summary>
        public String DisplayName { get; set; }

        /// <summary>描述</summary>
        public String Description { get; set; }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        public ServiceBase()
        {
        }
        #endregion

        #region 主函数
        /// <summary>服务主函数</summary>
        public void Main()
        {
            MachineInfo.RegisterAsync();

            // 以服务方式启动时，不写控制台日志
            var args = Environment.GetCommandLineArgs();
            if (args == null || args.Length <= 1 || args[1].ToLower() != "-s")
                XTrace.UseConsole();

            if (Host == null)
            {
                if (Runtime.Windows)
                    Host = new WindowsService();
                else
                    Host = new Systemd();
            }

            var service = this;
            service.Log = XTrace.Log;

            // 初始化配置
            var set = Setting.Current;
            if (set.ServiceName.IsNullOrEmpty()) set.ServiceName = service.ServiceName;
            if (set.DisplayName.IsNullOrEmpty()) set.DisplayName = service.DisplayName;
            if (set.Description.IsNullOrEmpty()) set.Description = service.Description;

            // 从程序集构造配置
            var asm = AssemblyX.Entry;
            if (set.ServiceName.IsNullOrEmpty()) set.ServiceName = asm.Name;
            if (set.DisplayName.IsNullOrEmpty()) set.DisplayName = asm.Title;
            if (set.Description.IsNullOrEmpty()) set.Description = asm.Description;

            set.Save();

            // 用配置覆盖
            service.ServiceName = set.ServiceName;
            service.DisplayName = set.DisplayName;
            service.Description = set.Description;

            if (args.Length > 1)
            {
                #region 命令
                var cmd = args[1].ToLower();
                switch (cmd)
                {
                    case "-s":
                        Host.Run(this);
                        break;
                    case "-i":
                        Install();
                        break;
                    case "-u":
                        Host.Remove(ServiceName);
                        break;
                    case "-start":
                        Host.Start(ServiceName);
                        break;
                    case "-stop":
                        Host.Stop(ServiceName);
                        break;
                }
                #endregion
            }
            else
            {
                Console.Title = service.DisplayName;

                // 输出状态，菜单循环
                service.ShowStatus();
                service.ProcessMenu();
            }
        }

        /// <summary>显示状态</summary>
        protected virtual void ShowStatus()
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            var name = ServiceName;
            if (name != DisplayName)
                Console.WriteLine("服务：{0}({1})", DisplayName, name);
            else
                Console.WriteLine("服务：{0}", name);
            Console.WriteLine("描述：{0}", Description);
            Console.Write("状态：{0} ", Host.GetType().Name);

            if (!Host.IsInstalled(name))
                Console.WriteLine("未安装");
            else if (Host.IsRunning(name))
                Console.WriteLine("运行中");
            else
                Console.WriteLine("未启动");

            var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
            Console.WriteLine();
            Console.WriteLine("{0}\t版本：{1}\t发布：{2:yyyy-MM-dd HH:mm:ss}", asm.Name, asm.FileVersion, asm.Compile);

            var asm2 = AssemblyX.Create(Assembly.GetEntryAssembly());
            if (asm2 != asm)
                Console.WriteLine("{0}\t版本：{1}\t发布：{2:yyyy-MM-dd HH:mm:ss}", asm2.Name, asm2.FileVersion, asm2.Compile);

            Console.ForegroundColor = color;
        }

        /// <summary>处理菜单</summary>
        protected virtual void ProcessMenu()
        {
            var service = this;
            var name = ServiceName;
            while (true)
            {
                //输出菜单
                ShowMenu();
                Console.Write("请选择操作（-x是命令行参数）：");

                //读取命令
                var key = Console.ReadKey();
                if (key.KeyChar == '0') break;
                Console.WriteLine();
                Console.WriteLine();

                switch (key.KeyChar)
                {
                    case '1':
                        //输出状态
                        ShowStatus();

                        break;
                    case '2':
                        if (Host.IsInstalled(name))
                            Host.Remove(name);
                        else
                            Install();
                        break;
                    case '3':
                        if (Host.IsRunning(name))
                            Host.Stop(name);
                        else
                            Host.Start(name);
                        break;
                    case '5':
                        #region 循环调试
                        try
                        {
                            Console.WriteLine("正在循环调试……");
                            StartWork("循环开始");

                            Console.WriteLine("任意键结束循环调试！");
                            Console.ReadKey(true);

                            StopWork("循环停止");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                        #endregion
                        break;
                    case '7':
                        if (WatchDogs.Length > 0) CheckWatchDog();
                        break;
                    default:
                        // 自定义菜单
                        if (_Menus.TryGetValue(key.KeyChar, out var menu)) menu.Callback();
                        break;
                }
            }
        }

        /// <summary>显示菜单</summary>
        protected virtual void ShowMenu()
        {
            var name = ServiceName;

            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine();
            Console.WriteLine("1 显示状态");

            var run = false;
            if (Host.IsInstalled(name))
            {
                if (Host.IsRunning(name))
                {
                    run = true;
                    Console.WriteLine("3 停止服务 -stop");
                }
                else
                {
                    Console.WriteLine("2 卸载服务 -u");

                    Console.WriteLine("3 启动服务 -start");
                }
            }
            else
            {
                Console.WriteLine("2 安装服务 -i");
            }

            if (!run)
            {
                Console.WriteLine("5 循环调试 -run");
            }

            var dogs = WatchDogs;
            if (dogs.Length > 0)
            {
                Console.WriteLine("7 看门狗保护服务 {0}", dogs.Join());
            }

            if (_Menus.Count > 0)
            {
                foreach (var item in _Menus)
                {
                    Console.WriteLine("{0} {1}", item.Key, item.Value.Name);
                }
            }

            Console.WriteLine("0 退出");

            Console.ForegroundColor = color;
        }

        private readonly Dictionary<Char, Menu> _Menus = new Dictionary<Char, Menu>();
        /// <summary>添加菜单</summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="callbak"></param>
        public void AddMenu(Char key, String name, Action callbak)
        {
            if (!_Menus.ContainsKey(key))
            {
                _Menus.Add(key, new Menu(key, name, callbak));
            }
        }

        class Menu
        {
            public Char Key { get; set; }
            public String Name { get; set; }
            public Action Callback { get; set; }

            public Menu(Char key, String name, Action callback)
            {
                Key = key;
                Name = name;
                Callback = callback;
            }
        }
        #endregion

        #region 服务控制
        /// <summary>开始工作</summary>
        /// <param name="reason"></param>
        protected virtual void StartWork(String reason)
        {
            WriteLog("服务启动 {0}", reason);

            if (_Timer == null) AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            _Timer = new TimerX(DoCheck, null, 10_000, 10_000, "AM") { Async = true };
        }

        private void OnProcessExit(Object sender, EventArgs e)
        {
            StopWork("ProcessExit");
            Environment.ExitCode = 0;
        }

        /// <summary>停止服务</summary>
        /// <param name="reason"></param>
        protected virtual void StopWork(String reason)
        {
            if (_Timer != null) AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            _Timer.TryDispose();
            _Timer = null;

            WriteLog("服务停止 {0}", reason);
        }

        private void Install()
        {
            var exe = GetExeName();

            // 兼容dotnet
            var args = Environment.GetCommandLineArgs();
            if (args.Length >= 1 && Path.GetFileName(exe).EqualIgnoreCase("dotnet", "dotnet.exe"))
                exe += " " + args[0].GetFullPath();
            //else
            //    exe = exe.GetFullPath();

            var bin = $"{exe} -s";
            //RunSC($"create {name} BinPath= \"{bin}\" start= auto DisplayName= \"{svc.DisplayName}\"");

            Host.Install(ServiceName, DisplayName, bin, Description);
        }

        /// <summary>Exe程序名</summary>
        static String GetExeName()
        {
            var p = Process.GetCurrentProcess();
            var filename = p.MainModule.FileName;
            //filename = Path.GetFileName(filename);
            filename = filename.Replace(".vshost.", ".");

            return filename;
        }
        #endregion

        #region 服务维护
        private TimerX? _Timer;

        /// <summary>服务管理线程封装</summary>
        /// <param name="data"></param>
        protected virtual void DoCheck(Object data)
        {
            // 如果某一项检查需要重启服务，则返回true，这里跳出循环，等待服务重启
            if (CheckMemory()) return;
            if (CheckThread()) return;
            if (CheckHandle()) return;
            if (CheckAutoRestart()) return;

            // 检查看门狗
            CheckWatchDog();
        }

        /// <summary>检查内存是否超标</summary>
        /// <returns>是否超标重启</returns>
        protected virtual Boolean CheckMemory()
        {
            var max = Setting.Current.MaxMemory;
            if (max <= 0) return false;

            var cur = GC.GetTotalMemory(false);
            cur = cur / 1024 / 1024;
            if (cur < max) return false;

            // 执行一次GC回收
#if NET4
            GC.Collect(2, GCCollectionMode.Forced);
#else
            GC.Collect(2, GCCollectionMode.Forced, false);
#endif

            // 再次判断内存
            cur = GC.GetTotalMemory(true);
            cur = cur / 1024 / 1024;
            if (cur < max) return false;

            WriteLog("当前进程占用内存 {0:n0}M，超过阀值 {1:n0}M，准备重新启动！", cur, max);

            Restart("MaxMemory");

            return true;
        }

        /// <summary>检查服务进程的总线程数是否超标</summary>
        /// <returns></returns>
        protected virtual Boolean CheckThread()
        {
            var max = Setting.Current.MaxThread;
            if (max <= 0) return false;

            var p = Process.GetCurrentProcess();
            if (p.Threads.Count < max) return false;

            WriteLog("当前进程总线程 {0:n0}个，超过阀值 {1:n0}个，准备重新启动！", p.Threads.Count, max);

            Restart("MaxThread");

            return true;
        }

        /// <summary>检查服务进程的句柄数是否超标</summary>
        /// <returns></returns>
        protected virtual Boolean CheckHandle()
        {
            var max = Setting.Current.MaxHandle;
            if (max <= 0) return false;

            var p = Process.GetCurrentProcess();
            if (p.HandleCount < max) return false;

            WriteLog("当前进程句柄 {0:n0}个，超过阀值 {1:n0}个，准备重新启动！", p.HandleCount, max);

            Restart("MaxHandle");

            return true;
        }

        /// <summary>服务开始时间</summary>
        private readonly DateTime Start = DateTime.Now;

        /// <summary>检查自动重启</summary>
        /// <returns></returns>
        protected virtual Boolean CheckAutoRestart()
        {
            var auto = Setting.Current.AutoRestart;
            if (auto <= 0) return false;

            var ts = DateTime.Now - Start;
            if (ts.TotalMinutes < auto) return false;

            WriteLog("服务已运行 {0:n0}分钟，达到预设重启时间（{1:n0}分钟），准备重启！", ts.TotalMinutes, auto);

            Restart("AutoRestart");

            return true;
        }

        /// <summary>重启服务</summary>
        /// <param name="reason"></param>
        public void Restart(String reason)
        {
            WriteLog("重启服务！{0}", reason);

            // 在临时目录生成重启服务的批处理文件
            var filename = "重启.bat".GetFullPath();
            if (File.Exists(filename)) File.Delete(filename);

            File.AppendAllText(filename, "net stop " + ServiceName);
            File.AppendAllText(filename, Environment.NewLine);
            File.AppendAllText(filename, "ping 127.0.0.1 -n 5 > nul ");
            File.AppendAllText(filename, Environment.NewLine);
            File.AppendAllText(filename, "net start " + ServiceName);

            //执行重启服务的批处理
            //RunCmd(filename, false, false);
            var p = new Process();
            var si = new ProcessStartInfo
            {
                FileName = filename,
                UseShellExecute = true,
                CreateNoWindow = true
            };
            p.StartInfo = si;

            p.Start();

            //if (File.Exists(filename)) File.Delete(filename);
        }
        #endregion

        #region 服务高级功能
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            StartWork("StartAsync");
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            StopWork("StopAsync");
        }
        #endregion

        #region 看门狗
        /// <summary>看门狗要保护的服务</summary>
        public static String[] WatchDogs => Setting.Current.WatchDog.Split(",", ";");

        /// <summary>检查看门狗。</summary>
        /// <remarks>
        /// XAgent看门狗功能由管理线程完成，每分钟一次。
        /// 检查指定的任务是否已经停止，如果已经停止，则启动它。
        /// </remarks>
        public static void CheckWatchDog()
        {
            var ss = WatchDogs;
            if (ss == null || ss.Length < 1) return;

            //foreach (var item in ss)
            //{
            //    // 注意：IsServiceRunning返回三种状态，null表示未知
            //    if (ServiceHelper.IsServiceRunning(item) == false)
            //    {
            //        XTrace.WriteLine("发现服务{0}被关闭，准备启动！", item);

            //        ServiceHelper.RunCmd("net start " + item, false, true);
            //    }
            //}
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }
}