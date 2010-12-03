using System;
using System.Collections.Generic;
using System.Text;

namespace XControl
{
    /// <summary>
    /// 分页数据源接口
    /// </summary>
    public interface IPagedDataSource
    {
        /// <summary>
        /// 总记录数
        /// </summary>
        Int32 TotalRowCount { get; set; }

        /// <summary>
        /// 每页大小
        /// </summary>
        Int32 PageSize { get; set; }

        /// <summary>
        /// 当前页。0开始
        /// </summary>
        Int32 PageIndex { get; set; }

        /// <summary>
        /// 页数
        /// </summary>
        Int32 PageCount { get; }

        /// <summary>
        /// 是否第一页
        /// </summary>
        Boolean IsFirstPage { get; }

        /// <summary>
        /// 是否最后一页
        /// </summary>
        Boolean IsLastPage { get; }
    }
}
