using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Mvc;
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

            var type = this.GetType();
            // 添加该类型下的所有Action
            foreach (var method in type.GetMethods())
            {
                if (method.IsStatic || !method.IsPublic) continue;

                if (!typeof(ActionResult).IsAssignableFrom(method.ReturnType)) continue;

                if (method.GetCustomAttribute<HttpPostAttribute>() != null) continue;

                var att = method.GetCustomAttribute<EntityAuthorizeAttribute>();
                if (att == null)
                {
                    dic.Add(method, 0);
                }
                else
                {
                    var name = att.ResourceName;
                    if (name.IsNullOrEmpty() || name == type.Name.TrimEnd("Controller"))
                    {
                        dic.Add(method, (Int32)att.Permission);
                    }
                    else
                    {
                        // 权限名
                        var pm = att.Permission.GetDescription();
                        if ((Int32)att.Permission >= 0x10) pm = method.GetDisplayName() ?? method.Name;

                        // 指定了资源名称，也就是专有菜单
                        var node = menu.Parent.FindByPath(name);
                        if (node == null)
                        {
                            node = menu.Parent.Add(name, method.GetDisplayName(), menu.Url + "/" + name);
                        }
                        node.Permissions[(Int32)att.Permission] = pm;
                        node.Save();
                    }
                }
            }

            return dic;
        }
        #endregion
    }
}