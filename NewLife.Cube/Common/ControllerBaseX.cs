using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Mvc;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>控制器基类</summary>
    public class ControllerBaseX : Controller
    {
        #region 权限菜单
        /// <summary>获取可用于生成权限菜单的Action集合</summary>
        /// <param name="menu">该控制器所在菜单</param>
        /// <returns></returns>
        protected virtual IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            var dic = new Dictionary<MethodInfo, Int32>();

            var type = GetType();

            // 添加该类型下的所有Action
            foreach (var method in type.GetMethods())
            {
                if (method.IsStatic || !method.IsPublic) continue;

                if (!method.ReturnType.As<ActionResult>()) continue;

                if (method.GetCustomAttribute<HttpPostAttribute>() != null) continue;
                if (method.GetCustomAttribute<AllowAnonymousAttribute>() != null) continue;

                var att = method.GetCustomAttribute<EntityAuthorizeAttribute>();
                if (att == null)
                {
                    dic.Add(method, 0);
                }
                else
                {
                    //var name = att.ResourceName;
                    var pm = (Int32)att.Permission;
                    //if (name.IsNullOrEmpty() || name == type.Name.TrimEnd("Controller"))
                    //{
                    dic.Add(method, pm);
                    //}
                    //else
                    //{
                    //    // 指定了资源名称，也就是专有菜单
                    //    var nodeName = method.Name;
                    //    var dis = method.GetDisplayName();
                    //    var node = menu.Parent.FindByPath(nodeName);
                    //    if (node == null)
                    //    {
                    //        XTrace.WriteLine("为控制器{0}添加独立菜单{1}[{2}]", type.FullName, nodeName, name);
                    //        node = menu.Parent.Add(nodeName, dis, type.FullName + "." + nodeName, menu.Url + "/" + nodeName);
                    //    }
                    //    if (node.FullName.IsNullOrEmpty()) node.FullName = type.FullName + "." + nodeName;

                    //    // 权限名
                    //    if (pm >= 0x10)
                    //        node.Permissions[pm] = dis ?? method.Name;
                    //    else if (att.Permission.ToString().Contains("|"))
                    //        node.Permissions[pm] = att.Permission.GetDescription();
                    //    else
                    //    {
                    //        // 附加的独立Action菜单，遍历所有权限位
                    //        var n = 1;
                    //        for (var i = 0; i < 8; i++)
                    //        {
                    //            var v = (PermissionFlags)n;
                    //            if (att.Permission.Has(v)) node.Permissions[n] = v.GetDescription();

                    //            n <<= 1;
                    //        }
                    //    }

                    //    (node as IEntity).Save();
                    //}
                }
            }

            return dic;
        }
        #endregion

        #region Ajax处理
        /// <summary>返回结果并跳转</summary>
        /// <param name="data">结果。可以是错误文本、成功文本、其它结构化数据</param>
        /// <param name="url">提示信息后跳转的目标地址，[refresh]表示刷新当前页</param>
        /// <returns></returns>
        protected virtual ActionResult JsonTips(Object data, String url = null)
        {
            return ControllerHelper.JsonTips(data, url);
        }

        /// <summary>返回结果并刷新</summary>
        /// <param name="data">消息</param>
        /// <returns></returns>
        protected virtual ActionResult JsonRefresh(Object data)
        {
            return ControllerHelper.JsonRefresh(data);
        }
        #endregion
    }
}