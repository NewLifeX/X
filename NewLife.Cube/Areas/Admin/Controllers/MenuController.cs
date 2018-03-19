using System.ComponentModel;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>菜单控制器</summary>
    [DisplayName("菜单")]
    [Description("系统操作菜单以及功能目录树。支持排序，不可见菜单仅用于功能权限限制。每个菜单的权限子项由系统自动生成，请不要人为修改")]
    public class MenuController : EntityTreeController<Menu>
    {
        static MenuController()
        {
            MenuOrder = 80;

            // 过滤要显示的字段
            ListFields.RemoveField("Remark");
            FormFields.RemoveField("Remark");
        }
    }
}