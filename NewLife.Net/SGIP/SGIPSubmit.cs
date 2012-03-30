using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.SGIP
{
    /// <summary>
    /// 在SP和SMG的通信中，SP用Submit命令向SMG提交MT短消息，发送到用户的手机中。SMG接收到Submit命令，会返回Submit_Resp应答。SMG根据Submit命令中的付费号码，判断出该命令是否应从本地SMSC发送，如果属于本地发送，则直接发送到相应的SMSC，否则路由至相应的SMG。
    /// 在SMG和SMG的通信中，Submit命令用于SMG客户端向服务器端路由从SP收到的MT短消息。服务器端接收到Submit命令后，再发送到与之相连的目的SMSC。
    /// </summary>
    public class SGIPSubmit : SGIPEntity
    {
        #region 属性
        [FieldSize(21)]
        private String _SPNumber;
        /// <summary>SP的接入号码</summary>
        public String SPNumber { get { return _SPNumber; } set { _SPNumber = value; } }

        [FieldSize(21)]
        private String _ChargeNumber;
        /// <summary>付费号码，手机号码前加“86”国别标志；当且仅当群发且对用户收费时为空；如果为空，则该条短消息产生的费用由UserNumber代表的用户支付；如果为全零字符串“000000000000000000000”，表示该条短消息产生的费用由SP支付。</summary>
        public String ChargeNumber { get { return _ChargeNumber; } set { _ChargeNumber = value; } }

        private Byte _UserCount;
        /// <summary>接收短消息的手机数量，取值范围1至100</summary>
        public Byte UserCount { get { return _UserCount; } set { _UserCount = value; } }

        [FieldSize(21)]
        private String _UserNumber;
        /// <summary>接收该短消息的手机号，该字段重复UserCount指定的次数，手机号码前加“86”国别标志</summary>
        public String UserNumber { get { return _UserNumber; } set { _UserNumber = value; } }

        [FieldSize(5)]
        private String _CorpId;
        /// <summary>企业代码，取值范围0-99999</summary>
        public String CorpId { get { return _CorpId; } set { _CorpId = value; } }

        [FieldSize(10)]
        private String _ServiceType;
        /// <summary>业务代码，由SP定义</summary>
        public String ServiceType { get { return _ServiceType; } set { _ServiceType = value; } }

        private FeeTypes _FeeType;
        /// <summary>计费类型</summary>
        public FeeTypes FeeType { get { return _FeeType; } set { _FeeType = value; } }

        [FieldSize(6)]
        private String _FeeValue;
        /// <summary>取值范围0-99999，该条短消息的收费值，单位为分，由SP定义 对于包月制收费的用户，该值为月租费的值</summary>
        public String FeeValue { get { return _FeeValue; } set { _FeeValue = value; } }

        [FieldSize(6)]
        private String _GivenValue;
        /// <summary>取值范围0-99999，赠送用户的话费，单位为分，由SP定义，特指由SP向用户发送广告时的赠送话费</summary>
        public String GivenValue { get { return _GivenValue; } set { _GivenValue = value; } }

        private SubmitAgentFlags _AgentFlag;
        /// <summary>代收费标志，0：应收；1：实收</summary>
        public SubmitAgentFlags AgentFlag { get { return _AgentFlag; } set { _AgentFlag = value; } }

        private SubmitMorelatetoMTFlags _MorelatetoMTFlag;
        /// <summary>引起MT消息的原因0-MO点播引起的第一条MT消息；1-MO点播引起的非第一条MT消息；2-非MO点播引起的MT消息；3-系统反馈引起的MT消息。</summary>
        public SubmitMorelatetoMTFlags MorelatetoMTFlag { get { return _MorelatetoMTFlag; } set { _MorelatetoMTFlag = value; } }

        private Byte _Priority;
        /// <summary>优先级0-9从低到高，默认为0</summary>
        public Byte Priority { get { return _Priority; } set { _Priority = value; } }

        [FieldSize(16)]
        private String _ExpireTime;
        /// <summary>短消息寿命的终止时间，如果为空，表示使用短消息中心的缺省值。时间内容为16个字符，格式为”yymmddhhmmsstnnp” ，其中“tnnp”取固定值“032+”，即默认系统为北京时间</summary>
        public String ExpireTime { get { return _ExpireTime; } set { _ExpireTime = value; } }

        [FieldSize(16)]
        private String _ScheduleTime;
        /// <summary>短消息定时发送的时间，如果为空，表示立刻发送该短消息。时间内容为16个字符，格式为“yymmddhhmmsstnnp” ，其中“tnnp”取固定值“032+”，即默认系统为北京时间</summary>
        public String ScheduleTime { get { return _ScheduleTime; } set { _ScheduleTime = value; } }

        private SubmitReportFlags _ReportFlag;
        /// <summary>状态报告标记0-该条消息只有最后出错时要返回状态报告1-该条消息无论最后是否成功都要返回状态报告2-该条消息不需要返回状态报告3-该条消息仅携带包月计费信息，不下发给用户，要返回状态报告其它-保留缺省设置为0</summary>
        public SubmitReportFlags ReportFlag { get { return _ReportFlag; } set { _ReportFlag = value; } }

        private Byte _TP_pid;
        /// <summary>GSM协议类型。详细解释请参考GSM03.40中的9.2.3.9</summary>
        public Byte TP_pid { get { return _TP_pid; } set { _TP_pid = value; } }

        private Byte _TP_udhi;
        /// <summary>GSM协议类型。详细解释请参考GSM03.40中的9.2.3.23,仅使用1位，右对齐</summary>
        public Byte TP_udhi { get { return _TP_udhi; } set { _TP_udhi = value; } }

        private SGIPMessageCodings _MessageCoding;
        /// <summary>短消息的编码格式。0：纯ASCII字符串3：写卡操作4：二进制编码8：UCS2编码15: GBK编码其它参见GSM3.38第4节：SMS Data Coding Scheme</summary>
        public SGIPMessageCodings MessageCoding { get { return _MessageCoding; } set { _MessageCoding = value; } }

        private Byte _MessageType;
        /// <summary>信息类型：0-短消息信息其它：待定</summary>
        public Byte MessageType { get { return _MessageType; } set { _MessageType = value; } }

        private UInt32 _MessageLength;
        /// <summary>短消息的长度</summary>
        public UInt32 MessageLength { get { return _MessageLength; } set { _MessageLength = value; } }

        [FieldSize("_MessageLength")]
        private String _MessageContent;
        /// <summary>短消息的内容</summary>
        public String MessageContent { get { return _MessageContent; } set { _MessageContent = value; } }

        [FieldSize(8)]
        private String _Reserve;
        /// <summary>保留，扩展用</summary>
        public String Reserve { get { return _Reserve; } set { _Reserve = value; } }
        #endregion
   
        #region 构造
        /// <summary>实例化</summary>
        public SGIPSubmit() : base(SGIPCommands.Submit) { }
        #endregion
 }

    /// <summary>代收费标志，0：应收；1：实收</summary>>
    public enum SubmitAgentFlags : byte
    {
        /// <summary>0：应收</summary>>
        SouldIncome = 0,
        /// <summary>1：实收</summary>>
        RealIncome = 1,
    }

    /// <summary>状态报告标记</summary>>
    public enum SubmitReportFlags : byte
    {
        /// <summary>0-该条消息只有最后出错时要返回状态报告</summary>>
        ErrorReport = 0,
        /// <summary>1-该条消息无论最后是否成功都要返回状态报告</summary>>
        Always = 1,
        /// <summary>2-该条消息不需要返回状态报告</summary>>
        NoReport = 2,
        /// <summary>3-该条消息仅携带包月计费信息，不下发给用户，要返回状态报告</summary>>
        MonthReport = 3,
    }

    /// <summary>引起MT消息的原因</summary>>
    public enum SubmitMorelatetoMTFlags : byte
    {
        /// <summary>0-MO点播引起的第一条MT消息；</summary>>
        VoteFirst = 0,
        /// <summary>1-MO点播引起的非第一条MT消息；</summary>>
        VoteNonFirst = 1,
        /// <summary>2-非MO点播引起的MT消息；</summary>>
        NormalFirst = 2,
        /// <summary>3-系统反馈引起的MT消息。</summary>>
        NormalNonFirst = 3,
    }

    /// <summary>计费类别定义</summary>>
    public enum FeeTypes : byte
    {
        /// <summary>0	“短消息类型”为“发送”，对“计费用户号码”不计信息费，此类话单仅用于核减SP对称的信道费</summary>>
        FreeSend = 0,
        /// <summary>1	对“计费用户号码”免费</summary>>
        Free = 1,
        /// <summary>2	对“计费用户号码”按条计信息费</summary>>
        RowNumFee = 2,
        /// <summary>3	对“计费用户号码”按包月收取信息费</summary>>
        MonthFee = 3,
        /// <summary>4	对“计费用户号码”的收费是由SP实现</summary>>
        SpFee = 4,
    }
}