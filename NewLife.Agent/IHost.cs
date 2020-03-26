using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewLife.Agent
{
    /// <summary>服务主机</summary>
    public interface IHost
    {
        ///// <summary>服务集合</summary>
        //IList<IHostedService> Services { get; }

        ///// <summary>开始</summary>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //Task StartAsync(CancellationToken cancellationToken = default);

        ///// <summary>结束</summary>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //Task StopAsync(CancellationToken cancellationToken = default);

        ///// <summary>获取托管服务</summary>
        ///// <param name="serviceName"></param>
        ///// <returns></returns>
        //IHostedService GetService(String serviceName);

        /// <summary>服务是否已安装</summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        Boolean IsInstalled(String serviceName);

        /// <summary>服务是否已启动</summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        Boolean IsRunning(String serviceName);

        Boolean Install(String serviceName, String displayName, String binPath, String description);
        Boolean Uninstall(String serviceName);
        Boolean Start(String serviceName);
        Boolean Stop(String serviceName);
    }

    /// <summary>服务主机。用于管理控制服务</summary>
    public abstract class Host : DisposeBase, IHost
    {
        ///// <summary>服务集合</summary>
        //public IList<IHostedService> Services { get; } = new List<IHostedService>();

        ///// <summary>销毁</summary>
        ///// <param name="disposing"></param>
        //protected override void Dispose(Boolean disposing)
        //{
        //    base.Dispose(disposing);

        //    foreach (var item in Services)
        //    {
        //        item.TryDispose();
        //    }
        //}

        ///// <summary>开始</summary>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //public virtual async Task StartAsync(CancellationToken cancellationToken = default)
        //{
        //    foreach (var item in Services)
        //    {
        //        cancellationToken.ThrowIfCancellationRequested();

        //        await item.StartAsync(cancellationToken).ConfigureAwait(false);
        //    }
        //}

        ///// <summary>结束</summary>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //public virtual async Task StopAsync(CancellationToken cancellationToken = default)
        //{
        //    foreach (var item in Services)
        //    {
        //        cancellationToken.ThrowIfCancellationRequested();

        //        await item.StopAsync(cancellationToken).ConfigureAwait(false);
        //    }
        //}

        ///// <summary>获取托管服务</summary>
        ///// <param name="serviceName"></param>
        ///// <returns></returns>
        //public virtual IHostedService GetService(String serviceName) => null;

        public virtual Boolean IsInstalled(String serviceName) => false;

        public virtual Boolean IsRunning(String serviceName) => false;

        public virtual Boolean Install(String serviceName, String displayName, String binPath, String description) => false;
        public virtual Boolean Uninstall(String serviceName) => false;
        public virtual Boolean Start(String serviceName) => false;
        public virtual Boolean Stop(String serviceName) => false;
    }
}