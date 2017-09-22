using System.ComponentModel;
using NewLife.Common;

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
    }
}