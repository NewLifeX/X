using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Web.Mvc;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>用户在线控制器</summary>
    [DisplayName("用户在线")]
    [Description("已登录系统的用户，操作情况。")]
    public class UserOnlineController : EntityController<UserOnline>
    {
        static UserOnlineController()
        {
            MenuOrder = 60;
        }

        /// <summary>不允许添加修改日志</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Add(UserOnline entity)
        {
            //return base.Save(entity);
            throw new Exception("不允许添加/修改记录");
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible)
            {
                menu.Visible = false;
                (menu as IEntity).Save();
            }

            return base.ScanActionMenu(menu);
        }
    }
}