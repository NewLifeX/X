
using System.ComponentModel;
namespace NewLife.Net.Stun
{
    /// <summary>UDP网络类型</summary>
    /// <remarks>
    /// <a target="_blank" href="http://zh.wikipedia.org/wiki/%E7%BD%91%E7%BB%9C%E5%9C%B0%E5%9D%80%E8%BD%AC%E6%8D%A2">网络地址转换【维基百科】</a>
    /// </remarks>
    public enum StunNetType
    {
        /// <summary>被禁止，或无法连接STUN服务器</summary>
        [Description("被禁止，或无法连接STUN服务器")]
        Blocked,

        /// <summary>公网地址，没有NAT和防火墙</summary>
        [Description("公网地址，没有NAT和防火墙")]
        OpenInternet,

        /// <summary>公网地址，没有NAT，对称UDP防火墙</summary>
        [Description("公网地址，没有NAT，对称UDP防火墙")]
        SymmetricUdpFirewall,

        /// <summary>
        /// 一对一完全圆锥NAT。IP和端口均可变。
        /// 一旦一个内部地址(iAddr:port1)映射到外部地址(eAddr:port2),所有发自iAddr:port1的包都经由eAddr:port2向外发送.
        /// 任意外部主机都能通过给eAddr:port2发包到达iAddr:port1
        /// </summary>
        [Description("一对一完全圆锥NAT")]
        FullCone,

        /// <summary>
        /// 受限圆锥NAT。IP必须固定，端口可变。
        /// 一旦一个内部地址(iAddr:port1)映射到外部地址(eAddr:port2),所有发自iAddr:port1的包都经由eAddr:port2向外发送.
        /// 任意外部主机(hostAddr:any)都能通过给eAddr:port2发包到达iAddr:port1的前提是：iAddr:port1之前发送过包到hostAddr:any. "any"也就是说端口不受限制
        /// </summary>
        [Description("受限圆锥NAT")]
        RestrictedCone,

        /// <summary>
        /// 端口受限圆锥NAT。IP和端口都必须固定
        /// 一旦一个内部地址(iAddr:port1)映射到外部地址(eAddr:port2),所有发自iAddr:port1的包都经由eAddr:port2向外发送.
        /// 一个外部主机(hostAddr:port3)能够发包到达iAddr:port1的前提是：iAddr:port1之前发送过包到hostAddr:port3.
        /// </summary>
        [Description("端口受限圆锥NAT")]
        PortRestrictedCone,

        /// <summary>
        /// 对称NAT。同一内部地址端口，连接不同外网时，映射的公网地址和端口均不同。
        /// 每一个来自相同内部IP与port的请求到一个特定目的地的IP地址和端口，映射到一个独特的外部来源的IP地址和端口。
        /// 同一个内部主机发出一个信息包到不同的目的端，不同的映射使用
        /// 只有曾经收到过内部主机封包的外部主机，才能够把封包发回来
        /// </summary>
        [Description("对称NAT")]
        Symmetric
    }
}