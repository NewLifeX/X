using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using NewLife.Log;

namespace NewLife.Agent
{
    /// <summary>服务助手</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ServiceHelper
    {
        #region 服务安装和启动
        /// <summary>Exe程序名</summary>
        public static String ExeName
        {
            get
            {
                var p = Process.GetCurrentProcess();
                var filename = p.MainModule.FileName;
                filename = Path.GetFileName(filename);
                filename = filename.Replace(".vshost.", ".");
                return filename;
            }
        }

        /// <summary>安装、卸载 服务</summary>
        /// <param name="service">服务对象</param>
        /// <param name="isinstall">是否安装</param>
        public static void Install(this IAgentService service, Boolean isinstall = true)
        {
            var name = service.ServiceName;
            if (String.IsNullOrEmpty(name)) throw new Exception("未指定服务名！");

            if (name.Length < name.GetBytes().Length) throw new Exception("服务名不能是中文！");

            name = name.Replace(" ", "_");
            // win7及以上系统时才提示
            if (Environment.OSVersion.Version.Major >= 6) WriteLine("在win7/win2008及更高系统中，可能需要管理员权限执行才能安装/卸载服务。");
            if (isinstall)
            {
                RunSC("create " + name + " BinPath= \"" + ExeName.GetFullPath() + " -s\" start= auto DisplayName= \"" + service.DisplayName + "\"");
                if (!String.IsNullOrEmpty(service.Description)) RunSC("description " + name + " \"" + service.Description + "\"");
            }
            else
            {
                service.ControlService(false);

                RunSC("Delete " + name);
            }
        }

        /// <summary>启动、停止 服务</summary>
        /// <param name="service">服务对象</param>
        /// <param name="isstart"></param>
        public static void ControlService(this IAgentService service, Boolean isstart = true)
        {
            var name = service.ServiceName;
            if (String.IsNullOrEmpty(name)) throw new Exception("未指定服务名！");

            if (isstart)
                RunCmd("net start " + name, false, true);
            else
                RunCmd("net stop " + name, false, true);
        }

        /// <summary>执行一个命令</summary>
        /// <param name="cmd"></param>
        /// <param name="showWindow"></param>
        /// <param name="waitForExit"></param>
        internal static void RunCmd(String cmd, Boolean showWindow, Boolean waitForExit)
        {
            WriteLine("RunCmd " + cmd);

            var p = new Process();
            var si = new ProcessStartInfo();
            var path = Environment.SystemDirectory;
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

                var str = p.StandardOutput.ReadToEnd();
                if (!String.IsNullOrEmpty(str)) WriteLine(str.Trim(new Char[] { '\r', '\n', '\t' }).Trim());
                str = p.StandardError.ReadToEnd();
                if (!String.IsNullOrEmpty(str)) WriteLine(str.Trim(new Char[] { '\r', '\n', '\t' }).Trim());
            }
        }

        /// <summary>执行SC命令</summary>
        /// <param name="cmd"></param>
        internal static void RunSC(String cmd)
        {
            var path = Environment.SystemDirectory;
            path = Path.Combine(path, @"sc.exe");
            if (!File.Exists(path)) path = "sc.exe";
            if (!File.Exists(path)) return;
            RunCmd(path + " " + cmd, false, true);
        }
        #endregion

        #region 服务操作辅助函数
        /// <summary>是否已安装</summary>
        public static Boolean? IsInstalled(this IAgentService service) => IsServiceInstalled(service.ServiceName);

        /// <summary>是否已启动</summary>
        public static Boolean? IsRunning(this IAgentService service) => IsServiceRunning(service.ServiceName);

        /// <summary>取得服务</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ServiceController GetService(String name)
        {
            var list = new List<ServiceController>(ServiceController.GetServices());
            if (list == null || list.Count < 1) return null;

            //return list.Find(delegate(ServiceController item) { return item.ServiceName == name; });
            foreach (var item in list)
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
                    var b = control.CanShutdown;
                    return true;
                }
                catch { return false; }
            }
            catch { return null; }
            finally { if (control != null) control.Dispose(); }
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
                    var b = control.CanShutdown;
                }
                catch { return false; }

                control.Refresh();
                if (control.Status == ServiceControllerStatus.Running) return true;
                if (control.Status == ServiceControllerStatus.Stopped) return false;
                return null;
            }
            catch { return null; }
            finally { if (control != null) control.Dispose(); }
        }
        #endregion

        #region 服务依赖
        /// <summary>启动服务准备工作</summary>
        public static void PreStartWork(this IAgentService service)
        {
            var Services = ServiceController.GetServices();

            //首先检查是否有依赖服务
            // 1.服务本身的依赖
            ServiceController[] servicesDependedOn = null;
            var scApp = Services.FirstOrDefault(s => s.ServiceName == service.ServiceName);
            if (scApp != null)
            {
                servicesDependedOn = scApp.ServicesDependedOn;

                foreach (var sc in servicesDependedOn)
                {
                    try
                    {
                        sc.Start();
                    }
                    catch (Exception ex)
                    {
                        //依赖服务启动未成功
                        throw new Exception("依赖服务" + sc.ServiceName + "未启动成功", ex);
                    }
                }
            }
            // 2.配置文件的依赖
            //var scConfig = Config.GetConfigSplit("XAgent.ServicesDependedOn", ",", Config.GetConfigSplit<String>("ServicesDependedOn", ",", null));
            var scConfig = Setting.Current.ServicesDependedOn.Split(",");
            if (scConfig != null)
            {
                foreach (var item in scConfig)
                {
                    var sc = Services.FirstOrDefault(s => s.ServiceName == item);
                    if (sc != null)
                        sc.Start();
                    else
                        throw new Exception(String.Format("依赖服务{0}不存在", item));
                }
            }
        }
        #endregion

        #region 日志
        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object[] args) => XTrace.WriteLine(format, args);
        #endregion
    }
}