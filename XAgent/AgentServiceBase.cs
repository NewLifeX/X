using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using System.Collections.Generic;

namespace XAgent
{
    /// <summary>服务程序基类</summary>
    /// <typeparam name="TService">服务类型</typeparam>
    public abstract class AgentServiceBase<TService> : AgentServiceBase
         where TService : AgentServiceBase<TService>, new()
    {
        #region 构造
        static AgentServiceBase()
        {
            Instance = new TService();
        }
        #endregion

        #region 静态辅助函数
        /// <summary>
        /// 服务主函数
        /// </summary>
        public static void ServiceMain()
        {
            //提升进程优先级
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            // 根据配置修改服务名
            String name = Config.GetConfig<String>("XAgent.ServiceName");
            if (!String.IsNullOrEmpty(name)) Instance.ServiceName = name;

            //Instance.MakeBat();

            String[] Args = Environment.GetCommandLineArgs();

            if (Args.Length > 1)
            {
                #region 命令
                String cmd = Args[1].ToLower();
                if (cmd == "-s")  //启动服务
                {
                    ServiceBase[] ServicesToRun = new ServiceBase[] { Instance };

                    try
                    {
                        ServiceBase.Run(ServicesToRun);
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteLine(ex.ToString());
                    }
                    return;
                }
                else if (cmd == "-i") //安装服务
                {
                    Install(true);
                    return;
                }
                else if (cmd == "-u") //卸载服务
                {
                    Install(false);
                    return;
                }
                else if (cmd == "-start") //启动服务
                {
                    ControlService(true);
                    return;
                }
                else if (cmd == "-stop") //停止服务
                {
                    ControlService(false);
                    return;
                }
                else if (cmd == "-run") //循环执行任务
                {
                    TService service = new TService();
                    service.StartWork();
                    Console.ReadKey(true);
                    return;
                }
                else if (cmd == "-step") //单步执行任务
                {
                    TService service = new TService();
                    for (int i = 0; i < service.ThreadCount; i++)
                    {
                        service.Work(i);
                    }
                    return;
                }
                #endregion
            }
            else
            {
                Console.Title = AgentDisplayName;

                #region 命令行
                XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);

                TService serivce = Instance as TService;

                //输出状态
                serivce.ShowStatus();

                while (true)
                {
                    //输出菜单
                    serivce.ShowMenu();
                    Console.Write("请选择操作（-x是命令行参数）：");

                    //读取命令
                    ConsoleKeyInfo key = Console.ReadKey();
                    if (key.KeyChar == '0') break;
                    Console.WriteLine();
                    Console.WriteLine();

                    switch ((Int32)(key.KeyChar - '0'))
                    {
                        case 1:
                            //输出状态
                            serivce.ShowStatus();

                            break;
                        case 2:
                            if (IsInstalled == true)
                                Install(false);
                            else
                                Install(true);
                            break;
                        case 3:
                            if (IsRunning == true)
                                ControlService(false);
                            else
                                ControlService(true);
                            break;
                        case 4:
                            #region 单步调试
                            try
                            {
                                Int32 n = 0;
                                if (Instance.ThreadCount > 1)
                                {
                                    Console.Write("请输入要调试的任务（任务数：{0}）：", Instance.ThreadCount);
                                    ConsoleKeyInfo k = Console.ReadKey();
                                    Console.WriteLine();
                                    n = k.KeyChar - '0';
                                }

                                Console.WriteLine("正在单步调试……");
                                if (n < 0 || n > Instance.ThreadCount - 1)
                                {
                                    for (int i = 0; i < Instance.ThreadCount; i++)
                                    {
                                        serivce.Work(i);
                                    }
                                }
                                else
                                {
                                    serivce.Work(n);
                                }
                                Console.WriteLine("单步调试完成！");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                            #endregion
                            break;
                        case 5:
                            #region 循环调试
                            try
                            {
                                Console.WriteLine("正在循环调试……");
                                serivce.StartWork();

                                Console.WriteLine("任意键结束循环调试！");
                                Console.ReadKey(true);

                                serivce.StopWork();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                            #endregion
                            break;
                        case 6:
                            #region 附加服务
                            Console.WriteLine("正在附加服务调试……");
                            serivce.StartAttachServers();

                            Console.WriteLine("任意键结束附加服务调试！");
                            Console.ReadKey(true);

                            serivce.StopAttachServers();
                            #endregion
                            break;
                        default:
                            break;
                    }
                }
                #endregion
            }
        }

        /// <summary>
        /// 生成批处理
        /// </summary>
        protected virtual void MakeBat()
        {
            File.WriteAllText("安装.bat", ExeName + " -i");
            File.WriteAllText("卸载.bat", ExeName + " -u");
            File.WriteAllText("启动.bat", ExeName + " -start");
            File.WriteAllText("停止.bat", ExeName + " -stop");
        }

        static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        /// <summary>
        /// 显示状态
        /// </summary>
        protected virtual void ShowStatus()
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;

            if (AgentServiceName != AgentDisplayName)
                Console.WriteLine("服务：{0}({1})", AgentDisplayName, AgentServiceName);
            else
                Console.WriteLine("服务：{0}", AgentServiceName);
            Console.WriteLine("描述：{0}", AgentDescription);
            Console.Write("状态：");
            if (IsInstalled == null)
                Console.WriteLine("未知");
            else if (IsInstalled == false)
                Console.WriteLine("未安装");
            else
            {
                if (IsRunning == null)
                    Console.WriteLine("未知");
                else
                {
                    if (IsRunning == false)
                        Console.WriteLine("未启动");
                    else
                        Console.WriteLine("运行中");
                }
            }

            AssemblyX asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
            Console.WriteLine();
            Console.WriteLine("核心：{0}", asm.Version);
            //Console.WriteLine("文件：{0}", asm.FileVersion);
            Console.WriteLine("发布：{0:yyyy-MM-dd HH:mm:ss}", asm.Compile);

            asm = AssemblyX.Create(Assembly.GetEntryAssembly());
            Console.WriteLine();
            Console.WriteLine("程序：{0}", asm.Version);
            Console.WriteLine("发布：{0:yyyy-MM-dd HH:mm:ss}", asm.Compile);

            Console.ForegroundColor = color;
        }

        /// <summary>
        /// 显示菜单
        /// </summary>
        protected virtual void ShowMenu()
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine();
            Console.WriteLine("1 显示状态");

            if (IsInstalled == true)
            {
                if (IsRunning == true)
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

            if (IsRunning != true)
            {
                Console.WriteLine("4 单步调试 -step");
                Console.WriteLine("5 循环调试 -run");

                var dic = Config.GetConfigByPrefix("XAgent.AttachServer.");
                if (dic != null && dic.Count > 0)
                {
                    Console.WriteLine("6 附加服务调试");
                    foreach (var item in dic)
                    {
                        Console.WriteLine("{0,10} = {1}", item.Key, item.Value);
                    }
                }
            }

            Console.WriteLine("0 退出");

            Console.ForegroundColor = color;
        }
        #endregion

        #region 服务控制
        private Thread[] _Threads;
        /// <summary>线程组</summary>
        private Thread[] Threads { get { return _Threads ?? (_Threads = new Thread[ThreadCount]); } set { _Threads = value; } }

        private Dictionary<String, IServer> _AttachServers;
        /// <summary>附加服务</summary>
        public Dictionary<String, IServer> AttachServers { get { return _AttachServers ?? (_AttachServers = new Dictionary<string, IServer>()); } /*set { _AttachServers = value; }*/ }

        /// <summary>服务启动事件</summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            StartWork();

            // 处理附加服务
            try
            {
                StartAttachServers();
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
            }
        }

