using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.Agent
{
    /// <summary>Linux版进程守护</summary>
    public class Systemd : Host
    {
        private IHostedService _service;

        /// <summary>启动服务</summary>
        /// <param name="service"></param>
        public override void Run(IHostedService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            _service = service;
        }
    }
}