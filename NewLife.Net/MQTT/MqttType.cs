using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.MQTT
{
    public enum MqttType : byte
    {
        /// <summary>保留</summary>
        Reserved = 0,

        /// <summary>连接</summary>
        Connect,

        /// <summary>连接确认</summary>
        ConnAck,

        /// <summary>发布消息</summary>
        Publish,

        /// <summary>发布确认</summary>
        PubAck,

        /// <summary>发布已接收</summary>
        PubRec,

        /// <summary>发布已释放</summary>
        PubRel,

        /// <summary>发布已完成</summary>
        PubComp,

        /// <summary>客户端订阅请求</summary>
        Subscribe,

        /// <summary>订阅确认</summary>
        SubAck,

        /// <summary>取消订阅</summary>
        UnSubscribe,

        /// <summary>取消订阅确认</summary>
        UnSubAck,

        /// <summary>Ping请求</summary>
        PingReq,

        /// <summary>Ping响应</summary>
        PingResp,

        /// <summary>断开连接</summary>
        Disconnect,

        /// <summary>保留</summary>
        Reserved2
    }
}