using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Web.Mvc;
using NewLife.Common;
using NewLife.Web;
using XCode;
using XCode.Membership;
using XCode.Statistics;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>访问统计控制器</summary>
    [DisplayName("访问统计")]
    [Description("每个页面每天的访问统计信息")]
    public class VisitStatController : EntityController<VisitStat>
    {
        static VisitStatController()
        {
            MenuOrder = 50;
        }

        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<VisitStat> Search(Pager p)
        {
            var model = new VisitStatModel();
            model.Fill(p.Params, StatLevels.Day);
            model.Page = p["p"];

            return VisitStat.Search(model, p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p);
        }

        /// <summary>不允许添加修改</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Add(VisitStat entity)
        {
            //return base.Save(entity);
            throw new Exception("不允许添加/修改");
        }

        /// <summary>不允许添加修改</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Edit(VisitStat entity)
        {
            //return base.Save(entity);
            throw new Exception("不允许添加/修改");
        }

        /// <summary>不允许删除</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName()]
        public override ActionResult Delete(Int32 id)
        {
            //return base.Delete(id);
            throw new Exception("不允许删除");
        }

        ///// <summary>不允许删除</summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //[DisplayName()]
        //public override JsonResult DeleteAjax(Int32 id)
        //{
        //    var url = Request.UrlReferrer + "";

        //    return Json(new { msg = "不允许删除！", code = -1, url = url }, JsonRequestBehavior.AllowGet);
        //}

        /// <summary>清空全表数据</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        [DisplayName("清空")]
        public override ActionResult Clear()
        {
            if (!SysConfig.Current.Develop || !Setting.Current.Debug || ManageProvider.User?.Role?.Name != "管理员") throw new Exception("不允许删除");

            return base.Clear();
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