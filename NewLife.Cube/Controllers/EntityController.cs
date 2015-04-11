using System;
using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Cube.Filters;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using XCode.Web;

namespace NewLife.Cube.Controllers
{
    /// <summary>实体控制器基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    [EntityAuthorize]
    public class EntityController<TEntity> : Controller where TEntity : Entity<TEntity>, new()
    {
        public EntityController()
        {
            ViewBag.Title = Entity<TEntity>.Meta.Table.Description + "管理";
        }

        ///// <summary>执行操作之前</summary>
        ///// <param name="filterContext"></param>
        //protected override void OnActionExecuting(ActionExecutingContext filterContext)
        //{
        //    var methodInfo = ((ReflectedActionDescriptor)filterContext.ActionDescriptor).MethodInfo;
        //    foreach (var p in methodInfo.GetParameters())
        //    {
        //        // 把所有值类型参数设为默认值，避免Int32等报null错误
        //        if (p.ParameterType.IsValueType)
        //        {
        //            filterContext.ActionParameters[p.Name] = Activator.CreateInstance(p.ParameterType);
        //        }
        //    }
        //}

        /// <summary>数据列表首页</summary>
        /// <returns></returns>
        [DisplayName("数据列表")]
        //public virtual ActionResult Index(String q, String sort, Int32 desc = 0, Int32 pageIndex = 1, Int32 pageSize = 20)
        public virtual ActionResult Index(Pager p)
        {
            ViewBag.Page = p;

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
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("数据表单")]
        public virtual ActionResult Form(String id)
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

            ViewBag.StatusMessage = "保存成功！";

            return View("Form", entity);
            //return RedirectToAction("Form/" + entity[Entity<TEntity>.Meta.Unique.Name]);
        }
    }

    public class PagerModel : Pager
    {
        private String _Q;
        /// <summary>查询关键字</summary>
        public String Q { get { return _Q; } set { _Q = value; } }
    }
}