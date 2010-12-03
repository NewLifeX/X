
namespace NewLife.Threading
{
    /// <summary>
    /// 任务状态
    /// </summary>
    public enum TaskState
    {
        /// <summary>
        /// 未处理
        /// </summary>
        Unstarted = 0,

        /// <summary>
        /// 正在处理
        /// </summary>
        Running = 1,

        /// <summary>
        /// 已完成
        /// </summary>
        Finished = 2
    }
}
