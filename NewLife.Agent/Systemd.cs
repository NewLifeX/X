using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NewLife.Log;

namespace NewLife.Agent
{
    /// <summary>Linux版进程守护</summary>
    public class Systemd : Host
    {
        private String _path;
        private IHostedService _service;

        /// <summary>实例化</summary>
        public Systemd()
        {
            var ps = new[] {
                "/etc/systemd/system",
                "/run/systemd/system",
                "/usr/lib/systemd/system",
                "/lib/systemd/system" };
            foreach (var p in ps)
            {
                if (Directory.Exists(p))
                {
                    _path = p;
                    break;
                }
            }
        }

        /// <summary>启动服务</summary>
        /// <param name="service"></param>
        public override void Run(IHostedService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            _service = service;

            var source = new CancellationTokenSource();
            service.StartAsync(source.Token);

            // 阻塞
            Thread.Sleep(-1);
        }

        /// <summary>服务是否已安装</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean IsInstalled(String serviceName)
        {
            var file = _path.CombinePath($"{serviceName}.service");

            return File.Exists(file);
        }

        /// <summary>服务是否已启动</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean IsRunning(String serviceName)
        {
            var file = _path.CombinePath($"{serviceName}.service");
            if (!File.Exists(file)) return false;

            var str = Execute("systemctl", $"status {serviceName}", false);
            if (!str.IsNullOrEmpty() && str.Contains("running")) return true;

            return false;
        }

        /// <summary>安装服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <param name="displayName">显示名</param>
        /// <param name="binPath">文件路径</param>
        /// <param name="description">描述信息</param>
        /// <returns></returns>
        public override Boolean Install(String serviceName, String displayName, String binPath, String description)
        {
            XTrace.WriteLine("{0}.Install {1}, {2}, {3}, {4}", GetType().Name, serviceName, displayName, binPath, description);

            var file = _path.CombinePath($"{serviceName}.service");
            XTrace.WriteLine(file);

            var asm = Assembly.GetEntryAssembly();
            var des = !displayName.IsNullOrEmpty() ? displayName : description;

            var sb = new StringBuilder();
            sb.AppendLine("[Unit]");
            sb.AppendLine($"Description={des}");

            sb.AppendLine();
            sb.AppendLine("[Service]");
            sb.AppendLine("Type=simple");
            //sb.AppendLine($"ExecStart=/usr/bin/dotnet {asm.Location}");
            sb.AppendLine($"ExecStart={binPath}");
            sb.AppendLine($"WorkingDirectory={".".GetFullPath()}");
            sb.AppendLine("Restart=on-failure");

            sb.AppendLine();
            sb.AppendLine("[Install]");
            sb.AppendLine("WantedBy=multi-user.target");

            File.WriteAllText(file, sb.ToString());

            // sudo systemctl daemon-reload
            // sudo systemctl enable StarAgent
            // sudo systemctl start StarAgent

            Execute("systemctl", "daemon-reload");
            Execute("systemctl", $"enable {serviceName}");
            //Execute("systemctl", $"start {serviceName}");

            return true;
        }

        /// <summary>卸载服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean Remove(String serviceName)
        {
            XTrace.WriteLine("{0}.Remove {1}", GetType().Name, serviceName);

            var file = _path.CombinePath($"{serviceName}.service");
            if (File.Exists(file)) File.Delete(file);

            return true;
        }

        /// <summary>启动服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean Start(String serviceName) => Execute("systemctl", $"start {serviceName}") != null;

        /// <summary>停止服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean Stop(String serviceName) => Execute("systemctl", $"stop {serviceName}") != null;

        private static String Execute(String cmd, String arguments, Boolean writeLog = true)
        {
            if (writeLog) XTrace.WriteLine("{0} {1}", cmd, arguments);

            var psi = new ProcessStartInfo(cmd, arguments) { RedirectStandardOutput = true };
            var process = Process.Start(psi);
            if (!process.WaitForExit(3_000))
            {
                process.Kill();
                return null;
            }

            return process.StandardOutput.ReadToEnd();
        }
    }
}