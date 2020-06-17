using System;
using System.Threading;
using NewLife.Log;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Log
{
    public class TracerTests
    {
        [Fact]
        public void Test1()
        {
            var tracer = new DefaultTracer
            {
                MaxSamples = 2,
                MaxErrors = 11
            };

            Assert.Throws<ArgumentNullException>(() => tracer.BuildSpan(null));

            // 标准用法
            {
                var builder = tracer.BuildSpan("test");
                Assert.NotNull(builder);
                Assert.Equal(tracer, builder.Tracer);
                Assert.Equal("test", builder.Name);

                using var span = builder.Start();
                span.Tag = "任意业务数据";
                Assert.NotEmpty(span.TraceId);
                Assert.Equal(DateTime.Today, span.Time.ToDateTime().ToLocalTime().Date);

                Thread.Sleep(100);
                span.Dispose();

                Assert.True(span.Cost >= 100);
                Assert.Null(span.Error);

                Assert.Equal(1, builder.Total);
                Assert.Equal(0, builder.Errors);
                Assert.Equal(span.Cost, builder.Cost);
                Assert.Equal(span.Cost, builder.MaxCost);
            }

            // 快速用法
            {
                using var span2 = tracer.NewSpan("test2");
                Thread.Sleep(200);
                span2.Dispose();

                Assert.True(span2.Cost >= 200);
                Assert.Null(span2.Error);

                var builder2 = tracer.BuildSpan("test2");
                Assert.Equal(1, builder2.Total);
                Assert.Equal(0, builder2.Errors);
                Assert.Equal(span2.Cost, builder2.Cost);
                Assert.Equal(span2.Cost, builder2.MaxCost);
            }

            var js = tracer.TakeAll().ToJson();
            Assert.Contains("\"Tag\":\"任意业务数据\"", js);
        }

        [Fact]
        public void TestSamples()
        {
            var tracer = new DefaultTracer
            {
                MaxSamples = 2,
                MaxErrors = 11
            };

            // 正常采样
            for (var i = 0; i < 10; i++)
            {
                using var span = tracer.NewSpan("test");
            }

            var builder = tracer.BuildSpan("test");
            var samples = builder.Samples;
            Assert.NotNull(samples);
            Assert.Equal(10, builder.Total);
            Assert.Equal(tracer.MaxSamples, samples.Count);
            Assert.NotEqual(samples[0].TraceId, samples[1].TraceId);

            // 异常采样
            for (var i = 0; i < 20; i++)
            {
                using var span = tracer.NewSpan("test");
                span.SetError(new Exception("My Error"), null);
            }

            var errors = builder.ErrorSamples;
            Assert.NotNull(errors);
            Assert.Equal(10 + 20, builder.Total);
            Assert.Equal(tracer.MaxErrors, errors.Count);
            Assert.NotEqual(errors[0].TraceId, errors[1].TraceId);

            var js = tracer.TakeAll().ToJson();
        }

        [Fact]
        public void TestTracerId()
        {
            var tracer = new DefaultTracer();

            // 内嵌片段，应该共用TraceId
            {
                using var span = tracer.NewSpan("test");
                Thread.Sleep(100);
                {
                    using var span2 = tracer.NewSpan("test2");

                    Assert.Equal(span.TraceId, span2.TraceId);
                }
            }

            // 内嵌片段，不同线程应该使用不同TraceId
            {
                using var span = tracer.NewSpan("test");
                Thread.Sleep(100);
                ThreadPool.QueueUserWorkItem(s =>
                {
                    using var span2 = tracer.NewSpan("test2");

                    Assert.NotEqual(span.TraceId, span2.TraceId);
                });
            }

            var builder = tracer.BuildSpan("test");
            Assert.Equal(2, builder.Total);
            Assert.Equal(0, builder.Errors);
        }

        [Fact]
        public void TestError()
        {
            var tracer = new DefaultTracer();

            {
                using var span = tracer.NewSpan("test");
                Thread.Sleep(100);
                {
                    using var span2 = tracer.NewSpan("test");
                    Thread.Sleep(200);

                    span2.SetError(new Exception("My Error"), null);
                }
            }

            var builder = tracer.BuildSpan("test");
            Assert.Equal(2, builder.Total);
            Assert.Equal(1, builder.Errors);
            Assert.True(builder.Cost >= 100 + 200);
            Assert.True(builder.MaxCost >= 200);
        }

        [Fact]
        public async void TestHttpClient()
        {
            var tracer = new DefaultTracer();

            var http = tracer.CreateHttpClient();
            await http.GetStringAsync("https://www.newlifex.com");
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                // 故意写错地址，让它抛出异常
                await http.GetStringAsync("https://www.newlifexxx.com/notfound");
            });

            // 取出全部跟踪数据
            var bs = tracer.TakeAll();
            Assert.Equal(2, bs.Count);
            Assert.Contains("/", bs.Keys);
            Assert.Contains("/notfound", bs.Keys);

            // 其中一项
            var builder = bs["/notfound"];
            Assert.Equal(1, builder.Total);
            Assert.Equal(1, builder.Errors);

            var span = builder.ErrorSamples[0];
            Assert.Equal("https://www.newlifexxx.com/notfound", span.Tag);
        }
    }
}