using System.Web.Mvc;

namespace NewLife.Cube.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "主页面";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "应用程序描述";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "联系我们";

            return View();
        }
    }
}