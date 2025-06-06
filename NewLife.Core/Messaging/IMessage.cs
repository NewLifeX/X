﻿using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Messaging;

/// <summary>消息命令</summary>
public interface IMessage : IDisposable
{
    /// <summary>是否响应</summary>
    Boolean Reply { get; }

    /// <summary>是否有错</summary>
    Boolean Error { get; set; }

    /// <summary>单向请求</summary>
    Boolean OneWay { get; }

    /// <summary>负载数据</summary>
    IPacket? Payload { get; set; }

    /// <summary>根据请求创建配对的响应消息</summary>
    /// <returns></returns>
    IMessage? CreateReply();

    /// <summary>从数据包中读取消息</summary>
    /// <param name="pk"></param>
    /// <returns>是否成功</returns>
    Boolean Read(IPacket pk);

    /// <summary>把消息转为封包</summary>
    /// <returns></returns>
    IPacket? ToPacket();
}

/// <summary>消息命令基类</summary>
public class Message : IMessage
{
    /// <summary>是否响应</summary>
    public Boolean Reply { get; set; }

    /// <summary>是否有错</summary>
    public Boolean Error { get; set; }

    /// <summary>单向请求</summary>
    public Boolean OneWay { get; set; }

    /// <summary>负载数据</summary>
    public IPacket? Payload { get; set; }

    #region 构造
    /// <summary>销毁。回收数据包到内存池</summary>
    public void Dispose() => Payload.TryDispose();
    #endregion

    /// <summary>根据请求创建配对的响应消息</summary>
    /// <returns></returns>
    public virtual IMessage CreateReply()
    {
        if (Reply) throw new Exception("Cannot create response message based on response message");

        var msg = GetType().CreateInstance() as Message;
        if (msg == null) throw new InvalidDataException($"Cannot create an instance of type [{GetType().FullName}]");

        msg.Reply = true;

        return msg;
    }

    /// <summary>从数据包中读取消息</summary>
    /// <param name="pk"></param>
    /// <returns>是否成功</returns>
    public virtual Boolean Read(IPacket pk)
    {
        Payload = pk;

        return true;
    }

    /// <summary>把消息转为封包</summary>
    /// <returns></returns>
    public virtual IPacket? ToPacket() => Payload;
}