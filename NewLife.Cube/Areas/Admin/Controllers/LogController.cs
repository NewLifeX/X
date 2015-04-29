using System;
using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Cube.Controllers;
using XCode.Membership;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>日志控制器</summary>
    [DisplayName("日志")]
    public class LogController : EntityController<XLog>
    {
        /// <summary>不允许添加修改日志</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override ActionResult Save(XLog entity)
        {
            //return base.Save(entity);
            throw new Exception("不允许添加/修改日志");
        }

        /// <summary>不允许删除日志</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override ActionResult Delete(int id)
        {
            //return base.Delete(id);
            throw new Exception("不允许删除日志");
        }
    }
}