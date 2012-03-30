using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.SGIP
{
    /// <summary>Report命令用于向SP发送一条先前的Submit命令的当前状态，或者用于向前转SMG发送一条先前的Deliver命令的当前状态。Report命令的接收方需要向发送方返回Report_Resp命令</summary>
    public class SGIPReport : SGIPEntity
    {
        #region 属性
        private UInt32 _SubmitSequenceNumber1;
        /// <summary>该命令所涉及的Submit或deliver命令的序列号</summary>
        public UInt32 SubmitSequenceNumber1 { get { return _SubmitSequenceNumber1; } set { _SubmitSequenceNumber1 = value; } }

        private UInt32 _SubmitSequenceNumber2;
        /// <summary>该命令所涉及的Submit或deliver命令的序列号</summary>
        public UInt32 SubmitSequenceNumber2 { get { return _SubmitSequenceNumber2; } set { _SubmitSequenceNumber2 = value; } }

        private UInt32 _SubmitSequenceNumber3;
        /// <summary>该命令所涉及的Submit或deliver命令的序列号</summary>
        public UInt32 SubmitSequenceNumber3 { get { return _SubmitSequenceNumber3; } set { _SubmitSequenceNumber3 = value; } }

        private SGIPReportTypes _ReportType;
        /// <summary>Report命令类型</summary>
        public SGIPReportTypes ReportType { get { return _ReportType; } set { _ReportType = value; } }

        [FieldSize(21)]
        private String _UserNumber;
        /// <summary>接收短消息的手机号，手机号码前加“86”国别标志</summary>
        public String UserNumber { get { return _UserNumber; } set { _UserNumber = value; } }

        private SGIPReportStates _State;
        /// <summary>该命令所涉及的短消息的当前执行状态</summary>
        public SGIPReportStates State { get { return _State; } set { _State = value; } }

        private SGIPErrorCodes _ErrorCode;
        /// <summary>当State=2时为错误码值，否则为0</summary>
        public SGIPErrorCodes ErrorCode { get { return _ErrorCode; } set { _ErrorCode = value; } }

        [FieldSize(8)]
        private String _Reserve;
        /// <summary>保留，扩展用</summary>
        public String Reserve { get { return _Reserve; } set { _Reserve = value; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public SGIPReport() : base(SGIPCommands.Report) { }
        #endregion
    }

    /// <summary>Report命令类型</summary>>
    public enum SGIPReportTypes : byte
    {
        /// <summary>0：对先前一条Submit命令的状态报告</summary>>
        Submit = 0,
        /// <summary>1：对先前一条前转Deliver命令的状态报告</summary>>
        Deliver = 1,
    }

    /// <summary>该命令所涉及的短消息的当前执行状态</summary>>
    public enum SGIPReportStates : byte
    {
        /// <summary>0：发送成功</summary>>
        Success = 0,
        /// <summary>1：等待发送</summary>>
        Accepted = 1,
        /// <summary>2：发送失败</summary>>
        Error = 2,
    }
}