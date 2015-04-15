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
    public class MenuController : EntityController<Menu>
    {
        /// <summary>首页</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public override ActionResult Index(Pager p)
        {
            // 一页显示全部菜单，取自缓存
            p.PageSize = 10000;
            ViewBag.Page = p;

            var list = Menu.Root.AllChilds;

            return View(list);
        }
    }
}