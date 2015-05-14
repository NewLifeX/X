using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using NewLife.Log;
using NewLife.Reflection;
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

        #region 列表字段和表单字段
        private static List<FieldItem> _ListFields;
        /// <summary>列表字段过滤</summary>
        protected static List<FieldItem> ListFields { get { if (_ListFields == null) { InitFields(); } return _ListFields; } set { _ListFields = value; } }

        private static List<FieldItem> _FormFields;
        /// <summary>表单字段过滤</summary>
        protected static List<FieldItem> FormFields { get { if (_FormFields == null) { InitFields(); } return _FormFields; } set { _FormFields = value; } }

        private static void InitFields()
        {
            if (ListFields == null)
            {
                var list = Entity<TEntity>.Meta.Fields.ToList();

                var type = typeof(TEntity);
                // 扩展属性
                foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var dr = pi.GetCustomAttribute<BindRelationAttribute>();
                    if (dr != null && !dr.RelationTable.IsNullOrEmpty())
                    {
                        var rt = EntityFactory.CreateOperate(dr.RelationTable);
                        if (rt != null && rt.Master != null)
                        {
                            // 找到扩展表主字段是否属于当前实体类扩展属性
                            // 首先用对象扩展属性名加上外部主字段名
                            var master = type.GetProperty(pi.Name + rt.Master.Name);
                            // 再用外部类名加上外部主字段名
                            if (master == null) master = type.GetProperty(dr.RelationTable + rt.Master.Name);
                            if (master != null)
                            {
                                // 去掉本地用于映射的字段（如果不是主键），替换为扩展属性
                                Replace(list, dr.Column, master.Name);
                            }
                        }
                    }
                }

                ListFields = list;
            }
            if (FormFields == null)
            {
                var list = Entity<TEntity>.Meta.Fields.ToList();
                FormFields = list;
            }
        }

        /// <summary>操作字段列表，把旧项换成新项</summary>
        /// <param name="list"></param>
        /// <param name="oriName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        protected static List<FieldItem> Replace(List<FieldItem> list, String oriName, String newName)
        {

        }

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

            return View(list);
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