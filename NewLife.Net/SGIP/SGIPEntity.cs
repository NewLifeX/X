using System;
using System.ComponentModel;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Net.SGIP
{
    /// <summary>SGIP命令实体基类</summary>
    public abstract class SGIPEntity
    {
        #region 属性
        [NonSerialized]
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
        /// <summary>实例化</summary>
        public SGIPEntity()
        {
            DateSequence = (uint)(DateTime.Now.Month * 100000000 +
        DateTime.Now.Day * 1000000 +
        DateTime.Now.Hour * 10000 +
        DateTime.Now.Minute * 100 +
        DateTime.Now.Second);

            MsgSequence = ++msgseq;
        }

        /// <summary>实例化</summary>
        /// <param name="command"></param>
        public SGIPEntity(SGIPCommands command) : this() { Command = command; }

        static SGIPEntity() { Install(); }

        static void Install()
        {
            NetService.Container.Register<SGIPEntity, SGIPBind>(SGIPCommands.Bind)
                .Register<SGIPEntity, SGIPDeliver>(SGIPCommands.Deliver)
                .Register<SGIPEntity, SGIPReport>(SGIPCommands.Report)
                .Register<SGIPEntity, SGIPSubmit>(SGIPCommands.Submit)
                .Register<SGIPEntity, SGIPUnbind>(SGIPCommands.Unbind)
                .Register<SGIPEntity, SGIPResponse>(SGIPCommands.Bind_Resp)
                .Register<SGIPEntity, SGIPResponse>(SGIPCommands.Deliver_Resp)
                .Register<SGIPEntity, SGIPResponse>(SGIPCommands.Report_Resp)
                .Register<SGIPEntity, SGIPResponse>(SGIPCommands.Submit_Resp)
                .Register<SGIPEntity, SGIPResponse>(SGIPCommands.Unbind_Resp);
        }
        #endregion

        #region 读写
        /// <summary>从流中读取对象</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static SGIPEntity Read(Stream stream)
        {
            var reader = new BinaryReaderX(stream);
            reader.Settings.EncodeInt = false;

            // 先读取包长度和命令类型
            var len = reader.ReadInt32();
            var cmd = (SGIPCommands)reader.ReadUInt32();

            var type = NetService.Container.ResolveType<SGIPEntity>(cmd);
            var entity = reader.ReadObject(type) as SGIPEntity;
            entity.Command = cmd;
            return entity;
        }

        /// <summary>把对象写入流中</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public void Write(Stream stream)
        {
            var writer = new BinaryWriterX();
            writer.Settings.EncodeInt = false;
            writer.Write((UInt32)Command);
            writer.WriteObject(this);

            // 拿出内部流，换一个流，为了用这个读写器
            var ms = writer.Stream;
            writer.Stream = stream;

            // 写入长度
            writer.Write((UInt32)ms.Length);
            ms.Position = 0;
            ms.CopyTo(stream);
        }

        /// <summary>获取数据流</summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            var ms = new MemoryStream();
            Write(ms);
            ms.Position = 0;
            return ms;
        }
        #endregion
    }

    /// <summary>响应</summary>
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

    /// <summary>指令枚举</summary>
    public enum SGIPCommands : uint
    {
        /// <summary>绑定</summary>
        Bind = 0x1,
        /// <summary>绑定响应</summary>
        Bind_Resp = 0x80000001,
        /// <summary>取消绑定</summary>
        Unbind = 0x2,
        /// <summary>取消绑定响应</summary>
        Unbind_Resp = 0x80000002,
        /// <summary>提交</summary>
        Submit = 0x3,
        /// <summary>提交响应</summary>
        Submit_Resp = 0x80000003,
        /// <summary>分发</summary>
        Deliver = 0x4,
        /// <summary>分发响应</summary>
        Deliver_Resp = 0x80000004,
        /// <summary>报告</summary>
        Report = 0x5,
        /// <summary>报告响应</summary>
        Report_Resp = 0x80000005,
        //ADDSP = 0x6,
        //ADDSP_RESP = 0x80000006,
        //MODIFYSP = 0x7,
        //MODIFYSP_RESP = 0x80000007,
        //DELETESP = 0x8,
        //DELETESP_RESP = 0x80000008,
        //QUERYROUTE = 0x9,
        //QUERYROUTE_RESP = 0x80000009,
        //ADDTELESEG = 0xa,
        //ADDTELESEG_RESP = 0x8000000a,
        //MODIFYTELESEG = 0xb,
        //MODIFYTELESEG_RESP = 0x8000000b,
        //DELETETELESEG = 0xc,
        //DELETETELESEG_RESP = 0x8000000c,
        //ADDSMG = 0xd,
        //ADDSMG_RESP = 0x8000000d,
        //MODIFYSMG = 0xe,
        //MODIFYSMG_RESP = 0x0000000e,
        //DELETESMG = 0xf,
        //DELETESMG_RESP = 0x8000000f,
        //CHECKUSER = 0x10,
        //CHECKUSER_RESP = 0x80000010,
        //USERRPT = 0x11,
        //USERRPT_RESP = 0x80000011,
        //TRACE = 0x1000,
        //TRACE_RESP = 0x80001000,
    }

    /// <summary>错误代码</summary>
    public enum SGIPErrorCodes : byte
    {
        /// <summary>无错误，命令正确接收</summary>
        [Description("无错误，命令正确接收")]
        Success = 0,

        /// <summary>非法登录，如登录名、口令出错、登录名与口令不符等。</summary>
        [Description("非法登录，如登录名、口令出错、登录名与口令不符等")]
        LoginError = 1,

        /// <summary>重复登录，如在同一TCP/IP连接中连续两次以上请求登录。</summary>
        [Description("重复登录，如在同一TCP/IP连接中连续两次以上请求登录")]
        Relogon = 2,

        /// <summary>连接过多，指单个节点要求同时建立的连接数过多。</summary>
        [Description("连接过多，指单个节点要求同时建立的连接数过多")]
        ConnectionFull = 3,

        /// <summary>登录类型错，指bind命令中的logintype字段出错。</summary>
        [Description("登录类型错，指bind命令中的logintype字段出错")]
        ErrorLoginType = 4,

        /// <summary>参数格式错，指命令中参数值与参数类型不符或与协议规定的范围不符。</summary>
        [Description("参数格式错，指命令中参数值与参数类型不符或与协议规定的范围不符")]
        ParameterError = 5,

        /// <summary>非法手机号码，协议中所有手机号码字段出现非86130号码或手机号码前未加“86”时都应报错。</summary>
        [Description("非法手机号码，协议中所有手机号码字段出现非86130号码或手机号码前未加“86”时都应报错")]
        TelnumberError = 6,

        /// <summary>消息ID错</summary>
        [Description("消息ID错")]
        MsgIDError = 7,

        /// <summary>信息长度错</summary>
        [Description("信息长度错")]
        PackageLengthError = 8,

        /// <summary>非法序列号，包括序列号重复、序列号格式错误等</summary>
        [Description("非法序列号，包括序列号重复、序列号格式错误等")]
        SequenceError = 9,

        /// <summary>非法操作GNS</summary>
        [Description("非法操作GNS")]
        GnsOperationError = 10,

        /// <summary>节点忙，指本节点存储队列满或其他原因，暂时不能提供服务的情况</summary>
        [Description("节点忙，指本节点存储队列满或其他原因，暂时不能提供服务的情况")]
        NodeBusy = 11,

        /// <summary>目的地址不可达，指路由表存在路由且消息路由正确但被路由的节点暂时不能提供服务的情况</summary>
        [Description("目的地址不可达，指路由表存在路由且消息路由正确但被路由的节点暂时不能提供服务的情况")]
        NodeCanNotReachable = 21,

        /// <summary>路由错，指路由表存在路由但消息路由出错的情况，如转错SMG等</summary>
        [Description("路由错，指路由表存在路由但消息路由出错的情况，如转错SMG等")]
        RouteError = 22,

        /// <summary>路由不存在，指消息路由的节点在路由表中不存在</summary>
        [Description("路由不存在，指消息路由的节点在路由表中不存在")]
        RoutNodeNotExisted = 23,

        /// <summary>计费号码无效，鉴权不成功时反馈的错误信息</summary>
        [Description("计费号码无效，鉴权不成功时反馈的错误信息")]
        FeeNumberError = 24,

        /// <summary>用户不能通信（如不在服务区、未开机等情况）</summary>
        [Description("用户不能通信（如不在服务区、未开机等情况）")]
        UserCanNotReachable = 25,

        /// <summary>手机内存不足</summary>
        [Description("手机内存不足")]
        HandsetFull = 26,

        /// <summary>手机不支持短消息</summary>
        [Description("手机不支持短消息")]
        HandsetCanNotRecvSms = 27,

        /// <summary>手机接收短消息出现错误</summary>
        [Description("手机接收短消息出现错误")]
        HandsetReturnError = 28,

        /// <summary>不知道的用户</summary>
        [Description("不知道的用户")]
        UnknownUser = 29,

        /// <summary>不提供此功能</summary>
        [Description("不提供此功能")]
        NoDevice = 30,

        /// <summary>非法设备</summary>
        [Description("非法设备")]
        InvalidateDevice = 31,

        /// <summary>系统失败（一般指系统消息队列满）</summary>
        [Description("系统失败（一般指系统消息队列满）")]
        SystemError = 32,

        /// <summary>超过流量限制，指发送方在一秒内的流量已经达到限制，拒绝发送</summary>
        [Description("超过流量限制，指发送方在一秒内的流量已经达到限制，拒绝发送")]
        FullSequence = 33,

        /// <summary>未知错误</summary>
        [Description("未知错误")]
        OtherError = 99,
    }
}