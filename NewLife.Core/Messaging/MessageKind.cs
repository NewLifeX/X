using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Messaging
{
    /// <summary>消息类型</summary>
    /// <remarks>定义一些常用的消息类型，其它用户自定义类型用数字代替</remarks>
    public enum MessageKind : byte
    {
        /// <summary>空消息</summary>
        Null,

        /// <summary>指定长度的字节数据消息</summary>
        Data,

        /// <summary>指定类型的实体对象消息</summary>
        Entity,

        /// <summary>指定类型的实体对象数组消息</summary>
        Entities,

        /// <summary>异常消息</summary>
        Exception,

        /// <summary>经过压缩的消息</summary>
        Compression,

        /// <summary>远程方法调用消息</summary>
        Method,

        /// <summary>用户自定义消息在此基础上增加</summary>
        UserDefine = 0x10
    }
}