using System;
using System.Threading;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Log
{
    public class TracerTests
    {
        [Fact]
        public void Test1()
        {
            var tracer = DefaultTracer.Instance;
            if (tracer is DefaultTracer dt)
            {
                dt.MaxSamples = 2;
                dt.MaxError = 11;
            }

            Assert.Throws<ArgumentNullException>(() => tracer.BuildSpan(null));

            // 标准用法
            {
                var builder = tracer.BuildSpan("test");
                Assert.NotNull(builder);
                Assert.Equal(tracer, builder.Tracer);
                Assert.Equal("test", builder.Name);

                var span = builder.Start();
                Assert.NotEmpty(span.TracerId);
                Assert.Equal(DateTime.Today, span.Time.Date);

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
                var span = tracer.Start("test2");
                Thread.Sleep(200);
                span.Dispose();

                Assert.True(span.Cost >= 200);
                Assert.Null(span.Error);

                var builder = tracer.BuildSpan("test2");
                Assert.Equal(1, builder.Total);
                Assert.Equal(0, builder.Errors);
                Assert.Equal(span.Cost, builder.Cost);
                Assert.Equal(span.Cost, builder.MaxCost);
            }
        }
    }
}