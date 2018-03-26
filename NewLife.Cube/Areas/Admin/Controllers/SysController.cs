using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using NewLife.Common;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>系统设置控制器</summary>
    [DisplayName("系统设置")]
    public class SysController : ConfigController<SysConfig>
    {
        static SysController()
        {
            MenuOrder = 38;
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