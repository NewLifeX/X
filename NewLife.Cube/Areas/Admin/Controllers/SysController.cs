using System.ComponentModel;
using System.Web;
using System.Web.Mvc;
using NewLife.Common;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>系统设置控制器</summary>
    [DisplayName("高级设置")]
    public class SysController : ControllerBaseX
    {
        //protected override void OnActionExecuting(ActionExecutingContext filterContext)
        //{
        //    var act = filterContext.ActionDescriptor;
        //    if (act.ActionName == "Index")
        //    {

        //    }

        //    base.OnActionExecuting(filterContext);
        //}

        /// <summary>系统设置</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        [DisplayName("系统设置")]
        public ActionResult Index(SysConfig config)
        {
            if (HttpContext.Request.HttpMethod == "POST")
            {
                LogProvider.Provider.WriteLog(config.GetType(), "修改", null);

                config.Save(SysConfig.Current.ConfigFile);
                SysConfig.Current = null;
            }
            else
            {
                config = SysConfig.Current;
            }

            return View("SysConfig", config);
        }

        //public ActionResult Index(SysConfig config)
        //{
        //    config.Save();

        //    return View("SysConfig", config);
        //}
    }
}
