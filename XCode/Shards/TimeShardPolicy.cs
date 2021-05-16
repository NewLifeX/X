using System;
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
                var time = entity[Field.Name].ToDateTime();
                if (time.Year <= 1) throw new XCodeException("实体对象时间字段为空，无法用于分表");

                return Get(time);
            }
            else if (fi.Type == typeof(Int64))
            {
                var id = entity[Field.Name].ToLong();
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
            if (ConnPolicy.IsNullOrEmpty() && TablePolicy.IsNullOrEmpty()) return null;

            var table = Field.Factory.Table;

            var model = new ShardModel();
            if (!ConnPolicy.IsNullOrEmpty()) model.ConnName = String.Format(ConnPolicy, table.ConnName, time);
            if (!TablePolicy.IsNullOrEmpty()) model.TableName = String.Format(TablePolicy, table.TableName, time);

            return model;
        }
    }
}