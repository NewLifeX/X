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
    /// 借助实体字典，缓冲实体对象，定期给字典换新，实现批量处理。
    /// 
    /// 有可能外部拿到对象后，正在修改，内部恰巧执行批量处理，导致外部的部分修改未能得到处理。
    /// 解决办法是增加一个提交机制，外部用完后提交修改，内部需要处理时，等待一个时间。
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

        /// <summary>周期。默认10_000毫秒</summary>
        public Int32 Period { get; set; } = 10_000;

        /// <summary>最大个数。超过该个数时，进入队列将产生堵塞。默认100_000</summary>
        public Int32 MaxEntity { get; set; } = 100_000;

        /// <summary>批大小。默认5_000</summary>
        public Int32 BatchSize { get; set; } = 5_000;

        /// <summary>等待借出对象确认修改的时间，默认3000ms</summary>
        public Int32 WaitForBusy { get; set; } = 3_000;

        /// <summary>保存速度，每秒保存多少个实体</summary>
        public Int32 Speed { get; private set; }

        /// <summary>是否异步处理。默认true表示异步处理，共用DQ定时调度；false表示同步处理，独立线程</summary>
        public Boolean Async { get; set; } = true;

        private Int32 _Times;
        /// <summary>合并保存的总次数</summary>
        public Int32 Times => _Times;

        /// <summary>批次处理成功时</summary>
        public Action<IList<Object>> Finish;

        /// <summary>批次处理失败时</summary>
        public Action<IList<Object>, Exception> Error;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public DeferredQueue() => Name = GetType().Name.TrimEnd("Queue", "Actor", "Cache");

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

            var timer = new TimerX(Work, null, p, Period, name)
            {
                Async = Async,
                CanExecute = () => _Entities.Any()
            };

            // 独立调度时加大最大耗时告警
            if (!Async) timer.Scheduler.MaxCost = 30_000;

            return timer;
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
            CheckMax();

            return true;
        }

        /// <summary>获取 或 添加 实体对象，在外部修改对象值</summary>
        /// <remarks>
        /// 外部正在修改对象时，内部不允许执行批量处理
        /// </remarks>
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
            CheckMax();

            // 增加繁忙数
            Interlocked.Increment(ref _busy);

            return entity as T;
        }

        private void CheckMax()
        {
            if (_count < MaxEntity) return;

            // 超过最大值时，堵塞一段时间，等待消费完成
            var t = WaitForBusy * 5;
            while (t > 0)
            {
                if (_count < MaxEntity) return;

                Thread.Sleep(100);
                t -= 100;
            }

            throw new InvalidOperationException($"已有数据量[{_count:n0}]超过最大数据量[{MaxEntity:n0}]");
        }

        /// <summary>等待确认修改的借出对象数</summary>
        private volatile Int32 _busy;

        /// <summary>提交对象的修改，外部不再使用该对象</summary>
        /// <param name="key"></param>
        public virtual void Commit(String key)
        {
            // 减少繁忙数
            if (_busy > 0) Interlocked.Decrement(ref _busy);
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

            // 检查繁忙数，等待外部未完成的修改
            var t = WaitForBusy;
            while (_busy > 0 && t > 0)
            {
                Thread.Sleep(100);
                t -= 100;
            }
            //_busy = 0;

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

                    Finish?.Invoke(batch);
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
        public virtual Int32 Process(IList<Object> list) => 0;

        /// <summary>发生错误</summary>
        /// <param name="list"></param>
        /// <param name="ex"></param>
        protected virtual void OnError(IList<Object> list, Exception ex)
        {
            if (Error != null)
                Error(list, ex);
            else
                XTrace.WriteException(ex);
        }
        #endregion
    }
}