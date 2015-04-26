using System;
using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Web;
using XCode;
using System.Linq;

namespace NewLife.Cube
{
    /// <summary>实体控制器基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    [EntityAuthorize]
    public class EntityController<TEntity> : Controller where TEntity : Entity<TEntity>, new()
    {
        /// <summary>构造函数</summary>
        public EntityController()
        {
            ViewBag.Title = Entity<TEntity>.Meta.Table.Description + "管理";
        }

        /// <summary>数据列表首页</summary>
        /// <returns></returns>
        [DisplayName("数据列表")]
        public virtual ActionResult Index(Pager p)
        {
            ViewBag.Page = p;
            ViewBag.Factory = Entity<TEntity>.Meta.Factory;

            // 用于显示的列
            var fields = Entity<TEntity>.Meta.Fields;
            // 长字段和密码字段不显示
            fields = fields.Where(e => e.Type != typeof(String) ||
                e.Length > 0 && e.Length <= 200
                && !e.Name.EqualIgnoreCase("password", "pass")
                ).ToArray();
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

        /// <summary>删除</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //[HttpPost]
        [DisplayName("删除")]
        public virtual ActionResult Delete(Int32 id)
        {
            var entity = Entity<TEntity>.FindByKey(id);
            entity.Delete();

            return RedirectToAction("Index");
        }

        /// <summary>表单，添加/修改</summary>
        /// <param name="id">主键。可能为空（表示添加），所以用字符串而不是整数</param>
        /// <returns></returns>
        [DisplayName("数据表单")]
        public virtual ActionResult Form(String id)
        {
            // 用于显示的列
            ViewBag.Fields = Entity<TEntity>.Meta.Fields;
            ViewBag.Factory = Entity<TEntity>.Meta.Factory;

            var entity = Entity<TEntity>.FindByKeyForEdit(id);

            return View(entity);
        }

        /// <summary>保存</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("保存")]
        public virtual ActionResult Save(TEntity entity)
        {
            entity.Save();

            ViewBag.StatusMessage = "保存成功！";

            return View("Form", entity);
            //return RedirectToAction("Form/" + entity[Entity<TEntity>.Meta.Unique.Name]);
        }
    }
}