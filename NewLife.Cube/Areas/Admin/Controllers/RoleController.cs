using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Cube.Controllers;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    [DisplayName("角色")]
    public class RoleController : EntityController<Role> { }
}