using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NewLife;
using NewLife.Log;
using NewLife.Threading;

namespace XCode
{
    /// <summary>实体队列</summary>
    public class EntityQueue
    {
        #region 属性
        /// <summary>需要近实时保存的实体队列</summary>
        private ConcurrentDictionary<IEntity, IEntity> Entities { get; set; } = new ConcurrentDictionary<IEntity, IEntity>();

        /// <summary>需要延迟保存的实体队列</summary>
        private ConcurrentDictionary<IEntity, DateTime> DelayEntities { get; } = new ConcurrentDictionary<IEntity, DateTime>();

        /// <summary>调试开关，默认false</summary>
        public Boolean Debug { get; set; }

        /// <summary>数据访问</summary>
        public IEntitySession Session { get; }

        /// <summary>周期。默认1000毫秒，根据繁忙程度动态调节，尽量靠近每次持久化1000个对象</summary>
        public Int32 Period { get; set; } = 1000;

        /// <summary>最大个数，超过该个数时，进入队列将产生堵塞。默认10000</summary>
        public Int32 MaxEntity { get; set; } = 10_000;

        /// <summary>保存速度，每秒保存多少个实体</summary>
        public Int32 Speed { get; private set; }

        ///// <summary>完成事件。</summary>
        //public event EventHandler<EventArgs<IEntity, Int32>> Completed;

        /// <summary>错误发生时</summary>
        public event EventHandler<EventArgs<Exception>> Error;

        private TimerX _Timer;
        #endregion

        #region 构造
        /// <summary>实例化实体队列</summary>
        public EntityQueue(IEntitySession session)
        {
            Session = session;
        }
        #endregion

        #region 方法
        /// <summary>添加实体对象进入队列</summary>
        /// <param name="entity">实体对象</param>
        /// <param name="msDelay">延迟保存的时间</param>
        /// <returns>返回是否添加成功，实体对象已存在于队列中则返回false</returns>
        public Boolean Add(IEntity entity, Int32 msDelay)
        {
            // 首次使用时初始化定时器
            if (_Timer == null)
            {
                lock (this)
                {
                    if (_Timer == null)
                    {
                        _Timer = new TimerX(Work, null, Period, Period, "EQ") { Async = true };
                        _Timer.CanExecute = () => DelayEntities.Any() || Entities.Any();
                    }
                }
            }

            var rs = false;
            if (msDelay <= 0)
                rs = Entities.TryAdd(entity, entity);
            else
                rs = DelayEntities.TryAdd(entity, TimerX.Now.AddMilliseconds(msDelay));
            if (!rs) return false;

            Interlocked.Increment(ref _count);

            // 超过最大值时，堵塞一段时间，等待消费完成
            while (_count >= MaxEntity)
            {
                Thread.Sleep(100);
            }

            return true;
        }

        /// <summary>当前缓存个数</summary>
        private Int32 _count;

        private void Work(Object state)
        {
            var list = new List<IEntity>();
            var n = 0;

            // 检查是否有延迟保存
            var ds = DelayEntities;
            if (ds.Any())
            {
                var now = TimerX.Now;
                foreach (var item in ds)
                {
                    if (item.Value < now) list.Add(item.Key);

                    n++;
                }
                // 从列表删除过期
                foreach (var item in list)
                {
                    ds.Remove(item);
                }

                //n += ds.Count;
            }

            // 检查是否有近实时保存
            var es = Entities;
            if (es.Any())
            {
                // 为了速度，不拷贝，直接创建一个新的集合
                Entities = new ConcurrentDictionary<IEntity, IEntity>();
                list.AddRange(es.Keys);

                n += es.Count;
            }

            //_count = n;

            if (list.Count > 0)
            {
                //_count -= list.Count;
                Interlocked.Add(ref _count, -list.Count);

                Process(list);
            }
        }

        private void Process(Object state)
        {
            var list = state as ICollection<IEntity>;
            var ss = Session;
            var dal = ss.Dal;

            var speed = Speed;
            if (Debug || list.Count > 100_000)
            {
                var cost = speed == 0 ? 0 : list.Count * 1000 / speed;
                XTrace.WriteLine($"实体队列[{ss.TableName}/{ss.ConnName}]\t保存 {list.Count:n0}\t预测耗时 {cost:n0}ms");
            }

            var sw = Stopwatch.StartNew();

            // 分批
            var batchSize = 5000;
            for (var i = 0; i < list.Count();)
            {
                var batch = list.Skip(i).Take(batchSize).ToList();

                try
                {
                    batch.SaveWithoutValid();
                }
                catch (Exception ex)
                {
                    //// 保存失败，写回去
                    //foreach (var entity in list)
                    //{
                    //    Entities.TryAdd(entity, entity);
                    //}
                    //Interlocked.Add(ref _count, list.Count);

                    if (Error != null)
                        Error(this, new EventArgs<Exception>(ex));
                    else
                        XTrace.WriteException(ex);
                }

                i += batch.Count;
            }

            sw.Stop();

            // 根据繁忙程度动态调节
            // 大于1000个对象时，说明需要加快持久化间隔，缩小周期
            // 小于1000个对象时，说明持久化太快了，加大周期
            var p = Period;
            if (list.Count > 1000)
                p = p * 1000 / list.Count;
            else
                p = p * 1000 / list.Count;

            // 最小间隔
            if (p < 500) p = 500;
            // 最大间隔
            if (p > 5000) p = 5000;

            if (p != Period)
            {
                Period = p;
                _Timer.Period = p;
            }

            var ms = sw.Elapsed.TotalMilliseconds;
            Speed = ms == 0 ? 0 : (Int32)(list.Count * 1000 / ms);
            if (Debug || list.Count > 10000)
            {
                XTrace.WriteLine($"实体队列[{ss.TableName}/{ss.ConnName}]\t保存 {list.Count:n0}\t耗时 {ms:n0}ms\t速度 {speed:n0}tps\t周期 {p:n0}ms");
            }

            // 马上再来一次，以便于连续处理数据
            _Timer.SetNext(-1);
        }
        #endregion
    }
}