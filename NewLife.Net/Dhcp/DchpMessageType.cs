using System.ComponentModel;

namespace NewLife.Net.Dhcp
{
    /// <summary>DHCP消息类型</summary>
    public enum DchpMessageType : byte
    {
        /// <summary>发现</summary>
        [Description("发现")]
        Discover = 1,

        /// <summary>提供</summary>
        [Description("提供")]
        Offer = 2,

        /// <summary>请求</summary>
        [Description("请求")]
        Request = 3,

        /// <summary>谢绝</summary>
        [Description("谢绝")]
        Decline = 4,

        /// <summary>应答</summary>
        [Description("应答")]
        Ack = 5,

        /// <summary>拒绝</summary>
        [Description("拒绝")]
        Nak = 6,

        /// <summary>释放</summary>
        [Description("释放")]
        Release = 7,

        /// <summary>通知</summary>
        [Description("通知")]
        Inform = 8
    }
}