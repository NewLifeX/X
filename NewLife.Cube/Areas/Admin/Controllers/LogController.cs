using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Common;
using NewLife.Web;
using XCode.Membership;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>日志控制器</summary>
    [DisplayName("日志")]
    [Description("系统内重要操作均记录日志，便于审计。任何人都不能删除、修改或伪造操作日志。")]
    public class LogController : EntityController<XLog>
    {
        static LogController()
        {
            MenuOrder = 70;

            // 日志列表需要显示详细信息，不需要显示用户编号
            ListFields.AddField("Action", "Remark");
            ListFields.RemoveField("CreateUserID");
            FormFields.RemoveField("Remark");
        }

        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<XLog> Search(Pager p)
        {
            return XLog.Search(p["Q"], p["userid"].ToInt(-1), p["category"], p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p);
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

        ///// <summary>不允许删除日志</summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //[DisplayName()]
        //public override JsonResult DeleteAjax(Int32 id)
        //{
        //    var url = Request.UrlReferrer + "";

        //    return Json(new { msg = "不允许删除日志！", code = -1, url = url }, JsonRequestBehavior.AllowGet);
        //}

        /// <summary>清空全表数据</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        [DisplayName("清空")]
        public override ActionResult Clear()
        {
            if (!SysConfig.Current.Develop || !Setting.Current.Debug || ManageProvider.User?.Role?.Name != "管理员") throw new Exception("不允许删除日志");

            return base.Clear();
        }
    }
}