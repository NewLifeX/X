
namespace NewLife.Net.MQTT.Packets
{
    /// <summary>连接返回代码</summary>
    public enum ConnectReturnCode
    {
        /// <summary>已接受</summary>
        Accepted = 0x00,

        /// <summary>拒绝不可用协议版本</summary>
        RefusedUnacceptableProtocolVersion = 0X01,

        /// <summary>拒绝标识</summary>
        RefusedIdentifierRejected = 0x02,

        /// <summary>服务不可用</summary>
        RefusedServerUnavailable = 0x03,

        /// <summary>错误用户名密码</summary>
        RefusedBadUsernameOrPassword = 0x04,

        /// <summary>为认证</summary>
        RefusedNotAuthorized = 0x05
    }
}