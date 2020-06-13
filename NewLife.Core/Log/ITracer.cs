using System;
using System.Collections.Concurrent;

namespace NewLife.Log
{
    /// <summary>性能跟踪器。轻量级APM</summary>
    public interface ITracer
    {
        #region 属性

        #endregion

        /// <summary>建立Span构建器</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ISpanBuilder BuildSpan(String name);

        /// <summary>开始一个Span</summary>
        /// <param name="name">操作名</param>
        /// <returns></returns>
        ISpan Start(String name);
    }

    /// <summary>性能跟踪器。轻量级APM</summary>
    public class DefaultTracer : ITracer
    {
        #region 静态
        /// <summary>全局实例</summary>
        public static ITracer Instance { get; set; } = new DefaultTracer();
        #endregion

        #region 属性
        /// <summary>最大正常采样数。采样周期内，最多只记录指定数量的正常事件，用于绘制依赖关系</summary>
        public Int32 MaxSamples { get; set; } = 1;

        /// <summary>最大异常采样数。采样周期内，最多只记录指定数量的异常事件，默认10</summary>
        public Int32 MaxError { get; set; } = 10;

        private readonly ConcurrentDictionary<String, ISpanBuilder> _builders = new ConcurrentDictionary<String, ISpanBuilder>();
        #endregion

        #region 方法
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
        public virtual ISpan Start(String name) => BuildSpan(name).Start();
        #endregion
    }
}