namespace NewLife.Collections;

/// <summary>
/// 字典数据源接口。定义该模型类支持输出名值字典，便于序列化传输
/// </summary>
public interface IDictionarySource
{
    /// <summary>
    /// 把对象转为名值字典，便于序列化传输
    /// </summary>
    /// <returns></returns>
    IDictionary<String, Object?> ToDictionary();
}