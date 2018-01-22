using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using XCode;
using XCode.Configuration;
using XCode.Membership;

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
    var fields = ViewBag.Fields as IReadOnlyList<FieldItem>;
    var enableSelect = this.EnableSelect();
    //var provider = ManageProvider.Provider;
}
<table class=""table table-bordered table-hover table-striped table-condensed"">
    <thead>
        <tr>
            @if (enableSelect)
            {
                <th class=""text-center"" style=""width:10px;""><input type=""checkbox"" id=""chkAll"" title=""全选"" /></th>
            }
            @foreach(var item in fields)
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
                <th class=""text-center"" style=""min-width:100px;"">操作</th>
            }
        </tr>
    </thead>
    <tbody>
        @foreach (var entity in Model)
        {
            <tr>
                @if (enableSelect)
                {
                    <td class=""text-center""><input type=""checkbox"" name=""keys"" value=""@entity.ID"" /></td>
                }
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
            var fact = EntityFactory.CreateOperate(entityType);

            sb.AppendFormat("@model IList<{0}>", entityType.FullName);
            sb.AppendLine();

            var str = tmp.Substring(null, "            @foreach");
            // 如果有用户字段，则启用provider
            if (fields.Any(f => f.Name.EqualIgnoreCase("CreateUserID", "UpdateUserID")))
                str = str.Replace("//var provider", "var provider");
            sb.Append(str);

            var ident = new String(' ', 4 * 3);

            foreach (var item in fields)
            {
                // 缩进
                sb.Append(ident);

                var name = item.OriField?.Name ?? item.Name;
                var des = item.DisplayName ?? item.Name;

                // 样式
                if (item.PrimaryKey)
                    sb.Append(@"<th class=""text-center hidden-md hidden-sm hidden-xs""");
                else
                    sb.Append(@"<th class=""text-center""");

                // 固定宽度
                if (item.Type == typeof(DateTime))
                    sb.AppendFormat(@" style=""min-width:134px;""");

                // 备注
                if (!item.Description.IsNullOrEmpty() && item.Description != des) sb.AppendFormat(@" title=""{0}""", item.Description);

                // 内容
                sb.AppendFormat(@"><a href=""@Html.Raw(page.GetSortUrl(""{1}""))"">{0}</a></th>", des, name);

                sb.AppendLine();
            }

            var ps = new Int32[2];
            str = tmp.Substring("            @if (ManageProvider", "                @foreach (var item in fields)", 0, ps);
            if (fact.Unique != null)
                str = str.Replace("@entity.ID", "@entity." + fact.Unique.Name);
            else
                str = str.Replace("@entity.ID", "");

            sb.Append("            @if (ManageProvider");
            sb.Append(str);

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
                            else if (item.Name.EqualIgnoreCase("CreateUserID", "UpdateUserID"))
                                BuildUser(item, sb);
                            else
                                sb.AppendFormat(@"<td class=""text-right"">@entity.{0}.ToString(""n0"")</td>", item.Name);
                            break;
                        case TypeCode.String:
                            if (item.Map != null && item.Map.Provider != null)
                            {
                                var prv = item.Map.Provider;
                                sb.AppendFormat(@"<td><a href=""{1}?{2}=@entity.{3}"">@entity.{0}</a></td>", item.Name, prv.EntityType.Name, prv.Key, item.OriField?.Name);
                            }
                            else if (item.Name.EqualIgnoreCase("CreateIP", "UpdateIP"))
                                BuildIP(item, sb);
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

        private static void BuildUser(FieldItem item, StringBuilder sb)
        {
            sb.AppendFormat(@"<td class=""text-right"">@provider.FindByID(entity.{0})</td>", item.Name);
        }

        private static void BuildIP(FieldItem item, StringBuilder sb)
        {
            sb.AppendFormat(@"<td title=""@entity.{0}.IPToAddress()"">@entity.{0}</td>", item.Name);
        }

        internal static Boolean MakeFormView()
        {
            return false;
        }

        /// <summary>是否启用多选</summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static Boolean EnableSelect(this WebViewPage page)
        {
            var fact = page.ViewBag.Factory as IEntityOperate;
            var fk = fact?.Unique;
            if (fk == null) return false;

            if (page.ViewData.ContainsKey("EnableSelect")) return (Boolean)page.ViewData["EnableSelect"];

            var user = page.ViewBag.User as IUser ?? page.User.Identity as IUser;
            if (user == null) return false;

            return user.Has(PermissionFlags.Update, PermissionFlags.Delete);
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