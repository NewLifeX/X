using Xunit;

namespace XUnitTest.Log;

/// <summary>遥测全局态用例集合：挂载/卸载进程级活动监听器的用例必须与其它用例串行</summary>
/// <remarks>
/// <para>ActivitySource 的监听器注册表是进程级全局设施：采集器用例挂上"监听全部源"的监听器后，
/// 任意源的 <c>HasListeners()</c> 都会短暂为真，并行执行的 OTel 跟踪器用例会把"无监听者"路径测成有监听者
/// （表现为 NewSpan 返回空实现与真实片段的错乱）。</para>
/// <para>与 GlobalState 集合同一机制：xUnit 会把本集合安排在并行集合之后串行执行。</para>
/// </remarks>
[CollectionDefinition("Telemetry", DisableParallelization = true)]
public class TelemetryTestCollection
{
    // 此类仅用于定义集合，不包含实际测试方法
}
