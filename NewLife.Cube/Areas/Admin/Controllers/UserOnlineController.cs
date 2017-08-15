using System;
using System.ComponentModel;
using System.Web.Mvc;
using System.Web.Security;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>用户在线控制器</summary>
    [DisplayName("用户在线")]
    [Description("已登录系统的用户，操作情况。")]
    public class UserOnlineController : EntityController<UserOnline>
    {
        //static UserOnlineController()
        //{
        //}

        ///// <summary>搜索数据集</summary>
        ///// <param name="p"></param>
        ///// <returns></returns>
        //protected override EntityList<UserOnline> FindAll(Pager p)
        //{
        //    return UserX.Search(p["Q"], p["RoleID"].ToInt(), null, p);
        //}

        /// <summary>不允许添加修改日志</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Add(UserOnline entity)
        {
            //return base.Save(entity);
            throw new Exception("不允许添加/修改记录");
        }
    }
}