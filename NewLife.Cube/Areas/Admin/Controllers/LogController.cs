using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using XCode.Membership;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>日志控制器</summary>
    [DisplayName("日志")]
    public class LogController : EntityController<XLog>
    {
        static LogController()
        {
            // 日志列表需要显示详细信息，不需要显示用户编号
            ListFields.AddField("Remark").RemoveField("UserID");
        }

        /// <summary>不允许添加修改日志</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Add(XLog entity)
        {
            //return base.Save(entity);
            throw new Exception("不允许添加/修改日志");
        }

        /// <summary>不允许添加修改日志</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Edit(XLog entity)
        {
            //return base.Save(entity);
            throw new Exception("不允许添加/修改日志");
        }

        /// <summary>不允许删除日志</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Delete(Int32 id)
        {
            //return base.Delete(id);
            throw new Exception("不允许删除日志");
        }

        /// <summary>获取可用于生成权限菜单的Action集合</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            var dic = base.ScanActionMenu(menu);

            dic = dic.Where(e => !e.Key.Name.EqualIgnoreCase("Add", "Edit", "Delete")).ToDictionary(e => e.Key, e => e.Value);

            return dic;
        }
    }
}