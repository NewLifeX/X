using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Data;

namespace NewLife.Web
{
    /// <summary>分页器。包含分页排序参数，支持构造Url的功能</summary>
    public class Pager : PageParameter
    {
        #region 名称
        /// <summary>名称类。用户可根据需要修改Url参数名</summary>
        public class __
        {
            /// <summary>排序字段</summary>
            public String Sort = "Sort";

            /// <summary>是否降序</summary>
            public String Desc = "Desc";

            /// <summary>页面索引</summary>
            public String PageIndex = "PageIndex";

            /// <summary>页面大小</summary>
            public String PageSize = "PageSize";
        }

        /// <summary>名称类。用户可根据需要修改Url参数名</summary>
        public __ _ = new __();
        #endregion

        #region 扩展属性
        private IDictionary<String, String> _Params;
        /// <summary>参数集合</summary>
        public IDictionary<String, String> Params { get { return _Params ?? (_Params = WebHelper.Params); } set { _Params = value; } }

        private String _PageUrlTemplate = "<a href=\"{链接}\">{名称}</a>";
        /// <summary>分页链接模版。内部将会替换{链接}和{名称}</summary>
        public String PageUrlTemplate { get { return _PageUrlTemplate; } set { _PageUrlTemplate = value; } }

        private static PageParameter _def = new PageParameter();

        private PageParameter _Default = _def;
        /// <summary>默认参数。如果分页参数为默认参数，则不参与构造Url</summary>
        public PageParameter Default { get { return _Default; } set { _Default = value; } }

        /// <summary>获取/设置 参数</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public String this[String key]
        {
            get
            {
                if (key.EqualIgnoreCase(_.Sort))
                    return Sort;
                else if (key.EqualIgnoreCase(_.Desc))
                    return Desc + "";
                else if (key.EqualIgnoreCase(_.PageIndex))
                    return PageIndex + "";
                else if (key.EqualIgnoreCase(_.PageSize))
                    return PageSize + "";
                else
                    return Params[key];
            }
            set
            {
                if (key.EqualIgnoreCase(_.Sort))
                    Sort = value;
                else if (key.EqualIgnoreCase(_.Desc))
                    Desc = value.ToBoolean();
                else if (key.EqualIgnoreCase(_.PageIndex))
                    PageIndex = value.ToInt();
                else if (key.EqualIgnoreCase(_.PageSize))
                    PageSize = value.ToInt();
                else
                    Params[key] = value;
            }
        }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public Pager() { }

        /// <summary>用另一个分页参数实例化</summary>
        /// <param name="pm"></param>
        public Pager(PageParameter pm) : base(pm) { }

        //public Pager(ICollection collection) { SetParams(collection); }
        #endregion

        #region 方法
        //public virtual void SetParams(ICollection collection)
        //{
        //    foreach (var item in collection)
        //    {
        //        var key = item + "";
        //        var value = item.GetType().FullName + "";

        //        if (key.EqualIgnoreCase(_.Sort))
        //            Sort = value;
        //        else if (key.EqualIgnoreCase(_.Desc))
        //            Desc = value.ToBoolean();
        //        else if (key.EqualIgnoreCase(_.PageIndex))
        //            PageIndex = value.ToInt();
        //        else if (key.EqualIgnoreCase(_.PageSize))
        //            PageSize = value.ToInt();
        //        else
        //            Params.Add(key, value);
        //    }
        //}

        /// <summary>获取基础Url，用于附加参数</summary>
        /// <param name="where">查询条件，不包含排序和分页</param>
        /// <param name="order">排序</param>
        /// <param name="page">分页</param>
        /// <returns></returns>
        public virtual StringBuilder GetBaseUrl(Boolean where, Boolean order, Boolean page)
        {
            var sb = new StringBuilder();
            var dic = Params;
            // 先构造基本条件，再排序到分页
            if (where) sb.UrlParamsExcept(dic, _.Sort, _.Desc, _.PageIndex, _.PageSize);
            if (order) sb.UrlParams(dic, _.Sort, _.Desc);
            if (page) sb.UrlParams(dic, _.PageIndex, _.PageSize);

            return sb;
        }

        /// <summary>获取排序的Url</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual String GetSortUrl(String name)
        {
            // 首次访问该排序项，默认升序，重复访问取反
            var desc = false;
            if (Sort.EqualIgnoreCase(name)) desc = !Desc;

            var url = GetBaseUrl(true, false, true);
            // 默认排序不处理
            if (!name.EqualIgnoreCase(Default.Sort)) url.UrlParam(_.Sort, name);
            if (desc) url.UrlParam(_.Desc, true);
            return url.Length > 0 ? "?" + url.ToString() : "";
        }

        /// <summary>获取分页Url</summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual String GetPageUrl(String name, Int32 index)
        {
            var url = GetBaseUrl(true, true, false);
            // 当前在非首页而要跳回首页，不写页面序号
            //if (!(PageIndex > 1 && index == 1)) url.UrlParam(_.PageIndex, index);
            // 还是写一下页面序号，因为页面Url本身就有，如果这里不写，有可能首页的href为空
            if (PageIndex != index) url.UrlParam(_.PageIndex, index);
            if (PageSize != Default.PageSize) url.UrlParam(_.PageSize, PageSize);

            var url2 = url.Length > 0 ? "?" + url.ToString() : "";

            var txt = PageUrlTemplate;
            txt = txt.Replace("{链接}", url2);
            txt = txt.Replace("{名称}", name);

            return txt;
        }

        /// <summary>获取分页Url</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual String GetPage(String name)
        {
            if (PageIndex == 1)
            {
                if (name == "首页" || name == "上一页") return name;
            }
            if (PageIndex >= PageCount)
            {
                if (name == "尾页" || name == "下一页") return name;
            }

            if (PageIndex > 1)
            {
                if (name == "首页") return GetPageUrl("首页", 1);
                if (name == "上一页") return GetPageUrl("上一页", PageIndex - 1);
            }
            if (PageIndex < PageCount)
            {
                if (name == "尾页") return GetPageUrl("尾页", PageCount);
                if (name == "下一页") return GetPageUrl("下一页", PageIndex + 1);
            }

            return name;
        }
        #endregion
    }
}