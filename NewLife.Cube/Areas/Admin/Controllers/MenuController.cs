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
        /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override ActionResult IndexView(Pager p)
        {
            // 一页显示全部菜单，取自缓存
            p.PageSize = 10000;
            ViewBag.Page = p;

            var list = Menu.Root.AllChilds;

            return View(list);
        }

        /// <summary>上升</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("上升")]
        public ActionResult Up(Int32 id)
        {
            var menu = Menu.FindByID(id);
            menu.Up();

            return RedirectToAction("Index");
        }

        /// <summary>下降</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("下降")]
        public ActionResult Down(Int32 id)
        {
            var menu = Menu.FindByID(id);
            menu.Down();

            return RedirectToAction("Index");
        }
    }
}