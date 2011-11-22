using System;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using XCode;
using NewLife.Linq;

namespace NewLife.CommonEntity
{
    /// <summary>地区</summary>
    [BindIndex("IX_Area_Code", true, "Code")]
    [BindIndex("IX_Area_Name", false, "Name")]
    [BindIndex("PK_Area_ID", true, "ID")]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public partial class Area<TEntity> : EntityTree<TEntity> where TEntity : Area<TEntity>, new()
    {
        #region 扩展属性
        /// <summary>已重载。</summary>
        protected override string KeyName { get { return _.Code; } }

        /// <summary>关联父键名，一般是Parent加主键，如ParentID</summary>
        protected override string ParentKeyName { get { return _.ParentCode; } }

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
        /// <param name="id">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByKeyForEdit(Int32 id)
        {
            TEntity entity = Find(new String[] { _.ID }, new Object[] { id });
            if (entity == null)
            {
                entity = new TEntity();
            }
            return entity;
        }


        /// <summary>
        /// 根据编号查找。实体缓存
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0) return null;
            return Meta.Cache.Entities.Find(_.ID, id);
        }

        /// <summary>
        /// 按Code查找。实体缓存
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static TEntity FindByCode(Int32 code)
        {
            if (code <= 0) return null;
            //return Meta.Cache.Entities.Find(_.Code, code);
            if (Meta.Cache.Entities.Find(_.Code, code) == null)
                return Meta.Cache.Entities.Find(_.OldCode, code);
            else
                return Meta.Cache.Entities.Find(_.Code, code);
        }

        /// <summary>
        /// 按名称查找。实体缓存
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static EntityList<TEntity> FindByName(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;
            //return Meta.Cache.Entities.FindAll(_.Name, name);

            //return FindAllByName(name);
            return Root.FindAllByName(name);
        }

        /// <summary>
        /// 按名称查找。实体缓存
        /// </summary>
        /// <param name="name">地区名称</param>
        /// <param name="withLike">未找到时，是否查找相似的地区。因为地区可能有市、县、区等字样，而查询名称没填</param>
        /// <param name="deepth">地区路径的最大可能层次。内置地区数据库只有三层</param>
        /// <returns></returns>
        public EntityList<TEntity> FindAllByName(String name, Boolean withLike = true, Int32 deepth = 0)
        {
            if (String.IsNullOrEmpty(name)) return null;
            if (deepth <= 0)
            {
                if ((this as IEntity).IsNullKey)
                    deepth = 3;
                else if ((this.Parent as IEntity).IsNullKey)
                    deepth = 2;
                else
                    deepth = 1;
            }

            //EntityList<TEntity> list = Meta.Cache.Entities.FindAll(_.Name, name);
            EntityList<TEntity> list = Childs.FindAll(_.Name, name);
            if (list != null && list.Count > 0) return list;

            // 试试下一级
            if (deepth >= 2)
            {
                foreach (var item in Childs)
                {
                    list = item.FindAllByName(name, withLike, deepth - 1);
                    if (list != null && list.Count > 0) return list;
                }
            }

            if (!withLike) return list;

            // 未找到，开始模糊查找
            //String[] names = Meta.Cache.Entities.Select<TEntity, String>(e => e.Name).ToArray();
            String[] names = Childs.Select<TEntity, String>(e => e.Name).ToArray();
            String[] rs = StringHelper.LCSSearch(name, names);
            if (rs != null && rs.Length > 0)
            {
                list = new EntityList<TEntity>(Childs.Where<TEntity>(e => rs.Contains(e.Name, StringComparer.OrdinalIgnoreCase)));
                return list;
            }

            // 如果层次大于1，开始拆分。比如江苏省南京市鼓楼区，第一段至少一个字
            if (deepth > 1)
            {
                // 应该从右往左，这样子才能做到最大匹配，否则会因为模糊查找而混杂很多其它东西
                //for (int i = name.Length - 1; i >= 1; i--)
                for (int i = 0; i < name.Length; i++)
                {
                    String first = name.Substring(0, i);
                    String last = name.Substring(i);

                    // 必须找到左边的，否则这个匹配没有意义
                    //TEntity entity = Meta.Cache.Entities.Find(_.Name, first);
                    // 模糊查询一层
                    var list2 = FindAllByName(first, true, 1);
                    if (list2 != null && list2.Count > 0)
                    {
                        foreach (var item in list2)
                        {
                            list = item.FindAllByName(last, withLike, deepth - 1);
                            if (list != null && list.Count > 0) return list;
                        }
                    }
                }
            }

            return list;
        }

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
            //return FindAllByParent(a.Code);
            if (FindAllByParent(a.Code) == null)
                return FindAllByParent(a.OldCode);
            else
                return FindAllByParent(a.Code);

        }
        #endregion

        #region 对象操作
        ///// <summary>
        ///// 已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert
        ///// </summary>
        ///// <returns></returns>
        //public override Int32 Insert()
        //{
        //    return base.Insert();
        //}

        ///// <summary>
        ///// 已重载。在事务保护范围内处理业务，位于Valid之后
        ///// </summary>
        ///// <returns></returns>
        //protected override Int32 OnInsert()
        //{
        //    return base.OnInsert();
        //}

        /// <summary>
        /// 验证数据，通过抛出异常的方式提示验证失败。
        /// </summary>
        /// <param name="isNew"></param>
        public override void Valid(Boolean isNew)
        {
            // 建议先调用基类方法，基类方法会对唯一索引的数据进行验证
            base.Valid(isNew);

            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (String.IsNullOrEmpty(_.Name)) throw new ArgumentNullException(_.Name, _.Name.Description + "无效！");
            if (Code < 1) throw new ArgumentOutOfRangeException(_.Code, _.Code.Description + "必须大于0！");

            // 在新插入数据或者修改了指定字段时进行唯一性验证，CheckExist内部抛出参数异常
            //if (isNew || Dirtys[_.Name]) CheckExist(_.Name);
            if (isNew || Dirtys[_.Name] || Dirtys[_.ParentCode]) CheckExist(_.Name, _.ParentCode);
            //if ((isNew || Dirtys[_.Name]) && Exist(_.Name)) throw new ArgumentException(_.Name, "值为" + Name + "的" + _.Name.Description + "已存在！");
        }


        ///// <summary>
        ///// 首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法
        ///// </summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    base.InitData();

        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    // Meta.Count是快速取得表记录数
        //    if (Meta.Count > 0) return;

        //    // 需要注意的是，如果该方法调用了其它实体类的首次数据库操作，目标实体类的数据初始化将会在同一个线程完成
        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}管理员数据……", typeof(TEntity).Name);

        //    TEntity user = new TEntity();
        //    user.Name = "admin";
        //    user.Password = DataHelper.Hash("admin");
        //    user.DisplayName = "管理员";
        //    user.RoleID = 1;
        //    user.IsEnable = true;
        //    user.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}管理员数据！", typeof(TEntity).Name);
        //}
        #endregion

        #region 高级查询
        // 以下为自定义高级查询的例子

        ///// <summary>
        ///// 查询满足条件的记录集，分页、排序
        ///// </summary>
        ///// <param name="key">关键字</param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>实体集</returns>
        //[DataObjectMethod(DataObjectMethodType.Select, true)]
        //public static EntityList<Area> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindAll(SearchWhere(key), orderClause, null, startRowIndex, maximumRows);
        //}

        ///// <summary>
        ///// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
        ///// </summary>
        ///// <param name="key">关键字</param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>记录数</returns>
        //public static Int32 SearchCount(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindCount(SearchWhere(key), null, null, 0, 0);
        //}

        /// <summary>
        /// 构造搜索条件
        /// </summary>
        /// <param name="key">关键字</param>
        /// <returns></returns>
        private static String SearchWhere(String key)
        {
            // WhereExpression重载&和|运算符，作为And和Or的替代
            WhereExpression exp = new WhereExpression();

            // SearchWhereByKeys系列方法用于构建针对字符串字段的模糊搜索
            if (!String.IsNullOrEmpty(key)) SearchWhereByKeys(exp.Builder, key);

            // 以下仅为演示，2、3行是同一个意思的不同写法，FieldItem重载了等于以外的运算符（第4行）
            //exp &= _.Name.Equal("testName")
            //    & !String.IsNullOrEmpty(key) & _.Name.Equal(key)
            //    .AndIf(!String.IsNullOrEmpty(key), _.Name.Equal(key))
            //    | _.ID > 0;

            return exp;
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1}", Code, Name);
        }
        #endregion
    }
}