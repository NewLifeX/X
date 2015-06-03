using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using NewLife.Common;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>实体控制器基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    [EntityAuthorize]
    public class EntityController<TEntity> : ControllerBaseX where TEntity : Entity<TEntity>, new()
    {
        #region 构造
        /// <summary>构造函数</summary>
        public EntityController()
        {
            ViewBag.Title = Entity<TEntity>.Meta.Table.Description + "管理";
        }
        #endregion

        #region 默认Action
        /// <summary>数据列表首页</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        [DisplayName("{type}管理")]
        public virtual ActionResult Index(Pager p)
        {
            ViewBag.Page = p;

            // 用于显示的列
            var fields = GetFields(false);
            ViewBag.Fields = fields;

            return IndexView(p);
        }

        /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected virtual ActionResult IndexView(Pager p)
        {
            var list = Entity<TEntity>.Search(p["Q"], p);

            return View("List", list);
        }

        /// <summary>表单，查看</summary>
        /// <param name="id">主键。可能为空（表示添加），所以用字符串而不是整数</param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        [DisplayName("查看{type}")]
        public virtual ActionResult Detail(String id)
        {
            var entity = Entity<TEntity>.FindByKeyForEdit(id);
            if (entity.IsNullKey) throw new XException("要查看的数据[{0}]不存在！", id);

            return FormView(entity);
        }

        /// <summary>删除</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        [DisplayName("删除{type}")]
        public virtual ActionResult Delete(Int32 id)
        {
            var url = Request.UrlReferrer + "";

            try
            {
                var entity = Entity<TEntity>.FindByKey(id);
                OnDelete(entity);

                Js.Alert("删除成功！").Redirect(url);
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                Js.Alert("删除失败！" + ex.Message).Redirect(url);
                return new EmptyResult();
            }

            //// 跳转到来源地址
            //if (url != "")
            //    return Redirect(url);
            //else
            //    return RedirectToAction("Index");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        [DisplayName("删除{type}")]
        public virtual JsonResult DeleteAjax(Int32 id)
        {
            var url = Request.UrlReferrer + "";

            try
            {
                var entity = Entity<TEntity>.FindByKey(id);
                OnDelete(entity);

                return Json(new { msg = "删除成功！", code = 0, url = url }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { msg = "删除失败！" + ex.Message, url, code = -1 }, JsonRequestBehavior.AllowGet);

            }
        }

        /// <summary>表单，添加/修改</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        [DisplayName("添加{type}")]
        public virtual ActionResult Add()
        {
            var entity = Entity<TEntity>.Meta.Factory.Create() as TEntity;

            // 记下添加前的来源页，待会添加成功以后跳转
            Session["Cube_Add_Referrer"] = Request.UrlReferrer.ToString();

            return FormView(entity);
        }

        /// <summary>保存</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        [HttpPost]
        [ValidateInput(false)]
        public virtual ActionResult Add(TEntity entity)
        {
            if (!Valid(entity))
            {
                ViewBag.StatusMessage = "验证失败！";
                return FormView(entity);
            }

            var rs = false;
            try
            {
                OnInsert(entity);
                rs = true;
            }
            catch (ArgumentException aex)
            {
                ModelState.AddModelError(aex.ParamName, aex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            if (!rs)
            {
                ViewBag.StatusMessage = "添加失败！";
                return FormView(entity);
            }

            ViewBag.StatusMessage = "添加成功！";

            var url = Session["Cube_Add_Referrer"] + "";
            if (!url.IsNullOrEmpty())
                return Redirect(url);
            else
                // 新增完成跳到列表页，更新完成保持本页
                return RedirectToAction("Index");
        }

        /// <summary>表单，添加/修改</summary>
        /// <param name="id">主键。可能为空（表示添加），所以用字符串而不是整数</param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Update)]
        [DisplayName("更新{type}")]
        public virtual ActionResult Edit(String id)
        {
            var entity = Entity<TEntity>.FindByKeyForEdit(id);
            if (entity.IsNullKey) throw new XException("要编辑的数据[{0}]不存在！", id);

            return FormView(entity);
        }

        /// <summary>保存</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Update)]
        [HttpPost]
        [ValidateInput(false)]
        public virtual ActionResult Edit(TEntity entity)
        {
            if (!Valid(entity))
            {
                ViewBag.StatusMessage = "验证失败！";
                return FormView(entity);
            }

            var rs = false;
            try
            {
                OnUpdate(entity);
                rs = true;
            }
            catch (ArgumentException aex)
            {
                ModelState.AddModelError(aex.ParamName, aex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            if (!rs)
            {
                ViewBag.StatusMessage = "保存失败！";
                return FormView(entity);
            }

            ViewBag.StatusMessage = "保存成功！";

            // 更新完成保持本页
            return FormView(entity);
        }

        /// <summary>表单页视图。子控制器可以重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual ActionResult FormView(TEntity entity)
        {
            // 用于显示的列
            if (ViewBag.Fields == null) ViewBag.Fields = GetFields(true);

            return View("Form", entity);
        }
        #endregion

        #region 实体操作重载
        /// <summary>添加实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual Int32 OnInsert(TEntity entity) { return entity.Insert(); }

        /// <summary>更新实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual Int32 OnUpdate(TEntity entity) { return entity.Update(); }

        /// <summary>删除实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual Int32 OnDelete(TEntity entity) { return entity.Delete(); }

        /// <summary>验证实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual Boolean Valid(TEntity entity) { return true; }
        #endregion

        #region 列表字段和表单字段
        private static FieldCollection _ListFields = new FieldCollection(Entity<TEntity>.Meta.Factory).SetRelation(false);
        /// <summary>列表字段过滤</summary>
        protected static FieldCollection ListFields { get { return _ListFields; } set { _ListFields = value; } }

        private static FieldCollection _FormFields = new FieldCollection(Entity<TEntity>.Meta.Factory).SetRelation(true);
        /// <summary>表单字段过滤</summary>
        protected static FieldCollection FormFields { get { return _FormFields; } set { _FormFields = value; } }

        /// <summary>获取要显示的字段列表</summary>
        /// <param name="isForm">是否是表单</param>
        /// <returns></returns>
        protected virtual List<FieldItem> GetFields(Boolean isForm)
        {
            return (isForm ? FormFields : ListFields) ?? Entity<TEntity>.Meta.Fields.ToList();
        }
        #endregion

        #region 权限菜单
        /// <summary>自动从实体类拿到显示名</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            // 设置显示名
            if (menu.DisplayName.IsNullOrEmpty())
            {
                menu.DisplayName = Entity<TEntity>.Meta.Table.DataTable.DisplayName;
                menu.Visible = true;
                //menu.Save();
            }

            return base.ScanActionMenu(menu);
        }
        #endregion

        #region 默认页头
        /// <summary>动作执行前</summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // 默认加上实体工厂
            ViewBag.Factory = Entity<TEntity>.Meta.Factory;

            if (ViewBag.HeaderTitle == null) ViewBag.HeaderTitle = Entity<TEntity>.Meta.Table.Description + "管理";
            if (ViewBag.HeaderContent == null && SysConfig.Current.Develop)
                ViewBag.HeaderContent = "这里是页头内容，你可以通过重载OnActionExecuting然后设置ViewBag.HeaderTitle/HeaderContent来修改";

            base.OnActionExecuting(filterContext);
        }
        #endregion
    }

    /// <summary>实体树控制器基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EntityTreeController<TEntity> : EntityController<TEntity> where TEntity : EntityTree<TEntity>, new()
    {
        /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override ActionResult IndexView(Pager p)
        {
            // 一页显示全部菜单，取自缓存
            p.PageSize = 10000;
            ViewBag.Page = p;

            var list = EntityTree<TEntity>.Root.AllChilds;

            return View("List", list);
        }

        /// <summary>上升</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("上升")]
        public ActionResult Up(Int32 id)
        {
            var menu = FindByID(id);
            menu.Up();

            return RedirectToAction("Index");
        }

        /// <summary>下降</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("下降")]
        public ActionResult Down(Int32 id)
        {
            var menu = FindByID(id);
            menu.Down();

            return RedirectToAction("Index");
        }

        /// <summary>根据ID查找节点</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected static TEntity FindByID(Int32 id)
        {
            var key = EntityTree<TEntity>.Meta.Unique.Name;
            return EntityTree<TEntity>.Meta.Cache.Entities.ToList().FirstOrDefault(e => (Int32)e[key] == id);
        }
    }
}