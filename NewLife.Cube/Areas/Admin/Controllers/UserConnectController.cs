using System.ComponentModel;
using NewLife.Cube.Entity;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>用户链接控制器</summary>
    [DisplayName("用户链接")]
    [Description("第三方登录信息")]
    public class UserConnectController : EntityController<UserConnect>
    {
        static UserConnectController()
        {
            MenuOrder = 40;

            ListFields.RemoveField("AccessToken");
            ListFields.RemoveField("RefreshToken");
            ListFields.RemoveField("Avatar");
        }
    }
}