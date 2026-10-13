using Xunit;

namespace XUnitTest.IO;

/// <summary>IO测试集合定义，控制并发数避免磁盘IO冲突</summary>
[CollectionDefinition("IO", DisableParallelization = false)]
public class IOTestCollection
{
    // 此类仅用于定义集合，不包含实际测试方法
    // xUnit会自动管理同一Collection内的测试并发度
}

/// <summary>改写进程级全局状态的用例集合：与其它用例串行执行</summary>
/// <remarks>
/// 这类用例会临时改写 <c>PathHelper.BasePath</c> 等全局静态，并行执行期间其它用例按相对路径读写文件会落到错误目录
/// （表现为“文件明明刚写过却读不到”，例如配置用例读到默认值）。xUnit 会把本集合安排在并行集合之后串行执行。
/// </remarks>
[CollectionDefinition("GlobalState", DisableParallelization = true)]
public class GlobalStateTestCollection
{
    // 此类仅用于定义集合，不包含实际测试方法
}
