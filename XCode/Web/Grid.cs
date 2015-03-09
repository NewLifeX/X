using System;
using System.Collections;
using System.Collections.Generic;
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
        /// <summary>数据源</summary>
        public IEntityList DataSource { get { if (_DataSource == null)PerformSelect(); return _DataSource; } set { _DataSource = value; } }

        private List<String> _Titles;
        /// <summary>标题集合</summary>
        public List<String> Titles { get { return _Titles; } set { _Titles = value; } }

        private List<String> _Names;
        /// <summary>名称集合</summary>
        public List<String> Names { get { return _Names; } set { _Names = value; } }
        #endregion

        #region 构造
        /// <summary>无参数构造</summary>
        public Grid() { }

        /// <summary>使用实体工厂构造</summary>
        /// <param name="factory"></param>
        public Grid(IEntityOperate factory) { Factory = factory; }
        #endregion

        #region 方法
        //public virtual String Render()
        //{
        //    var sb = new HtmlWriter();
        //    sb.Append("<table class=\"table table-bordered table-hover table-striped table-condensed\">");
        //    sb.Append("</table>");

        //    return sb.ToString();
        //}

        public virtual String RenderHeader(Boolean includeHeader, params String[] names)
        {
            var sb = new StringBuilder();
            if (includeHeader) sb.Append("<thead><tr>");
            foreach (var item in names)
            {
                var ss = item.Split(":");
                if (ss.Length == 2)
                    sb.AppendFormat("<th><a href=\"{0}\">{1}</a></th> ", GetSortUrl(ss[0]), ss[1]);
                else
                    sb.AppendFormat("<th>{0}</th>", item);
            }
            if (includeHeader) sb.Append("</tr></thead>");
            return sb.ToString();

            //ww.Indent += 4;
            //ww.AppendLine("<thead>");

            //ww.Indent += 4;
            //ww.AppendLine("<tr>");

            //ww.Indent += 4;
            //for (int i = 0; i < Titles.Count; i++)
            //{
            //    ww.AppendLine("<th>{0}</th>", Titles[0]);
            //}
            //ww.Indent -= 4;

            //ww.AppendLine("</tr>");
            //ww.Indent -= 4;

            //ww.AppendLine("</thead>");
            //ww.Indent -= 4;
        }

        //protected virtual void RenderBody(HtmlWriter ww)
        //{
        //    ww.Indent += 4;

        //    ww.Indent -= 4;
        //}

        /// <summary>获取基础Url，用于附加参数</summary>
        /// <param name="where"></param>
        /// <param name="order"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        protected virtual String GetBaseUrl(Boolean where, Boolean order, Boolean page)
        {
            var sb = new StringBuilder();
            sb.Append("?");
            var nvs = WebHelper.Request.QueryString;
            foreach (var item in nvs.AllKeys)
            {
                // 过滤
                if (item.EqualIgnoreCase("Sort", "Desc"))
                {
                    if (!order) continue;
                }
                else if (item.EqualIgnoreCase("PageIndex", "PageSize"))
                {
                    if (!page) continue;
                }
                else
                {
                    if (!where) continue;
                }

                if (sb.Length > 1) sb.Append("&");
                sb.AppendFormat("{0}={1}", item, nvs[item]);
            }
            return sb.ToString();
        }
        #endregion

        #region 排序
        /// <summary>获取排序的Url</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual String GetSortUrl(String name)
        {
            // 首次访问该排序项，默认升序，重复访问取反
            var sort = WebHelper.Request["Sort"];
            var desc = WebHelper.Request["Desc"].ToInt() == 0;
            if (sort.EqualIgnoreCase(name)) desc = !desc;

            var url = GetBaseUrl(true, false, true);
            if (url.Length > 1) url += "&";
            if (desc)
                return url + "Sort={0}".F(name);
            else
                return url + "Sort={0}&Desc=1".F(name);
        }

        /// <summary>排序字句</summary>
        public virtual String OrderBy
        {
            get
            {
                var sort = WebHelper.Request["Sort"];
                if (sort.IsNullOrWhiteSpace()) return null;

                var desc = WebHelper.Request["Desc"].ToInt() == 0;
                if (desc) sort += " Desc";
                return sort;
            }
        }
        #endregion

        #region 分页
        private Int32 _PageSize = 20;
        /// <summary>页大小</summary>
        public Int32 PageSize { get { return _PageSize; } set { _PageSize = value; } }

        private Int32 _PageIndex;
        /// <summary>页索引</summary>
        public Int32 PageIndex
        {
            get
            {
                if (_PageIndex <= 0) _PageIndex = WebHelper.RequestInt("PageIndex");
                if (_PageIndex < 1) _PageIndex = 1;
                return _PageIndex;
            }
            set { _PageIndex = value; }
        }

        private Int32 _TotalCount = -1;
        /// <summary>总记录数</summary>
        public Int32 TotalCount
        {
            get
            {
                if (_TotalCount < 0) _TotalCount = Factory.FindCount(SearchWhere(), null, null, 0, 0);
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

        private String _PageTemplate = "共<span>{TotalCount}</span>条&nbsp;每页<span>{PageSize}</span>条&nbsp;当前第<span>{PageIndex}</span>页/共<span>{PageCount}</span>页&nbsp;{首页}{上一页}{下一页}{尾页}转到第<input name=\"PageIndex\" type=\"text\" value=\"{PageIndex}\" style=\"width:40px;text-align:right;\" />页<input type=\"button\" name=\"PageJump\" value=\"GO\" />";
        /// <summary>分页模版</summary>
        public String PageTemplate { get { return _PageTemplate; } set { _PageTemplate = value; } }

        private String _PageUrlTemplate = "<a href=\"{链接}\">{名称}</a>&nbsp;";
        /// <summary>分页链接模版</summary>
        public String PageUrlTemplate { get { return _PageUrlTemplate; } set { _PageUrlTemplate = value; } }

        public virtual String RenderPage()
        {
            var txt = PageTemplate;
            txt = txt.Replace("{TotalCount}", TotalCount + "");
            txt = txt.Replace("{PageIndex}", PageIndex + "");
            txt = txt.Replace("{PageSize}", PageSize + "");
            txt = txt.Replace("{PageCount}", PageCount + "");

            if (PageIndex == 1)
            {
                txt = txt.Replace("{首页}", null);
                txt = txt.Replace("{上一页}", null);
            }
            if (PageIndex >= PageCount)
            {
                txt = txt.Replace("{尾页}", null);
                txt = txt.Replace("{下一页}", null);
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

        String GetPageUrl(String name, Int32 index)
        {
            var url = GetBaseUrl(true, true, false);
            if (url.Length > 1) url += "&";
            if (PageIndex != index && index > 1) url += "PageIndex=" + index;

            var txt = PageUrlTemplate;
            txt = txt.Replace("{链接}", url);
            txt = txt.Replace("{名称}", name);

            return txt;
        }
        #endregion

        #region 查询
        ///// <summary>查询数据的方法</summary>
        //public Func<String, String, String, Int32, Int32, IEntityList> SelectMethod;

        //private Int32 _SelectCountMethod;
        ///// <summary>查询记录数的方法</summary>
        //public Int32 SelectCountMethod { get { return _SelectCountMethod; } set { _SelectCountMethod = value; } }

        private IEntityOperate _Factory;
        /// <summary>实体工厂</summary>
        public IEntityOperate Factory { get { return _Factory; } set { _Factory = value; } }

        public virtual String SearchWhere()
        {
            return null;
        }

        protected virtual void PerformSelect()
        {
            DataSource = Factory.FindAll(SearchWhere(), OrderBy, null, (PageIndex - 1) * PageSize, PageSize);
        }
        #endregion
    }

    ///// <summary>网页写入器</summary>
    //public class HtmlWriter
    //{
    //    /// <summary>内部写入器</summary>
    //    public StringBuilder Builder = new StringBuilder();

    //    private Int32 _Indent;
    //    /// <summary>缩进空格数</summary>
    //    public Int32 Indent { get { return _Indent; } set { _Indent = value; } }

    //    public HtmlWriter Append(String format, params Object[] args)
    //    {
    //        if (Indent > 0) Builder.Append(new String(' ', Indent));
    //        Builder.Append(format.F(args));

    //        return this;
    //    }

    //    public HtmlWriter AppendLine(String format, params Object[] args)
    //    {
    //        if (Indent > 0) Builder.Append(new String(' ', Indent));
    //        Builder.AppendLine(format.F(args));

    //        return this;
    //    }

    //    public override string ToString()
    //    {
    //        return Builder.ToString();
    //    }
    //}
}