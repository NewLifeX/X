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
            if (fi == null || fi.Type != typeof(DateTime)) throw new XCodeException("分表策略要求指定时间字段！");

            var time = entity[Field.Name].ToDateTime();
            if (time.Year <= 1) throw new XCodeException("实体对象时间字段为空，无法用于分表");

            return Get(time);
        }

        /// <summary>为时间计算分表分库</summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public virtual ShardModel Get(DateTime time)
        {
            if (ConnPolicy.IsNullOrEmpty() && TablePolicy.IsNullOrEmpty()) return null;

            var table =Field.Factory.Table;

            var model = new ShardModel();
            if (!ConnPolicy.IsNullOrEmpty()) model.ConnName = String.Format(ConnPolicy, table.ConnName, time);
            if (!TablePolicy.IsNullOrEmpty()) model.TableName = String.Format(TablePolicy, table.TableName, time);

            return model;
        }
    }
}