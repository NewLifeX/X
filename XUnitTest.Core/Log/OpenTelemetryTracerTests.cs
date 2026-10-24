using System.ComponentModel;
using System.Diagnostics;
using NewLife;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

[Collection("Telemetry")]
[DisplayName("OpenTelemetry跟踪器")]
public class OpenTelemetryTracerTests
{
    /// <summary>记录型底盘跟踪器，用于观察内层片段接收到的标识</summary>
    private sealed class RecordingTracer : ITracer
    {
        public ISpan? Last { get; private set; }

        public Int32 Period { get; set; }
        public Int32 MaxSamples { get; set; } = 1;
        public Int32 MaxErrors { get; set; } = 10;
        public Int32 Timeout { get; set; } = 15000;
        public Int32 MaxTagLength { get; set; } = 1024;
        public String? AttachParameter { get; set; }
        public ITracerResolver Resolver { get; set; } = new DefaultTracerResolver();

        public ISpanBuilder BuildSpan(String name) => new DefaultSpanBuilder(this, name);

        public ISpan NewSpan(String name)
        {
            var span = new DefaultSpan(this) { Name = name };
            span.Start();

            return Last = span;
        }

        public ISpan NewSpan(String name, Object? tag) => NewSpan(name);

        public ISpanBuilder[] TakeAll() => [];
    }

