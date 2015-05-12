using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
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
            ViewBag.Factory = Entity<TEntity>.Meta.Factory;

            // 用于显示的列
            var fields = GetFields(false);
            // 长字段和密码字段不显示
            fields = fields.Where(e => e.Type != typeof(String) ||
                e.Length > 0 && e.Length <= 200
                && !e.Name.EqualIgnoreCase("password", "pass")
                ).ToList();
            ViewBag.Fields = fields;

            return IndexView(p);
        }

        /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected virtual ActionResult IndexView(Pager p)
        {
            var list = Entity<TEntity>.Search(p["Q"], p);

            return View(list);
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
            var entity = Entity<TEntity>.FindByKey(id);
            entity.Delete();

            return RedirectToAction("Index");
        }

        /// <summary>表单，添加/修改</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        [DisplayName("添加{type}")]
        public virtual ActionResult Add()
        {
            var entity = Entity<TEntity>.Meta.Factory.Create() as TEntity;

            return FormView(entity);
        }

        /// <summary>保存</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        [HttpPost]
        public virtual ActionResult Add(TEntity entity)
        {
            entity.Insert();

            ViewBag.StatusMessage = "保存成功！";

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
        public virtual ActionResult Edit(TEntity entity)
        {
            var isnew = entity.IsNullKey;

            entity.Save();

            ViewBag.StatusMessage = "保存成功！";

            // 新增完成跳到列表页，更新完成保持本页
            if (isnew)
                return RedirectToAction("Index");
            else
                return FormView(entity);
            //return RedirectToAction("Form", new { id = entity[Entity<TEntity>.Meta.Unique.Name] });
        }

        /// <summary>表单页视图。子控制器可以重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual ActionResult FormView(TEntity entity)
        {
            // 用于显示的列
            if (ViewBag.Fields == null) ViewBag.Fields = GetFields(true);
            ViewBag.Factory = Entity<TEntity>.Meta.Factory;

            return View("Form", entity);
        }
        #endregion

        #region 辅助
        /// <summary>列表字段过滤</summary>
        protected String[] ListFields;

        /// <summary>表单字段过滤</summary>
        protected String[] FormFields;

        /// <summary>获取要显示的字段列表</summary>
        /// <param name="isForm">是否是表单</param>
        /// <returns></returns>
        protected virtual List<FieldItem> GetFields(Boolean isForm)
        {
            var fs = Entity<TEntity>.Meta.Fields.ToList();

            var filter = isForm ? FormFields : ListFields;
            if (filter == null || filter.Length == 0) return fs;

            fs = Entity<TEntity>.Meta.AllFields.ToList();
            return fs.Where(e => filter.Contains(e.Name)).ToList();
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
    }
}