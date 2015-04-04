using System.Web.Mvc;
using NewLife.Cube.Filters;
using XCode;

namespace NewLife.Cube.Controllers
{
    /// <summary>实体控制器基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    [EntityAuthorize]
    public class EntityController<TEntity> : Controller where TEntity : Entity<TEntity>, new()
    {
        public ActionResult Insert(TEntity entity)
        {
            ViewBag.Message = "主页面";

            return View();
        }

        public ActionResult Update(TEntity entity)
        {
            ViewBag.Message = "应用程序描述";

            return View();
        }

        public ActionResult Delete(TEntity entity)
        {
            ViewBag.Message = "联系我们";

            return View();
        }
    }
}