namespace NewLife.Data;

/// <summary>模型数据接口，支持通过键索引读写“模型属性”</summary>
/// <remarks>
/// 目的：在不直接使用反射的情况下，以字符串键访问模型属性，常用于 WebApi 模型、XCode 实体、对象拷贝与绑定。
/// 说明：实现者应将键与公开属性名（或逻辑名）映射，避免高频场景中频繁反射；可以通过缓存或源码生成优化。
/// 建议：对缺失键的读取返回 null；写入缺失键时由实现自行决定是忽略、创建还是抛出异常，但应在实现类文档中说明。
/// 关系：大部分场景逐步替代 <see cref="IExtend"/>；<see cref="IExtend"/> 适合“临时扩展字段”，而本接口面向“模型属性”。
/// </remarks>
public interface IModel
{
    /// <summary>设置 或 获取 模型数据项</summary>
    /// <param name="key">属性名或逻辑键</param>
    /// <returns>存在则返回属性值；未命中返回 null（推荐约定）</returns>
    Object? this[String key] { get; set; }
}