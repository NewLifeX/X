using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>设置控制器</summary>
    [DisplayName("数据中间件")]
    public class XCodeController : ConfigController<XCode.Setting>
    {
        static XCodeController()
        {
            MenuOrder = 36;
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