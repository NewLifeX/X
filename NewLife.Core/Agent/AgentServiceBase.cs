#if !__CORE__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Reflection;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Agent
{
    /// <summary>服务程序基类</summary>
    /// <typeparam name="TService">服务类型</typeparam>
    public abstract class AgentServiceBase<TService> : AgentServiceBase, IAgentService
         where TService : AgentServiceBase<TService>, new()
    {
        #region 构造
        static AgentServiceBase()
        {
            XTrace.UseConsole();

            if (_Instance == null) _Instance = new TService();
        }

        /// <summary>实例化，读取配置</summary>
        public AgentServiceBase()
        {
            var set = Setting.Current;
        }
        #endregion

        #region 静态辅助函数
        /// <summary>服务主函数</summary>
        public static void ServiceMain()
        {
            //// 降低进程优先级，提升稳定性
            //Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            var service = Instance as TService;

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

            var Args = Environment.GetCommandLineArgs();

            if (Args.Length > 1)
            {
                service.Log = XTrace.Log;

                #region 命令
                var cmd = Args[1].ToLower();
                if (cmd == "-s")  //启动服务
                {
                    var ServicesToRun = new ServiceBase[] { service };

                    try
                    {
                        ServiceBase.Run(ServicesToRun);
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);
                    }
                    return;
                }
                else if (cmd == "-i") //安装服务
                {
                    service.Install(true);
                    return;
                }
                else if (cmd == "-u") //卸载服务
                {
                    service.Install(false);
                    return;
                }
                else if (cmd == "-start") //启动服务
                {
                    service.ControlService(true);
                    return;
                }
                else if (cmd == "-stop") //停止服务
                {
                    service.ControlService(false);
                    return;
                }
                //else if (cmd == "-run") //循环执行任务
                //{
                //    var service2 = new TService();
                //    service2.StartWork("-run");
                //    Console.ReadKey(true);
                //    return;
                //}
                //else if (cmd == "-step") //单步执行任务
                //{
                //    var service2 = new TService();
                //    for (var i = 0; i < service2.ThreadCount; i++)
                //    {
                //        service2.Work(i);
                //    }
                //    return;
                //}
                #endregion
            }
            else
            {
                Console.Title = service.DisplayName;

                #region 命令行
                service.Log = XTrace.Log;

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
                            if (service.IsInstalled() == true)
                                service.Install(false);
                            else
                                service.Install(true);
                            break;
                        case '3':
                            if (service.IsRunning() == true)
                                service.ControlService(false);
                            else
                                service.ControlService(true);
                            break;
                        case '4':
                            #region 单步调试
                            try
                            {
                                var count = Instance.ThreadCount;
                                var n = 0;
                                if (count > 1)
                                {
                                    Console.Write("请输入要调试的任务（任务数：{0}）：", count);
                                    var k = Console.ReadKey();
                                    Console.WriteLine();
                                    n = k.KeyChar - '0';
                                }

                                Console.WriteLine("正在单步调试……");
                                if (n < 0 || n > count - 1)
                                {
                                    for (var i = 0; i < count; i++)
                                    {
                                        service.Work(i);
                                    }
                                }
                                else
                                {
                                    service.Work(n);
                                }
                                Console.WriteLine("单步调试完成！");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                            #endregion
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

            var service = Instance as IAgentService;
            var name = service.ServiceName;

            if (name != service.DisplayName)
                Console.WriteLine("服务：{0}({1})", service.DisplayName, name);
            else
                Console.WriteLine("服务：{0}", name);
            Console.WriteLine("描述：{0}", service.Description);
            Console.Write("状态：");
            if (service.IsInstalled() == null)
                Console.WriteLine("未知");
            else if (service.IsInstalled() == false)
                Console.WriteLine("未安装");
            else
            {
                if (service.IsRunning() == null)
                    Console.WriteLine("未知");
                else if (service.IsRunning() == false)
                    Console.WriteLine("未启动");
                else
                    Console.WriteLine("运行中");
            }

            var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
            Console.WriteLine();
            Console.WriteLine("{0}\t版本：{1}\t发布：{2:yyyy-MM-dd HH:mm:ss}", asm.Name, asm.FileVersion, asm.Compile);
            //Console.WriteLine("文件：{0}", asm.FileVersion);
            //Console.WriteLine("发布：{0:yyyy-MM-dd HH:mm:ss}", asm.Compile);

            var asm2 = AssemblyX.Create(Assembly.GetEntryAssembly());
            if (asm2 != asm)
            {
                //Console.WriteLine();
                Console.WriteLine("{0}\t版本：{1}\t发布：{2:yyyy-MM-dd HH:mm:ss}", asm2.Name, asm2.FileVersion, asm2.Compile);
                //Console.WriteLine("发布：{0:yyyy-MM-dd HH:mm:ss}", asm2.Compile);
            }

            Console.ForegroundColor = color;
        }

        /// <summary>显示菜单</summary>
        protected virtual void ShowMenu()
        {
            var service = Instance as IAgentService;

            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine();
            Console.WriteLine("1 显示状态");

            if (service.IsInstalled() == true)
            {
                if (service.IsRunning() == true)
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

            if (service.IsRunning() != true)
            {
                Console.WriteLine("4 单步调试 -step");
                Console.WriteLine("5 循环调试 -run");
            }

            if (WatchDogs.Length > 0)
            {
                Console.WriteLine("7 看门狗保护服务 {0}", String.Join(",", WatchDogs));
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
        /// <summary>任务项</summary>
        public ServiceItem[] Items { get; private set; }

        /// <summary>任务调度器</summary>
        public JobSchedule Schedule { get; } = new JobSchedule();

        /// <summary>开始工作</summary>
        /// <param name="reason"></param>
        protected virtual void StartWork(String reason)
        {
            // 依赖服务检测
            this.PreStartWork();

            var tcount = ThreadCount;
            var sch = Schedule;
            var count = tcount + sch.Count;
            WriteLog("服务启动 共[{0:n0}]个工作线程 {1}", count, reason);

            try
            {
                var ss = Items = new ServiceItem[count];

                // 可以通过设置任务的时间间隔小于0来关闭指定任务
                var vs = Intervals;
                for (var i = 0; i < count; i++)
                {
                    var time = vs[0];
                    // 使用专用的时间间隔
                    if (i < vs.Length) time = vs[i];

                    var si = ss[i] = new ServiceItem(i, null, time);
                    if (i < tcount)
                    {
                        si.Callback = Work;
                    }
                    else
                    {
                        var job = sch.Jobs[i - tcount];
                        if (job is JobBase jb) jb.Log = Log;
                        si.Job = job;
                    }

                    //StartWork(i);
                    ss[i].Start(reason);
                }

                // 启动服务管理线程
                StartManagerThread();

                //// 显示用户界面交互窗体
                //Interactive.ShowForm();
            }
            catch (Exception ex)
            {
                //WriteLog(ex.ToString());
                Log?.Error(ex.GetTrue()?.ToString());
            }
        }

        /// <summary>核心工作方法。调度线程会定期调用该方法</summary>
        /// <param name="index">线程序号</param>
        /// <returns>是否立即开始下一步工作。某些任务能达到满负荷，线程可以不做等待</returns>
        public virtual Boolean Work(Int32 index)
        {
            //return false;
            throw new NotImplementedException("工作任务需要重载 Boolean Work(Int32 index)");
        }

        /// <summary>停止服务</summary>
        /// <param name="reason"></param>
        protected virtual void StopWork(String reason)
        {
            WriteLog("服务停止");

            // 停止服务管理线程
            StopManagerThread();

            var ss = Items;
            if (ss != null)
            {
                //// 先停止各个任务，然后才停止线程
                //foreach (var item in ss)
                //{
                //    item.Active = false;
                //    item.Event?.Set();
                //}

                // 等待各个工作线程退出
                var set = Setting.Current;
                var ts = new List<Task>();
                foreach (var item in ss)
                {
                    ts.Add(TaskEx.Run(() => item.Stop(reason)));
                }
                Task.WaitAll(ts.ToArray(), set.WaitForExit);
            }

            //Interactive.Hide();
        }

        /// <summary>唤醒指定任务马上开始处理任务</summary>
        /// <param name="index"></param>
        public virtual void Wake(Int32 index)
        {
            var ss = Items;
            if (index < 0 || index >= ss.Length) return;

            ss[index].Event?.Set();
        }
        #endregion

        #region 服务维护线程
        /// <summary>服务管理线程</summary>
        private Thread ManagerThread;

        /// <summary>开始服务管理线程</summary>
        public void StartManagerThread()
        {
            // 管理线程具有最高优先级
            var mt = ManagerThread = new Thread(ManagerThreadWaper)
            {
                //ManagerThread.Name = "XAgent_Manager";
                Name = "AM",
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
            mt.Start();
        }

        /// <summary>停止服务管理线程</summary>
        public void StopManagerThread()
        {
            var mt = ManagerThread;
            if (mt == null) return;
            if (mt.IsAlive)
            {
                try
                {
                    mt.Abort();
                }
                catch (Exception ex)
                {
                    //WriteLine(ex.ToString());
                    Log?.Error(ex.GetTrue()?.ToString());
                }
            }
        }

        /// <summary>服务管理线程封装</summary>
        /// <param name="data"></param>
        protected virtual void ManagerThreadWaper(Object data)
        {
            // 暂停一会，等待各个任务线程完全启动
            Thread.Sleep(2 * 1000);
            while (true)
            {
                try
                {
                    CheckActive();

                    // 如果某一项检查需要重启服务，则返回true，这里跳出循环，等待服务重启
                    if (CheckMemory()) break;
                    if (CheckThread()) break;
                    if (CheckHandle()) break;
                    if (CheckAutoRestart()) break;

                    // 检查看门狗
                    CheckWatchDog();

                    Thread.Sleep(10 * 1000);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    //WriteLine(ex.ToString());
                    Log?.Error(ex.GetTrue()?.ToString());
                }
            }
        }

        /// <summary>检查是否有工作线程死亡</summary>
        protected virtual void CheckActive()
        {
            var ss = Items;
            if (ss == null || ss.Length < 1) return;

            // 检查已经停止了的工作线程
            foreach (var item in ss)
            {
                item.CheckActive();
            }
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
            if (cur > max)
            {
                WriteLog("当前进程占用内存 {0:n0}M，超过阀值 {1:n0}M，准备重新启动！", cur, max);

                Restart("MaxMemory");

                return true;
            }

            return false;
        }

        /// <summary>检查服务进程的总线程数是否超标</summary>
        /// <returns></returns>
        protected virtual Boolean CheckThread()
        {
            var max = Setting.Current.MaxThread;
            if (max <= 0) return false;

            var p = Process.GetCurrentProcess();
            if (p.Threads.Count > max)
            {
                WriteLog("当前进程总线程 {0:n0}个，超过阀值 {1:n0}个，准备重新启动！", p.Threads.Count, max);

                Restart("MaxThread");

                return true;
            }

            return false;
        }

        /// <summary>检查服务进程的句柄数是否超标</summary>
        /// <returns></returns>
        protected virtual Boolean CheckHandle()
        {
            var max = Setting.Current.MaxHandle;
            if (max <= 0) return false;

            var p = Process.GetCurrentProcess();
            if (p.HandleCount > max)
            {
                WriteLog("当前进程句柄 {0:n0}个，超过阀值 {1:n0}个，准备重新启动！", p.HandleCount, max);

                Restart("MaxHandle");

                return true;
            }

            return false;
        }

        /// <summary>服务开始时间</summary>
        private DateTime Start = DateTime.Now;

        /// <summary>检查自动重启</summary>
        /// <returns></returns>
        protected virtual Boolean CheckAutoRestart()
        {
            var auto = Setting.Current.AutoRestart;
            if (auto <= 0) return false;

            var ts = DateTime.Now - Start;
            if (ts.TotalMinutes > auto)
            {
                WriteLog("服务已运行 {0:n0}分钟，达到预设重启时间（{1:n0}分钟），准备重启！", ts.TotalMinutes, auto);

                Restart("AutoRestart");

                return true;
            }

            return false;
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

            // 准备重启服务，等待所有工作线程返回
            foreach (var item in Items)
            {
                item.Stop(reason);
            }

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
        protected override void OnStart(String[] args)
        {
            EventLog.WriteEntry("服务启动 共[{0:n0}]个工作线程".F(ThreadCount), EventLogEntryType.Information);

            StartWork("服务启动");
        }

        /// <summary>服务停止事件</summary>
        protected override void OnStop()
        {
            StopWork("服务停止");
        }

        /// <summary>暂停命令发送到服务的服务控制管理器 (SCM) 时执行。 指定当服务就会暂停时要执行的操作。</summary>
        protected override void OnPause()
        {
            WriteLog(nameof(OnPause));

            foreach (var item in Items)
            {
                item.Stop(nameof(OnPause));
            }
        }

        /// <summary>继续命令发送到服务的服务控制管理器 (SCM) 运行。 指定当某个服务后继续正常工作正在暂停时要执行的操作。</summary>
        protected override void OnContinue()
        {
            WriteLog(nameof(OnContinue));

            foreach (var item in Items)
            {
                item.Start(nameof(OnContinue));
            }
        }

        /// <summary>在系统关闭时执行。 指定在系统关闭之前应该发生什么。</summary>
        protected override void OnShutdown()
        {
            WriteLog(nameof(OnShutdown));

            StopWork(nameof(OnShutdown));
        }

        /// <summary>在计算机的电源状态已发生更改时执行。 这适用于便携式计算机，当他们进入挂起模式，这不是系统关闭相同。</summary>
        /// <param name="powerStatus"></param>
        /// <returns></returns>
        protected override Boolean OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            WriteLog(nameof(OnPowerEvent) + " " + powerStatus);

            switch (powerStatus)
            {
                case PowerBroadcastStatus.BatteryLow:
                    break;
                case PowerBroadcastStatus.OemEvent:
                    break;
                case PowerBroadcastStatus.PowerStatusChange:
                    break;
                case PowerBroadcastStatus.QuerySuspend:
                    break;
                case PowerBroadcastStatus.QuerySuspendFailed:
                    break;
                case PowerBroadcastStatus.ResumeAutomatic:
                    break;
                case PowerBroadcastStatus.ResumeCritical:
                    break;
                case PowerBroadcastStatus.ResumeSuspend:
                    break;
                case PowerBroadcastStatus.Suspend:
                    break;
                default:
                    break;
            }

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
        private static String[] _WatchDogs;
        /// <summary>看门狗要保护的服务</summary>
        public static String[] WatchDogs
        {
            get
            {
                if (_WatchDogs == null) _WatchDogs = Setting.Current.WatchDog.Split(",", ";");

                return _WatchDogs;
            }
        }

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
    }
}
#endif