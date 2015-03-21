using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.MQTT
{
    /// <summary>MQTT（Message Queue Telemetry Transport）,遥测传输协议</summary>
    /// <remarks>
    /// 提供订阅/发布模式，更为简约、轻量，易于使用，针对受限环境（带宽低、网络延迟高、网络通信不稳定），可以简单概括为物联网打造，官方总结特点如下：
    /// 1.使用发布/订阅消息模式，提供一对多的消息发布，解除应用程序耦合。
    /// 2. 对负载内容屏蔽的消息传输。
    /// 3. 使用 TCP/IP 提供网络连接。
    /// 4. 有三种消息发布服务质量：
    /// “至多一次”，消息发布完全依赖底层 TCP/IP 网络。会发生消息丢失或重复。这一级别可用于如下情况，环境传感器数据，丢失一次读记录无所谓，因为不久后还会有第二次发送。
    /// “至少一次”，确保消息到达，但消息重复可能会发生。
    /// “只有一次”，确保消息到达一次。这一级别可用于如下情况，在计费系统中，消息重复或丢失会导致不正确的结果。
    /// 5. 小型传输，开销很小（固定长度的头部是 2 字节），协议交换最小化，以降低网络流量。
    /// 6. 使用 Last Will 和 Testament 特性通知有关各方客户端异常中断的机制。

    /// </remarks>
    public class MqttMessage
    {
        #region 属性
        [BitSize(4)]
        private Byte _Type;
        /// <summary>消息类型</summary>
        public Byte Type { get { return _Type; } set { _Type = value; } }

        [BitSize(1)]
        private Byte _Dup;
        /// <summary>打开标识。值为1时表示当前消息先前已经被传送过</summary>
        /// <remarks>
        /// 保证消息可靠传输，默认为0，只占用一个字节，表示第一次发送。不能用于检测消息重复发送等。只适用于客户端或服务器端尝试重发PUBLISH, PUBREL, SUBSCRIBE 或 UNSUBSCRIBE消息，注意需要满足以下条件：
        /// 当QoS > 0
        /// 消息需要回复确认
        /// 此时，在可变头部需要包含消息ID。当值为1时，表示当前消息先前已经被传送过。
        /// </remarks>
        public Byte Dup { get { return _Dup; } set { _Dup = value; } }

        [BitSize(2)]
        private Byte _QoS;
        /// <summary>QoS等级</summary>
        public Byte QoS { get { return _QoS; } set { _QoS = value; } }

        [BitSize(1)]
        private Byte _Retain;
        /// <summary>保持。仅针对PUBLISH消息。不同值，不同含义</summary>
        /// <remarks>
        /// 1：表示发送的消息需要一直持久保存（不受服务器重启影响），不但要发送给当前的订阅者，并且以后新来的订阅了此Topic name的订阅者会马上得到推送。
        /// 备注：新来乍到的订阅者，只会取出最新的一个RETAIN flag = 1的消息推送。
        /// 0：仅仅为当前订阅者推送此消息。
        /// 假如服务器收到一个空消息体(zero-length payload)、RETAIN = 1、已存在Topic name的PUBLISH消息，服务器可以删除掉对应的已被持久化的PUBLISH消息。
        /// </remarks>
        public Byte Retain { get { return _Retain; } set { _Retain = value; } }

        private Byte _Length;
        /// <summary>长度。7位压缩编码整数</summary>
        /// <remarks>
        /// 在当前消息中剩余的byte(字节)数，包含可变头部和负荷(内容)。
        /// 单个字节最大值：01111111，16进制：0x7F，10进制为127。
        /// MQTT协议规定，第八位（最高位）若为1，则表示还有后续字节存在。
        /// </remarks>
        public Byte Length { get { return _Length; } set { _Length = value; } }
        #endregion
    }
}