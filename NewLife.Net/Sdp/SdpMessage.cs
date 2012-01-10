using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Sdp
{
    /// <summary>会话描述协议</summary>
    /// <remarks>
    /// <a target="_blank" href="http://baike.baidu.com/view/875414.htm">会话描述协议</a>
    /// 
    /// 为会话通知、会话邀请和其它形式的多媒体会话初始化等目的提供了多媒体会话描述。
    /// </remarks>
    public class SdpMessage
    {
        #region 属性
        private String _Version;
        /// <summary>版本</summary>
        public String Version { get { return _Version; } set { _Version = value; } }

        private SdpOrigin _Origin;
        /// <summary>属性说明</summary>
        public SdpOrigin Origin { get { return _Origin; } set { _Origin = value; } }

        private String _SessionName;
        /// <summary>会话名</summary>
        public String SessionName { get { return _SessionName; } set { _SessionName = value; } }

        private String _SessionDescription;
        /// <summary>会话描述</summary>
        public String SessionDescription { get { return _SessionDescription; } set { _SessionDescription = value; } }

        private String _Uri;
        /// <summary>资源标识</summary>
        public String Uri { get { return _Uri; } set { _Uri = value; } }

        private SdpConnection _Connection;
        /// <summary>连接</summary>
        public SdpConnection Connection { get { return _Connection; } set { _Connection = value; } }

        private List<SdpTime> _Times;
        /// <summary>属性说明</summary>
        public List<SdpTime> Times { get { return _Times ?? (_Times = new List<SdpTime>()); } }

        private String _RepeatTimes;
        /// <summary>会话重复次数</summary>
        public String RepeatTimes { get { return _RepeatTimes; } set { _RepeatTimes = value; } }

        private List<SdpAttribute> _Attributes;
        /// <summary>属性集合</summary>
        public List<SdpAttribute> Attributes { get { return _Attributes ?? (_Attributes = new List<SdpAttribute>()); } }

        private List<SdpMediaDescription> _MediaDescriptions;
        /// <summary>媒体描述集合</summary>
        public List<SdpMediaDescription> MediaDescriptions { get { return _MediaDescriptions ?? (_MediaDescriptions = new List<SdpMediaDescription>()); } }
        #endregion
    }
}