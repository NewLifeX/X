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
        private IEnumerable _DataSource;
        /// <summary>数据源</summary>
        public IEnumerable DataSource { get { return _DataSource; } set { _DataSource = value; } }

        public Func<String, String, Int32, Int32, IEnumerable> DataMethod;

        private List<String> _Titles;
        /// <summary>标题集合</summary>
        public List<String> Titles { get { return _Titles; } set { _Titles = value; } }

        private List<String> _Names;
        /// <summary>名称集合</summary>
        public List<String> Names { get { return _Names; } set { _Names = value; } }
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
                    sb.AppendFormat("<th><a href=\"{0}\">{1}</a></th> ", GetSort(ss[0]), ss[1]);
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

        #endregion

        #region 排序
        public virtual String GetSort(String name)
        {
            // 首次访问该排序项，默认升序，重复访问取反
            var sort = WebHelper.Request["Sort"];
            var desc = WebHelper.Request["Desc"].ToInt() == 0;
            if (sort.EqualIgnoreCase(name)) desc = !desc;

            if (desc)
                return "?Sort={0}".F(name);
            else
                return "?Sort={0}&Desc=1".F(name);
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