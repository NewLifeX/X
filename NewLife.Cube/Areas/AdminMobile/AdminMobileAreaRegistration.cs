using NewLife.Log;
using System.ComponentModel;
using System.Web.Mvc;
using XCode.Membership;

namespace NewLife.Cube.AdminMobile
{
    /// <summary>移动管理区域注册</summary>
    [DisplayName("移动管理")]
    public class AdminMobileAreaRegistration : AreaRegistrationBase
    {
        /// <summary>注册区域</summary>
        /// <param name="context"></param>
        public override void RegisterArea(AreaRegistrationContext context)
        {
            base.RegisterArea(context);

            context.Routes.IgnoreRoute("bootstrap/{*relpath}");

            // 自动检查并添加菜单
            XTrace.WriteLine("初始化权限管理体系");
            var user = ManageProvider.User;
        }
    }
}