using System;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using XCode;
using NewLife.Reflection;

namespace NewLife.Cube
{
    /// <summary>Html扩展</summary>
    public static class HtmlExtensions
    {
        /// <summary>输出编辑框</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForEditor(this HtmlHelper Html, String name, Object value, Type type = null, String format = null, Object htmlAttributes = null)
        {
            if (type == null && value != null) type = value.GetType();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return Html.ForBoolean(name, value.ToBoolean());
                case TypeCode.DateTime:
                    return Html.ForDateTime(name, value.ToDateTime());
                case TypeCode.Decimal:
                    return Html.ForDecimal(name, Convert.ToDecimal(value));
                case TypeCode.Single:
                case TypeCode.Double:
                    return Html.ForDouble(name, value.ToDouble());
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return Html.ForInt(name, Convert.ToInt64(value));
                case TypeCode.String:
                    return Html.ForString(name, value + "");
                default:
#if DEBUG
                    throw new Exception("不支持的类型" + type);
#else
                    return Html.Editor(name);
#endif
            }
        }

        /// <summary>输出编辑框</summary>
        /// <param name="Html"></param>
        /// <param name="expression"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForEditor<TModel, TProperty>(this HtmlHelper<TModel> Html, Expression<Func<TModel, TProperty>> expression, Object htmlAttributes = null)
        {
            var meta = ModelMetadata.FromLambdaExpression(expression, Html.ViewData);
            var name = meta.PropertyName;
            var pi = typeof(TModel).GetProperty(name);

            return Html.ForEditor(name, Html.ViewData.Model.GetValue(pi), pi.PropertyType, null, htmlAttributes);
        }

        #region 基础属性
        /// <summary>输出字符串</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForString(this HtmlHelper Html, String name, String value, Int32 length = 0, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control");

            // 首先输出图标
            var ico = "";

            MvcHtmlString txt = null;
            if (name.EqualIgnoreCase("Pass", "Password"))
            {
                txt = Html.Password(name, (String)value, atts);
            }
            else if (name.EqualIgnoreCase("Phone"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-phone-alt\"></i></span>";
                if (!atts.ContainsKey("@type")) atts.Add("@type", "phone");
                txt = Html.TextBox(name, (String)value, atts);
            }
            else if (name.EqualIgnoreCase("email", "mail"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-envelope\"></i></span>";
                if (!atts.ContainsKey("@type")) atts.Add("@type", "email");
                txt = Html.TextBox(name, (String)value, atts);
            }
            else if (name.EndsWithIgnoreCase("url"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-home\"></i></span>";
                if (!atts.ContainsKey("@type")) atts.Add("@type", "url");
                txt = Html.TextBox(name, (String)value, atts);
            }
            else if (length < 0 || length > 300)
            {
                txt = Html.TextArea(name, (String)value, atts);
            }
            else
            {
                txt = Html.TextBox(name, (String)value, atts);
            }

            return new MvcHtmlString(ico.ToString() + txt);
        }

        /// <summary>输出整数</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForInt(this HtmlHelper Html, String name, Int64 value, String format = null, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control");
            if (!atts.ContainsKey("role")) atts.Add("role", "number");

            return Html.TextBox(name, value, format, atts);
        }

        /// <summary>时间日期输出</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDateTime(this HtmlHelper Html, String name, DateTime value, String format = null, Object htmlAttributes = null)
        {
            //var fullHtmlFieldName = Html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            //if (String.IsNullOrEmpty(fullHtmlFieldName))
            //    throw new ArgumentException("", "name");

            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("type")) atts.Add("type", "date");
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control date form_datetime");

            var obj = value.ToFullString();
            if (value <= DateTime.MinValue) obj = null;
            //if (format.IsNullOrWhiteSpace()) format = "yyyy-MM-dd HH:mm:ss";

            // 首先输出图标
            var ico = Html.Raw("<span class=\"input-group-addon\"><i class=\"fa fa-calendar\"></i></span>");

            var txt = Html.TextBox(name, obj, format, atts);
            //var txt = BuildInput(InputType.Text, name, obj, atts);

            return new MvcHtmlString(ico.ToString() + txt);
        }

        /// <summary>时间日期输出</summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="Html"></param>
        /// <param name="expression"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDateTime<TModel, TProperty>(this HtmlHelper<TModel> Html, Expression<Func<TModel, TProperty>> expression, String format = null, Object htmlAttributes = null)
        {
            var meta = ModelMetadata.FromLambdaExpression(expression, Html.ViewData);
            var entity = Html.ViewData.Model as IEntity;
            var value = (DateTime)entity[meta.PropertyName];

            return Html.ForDateTime(meta.PropertyName, value, format, htmlAttributes);
        }

        /// <summary>输出布尔型</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForBoolean(this HtmlHelper Html, String name, Boolean value, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control");

            return Html.CheckBox(name, value, atts);
        }

        /// <summary>输出货币类型</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDecimal(this HtmlHelper Html, String name, Decimal value, String format = null, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control");

            // 首先输出图标
            var ico = Html.Raw("<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-yen\"></i></span>");
            var txt = Html.TextBox(name, value, format, atts);

            return new MvcHtmlString(ico.ToString() + txt);
        }

        /// <summary>输出浮点数</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDouble(this HtmlHelper Html, String name, Double value, String format = null, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control");

            // 首先输出图标
            var ico = Html.Raw("<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-yen\"></i></span>");
            var txt = Html.TextBox(name, value, format, atts);

            return new MvcHtmlString(ico.ToString() + txt);
        }
        #endregion

        #region 专有属性
        #endregion
    }
}