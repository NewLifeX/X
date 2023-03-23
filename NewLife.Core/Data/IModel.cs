namespace NewLife.Data;

/// <summary>模型数据接口，支持索引器读写属性</summary>
/// <remarks>
/// 可借助反射取得属性列表成员，从而对实体模型属性进行读写操作，避免反射带来的负担。
/// 常用于WebApi模型类以及XCode数据实体类，也用于魔方接口拷贝。
/// 
/// 逐步替代 IExtend 的大部分使用场景
/// </remarks>
public interface IModel
{
    /// <summary>设置 或 获取 数据项</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Object this[String key] { get; set; }
}