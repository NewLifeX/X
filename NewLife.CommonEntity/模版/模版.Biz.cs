/*
 * XCoder v4.5.2011.1108
 * 作者：nnhy/X
 * 时间：2011-11-13 22:43:10
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.DataAccessLayer;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewLife.CommonEntity
{
    /// <summary>模版</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Template : Template<Template> { }

    /// <summary>模版</summary>
    public partial class Template<TEntity> : EntityTree<TEntity> where TEntity : Template<TEntity>, new()
    {
        #region 扩展属性﻿
        [NonSerialized]
        private EntityList<TemplateItem> _TemplateItems;
        /// <summary>该模版所拥有的模版项集合</summary>
        [XmlIgnore]
        public EntityList<TemplateItem> TemplateItems
        {
            get
            {
                if (_TemplateItems == null && ID > 0 && !Dirtys.ContainsKey("TemplateItems"))
                {
                    _TemplateItems = TemplateItem.FindAllByTemplateID(ID);
                    Dirtys["TemplateItems"] = true;
                }
                return _TemplateItems;
            }
            set { _TemplateItems = value; }
        }
        #endregion

        #region 扩展查询﻿
        /// <summary>
        /// 根据父编号、名称查找
        /// </summary>
        /// <param name="parentid">父编号</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByParentIDAndName(Int32 parentid, String name)
        {
            if (Meta.Count >= 1000)
                return Find(new String[] { _.ParentID, _.Name }, new Object[] { parentid, name });
            else // 实体缓存
                return Meta.Cache.Entities.Find(e => e.ParentID == parentid && e.Name == name);
        }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid">用户编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllByUserID(Int32 userid)
        {
            if (Meta.Count >= 1000)
                return FindAll(new String[] { _.UserID }, new Object[] { userid });
            else // 实体缓存
                return Meta.Cache.Entities.FindAll(_.UserID, userid);
        }

        /// <summary>
        /// 根据编号查找
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByID(Int32 id)
        {
            if (Meta.Count >= 1000)
                return Find(new String[] { _.ID }, new Object[] { id });
            else // 实体缓存
                return Meta.Cache.Entities.Find(_.ID, id);
            // 单对象缓存
            //return Meta.SingleCache[id];
        }
        #endregion

        #region 对象操作﻿
        static Template()
        {
            // 检查并增加连接
            if (!DAL.ConnStrs.ContainsKey(Meta.ConnName))
            {
                String path = Runtime.IsWeb ? @"~\App_Data\" : @"Data\";
                // 默认使用SQLite数据库
                DAL.AddConnStr(Meta.ConnName, String.Format("Data Source={0}{1}.db", path, Meta.ConnName), null, "SQLite");
            }
        }

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
            if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(_.Name, _.Name.Description + "不能为空！");

            // 建议先调用基类方法，基类方法会对唯一索引的数据进行验证
            base.Valid(isNew);

            if (isNew && !Dirtys[_.CreateTime])
                CreateTime = DateTime.Now;
            else if (!isNew && !Dirtys[_.LastModify])
                LastModify = DateTime.Now;

            // 获取当前登录用户
            IManageUser user = CommonService.Resolve<IManageProvider>().Current;
            if (user != null && !Dirtys[_.UserID] && !Dirtys[_.UserName])
            {
                if (user.ID is Int32) UserID = (Int32)user.ID;
                UserName = user.ToString();
            }
        }

        /// <summary>
        /// 已重载。删除关联数据
        /// </summary>
        /// <returns></returns>
        protected override int OnDelete()
        {
            if (TemplateItems != null) TemplateItems.Delete();
            if (Childs != null) Childs.Delete();

            return base.OnDelete();
        }

        ///// <summary>
        ///// 首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法
        ///// </summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    base.InitData();

        //    //// InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    //// Meta.Count是快速取得表记录数
        //    //if (Meta.Count > 0) return;

        //    //// 需要注意的是，如果该方法调用了其它实体类的首次数据库操作，目标实体类的数据初始化将会在同一个线程完成
        //    //if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}模版数据……", typeof(TEntity).Name);

        //    //TEntity user = new TEntity();
        //    //user.Name = "admin";
        //    //user.Password = DataHelper.Hash("admin");
        //    //user.DisplayName = "管理员";
        //    //user.RoleID = 1;
        //    //user.IsEnable = true;
        //    //user.Insert();

        //    //if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}模版数据！", typeof(TEntity).Name);
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
        //public static EntityList<TEntity> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
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
        #endregion

        #region 检查并导入模版
        /// <summary>检查并导入模版</summary>
        /// <param name="pid">目标</param>
        /// <param name="asm"></param>
        /// <param name="prefix"></param>
        public static void ImportFromAssembly(Int32 pid, Assembly asm, String prefix)
        {
            Meta.BeginTrans();
            try
            {
                // 默认导入到全局中
                TEntity parent = FindByID(pid);
                if (parent == null)
                {
                    parent = new TEntity();
                    parent.Name = "全局";
                    parent.Save();
                }

                // 内置模版进入字典
                Dictionary<String, List<String>> dic = new Dictionary<String, List<String>>();
                if (asm == null) asm = Assembly.GetCallingAssembly();
                foreach (String item in asm.GetManifestResourceNames())
                {
                    String name = item;
                    // 去掉前缀，注意点号
                    if (!name.StartsWith(prefix)) continue;
                    name = name.Substring(prefix.Length + 1);

                    Int32 p = name.IndexOf(".");
                    String path = name.Substring(0, p);
                    name = name.Substring(p + 1);

                    List<String> list = null;
                    if (!dic.TryGetValue(path, out list))
                    {
                        list = new List<string>();
                        dic.Add(path, list);
                    }

                    list.Add(name);
                }

                // 开始处理模版
                foreach (String path in dic.Keys)
                {
                    List<String> list = dic[path];

                    TEntity entity = FindByParentIDAndName(parent.ID, path);
                    if (entity == null)
                    {
                        entity = new TEntity();
                        entity.Name = path;
                        entity.ParentID = parent.ID;
                        entity.Save();
                    }

                    // 读出模版，添加模版项
                    foreach (String name in list)
                    {
                        TemplateItem ti = TemplateItem.FindByTemplateIDAndName(entity.ID, name);
                        if (ti != null) continue;

                        Stream stream = asm.GetManifestResourceStream(String.Format("{0}.{1}.{2}", prefix, path, name));
                        Byte[] bts = new Byte[stream.Length];
                        stream.Read(bts, 0, bts.Length);

                        ti = new TemplateItem();
                        ti.TemplateID = entity.ID;
                        ti.Name = name;
                        ti["Content"] = Encoding.UTF8.GetString(bts);
                        ti.Save();
                    }
                }

                Meta.Commit();
            }
            catch
            {
                Meta.Rollback();
                throw;
            }
        }
        #endregion
    }
}