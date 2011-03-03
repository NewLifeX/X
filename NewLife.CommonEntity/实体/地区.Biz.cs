using System;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 地区
    /// </summary>
    public partial class Area<TEntity> : EntityTree<TEntity> where TEntity : Area<TEntity>, new()
    {
        #region 对象操作
        //static Area()
        //{
        //    InitData();
        //}

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1}", Code, Name);
        }
        #endregion

        #region 扩展属性
        /// <summary>已重载。</summary>
        protected override string KeyName
        {
            get { return "Code"; }
        }

        [NonSerialized]
        private String _FriendName;
        /// <summary>友好名</summary>
        [XmlIgnore]
        public virtual String FriendName
        {
            get
            {
                if (String.IsNullOrEmpty(_FriendName))
                {
                    EntityList<TEntity> list = GetFullPath(true);
                    StringBuilder sb = new StringBuilder();
                    foreach (TEntity item in list)
                    {
                        if (item.Name == "市辖区") continue;
                        if (item.Name == "县") continue;

                        sb.Append(item.Name);
                    }
                    //for (int i = 0; i < list.Count; i++)
                    //{
                    //    if (i < list.Count - 1)
                    //    {
                    //        if (list[i].Name == "市辖区") continue;
                    //        if (list[i].Name == "县") continue;
                    //    }

                    //    sb.Append(list[i].Name);
                    //}
                    _FriendName = sb.ToString();
                }
                return _FriendName;
            }
            set { _FriendName = value; }
        }
        #endregion

        #region 扩展查询
        /// <summary>
        /// 根据主键查询一个地区实体对象用于表单编辑
        /// </summary>
        /// <param name="__ID">编号</param>
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
        /// 根据编号查找
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0) return null;
            return Meta.Cache.Entities.Find(_.ID, id);
        }

        /// <summary>
        /// 按Code查找
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static TEntity FindByCode(Int32 code)
        {
            return Meta.Cache.Entities.Find(_.Code, code);
        }

        ///// <summary>
        ///// 找到下一级
        ///// </summary>
        ///// <param name="parentcode"></param>
        ///// <returns></returns>
        //[DataObjectMethod(DataObjectMethodType.Select)]
        //public static EntityList<TEntity> FindAllByParent(Int32 parentcode)
        //{
        //    return Meta.Cache.Entities.FindAll(_.ParentCode, parentcode);
        //}

        /// <summary>
        /// 查找指定名称的父菜单下一级的子菜单
        /// </summary>
        /// <param name="parentname"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select)]
        public static EntityList<TEntity> FindAllByParent(String parentname)
        {
            TEntity a = Meta.Cache.Entities.Find(_.Name, parentname);
            if (a == null) return null;
            return FindAllByParent(a.Code);
        }

        ///// <summary>
        ///// 取得全路径的实体，由上向下排序
        ///// </summary>
        ///// <param name="includeSelf"></param>
        ///// <returns></returns>
        //public EntityList<TEntity> GetFullPath(Boolean includeSelf)
        //{
        //    EntityList<TEntity> list = null;
        //    if (Parent != null) list = Parent.GetFullPath(true);

        //    if (!includeSelf) return list;

        //    if (list == null) list = new EntityList<TEntity>();
        //    list.Add(this as TEntity);

        //    return list;
        //}
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        #endregion
    }
}