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
        /// <summary>获取 或 设置 排序字段，前台接收，便于做安全性校验</summary>
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

        /// <summary>获取 或 设置 页面索引。从1开始，默认1</summary>
        /// <remarks>如果设定了开始行，分页时将不再使用PageIndex</remarks>
        public virtual Int32 PageIndex { get; set; } = 1;

        /// <summary>获取 或 设置 页面大小。默认20，若为0表示不分页</summary>
        public virtual Int32 PageSize { get; set; } = 20;
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

        private String _OrderBy;
        /// <summary>获取 或 设置 组合起来的排序字句。如果没有设置则取Sort+Desc，后台设置，不经过安全性校验</summary>
        public virtual String OrderBy
        {
            get
            {
                if (!_OrderBy.IsNullOrEmpty()) return _OrderBy;

                var str = Sort;
                if (str.IsNullOrWhiteSpace()) return null;
                if (Desc) str += " Desc";

                return str;
            }
            set { _OrderBy = value; Sort = value; }
        }

        /// <summary>获取 或 设置 开始行</summary>
        /// <remarks>如果设定了开始行，分页时将不再使用PageIndex</remarks>
        [XmlIgnore, ScriptIgnore]
        public virtual Int64 StartRow { get; set; } = -1;

        /// <summary>获取 或 设置 是否获取总记录数，默认false</summary>
        [XmlIgnore, ScriptIgnore]
        public Boolean RetrieveTotalCount { get; set; }

        /// <summary>获取 或 设置 状态。用于传递统计等数据</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual Object State { get; set; }

        /// <summary>获取 或 设置 是否获取统计，默认false</summary>
        [XmlIgnore, ScriptIgnore]
        public Boolean RetrieveState { get; set; }
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

            _OrderBy = pm._OrderBy;
            Sort = pm.Sort;
            Desc = pm.Desc;
            PageIndex = pm.PageIndex;
            PageSize = pm.PageSize;
            StartRow = pm.StartRow;

            TotalCount = pm.TotalCount;
            RetrieveTotalCount = pm.RetrieveTotalCount;
            State = pm.State;
            RetrieveState = pm.RetrieveState;

            return this;
        }

        /// <summary>获取表示分页参数唯一性的键值，可用作缓存键</summary>
        /// <returns></returns>
        public virtual String GetKey() => "{0}-{1}-{2}".F(PageIndex, PageCount, OrderBy);
        #endregion
    }
}