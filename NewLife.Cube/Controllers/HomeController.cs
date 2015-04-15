using System.Web.Mvc;

namespace NewLife.Cube.Controllers
{
    /// <summary>主页面</summary>
    public class HomeController : Controller
    {
        /// <summary>主页面</summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            ViewBag.Message = "主页面";

            return View();
        }

        /// <summary>应用程序描述</summary>
        public ActionResult About()
        {
            ViewBag.Message = "应用程序描述";

            return View();
        }

        /// <summary>联系我们</summary>
        public ActionResult Contact()
        {
            ViewBag.Message = "联系我们";

            return View();
        }
    }
}