using System.ComponentModel;

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
    }
}