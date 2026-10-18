namespace System.Diagnostics.CodeAnalysis;

// 低版本元数据减重：本文件仅被低版本 TFM 编译，而这些注解在低版本没有裁剪器/分析器消费，
// 使用点纯属元数据负担（实测每个低版本 DLL 约 9~10 KB，主要是 UnconditionalSuppressMessage 的说明串）。
// 各注解类加 [Conditional(镜像符号)] 后，符号未定义（低版本编译时必然如此，与本文件 #if 守卫互为镜像）
// 时使用点自动不进入元数据，源码使用点零改动；高版本 TFM 不编译本文件（经 CodeAnalysisForwards 转发 BCL），
// 注解原样保留供裁剪/AOT 使用。与 BCL 的 SuppressMessageAttribute（[Conditional("CODE_ANALYSIS")]）同源；
// 若未来低版本需要保留这些注解，删除对应 Conditional 行即可恢复。
#if NETFRAMEWORK || NETSTANDARD
/// <summary>指定反射访问所需的成员类型，供裁剪器/AOT 编译器保留对应元数据。BCL 从 net5 起提供，低版本在此补齐</summary>
[Flags]
public enum DynamicallyAccessedMemberTypes
{
    /// <summary>无</summary>
    None = 0,

    /// <summary>公开无参构造函数</summary>
    PublicParameterlessConstructor = 0x0001,

    /// <summary>公开构造函数</summary>
    PublicConstructors = 0x0003,

    /// <summary>非公开构造函数</summary>
    NonPublicConstructors = 0x0004,

    /// <summary>公开方法</summary>
    PublicMethods = 0x0008,

    /// <summary>非公开方法</summary>
    NonPublicMethods = 0x0010,

    /// <summary>公开字段</summary>
    PublicFields = 0x0020,

    /// <summary>非公开字段</summary>
    NonPublicFields = 0x0040,

    /// <summary>公开嵌套类型</summary>
    PublicNestedTypes = 0x0080,

    /// <summary>非公开嵌套类型</summary>
    NonPublicNestedTypes = 0x0100,

    /// <summary>公开属性</summary>
    PublicProperties = 0x0200,

    /// <summary>非公开属性</summary>
    NonPublicProperties = 0x0400,

    /// <summary>公开事件</summary>
    PublicEvents = 0x0800,

    /// <summary>非公开事件</summary>
    NonPublicEvents = 0x1000,

    /// <summary>接口</summary>
    Interfaces = 0x2000,

    /// <summary>全部成员</summary>
    All = ~None,
}

/// <summary>指示某个类型值会被反射访问指定成员，裁剪器据此保留元数据</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method, Inherited = false)]
[System.Diagnostics.Conditional("NET5_0_OR_GREATER")]
public sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    /// <summary>需要保留的成员类型</summary>
    public DynamicallyAccessedMemberTypes MemberTypes { get; }

    /// <summary>实例化</summary>
    /// <param name="memberTypes">需要保留的成员类型</param>
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) => MemberTypes = memberTypes;
}

/// <summary>指示该方法或类型在裁剪后可能失效（依赖被裁剪的代码），调用方需自行评估</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
[System.Diagnostics.Conditional("NET5_0_OR_GREATER")]
public sealed class RequiresUnreferencedCodeAttribute : Attribute
{
    /// <summary>说明消息</summary>
    public String Message { get; }

    /// <summary>相关文档地址</summary>
    public String? Url { get; set; }

    /// <summary>实例化</summary>
    /// <param name="message">说明消息</param>
    public RequiresUnreferencedCodeAttribute(String message) => Message = message;
}

/// <summary>无条件抑制指定的分析告警，且不依赖 SuppressMessage 的字符串条件</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
[System.Diagnostics.Conditional("NET5_0_OR_GREATER")]
public sealed class UnconditionalSuppressMessageAttribute : Attribute
{
    /// <summary>分类</summary>
    public String Category { get; }

    /// <summary>检查项编号</summary>
    public String CheckId { get; }

    /// <summary>作用范围</summary>
    public String? Scope { get; set; }

    /// <summary>作用目标</summary>
    public String? Target { get; set; }

    /// <summary>消息编号</summary>
    public String? MessageId { get; set; }

    /// <summary>抑制理由</summary>
    public String? Justification { get; set; }

    /// <summary>实例化</summary>
    /// <param name="category">分类</param>
    /// <param name="checkId">检查项编号</param>
    public UnconditionalSuppressMessageAttribute(String category, String checkId)
    {
        Category = category;
        CheckId = checkId;
    }
}
/// <summary>声明对指定类型成员的依赖，裁剪器/AOT 编译器据此保留元数据。适用于成员访问目标无法静态关联到调用点的场景（如运行时未知的泛型实例）</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[System.Diagnostics.Conditional("NET5_0_OR_GREATER")]
public sealed class DynamicDependencyAttribute : Attribute
{
    /// <summary>成员签名</summary>
    public String? MemberSignature { get; }

    /// <summary>需要保留的成员类型</summary>
    public DynamicallyAccessedMemberTypes MemberTypes { get; }

    /// <summary>类型</summary>
    public Type? Type { get; }

    /// <summary>类型名称</summary>
    public String? TypeName { get; }

    /// <summary>程序集名称</summary>
    public String? AssemblyName { get; set; }

    /// <summary>条件</summary>
    public String? Condition { get; set; }

    /// <summary>实例化。按成员签名声明</summary>
    /// <param name="memberSignature">成员签名</param>
    public DynamicDependencyAttribute(String memberSignature) => MemberSignature = memberSignature;

    /// <summary>实例化。按类型与成员类别声明</summary>
    /// <param name="memberTypes">需要保留的成员类型</param>
    /// <param name="type">类型</param>
    public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, Type type)
    {
        MemberTypes = memberTypes;
        Type = type;
    }

    /// <summary>实例化。按类型名称与成员签名声明</summary>
    /// <param name="memberTypes">需要保留的成员类型</param>
    /// <param name="typeName">类型名称</param>
    /// <param name="memberSignature">成员签名</param>
    public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, String typeName, String memberSignature)
    {
        MemberTypes = memberTypes;
        TypeName = typeName;
        MemberSignature = memberSignature;
    }
}
#endif

// RequiresDynamicCode 从 net7 起才进入 BCL，net6 也需补齐；
// DAM/RUC/USM 从 net5 起为 BCL 自带，不可在 net5+ 重复定义
#if NETFRAMEWORK || NETSTANDARD || NET6_0
/// <summary>指示该方法依赖运行时代码生成，NativeAOT 下不可用</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
[System.Diagnostics.Conditional("NET7_0_OR_GREATER")]
public sealed class RequiresDynamicCodeAttribute : Attribute
{
    /// <summary>说明消息</summary>
    public String Message { get; }

    /// <summary>相关文档地址</summary>
    public String? Url { get; set; }

    /// <summary>实例化</summary>
    /// <param name="message">说明消息</param>
    public RequiresDynamicCodeAttribute(String message) => Message = message;
}
#endif
