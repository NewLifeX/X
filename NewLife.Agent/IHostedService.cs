using System.Threading;
using System.Threading.Tasks;

namespace NewLife.Agent
{
    /// <summary>主机承载的服务</summary>
    public interface IHostedService
    {
        /// <summary>开始</summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>停止</summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StopAsync(CancellationToken cancellationToken);
    }
}