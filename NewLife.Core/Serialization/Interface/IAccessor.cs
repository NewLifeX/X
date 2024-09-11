﻿using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>数据流序列化访问器。接口实现者可以在这里完全自定义序列化行为</summary>
public interface IAccessor
{
    /// <summary>从数据流中读取消息</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    /// <returns>是否成功</returns>
    Boolean Read(Stream stream, Object? context);

    /// <summary>把消息写入到数据流中</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    /// <returns>是否成功</returns>
    Boolean Write(Stream stream, Object? context);
}

/// <summary>自定义数据序列化访问器。数据T支持Span/Memory等，接口实现者可以在这里完全自定义序列化行为</summary>
public interface IAccessor<T>
{
    /// <summary>从数据中读取消息</summary>
    /// <param name="data">数据</param>
    /// <param name="context">上下文</param>
    /// <returns>是否成功</returns>
    Boolean Read(T data, Object? context);

    /// <summary>把消息写入到数据中</summary>
    /// <param name="data">数据</param>
    /// <param name="context">上下文</param>
    /// <returns>是否成功</returns>
    Boolean Write(T data, Object? context);
}

/// <summary>访问器助手</summary>
public static class AccessorHelper
{
    /// <summary>支持访问器的对象转数据包</summary>
    /// <param name="accessor">访问器</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    public static IPacket ToPacket(this IAccessor accessor, Object? context = null)
    {
        var ms = new MemoryStream { Position = 8 };
        accessor.Write(ms, context);

        ms.Position = 8;

        // 包装为数据包，直接窃取内存流内部的缓冲区
        return new ArrayPacket(ms);
    }

    /// <summary>通过访问器读取</summary>
    /// <param name="type"></param>
    /// <param name="pk"></param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    public static Object? AccessorRead(this Type type, IPacket pk, Object? context = null)
    {
        var obj = type.CreateInstance();
        if (obj is IAccessor accessor)
            accessor.Read(pk.GetStream(), context);

        return obj;
    }

    /// <summary>通过访问器转换数据包为实体对象</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pk"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static T ToEntity<T>(this IPacket pk, Object? context = null) where T : IAccessor, new()
    {
        //if (!typeof(T).As<IAccessor>()) return default(T);

        var obj = new T();
        obj.Read(pk.GetStream(), context);

        return obj;
    }
}