using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Log;
using NewLife.Threading;
using XCode.Membership;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

/*
 * 数据抽取流程：
 *      Start   启动检查抽取器和抽取设置
 *      Process 大循环处理
 *          Processing  检查启动
 *          克隆一份抽取配置，抽取时会滑动到下一批
 *          Fetch   抽取一批数据，并滑动配置
 *          ProcessList 处理列表，可异步调用
 *              OnProcess   处理列表，异步调用前，先新增配置项，以免失败
 *                  ProcessItem 处理实体
 *                  OnError     处理实体异常
 *              OnSync      同步列表
 *                  SyncItem    同步实体
 *                      GetItem     查找或新建目标对象
 *                      SaveItem    保存目标对象
 *              ProcessFinished 处理完成，保存统计，异步时修改配置项为成功
 *          OnError     处理列表异常
 *          Processed   保存进度
 *      Stop    停止处理
 */

namespace XCode.Transform
{
    /// <summary>数据分批统计</summary>
    /// <typeparam name="TSource">源实体类</typeparam>
    public class ETL<TSource> : ETL
        where TSource : Entity<TSource>, new()
    {
        #region 构造
        /// <summary>实例化数据抽取器</summary>
        public ETL() : base(Entity<TSource>.Meta.Factory) { }
        #endregion
    }

    /// <summary>数据抽取转换处理</summary>
    /// <remarks>
    /// ETL数据抽取可以独立使用，也可以继承扩展。
    /// 同步数据或数据分批统计。
    /// </remarks>
    public class ETL
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>数据源抽取器</summary>
        public IExtracter Extracter { get; set; }

        /// <summary>数据源抽取设置</summary>
        public IExtractSetting Setting { get; set; }

        /// <summary>最大错误数，连续发生多个错误时停止</summary>
        public Int32 MaxError { get; set; }

        /// <summary>当前累计连续错误次数</summary>
        private Int32 _Error;

        /// <summary>统计</summary>
        public IETLStat Stat { get; set; }

        /// <summary>最大并行处理任务数。默认0表示同步，N表示最多可以有N批数据同时处理</summary>
        public Int32 MaxTask { get; set; }

        /// <summary>当前处理中的任务数</summary>
        private Int32 _currentTask;

