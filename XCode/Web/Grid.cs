using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Reflection;
using NewLife.Web;

namespace XCode.Web
{
    /// <summary>用于显示数据的网格</summary>
    public class Grid
    {
        #region 属性
        private IEntityList _DataSource;
        /// <summary>数据源。如果为空，将会自动使用<see cref="Where"/>查询</summary>
        public IEntityList DataSource
        {
            get
            {
                if (_DataSource == null) Select();
                return _DataSource;
            }
            set { _DataSource = value; }
        }

        private IEntityOperate _Factory;
        /// <summary>实体工厂</summary>
        public IEntityOperate Factory { get { return _Factory; } set { _Factory = value; } }

        private String _Name;
        /// <summary>参数前缀。一个页面有多个Grid时很必要</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private ICollection<String> _Prefixs = new List<String>(new String[] { "txt", "ddl", "dt", "btn" });
        /// <summary>Http参数前缀集合，默认txt/ddl/dt/btn</summary>
        public ICollection<String> Prefixs { get { return _Prefixs; } set { _Prefixs = value; } }
        #endregion

        #region 构造
        /// <summary>无参数构造</summary>
        public Grid() { Init(); }

        /// <summary>使用实体工厂构造</summary>
        /// <param name="factory"></param>
        public Grid(IEntityOperate factory) { Factory = factory; Init(); }

        /// <summary>获取参数</summary>
        public void Init()
        {
            PageIndex = WebHelper.Params["PageIndex"].ToInt();
            PageSize = WebHelper.Params["PageSize"].ToInt();
            Sort = WebHelper.Params["Sort"];
            SortDesc = WebHelper.Params["Desc"].ToInt() != 0;

            if (Factory != null && Factory.Unique != null) DefaultSort = Factory.Unique.Name;
        }
        #endregion

        #region 方法
        /// <summary>获取基础Url，用于附加参数</summary>
        /// <param name="where"></param>
        /// <param name="order"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public virtual StringBuilder GetBaseUrl(Boolean where, Boolean order, Boolean page)
        {
            var sb = new StringBuilder();
            var dic = WebHelper.Params;
            // 先构造基本条件，再排序到分页
            if (where)
            {
                foreach (var item in dic)
                {
                    // 过滤
                    if (!item.Key.EqualIgnoreCase("Sort", "Desc", "PageIndex", "PageSize"))
                        sb.UrlParam(item.Key, item.Value);
                }
            }
            if (order)
            {
                foreach (var item in dic)
                {
                    if (item.Key.EqualIgnoreCase("Sort", "Desc"))
                        sb.UrlParam(item.Key, item.Value);
                }
            }
            if (page)
            {
                foreach (var item in dic)
                {
                    if (item.Key.EqualIgnoreCase("PageIndex", "PageSize"))
                        sb.UrlParam(item.Key, item.Value);
                }
            }
            return sb;
        }
        #endregion

        #region 排序
        private String _DefaultSort;
        /// <summary>默认排序字段</summary>
        public String DefaultSort { get { return _DefaultSort; } set { _DefaultSort = value; } }

        private String _Sort;
        /// <summary>排序字段</summary>
        public String Sort { get { return _Sort ?? DefaultSort; } set { _Sort = value; } }

        private Boolean _SortDesc;
        /// <summary>是否降序</summary>
        public Boolean SortDesc { get { return _SortDesc; } set { _SortDesc = value; } }

        /// <summary>获取排序的Url</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual String GetSortUrl(String name)
        {
            // 首次访问该排序项，默认升序，重复访问取反
            var desc = false;
            if (Sort.EqualIgnoreCase(name)) desc = !SortDesc;

            var url = GetBaseUrl(true, false, true);
            // 默认排序不处理
            if (!name.EqualIgnoreCase(DefaultSort)) url.UrlParam("Sort", name);
            if (desc) url.UrlParam("Desc", 1);
            return url.ToString();
        }

        /// <summary>排序字句</summary>
        public virtual String OrderBy
        {
            get
            {
                var sort = Sort;
                if (sort.IsNullOrWhiteSpace()) return null;
                if (SortDesc) sort += " Desc";
                return sort;
            }
        }
        #endregion

        #region 分页
        private Int32 _DefaultPageSize = 20;
        /// <summary>默认页大小</summary>
        public Int32 DefaultPageSize { get { return _DefaultPageSize; } set { if (value <= 0)value = 20; _DefaultPageSize = value; } }

        private Int32 _PageSize = 0;
        /// <summary>页大小。设置有值时采用已有值，否则采用默认也大小</summary>
        public Int32 PageSize { get { return _PageSize > 0 ? _PageSize : DefaultPageSize; } set { _PageSize = value; } }

        private Int32 _PageIndex = 1;
        /// <summary>页索引</summary>
        public Int32 PageIndex { get { return _PageIndex; } set { if (value <= 0)value = 1; _PageIndex = value; } }

