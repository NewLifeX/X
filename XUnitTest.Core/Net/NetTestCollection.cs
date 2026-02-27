using Xunit;

namespace XUnitTest.Net;

/// <summary>网络测试集合定义，同一集合内的测试类串行执行，避免端口和连接冲突</summary>
[CollectionDefinition("Net", DisableParallelization = true)]
public class NetTestCollection
{
    // 此类仅用于定义集合，不包含实际测试方法
}
