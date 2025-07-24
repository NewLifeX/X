﻿using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace NewLife.Serialization;

/// <summary>颜色处理器。</summary>
public class BinaryColor : BinaryHandlerBase
{
    /// <summary>实例化</summary>
    public BinaryColor() => Priority = 50;

    /// <summary>写入对象</summary>
    /// <param name="value">目标对象</param>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public override Boolean Write(Object? value, Type type)
    {
        if (type != typeof(Color)) return false;

        var color = (Color)(value ?? Color.Empty);
        WriteLog("WriteColor {0}", color);

        Host.Write(color.A);
        Host.Write(color.R);
        Host.Write(color.G);
        Host.Write(color.B);

        return true;
    }

    /// <summary>尝试读取指定类型对象</summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override Boolean TryRead(Type type, ref Object? value)
    {
        if (type != typeof(Color)) return false;

        var buf = Host.ReadBytes(4);
        if (buf.Length < 4) return false;

        var a = buf[0];
        var r = buf[1];
        var g = buf[2];
        var b = buf[3];
        var color = Color.FromArgb(a, r, g, b);
        WriteLog("ReadColor {0}", color);
        value = color;

        return true;
    }
}