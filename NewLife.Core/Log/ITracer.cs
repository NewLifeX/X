using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using NewLife.Http;
using NewLife.Threading;

namespace NewLife.Log
{
    /// <summary>性能跟踪器。轻量级APM</summary>
    public interface ITracer
    {
        #region 属性
        /// <summary>采样周期</summary>
        Int32 Period { get; set; }

        /// <summary>最大正常采样数。采样周期内，最多只记录指定数量的正常事件，用于绘制依赖关系</summary>
        Int32 MaxSamples { get; set; }

        /// <summary>最大异常采样数。采样周期内，最多只记录指定数量的异常事件，默认10</summary>
        Int32 MaxErrors { get; set; }
        #endregion

        /// <summary>建立Span构建器</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ISpanBuilder BuildSpan(String name);

        /// <summary>开始一个Span</summary>
        /// <param name="name">操作名</param>
        /// <returns></returns>
        ISpan NewSpan(String name);

        /// <summary>截断所有Span构建器数据，重置集合</summary>
        /// <returns></returns>
        ISpanBuilder[] TakeAll();
    }

    /// <summary>性能跟踪器。轻量级APM</summary>
    public class DefaultTracer : DisposeBase, ITracer
    {
        #region 静态
        /// <summary>全局实例。默认每15秒采样一次</summary>
        public static ITracer Instance { get; set; } = new DefaultTracer { Log = XTrace.Log };
        #endregion

        #region 属性
        /// <summary>采样周期。默认15s</summary>
        public Int32 Period { get; set; } = 15;

        /// <summary>最大正常采样数。采样周期内，最多只记录指定数量的正常事件，用于绘制依赖关系</summary>
        public Int32 MaxSamples { get; set; } = 1;

        /// <summary>最大异常采样数。采样周期内，最多只记录指定数量的异常事件，默认10</summary>
        public Int32 MaxErrors { get; set; } = 10;

        /// <summary>采样结束时等待片段完成的时间。默认1000ms</summary>
        public Int32 WaitForFinish { get; set; } = 1000;

        /// <summary>Span构建器集合</summary>
        protected ConcurrentDictionary<String, ISpanBuilder> _builders = new ConcurrentDictionary<String, ISpanBuilder>();

        /// <summary>采样定时器</summary>
        protected TimerX _timer;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public DefaultTracer() { }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _timer.TryDispose();
        }
        #endregion

        #region 方法
        private void InitTimer()
        {
            if (_timer == null)
            {
                lock (this)
                {
                    if (_timer == null) _timer = new TimerX(s => DoProcessSpans(), null, Period * 1000, Period * 1000) { Async = true };
                }
            }
        }

        private void DoProcessSpans()
        {
            var builders = TakeAll();
            if (builders != null && builders.Length > 0)
            {
                // 等待未完成Span的时间，默认1000ms
                Thread.Sleep(WaitForFinish);

                ProcessSpans(builders);
            }

            // 采样周期可能改变
            if (Period > 0 && _timer.Period != Period * 1000) _timer.Period = Period * 1000;
        }

        /// <summary>处理Span集合。默认输出日志，可重定义输出控制台</summary>
        protected virtual void ProcessSpans(ISpanBuilder[] builders)
        {
            if (builders == null) return;

            foreach (var bd in builders)
            {
                if (bd.Total > 0)
                {
                    var ms = bd.EndTime - bd.StartTime;
                    var speed = ms == 0 ? 0 : bd.Total * 1000 / ms;
                    WriteLog("Tracer[{0}] Total={1:n0} Errors={2:n0} Speed={3:n0}tps Cost={4:n0}ms MaxCost={5:n0}ms MinCost={6:n0}ms", bd.Name, bd.Total, bd.Errors, speed, bd.Cost / bd.Total, bd.MaxCost, bd.MinCost);

#if DEBUG
                    foreach (var span in bd.Samples)
                    {
                        WriteLog("Span Id={0} ParentId={1} TraceId={2} Tag={3} Error={4}", span.Id, span.ParentId, span.TraceId, span.Tag, span.Error);
                    }
#endif
                }
            }
        }

        /// <summary>建立Span构建器</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual ISpanBuilder BuildSpan(String name)
        {
            InitTimer();

            //if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));
            if (name == null) name = "";

            return _builders.GetOrAdd(name, k => new DefaultSpanBuilder(this, k));
        }

        /// <summary>开始一个Span</summary>
        /// <param name="name">操作名</param>
        /// <returns></returns>
        public virtual ISpan NewSpan(String name) => BuildSpan(name).Start();

        /// <summary>截断所有Span构建器数据，重置集合</summary>
        /// <returns></returns>
        public virtual ISpanBuilder[] TakeAll()
        {
            var bs = _builders;
            if (!bs.Any()) return null;

            _builders = new ConcurrentDictionary<String, ISpanBuilder>();

            var bs2 = bs.Values.Where(e => e.Total > 0).ToArray();

            // 设置结束时间
            foreach (var item in bs2)
            {
                item.EndTime = DateTime.UtcNow.ToLong();
            }

            return bs2;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }

    /// <summary>跟踪扩展</summary>
    public static class TracerExtension
    {
        #region 扩展方法
        /// <summary>创建受跟踪的HttpClient</summary>
        /// <param name="tracer">跟踪器</param>
        /// <param name="handler">http处理器</param>
        /// <returns></returns>
        public static HttpClient CreateHttpClient(this ITracer tracer, HttpMessageHandler handler = null)
        {
            if (handler == null) handler = new HttpClientHandler();

            return new HttpClient(new HttpTraceHandler(handler) { Tracer = tracer });
        }
        #endregion
    }
}