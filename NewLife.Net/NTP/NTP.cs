using System;

namespace NewLife.Net.NTP
{
    /// <summary>Network Time Protocol（NTP）是用来使计算机时间同步化的一种协议</summary>
    public class NTP
    {
        #region 属性
        private NTPLeapIndicator _LeapIndicator;
        /// <summary>跳跃指示器，警告在当月最后一天的最终时刻插入的迫近闺秒（闺秒）。</summary>
        public NTPLeapIndicator LeapIndicator
        {
            get { return _LeapIndicator; }
            set { _LeapIndicator = value; }
        }

        private Byte _VersionNumber;
        /// <summary>版本号。</summary>
        public Byte VersionNumber
        {
            get { return _VersionNumber; }
            set { _VersionNumber = value; }
        }

        private NTPMode _Mode;
        /// <summary>工作模式。该字段包括以下值：0－预留；1－对称行为；3－客户机；4－服务器；5－广播；6－NTP控制信息。NTP协议具有3种工作模式，分别为主/被动对称模式、客户/服务器模式、广播模式。在主/被动对称模式中，有一对一的连接，双方均可同步对方或被对方同步，先发出申请建立连接的一方工作在主动模式下，另一方工作在被动模式下；客户/服务器模式与主/被动模式基本相同，惟一区别在于客户方可被服务器同步，但服务器不能被客户同步；在广播模式中，有一对多的连接，服务器不论客户工作在何种模式下，都会主动发出时间信息，客户根据此信息调整自己的时间。</summary>
        public NTPMode Mode
        {
            get { return _Mode; }
            set { _Mode = value; }
        }

        private NTPStratum _Stratum;
        /// <summary>对本地时钟级别的整体识别。</summary>
        public NTPStratum Stratum
        {
            get { return _Stratum; }
            set { _Stratum = value; }
        }

        private UInt32 _PollInterval;
        /// <summary>有符号整数表示连续信息间的最大间隔。</summary>
        public UInt32 PollInterval
        {
            get { return _PollInterval; }
            set { _PollInterval = value; }
        }

        private Double _Precision;
        /// <summary>有符号整数表示本地时钟精确度。</summary>
        public Double Precision
        {
            get { return _Precision; }
            set { _Precision = value; }
        }

        private Double _RootDelay;
        /// <summary>表示到达主参考源的一次往复的总延迟，它是有15～16位小数部分的符号定点小数。</summary>
        public Double RootDelay
        {
            get { return _RootDelay; }
            set { _RootDelay = value; }
        }

        private Double _RootDispersion;
        /// <summary>表示一次到达主参考源的标准误差，它是有15～16位小数部分的无符号定点小数。</summary>
        public Double RootDispersion
        {
            get { return _RootDispersion; }
            set { _RootDispersion = value; }
        }

        private String _ReferenceID;
        /// <summary>识别特殊参考源。</summary>
        public String ReferenceID
        {
            get { return _ReferenceID; }
            set { _ReferenceID = value; }
        }

        private DateTime _ReferenceTimestamp;
        /// <summary>属性说明</summary>
        public DateTime ReferenceTimestamp
        {
            get { return _ReferenceTimestamp; }
            set { _ReferenceTimestamp = value; }
        }

        private DateTime _OriginateTimestamp;
        /// <summary>属性说明</summary>
        public DateTime OriginateTimestamp
        {
            get { return _OriginateTimestamp; }
            set { _OriginateTimestamp = value; }
        }

        private DateTime _ReceiveTimestamp;
        /// <summary>属性说明</summary>
        public DateTime ReceiveTimestamp
        {
            get { return _ReceiveTimestamp; }
            set { _ReceiveTimestamp = value; }
        }

        private DateTime _TransmitTimestamp;
        /// <summary>属性说明</summary>
        public DateTime TransmitTimestamp
        {
            get { return _TransmitTimestamp; }
            set { _TransmitTimestamp = value; }
        }
        #endregion

        #region 扩展属性
        private Int32 _KeyID;
        /// <summary>属性说明</summary>
        public Int32 KeyID
        {
            get { return _KeyID; }
            set { _KeyID = value; }
        }

        private Byte[] _MessageDigest;
        /// <summary>消息签名</summary>
        public Byte[] MessageDigest
        {
            get { return _MessageDigest; }
            set { _MessageDigest = value; }
        }
        #endregion
    }
}