namespace NewLife.Algorithms;

/// <summary>
/// 对齐模型。数据采样生成代表点时的 X 轴（时间）对齐策略。
/// </summary>
public enum AlignModes
{
    /// <summary>
    /// 不对齐，取桶起始点时间（或原始值），默认策略
    /// </summary>
    None,

    /// <summary>
    /// 左对齐：使用桶起始数据点时间
    /// </summary>
    Left,

    /// <summary>
    /// 中间对齐：取桶内中间位置（四舍五入）对应的数据点时间
    /// </summary>
    Center,

    /// <summary>
    /// 右对齐：使用桶结束前一个数据点时间
    /// </summary>
    Right,
}