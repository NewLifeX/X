using System;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using NewLife.Log;
using XCode;
using XCode.Configuration;

/*
 *  时间段抽取流程：
 *      验证时间start
 *      抽取一段数据 list = FindAll(UpdateTime >= start && UpdateTime < end, UpdateTime.Asc() & ID.Asc(), null, Row, 0)
 */

namespace XCode.Transform
{
    /// <summary>基于时间片的数据抽取器</summary>
    public class TimeSpanExtracter : ExtracterBase, IExtracter
    {
        #region 属性
        #endregion

        #region 方法
        /// <summary>初始化</summary>
        public override void Init()
        {
            var fi = Field;
            // 自动找时间字段
            if (fi == null && FieldName.IsNullOrEmpty()) fi = Field = Factory.MasterTime;

            base.Init();

            fi = Field;
            if (fi == null) throw new ArgumentNullException(nameof(FieldName), "未指定用于顺序抽取数据的时间字段！");

            // 先按时间升序，再按主键升序，避免同一秒存在多行数据时，数据顺序不统一
            var sort = fi.Asc();
            var uq = Factory.Unique;
            if (uq != null && uq.Name != fi.Name) sort &= uq.Asc();

            OrderBy = sort;
        }
        #endregion

        #region 抽取数据
        /// <summary>抽取一批数据</summary>
        /// <param name="set">设置</param>
        /// <returns></returns>
        public virtual IList<IEntity> Fetch(IExtractSetting set)
        {
            if (Field == null) throw new ArgumentNullException(nameof(FieldName), "未指定用于顺序抽取数据的时间字段！");
            if (set == null) throw new ArgumentNullException(nameof(set), "没有设置数据抽取配置");

            // 验证时间段
            var start = set.Start;
            var end = set.End;

            // 区间无效
            if (start >= end) return null;

            var size = set.BatchSize;

            // 分批获取数据，如果没有取到，则结束
            var list = FetchData(start, end, set.Row, size);
            // 取到数据，需要滑动窗口
            if (list.Count > 0)
                set.Row += list.Count;
            else
                set.Row = 0;

            return list;
        }

        /// <summary>分段分页抽取数据</summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="startRow"></param>
        /// <param name="maxRows"></param>
        /// <returns></returns>
        protected virtual IList<IEntity> FetchData(DateTime start, DateTime end, Int32 startRow, Int32 maxRows)
        {
            var fi = Field;
            //var exp = fi.Between(start, end);
            // (2017-11-08 23:59:30, 2017-11-09 00:00:00)因Between的BUG变成了(2017-11-08 23:59:30, 2017-11-10 00:00:00)
            var exp = new WhereExpression();
            if (start > DateTime.MinValue) exp &= fi >= start;
            if (end > DateTime.MinValue) exp &= fi < end;

            if (!Where.IsNullOrEmpty()) exp &= Where;

            return Factory.FindAll(exp, OrderBy, Selects, startRow, maxRows);
        }
        #endregion
    }
}