using System;
using NewLife.Data;

namespace XCode.DataAccessLayer
{
    /// <summary>数据页事件参数。备份、还原、同步</summary>
    public class PageEventArgs : EventArgs
    {
        /// <summary>数据表</summary>
        public IDataTable Table { get; set; }

        /// <summary>开始行。分页时表示偏移行数，自增时表示下一个编号，默认0</summary>
        public Int64 Row { get; set; }

        /// <summary>数据页</summary>
        public DbTable Page { get; set; }
    }
}