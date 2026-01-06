namespace NewLife.Configuration;

/// <summary>获取配置委托</summary>
/// <remarks>便于集成配置中心，用于按键获取配置值</remarks>
/// <param name="key">配置键名，支持冒号分隔的多级名称</param>
/// <returns>对应的配置值；未找到时返回 null</returns>
public delegate String? GetConfigCallback(String key);