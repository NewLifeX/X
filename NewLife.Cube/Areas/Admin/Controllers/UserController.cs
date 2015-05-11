using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using NewLife.Web;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>用户控制器</summary>
    [DisplayName("用户")]
    public class UserController : EntityController<UserX>
    {
        /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override ActionResult IndexView(Pager p)
        {
            // 让角色ID字段变为角色名字段，友好显示
            var fields = ViewBag.Fields as List<FieldItem>;
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].Name.EqualIgnoreCase("RoleID"))
                    fields[i] = UserX.Meta.AllFields.FirstOrDefault(e => e.Name == "RoleName");
            }

            return base.IndexView(p);
        }

        /// <summary>表单页视图。</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override ActionResult FormView(UserX entity)
        {
            // 清空密码，不向浏览器输出
            entity.Password = null;

            return base.FormView(entity);
        }
    }
}