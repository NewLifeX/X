using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Cube.Controllers;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>角色控制器</summary>
    [DisplayName("角色")]
    public class RoleController : EntityController<Role> { }
}