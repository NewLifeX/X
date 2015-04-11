using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Web
{
    /// <summary>分页参数信息</summary>
    public class PageParameter
    {
        #region 核心属性
        private String _Sort;
        /// <summary>排序字段</summary>
        public virtual String Sort { get { return _Sort; } set { _Sort = value; } }

        private Boolean _Desc;
        /// <summary>是否降序</summary>
        public virtual Boolean Desc { get { return _Desc; } set { _Desc = value; } }

        private Int32 _PageIndex = 1;
        /// <summary>页面索引</summary>
        public virtual Int32 PageIndex { get { return _PageIndex; } set { _PageIndex = value > 1 ? value : 1; } }

        private Int32 _PageSize = 20;
        /// <summary>页面大小</summary>
        public virtual Int32 PageSize { get { return _PageSize; } set { _PageSize = value > 1 ? value : 20; } }
        #endregion

        #region 扩展属性

        private Int32 _TotalCount;
        /// <summary>总记录数</summary>
        public virtual Int32 TotalCount { get { return _TotalCount; } set { _TotalCount = value; } }

        /// <summary>页数</summary>
        public virtual Int32 PageCount
        {
            get
            {
                var count = TotalCount / PageSize;
                if ((TotalCount % PageSize) != 0) count++;
                return count;
            }
        }

        /// <summary>排序字句</summary>
        public virtual String OrderBy
        {
            get
            {
                var sort = Sort;
                if (sort.IsNullOrWhiteSpace()) return null;
                if (Desc) sort += " Desc";

                return sort;
            }
        }
        #endregion

        #region 构造克隆
        /// <summary>实例化分页参数</summary>
        public PageParameter() { }

        /// <summary>通过另一个分页参数来实例化当前分页参数</summary>
        /// <param name="pm"></param>
        public PageParameter(PageParameter pm) { CopyFrom(pm); }

        /// <summary>从另一个分页参数拷贝到当前分页参数</summary>
        /// <param name="pm"></param>
        /// <returns></returns>
        public virtual PageParameter CopyFrom(PageParameter pm)
        {
            Sort = pm.Sort;
            Desc = pm.Desc;
            PageIndex = pm.PageIndex;
            PageSize = pm.PageSize;

            TotalCount = pm.TotalCount;

            return this;
        }
        #endregion
    }
}