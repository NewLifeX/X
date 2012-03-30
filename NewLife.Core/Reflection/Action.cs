using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Reflection
{
    ///// <summary>
    ///// 一个参数
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    ///// <param name="arg"></param>
    //public delegate void Action<T>(T arg);

    /// <summary>两个参数</summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="arg"></param>
    /// <param name="arg2"></param>
    public delegate void Action<T, T2>(T arg, T2 arg2);

    /// <summary>三个参数</summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <param name="arg"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    public delegate void Action<T, T2, T3>(T arg, T2 arg2, T3 arg3);

    /// <summary>四个参数</summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <param name="arg"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    public delegate void Action<T, T2, T3, T4>(T arg, T2 arg2, T3 arg3, T4 arg4);

}