        /// <summary>过滤模块列表</summary>
        public IETLModule Module { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化数据抽取器</summary>
        public ETL()
        {
            Name = GetType().Name.TrimEnd("Worker");
        }

        /// <summary>实例化数据抽取器</summary>
        /// <param name="source"></param>
        public ETL(IEntityOperate source) : this()
        {
            Extracter = new TimeExtracter { Factory = source };
        }
        #endregion

        #region 开始停止
        /// <summary>开始</summary>
        public virtual void Start()
        {
            Module?.Start();

            var ext = Extracter;
            if (ext == null) throw new ArgumentNullException(nameof(Extracter), "没有设置数据抽取器");

            //if (ext.Setting == null) ext.Setting = new ExtractSetting();
            ext.Init();

            if (Stat == null) Stat = new ETLStat();

            // 指定轮询周期，表示定时执行，而不使用服务
            if (Period > 0) _timer = new TimerX(Loop, null, 100, Period * 1000, "ETL") { Async = true };
        }

        /// <summary>停止</summary>
        public virtual void Stop()
        {
            _Inited = false;

            _timer.TryDispose();
            Module?.Stop();
        }
        #endregion

        #region 数据转换
        private Boolean _Inited;
        /// <summary>每一轮启动时</summary>
        /// <param name="set"></param>
        /// <returns></returns>
        protected virtual Boolean Init(IExtractSetting set)
        {
            WriteLog("开始处理{0}，区间({1} + {3:n0}, {2})", Name, set.Start, set.End, set.Row);

            Module?.Init();

            return true;
        }

        /// <summary>抽取并处理一批数据</summary>
        /// <returns>返回抽取数据行数，没有数据返回0，初始化或配置失败返回-1</returns>
        public virtual Int32 Process()
        {
            var ctx = new DataContext();
            if (Module != null && !Module.Processing(ctx))
            {
                _Inited = false;
                return -1;
            }

            var set = ctx.Setting ?? Setting;

            // 最后一次处理之前，重新启动
            if (!_Inited)
            {
                if (!Init(set)) return -1;
                _Inited = true;
            }

            // 拷贝配置，支持多线程
            ctx.Setting = set.Clone();

            var ext = Extracter;
            IList<IEntity> list = null;
            try
            {
                var sw = Stopwatch.StartNew();

                // 分批抽取
                list = Fetch(ctx, ext, set);
                if (list == null || list.Count == 0) return 0;
                sw.Stop();

                // 抽取出来后，先保存统计数据再批量处理
                ctx.Data = list;
                ctx.FetchCost = sw.Elapsed.TotalMilliseconds;

                var st = Stat;
                st.Total += list.Count;
                st.Times++;
                //st.FetchSpeed = ctx.FetchSpeed;

                Module?.Fetched(ctx);
            }
            catch (Exception ex)
            {
                ctx.Error = ex;
                ex = OnError(ctx);
                if (ex != null) throw ex;
            }
            // 抛出异常后，可能数据列表为空
            if (list == null || list.Count == 0) return 0;

            /*
             * 并行计算逻辑：
             * 1，参数0表示同步
             * 2，参数1表示开一个异步任务
             * 3，参数n表示开多个异步任务
             * 4，检查资源数，不足时等待
             */

            // 批量处理
            if (MaxTask == 0)
                ProcessList(ctx);
            else
                ProcessListAsync(ctx);

            Module?.Processed(ctx);

            return list == null ? 0 : list.Count;
        }

        /// <summary>抽取一批数据</summary>
        /// <param name="ctx">数据上下文</param>
        /// <param name="extracter"></param>
        /// <param name="set">设置</param>
        /// <returns></returns>
        internal protected virtual IList<IEntity> Fetch(DataContext ctx, IExtracter extracter, IExtractSetting set)
        {
            return extracter?.Fetch(set);
        }

        /// <summary>处理列表，传递批次配置，支持多线程和异步</summary>
        /// <remarks>
        /// 子类可以根据需要重载该方法，实现异步处理。
        /// 异步处理之前，需要先保存配置
        /// </remarks>
        /// <param name="ctx">数据上下文</param>
        internal protected void ProcessList(DataContext ctx)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var rs = OnProcess(ctx);
                if (rs != null && ctx.Success == 0) ctx.Success = rs.Count;

                sw.Stop();
                ctx.ProcessCost = sw.Elapsed.TotalMilliseconds;

                OnFinished(ctx);
            }
            catch (Exception ex)
            {
                ctx.Error = ex;
                ex = OnError(ctx);
                if (ex != null) throw ex;
            }
        }

        /// <summary>异步处理列表，传递批次配置</summary>
        /// <param name="ctx">数据上下文</param>
        internal protected void ProcessListAsync(DataContext ctx)
        {
            var cur = _currentTask;
            // 当前任务已达上限，或者出现多线程争夺时，等待一段时间
            while (cur >= MaxTask || Interlocked.CompareExchange(ref _currentTask, cur + 1, cur) != cur)
            {
                Thread.Sleep(100);
                cur = _currentTask;
            }

            ThreadPoolX.QueueUserWorkItem(s =>
            {
                try
                {
                    ProcessList(s);
                }
                finally
                {
                    Interlocked.Decrement(ref _currentTask);
                }
            }, ctx);
        }

        /// <summary>批量处理数据列表，可重载打开事务保护</summary>
        /// <param name="ctx">数据上下文</param>
        protected virtual IList<IEntity> OnProcess(DataContext ctx)
        {
            var list = new List<IEntity>();
            foreach (var source in ctx.Data)
            {
                ctx.Entity = source;
                try
                {
                    var rs = ProcessItem(ctx, source);
                    if (rs != null) list.Add(rs);
                }
                catch (Exception ex)
                {
                    ctx.Error = ex;
                    ex = OnError(ctx);
                    if (ex != null) throw ex;
                }
            }
            ctx.Entity = null;

            return list;
        }

        /// <summary>处理完成</summary>
        /// <param name="ctx">数据上下文</param>
        protected virtual void OnFinished(DataContext ctx)
        {
            // 累计错误清零
            _Error = 0;

            var set = ctx.Setting;

            var st = Stat;
            var total = ctx.Data.Count;
            st.Success += ctx.Success;

            //st.Speed = ctx.ProcessSpeed;

            var end = set.End;
            if (Extracter is TimeExtracter ext) end = ext.ActualEnd;
            var ends = end > DateTime.MinValue && end < DateTime.MaxValue ? ", {0}".F(end) : "";
            WriteLog("共处理{0}行，区间({1}, {2})，抓取{4:n0}ms，{5:n0}qps，处理{6:n0}ms，{7:n0}tps", total, set.Start, set.Row, ends, ctx.FetchCost, ctx.FetchSpeed, ctx.ProcessCost, ctx.ProcessSpeed);

            Module?.OnFinished(ctx);
        }

        /// <summary>处理单行数据</summary>
        /// <param name="ctx">数据上下文</param>
        /// <param name="source">源实体</param>
        /// <returns></returns>
        protected virtual IEntity ProcessItem(DataContext ctx, IEntity source) { return source; }

        private Exception _lastError;
        /// <summary>遇到错误时如何处理</summary>
        /// <param name="ctx">数据上下文</param>
        /// <returns></returns>
        protected virtual Exception OnError(DataContext ctx)
        {
            Module?.OnError(ctx);

            var ex = ctx.Error;
            // 处理单个实体时的异常，会被外层捕获，需要判断跳过
            if (_lastError == ex) return ex;

            ex = ex?.GetTrue();
            if (ex == null) return null;

            _Error++;
            if (MaxError > 0 && _Error >= MaxError) return _lastError = ex;

            // 跳过错误时，把错误记下来
            var st = Stat;
            st.Error++;
            st.Message = ex.Message;

            if (ShowError) WriteError(ex.ToString());

            return null;
        }
        #endregion

        #region 调度
        /// <summary>定时轮询周期。默认0秒表示不打开</summary>
        public Int32 Period { get; set; }

        private TimerX _timer;
        void Loop(Object state)
        {
            var count = Process();

            // 如果有数据，马上开始下一轮
            if (count > 0) TimerX.Current.SetNext(-1);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public NewLife.Log.ILog Log { get; set; } = Logger.Null;

        /// <summary>数据库日志提供者</summary>
        public LogProvider Provider { get; set; }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(Name + " " + format, args);

            Provider?.WriteLog(Name, "处理", format.F(args));
        }

        /// <summary>显示错误日志。默认true</summary>
        public Boolean ShowError { get; set; } = true;

        /// <summary>写错误日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WriteError(String format, params Object[] args)
        {
            Log?.Error(Name + " " + format, args);

            Provider?.WriteLog(Name, "错误", format.F(args));
        }
        #endregion
    }
}