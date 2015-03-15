
namespace NewLife.Net.Stun
{
    /// <summary>STUN消息类型</summary>
    public enum StunMessageType : ushort
    {
        /// <summary>绑定请求</summary>
        BindingRequest = 0x0001,

        /// <summary>绑定响应</summary>
        BindingResponse = 0x0101,

        /// <summary>错误响应</summary>
        BindingErrorResponse = 0x0111,

        /// <summary>安全请求</summary>
        SharedSecretRequest = 0x0002,

        /// <summary>安全响应</summary>
        SharedSecretResponse = 0x0102,

        /// <summary>安全错误响应</summary>
        SharedSecretErrorResponse = 0x0112,
    }
}