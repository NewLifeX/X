using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Log;
using XCode;
using XCode.Configuration;

/*
 *  自增抽取流程：
 *      抽取一段数据 list = FindAll(ID > lastid, ID.Asc() & ID.Asc(), null, 0, 1000)
 */

namespace XCode.Transform
{
    /// <summary>以自增列为比较点的数据抽取器</summary>
    public class IdentityExtracter : ExtracterBase, IExtracter
    {
        #region 属性
        #endregion

        #region 方法
        /// <summary>初始化</summary>
        public override void Init()
        {
            var fi = Field;
            // 自动找自增字段
            if (fi == null && FieldName.IsNullOrEmpty()) fi = Field = Factory.Table.Identity;

            base.Init();

            fi = Field;
            if (fi == null) throw new ArgumentNullException(nameof(FieldName), "未指定用于顺序抽取数据的自增字段！");

            OrderBy = fi.Asc();
        }
        #endregion

        #region 抽取数据
        /// <summary>抽取一批数据</summary>
        /// <param name="set">设置</param>
        /// <returns></returns>
        public virtual IList<IEntity> Fetch(IExtractSetting set)
        {
            if (Field == null) throw new ArgumentNullException(nameof(FieldName), "未指定用于顺序抽取数据的自增字段！");
            if (set == null) throw new ArgumentNullException(nameof(set), "没有设置数据抽取配置");

            var start = set.Row;

            var size = set.BatchSize;
            if (size <= 0) size = 1000;

            // 分批获取数据，如果没有取到，则结束
            var list = FetchData(start, size);
            // 取到数据，需要滑动窗口
            if (list.Count > 0)
            {
                var last = (Int32)list.Last()[FieldName];
                set.Row = last;
            }

            return list;
        }

        /// <summary>分段分页抽取数据</summary>
        /// <param name="start"></param>
        /// <param name="maxRows"></param>
        /// <returns></returns>
        protected virtual IList<IEntity> FetchData(Int32 start, Int32 maxRows)
        {
            var fi = Field;
            var exp = fi >= start;

            if (!Where.IsNullOrEmpty()) exp &= Where;

            return Factory.FindAll(exp, OrderBy, Selects, 0, maxRows);
        }
        #endregion
    }
}