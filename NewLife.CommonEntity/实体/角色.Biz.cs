using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NewLife.Log;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 角色
    /// </summary>
    public partial class Role<TEntity, TMenuEntity, TRoleMenuEntity> : Role<TEntity>
        where TEntity : Role<TEntity, TMenuEntity, TRoleMenuEntity>, new()
        where TMenuEntity : Menu<TMenuEntity>, new()
        where TRoleMenuEntity : RoleMenu<TRoleMenuEntity>, new()
    {
        #region 扩展属性
        //private List<String> hasLoaded = new List<string>();

        /// <summary>
        /// 菜单
        /// </summary>
        [XmlIgnore]
        public virtual EntityList<TRoleMenuEntity> Menus
        {
            //get
            //{
            //    return RoleMenu<TRoleMenuEntity>.FindAllByRoleID(ID);
            //}
            get { return GetExtend<TMenuEntity, EntityList<TRoleMenuEntity>>("Menus", delegate { return RoleMenu<TRoleMenuEntity>.FindAllByRoleID(ID); }); }
            set { Extends["Menus"] = value; }
        }

        //[NonSerialized]
        //private EntityList<TMenuEntity> _MenuList;
        /// <summary>
        /// 拥有权限的菜单
        /// </summary>
        [XmlIgnore]
        public virtual EntityList<TMenuEntity> MenuList
        {
            //get
            //{
            //    if (_MenuList == null && !Dirtys["MenuList"])
            //    {
            //        Dirtys["MenuList"] = true;
            //        _MenuList = EntityList<TMenuEntity>.From<TRoleMenuEntity>(RoleMenu<TRoleMenuEntity>.FindAllByRoleID(ID), delegate(TRoleMenuEntity item)
            //        {
            //            return Menu<TMenuEntity>.FindByID(item.MenuID);
            //        });
            //    }
            //    return _MenuList;
            //}
            get
            {
                return GetExtend<TMenuEntity, EntityList<TMenuEntity>>("MenuList", delegate
                {
                    EntityList<TMenuEntity> list = EntityList<TMenuEntity>.From<TRoleMenuEntity>(Menus, delegate(TRoleMenuEntity item)
                    {
                        return Menu<TMenuEntity>.FindByID(item.MenuID);
                    });
                    if (list != null) list.Sort(Menu<TMenuEntity>._.Sort, true);
                    return list;
                });
            }
            set { Extends["MenuList"] = value; }
        }
        #endregion

        #region 业务
        /// <summary>
        /// 拥有指定菜单的权限，支持路径查找
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Boolean HasMenu(String name)
        {
            if (String.IsNullOrEmpty(name) || MenuList == null || MenuList.Count < 1) return false;

            //String[] ss = name.Split(new Char[] { '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            //if (ss == null || ss.Length < 1) return false;

            //EntityList<TMenuEntity> list = MenuList;
            //foreach (String item in ss)
            //{
            //    if (list == null || list.Count < 1) return false;

            //    // 找到
            //    Menu<TMenuEntity> entity = list.Find(Menu<TMenuEntity>._.Permission, item);
            //    if (entity == null) return false;

            //    list = entity.Childs;
            //}
            //return true;

            TMenuEntity entity = Menu<TMenuEntity>.FindByPath(MenuList, name, Menu<TMenuEntity>._.Permission);
            return entity != null;
        }

        /// <summary>
        /// 取得当前角色的子菜单，有权限、可显示、排序
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        public EntityList<TMenuEntity> GetMySubMenus(Int32 parentID)
        {
            EntityList<TMenuEntity> list = MenuList;
            if (list == null || list.Count < 1) return null;

            list = list.FindAll(Menu<TMenuEntity>._.ParentID, parentID);
            if (list == null || list.Count < 1) return null;
            list = list.FindAll(Menu<TMenuEntity>._.IsShow, true);
            if (list == null || list.Count < 1) return null;

            return list;
        }
        #endregion
    }

    /// <summary>
    /// 角色
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public partial class Role<TEntity> : Entity<TEntity>
          where TEntity : Role<TEntity>, new()
    {
        #region 对象操作
        static Role()
        {
            if (Meta.Count < 1)
            {
                if (XTrace.Debug) XTrace.WriteLine("开始初始化角色数据……");

                TEntity entity = new TEntity();
                entity.Name = "管理员";
                entity.Save();

                if (XTrace.Debug) XTrace.WriteLine("完成初始化角色数据！");
            }
        }

        //~Role()
        //{
        //    Console.WriteLine("Role {0} 被回收！", ID);
        //}
        #endregion

        #region 扩展查询
        /// <summary>
        /// 根据主键查询一个角色实体对象用于表单编辑
        /// </summary>
        /// <param name="__ID">角色编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKeyForEdit(Int32 __ID)
        {
            TEntity entity = FindByKey(__ID);
            if (entity == null)
            {
                entity = new TEntity();
            }
            return entity;
        }

        /// <summary>
        /// 根据编号查找角色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0 || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.Find(_.ID, id);
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        /// <summary>
        /// 拥有指定菜单的权限，支持路径查找
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Boolean HasMenu(String name) { return false; }
        #endregion
    }
}