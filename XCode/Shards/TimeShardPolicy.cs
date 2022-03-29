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
        /// <summary>实体工厂</summary>
        public IEntityFactory Factory { get; set; }

        /// <summary>字段</summary>
        public FieldItem Field { get; set; }

        /// <summary>连接名策略。格式化字符串，0位基础连接名，1位时间，如{0}_{1:yyyy}</summary>
        public String ConnPolicy { get; set; }

        /// <summary>表名策略。格式化字符串，0位基础表名，1位时间，如{0}_{1:yyyyMM}</summary>
        public String TablePolicy { get; set; }

        /// <summary>时间区间步进。遇到时间区间需要扫描多张表时的时间步进，默认1天</summary>
        public TimeSpan Step { get; set; } = TimeSpan.FromDays(1);

        private readonly String _fieldName;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public TimeShardPolicy() { }

        /// <summary>指定字段实例化分表策略</summary>
        /// <param name="field"></param>
        /// <param name="factory"></param>
        public TimeShardPolicy(FieldItem field, IEntityFactory factory = null)
        {
            Field = field;
            Factory = factory ?? field.Factory;
        }

        /// <summary>指定字段名和工厂实例化分表策略</summary>
        /// <param name="fieldName"></param>
        /// <param name="factory"></param>
        public TimeShardPolicy(String fieldName, IEntityFactory factory)
        {
            _fieldName = fieldName;
            Factory = factory;
        }

        private FieldItem GetField() => Field ??= Factory.Table.FindByName(_fieldName);
        #endregion

        /// <summary>为实体对象、时间、雪花Id等计算分表分库</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual ShardModel Shard(Object value)
        {
            if (value is IEntity entity) return Shard(entity);
            if (value is DateTime dt) return Shard(dt);
            if (value is Int64 id)
            {
                if (!Factory.Snow.TryParse(id, out var time, out _, out _)) throw new XCodeException("雪花Id解析时间失败，无法用于分表");

                return Shard(time);
            }

            throw new XCodeException($"分表策略无法识别数据[{value}]");
        }

        /// <summary>为实体对象计算分表分库</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual ShardModel Shard(IEntity entity)
        {
            var fi = GetField();
            if (fi == null) throw new XCodeException("分表策略要求指定时间字段！");

            if (fi.Type == typeof(DateTime))
            {
                var time = entity[fi.Name].ToDateTime();
                if (time.Year <= 1) throw new XCodeException("实体对象时间字段为空，无法用于分表");

                return Shard(time);
            }
            else if (fi.Type == typeof(Int64))
            {
                var id = entity[fi.Name].ToLong();
                if (!Factory.Snow.TryParse(id, out var time, out _, out _)) throw new XCodeException("雪花Id解析时间失败，无法用于分表");

                return Shard(time);
            }

            throw new XCodeException($"时间分表策略不支持[{fi.Type.FullName}]类型字段");
        }

        /// <summary>为时间计算分表分库</summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public virtual ShardModel Shard(DateTime time)
        {
            if (time.Year <= 1) throw new ArgumentNullException(nameof(time), "分表策略要求指定时间！");

            var fi = GetField();
            if (fi == null) throw new XCodeException("分表策略要求指定时间字段！");

            if (ConnPolicy.IsNullOrEmpty() && TablePolicy.IsNullOrEmpty()) return null;

            var table = Factory.Table;

            var model = new ShardModel();
            if (!ConnPolicy.IsNullOrEmpty()) model.ConnName = String.Format(ConnPolicy, table.ConnName, time);
            if (!TablePolicy.IsNullOrEmpty()) model.TableName = String.Format(TablePolicy, table.TableName, time);

            return model;
        }

        /// <summary>从时间区间中计算多个分表分库，支持倒序。步进由Step指定，默认1天</summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public virtual ShardModel[] Shards(DateTime start, DateTime end)
        {
            if (start.Year <= 1) throw new ArgumentNullException(nameof(start), "分表策略要求指定时间！");
            if (end.Year <= 1) throw new ArgumentNullException(nameof(end), "分表策略要求指定时间！");

            if (start <= end) return GetModels(start, end);

            var arr = GetModels(end, start);
            Array.Reverse(arr);
            return arr;
        }

        /// <summary>从查询表达式中计算多个分表分库</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public virtual ShardModel[] Shards(Expression expression)
        {
            //if (expression is not WhereExpression where) return null;

            // 时间范围查询，用户可能自己写分表查询
            var fi = GetField();
            var exps = new List<FieldExpression>();
            if (expression is WhereExpression where)
                exps = where.Where(e => e is FieldExpression fe && fe.Field == fi).Cast<FieldExpression>().ToList();
            else if (expression is FieldExpression fieldExpression && fieldExpression.Field == fi)
                exps.Add(fieldExpression);
            //if (exps.Count == 0) throw new XCodeException($"分表策略要求查询条件包括[{fi}]字段！");
            if (exps.Count == 0) return null;

            if (fi.Type == typeof(DateTime))
            {
                var sf = exps.FirstOrDefault(e => e.Action is ">" or ">=");
                var ef = exps.FirstOrDefault(e => e.Action is "<" or "<=");
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
                var sf = exps.FirstOrDefault(e => e.Action is ">" or ">=");
                var ef = exps.FirstOrDefault(e => e.Action is "<" or "<=");
                if (sf != null)
                {
                    var id = sf.Value.ToLong();
                    if (Factory.Snow.TryParse(id, out var time, out _, out _))
                    {
                        var start = time;
                        var end = DateTime.Now;

                        if (ef != null && Factory.Snow.TryParse(ef.Value.ToLong(), out var time2, out _, out _))
                        {
                            end = time2;
                        }

                        return GetModels(start, end);
                    }
                }

                var eq = exps.FirstOrDefault(e => e.Action == "=");
                if (eq != null)
                {
                    var id = eq.Value.ToLong();
                    if (Factory.Snow.TryParse(id, out var time, out _, out _))
                        return new[] { Shard(time) };
                }
            }

            throw new XCodeException("分表策略因条件不足无法执行分表查询操作！");
        }

        private ShardModel[] GetModels(DateTime start, DateTime end)
        {
            var models = new List<ShardModel>();

            var hash = new HashSet<String>();
            for (var dt = start; dt < end; dt = dt.Add(Step))
            {
                var model = Shard(dt);
                var key = $"{model.ConnName}#{model.TableName}";
                if (key != "#" && !hash.Contains(key))
                {
                    models.Add(model);
                    hash.Add(key);
                }
            }

            // 标准时间区间 start <= @fi < end ，但是要考虑到end有一部分落入新的分片，减一秒判断
            {
                var model = Shard(end.AddSeconds(1));
                var key = $"{model.ConnName}#{model.TableName}";
                if (key != "#" && !hash.Contains(key))
                {
                    models.Add(model);
                    hash.Add(key);
                }
            }

            return models.ToArray();
        }
    }
}