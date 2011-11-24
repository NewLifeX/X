using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using NewLife.Linq;
using XCode;
using System.IO;

namespace NewLife.CommonEntity
{
    /// <summary>地区</summary>
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
                    _FriendName = sb.ToString();
                }
                return _FriendName;
            }
            set { _FriendName = value; }
        }

        private Dictionary<Int32, String> _OldArea;
        /// <summary>旧地区</summary>
        public Dictionary<Int32, String> OldArea
        {
            get
            {
                if (_OldArea == null)
                {
                    _OldArea = new Dictionary<Int32, String>();

                    if (!String.IsNullOrEmpty(Description))
                    {
                        String[] ss = Description.Split(";", "；");
                        foreach (var item in ss)
                        {
                            String[] ss2 = item.Split("|");
                            if (ss2 != null && ss2.Length > 0)
                            {
                                Int32 code = 0;
                                if (Int32.TryParse(ss2[0], out code)) _OldArea.Add(code, ss2.Length > 1 ? ss2[1] : null);
                            }
                        }
                    }
                }
                return _OldArea;
            }
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

            TEntity entity = Meta.Cache.Entities.Find(a => a.Code == code || a.OldCode == code || a.OldCode2 == code || a.OldCode3 == code);
            if (entity != null) return entity;

            return Meta.Cache.Entities.Find(a => a.OldArea.ContainsKey(code));
        }

        /// <summary>
        /// 按名称查找。实体缓存
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [Obsolete("请改为使用指定地区下的FindAllByName或Root.FindAllByName！")]
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
                else if (this.Parent == null || (this.Parent as IEntity).IsNullKey)
                    deepth = 2;
                else
                    deepth = 1;
            }

            //EntityList<TEntity> list = Meta.Cache.Entities.FindAll(_.Name, name);
            //EntityList<TEntity> list = Childs.FindAll(_.Name, name);
            EntityList<TEntity> list = Childs.FindAll(a => CompAreaName(a.Name, name));
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

        static Dictionary<String, String> maps;
        /// <summary>比较地区名，考虑砍掉后缀的比较</summary>
        /// <param name="name1"></param>
        /// <param name="name2"></param>
        /// <returns></returns>
        static Boolean CompAreaName(String name1, String name2)
        {
            if (name1 == name2) return true;
            name1 = CutName(name1);
            name2 = CutName(name2);
            if (name1 == name2) return true;

            // 再来一次
            name1 = CutName(name1);
            name2 = CutName(name2);
            if (name1 == name2) return true;

            if (maps == null)
            {
                maps = new Dictionary<string, string>();
                //maps.Add("通州", "通");
                maps.Add("邱", "丘");
                //maps.Add("涿州", "涿");
                maps.Add("峨眉山", "峨眉");
                maps.Add("伊犁", "伊犁哈萨克");
            }

            String v = null;
            if (maps.TryGetValue(name1, out v) && v == name2) return true;
            if (maps.TryGetValue(name2, out v) && v == name1) return true;

            if (name1.Length < 1 || name2.Length < 1) return false;

            if (name1.Contains(name2) || name2.Contains(name1)) return true;

            return false;
        }

        static String[] suffixs = new String[] { "自治区", "自治州", "各族自治县", "自治县", "地区", "辖区", "区", "市", "盟", "州", "县", "旗" };
        static String CutName(String name)
        {
            foreach (var item in suffixs)
            {
                if (name.EndsWith(item)) return name = name.Substring(0, name.Length - item.Length);
            }

            return name;
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
        /// <summary>
        /// 已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert
        /// </summary>
        /// <returns></returns>
        public override Int32 Insert()
        {
            CheckOldArea();

            return base.Insert();
        }

        /// <summary>
        /// 已重载
        /// </summary>
        /// <returns></returns>
        public override Int32 Update()
        {
            CheckOldArea();

            return base.Update();
        }

        void CheckOldArea()
        {
            if (OldArea.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in OldArea)
                {
                    if (sb.Length > 0) sb.Append(";");
                    sb.AppendFormat("{0}|{1}", item.Key, item.Value);
                }
                Description = sb.ToString();
            }
        }

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
        /// <summary>查找地区。查找编码是否存在，若不存在，则按照名称匹配</summary>
        /// <param name="code"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindByCodeAndName(Int32 code, String name)
        {
            TEntity entity = FindByCode(code);
            // 如果编码找不到，则从上级开始找名字
            if (entity == null)
            {
                // 直接上级
                TEntity parent = null;
                if (code % 100 != 0) parent = FindByCode(code / 100 * 100);
                if (parent != null)
                {
                    EntityList<TEntity> list = parent.FindAllByName(name, false);
                    if (list != null && list.Count > 0) entity = list[0];
                }
                if (entity == null)
                {
                    // 再上一级
                    parent = FindByCode(code / 10000 * 10000);

                    // 上级都找不到，就没有意义了
                    if (parent == null) return null;

                    EntityList<TEntity> list = parent.FindAllByName(name, false);
                    if (list != null && list.Count > 0) entity = list[0];
                }
                if (entity == null && code / 10000 * 10000 == 510000)
                {
                    // 四川省51划分出重庆直辖市50
                    parent = FindByCode(500000);

                    // 上级都找不到，就没有意义了
                    if (parent == null) return null;

                    EntityList<TEntity> list = parent.FindAllByName(name, false);
                    if (list != null && list.Count > 0) entity = list[0];
                }
            }

            return entity;
        }

        /// <summary>检查并附件到现有地区，如果没有找到匹配地区，则附加到最近的顶级地区，不会新增地区</summary>
        /// <param name="code"></param>
        /// <param name="name"></param>
        /// <param name="fullname"></param>
        /// <returns></returns>
        public static TEntity CheckAndAppend(Int32 code, String name, String fullname = null)
        {
            TEntity entity = FindByCodeAndName(code, name);
            if (entity != null)
            {
                if (entity.Code == entity.OldCode) entity.OldCode = 0;
                if (entity.Code == entity.OldCode2) entity.OldCode2 = 0;
                if (entity.Code == entity.OldCode3) entity.OldCode3 = 0;
                if (entity.Code != code && entity.OldCode != code && entity.OldCode2 != code && entity.OldCode3 != code && !entity.OldArea.ContainsKey(code))
                {
                    if (entity.OldCode == 0)
                        entity.OldCode = code;
                    else if (entity.OldCode2 == 0)
                        entity.OldCode2 = code;
                    else if (entity.OldCode3 == 0)
                        entity.OldCode3 = code;
                    else
                        entity.OldArea.Add(code, fullname);

                    entity.SaveWithoutValid();
                }

                return entity;
            }

            // 直接上级
            TEntity parent = null;
            if (code % 100 != 0) parent = FindByCode(code / 100 * 100);
            // 再上一级
            if (parent == null) parent = FindByCode(code / 10000 * 10000);

            // 上级都找不到，就没有意义了
            if (parent == null) return null;

            // 挂在直接上级的扩展里面
            if (!parent.OldArea.ContainsKey(code))
            {
                parent.OldArea.Add(code, fullname);
                parent.SaveWithoutValid();
            }

            return parent;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1} {2} {3} {4} {5} {6}", Code, OldCode, OldCode2, OldCode3, Name, FriendName, Description);

            //StringBuilder sb = new StringBuilder();
            //sb.AppendFormat("{0} {1}", Code, Name);

            //if (OldCode2 > 0) sb.AppendFormat(" {0}", OldCode2);
            //if (OldCode3 > 0) sb.AppendFormat(" {0}", OldCode3);

            //sb.AppendFormat(" {0}", FriendName);

            //return sb.ToString();
        }
        #endregion

        #region 导入导出
        /// <summary>导入</summary>
        /// <param name="reader"></param>
        public static void Import(StreamReader reader)
        {
            Meta.BeginTrans();
            try
            {
                while (!reader.EndOfStream)
                {
                    String context = reader.ReadLine();
                    if (String.IsNullOrEmpty(context)) break;

                    String[] ss = context.Split(new Char[] { ' ' });

                    TEntity entity = new TEntity();
                    Int32 code = Int32.Parse(ss[0]);
                    entity.Code = code;

                    Int32 oldcode = Int32.Parse(ss[1]);
                    if (code != oldcode) entity.OldCode = oldcode;

                    Int32 oldcode2 = Int32.Parse(ss[2]);
                    if (code != oldcode2) entity.OldCode2 = oldcode2;

                    Int32 oldcode3 = Int32.Parse(ss[3]);
                    if (code != oldcode3) entity.OldCode3 = oldcode3;

                    entity.Name = ss[4];

                    if (ss.Length > 5) entity.Description = ss[5];

                    // 查找父级地区
                    if (code % 10000 == 0)
                        entity.ParentCode = 0;
                    else if (code % 100 == 0)
                        entity.ParentCode = code / 10000;
                    else
                        entity.ParentCode = code / 100;

                    entity.SaveWithoutValid();
                }
                Meta.Commit();
            }
            catch
            {
                Meta.Rollback();
                throw;
            }
        }

        /// <summary>从文本文件导入</summary>
        /// <param name="fileName"></param>
        public static void Import(String fileName = "AreaCode.txt")
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                Import(reader);
            }
        }

        /// <summary>导出</summary>
        /// <param name="writer"></param>
        public static void Export(StreamWriter writer)
        {
            var list = FindAllByName(null, null, _.Code, 0, 0);
            if (list == null || list.Count < 1) return;

            foreach (var item in list)
            {
                writer.Write("{0} {1} {2} {3} {4}", item.Code, item.OldCode, item.OldCode2, item.OldCode3, item.Name);
                if (!String.IsNullOrEmpty(item.Description)) writer.Write(" {0}", item.Description);

                writer.WriteLine();
            }
        }

        /// <summary>导出到文本文件</summary>
        /// <param name="fileName"></param>
        public static void Export(String fileName = "AreaCode.txt")
        {
            using (StreamWriter writer = new StreamWriter(fileName, false))
            {
                Export(writer);
            }
        }
        #endregion
    }
}