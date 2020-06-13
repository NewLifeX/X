using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using NewLife.Threading;

namespace NewLife.Log
{
    /// <summary>性能跟踪器。轻量级APM</summary>
    public interface ITracer
    {
        #region 属性
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
        IDictionary<String, ISpanBuilder> TakeAll();
    }

    /// <summary>性能跟踪器。轻量级APM</summary>
    public class DefaultTracer : DisposeBase, ITracer
    {
        #region 静态
        /// <summary>全局实例</summary>
        public static ITracer Instance { get; set; } = new DefaultTracer(60) { Log = XTrace.Log };
        #endregion

        #region 属性
        /// <summary>最大正常采样数。采样周期内，最多只记录指定数量的正常事件，用于绘制依赖关系</summary>
        public Int32 MaxSamples { get; set; } = 1;

        /// <summary>最大异常采样数。采样周期内，最多只记录指定数量的异常事件，默认10</summary>
        public Int32 MaxErrors { get; set; } = 10;

        /// <summary>Span构建器集合</summary>
        private ConcurrentDictionary<String, ISpanBuilder> _builders = new ConcurrentDictionary<String, ISpanBuilder>();

        private TimerX _timer;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public DefaultTracer() { }

        /// <summary>实例化。指定定时采样周期</summary>
        /// <param name="period">采样周期。单位秒</param>
        public DefaultTracer(Int32 period) => SetPeriod(period);

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _timer.TryDispose();
        }
        #endregion

        #region 方法
        /// <summary>设置采样周期</summary>
        /// <param name="period">采样周期。单位秒</param>
        public void SetPeriod(Int32 period)
        {
            if (_timer != null)
                _timer.Period = period * 1000;
            else if (period > 0)
                _timer = new TimerX(s => ProcessSpans(), null, period * 1000, period * 1000) { Async = true };
        }

        /// <summary>处理Span集合。默认输出日志，可重定义输出控制台</summary>
        protected virtual void ProcessSpans()
        {
            var builders = TakeAll();
            if (builders.Count > 0)
            {
                // 等待未完成Span的时间，默认1000ms
                var msWait = 1000;
                Thread.Sleep(msWait);

                foreach (var item in builders)
                {
                    var bd = item.Value;
                    if (bd.Total > 0)
                    {
                        var ms = bd.EndTime - bd.StartTime;
                        var speed = ms == 0 ? 0 : bd.Total * 1000 / ms;
                        WriteLog("Tracer[{0}] Total={1:n0} Errors={2:n0} Speed={3:n0}tps Cost={4:n0}ms MaxCost={5:n0}ms", bd.Name, bd.Total, bd.Errors, speed, bd.Cost / bd.Total, bd.MaxCost);
                    }
                }
            }
        }

        /// <summary>建立Span构建器</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual ISpanBuilder BuildSpan(String name)
        {
            if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));

            return _builders.GetOrAdd(name, k => new DefaultSpanBuilder(this, k));
        }

        /// <summary>开始一个Span</summary>
        /// <param name="name">操作名</param>
        /// <returns></returns>
        public virtual ISpan NewSpan(String name) => BuildSpan(name).Start();

        /// <summary>截断所有Span构建器数据，重置集合</summary>
        /// <returns></returns>
        public virtual IDictionary<String, ISpanBuilder> TakeAll()
        {
            var bs = _builders;
            _builders = new ConcurrentDictionary<String, ISpanBuilder>();

            // 设置结束时间
            foreach (var item in bs)
            {
                item.Value.EndTime = DateTime.UtcNow.ToLong();
            }

            return bs;
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
}