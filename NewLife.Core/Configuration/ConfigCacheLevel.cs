namespace NewLife.Configuration;

/// <summary>配置数据缓存等级</summary>
/// <remarks>用于控制 HTTP 配置提供者的本地缓存策略</remarks>
public enum ConfigCacheLevel
{
    /// <summary>不缓存</summary>
    NoCache,

    /// <summary>Json格式缓存</summary>
    Json,

    /// <summary>加密缓存</summary>
    Encrypted,
}