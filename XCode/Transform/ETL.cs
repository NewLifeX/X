using System;
using System.Collections.Generic;
using System.Diagnostics;
using NewLife.Log;
using XCode.Membership;

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
            var ext = Extracter;
            if (ext == null) ext = Extracter = new TimeExtracter();
            if (ext.Setting == null) ext.Setting = new ExtractSetting();
            ext.Init();

            if (Stat == null) Stat = new ETLStat();
        }

        /// <summary>停止</summary>
        public virtual void Stop() { }
        #endregion

        #region 数据转换
        private Boolean _Inited;
        /// <summary>每一轮启动时</summary>
        /// <param name="set"></param>
        /// <returns></returns>
        protected virtual Boolean Init(IExtractSetting set)
        {
            WriteLog("开始处理{0}，区间({1} + {3:n0}, {2})", Name, set.Start, set.End, set.Row);

            return true;
        }

        /// <summary>抽取并处理一批数据</summary>
        /// <returns></returns>
        public virtual Boolean Process()
        {
            var set = Extracter.Setting;
            if (set == null || !set.Enable) return _Inited = false;

            if (!_Inited)
            {
                if (!Init(set)) return false;
                _Inited = true;
            }

            var start = set.Start;
            var end = set.End;
            var row = set.Row;

            var st = Stat;
            st.Message = null;

            var ext = Extracter;
            //ext.Setting = set;
            IList<IEntity> list = null;
            try
            {
                var sw = Stopwatch.StartNew();
                st.Speed = 0;

                // 分批抽取
                list = ext.Fetch();
                if (list == null || list.Count == 0) return false;

                // 批量处理
                ProcessList(list);
                sw.Stop();

                // 当前批数据处理完成，移动到下一块
                ext.SaveNext();

                // 累计错误清零
                _Error = 0;

                var count = list.Count;
                //st.Total += count;
                st.Times++;

                var ms = sw.ElapsedMilliseconds;
                st.Speed = ms <= 0 ? 0 : (Int32)(count * 1000 / ms);

                if (ext is TimeExtracter time) end = time.BatchEnd;
                var ends = end > DateTime.MinValue && end < DateTime.MaxValue ? ", {0}".F(end) : "";
                var msg = "共处理{0}行，区间({1}, {2}{3})，{4:n0}ms，{5:n0}tps".F(count, start, row, ends, ms, st.Speed);
                WriteLog(msg);
                //LogProvider.Provider.WriteLog(Name, "同步", msg);
            }
            catch (Exception ex)
            {
                ex = OnError(list, ex);
                if (ex != null) throw ex;
            }

            return true;
        }

        /// <summary>处理列表，批量事务提交</summary>
        /// <param name="list"></param>
        protected virtual void ProcessList(IList<IEntity> list)
        {
            foreach (var source in list)
            {
                try
                {
                    ProcessItem(source);
                }
                catch (Exception ex)
                {
                    ex = OnError(source, ex);
                    if (ex != null) throw ex;
                }
            }
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
        /// <summary>处理单个实体遇到错误时如何处理</summary>
        /// <param name="source"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual Exception OnError(Object source, Exception ex)
        {
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