using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.SGIP
{
    public abstract class SGIPEntity
    {
        #region 属性
        private SGIPCommands _Command;
        /// <summary>命令</summary>
        public SGIPCommands Command { get { return _Command; } set { _Command = value; } }

        private UInt32 _SrcNodeSequence;
        /// <summary>序列号</summary>
        public UInt32 SrcNodeSequence { get { return _SrcNodeSequence; } set { _SrcNodeSequence = value; } }

        private UInt32 _DateSequence;
        /// <summary>序列号</summary>
        public UInt32 DateSequence { get { return _DateSequence; } set { _DateSequence = value; } }

        private static UInt32 msgseq = 0;
        private UInt32 _MsgSequence;
        /// <summary>序列号</summary>
        public UInt32 MsgSequence { get { return _MsgSequence; } set { _MsgSequence = value; } }
        #endregion

        #region 构造
        public SGIPEntity()
        {
            DateSequence = (uint)(DateTime.Now.Month * 100000000 +
                DateTime.Now.Day * 1000000 +
                DateTime.Now.Hour * 10000 +
                DateTime.Now.Minute * 100 +
                DateTime.Now.Second);

            MsgSequence = ++msgseq;
        }
        #endregion
    }

    public class SGIPResponse : SGIPEntity
    {
        #region 属性
        private SGIPErrorCodes _Result;
        /// <summary>结果</summary>
        public SGIPErrorCodes Result { get { return _Result; } set { _Result = value; } }

        [FieldSize(8)]
        private String _Reserve;
        /// <summary>保留</summary>
        public String Reserve { get { return _Reserve; } set { _Reserve = value; } }
        #endregion
    }

    /// <summary>
    /// 指令枚举
    /// </summary>
    public enum SGIPCommands : uint
    {
        /// <summary>
        /// 绑定
        /// </summary>
        Bind = 0x1,
        /// <summary>
        /// 绑定响应
        /// </summary>
        Bind_Resp = 0x80000001,
        /// <summary>
        /// 取消绑定
        /// </summary>
        Unbind = 0x2,
        /// <summary>
        /// 取消绑定响应
        /// </summary>
        Unbind_Resp = 0x80000002,
        /// <summary>
        /// 提交
        /// </summary>
        Submit = 0x3,
        /// <summary>
        /// 提交响应
        /// </summary>
        Submit_Resp = 0x80000003,
        /// <summary>
        /// 分发
        /// </summary>
        Deliver = 0x4,
        /// <summary>
        /// 分发响应
        /// </summary>
        Deliver_Resp = 0x80000004,
        /// <summary>
        /// 报告
        /// </summary>
        Report = 0x5,
        /// <summary>
        /// 报告响应
        /// </summary>
        Report_Resp = 0x80000005,
        //SGIP_ADDSP = 0x6,
        //SGIP_ADDSP_RESP = 0x80000006,
        //SGIP_MODIFYSP = 0x7,
        //SGIP_MODIFYSP_RESP = 0x80000007,
        //SGIP_DELETESP = 0x8,
        //SGIP_DELETESP_RESP = 0x80000008,
        //SGIP_QUERYROUTE = 0x9,
        //SGIP_QUERYROUTE_RESP = 0x80000009,
        //SGIP_ADDTELESEG = 0xa,
        //SGIP_ADDTELESEG_RESP = 0x8000000a,
        //SGIP_MODIFYTELESEG = 0xb,
        //SGIP_MODIFYTELESEG_RESP = 0x8000000b,
        //SGIP_DELETETELESEG = 0xc,
        //SGIP_DELETETELESEG_RESP = 0x8000000c,
        //SGIP_ADDSMG = 0xd,
        //SGIP_ADDSMG_RESP = 0x8000000d,
        //SGIP_MODIFYSMG = 0xe,
        //SGIP_MODIFYSMG_RESP = 0x0000000e,
        //SGIP_DELETESMG = 0xf,
        //SGIP_DELETESMG_RESP = 0x8000000f,
        //SGIP_CHECKUSER = 0x10,
        //SGIP_CHECKUSER_RESP = 0x80000010,
        //SGIP_USERRPT = 0x11,
        //SGIP_USERRPT_RESP = 0x80000011,
        //SGIP_TRACE = 0x1000,
        //SGIP_TRACE_RESP = 0x80001000,
    }

    /// <summary>
    /// 错误代码
    /// </summary>
    public enum SGIPErrorCodes : byte
    {

        /// <summary>
        /// 0	无错误，命令正确接收
        /// </summary>
        Success = 0,
        /// <summary>
        /// 1	非法登录，如登录名、口令出错、登录名与口令不符等。
        /// </summary>
        LoginError = 1,
        /// <summary>
        /// 2	重复登录，如在同一TCP/IP连接中连续两次以上请求登录。
        /// </summary>
        Relogon = 2,
        /// <summary>
        /// 3	连接过多，指单个节点要求同时建立的连接数过多。
        /// </summary>
        ConnectionFull = 3,
        /// <summary>
        /// 4	登录类型错，指bind命令中的logintype字段出错。
        /// </summary>
        ErrorLoginType = 4,
        /// <summary>
        /// 5	参数格式错，指命令中参数值与参数类型不符或与协议规定的范围不符。
        /// </summary>
        ParameterError = 5,
        /// <summary>
        /// 6	非法手机号码，协议中所有手机号码字段出现非86130号码或手机号码前未加“86”时都应报错。
        /// </summary>
        TelnumberError = 6,
        /// <summary>
        /// 7	消息ID错
        /// </summary>
        MsgIDError = 7,
        /// <summary>
        /// 8	信息长度错
        /// </summary>
        PackageLengthError = 8,
        /// <summary>
        /// 9	非法序列号，包括序列号重复、序列号格式错误等
        /// </summary>
        SequenceError = 9,
        /// <summary>
        /// 10	非法操作GNS
        /// </summary>
        GnsOperationError = 10,
        /// <summary>
        /// 11	节点忙，指本节点存储队列满或其他原因，暂时不能提供服务的情况
        /// </summary>
        NodeBusy = 11,
        /// <summary>
        /// 21	目的地址不可达，指路由表存在路由且消息路由正确但被路由的节点暂时不能提供服务的情况
        /// </summary>
        NodeCanNotReachable = 21,
        /// <summary>
        /// 22	路由错，指路由表存在路由但消息路由出错的情况，如转错SMG等
        /// </summary>
        RouteError = 22,
        /// <summary>
        /// 23	路由不存在，指消息路由的节点在路由表中不存在
        /// </summary>
        RoutNodeNotExisted = 23,
        /// <summary>
        /// 24	计费号码无效，鉴权不成功时反馈的错误信息
        /// </summary>
        FeeNumberError = 24,
        /// <summary>
        /// 25	用户不能通信（如不在服务区、未开机等情况）
        /// </summary>
        UserCanNotReachable = 25,
        /// <summary>
        /// 26	手机内存不足
        /// </summary>
        HandsetFull = 26,
        /// <summary>
        /// 27	手机不支持短消息
        /// </summary>
        HandsetCanNotRecvSms = 27,
        /// <summary>
        /// 28	手机接收短消息出现错误
        /// </summary>
        HandsetReturnError = 28,
        /// <summary>
        /// 29	不知道的用户
        /// </summary>
        UnknownUser = 29,
        /// <summary>
        /// 30	不提供此功能
        /// </summary>
        NoDevice = 30,
        /// <summary>
        /// 31	非法设备
        /// </summary>
        InvalidateDevice = 31,
        /// <summary>
        /// 32	系统失败（一般指系统消息队列满）
        /// </summary>
        SystemError = 32,
        /// <summary>
        /// 33	 超过流量限制，指发送方在一秒内的流量已经达到限制，拒绝发送
        /// </summary>
        FullSequence = 33,
        /// <summary>
        /// 未知错误
        /// </summary>
        OtherError = 99,
    }
}