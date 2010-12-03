using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.Common
{
    /// <summary>
    /// 没有参数和返回值的委托
    /// </summary>
    public delegate void Func();

    /// <summary>
    /// 具有指定类型返回的委托
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public delegate TResult Func<TResult>();

    //internal delegate void Func<T>(T arg);

    /// <summary>
    /// 具有指定参数和返回的委托
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="arg"></param>
    /// <returns></returns>
    public delegate TResult Func<T, TResult>(T arg);

    /// <summary>
    /// 具有指定两个参数和返回的委托
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="arg"></param>
    /// <param name="arg2"></param>
    /// <returns></returns>
    public delegate TResult Func<T, T2, TResult>(T arg, T2 arg2);
}
