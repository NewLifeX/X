namespace NewLife.Log;

/// <summary>日志功能接口</summary>
public interface ITracerFeature
{
    /// <summary>性能追踪</summary>
    ITracer Tracer { get; set; }
}