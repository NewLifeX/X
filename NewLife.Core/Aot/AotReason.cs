namespace NewLife;

/// <summary>AOT 抑制理由分类</summary>
/// <remarks>
/// 同文重复的 <c>UnconditionalSuppressMessage</c> 备注收敛为常量，便于按边界性质审查；
/// 各具场景特异性的长文案仍保留在被标注处，不强行归类。
/// </remarks>
internal static class AotReason
{
    /// <summary>运行时类型反射边界；AOT 下不受支持，或应改用泛型入口</summary>
    public const String RuntimeTypeBoundary = "运行时类型反射/动态加载边界；AOT 下不受支持或需改用泛型入口（见分析稿 §4/§5）";

    /// <summary>针对特定目标的程序集保留声明；其它目标缺少该程序集属预期</summary>
    public const String AssemblyTargetDiff = "Android 专用 DynamicDependency，非 Android 目标缺少 Mono.Android 属预期，保留声明供 Android 消费端使用";
}