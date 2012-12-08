
namespace NewLife.Net.SGIP
{
    /// <summary>Unbind操作由Unbind命令和Unbind_Resp应答组成。通信连接建立以后，客户端如果要停止通信，需要发送Unbind命令；服务器端收到Unbind命令后，向客户端发送Unbind_Resp相应，然后双方断开连接</summary>
    public class SGIPUnbind : SGIPEntity
    {
        #region 构造
        /// <summary>实例化</summary>
        public SGIPUnbind() : base(SGIPCommands.Unbind) { }
        #endregion
    }
}