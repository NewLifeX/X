using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using XCode.DataAccessLayer;
using XCode.Membership;

namespace XCode.Transform
{
    /// <summary>数据抽取转换处理</summary>
    /// <remarks>
    /// ETL数据抽取可以独立使用，也可以继承扩展。
    /// </remarks>
    public class ETL
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>数据源抽取器</summary>
        public IExtracter Extracter { get; set; }

        /// <summary>目标实体工厂</summary>
        public IEntityOperate Target { get; set; }

        /// <summary>逐行处理后自动保存。默认true</summary>
        public Boolean AutoSave { get; set; } = true;

        /// <summary>处理单个实体遇到错误时如何处理。默认true跳过错误，否则抛出异常</summary>
        public Boolean SkipError { get; set; } = true;
        #endregion

        #region 性能指标
        /// <summary>总数</summary>
        public Int32 Total { get; set; }

        /// <summary>次数</summary>
        public Int32 Times { get; set; }

        /// <summary>速度</summary>
        public Int32 Speed { get; set; }

        /// <summary>错误</summary>
        public Int32 Error { get; set; }

        /// <summary>错误内容</summary>
        public String Message { get; set; }
        #endregion

        #region 构造
        public ETL()
        {
            Name = GetType().Name.TrimEnd("Worker");
        }

        public ETL(IEntityOperate source, IEntityOperate target)
        {
            Extracter = new TimeExtracter { Factory = source };
            Target = target;
        }

        //protected override void OnDispose(Boolean disposing)
        //{
        //    base.OnDispose(disposing);

        //    Stop();
        //}
        #endregion

        #region 开始停止
        /// <summary>开始</summary>
        public virtual void Start()
        {
            var ext = Extracter;
            if (ext == null) ext = Extracter = new TimeExtracter();
            if (ext.Setting == null) ext.Setting = new ExtractSetting();
            ext.Init();
        }

        /// <summary>停止</summary>
        public virtual void Stop()
        {
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

            var ext = Extracter;
            //ext.Setting = set;
            try
            {
                var sw = Stopwatch.StartNew();
                Speed = 0;

                // 分批抽取
                var list = ext.Fetch();
                if (list == null || list.Count == 0) return false;

                // 批量处理
                ProcessList(list);
                sw.Stop();

                // 当前批数据处理完成，移动到下一块
                ext.SaveNext();

                var count = list.Count;
                Total += count;
                Times++;

                Message = null;

                var ms = sw.ElapsedMilliseconds;
                Speed = ms <= 0 ? 0 : (Int32)(count * 1000 / ms);

                var msg = "共同步{0}行，区间({1}, {2}, {3})，耗时 {4:n0}毫秒，{5:n0}".F(count, start, row, end, ms, Speed);
                WriteLog(msg);
                LogProvider.Provider.WriteLog(Name, "同步", msg);
            }
            catch (Exception ex)
            {
                Error++;
                Message = ex?.GetTrue()?.Message;
            }

            return true;
        }

        /// <summary>处理列表，批量事务提交</summary>
        /// <param name="list"></param>
        protected virtual void ProcessList(IEntityList list)
        {
            // 批量提交
            using (var tran = Target.CreateTrans())
            {
                foreach (var entity in list)
                {
                    try
                    {
                        ProcessItem(entity);
                    }
                    catch (Exception ex)
                    {
                        ex = OnError(entity, ex);
                        if (ex != null) throw ex;
                    }
                }
                tran.Commit();
            }
        }

        /// <summary>处理单行数据</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual IEntity ProcessItem(IEntity entity)
        {
            var key = entity[Extracter.Factory.Unique.Name];

            // 查找目标，如果不存在则创建
            var flag = true;
            var e = Target.FindByKey(key);
            if (e == null)
            {
                e = Target.Create();
                e[Target.Unique.Name] = key;
                flag = false;
            }

            // 同名字段对拷
            e.CopyFrom(entity, true);

            // 自动保存
            if (AutoSave)
            {
                if (flag)
                    e.Update();
                else
                    e.Insert();
            }

            return e;
        }

        /// <summary>处理单个实体遇到错误时如何处理</summary>
        /// <param name="entity"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual Exception OnError(IEntity entity, Exception ex)
        {
            ex = ex?.GetTrue();
            if (ex == null) return null;

            if (!SkipError) return ex;

            // 跳过错误时，把错误记下来
            Error++;
            Message = ex.Message;

            Log?.Error(ex.Message);

            return null;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public NewLife.Log.ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(Name + " " + format, args);
        }
        #endregion
    }
}