using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Mvc;

namespace NewLife.Cube
{
    /// <summary>控制器基类</summary>
    public class ControllerBaseX : Controller
    {
        #region 权限菜单
        /// <summary>获取可用于生成权限菜单的Action集合</summary>
        /// <returns></returns>
        protected virtual IDictionary<MethodInfo, Int32> GetActions()
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
                var pm = att != null ? (Int32)att.Permission : 0;

                dic.Add(method, pm);
            }

            return dic;
        }
        #endregion
    }
}