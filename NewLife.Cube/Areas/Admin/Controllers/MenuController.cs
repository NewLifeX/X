using System;
using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Cube.Controllers;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    [DisplayName("菜单")]
    public class MenuController : EntityController<Menu>
    {
        public override ActionResult Index(Pager p)
        {
            p.PageSize = 10000;
            ViewBag.Page = p;

            //var list = Menu.Search(p["Q"], p);
            var list = Menu.Root.AllChilds;

            return View(list);
        }
    }
}