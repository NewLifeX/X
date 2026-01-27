using Xunit;

namespace XUnitTest.IO;

/// <summary>IO测试集合定义，控制并发数避免磁盘IO冲突</summary>
[CollectionDefinition("IO", DisableParallelization = false)]
public class IOTestCollection
{
    // 此类仅用于定义集合，不包含实际测试方法
    // xUnit会自动管理同一Collection内的测试并发度
}
