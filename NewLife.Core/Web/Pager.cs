using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;
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
        [XmlIgnore, ScriptIgnore]
        public __ _ = new __();
        #endregion

        #region 扩展属性
#if !__CORE__
        private IDictionary<String, String> _Params;
        /// <summary>参数集合</summary>
        [XmlIgnore, ScriptIgnore]
        public IDictionary<String, String> Params { get { return _Params ?? (_Params = WebHelper.Params); } set { _Params = value; } }
#else
        /// <summary>参数集合</summary>
        public IDictionary<String, String> Params { get; set; } = new NewLife.Collections.NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
#endif

        /// <summary>分页链接模版。内部将会替换{链接}和{名称}</summary>
        [XmlIgnore, ScriptIgnore]
        public String PageUrlTemplate { get; set; } = "<a href=\"{链接}\">{名称}</a>";

        private static PageParameter _def = new PageParameter();

        /// <summary>默认参数。如果分页参数为默认参数，则不参与构造Url</summary>
        [XmlIgnore, ScriptIgnore]
        public PageParameter Default { get; set; } = _def;

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
        public Pager(PageParameter pm) : base(pm)
        {
            if (pm is Pager p)
            {
                foreach (var item in p.Params)
                {
                    this[item.Key] = item.Value;
                }
            }
        }
        #endregion

        #region 方法
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
            // 首次访问该排序项，默认降序，重复访问取反
            var desc = true;
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
        public virtual String GetPageUrl(String name, Int64 index)
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

#if !__CORE__
        /// <summary>获取表单提交的Url</summary>
        /// <param name="action">动作</param>
        /// <returns></returns>
        public virtual String GetFormAction(String action = null)
        {
            var req = HttpContext.Current?.Request;
            if (req == null) return action;

            // 表单提交，不需要排序、分页，不需要表单提交上来的数据，只要请求字符串过来的数据
            var query = req.QueryString;
            var forms = new HashSet<String>(req.Form.AllKeys, StringComparer.OrdinalIgnoreCase);
            var excludes = new HashSet<String>(new[] { _.Sort, _.Desc, _.PageIndex, _.PageSize }, StringComparer.OrdinalIgnoreCase);

            var url = Pool.StringBuilder.Get();
            foreach (var item in query.AllKeys)
            {
                // 只要查询字符串，不要表单
                if (forms.Contains(item)) continue;

                // 排除掉排序和分页
                if (excludes.Contains(item)) continue;

                // 内容为空也不要
                var v = query[item];
                if (v.IsNullOrEmpty()) continue;

                url.UrlParam(item, v);
            }

            if (url.Length == 0) return action;
            if (!action.Contains('?')) action += '?';

            return action + url.Put(true);
        }
#endif
        #endregion
    }
}