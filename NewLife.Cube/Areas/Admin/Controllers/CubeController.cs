using System.ComponentModel;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>系统设置控制器</summary>
    [DisplayName("魔方设置")]
    public class CubeController : ConfigController<Setting>
    {
        static CubeController()
        {
            MenuOrder = 34;
        }
    }
}