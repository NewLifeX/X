using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>角色控制器</summary>
    [DisplayName("角色")]
    public class RoleController : EntityController<Role>
    {
        /// <summary>动作执行前</summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.HeaderTitle = "角色管理";
            ViewBag.HeaderContent = "系统基于角色授权，每个角色对不同的功能模块具备添删改查以及自定义权限等多种权限设定。";

            base.OnActionExecuting(filterContext);
        }

        /// <summary>保存</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override ActionResult Edit(Role entity)
        {
            // 保存权限项
            var menus = Menu.Root.AllChilds;
            //var pfs = EnumHelper.GetDescriptions<PermissionFlags>().Where(e => e.Key > PermissionFlags.None);
            var dels = new List<Int32>();
            // 遍历所有权限资源
            foreach (var item in menus)
            {
                // 是否授权该项
                var has = GetBool("p" + item.ID);
                if (!has)
                    dels.Add(item.ID);
                else
                {
                    // 遍历所有权限子项
                    var any = false;
                    foreach (var pf in item.Permissions)
                    {
                        var has2 = GetBool("pf" + item.ID + "_" + ((Int32)pf.Key));

                        if (has2)
                            entity.Set(item.ID, (PermissionFlags)pf.Key);
                        else
                            entity.Reset(item.ID, (PermissionFlags)pf.Key);
                        any |= has2;
                    }
                    // 如果原来没有权限，这是首次授权，且右边没有勾选任何子项，则授权全部
                    if (!any & !entity.Has(item.ID)) entity.Set(item.ID);
                }
            }
            // 删除已经被放弃权限的项
            foreach (var item in dels)
            {
                if (entity.Has(item)) entity.Permissions.Remove(item);
            }

            return base.Edit(entity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        [DisplayName("删除{type}")]
        public JsonResult DeleteAjax(int id)
        {
            var url = Request.UrlReferrer + "";

            try
            {
                var entity = Entity<Role>.FindByKey(id);
                entity.Delete();

                return Json(new { msg = "删除成功！", code = 0, url = url });
            }
            catch (Exception ex)
            {
                return Json(new { msg = "删除失败！" + ex.Message, url, code = -1 });

            }

        }

        Boolean GetBool(String name)
        {
            var v = Request[name];
            if (v.IsNullOrEmpty()) return false;

            v = v.Split(",")[0];

            if (!v.EqualIgnoreCase("true", "false")) throw new XException("非法布尔值Request[{0}]={1}", name, v);

            return v.ToBoolean();
        }
    }
}