namespace NewLife.Configuration;

/// <summary>配置特性</summary>
/// <remarks>
/// 声明配置模型使用哪一种配置提供者，以及所需要的文件名和分类名。
/// 如未指定提供者，则使用全局默认，此时将根据全局代码配置或环境变量配置使用不同提供者，实现配置信息整体转移。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ConfigAttribute : Attribute
{
    /// <summary>提供者。内置ini/xml/json/http，一般不指定，使用全局默认</summary>
    public String? Provider { get; set; }

    /// <summary>配置名。可以是文件名或分类名</summary>
    public String Name { get; set; }

    /// <summary>指定配置名</summary>
    /// <param name="name">配置名。可以是文件名或分类名</param>
    /// <param name="provider">提供者。内置ini/xml/json/http，一般不指定，使用全局默认</param>
    public ConfigAttribute(String name, String? provider = null)
    {
        Provider = provider;
        Name = name;
    }
}

/// <summary>Http配置特性</summary>
/// <remarks>
/// 声明配置模型使用Http配置提供者对接远程配置中心。
/// 支持星尘配置中心、阿波罗配置中心等远程配置服务。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class HttpConfigAttribute : ConfigAttribute
{
    /// <summary>服务器地址。配置中心的HTTP地址</summary>
    public String Server { get; set; }

    /// <summary>服务操作。获取配置的API路径，默认Config/GetAll</summary>
    public String Action { get; set; }

    /// <summary>应用标识。在配置中心注册的应用ID</summary>
    public String AppId { get; set; }

    /// <summary>应用密钥。用于身份验证</summary>
    public String? Secret { get; set; }

    /// <summary>作用域。获取指定作用域下的配置值，如：生产、开发、测试等</summary>
    public String? Scope { get; set; }

    /// <summary>本地缓存等级。即使网络断开仍能加载本地数据，默认Encrypted</summary>
    public ConfigCacheLevel CacheLevel { get; set; }

    /// <summary>指定Http配置</summary>
    /// <param name="name">配置名。可以是文件名或分类名</param>
    /// <param name="server">服务器地址</param>
    /// <param name="action">服务操作</param>
    /// <param name="appId">应用标识</param>
    /// <param name="secret">应用密钥</param>
    /// <param name="scope">作用域。获取指定作用域下的配置值，生产、开发、测试等</param>
    /// <param name="cacheLevel">本地缓存配置数据。即使网络断开，仍然能够加载使用本地数据，默认Encrypted</param>
    public HttpConfigAttribute(String name, String server, String action, String appId, String? secret = null, String? scope = null, ConfigCacheLevel cacheLevel = ConfigCacheLevel.Encrypted) : base(name, "http")
    {
        Server = server;
        Action = action;
        AppId = appId;
        Secret = secret;
        Scope = scope;
        CacheLevel = cacheLevel;
    }
}