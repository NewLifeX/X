using System;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Data
{
    /// <summary>分页参数信息</summary>
    public class PageParameter
    {
        #region 核心属性
        private String _Sort;
        /// <summary>获取 或 设置 排序字段</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual String Sort
        {
            get { return _Sort; }
            set
            {
                _Sort = value;

                // 自动识别带有Asc/Desc的排序
                if (!_Sort.IsNullOrEmpty())
                {
                    _Sort = _Sort.Trim();
                    var p = _Sort.LastIndexOf(" ");
                    if (p > 0)
                    {
                        var dir = _Sort.Substring(p + 1);
                        if (dir.EqualIgnoreCase("asc"))
                        {
                            Desc = false;
                            _Sort = _Sort.Substring(0, p).Trim();
                        }
                        else if (dir.EqualIgnoreCase("desc"))
                        {
                            Desc = true;
                            _Sort = _Sort.Substring(0, p).Trim();
                        }
                    }
                }
            }
        }

        /// <summary>获取 或 设置 是否降序</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual Boolean Desc { get; set; }

        private Int32 _PageIndex = 1;
        /// <summary>获取 或 设置 页面索引</summary>
        /// <remarks>如果设定了开始行，分页时将不再使用PageIndex</remarks>
        public virtual Int32 PageIndex { get { return _PageIndex; } set { _PageIndex = value > 1 ? value : 1; } }

        private Int32 _PageSize = 20;
        /// <summary>获取 或 设置 页面大小</summary>
        public virtual Int32 PageSize { get { return _PageSize; } set { _PageSize = value > 1 ? value : 20; } }
        #endregion

        #region 扩展属性
        /// <summary>获取 或 设置 总记录数</summary>
        public virtual Int64 TotalCount { get; set; }

        /// <summary>获取 页数</summary>
        public virtual Int64 PageCount
        {
            get
            {
                var count = TotalCount / PageSize;
                if ((TotalCount % PageSize) != 0) count++;
                return count;
            }
        }

        /// <summary>获取 组合起来的排序字句</summary>
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

        /// <summary>获取 或 设置 开始行</summary>
        /// <remarks>如果设定了开始行，分页时将不再使用PageIndex</remarks>
        [XmlIgnore, ScriptIgnore]
        public virtual Int64 StartRow { get; set; } = -1;

        /// <summary>获取 或 设置 是否获取总记录数</summary>
        [XmlIgnore, ScriptIgnore]
        public Boolean RetrieveTotalCount { get; set; }
        #endregion

        #region 构造函数
        /// <summary>实例化分页参数</summary>
        public PageParameter() { }

        /// <summary>通过另一个分页参数来实例化当前分页参数</summary>
        /// <param name="pm"></param>
        public PageParameter(PageParameter pm) { CopyFrom(pm); }
        #endregion

        #region 方法
        /// <summary>从另一个分页参数拷贝到当前分页参数</summary>
        /// <param name="pm"></param>
        /// <returns></returns>
        public virtual PageParameter CopyFrom(PageParameter pm)
        {
            if (pm == null) return this;

            Sort = pm.Sort;
            Desc = pm.Desc;
            PageIndex = pm.PageIndex;
            PageSize = pm.PageSize;
            StartRow = pm.StartRow;

            TotalCount = pm.TotalCount;

            return this;
        }

        /// <summary>获取表示分页参数唯一性的键值，可用作缓存键</summary>
        /// <returns></returns>
        public virtual String GetKey()
        {
            return "{0}-{1}-{2}".F(PageIndex, PageCount, OrderBy);
        }
        #endregion
    }
}