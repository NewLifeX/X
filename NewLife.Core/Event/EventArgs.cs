using System.Runtime.InteropServices;

namespace NewLife;

/// <summary>泛型事件参数</summary>
/// <typeparam name="TArg"></typeparam>
[Serializable]
[ComVisible(true)]
public class EventArgs<TArg> : EventArgs
{
    /// <summary>参数</summary>
    public TArg Arg { get; set; }

    /// <summary>使用参数初始化</summary>
    /// <param name="arg"></param>
    public EventArgs(TArg arg) => Arg = arg;

    /// <summary>弹出</summary>
    /// <param name="arg"></param>
    public void Pop(ref TArg arg) => arg = Arg;
}

/// <summary>泛型事件参数</summary>
/// <typeparam name="TArg1"></typeparam>
/// <typeparam name="TArg2"></typeparam>
public class EventArgs<TArg1, TArg2> : EventArgs
{
    /// <summary>参数</summary>
    public TArg1 Arg1 { get; set; }

    /// <summary>参数2</summary>
    public TArg2 Arg2 { get; set; }

    /// <summary>使用参数初始化</summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    public EventArgs(TArg1 arg1, TArg2 arg2)
    {
        Arg1 = arg1;
        Arg2 = arg2;
    }

    /// <summary>弹出</summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    public void Pop(ref TArg1 arg1, ref TArg2 arg2)
    {
        arg1 = Arg1;
        arg2 = Arg2;
    }
}

/// <summary>泛型事件参数</summary>
/// <typeparam name="TArg1"></typeparam>
/// <typeparam name="TArg2"></typeparam>
/// <typeparam name="TArg3"></typeparam>
public class EventArgs<TArg1, TArg2, TArg3> : EventArgs
{
    /// <summary>参数</summary>
    public TArg1 Arg1 { get; set; }

    /// <summary>参数2</summary>
    public TArg2 Arg2 { get; set; }

    /// <summary>参数3</summary>
    public TArg3 Arg3 { get; set; }

    /// <summary>使用参数初始化</summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    public EventArgs(TArg1 arg1, TArg2 arg2, TArg3 arg3)
    {
        Arg1 = arg1;
        Arg2 = arg2;
        Arg3 = arg3;
    }

    /// <summary>弹出</summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    public void Pop(ref TArg1 arg1, ref TArg2 arg2, ref TArg3 arg3)
    {
        arg1 = Arg1;
        arg2 = Arg2;
        arg3 = Arg3;
    }
}

/// <summary>泛型事件参数</summary>
/// <typeparam name="TArg1"></typeparam>
/// <typeparam name="TArg2"></typeparam>
/// <typeparam name="TArg3"></typeparam>
/// <typeparam name="TArg4"></typeparam>
public class EventArgs<TArg1, TArg2, TArg3, TArg4> : EventArgs
{
    /// <summary>参数</summary>
    public TArg1 Arg1 { get; set; }

    /// <summary>参数2</summary>
    public TArg2 Arg2 { get; set; }

    /// <summary>参数3</summary>
    public TArg3 Arg3 { get; set; }

    /// <summary>参数4</summary>
    public TArg4 Arg4 { get; set; }

    /// <summary>使用参数初始化</summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    public EventArgs(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
    {
        Arg1 = arg1;
        Arg2 = arg2;
        Arg3 = arg3;
        Arg4 = arg4;
    }

    /// <summary>弹出</summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    public void Pop(ref TArg1 arg1, ref TArg2 arg2, ref TArg3 arg3, ref TArg4 arg4)
    {
        arg1 = Arg1;
        arg2 = Arg2;
        arg3 = Arg3;
        arg4 = Arg4;
    }
}