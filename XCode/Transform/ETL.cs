using System;
using System.Collections.Generic;
using System.Diagnostics;
using NewLife.Log;
using XCode.Membership;

/*
 * 数据抽取流程：
 *      Start   启动检查抽取器和抽取设置
 *      Process 大循环处理
 *          克隆一份抽取配置，抽取时会滑动到下一批
 *          Fetch   抽取一批数据，并滑动配置
 *              ProcessList 处理列表，可异步调用
 *                  OnProcess   处理列表，异步调用前，先新增配置项，以免失败
 *                      ProcessItem 处理实体
 *                      OnError     处理实体异常
 *                  OnSync      同步列表
 *                      SyncItem    同步实体
 *                          GetItem     查找或新建目标对象
 *                          SaveItem    保存目标对象
 *                  ProcessFinished 处理完成，保存统计，异步时修改配置项为成功
 *              OnError     处理列表异常
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

        /// <summary>最大错误数，连续发生多个错误时停止</summary>
        public Int32 MaxError { get; set; }

        /// <summary>当前累计连续错误次数</summary>
        private Int32 _Error;

        /// <summary>统计</summary>
        public IETLStat Stat { get; set; }

        /// <summary>过滤模块列表</summary>
        public List<IETLModule> Modules { get; set; } = new List<IETLModule>();
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
            Modules.Start();

            var ext = Extracter;
            if (ext == null) throw new ArgumentNullException(nameof(Extracter), "没有设置数据抽取器");

            //if (ext.Setting == null) ext.Setting = new ExtractSetting();
            ext.Init();

            if (Stat == null) Stat = new ETLStat();
        }

        /// <summary>停止</summary>
        public virtual void Stop()
        {
            _Inited = false;

            Modules.Stop();
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

            Modules.Init();

            return true;
        }

        /// <summary>抽取并处理一批数据</summary>
        /// <returns>返回抽取数据行数，没有数据返回0，初始化或配置失败返回-1</returns>
        public virtual Int32 Process()
        {
            if (!Modules.Processing()) { _Inited = false; return -1; }

            var set = Extracter.Setting;

            if (!_Inited)
            {
                if (!Init(set)) return -1;
                _Inited = true;
            }

            // 拷贝配置，支持多线程
            var set2 = set.Clone();

            var ext = Extracter;
            IList<IEntity> list = null;
            try
            {
                var sw = Stopwatch.StartNew();

                // 分批抽取
                list = Fetch(ext);
                if (list == null || list.Count == 0) return 0;
                sw.Stop();

                // 批量处理
                ProcessList(list, set2, sw.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                ex = OnError(list, set2, ex);
                if (ex != null) throw ex;
            }

            Modules.Processed();

            return list == null ? 0 : list.Count;
        }

        /// <summary>抽取一批数据</summary>
        /// <param name="extracter"></param>
        /// <returns></returns>
        protected virtual IList<IEntity> Fetch(IExtracter extracter)
        {
            return extracter?.Fetch();
        }

        /// <summary>处理列表，传递批次配置，支持多线程和异步</summary>
        /// <remarks>
        /// 子类可以根据需要重载该方法，实现异步处理。
        /// 异步处理之前，需要先保存配置
        /// </remarks>
        /// <param name="list">实体列表</param>
        /// <param name="set">本批次配置</param>
        /// <param name="fetchCost">抽取数据耗时</param>
        protected virtual void ProcessList(IList<IEntity> list, IExtractSetting set, Double fetchCost)
        {
            var sw = Stopwatch.StartNew();

            var count = OnProcess(list, set);

            sw.Stop();

            OnFinished(list, set, count, fetchCost, sw.Elapsed.TotalMilliseconds);
        }

        /// <summary>处理列表</summary>
        /// <param name="list">实体列表</param>
        /// <param name="set">本批次配置</param>
        protected virtual Int32 OnProcess(IList<IEntity> list, IExtractSetting set)
        {
            var count = 0;
            foreach (var source in list)
            {
                try
                {
                    ProcessItem(source);

                    count++;
                }
                catch (Exception ex)
                {
                    ex = OnError(source, set, ex);
                    if (ex != null) throw ex;
                }
            }

            return count;
        }

        /// <summary>处理完成</summary>
        /// <param name="list">实体列表</param>
        /// <param name="set">本批次配置</param>
        /// <param name="success">成功行数</param>
        /// <param name="fetchCost">抽取数据耗时</param>
        /// <param name="processCost">处理数据耗时</param>
        protected virtual void OnFinished(IList<IEntity> list, IExtractSetting set, Int32 success, Double fetchCost, Double processCost)
        {
            // 累计错误清零
            _Error = 0;

            var ext = Extracter;
            var start = set.Start;
            var end = set.End;
            var row = set.Row;

            var st = Stat;
            var total = list.Count;
            st.Total += total;
            st.Success += success;
            st.Times++;

            st.Speed = processCost <= 0 ? 0 : (Int32)(total * 1000 / processCost);
            st.FetchSpeed = fetchCost <= 0 ? 0 : (Int32)(total * 1000 / fetchCost);

            if (ext is TimeExtracter time) end = time.ActualEnd;
            var ends = end > DateTime.MinValue && end < DateTime.MaxValue ? ", {0}".F(end) : "";
            WriteLog("共处理{0}行，区间({1}, {2}{3})，抓取{4:n0}ms，{5:n0}qps，处理{6:n0}ms，{7:n0}tps", total, start, row, ends, fetchCost, st.FetchSpeed, processCost, st.Speed);

            Modules.OnFinished(list, set, success, fetchCost, processCost);
        }

        /// <summary>处理单行数据</summary>
        /// <remarks>打开AutoSave时，上层ProcessList会自动保存数据</remarks>
        /// <param name="source">源实体</param>
        /// <returns></returns>
        protected virtual IEntity ProcessItem(IEntity source)
        {
            return source;
        }

        private Exception _lastError;
        /// <summary>遇到错误时如何处理</summary>
        /// <param name="source"></param>
        /// <param name="set">本批次配置</param>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual Exception OnError(Object source, IExtractSetting set, Exception ex)
        {
            Modules.OnError(source, set, ex);

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

            WriteError(ex.ToString());

            return null;
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

        /// <summary>写错误日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteError(String format, params Object[] args)
        {
            Log?.Error(Name + " " + format, args);

            Provider?.WriteLog(Name, "错误", format.F(args));
        }
        #endregion
    }
}