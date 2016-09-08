using NewLife.Log;

namespace NewLife.Remoting
{
    /// <summary>应用接口客户端接口</summary>
    public interface IApiClient
    {
        /// <summary>打开</summary>
        void Open();

        /// <summary>关闭</summary>
        void Close();

        /// <summary>日志</summary>
        ILog Log { get; set; }
    }
}