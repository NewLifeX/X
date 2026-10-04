using Xunit;

namespace XUnitTest.Net;

// 网络用例统一使用动态端口（Port=0）与独立的本地资源，彼此没有共享状态，
// 因此拆分为多个集合以便并行：xUnit 只并行“不同集合”，集合内串行。
// 此前单集合 + DisableParallelization=true 让整个网络模块独占测试进程，
// 其它模块（日志、缓存等）只能等它跑完，全量耗时几乎等于网络模块串行时间。
// 集合划分仅按耗时均衡，不代表功能分组，可随用例增删调整；
// 用例必须保持“动态端口 + 无跨类共享状态”，否则拆集会引入新的偶发失败。

/// <summary>网络测试集合 A：客户端会话、异步收发、拉取模式</summary>
[CollectionDefinition("Net.A")]
public class NetCollectionA
{
    // 此类仅用于定义集合，不包含实际测试方法
}

/// <summary>网络测试集合 B：服务端、超时清理、会话管道</summary>
[CollectionDefinition("Net.B")]
public class NetCollectionB
{
    // 此类仅用于定义集合，不包含实际测试方法
}

/// <summary>网络测试集合 C：边界、发送队列、UDP 会话</summary>
[CollectionDefinition("Net.C")]
public class NetCollectionC
{
    // 此类仅用于定义集合，不包含实际测试方法
}

/// <summary>网络测试集合 D：WebSocket、会话基类、健壮性</summary>
[CollectionDefinition("Net.D")]
public class NetCollectionD
{
    // 此类仅用于定义集合，不包含实际测试方法
}

/// <summary>网络测试集合（保留）：暂未并入分组的测试类使用，同样允许与其它集合并行</summary>
[CollectionDefinition("Net")]
public class NetTestCollection
{
    // 此类仅用于定义集合，不包含实际测试方法
}
