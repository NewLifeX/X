using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Agent
{
    /// <typeparam name="TService">服务类型</typeparam>
    public abstract class AgentServiceBase<TService> : AgentServiceBase
         where TService : AgentServiceBase<TService>, new()
    {
        /// <summary>服务主函数</summary>
        public static void ServiceMain() => new TService().Main();
    }

    /// <summary>服务程序基类</summary>
    public abstract class AgentServiceBase : ServiceBase
    {
        #region 属性
        /// <summary>显示名</summary>
        public virtual String DisplayName { get; set; }

        /// <summary>描述</summary>
        public virtual String Description { get; set; }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        public AgentServiceBase()
        {
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = false;
            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
            AutoLog = true;
        }
        #endregion

        #region 主函数
        /// <summary>服务主函数</summary>
        public void Main()
        {
            XTrace.UseConsole();

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

            set.SaveAsync();

            // 用配置覆盖
            service.ServiceName = set.ServiceName;
            service.DisplayName = set.DisplayName;
            service.Description = set.Description;

            var name = service.ServiceName;
            var args = Environment.GetCommandLineArgs();

            if (args.Length > 1)
            {
                #region 命令
                var cmd = args[1].ToLower();
                if (cmd == "-s")  //启动服务
                {

                    try
                    {
                        ServiceBase.Run(new[] { service });
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);
                    }
                }
                else if (cmd == "-i") //安装服务
                    service.Install(true);
                else if (cmd == "-u") //卸载服务
                    service.Install(false);
                else if (cmd == "-start") //启动服务
                    service.ControlService(true);
                else if (cmd == "-stop") //停止服务
                    service.ControlService(false);
                #endregion
            }
            else
            {
                Console.Title = service.DisplayName;

                #region 命令行
                // 输出状态
                service.ShowStatus();

                while (true)
                {
                    //输出菜单
                    service.ShowMenu();
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
                            service.ShowStatus();

                            break;
                        case '2':
                            if (ServiceHelper.IsInstalled(name) == true)
                                service.Install(false);
                            else
                                service.Install(true);
                            break;
                        case '3':
                            if (ServiceHelper.IsRunning(name) == true)
                                service.ControlService(false);
                            else
                                service.ControlService(true);
                            break;
                        case '5':
                            #region 循环调试
                            try
                            {
                                Console.WriteLine("正在循环调试……");
                                service.StartWork("循环开始");

                                Console.WriteLine("任意键结束循环调试！");
                                Console.ReadKey(true);

                                service.StopWork("循环停止");
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
                            if (service._Menus.TryGetValue(key.KeyChar, out var menu)) menu.Callback();
                            break;
                    }
                }
                #endregion
            }
        }

        /// <summary>显示状态</summary>
        protected virtual void ShowStatus()
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            var service = this;
            var name = service.ServiceName;

            if (name != service.DisplayName)
                Console.WriteLine("服务：{0}({1})", service.DisplayName, name);
            else
                Console.WriteLine("服务：{0}", name);
            Console.WriteLine("描述：{0}", service.Description);
            Console.Write("状态：");

            var install = ServiceHelper.IsInstalled(name);
            if (install == null)
                Console.WriteLine("未知");
            else if (install == false)
                Console.WriteLine("未安装");
            else
            {
                var run = ServiceHelper.IsRunning(name);
                if (run == null)
                    Console.WriteLine("未知");
                else if (run == false)
                    Console.WriteLine("未启动");
                else
                    Console.WriteLine("运行中");
            }

            var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
            Console.WriteLine();
            Console.WriteLine("{0}\t版本：{1}\t发布：{2:yyyy-MM-dd HH:mm:ss}", asm.Name, asm.FileVersion, asm.Compile);

            var asm2 = AssemblyX.Create(Assembly.GetEntryAssembly());
            if (asm2 != asm)
                Console.WriteLine("{0}\t版本：{1}\t发布：{2:yyyy-MM-dd HH:mm:ss}", asm2.Name, asm2.FileVersion, asm2.Compile);

            Console.ForegroundColor = color;
        }

        /// <summary>显示菜单</summary>
        protected virtual void ShowMenu()
        {
            var service = this;
            var name = service.ServiceName;

            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine();
            Console.WriteLine("1 显示状态");

            var install = ServiceHelper.IsInstalled(name);
            var run = ServiceHelper.IsRunning(name);
            if (install == true)
            {
                if (run == true)
                {
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

            if (run != true)
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

        private Dictionary<Char, Menu> _Menus = new Dictionary<Char, Menu>();
        /// <summary>添加菜单</summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="callbak"></param>
        public void AddMenu(Char key, String name, Action callbak)
        {
            if (!_Menus.ContainsKey(key))
            {
                _Menus.Add(key, new Menu { Key = key, Name = name, Callback = callbak });
            }
        }

        class Menu
        {
            public Char Key { get; set; }
            public String Name { get; set; }
            public Action Callback { get; set; }
        }
        #endregion

        #region 服务控制
        /// <summary>开始工作</summary>
        /// <param name="reason"></param>
        protected virtual void StartWork(String reason)
        {
            WriteLog("服务启动 {0}", reason);

            _Timer = new TimerX(DoCheck, null, 10_000, 10_000, "AM");
        }

        /// <summary>停止服务</summary>
        /// <param name="reason"></param>
        protected virtual void StopWork(String reason)
        {
            _Timer.TryDispose();

            WriteLog("服务停止 {0}", reason);
        }
        #endregion

        #region 服务维护
        private TimerX _Timer;

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

            var p = Process.GetCurrentProcess();
            var cur = p.WorkingSet64 + p.PrivateMemorySize64;
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
            WriteLog("重启服务！");

            // 在临时目录生成重启服务的批处理文件
            var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "重启.bat");
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
        /// <summary>服务启动事件</summary>
        /// <param name="args"></param>
        protected override void OnStart(String[] args) => StartWork(nameof(OnStart));

        /// <summary>服务停止事件</summary>
        protected override void OnStop() => StopWork(nameof(OnStop));

        /// <summary>在系统关闭时执行。 指定在系统关闭之前应该发生什么。</summary>
        protected override void OnShutdown() => StopWork(nameof(OnShutdown));

        /// <summary>在计算机的电源状态已发生更改时执行。 这适用于便携式计算机，当他们进入挂起模式，这不是系统关闭相同。</summary>
        /// <param name="powerStatus"></param>
        /// <returns></returns>
        protected override Boolean OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            WriteLog(nameof(OnPowerEvent) + " " + powerStatus);

            return true;
        }

        /// <summary>在终端服务器会话中接收的更改事件时执行</summary>
        /// <param name="changeDescription"></param>
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            WriteLog(nameof(OnSessionChange) + " SessionId={0} Reason={1}", changeDescription.SessionId, changeDescription.Reason);
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

            foreach (var item in ss)
            {
                // 注意：IsServiceRunning返回三种状态，null表示未知
                if (ServiceHelper.IsServiceRunning(item) == false)
                {
                    XTrace.WriteLine("发现服务{0}被关闭，准备启动！", item);

                    ServiceHelper.RunCmd("net start " + item, false, true);
                }
            }
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log != null && Log.Enable) Log.Info(format, args);
        }
        #endregion
    }
}