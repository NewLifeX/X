﻿using System.Text;

namespace NewLife.Serialization;

/// <summary>IJson序列化接口</summary>
public interface IJson : IFormatterX
{
    #region 属性
    /// <summary>是否缩进</summary>
    Boolean Indented { get; set; }

    /// <summary>处理器列表</summary>
    IList<IJsonHandler> Handlers { get; }
    #endregion

    #region 写入
    /// <summary>写入字符串</summary>
    /// <param name="value"></param>
    void Write(String value);

    /// <summary>写入</summary>
    /// <param name="sb"></param>
    /// <param name="value"></param>
    void Write(StringBuilder sb, Object value);
    #endregion

    #region 读取
    /// <summary>读取</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    Boolean Read(String value);

    /// <summary>读取字节</summary>
    /// <returns></returns>
    Byte ReadByte();
    #endregion
}

/// <summary>IJson读写处理器接口</summary>
public interface IJsonHandler : IHandler<IJson>
{
    /// <summary>获取对象的Json字符串表示形式。</summary>
    /// <param name="value"></param>
    /// <returns>返回null表示不支持</returns>
    String GetString(Object value);
}

/// <summary>IJson读写处理器基类</summary>
public abstract class JsonHandlerBase : HandlerBase<IJson, IJsonHandler>, IJsonHandler
{
    /// <summary>获取对象的Json字符串表示形式。</summary>
    /// <param name="value"></param>
    /// <returns>返回null表示不支持</returns>
    public virtual String GetString(Object value) => null;

    /// <summary>写入一个对象</summary>
    /// <param name="value">目标对象</param>
    /// <param name="type">类型</param>
    /// <returns>是否处理成功</returns>
    public override Boolean Write(Object value, Type type)
    {
        var v = GetString(value);
        if (v == null) return false;

        Host.Write(v);

        return true;
    }
}