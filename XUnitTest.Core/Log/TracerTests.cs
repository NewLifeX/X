using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            //Assert.Throws<ArgumentNullException>(() => tracer.BuildSpan(null));
            // 空名称
            {
                var builder = tracer.BuildSpan(null);
                Assert.NotNull(builder);
                Assert.NotNull(builder.Name);
                Assert.Empty(builder.Name);
            }

            // 标准用法
            {
                var builder = tracer.BuildSpan("test");
                Assert.NotNull(builder);
                Assert.Equal(tracer, builder.Tracer);
                Assert.Equal("test", builder.Name);
                Assert.True(builder.StartTime > 0);
                Assert.Equal(0, builder.EndTime);

                using var span = builder.Start();
                span.Tag = "任意业务数据";
                Assert.NotEmpty(span.TraceId);
                Assert.NotEmpty(span.Id);
                Assert.Null(span.ParentId);
                Assert.Equal(DateTime.Today, span.StartTime.ToDateTime().ToLocalTime().Date);

                Assert.Equal(span, DefaultSpan.Current);

                Thread.Sleep(100);
                span.Dispose();

                Assert.Null(DefaultSpan.Current);

                var cost = span.EndTime - span.StartTime;
                Assert.True(cost >= 100);
                Assert.Null(span.Error);

                Assert.Equal(1, builder.Total);
                Assert.Equal(0, builder.Errors);
                Assert.Equal(cost, builder.Cost);
                Assert.Equal(cost, builder.MaxCost);
                Assert.Equal(cost, builder.MinCost);
            }

            // 快速用法
            {
                using var span2 = tracer.NewSpan("test2");
                Thread.Sleep(200);
                span2.Dispose();

                var cost = span2.EndTime - span2.StartTime;
                Assert.True(cost >= 200);
                Assert.Null(span2.Error);

                var builder2 = tracer.BuildSpan("test2");
                Assert.Equal(1, builder2.Total);
                Assert.Equal(0, builder2.Errors);
                Assert.Equal(cost, builder2.Cost);
                Assert.Equal(cost, builder2.MaxCost);
                Assert.Equal(cost, builder2.MinCost);
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
            Assert.NotEqual(samples[0].Id, samples[1].Id);

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
            Assert.NotEqual(errors[0].Id, errors[1].Id);

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
                    Assert.Equal(span, DefaultSpan.Current);

                    using var span2 = tracer.NewSpan("test2");
                    Assert.Equal(span2, DefaultSpan.Current);

                    Assert.Equal(span.TraceId, span2.TraceId);
                    Assert.Equal(span.Id, span2.ParentId);
                    Assert.NotEqual(span.Id, span2.Id);

                    Thread.Sleep(100);
                    {
                        using var span3 = tracer.NewSpan("test3");
                        Assert.Equal(span3, DefaultSpan.Current);

                        Assert.Equal(span.TraceId, span3.TraceId);
                        Assert.Equal(span2.Id, span3.ParentId);
                        Assert.NotEqual(span2.Id, span3.Id);
                    }

                    Assert.Equal(span2, DefaultSpan.Current);
                }

                Assert.Equal(span, DefaultSpan.Current);
            }

            // 内嵌片段，不同线程应该使用不同TraceId
            {
                using var span = tracer.NewSpan("test");
                Thread.Sleep(100);

                // 另一个线程建立span，必须用UnsafeQueueUserWorkItem截断上下文传递，否则还是会建立父子关系
                ISpan span2 = null;
                ThreadPool.UnsafeQueueUserWorkItem(s =>
                {
                    span2 = tracer.NewSpan("test2");
                }, null);
                Thread.Sleep(100);
                //using var span2 = Task.Factory.StartNew(() => tracer.NewSpan("test2"), TaskCreationOptions.LongRunning).Result;
                Assert.NotEqual(span.TraceId, span2.TraceId);
                Assert.NotEqual(span.Id, span2.ParentId);
                span2.Dispose();
            }

            var builder = tracer.BuildSpan("test");
            Assert.Equal(2, builder.Total);
            Assert.Equal(0, builder.Errors);
        }

        [Fact]
        public void TestParentId()
        {
            var tracer = new DefaultTracer();

            // QueueUserWorkItem传递上下文
            {
                using var span = tracer.NewSpan("test");
                Thread.Sleep(100);

                // 另一个线程建立span
                ISpan span2 = null;
                ThreadPool.QueueUserWorkItem(s =>
                {
                    span2 = tracer.NewSpan("test2");
                }, null);
                Thread.Sleep(100);

                Assert.Equal(span.TraceId, span2.TraceId);
                Assert.Equal(span.Id, span2.ParentId);
                span2.Dispose();
            }

            // Task传递上下文
            {
                using var span = tracer.NewSpan("test");
                Thread.Sleep(100);

                // 另一个线程建立span
                using var span2 = Task.Run(() => tracer.NewSpan("test2")).Result;
                Assert.Equal(span.TraceId, span2.TraceId);
                Assert.Equal(span.Id, span2.ParentId);
            }

            // Task传递上下文
            {
                using var span = tracer.NewSpan("test");
                Thread.Sleep(100);

                // 另一个线程建立span
                using var span2 = Task.Factory.StartNew(() => tracer.NewSpan("test2"), TaskCreationOptions.LongRunning).Result;
                Assert.Equal(span.TraceId, span2.TraceId);
                Assert.Equal(span.Id, span2.ParentId);
            }

            var builder = tracer.BuildSpan("test");
            Assert.Equal(3, builder.Total);
            Assert.Equal(0, builder.Errors);

            builder = tracer.BuildSpan("test2");
            Assert.Equal(3, builder.Total);
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
            var keys = bs.Select(e => e.Name).ToArray();
            Assert.Equal(2, bs.Length);
            Assert.Contains("/", keys);
            Assert.Contains("/notfound", keys);

            // 其中一项
            var builder = bs.FirstOrDefault(e => e.Name == "/notfound");
            Assert.Equal(1, builder.Total);
            Assert.Equal(1, builder.Errors);

            var span = builder.ErrorSamples[0];
            Assert.Equal("https://www.newlifexxx.com/notfound", span.Tag);
        }
    }
}