    /// <summary>订阅指定源的活动</summary>
    private static ActivityListener Listen(String sourceName, List<Activity> started, List<Activity> stopped)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> o) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => started.Add(a),
            ActivityStopped = a => stopped.Add(a),
        };
        ActivitySource.AddActivityListener(listener);

        return listener;
    }

    [Fact]
    [DisplayName("无监听者_返回空实现且不污染当前片段")]
    public void NoListenerReturnsEmpty()
    {
        var tracer = new OpenTelemetryTracer { SourceName = "XUnitTest.NoListener" };

        Assert.False(tracer.IsActive);

        using var span = tracer.NewSpan("空埋点", new { Id = 1 });

        // 空实现不携带任何标识，也不会成为当前片段
        Assert.True(span.Id.IsNullOrEmpty());
        Assert.True(span.TraceId.IsNullOrEmpty());
        Assert.Null(DefaultSpan.Current);

        // 所有写入操作都是安全的
        span.Tag = "任意数据";
        span.Value = 123;
        span.Error = "错误";
        span.SetError(new InvalidOperationException("异常"), "附加");
        span.Abandon();
    }

    [Fact]
    [DisplayName("无监听者_Http埋点解析直接返回空")]
    public void NoListenerResolverReturnsNull()
    {
        var tracer = new OpenTelemetryTracer { SourceName = "XUnitTest.NoListener2" };

        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "http://sso.newlifex.com/user/query?id=1");
        var span = tracer.NewSpan(request);

        Assert.Null(span);
        // 没有埋点就不应该注入链路头部，避免无谓的请求头污染
        Assert.False(request.Headers.Contains("traceparent"));
    }

    [Fact]
    [DisplayName("有监听者_Http埋点注入W3C标准traceparent")]
    public void ListenerInjectsTraceparent()
    {
        const String source = "XUnitTest.Otel.Attach";
        using var listener = Listen(source, [], []);

        var tracer = new OpenTelemetryTracer { SourceName = source };

        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "http://sso.newlifex.com/user/query?id=1");
        using var span = tracer.NewSpan(request);
        Assert.NotNull(span);

        // 出站请求必须带上本片段的四段式W3C标识，否则下游会另起一条链路
        var values = request.Headers.TryGetValues("traceparent", out var vs) ? vs : null;
        var value = values == null ? "" : String.Join(",", values);
        Assert.Equal(span!.ToString(), value);

        var ss = value.Split('-');
        Assert.Equal(4, ss.Length);
        Assert.Equal(32, ss[1].Length);
        Assert.Equal(16, ss[2].Length);
        Assert.Equal(span.TraceId, ss[1]);
        Assert.Equal(span.Id, ss[2]);
    }

    [Fact]
    [DisplayName("无监听者但有底盘_注入底盘片段的W3C标识")]
    public void InnerOnlyInjectsTraceparent()
    {
        var inner = new RecordingTracer();
        var tracer = new OpenTelemetryTracer { SourceName = "XUnitTest.Otel.AttachInner", Inner = inner };

        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "http://sso.newlifex.com/user/query?id=1");
        using var span = tracer.NewSpan(request);
        Assert.NotNull(span);

        // 无活动时以底盘片段为准，保证出站头与星尘链路一致
        var values = request.Headers.TryGetValues("traceparent", out var vs) ? vs : null;
        var value = values == null ? "" : String.Join(",", values);

        var ss = value.Split('-');
        Assert.Equal(4, ss.Length);
        Assert.Equal(inner.Last!.TraceId, ss[1]);
        Assert.Equal(inner.Last.Id, ss[2]);
    }

    [Fact]
    [DisplayName("有监听者_产出标准活动并映射标签与错误")]
    public void ListenerProducesActivity()
    {
        const String source = "XUnitTest.Produce";
        var started = new List<Activity>();
        var stopped = new List<Activity>();
        using var listener = Listen(source, started, stopped);

        var tracer = new OpenTelemetryTracer { SourceName = source };
        Assert.True(tracer.IsActive);

        var activity = (Activity?)null;
        {
            using var span = tracer.NewSpan("业务操作", new { Id = 123 });

            Assert.Single(started);
            activity = started[0];
            Assert.Equal(source, activity.Source.Name);
            Assert.Equal("业务操作", activity.DisplayName);

            // 数据标签以 newlife.tag 属性进入 OpenTelemetry
            Assert.Contains("123", activity.GetTagItem("newlife.tag") + "");

            span.Value = 456;
            Assert.Equal(456L, activity.GetTagItem("newlife.value"));

            // 埋点标识采用W3C规范
            Assert.Equal(32, span.TraceId.Length);
            Assert.Equal(16, span.Id.Length);
            Assert.Equal(activity.TraceId.ToHexString(), span.TraceId);
            Assert.Equal(activity.SpanId.ToHexString(), span.Id);

            span.SetError(new InvalidOperationException("炸了"), null);
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Contains("InvalidOperationException", activity.GetTagItem("error.type") + "");
            Assert.Contains(activity.Events, e => e.Name == "exception");
        }

        // 释放后活动结束，耗时与状态可读
        Assert.Single(stopped);
        Assert.True(activity!.Duration.TotalMilliseconds >= 0);
        Assert.Null(DefaultSpan.Current);
    }

    [Fact]
    [DisplayName("有监听者_嵌套片段自动构成父子关系")]
    public void NestedSpansBuildHierarchy()
    {
        const String source = "XUnitTest.Hierarchy";
        var started = new List<Activity>();
        using var listener = Listen(source, started, []);

        var tracer = new OpenTelemetryTracer { SourceName = source };

        String outerId;
        using (var outer = tracer.NewSpan("外层"))
        {
            outerId = outer.Id;

            using var inner = tracer.NewSpan("内层");
            Assert.Equal(outer.TraceId, inner.TraceId);
            Assert.Equal(outerId, inner.ParentId);
        }

        Assert.Equal(2, started.Count);
        Assert.Equal(started[0].SpanId.ToHexString(), started[1].ParentSpanId.ToHexString());
    }

    [Fact]
    [DisplayName("底盘双写_内层片段继承活动的W3C标识")]
    public void InnerTracerAdoptsW3cIds()
    {
        const String source = "XUnitTest.DualWrite";
        var started = new List<Activity>();
        using var listener = Listen(source, started, []);

        var inner = new RecordingTracer();
        var tracer = new OpenTelemetryTracer { SourceName = source, Inner = inner };

        using var span = tracer.NewSpan("双写");

        // 无监听者时也会写出底盘，保证星尘链路不丢
        Assert.NotNull(inner.Last);
        var activity = started[0];
        Assert.Equal(activity.TraceId.ToHexString(), inner.Last!.TraceId);
        Assert.Equal(activity.SpanId.ToHexString(), inner.Last.Id);
        Assert.Equal(activity.ParentSpanId.ToHexString(), inner.Last.ParentId);
        Assert.Equal(span.TraceId, inner.Last.TraceId);
    }

    [Fact]
    [DisplayName("底盘双写_无监听者时仍然写出底盘")]
    public void InnerTracerWorksWithoutListener()
    {
        var inner = new RecordingTracer();
        var tracer = new OpenTelemetryTracer { SourceName = "XUnitTest.NoListener3", Inner = inner };

        Assert.True(tracer.IsActive);

        using var span = tracer.NewSpan("仅底盘");

        Assert.NotNull(inner.Last);
        // 没有监听者时活动为空，埋点标识回退到底盘片段
        Assert.Equal(inner.Last!.TraceId, span.TraceId);
        Assert.Equal(inner.Last.Id, span.Id);
    }

    [Fact]
    [DisplayName("构建器_统计埋点总数与错误数")]
    public void BuilderAggregatesCounters()
    {
        const String source = "XUnitTest.Builder";
        var started = new List<Activity>();
        using var listener = Listen(source, started, []);

        var tracer = new OpenTelemetryTracer { SourceName = source };
        var builder = tracer.BuildSpan("统计");

        using (var span = builder.Start()) { }
        using (var span = builder.Start())
        {
            span.Error = "失败";
        }

        Assert.Equal(2, builder.Total);
        Assert.Equal(1, builder.Errors);
        Assert.Equal(0, builder.Samples?.Count ?? 0);

        // OpenTelemetry 由采集端处理器上报，本地不产生上报批次
        Assert.Empty(tracer.TakeAll());
    }

    [Fact]
    [DisplayName("服务端片段_活动父级取自上游traceparent")]
    public void NewServerSpanInheritsUpstreamParent()
    {
        const String source = "XUnitTest.Otel.Server";
        const String traceId = "7ae122a215982779017518707e02e4ab";
        const String parentId = "1111111111111111";
        const String traceParent = "00-" + traceId + "-" + parentId + "-01";

        var started = new List<Activity>();
        using var listener = Listen(source, started, []);

        var tracer = new OpenTelemetryTracer { SourceName = source };

        // W3C 标准头
        {
            var headers = new Dictionary<String, String> { ["traceparent"] = traceParent };

            using var span = tracer.NewServerSpan("api", headers);

            var activity = Assert.Single(started);
            Assert.Equal(traceId, activity.TraceId.ToHexString());
            Assert.Equal(parentId, activity.ParentSpanId.ToHexString());
            Assert.Equal(traceId, span!.TraceId);
        }

        // NewLife RPC 使用的 TraceId 头，其值同样是 traceparent 格式
        {
            started.Clear();
            var headers = new Dictionary<String, String> { ["TraceId"] = traceParent };

            using var span = tracer.NewServerSpan("api", headers);

            var activity = Assert.Single(started);
            Assert.Equal(traceId, activity.TraceId.ToHexString());
            Assert.Equal(parentId, activity.ParentSpanId.ToHexString());
        }
    }

    [Fact]
    [DisplayName("服务端片段_无有效上游头时开启新链路")]
    public void NewServerSpanWithoutUpstreamStartsNewTrace()
    {
        const String source = "XUnitTest.Otel.NoUpstream";
        var started = new List<Activity>();
        using var listener = Listen(source, started, []);

        var tracer = new OpenTelemetryTracer { SourceName = source };

        // 历史格式 Request-Id 不是 W3C，无法作为活动父级
        var headers = new Dictionary<String, String> { ["Request-Id"] = "|abc.def." };
        using (var span = tracer.NewServerSpan("api", headers))
        {
            var activity = Assert.Single(started);
            Assert.Equal("0000000000000000", activity.ParentSpanId.ToHexString());
            Assert.Equal(32, span!.TraceId.Length);
        }

        // 空请求头等同普通埋点
        started.Clear();
        using (tracer.NewServerSpan("api", (IDictionary<String, String>?)null)) { }
        Assert.Equal("0000000000000000", Assert.Single(started).ParentSpanId.ToHexString());
    }
}