        private Int32 _TotalCount = -1;
        /// <summary>总记录数</summary>
        public Int32 TotalCount
        {
            get
            {
                if (_TotalCount < 0) _TotalCount = Factory.FindCount(Where, null, null, 0, 0);
                return _TotalCount;
            }
            set { _TotalCount = value; }
        }

        /// <summary>页数</summary>
        public Int32 PageCount
        {
            get
            {
                var count = TotalCount / PageSize;
                if ((TotalCount % PageSize) != 0) count++;
                return count;
            }
        }

        private String _PageTemplate = "共<span>{TotalCount}</span>条&nbsp;每页<span>{PageSize}</span>条&nbsp;当前第<span>{PageIndex}</span>页/共<span>{PageCount}</span>页&nbsp;{首页}{上一页}{下一页}{尾页}转到第<input name=\"PageIndex\" type=\"text\" value=\"{PageIndex}\" style=\"width:40px;text-align:right;\" />页<input type=\"submit\" name=\"PageJump\" value=\"GO\" />";
        /// <summary>分页模版</summary>
        public String PageTemplate { get { return _PageTemplate; } set { _PageTemplate = value; } }

        private String _PageUrlTemplate = "<a href=\"{链接}\">{名称}</a>&nbsp;";
        /// <summary>分页链接模版</summary>
        public String PageUrlTemplate { get { return _PageUrlTemplate; } set { _PageUrlTemplate = value; } }

        /// <summary>生成分页输出</summary>
        /// <returns></returns>
        public virtual String RenderPage()
        {
            var txt = PageTemplate;
            txt = txt.Replace("{TotalCount}", TotalCount + "");
            txt = txt.Replace("{PageIndex}", PageIndex + "");
            txt = txt.Replace("{PageSize}", PageSize + "");
            txt = txt.Replace("{PageCount}", PageCount + "");

            if (PageIndex == 1)
            {
                txt = txt.Replace("{首页}", "首页&nbsp;");
                txt = txt.Replace("{上一页}", "上一页&nbsp;");
            }
            if (PageIndex >= PageCount)
            {
                txt = txt.Replace("{尾页}", "尾页&nbsp;");
                txt = txt.Replace("{下一页}", "下一页&nbsp;");
            }

            if (PageIndex > 1)
            {
                txt = txt.Replace("{首页}", GetPageUrl("首页", 1));
                txt = txt.Replace("{上一页}", GetPageUrl("上一页", PageIndex - 1));
            }
            if (PageIndex < PageCount)
            {
                txt = txt.Replace("{尾页}", GetPageUrl("尾页", PageCount));
                txt = txt.Replace("{下一页}", GetPageUrl("下一页", PageIndex + 1));
            }

            return txt;
        }

        /// <summary>获取分页Url</summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public String GetPageUrl(String name, Int32 index)
        {
            var url = GetBaseUrl(true, true, false);
            // 当前在非首页而要跳回首页，不写页面序号
            //if (!(PageIndex > 1 && index == 1)) url.UrlParam("PageIndex", index);
            // 还是写一下页面序号，因为页面Url本身就有，如果这里不写，有可能首页的href为空
            if (PageIndex != index) url.UrlParam("PageIndex", index);
            if (PageSize != DefaultPageSize) url.UrlParam("PageSize", PageSize);

            var txt = PageUrlTemplate;
            txt = txt.Replace("{链接}", url.ToString());
            txt = txt.Replace("{名称}", name);

            return txt;
        }
        #endregion

        #region 查询
        private String _Where;
        /// <summary>查询条件。由外部根据<see cref="WebHelper.Params"/>构造后赋值，<see cref="Select"/>将会调用该条件查询数据</summary>
        public String Where { get { return _Where; } set { _Where = value; } }

        private String _WhereMethod;
        /// <summary>查询Where条件的方法名</summary>
        public String WhereMethod { get { return _WhereMethod; } set { _WhereMethod = value; } }

        /// <summary>执行数据查询</summary>
        public virtual void Select()
        {
            // 如果指定了查询Where条件的方法，则根据请求参数反射调用
            if (!WhereMethod.IsNullOrWhiteSpace())
            {
                var method = Factory.EntityType.GetMethodEx(WhereMethod);
                // 过滤前缀
                var ps = WebHelper.Params.ToDictionary(e => e.Key.TrimStart(Prefixs.ToArray()), e => e.Value, StringComparer.OrdinalIgnoreCase);
                if (method != null) Where = Reflect.InvokeWithParams(null, method, ps as IDictionary) + "";
            }

            DataSource = Factory.FindAll(Where, OrderBy, null, (PageIndex - 1) * PageSize, PageSize);
            TotalCount = Factory.FindCount(Where, null, null, 0, 0);
        }
        #endregion
    }
}