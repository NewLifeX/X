using System;
using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Cube.Controllers;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>菜单控制器</summary>
    [DisplayName("菜单")]
    public class MenuController : EntityTreeController<Menu> { }
}