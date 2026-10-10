using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

[DisplayName("OpenTelemetry采集器")]
public class OpenTelemetryCollectorTests
{
    /// <summary>本次用例专属的源前缀，避免并行用例互相干扰</summary>
    private const String Prefix = "XUnitTest.Collect";

    [Fact]
    [DisplayName("采集第三方活动_按源名聚合进埋点统计与样本")]
    public void CollectThirdPartyActivity()
    {
        var tracer = new DefaultTracer { MaxSamples = 100 };
        using var collector = new OpenTelemetryCollector
        {
            Tracer = tracer,
            ExcludeSources = null,
            IncludeSources = [Prefix + ".Redis"],
        }.Attach();

        var source = new ActivitySource(Prefix + ".Redis");
        Activity activity;
        using (activity = source.StartActivity("SET", ActivityKind.Client)!)
        {
            activity.SetTag("db.system", "redis");
            activity.SetTag("db.statement", "SET k v");
        }

        var builder = tracer.TakeAll().Single(e => e.Name == Prefix + ".Redis:SET");
        Assert.Equal(1, builder.Total);

        // 强制采样，样本保留完整的 W3C 链路标识，便于星尘还原调用链
        var samples = builder.Samples;
        Assert.NotNull(samples);
        var span = samples.Single();
        Assert.Equal(activity.TraceId.ToHexString(), span.TraceId);
        Assert.Equal(activity.SpanId.ToHexString(), span.Id);
        Assert.Equal(32, span.TraceId.Length);
        Assert.Equal(16, span.Id.Length);
        Assert.True(span.StartTime > 0);
        Assert.True(span.EndTime >= span.StartTime);
        Assert.Contains("db.system = redis", span.Tag);
        Assert.Contains("db.statement = SET k v", span.Tag);
        Assert.Null(span.Error);
    }

    [Fact]
    [DisplayName("默认排除清单_已由DiagnosticListener覆盖的源不重复采集")]
    public void ExcludeSourcesByDefault()
    {
        var tracer = new DefaultTracer { MaxSamples = 100 };
        using var collector = new OpenTelemetryCollector { Tracer = tracer }.Attach();

        // 判断过滤器本身，避免依赖活动是否真的被创建
        using var http = new ActivitySource("System.Net.Http");
        using var sockets = new ActivitySource("Experimental.System.Net.Sockets");
        using var efcore = new ActivitySource("Microsoft.EntityFrameworkCore.Query");
        using var keep = new ActivitySource("XUnitTest.NotExcluded");

        Assert.False(collector.ShouldListenTo(http));
        Assert.False(collector.ShouldListenTo(sockets));
        Assert.False(collector.ShouldListenTo(efcore));
        Assert.True(collector.ShouldListenTo(keep));
        Assert.True(collector.ShouldListenTo(new ActivitySource("StackExchange.Redis")));

        // 被排除的源即使发出活动也不会进入埋点
        using (http.StartActivity("GET")) { }
        Assert.DoesNotContain(tracer.TakeAll(), e => e.Name.StartsWith("System.Net.Http"));
    }

    [Fact]
    [DisplayName("白名单模式_只采集指定源")]
    public void IncludeSourcesWhitelist()
    {
        var tracer = new DefaultTracer { MaxSamples = 100 };
        using var collector = new OpenTelemetryCollector
        {
            Tracer = tracer,
            ExcludeSources = null,
            IncludeSources = [Prefix + ".White.*", "Npgsql*"],
        }.Attach();

        var redis = new ActivitySource(Prefix + ".White.Redis");
        using (redis.StartActivity("GET")) { }

        var npgsql = new ActivitySource("Npgsql.Command");
        using (npgsql.StartActivity("Execute")) { }

        var other = new ActivitySource(Prefix + ".Other");
        using (other.StartActivity("Noise")) { }

        var builders = tracer.TakeAll();
        Assert.Contains(builders, e => e.Name == Prefix + ".White.Redis:GET");
        Assert.Contains(builders, e => e.Name == "Npgsql.Command:Execute");
        Assert.DoesNotContain(builders, e => e.Name == Prefix + ".Other:Noise");
    }

    [Fact]
    [DisplayName("异常活动_状态映射为错误并保留异常事件")]
    public void ErrorStatusMapsToError()
    {
        var tracer = new DefaultTracer { MaxErrors = 100 };
        using var collector = new OpenTelemetryCollector
        {
            Tracer = tracer,
            ExcludeSources = null,
            IncludeSources = [Prefix + ".Fail"],
        }.Attach();

        var source = new ActivitySource(Prefix + ".Fail");
        using (var activity = source.StartActivity("Run"))
        {
            activity!.SetStatus(ActivityStatusCode.Error, "炸了");
            activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                ["exception.type"] = "System.InvalidOperationException",
            }));
        }

        var builder = tracer.TakeAll().Single(e => e.Name == Prefix + ".Fail:Run");
        Assert.Equal(1, builder.Errors);

        var errors = builder.ErrorSamples;
        Assert.NotNull(errors);
        var span = errors.Single();
        Assert.Equal("炸了", span.Error);
        Assert.Contains("exception", span.Tag);
        Assert.Contains("System.InvalidOperationException", span.Tag);
    }

    [Fact]
    [DisplayName("采集过程_不污染当前片段上下文")]
    public void DoesNotPolluteCurrentSpan()
    {
        var tracer = new DefaultTracer { MaxSamples = 100 };
        using var collector = new OpenTelemetryCollector
        {
            Tracer = tracer,
            ExcludeSources = null,
            IncludeSources = [Prefix + ".Context"],
        }.Attach();

        Assert.Null(DefaultSpan.Current);

        var source = new ActivitySource(Prefix + ".Context");
        using (source.StartActivity("Op")) { }

        Assert.Null(DefaultSpan.Current);
        Assert.Single(tracer.TakeAll(), e => e.Name == Prefix + ".Context:Op");
    }

    [Fact]
    [DisplayName("注销后_不再采集活动")]
    public void DetachedCollectorStopsListening()
    {
        var tracer = new DefaultTracer { MaxSamples = 100 };
        var collector = new OpenTelemetryCollector
        {
            Tracer = tracer,
            ExcludeSources = null,
            IncludeSources = [Prefix + ".Detached"],
        }.Attach();
        collector.Dispose();

        var source = new ActivitySource(Prefix + ".Detached");
        using (source.StartActivity("Op")) { }

        Assert.DoesNotContain(tracer.TakeAll(), e => e.Name.StartsWith(Prefix + ".Detached"));
    }
}
