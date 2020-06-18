using System;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Security;
using NewLife.Serialization;

namespace NewLife.Log
{
    /// <summary>性能跟踪片段。轻量级APM</summary>
    public interface ISpan : IDisposable
    {
        /// <summary>唯一标识。随线程上下文、Http、Rpc传递，作为内部片段的父级</summary>
        String Id { get; set; }

        /// <summary>父级片段标识</summary>
        String ParentId { get; set; }

        /// <summary>跟踪标识。可用于关联多个片段，建立依赖关系，随线程上下文、Http、Rpc传递</summary>
        String TraceId { get; set; }

        /// <summary>开始时间。Unix毫秒</summary>
        Int64 StartTime { get; set; }

        /// <summary>结束时间。Unix毫秒</summary>
        Int64 EndTime { get; set; }

        /// <summary>数据标签。记录一些附加数据</summary>
        String Tag { get; set; }

        /// <summary>错误信息</summary>
        String Error { get; set; }

        /// <summary>设置错误信息</summary>
        /// <param name="ex">异常</param>
        /// <param name="tag">标签</param>
        void SetError(Exception ex, Object tag);
    }

    /// <summary>性能跟踪片段。轻量级APM</summary>
    public class DefaultSpan : ISpan
    {
        #region 属性
        /// <summary>构建器</summary>
        [XmlIgnore, ScriptIgnore]
        public ISpanBuilder Builder { get; }

        /// <summary>唯一标识。随线程上下文、Http、Rpc传递，作为内部片段的父级</summary>
        public String Id { get; set; }

        /// <summary>父级片段标识</summary>
        public String ParentId { get; set; }

        /// <summary>跟踪标识。可用于关联多个片段，建立依赖关系，随线程上下文、Http、Rpc传递</summary>
        public String TraceId { get; set; }

        /// <summary>开始时间。Unix毫秒</summary>
        public Int64 StartTime { get; set; }

        /// <summary>结束时间。Unix毫秒</summary>
        public Int64 EndTime { get; set; }

        /// <summary>数据标签。记录一些附加数据</summary>
        public String Tag { get; set; }

        /// <summary>错误信息</summary>
        public String Error { get; set; }

#if NET40 || NET45
        [ThreadStatic]
        private static ISpan _Current;
        /// <summary>当前线程正在使用的上下文</summary>
        public static ISpan Current { get => _Current; set => _Current = value; }
#else
        private static readonly System.Threading.AsyncLocal<ISpan> _Current = new System.Threading.AsyncLocal<ISpan>();
        /// <summary>当前线程正在使用的上下文</summary>
        public static ISpan Current { get => _Current.Value; set => _Current.Value = value; }
#endif

        private ISpan _parent;
        private Boolean _finished;
        private static Int64 _gid;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="builder"></param>
        public DefaultSpan(ISpanBuilder builder)
        {
            Builder = builder;
            StartTime = DateTime.UtcNow.ToLong();
        }

        /// <summary>释放资源</summary>
        public void Dispose() => Finish();
        #endregion

        #region 方法
        /// <summary>设置跟踪标识</summary>
        public void Start()
        {
            if (Id.IsNullOrEmpty()) Id = Interlocked.Increment(ref _gid) + "";

            // 设置父级
            var span = Current;
            if (span != null && span != this)
            {
                _parent = span;

                ParentId = span.Id;
                TraceId = span.TraceId;
            }

            // 否则创建新的跟踪标识
            if (TraceId.IsNullOrEmpty()) TraceId = Rand.NextString(16);

            // 设置当前片段
            Current = this;
        }

        /// <summary>完成跟踪</summary>
        private void Finish()
        {
            if (_finished) return;
            _finished = true;

            EndTime = DateTime.UtcNow.ToLong();

            // 从本线程中清除跟踪标识
            Current = _parent;

            Builder.Finish(this);
        }

        /// <summary>设置错误信息</summary>
        /// <param name="ex">异常</param>
        /// <param name="tag">标签</param>
        public void SetError(Exception ex, Object tag)
        {
            Error = ex?.GetMessage();
            if (tag is String str)
                Tag = str?.Cut(256);
            else if (tag != null)
                Tag = tag?.ToJson().Cut(256);
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{TraceId}-{Id}";
        #endregion
    }
}