using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;

namespace NewLife.Model
{
    /// <summary>延迟队列。缓冲合并对象，批量处理</summary>
    /// <remarks>
    /// 借助实体字典，缓冲实体对象，定期给字典换新，实现批量处理
    /// </remarks>
    public class DeferredQueue : DisposeBase
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        private volatile ConcurrentDictionary<String, Object> _Entities = new ConcurrentDictionary<String, Object>();
        /// <summary>实体字典</summary>
        public ConcurrentDictionary<String, Object> Entities => _Entities;

        /// <summary>跟踪数。达到该值时输出跟踪日志，默认1000</summary>
        public Int32 TraceCount { get; set; } = 1000;

        /// <summary>批量处理回调</summary>
        public Func<IList<Object>, Int32> OnProcess { get; set; }

        /// <summary>周期。默认10_000毫秒</summary>
        public Int32 Period { get; set; } = 10_000;

        /// <summary>最大个数。超过该个数时，进入队列将产生堵塞。默认100_000</summary>
        public Int32 MaxEntity { get; set; } = 100_000;

        /// <summary>批大小。默认5_000</summary>
        public Int32 BatchSize { get; set; } = 5_000;

        /// <summary>保存速度，每秒保存多少个实体</summary>
        public Int32 Speed { get; private set; }

        /// <summary>是否异步处理。true表示异步处理，共用DQ定时调度；false表示同步处理，独立线程</summary>
        public Boolean Async { get; set; } = true;

        private Int32 _Times;
        /// <summary>合并保存的总次数</summary>
        public Int32 Times => _Times;

        /// <summary>错误发生时</summary>
        public event EventHandler<EventArgs<Exception>> Error;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public DeferredQueue() => Name = GetType().Name.TrimEnd("Queue");

        /// <summary>销毁。统计队列销毁时保存数据</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _Timer.TryDispose();
            _Entities?.Clear();
        }

        /// <summary>初始化</summary>
        public void Init()
        {
            // 首次使用时初始化定时器
            if (_Timer == null)
            {
                lock (this)
                {
                    if (_Timer == null) _Timer = OnInit();
                }
            }
        }

        /// <summary>初始化</summary>
        protected virtual TimerX OnInit()
        {
            // 为了避免多队列并发，首次执行时间随机错开
            var p = Period;
            if (p > 1000) p = Rand.Next(1000, p);

            var name = Async ? "DQ" : Name;

            return new TimerX(Work, null, p, Period, name)
            {
                Async = Async,
                CanExecute = () => _Entities.Any()
            };
        }
        #endregion

        #region 方法
        /// <summary>尝试添加</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Boolean TryAdd(String key, Object value)
        {
            Interlocked.Increment(ref _Times);

            Init();

            if (!_Entities.TryAdd(key, value)) return false;

            Interlocked.Increment(ref _count);

            // 超过最大值时，堵塞一段时间，等待消费完成
            while (_count >= MaxEntity)
            {
                Thread.Sleep(100);
            }

            return true;
        }

        /// <summary>获取 或 添加</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="valueFactory"></param>
        /// <returns></returns>
        public virtual T GetOrAdd<T>(String key, Func<String, T> valueFactory = null) where T : class, new()
        {
            Interlocked.Increment(ref _Times);

            Init();

            Object entity = null;
            while (!_Entities.TryGetValue(key, out entity))
            {
                if (entity == null)
                {
                    if (valueFactory != null)
                        entity = valueFactory(key);
                    else
                        entity = new T();
                }
                if (_Entities.TryAdd(key, entity))
                {
                    Interlocked.Increment(ref _count);
                    break;
                }
            }

            // 超过最大值时，堵塞一段时间，等待消费完成
            while (_count >= MaxEntity)
            {
                Thread.Sleep(100);
            }

            return entity as T;
        }

        /// <summary>当前缓存个数</summary>
        private Int32 _count;
        private TimerX _Timer;

        private void Work(Object state)
        {
            var es = _Entities;
            if (!es.Any()) return;

            _Entities = new ConcurrentDictionary<String, Object>();
            var times = _Times;

            Interlocked.Add(ref _count, -es.Count);
            Interlocked.Add(ref _Times, -times);

            // 先取出来
            var list = es.Values.ToList();

            //if (list.Count > TraceCount)
            //{
            //    var cost = Speed == 0 ? 0 : list.Count * 1000 / Speed;
            //    XTrace.WriteLine($"延迟队列[{Name}]\t保存 {list.Count:n0}\t预测 {cost:n0}ms\t次数 {times:n0}");
            //}

            var sw = Stopwatch.StartNew();
            var total = ProcessAll(list);
            sw.Stop();

            var ms = sw.Elapsed.TotalMilliseconds;
            Speed = ms == 0 ? 0 : (Int32)(list.Count * 1000 / ms);
            if (list.Count >= TraceCount)
            {
                var sp = ms == 0 ? 0 : (Int32)(times * 1000 / ms);
                XTrace.WriteLine($"延迟队列[{Name}]\t保存 {list.Count:n0}\t耗时 {ms:n0}ms\t速度 {Speed:n0}tps\t次数 {times:n0}\t速度 {sp:n0}tps\t成功 {total:n0}");
            }
        }

        /// <summary>定时处理全部数据</summary>
        /// <param name="list"></param>
        protected virtual Int32 ProcessAll(ICollection<Object> list)
        {
            var total = 0;
            // 分批
            for (var i = 0; i < list.Count();)
            {
                var batch = list.Skip(i).Take(BatchSize).ToList();

                try
                {
                    total += Process(batch);
                }
                catch (Exception ex)
                {
                    OnError(batch, ex);
                }

                i += batch.Count;
            }

            return total;
        }

        /// <summary>处理一批</summary>
        /// <param name="list"></param>
        public virtual Int32 Process(IList<Object> list)
        {
            if (OnProcess == null) return 0;

            return OnProcess(list);
        }

        /// <summary>发生错误</summary>
        /// <param name="list"></param>
        /// <param name="ex"></param>
        protected virtual void OnError(IList<Object> list, Exception ex)
        {
            if (Error != null)
                Error(this, new EventArgs<Exception>(ex));
            else
                XTrace.WriteException(ex);
        }
        #endregion
    }
}