        /// <summary>服务停止事件</summary>
        protected override void OnStop()
        {
            StopWork();

            try
            {
                StopAttachServers();
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
            }
        }

        private void StartAttachServers()
        {
            var dic = Config.GetConfigByPrefix("XAgent.AttachServer.");
            if (dic != null && dic.Count > 0)
            {
                // 实例化
                foreach (var item in dic)
                {
                    if (!item.Key.IsNullOrWhiteSpace() && !item.Value.IsNullOrWhiteSpace())
                    {
                        WriteLine("");
                        WriteLine("正在加载：{0} = {1}", item.Key, item.Value);
                        var type = TypeX.GetType(item.Value, true);
                        if (type != null)
                        {
                            var service = TypeX.CreateInstance(type) as IServer;
                            if (service != null) AttachServers[item.Key] = service;
                        }
                    }
                }

                // 加载配置。【服务名.属性】的配置方式
                foreach (var item in AttachServers)
                {
                    if (item.Value != null)
                    {
                        var type = item.Value.GetType();
                        // 遍历所有属性，查找指定的设置项
                        foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty))
                        {
                            var name = String.Format("XAgent.{0}.{1}", item.Key, pi.Name);
                            Object value = null;
                            // 读取配置，并赋值
                            if (Config.TryGetConfig(name, pi.PropertyType, out value))
                            {
                                WriteLine("配置：{0} = {1}", name, value);
                                PropertyInfoX.Create(pi).SetValue(item.Value, value);
                            }
                        }
                    }
                }

