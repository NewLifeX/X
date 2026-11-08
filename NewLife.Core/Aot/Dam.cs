using System.Diagnostics.CodeAnalysis;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace NewLife;

/// <summary>AOT 保留需求预设</summary>
/// <remarks>
/// <see cref="DynamicallyAccessedMembersAttribute"/> 的实参预设，按语义意图命名，避免各处重复书写标志位组合。
/// 常量值在编译期直接写入特性元数据，与内联书写完全等价，裁剪器与静态分析器均可正确识别。
/// 调整某类保留集合只需修改此处，全库生效。
/// </remarks>
internal static class Dam
{
    /// <summary>序列化目标：公开属性 + 公开字段 + 公开无参构造</summary>
    public const DynamicallyAccessedMemberTypes Serialize = PublicProperties | PublicFields | PublicParameterlessConstructor;

    /// <summary>成员枚举：公开属性 + 公开字段</summary>
    public const DynamicallyAccessedMemberTypes Members = PublicProperties | PublicFields;

    /// <summary>属性与方法：公开属性 + 公开方法</summary>
    public const DynamicallyAccessedMemberTypes PropertiesAndMethods = PublicProperties | PublicMethods;

    /// <summary>公开构造函数</summary>
    public const DynamicallyAccessedMemberTypes Constructors = PublicConstructors;

    /// <summary>控制器/处理器类型：公开构造函数 + 公开方法</summary>
    public const DynamicallyAccessedMemberTypes Handlers = PublicConstructors | PublicMethods;

    /// <summary>仅公开无参构造</summary>
    public const DynamicallyAccessedMemberTypes Create = PublicParameterlessConstructor;

    /// <summary>方法查找含非公开：公开方法 + 非公开方法</summary>
    public const DynamicallyAccessedMemberTypes AllMethods = PublicMethods | NonPublicMethods;

    /// <summary>属性查找含非公开：公开属性 + 非公开属性</summary>
    public const DynamicallyAccessedMemberTypes AllProperties = PublicProperties | NonPublicProperties;

    /// <summary>实例创建含非公开：公开无参构造 + 非公开构造</summary>
    public const DynamicallyAccessedMemberTypes AllConstructors = PublicParameterlessConstructor | NonPublicConstructors;
}