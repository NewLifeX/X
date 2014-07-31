using System;

namespace XAgent
{
    /// <summary>代理服务接口</summary>
    public interface IAgentService
    {
        /// <summary>服务名</summary>
        String ServiceName { get; set; }

        /// <summary>显示名</summary>
        String DisplayName { get; }

        /// <summary>服务描述</summary>
        String Description { get; }

        ///// <summary>是否已安装</summary>
        //Boolean? IsInstalled { get; }

        ///// <summary>是否已启动</summary>
        //Boolean? IsRunning { get; }
    }
}