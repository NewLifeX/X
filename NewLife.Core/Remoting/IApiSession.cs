using System.Threading.Tasks;

namespace NewLife.Remoting
{
    /// <summary>Api会话</summary>
    public interface IApiSession
    {
        /// <summary>远程调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Task<TResult> Invoke<TResult>(string action, object args = null);
    }
}