namespace NewLife.Log;

/// <summary>链路追踪功能接口</summary>
public interface ITracerFeature
{
    /// <summary>链路追踪</summary>
    ITracer? Tracer { get; set; }
}

/// <summary>携带链路追踪标识的消息接口</summary>
/// <remarks>
/// 用于约束消息类型必须具备链路追踪标识 <see cref="TraceId"/>，便于在分布式系统中进行调用链路的追踪和分析。
/// </remarks>
public interface ITraceMessage
{
    /// <summary>链路追踪标识</summary>
    String? TraceId { get; set; }
}
