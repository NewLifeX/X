#if !__CORE__
using System;
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
                //filename = Path.GetFileName(filename);
                filename = filename.Replace(".vshost.", ".");

                return filename;
            }
        }

        /// <summary>启动、停止 服务</summary>
        /// <param name="service">服务对象</param>
        /// <param name="isstart"></param>
        public static void ControlService(this AgentServiceBase service, Boolean isstart = true)
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
        public static Boolean? IsInstalled(String serviceName) => IsServiceInstalled(serviceName);

        /// <summary>是否已启动</summary>
        public static Boolean? IsRunning(String serviceName) => IsServiceRunning(serviceName);

        /// <summary>是否已安装</summary>
        public static Boolean? IsServiceInstalled(String name)
        {
            try
            {
                // 取的时候就抛异常，是不知道是否安装的
                var control = ServiceController.GetServices().FirstOrDefault(e => e.ServiceName == name);
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
        }

        /// <summary>是否已启动</summary>
        public static Boolean? IsServiceRunning(String name)
        {
            try
            {
                var control = ServiceController.GetServices().FirstOrDefault(e => e.ServiceName == name);
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
#endif