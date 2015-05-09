using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>角色控制器</summary>
    [DisplayName("角色")]
    public class RoleController : EntityController<Role>
    {
        /// <summary>保存</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override ActionResult Edit(Role entity)
        {
            // 保存权限项
            var menus = Menu.Root.AllChilds;
            var pfs = EnumHelper.GetDescriptions<PermissionFlags>().Where(e => e.Key > PermissionFlags.None);
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
                    foreach (var pf in pfs)
                    {
                        var has2 = GetBool("pf" + item.ID + "_" + ((Int32)pf.Key));

                        entity.Set(item.ID, has2 ? pf.Key : PermissionFlags.None);
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