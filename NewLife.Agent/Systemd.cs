using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
            var p = "/usr/lib/systemd/system/";
            if (Directory.Exists(p))
                _path = p;
            else
            {
                p = "/etc/systemd/system";
                if (Directory.Exists(p))
                    _path = p;
            }
        }

        /// <summary>启动服务</summary>
        /// <param name="service"></param>
        public override void Run(IHostedService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            _service = service;
        }

        /// <summary>服务是否已安装</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean IsInstalled(String serviceName) => false;

        /// <summary>服务是否已启动</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean IsRunning(String serviceName) => false;

        /// <summary>安装服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <param name="displayName">显示名</param>
        /// <param name="binPath">文件路径</param>
        /// <param name="description">描述信息</param>
        /// <returns></returns>
        public override Boolean Install(String serviceName, String displayName, String binPath, String description) => false;

        /// <summary>卸载服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean Remove(String serviceName) => false;

        /// <summary>启动服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean Start(String serviceName) => false;

        /// <summary>停止服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean Stop(String serviceName) => false;
    }
}