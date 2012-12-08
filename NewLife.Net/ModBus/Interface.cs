
namespace NewLife.Net.ModBus
{
    /// <summary>请求接口</summary>
    public interface IModBusRequest
    {
        /// <summary>功能码</summary>
        MBFunction Function { get; set; }
    }

    /// <summary>响应接口</summary>
    public interface IModBusResponse
    {
        /// <summary>功能码</summary>
        MBFunction Function { get; set; }
    }
}