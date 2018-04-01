using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>用户令牌控制器</summary>
    [DisplayName("用户令牌")]
    [Description("授权其他人直接拥有指定用户的身份，支持有效期，支持数据接口")]
    public class UserTokenController : EntityController<UserToken>
    {
        static UserTokenController()
        {
            MenuOrder = 40;
        }

        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<UserToken> Search(Pager p)
        {
            var id = p["id"].ToInt(-1);
            if (id > 0)
            {
                var list = new List<UserToken>();
                var entity = UserToken.FindByID(id);
                if (entity != null) list.Add(entity);
                return list;
            }

            return UserToken.Search(p["Q"], p["RoleID"].ToInt(-1), null, p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p);
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible)
            {
                menu.Visible = false;
                (menu as IEntity).Save();
            }

            return base.ScanActionMenu(menu);
        }
    }
}