using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Log;
using XCode;
using XCode.Configuration;

/*
 *  时间点抽取流程：
 *      验证时间start，越界则退出
 *          有步进且位于第一页时，重新计算最小时间 FindAll(UpdateTime >= start, UpdateTime.Min(), 0, 0)
 *          结束时间有效时，设定末端边界
 *      抽取一批数据 list = FindAll(UpdateTime >= start, UpdateTime.Asc() & ID.Asc(), null, Row, 1000)
 *      有数据list.Count > 0
 *          last = list.Last().UpdateTime
 *          满一批，后续还有数据 list.Count == 1000
 *              最大时间行数 lastCount = list.Count(e=>UpdateTime==last)
 *              Row += lastCount
 *              滑动时间点 start = last
 *          不满一批，后续没有数据
 *              Row = 0;
 *              滑动时间点 start = last + 1
 *      没有数据
 *          有步进
 *              Row = 0;
 *              滑动时间窗口 start = end
 *      返回这一批数据
 *      
 *  时间段抽取简易流程：
 *      验证时间start
 *      抽取一段数据 list = FindAll(UpdateTime >= start && UpdateTime < end, UpdateTime.Asc() & ID.Asc(), null, 0, 0)
 */

namespace XCode.Transform
{
    /// <summary>以时间为比较点的数据抽取器</summary>
    public class TimeExtracter : ExtracterBase, IExtracter
    {
        #region 属性
        /// <summary>本批数据开始时间</summary>
        public DateTime ActualStart { get; set; }

        /// <summary>本批数据结束时间</summary>
        public DateTime ActualEnd { get; set; }
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
            FieldName = fi.Name;

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
            // 有步进且位于第一页时，重新计算最小时间
            if (set.Step > 0 && set.Row == 0) start = GetMinTime(start);
            var now = DateTime.Now.AddSeconds(-set.Offset);
            if (start >= now) return null;

            // 结束时间，必须是小于当前时间的有效值
            var end = set.Step <= 0 ? DateTime.MaxValue : start.AddSeconds(set.Step);
            // 结束时间有效时，设定末端边界
            if (set.End > DateTime.MinValue && set.End < DateTime.MaxValue && end > set.End) end = set.End;
            // 不能超过当前时间
            if (set.Step > 0 && end > now) end = now;
            // 区间无效
            if (start >= end) return null;

            ActualStart = start;
            ActualEnd = end;

            var size = set.BatchSize;
            // 如果批大小为零，表示不分批
            //if (size <= 0) size = 1000;

            // 分批获取数据，如果没有取到，则结束
            var list = FetchData(start, end, set.Row, size);
            // 取到数据，需要滑动窗口
            if (list.Count > 0)
            {
                var last = (DateTime)list.Last()[FieldName];
                // 有可能时间字段为空
                if (last <= DateTime.MinValue) last = list.Max(e => (DateTime)e[FieldName]);
                // 有可能时间超出区间
                if (last < start) last = list.Max(e => (DateTime)e[FieldName]);
                // 满一批，后续还有数据
                if (list.Count >= size)
                {
                    // 最大时间行数
                    var maxCount = list.Count(e => (DateTime)e[FieldName] == last);
                    // 以最后时间为起点，跳过若干行。注意可能产生连续分页的情况
                    if (last == set.Start)
                        set.Row += maxCount;
                    else
                        set.Row = maxCount;
                    set.Start = last;
                }
                else
                {
                    set.Start = last.AddSeconds(1);
                    set.Row = 0;
                }
            }
            else if (set.Step > 0)
            {
                set.Start = end;
                set.Row = 0;
            }

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
            if (start > DateTime.MinValue && start < DateTime.MaxValue) exp &= fi >= start;
            if (end > DateTime.MinValue && end < DateTime.MaxValue) exp &= fi < end;

            if (!Where.IsNullOrEmpty()) exp &= Where;

            return Factory.FindAll(exp, OrderBy, Selects, startRow, maxRows);
        }

        /// <summary>获取大于等于指定时间的最小修改时间</summary>
        /// <param name="start"></param>
        /// <returns></returns>
        protected virtual DateTime GetMinTime(DateTime start)
        {
            var fi = Field;
            var exp = new WhereExpression();
            if (start > DateTime.MinValue) exp &= fi >= start;

            if (!Where.IsNullOrEmpty()) exp &= Where;

            var list = Factory.FindAll(exp, null, fi.Min(), 0, 0);

            return list.Count > 0 ? (DateTime)list[0][FieldName] : DateTime.MaxValue;
        }
        #endregion
    }
}