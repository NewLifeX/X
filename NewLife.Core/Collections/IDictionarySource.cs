namespace NewLife.Collections;

/// <summary>
/// 字典数据源接口。定义该模型类支持输出名值字典，便于序列化传输
/// </summary>
public interface IDictionarySource
{
    /// <summary>
    /// 把对象转为名值字典，便于序列化传输。
    /// 实现方应返回可独立使用的快照字典，不应直接暴露内部可变集合引用。
    /// </summary>
    /// <returns>大小写敏感性由实现决定的字典实例，允许包含 null 值</returns>
    IDictionary<String, Object?> ToDictionary();
}