
namespace NewLife.Reflection
{
    /// <summary>没有参数和返回值的委托</summary>>
    public delegate void Func();

    /// <summary>具有指定类型返回的委托</summary>>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public delegate TResult Func<TResult>();

    /// <summary>具有指定参数和返回的委托</summary>>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="arg"></param>
    /// <returns></returns>
    public delegate TResult Func<T, TResult>(T arg);

    /// <summary>具有指定两个参数和返回的委托</summary>>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="arg"></param>
    /// <param name="arg2"></param>
    /// <returns></returns>
    public delegate TResult Func<T, T2, TResult>(T arg, T2 arg2);

    /// <summary>具有指定三个参数和返回的委托</summary>>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="arg"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <returns></returns>
    public delegate TResult Func<T, T2, T3, TResult>(T arg, T2 arg2, T3 arg3);

    /// <summary>具有指定四个参数和返回的委托</summary>>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="arg"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <returns></returns>
    public delegate TResult Func<T, T2, T3, T4, TResult>(T arg, T2 arg2, T3 arg3, T4 arg4);

    /// <summary>具有指定五个参数和返回的委托</summary>>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="arg"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <param name="arg5"></param>
    /// <returns></returns>
    public delegate TResult Func<T, T2, T3, T4, T5, TResult>(T arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
}
