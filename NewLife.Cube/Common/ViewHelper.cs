using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube
{
    /// <summary>视图助手</summary>
    public static class ViewHelper
    {
        /// <summary>创建页面设置的委托</summary>
        public static Func<Bootstrap> CreateBootstrap = () => new Bootstrap();

        /// <summary>获取页面设置</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Bootstrap Bootstrap(this HttpContextBase context)
        {
            var bs = context.Items["Bootstrap"] as Bootstrap;
            if (bs == null)
            {
                bs = CreateBootstrap();
                context.Items["Bootstrap"] = bs;
            }

            return bs;
        }

        /// <summary>获取页面设置</summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static Bootstrap Bootstrap(this WebViewPage page)
        {
            return Bootstrap(page.Context);
        }

        /// <summary>获取页面设置</summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        public static Bootstrap Bootstrap(this Controller controller)
        {
            return Bootstrap(controller.HttpContext);
        }

        /// <summary>获取路由Key</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static RouteValueDictionary GetRouteKey(this IEntity entity)
        {
            var fact = EntityFactory.CreateOperate(entity.GetType());
            var pks = fact.Table.PrimaryKeys;

            var rv = new RouteValueDictionary();
            if (fact.Unique != null)
            {
                rv["id"] = entity[fact.Unique.Name];
            }
            else if (pks.Length > 0)
            {
                foreach (var item in pks)
                {
                    rv[item.Name] = "{0}".F(entity[item.Name]);
                }
            }

            return rv;
        }

        internal static Boolean MakeListDataView(Type entityType, String vpath, List<FieldItem> fields)
        {
            var tmp = @"@using NewLife;
@using NewLife.Web;
@using XCode;
@using XCode.Configuration;
@using System.Web.Mvc;
@using System.Web.Mvc.Ajax;
@using System.Web.Mvc.Html;
@using System.Web.Routing;
@{
    var fact = ViewBag.Factory as IEntityOperate;
    var page = ViewBag.Page as Pager;
    var fields = ViewBag.Fields as List<FieldItem>;
}
<table class=""table table-bordered table-hover table-striped table-condensed"">
    <thead>
        <tr>
            @foreach (var item in fields)
            {
                var sortUrl = item.OriField != null ? page.GetSortUrl(item.OriField.Name) : page.GetSortUrl(item.Name);
                if (item.PrimaryKey)
                {
                    <th class=""text-center hidden-md hidden-sm hidden-xs""><a href=""@Html.Raw(sortUrl)"">@item.DisplayName</a></th>
                }
                else
                {
                    <th class=""text-center""><a href=""@Html.Raw(sortUrl)"">@item.DisplayName</a></th>
                }
            }
            @if (ManageProvider.User.Has(PermissionFlags.Detail, PermissionFlags.Update, PermissionFlags.Delete))
            {
                <th class=""text-center"">操作</th>
            }
        </tr>
    </thead>
    <tbody>
        @foreach (var entity in Model)
        {
            <tr>
                @foreach (var item in fields)
                {
                    @Html.Partial(""_List_Data_Item"", new Pair(entity, item))
                }
                @if (ManageProvider.User.Has(PermissionFlags.Detail, PermissionFlags.Update, PermissionFlags.Delete))
                {
                    <td class=""text-center"">
                        @Html.Partial(""_List_Data_Action"", (Object)entity)
                    </td>
                }
            </tr>
        }
    </tbody>
</table>";
            var sb = new StringBuilder();

            sb.AppendFormat("@model IList<{0}>", entityType.FullName);
            sb.AppendLine();

            var ident = new String(' ', 4 * 3);
            sb.Append(tmp.Substring(null, "            @foreach"));

            foreach (var item in fields)
            {
                // 缩进
                sb.Append(ident);
                var css = "";
                if (item.PrimaryKey) css = " hidden-md hidden-sm hidden-xs";
                sb.AppendFormat(@"<th class=""text-center{2}""><a href=""@Html.Raw(page.GetSortUrl(""{1}""))"">{0}</a></th>", item.DisplayName ?? item.Name, item.OriField?.Name ?? item.Name, css);
                sb.AppendLine();
            }

            var ps = new Int32[2];
            sb.Append("            @if");
            sb.Append(tmp.Substring("            @if", "                @foreach (var item in fields)", 0, ps));

            ident = new String(' ', 4 * 4);
            foreach (var item in fields)
            {
                // 缩进
                sb.Append(ident);
                //sb.AppendLine(@"@Html.Partial(""_List_Data_Item"", new Pair(entity, item))");
                if (item.PrimaryKey)
                    sb.AppendFormat(@"<td class=""text-center hidden-md hidden-sm hidden-xs"">@entity.{0}</td>", item.Name);
                else
                {
                    switch (Type.GetTypeCode(item.Type))
                    {
                        case TypeCode.Boolean:
                            sb.AppendLine(@"<td class=""text-center"">");
                            sb.Append(ident);
                            sb.AppendFormat(@"    <i class=""glyphicon glyphicon-@(entity.{0} ? ""ok"" : ""remove"")"" style=""color: @(entity.{0} ? ""green"" : ""red"");""></i>", item.Name);
                            sb.AppendLine();
                            sb.Append(ident);
                            sb.Append(@"</td>");
                            break;
                        case TypeCode.DateTime:
                            sb.AppendFormat(@"<td>@entity.{0}.ToFullString("""")</td>", item.Name);
                            break;
                        case TypeCode.Decimal:
                            sb.AppendFormat(@"<td class=""text-right"">@entity.{0:n2}</td>", item.Name);
                            break;
                        case TypeCode.Single:
                        case TypeCode.Double:
                            sb.AppendFormat(@"<td class=""text-right"">@entity.{0:n2}</td>", item.Name);
                            break;
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                            // 特殊处理枚举
                            if (item.Type.IsEnum)
                                sb.AppendFormat(@"<td class=""text-center"">@entity.{0}</td>", item.Name);
                            else
                                sb.AppendFormat(@"<td class=""text-right"">@entity.{0}.ToString(""n0"")</td>", item.Name);
                            break;
                        case TypeCode.String:
                            if (item.Map != null && item.Map.Provider != null)
                            {
                                var prv = item.Map.Provider;
                                sb.AppendFormat(@"<td><a href=""{1}?{2}=@entity.{3}"">@entity.{0}</a></td>", item.Name, prv.EntityType.Name, prv.Key, item.OriField?.Name);
                            }
                            else
                                sb.AppendFormat(@"<td>@entity.{0}</td>", item.Name);
                            break;
                        default:
                            sb.AppendFormat(@"<td>@entity.{0}</td>", item.Name);
                            break;
                    }
                }
                sb.AppendLine();
            }

            sb.Append("                @if");
            sb.Append(tmp.Substring("                @if", null, ps[1]));

            File.WriteAllText(vpath.GetFullPath().EnsureDirectory(true), sb.ToString(), Encoding.UTF8);

            return true;
        }

        internal static Boolean MakeFormView()
        {
            return false;
        }
    }

    /// <summary>Bootstrap页面控制。允许继承</summary>
    public class Bootstrap
    {
        #region 属性
        /// <summary>最大列数</summary>
        public Int32 MaxColumn { get; set; } //= 2;

        /// <summary>默认标签宽度</summary>
        public Int32 LabelWidth { get; set; }// = 4;
        #endregion

        #region 当前项
        ///// <summary>当前项</summary>
        //public FieldItem Item { get; set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>类型</summary>
        public Type Type { get; set; }

        /// <summary>长度</summary>
        public Int32 Length { get; set; }

        /// <summary>设置项</summary>
        public void Set(FieldItem item)
        {
            Name = item.Name;
            Type = item.Type;
            Length = item.Length;
        }
        #endregion

        #region 构造
        /// <summary>实例化一个页面助手</summary>
        public Bootstrap()
        {
            MaxColumn = 2;
            LabelWidth = 4;
        }
        #endregion

        #region 方法
        /// <summary>获取分组宽度</summary>
        /// <returns></returns>
        public virtual Int32 GetGroupWidth()
        {
            if (MaxColumn > 1 && Type != null)
            {
                if (Type != typeof(String) || Length <= 100) return 12 / MaxColumn;
            }

            return 12;
        }
        #endregion
    }
}