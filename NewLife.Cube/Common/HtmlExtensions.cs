using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using NewLife.Reflection;
using NewLife.Web;
using XCode;
using XCode.Configuration;

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
            if (type == null) new XException("设计错误！ForEditor({0}, null, null)不能值和类型同时为空，否则会导致死循环", name);

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
                    if (type.IsEnum)
                        return Html.ForEnum(name, value, format);
                    else
                        return Html.ForInt(name, Convert.ToInt64(value));
                case TypeCode.String:
                    return Html.ForString(name, value + "");
                default:
                    return Html.ForObject(name, value);
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

        /// <summary>输出编辑框</summary>
        /// <param name="Html"></param>
        /// <param name="field"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static MvcHtmlString ForEditor(this HtmlHelper Html, FieldItem field, IEntity entity = null)
        {
            if (entity == null) entity = Html.ViewData.Model as IEntity;

            // 优先处理映射。因为映射可能是字符串
            {
                var mhs = ForMap(Html, field, entity);
                if (mhs != null) return mhs;
            }

            if (field.ReadOnly)
            {
                var label = "<label class=\"form-control\">{0}</label>".F(entity[field.Name]);
                return new MvcHtmlString(label);
            }

            if (field.Type == typeof(String) && (field.Length <= 0 || field.Length > 300))
            {
                return Html.ForString(field.Name, (String)entity[field.Name], field.Length);
            }

            // 如果是实体树，并且当前是父级字段，则生产下拉
            if (entity is IEntityTree)
            {
                var mhs = ForTreeEditor(Html, field, entity as IEntityTree);
                if (mhs != null) return mhs;
            }
            //// 如果有表间关系，且是当前字段，则产生关联下拉
            //if (field.Table.DataTable.Relations.Count > 0)
            //{
            //    var mhs = ForRelation(Html, field, entity);
            //    if (mhs != null) return mhs;
            //}

            return Html.ForEditor(field.Name, entity[field.Name], field.Type);
        }

        private static MvcHtmlString ForTreeEditor(HtmlHelper Html, FieldItem field, IEntityTree entity)
        {
            var fact = EntityFactory.CreateOperate(entity.GetType());
            var set = entity.GetType().GetValue("Setting") as IEntityTreeSetting;
            if (set == null || set.Parent != field.Name) return null;

            var root = entity.GetType().GetValue("Root") as IEntityTree;
            // 找到完整菜单树，但是排除当前节点这个分支
            var list = root.FindAllChildsExcept(entity as IEntityTree);
            var data = new SelectList(list, set.Key, "TreeNodeText", entity[field.Name]);
            return Html.DropDownList(field.Name, data, new { @class = "multiselect" });
        }

        private static MvcHtmlString ForMap(HtmlHelper Html, FieldItem field, IEntity entity)
        {
            var map = field.Map;
            // 为该字段创建下拉菜单
            if (map == null || map.Provider == null) return null;

            // 如果映射目标列表项过多，不能使用下拉
            var fact = EntityFactory.CreateOperate(map.Provider.EntityType);
            if (fact != null && fact.Count > 30)
            {
                // 输出数字编辑框和标签
                var label = "<label class=\"\">{0}</label>".F(entity[field.Name]);
                if (field.OriField != null) field = field.OriField;
                var mhs = Html.ForEditor(field.Name, entity[field.Name], field.Type);
                return new MvcHtmlString(mhs.ToString() + label);
            }

            return Html.ForDropDownList(map.Name, map.Provider.GetDataSource(), entity[map.Name]);
        }

        //private static MvcHtmlString ForRelation(HtmlHelper Html, FieldItem field, IEntity entity)
        //{
        //    var dr = field.Table.DataTable.Relations.FirstOrDefault(e => e.Column.EqualIgnoreCase(field.Name));
        //    // 为该字段创建下拉菜单
        //    if (dr == null) return null;

        //    var rt = EntityFactory.CreateOperate(dr.RelationTable);
        //    var list = rt.FindAllWithCache();
        //    var data = new SelectList(list, dr.RelationColumn, rt.Master.Name, entity[field.Name]);
        //    return Html.DropDownList(field.Name, data, field.IsNullable ? "无" : null, new { @class = "multiselect" });
        //}

        /// <summary>输出编辑框</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static MvcHtmlString ForEditor(this HtmlHelper Html, String name, IEntity entity = null)
        {
            if (entity == null) entity = Html.ViewData.Model as IEntity;

            var fact = EntityFactory.CreateOperate(entity.GetType());
            var field = fact.Table.FindByName(name);

            return Html.ForEditor(field, entity);
        }

        /// <summary>输出复杂对象</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MvcHtmlString ForObject(this HtmlHelper Html, String name, Object value)
        {
            if (value == null || Type.GetTypeCode(value.GetType()) != TypeCode.Object) return Html.ForEditor(name, value);

            var pis = value.GetType().GetProperties(true);
            pis = pis.Where(pi => pi.CanWrite).ToArray();

            var sb = new StringBuilder();
            var txt = Html.Label(name);
            foreach (var pi in pis)
            {
                var pname = "{0}_{1}".F(name, pi.Name);

                sb.AppendLine("<div class=\"form-group\">");
                sb.AppendLine(Html.Label(pi.Name, pi.GetDisplayName(), new { @class = "control-label col-md-2" }).ToString());
                sb.AppendLine("<div class=\"input-group col-md-8\">");
                sb.AppendLine(Html.ForEditor(pi.Name, value.GetValue(pi), pi.PropertyType).ToString());

                var des = pi.GetDescription();
                if (!des.IsNullOrEmpty())
                {
                    sb.AppendFormat("<span>&nbsp;{0}</span>", des);
                    sb.AppendLine();
                }

                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
            }

            return new MvcHtmlString(sb.ToString());
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
            //if (!atts.ContainsKey("class")) atts.Add("class", "col-xs-10 col-sm-5");
            //if (!atts.ContainsKey("class")) atts.Add("class", "col-xs-12 col-sm-8 col-md-6 col-lg-4");
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
                if (!atts.ContainsKey("type")) atts.Add("type", "tel");
                txt = Html.TextBox(name, value, atts);
            }
            else if (name.EqualIgnoreCase("MobilePhone", "CellularPhone"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-phone\"></i></span>";
                if (!atts.ContainsKey("type")) atts.Add("type", "tel");
                txt = Html.TextBox(name, value, atts);
            }
            else if (name.EqualIgnoreCase("email", "mail"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-envelope\"></i></span>";
                if (!atts.ContainsKey("type")) atts.Add("type", "email");
                txt = Html.TextBox(name, value, atts);
            }
            else if (name.EndsWithIgnoreCase("url"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-home\"></i></span>";
                //if (!atts.ContainsKey("type")) atts.Add("type", "url");
                txt = Html.TextBox(name, value, atts);
            }
            else if (length < 0 || length > 300)
            {
                txt = Html.TextArea(name, value, 3, 20, atts);
            }
            else
            {
                txt = Html.TextBox(name, value, atts);
            }
            var icog = "<div class=\"input-group\">{0}</div>";
            var html = !String.IsNullOrWhiteSpace(ico) ? String.Format(icog, ico.ToString() + txt.ToString()) : txt.ToString();
            return new MvcHtmlString(html);
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
            //if (!atts.ContainsKey("type")) atts.Add("type", "date");
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control date form_datetime");

            var obj = value.ToFullString();
            // 最小时间不显示
            if (value <= DateTime.MinValue || value.Year <= 1900) obj = "";
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
            if (!atts.ContainsKey("class")) atts.Add("class", "chkSwitch");
            // 因为得不到很好的样式支撑，暂时去掉CheckBox的Boostrap样式
            //if (!atts.ContainsKey("class")) atts.Add("class", "form-control");
            //var html="<div><label><input name=\"{0}\" value=\"{1}\" type=\"checkbox\" class=\"ace\"><span class=\"lbl\"> Latest news and announcements</span></label></div>";
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

            var txt = Html.TextBox(name, value, format, atts);

            return txt;
        }
        #endregion

        #region 专有属性
        /// <summary>输出描述</summary>
        /// <param name="Html"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDescription(this HtmlHelper Html, FieldItem field)
        {
            var des = field.Description.TrimStart(field.DisplayName).TrimStart(",", ".", "，", "。");
            if (des.IsNullOrWhiteSpace()) return new MvcHtmlString(null);

            if (field.Type == typeof(Boolean))
                return Html.Label(field.Name, des);
            else
                return new MvcHtmlString("<span class=\"middle\">{0}</span>".F(des));
        }

        /// <summary>输出描述</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDescription(this HtmlHelper Html, String name)
        {
            var entity = Html.ViewData.Model as IEntity;

            var fact = EntityFactory.CreateOperate(entity.GetType());
            var field = fact.Table.FindByName(name);

            return Html.ForDescription(field);
        }

        /// <summary>枚举</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static MvcHtmlString ForEnum(this HtmlHelper Html, String name, Object value, String label = null)
        {
            var dic = System.EnumHelper.GetDescriptions(value.GetType());
            var data = new SelectList(dic, "Key", "Value", value.ToInt());
            // 由于 Html.DropDownList 获取默认值，会从 ViewData，ViewData.Model，中获取name的值
            // 如果获取到了，则不会再看传入的selectlist的默认值，由于此处是枚举，所以通过 Html.ViewData.Eval(name) 会得到字符串值，所以导致绑定默认值失败
            // 通过 Html.ViewData[name]=(Int32)value，可以让  Html.DropDownList 优先拿到手动设置的值，就不会再从 ViewData.Model 里面找
            // 当然这里会有一个问题，如果外部同样设置ViewData[name]，则就会出现潜在的bug,所以把之前值保存到oldvalue
            var oldvalue = Html.ViewData[name];
            Html.ViewData[name] = value.ToInt();
            var hmstr = Html.DropDownList(name, data, label, new { @class = "multiselect" });

            //还原ViewData现场
            if (oldvalue != null)
                // 如果外部刚好设置这个值，则还原
                Html.ViewData[name] = oldvalue;
            else
                // 输出html后，删除垃圾
                Html.ViewData.Remove(name);
            return hmstr;
        }

        /// <summary>枚举多选，支持默认全选或不选。需要部分选中可使用ForListBox</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="selectAll">是否全部选中。默认false</param>
        /// <param name="autoPostback">自动回发</param>
        /// <returns></returns>
        public static MvcHtmlString ForEnum<T>(this HtmlHelper Html, String name, Boolean selectAll = false, Boolean autoPostback = false)
        {
            var dic = System.EnumHelper.GetDescriptions(typeof(T));

            IEnumerable values = null;
            var vs = WebHelper.Params[name].SplitAsInt();
            if (vs != null && vs.Length > 0)
                values = vs;
            else if (selectAll)
            {
                var arr = Enum.GetValues(typeof(T)) as T[];
                values = arr.Cast<Int32>().ToArray();
            }

            return Html.ForListBox(name, dic, values, autoPostback);
        }
        #endregion

        #region 下拉列表
        /// <summary>字典的下拉列表</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="items"></param>
        /// <param name="selectedValue"></param>
        /// <param name="optionLabel">默认空项的文本。此参数可以为 null。</param>
        /// <param name="autoPostback">自动回发</param>
        /// <returns></returns>
        public static MvcHtmlString ForDropDownList(this HtmlHelper Html, String name, IEnumerable items, Object selectedValue = null, String optionLabel = null, Boolean autoPostback = false)
        {
            SelectList data = null;
            if (items is IDictionary)
                data = new SelectList(items, "Key", "Value", selectedValue);
            else
                data = new SelectList(items, selectedValue);

            var atts = new Dictionary<String, Object>();
            if (Setting.Current.BootstrapSelect)
                atts.Add("class", "multiselect");
            else
                atts.Add("class", "form-control");

            // 处理自动回发
            //if (autoPostback) atts.Add("onchange", "$(':submit').click();");
            // 一个页面可能存在多个表单，比如搜索区和分页区
            // Support By Dark Li(858587868) / 老牛(65485989)
            if (autoPostback) atts.Add("onchange", "$(this).parents('form').submit();");

            return Html.DropDownList(name, data, optionLabel, atts);
        }

        /// <summary>实体列表的下拉列表。单选，自动匹配当前模型的选中项</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="list"></param>
        /// <param name="optionLabel"></param>
        /// <param name="autoPostback">自动回发</param>
        /// <returns></returns>
        public static MvcHtmlString ForDropDownList(this HtmlHelper Html, String name, IList<IEntity> list, String optionLabel = null, Boolean autoPostback = false)
        {
            var entity = Html.ViewData.Model as IEntity;
            var selectedValue = entity == null ? WebHelper.Params[name] : entity[name];

            var atts = new Dictionary<String, Object>();
            if (Setting.Current.BootstrapSelect)
                atts.Add("class", "multiselect");
            else
                atts.Add("class", "form-control");

            // 处理自动回发
            //if (autoPostback) atts.Add("onchange", "$(':submit').click();");
            if (autoPostback) atts.Add("onchange", "$(this).parents('form').submit();");

            var data = new SelectList(list.ToDictionary(), "Key", "Value", selectedValue);
            return Html.DropDownList(name, data, optionLabel, atts);
        }

        /// <summary>字典的下拉列表</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="dic"></param>
        /// <param name="selectedValues"></param>
        /// <param name="autoPostback">自动回发</param>
        /// <returns></returns>
        public static MvcHtmlString ForListBox(this HtmlHelper Html, String name, IDictionary dic, IEnumerable selectedValues, Boolean autoPostback = false)
        {
            var atts = new RouteValueDictionary();
            if (Setting.Current.BootstrapSelect)
                atts.Add("class", "multiselect");
            else
                atts.Add("class", "form-control");

            atts.Add("multiple", "");
            // 处理自动回发
            if (autoPostback) atts.Add("onchange", "$(':submit').click();");

            return Html.ListBox(name, new MultiSelectList(dic, "Key", "Value", selectedValues), atts);
        }

        /// <summary>实体列表的下拉列表。多选，自动匹配当前模型的选中项，支持数组类型或字符串类型（自动分割）的选中项</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="list"></param>
        /// <param name="autoPostback">自动回发</param>
        /// <returns></returns>
        public static MvcHtmlString ForListBox(this HtmlHelper Html, String name, IList<IEntity> list, Boolean autoPostback = false)
        {
            var entity = Html.ViewData.Model as IEntity;
            var vs = entity == null ? WebHelper.Params[name] : entity[name];
            // 如果是字符串，分割为整型数组，全局约定逗号分割
            if (vs is String) vs = (vs as String).SplitAsInt();

            var atts = new RouteValueDictionary();
            if (Setting.Current.BootstrapSelect)
                atts.Add("class", "multiselect");
            else
                atts.Add("class", "form-control");
            atts.Add("multiple", "");
            // 处理自动回发
            if (autoPostback) atts.Add("onchange", "$(':submit').click();");

            return Html.ListBox(name, new MultiSelectList(list.ToDictionary(), "Key", "Value", vs as IEnumerable), atts);
        }
        #endregion
    }
}