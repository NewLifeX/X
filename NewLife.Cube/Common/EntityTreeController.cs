using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using NewLife.Reflection;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>实体树控制器基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EntityTreeController<TEntity> : EntityController<TEntity> where TEntity : EntityTree<TEntity>, new()
    {
        static EntityTreeController()
        {
            var type = typeof(TEntity);
            var all = Entity<TEntity>.Meta.AllFields;
            //var list = new List<FieldItem>();
            var list = ListFields;
            var set = type.GetValue("Setting") as IEntityTreeSetting;
            var k = 0;
            var names = new String[] { set.Key, "TreeNodeName" };
            foreach (var item in names)
            {
                var fi = all.FirstOrDefault(e => e.Name.EqualIgnoreCase(item));
                if (fi != null)
                {
                    if (list.Contains(fi)) list.Remove(fi);
                    list.Insert(k++, fi);
                }
            }

            foreach (var item in all)
            {
                if (set != null && item.Name.EqualIgnoreCase(set.Name, set.Parent))
                {
                    list.Remove(item);
                    continue;
                }

                var pi = type.GetProperty(item.Name);
                if (pi == null || pi.GetCustomAttribute<DisplayNameAttribute>() == null)
                {
                    list.Remove(item);
                    continue;
                }

                //if (!list.Contains(item)) list.Insert(k++, item);
            }

            //ListFields.Clear();
            //ListFields.AddRange(list);
        }

        /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override ActionResult IndexView(Pager p)
        {
            // 一页显示全部菜单，取自缓存
            p.PageSize = 10000;
            ViewBag.Page = p;

            var list = EntityTree<TEntity>.Root.AllChilds;

            return View("ListTree", list);
        }

        ///// <summary>要导出Xml的对象</summary>
        ///// <returns></returns>
        //protected override Object OnExportXml()
        //{
        //    return EntityTree<TEntity>.Root.Childs;
        //}

        ///// <summary>要导出Json的对象</summary>
        ///// <returns></returns>
        //protected override Object OnExportJson()
        //{
        //    return EntityTree<TEntity>.Root.Childs;
        //}

        /// <summary>上升</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("上升")]
        [EntityAuthorize(PermissionFlags.Update)]
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
        [EntityAuthorize(PermissionFlags.Update)]
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
            return EntityTree<TEntity>.Meta.Cache.Find(e => (Int32)e[key] == id);
        }
    }
}