                // 启动
                foreach (var item in AttachServers)
                {
                    if (item.Value != null)
                    {
                        WriteLine("启动：{0}", item.Key);
                        item.Value.Start();
                    }
                }
            }
        }

        private void StopAttachServers()
        {
            if (AttachServers != null)
            {
                foreach (var item in AttachServers.Values)
                {
                    if (item != null) item.Stop();
                }
            }
        }

        /// <summary>
        /// 销毁资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (AttachServers != null)
            {
                foreach (var item in AttachServers.Values)
                {
                    if (item != null && item is IDisposable) (item as IDisposable).Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// 开始循环工作
        /// </summary>
        public virtual void StartWork()
        {
            WriteLine("服务启动");

            try
            {
                for (int i = 0; i < ThreadCount; i++)
                {
                    StartWork(i);
                }

                // 启动服务管理线程
                StartManagerThread();

                //// 显示用户界面交互窗体
                //Interactive.ShowForm();
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 开始循环工作
        /// </summary>
        /// <param name="index">线程序号</param>
        public virtual void StartWork(Int32 index)
        {
            if (index < 0 || index >= ThreadCount) throw new ArgumentOutOfRangeException("index");

            // 可以通过设置任务的时间间隔小于0来关闭指定任务
            Int32 time = Intervals[0];
            // 使用专用的时间间隔
            if (index < Intervals.Length) time = Intervals[index];
            if (time < 0) return;

            Threads[index] = new Thread(workWaper);
            String name = "XAgent_" + index;
            if (ThreadNames != null && ThreadNames.Length > index && !String.IsNullOrEmpty(ThreadNames[index]))
                name = ThreadNames[index];
            Threads[index].Name = name;
            Threads[index].IsBackground = true;
            Threads[index].Priority = ThreadPriority.AboveNormal;
            Threads[index].Start(index);
        }

        /// <summary>
        /// 线程包装
        /// </summary>
        /// <param name="data">线程序号</param>
        private void workWaper(Object data)
        {
            Int32 index = (Int32)data;

            // 旧异常
            Exception oldEx = null;

            while (true)
            {
                Boolean isContinute = false;
                Active[index] = DateTime.Now;

                try
                {
                    isContinute = Work(index);

                    oldEx = null;
                }
                catch (ThreadAbortException) //线程被取消
                {
                    WriteLine("线程" + index + "被取消！");
                    break;
                }
                catch (ThreadInterruptedException) //线程中断错误
                {
                    WriteLine("线程" + index + "中断错误！");
                    break;
                }
                catch (Exception ex) //确保拦截了所有的异常，保证服务稳定运行
                {
                    // 避免同样的异常信息连续出现，造成日志膨胀
                    if (oldEx == null || oldEx.GetType() != ex.GetType() || oldEx.Message != ex.Message)
                    {
                        oldEx = ex;

                        WriteLine(ex.ToString());
                    }
                }
                Active[index] = DateTime.Now;

                //检查服务是否正在重启
                if (IsShutdowning)
                {
                    WriteLine("服务准备重启，" + Thread.CurrentThread.Name + "退出！");
                    break;
                }

                Int32 time = Intervals[0];
                //使用专用的时间间隔
                if (index < Intervals.Length) time = Intervals[index];

                //如果有数据库连接错误，则将等待间隔放大十倍
                //if (hasdberr) time *= 10;
                if (oldEx != null) time *= 10;

                if (oldEx == null && !isContinute) Thread.Sleep(time * 1000);
            }
        }

        /// <summary>
        /// 核心工作方法。调度线程会定期调用该方法
        /// </summary>
        /// <param name="index">线程序号</param>
        /// <returns>是否立即开始下一步工作。某些任务能达到满负荷，线程可以不做等待</returns>
        public abstract Boolean Work(Int32 index);

        /// <summary>
        /// 停止循环工作。
        /// 只能停止循环而已，如果已经有一批任务在处理，
        /// 则内部需要捕获ThreadAbortException异常，否则无法停止任务处理。
        /// </summary>
        public virtual void StopWork()
        {
            WriteLine("服务停止");

            //停止服务管理线程
            StopManagerThread();

            //if (threads != null && threads.IsAlive) threads.Abort();
            if (Threads != null)
            {
                foreach (Thread item in Threads)
                {
                    try
                    {
                        if (item != null && item.IsAlive) item.Abort();
                    }
                    catch (Exception ex)
                    {
                        WriteLine(ex.ToString());
                    }
                }
            }

            //Interactive.Hide();
        }

        /// <summary>
        /// 停止循环工作
        /// </summary>
        /// <param name="index">线程序号</param>
        public virtual void StopWork(Int32 index)
        {
            if (index < 0 || index >= ThreadCount) throw new ArgumentOutOfRangeException("index");

            Thread item = Threads[index];
            try
            {
                if (item != null && item.IsAlive) item.Abort();
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
            }
        }
        #endregion

        #region 服务维护线程
        /// <summary>
        /// 服务管理线程
        /// </summary>
        private Thread ManagerThread;

        /// <summary>
        /// 开始服务管理线程
        /// </summary>
        public void StartManagerThread()
        {
            ManagerThread = new Thread(ManagerThreadWaper);
            ManagerThread.Name = "XAgent_Manager";
            ManagerThread.IsBackground = true;
            ManagerThread.Priority = ThreadPriority.Highest;
            ManagerThread.Start();
        }

        /// <summary>
        /// 停止服务管理线程
        /// </summary>
        public void StopManagerThread()
        {
            if (ManagerThread == null) return;
            if (ManagerThread.IsAlive)
            {
                try
                {
                    ManagerThread.Abort();
                }
                catch (Exception ex)
                {
                    WriteLine(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 服务管理线程封装
        /// </summary>
        /// <param name="data"></param>
        protected virtual void ManagerThreadWaper(Object data)
        {
            while (true)
            {
                try
                {
                    CheckActive();

                    //如果某一项检查需要重启服务，则返回true，这里跳出循环，等待服务重启
                    if (CheckMemory()) break;
                    if (CheckThread()) break;
                    if (CheckAutoRestart()) break;

                    Thread.Sleep(60 * 1000);
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    WriteLine(ex.ToString());
                }
            }
        }

        private DateTime[] _Active;
        /// <summary>活动时间</summary>
        public DateTime[] Active
        {
            get
            {
                if (_Active == null)
                {
                    _Active = new DateTime[ThreadCount];
                    for (int i = 0; i < ThreadCount; i++)
                    {
                        _Active[i] = DateTime.Now;
                    }
                }
                return _Active;
            }
            set { _Active = value; }
        }

        /// <summary>
        /// 检查是否有工作线程死亡
        /// </summary>
        protected virtual void CheckActive()
        {
            if (Threads == null || Threads.Length < 1) return;

            //检查已经停止了的工作线程
            for (int i = 0; i < ThreadCount; i++)
            {
                if (Threads[i] != null && !Threads[i].IsAlive)
                {
                    WriteLine(Threads[i].Name + "处于停止状态，准备重新启动！");

                    StartWork(i);
                }
            }

            //是否检查最大活动时间
            if (MaxActive <= 0) return;

            for (int i = 0; i < ThreadCount; i++)
            {
                TimeSpan ts = DateTime.Now - Active[i];
                if (ts.TotalSeconds > MaxActive)
                {
                    WriteLine(Threads[i].Name + "已经" + ts.TotalSeconds + "秒没有活动了，准备重新启动！");

                    StopWork(i);
                    //等待线程结束
                    Threads[i].Join(5000);
                    StartWork(i);
                }
            }
        }

        /// <summary>
        /// 检查内存是否超标
        /// </summary>
        /// <returns>是否超标重启</returns>
        protected virtual Boolean CheckMemory()
        {
            if (MaxMemory <= 0) return false;

            Process p = Process.GetCurrentProcess();
            long cur = p.WorkingSet64 + p.PrivateMemorySize64;
            cur = cur / 1024 / 1024;
            if (cur > MaxMemory)
            {
                WriteLine("当前进程占用内存" + cur + "M，超过阀值" + MaxMemory + "M，准备重新启动！");

                RestartService();

                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查服务进程的总线程数是否超标
        /// </summary>
        /// <returns></returns>
        protected virtual Boolean CheckThread()
        {
            if (MaxThread <= 0) return false;

            Process p = Process.GetCurrentProcess();
            if (p.Threads.Count > MaxThread)
            {
                WriteLine("当前进程总线程" + p.Threads.Count + "个，超过阀值" + MaxThread + "个，准备重新启动！");

                RestartService();

                return true;
            }

            return false;
        }

        /// <summary>
        /// 服务开始时间
        /// </summary>
        private DateTime Start = DateTime.Now;

        /// <summary>
        /// 检查自动重启
        /// </summary>
        /// <returns></returns>
        protected virtual Boolean CheckAutoRestart()
        {
            if (AutoRestart <= 0) return false;

            TimeSpan ts = DateTime.Now - Start;
            if (ts.TotalMinutes > AutoRestart)
            {
                WriteLine("服务已运行" + ts.TotalMinutes + "分钟，达到预设重启时间（" + AutoRestart + "分钟），准备重启！");

                RestartService();

                return true;
            }

            return false;
        }

        /// <summary>
        /// 是否正在重启
        /// </summary>
        private Boolean IsShutdowning = false;

        /// <summary>
        /// 重启服务
        /// </summary>
        protected void RestartService()
        {
            WriteLine("重启服务！");

            //在临时目录生成重启服务的批处理文件
            String filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "重启.bat");
            if (File.Exists(filename)) File.Delete(filename);

            File.AppendAllText(filename, "net stop " + AgentServiceName);
            File.AppendAllText(filename, Environment.NewLine);
            File.AppendAllText(filename, "ping 127.0.0.1 -n 5 > nul ");
            File.AppendAllText(filename, Environment.NewLine);
            File.AppendAllText(filename, "net start " + AgentServiceName);

            //准备重启服务，等待所有工作线程返回
            IsShutdowning = true;
            for (int i = 0; i < 10; i++)
            {
                Boolean b = false;
                foreach (Thread item in Threads)
                {
                    if (item.IsAlive)
                    {
                        b = true;
                        break;
                    }
                }
                if (!b) break;
                Thread.Sleep(1000);
            }

            //执行重启服务的批处理
            //RunCmd(filename, false, false);
            Process p = new Process();
            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = filename;
            si.UseShellExecute = true;
            si.CreateNoWindow = true;
            p.StartInfo = si;

            p.Start();

            //if (File.Exists(filename)) File.Delete(filename);
        }
        #endregion
    }
}