using System;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using XCode.Configuration;

namespace XCode.Shards
{
    /// <summary>时间分表策略</summary>
    public class TimeShardPolicy : IShardPolicy
    {
        #region 属性
        ///// <summary>实体工厂</summary>
        //public IEntityFactory Factory { get; set; }

        /// <summary>字段</summary>
        public FieldItem Field { get; set; }

        /// <summary>连接名策略。格式化字符串，0位基础连接名，1位时间，如{0}_{1:yyyy}</summary>
        public String ConnPolicy { get; set; }

        /// <summary>表名策略。格式化字符串，0位基础表名，1位时间，如{0}_{1:yyyyMM}</summary>
        public String TablePolicy { get; set; }

        /// <summary>时间区间步进。遇到时间区间需要扫描多张表时的时间步进，默认1天</summary>
        public TimeSpan Step { get; set; } = TimeSpan.FromDays(1);

        /// <summary>允许搜索</summary>
        public Boolean AllowSearch { get; set; }
        #endregion

        /// <summary>为实体对象计算分表分库</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual ShardModel Get(IEntity entity)
        {
            var fi = Field;
            if (fi == null) throw new XCodeException("分表策略要求指定时间字段！");

            if (fi.Type == typeof(DateTime))
            {
                var time = entity[fi.Name].ToDateTime();
                if (time.Year <= 1) throw new XCodeException("实体对象时间字段为空，无法用于分表");

                return Get(time);
            }
            else if (fi.Type == typeof(Int64))
            {
                var id = entity[fi.Name].ToLong();
                if (!fi.Factory.Snow.TryParse(id, out var time, out _, out _)) throw new XCodeException("雪花Id解析时间失败，无法用于分表");

                return Get(time);
            }

            throw new XCodeException($"时间分表策略不支持[{fi.Type.FullName}]类型字段");
        }

        /// <summary>为时间计算分表分库</summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public virtual ShardModel Get(DateTime time)
        {
            if (time.Year <= 1) throw new ArgumentNullException(nameof(time), "分表策略要求指定时间！");

            var fi = Field;
            if (fi == null) throw new XCodeException("分表策略要求指定时间字段！");

            if (ConnPolicy.IsNullOrEmpty() && TablePolicy.IsNullOrEmpty()) return null;

            var table = fi.Factory.Table;

            var model = new ShardModel();
            if (!ConnPolicy.IsNullOrEmpty()) model.ConnName = String.Format(ConnPolicy, table.ConnName, time);
            if (!TablePolicy.IsNullOrEmpty()) model.TableName = String.Format(TablePolicy, table.TableName, time);

            return model;
        }

        /// <summary>从查询表达式中计算多个分表分库</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public virtual ShardModel[] Gets(Expression expression)
        {
            if (!AllowSearch) return null;

            if (expression is not WhereExpression where) throw new XCodeException($"分表策略要求指定[{Field}]字段！");

            // 时间范围查询
            var fi = Field;
            var exps = where.Where(e => e is FieldExpression fe && fe.Field == fi).Cast<FieldExpression>().ToList();
            if (exps.Count == 0) throw new XCodeException($"分表策略要求查询条件包括[{fi}]字段！");

            if (fi.Type == typeof(DateTime))
            {
                var sf = exps.FirstOrDefault(e => e.Action == ">" || e.Action == ">=");
                var ef = exps.FirstOrDefault(e => e.Action == "<" || e.Action == "<=");
                if (sf != null)
                {
                    var start = sf.Value.ToDateTime();
                    if (start.Year > 1)
                    {
                        var end = DateTime.Now;

                        if (ef != null)
                        {
                            var time = ef.Value.ToDateTime();
                            if (time.Year > 1) end = time;
                        }

                        return GetModels(start, end);
                    }
                }
            }
            else if (fi.Type == typeof(Int64))
            {
                var sf = exps.FirstOrDefault(e => e.Action == ">" || e.Action == ">=");
                var ef = exps.FirstOrDefault(e => e.Action == "<" || e.Action == "<=");
                if (sf != null)
                {
                    var id = sf.Value.ToLong();
                    if (fi.Factory.Snow.TryParse(id, out var time, out _, out _))
                    {
                        var start = time;
                        var end = DateTime.Now;

                        if (ef != null && fi.Factory.Snow.TryParse(ef.Value.ToLong(), out var time2, out _, out _))
                        {
                            end = time2;
                        }

                        return GetModels(start, end);
                    }
                }
            }

            throw new XCodeException("分表策略因条件不足无法执行分表查询操作！");
        }

        private ShardModel[] GetModels(DateTime start, DateTime end)
        {
            var models = new List<ShardModel>();

            // 构建了一个时间区间 start <= @fi < end
            // 简单起见，按照分钟步进
            ShardModel last = null;
            for (var dt = start; dt < end; dt = dt.Add(Step))
            {
                var model = Get(dt);
                if (last == null || model.ConnName != last.ConnName || model.TableName != last.TableName)
                {
                    models.Add(model);
                    last = model;
                }
            }
            return models.ToArray();
        }
    }
}