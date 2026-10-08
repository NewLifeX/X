namespace NewLife.Model;

/// <summary>服务工厂表。源生成器为容器将在运行时构造的类型预生成无参构造工厂，容器解析时优先使用，避免反射构造与表达式编译，NativeAOT 友好</summary>
/// <remarks>
/// 由 NewLife.Core 源生成器在编译期扫描 <c>AddTransient&lt;TService, TImpl&gt;()</c> 等注册调用点后生成注册代码，
/// 仅收录"唯一公开无参构造"的实现类型，与非生成路径的构造语义一致。
/// 用户代码也可手动调用 <see cref="Register"/> 注册自定义工厂。
/// </remarks>
public static class ServiceFactoryTable
{
    #region 属性
    private static readonly Object _lock = new();
    private static volatile Dictionary<Type, Func<Object>> _factories = [];
    #endregion

    #region 方法
    /// <summary>注册类型工厂</summary>
    /// <param name="type">实现类型</param>
    /// <param name="factory">无参构造工厂</param>
    public static void Register(Type type, Func<Object> factory)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        // 写时复制发布（lock 保证唯一写入，volatile 保证所有读者可见）
        lock (_lock)
        {
            if (!_factories.ContainsKey(type))
                _factories = new Dictionary<Type, Func<Object>>(_factories) { [type] = factory };
        }
    }

    /// <summary>尝试获取类型工厂</summary>
    /// <param name="type">实现类型</param>
    /// <returns>工厂委托，未注册时返回 null</returns>
    public static Func<Object>? TryGet(Type type) => _factories.TryGetValue(type, out var factory) ? factory : null;
    #endregion
}
