using System;
using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Cube.Filters;
using XCode;
using XCode.Web;

namespace NewLife.Cube.Controllers
{
    /// <summary>实体控制器基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    [EntityAuthorize]
    public class EntityController<TEntity> : Controller where TEntity : Entity<TEntity>, new()
    {
        /// <summary>数据列表首页</summary>
        /// <returns></returns>
        [DisplayName("数据列表")]
        public virtual ActionResult Index()
        {
            ViewBag.Title = Entity<TEntity>.Meta.Table.Description + "管理";
            ViewBag.Grid = new EntityGrid(Entity<TEntity>.Meta.Factory);

            var list = Entity<TEntity>.FindAllWithCache();

            return View(list);
        }

        /// <summary>删除</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [DisplayName("删除")]
        public virtual ActionResult Delete(String id)
        {
            var entity = Entity<TEntity>.FindByKey(id);

            return View(entity);
        }

        /// <summary>表单，添加/修改</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("添加数据")]
        public virtual ActionResult Form(Int32 id)
        {
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

            return View(entity);
        }
    }
}