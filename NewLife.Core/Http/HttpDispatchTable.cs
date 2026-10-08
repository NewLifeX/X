using System.Diagnostics.CodeAnalysis;

namespace NewLife.Http;

/// <summary>Http 控制器静态分发表。源生成器为 <c>MapController&lt;T&gt;()</c> 注册的控制器预生成"方法名 → 强类型调用"委托，请求分发时优先命中，避免方法查找、参数绑定与调用的反射开销，NativeAOT 友好</summary>
/// <remarks>
/// 由 NewLife.Core 源生成器在编译期扫描控制器注册调用点后生成注册代码，仅收录可安全静态化的方法（公开、非泛型、参数可生成）；
/// 未注册的类型或未命中的方法名自动回退原反射路径，语义不变。用户代码也可手动调用 <see cref="RegisterController"/> 注册自定义分发器。
/// </remarks>
public static class HttpDispatchTable
{
    /// <summary>控制器方法调用委托</summary>
    /// <param name="controller">控制器实例</param>
    /// <param name="context">Http上下文</param>
    /// <returns>调用任务</returns>
    public delegate Task ControllerInvokeDelegate(Object controller, IHttpContext context);

    #region 属性
    private static readonly Object _lock = new();
    private static volatile Dictionary<Type, Dictionary<String, ControllerInvokeDelegate>> _controllers = [];
    #endregion

    #region 方法
    /// <summary>注册控制器的静态分发表</summary>
    /// <param name="controllerType">控制器类型。AOT 下其公开构造与公开方法将按需保留（运行时将由处理器实例化并分发）</param>
    /// <param name="dispatchers">方法名到调用委托的映射，方法名匹配不区分大小写</param>
    public static void RegisterController([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] Type controllerType, Dictionary<String, ControllerInvokeDelegate> dispatchers)
    {
        if (controllerType == null) throw new ArgumentNullException(nameof(controllerType));
        if (dispatchers == null) throw new ArgumentNullException(nameof(dispatchers));

        // 写时复制发布（lock 保证唯一写入，volatile 保证所有读者可见）；同一类型保持首次注册结果
        lock (_lock)
        {
            if (!_controllers.ContainsKey(controllerType))
                _controllers = new Dictionary<Type, Dictionary<String, ControllerInvokeDelegate>>(_controllers) { [controllerType] = dispatchers };
        }
    }

    /// <summary>尝试获取控制器的静态分发表</summary>
    /// <param name="controllerType">控制器类型</param>
    /// <returns>分发表，未注册时返回 null</returns>
    public static Dictionary<String, ControllerInvokeDelegate>? TryGet(Type controllerType) => _controllers.TryGetValue(controllerType, out var dispatchers) ? dispatchers : null;
    #endregion
}
