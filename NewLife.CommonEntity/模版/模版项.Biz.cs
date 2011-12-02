/*
 * XCoder v4.5.2011.1108
 * 作者：nnhy/X
 * 时间：2011-11-13 22:43:14
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>模版项</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class TemplateItem : TemplateItem<TemplateItem> { }

    /// <summary>模版项</summary>
    public partial class TemplateItem<TEntity> : Entity<TEntity> where TEntity : TemplateItem<TEntity>, new()
    {
        #region 扩展属性﻿
        [NonSerialized]
        private EntityList<TemplateContent> _TemplateContents;
        /// <summary>该模版项所拥有的模版内容集合</summary>
        [XmlIgnore]
        public EntityList<TemplateContent> TemplateContents
        {
            get
            {
                if (_TemplateContents == null && ID > 0 && !Dirtys.ContainsKey("TemplateContents"))
                {
                    _TemplateContents = TemplateContent.FindAllByTemplateItemID(ID);
                    Dirtys["TemplateContents"] = true;
                }
                return _TemplateContents;
            }
            set { _TemplateContents = value; }
        }

        [NonSerialized]
        private Template _Template;
        /// <summary>该模版项所对应的模版</summary>
        [XmlIgnore]
        public Template Template
        {
            get
            {
                if (_Template == null && TemplateID > 0 && !Dirtys.ContainsKey("Template"))
                {
                    _Template = Template.FindByID(TemplateID);
                    Dirtys["Template"] = true;
                }
                return _Template;
            }
            set { _Template = value; }
        }

        /// <summary>该模版项所对应的模版名称</summary>
        [XmlIgnore]
        public String TemplateName { get { return Template != null ? Template.FullPath : null; } }

        [NonSerialized]
        private TemplateContent _LastTemplateContent;
        /// <summary>该模版项所对应的最后一个模版内容</summary>
        [XmlIgnore]
        public TemplateContent LastTemplateContent
        {
            get
            {
                if (_LastTemplateContent == null && TemplateID > 0 && !Dirtys.ContainsKey("LastTemplateContent"))
                {
                    _LastTemplateContent = TemplateContent.FindLastByTemplateItemID(ID);
                    Dirtys["LastTemplateContent"] = true;
                }
                return _LastTemplateContent;
            }
            set { _LastTemplateContent = value; }
        }

        /// <summary>该模版项所对应的模版内容</summary>
        [XmlIgnore]
        public String Content { get { return LastTemplateContent != null ? LastTemplateContent.Content : null; } }

        /// <summary>该模版项所对应的模版版本</summary>
        [XmlIgnore]
        public Int32 Version { get { return LastTemplateContent != null ? LastTemplateContent.Version : 0; } }
        #endregion

        #region 扩展查询﻿
        /// <summary>
        /// 根据模版、名称查找
        /// </summary>
        /// <param name="templateid">模版</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByTemplateIDAndName(Int32 templateid, String name)
        {
            if (Meta.Count >= 1000)
                return Find(new String[] { _.TemplateID, _.Name }, new Object[] { templateid, name });
            else // 实体缓存
                return Meta.Cache.Entities.Find(e => e.TemplateID == templateid && e.Name == name);
        }

        /// <summary>
        /// 根据模版查找
        /// </summary>
        /// <param name="templateid">模版</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static EntityList<TEntity> FindAllByTemplateID(Int32 templateid)
        {
            if (Meta.Count >= 1000)
                return FindAll(new String[] { _.TemplateID }, new Object[] { templateid });
            else // 实体缓存
                return Meta.Cache.Entities.FindAll(_.TemplateID, templateid);
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
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="forEdit"></param>
        /// <returns></returns>
        protected override TEntity CreateInstance(bool forEdit = false)
        {
            TEntity entity = base.CreateInstance(forEdit);
            if (forEdit) entity.Kind = "XTemplate";
            return entity;
        }

        ///// <summary>
        ///// 已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert
        ///// </summary>
        ///// <returns></returns>
        //public override Int32 Insert()
        //{
        //    return base.Insert();
        //}

        /// <summary>
        /// 已重载。在事务保护范围内处理业务，位于Valid之后
        /// </summary>
        /// <returns></returns>
        protected override Int32 OnInsert()
        {
            Int32 rs = base.OnInsert();

            TemplateContent tc = new TemplateContent();
            tc.TemplateItemID = ID;
            // 数据放在扩展里面
            tc.Content = (String)Extends["Content"];
            tc.Insert();

            return rs;
        }

        /// <summary>
        /// 已重载
        /// </summary>
        /// <returns></returns>
        protected override int OnUpdate()
        {
            // 数据放在扩展里面
            String content = (String)Extends["Content"];

            // 如果扩展里面的内容跟最后内容不一致，则更新
            if (Dirtys["Content"] && (LastTemplateContent == null || "" + content != "" + LastTemplateContent.Content))
            {
                TemplateContent tc = new TemplateContent();
                tc.TemplateItemID = ID;
                tc.Content = content;
                tc.Insert();
            }

            return base.OnUpdate();
        }

        /// <summary>
        /// 验证数据，通过抛出异常的方式提示验证失败。
        /// </summary>
        /// <param name="isNew"></param>
        public override void Valid(Boolean isNew)
        {
            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (String.IsNullOrEmpty(_.Name)) throw new ArgumentNullException(_.Name, _.Name.Description + "无效！");
            if (!isNew && ID < 1) throw new ArgumentOutOfRangeException(_.ID, _.ID.Description + "必须大于0！");

            // 建议先调用基类方法，基类方法会对唯一索引的数据进行验证
            base.Valid(isNew);

            if (!Dirtys[_.Kind]) Kind = "XTemplate";
        }

        /// <summary>
        /// 已重载。删除关联数据
        /// </summary>
        /// <returns></returns>
        protected override int OnDelete()
        {
            if (TemplateContents != null) TemplateContents.Delete();

            return base.OnDelete();
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
        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}模版项数据……", typeof(TEntity).Name);

        //    TEntity user = new TEntity();
        //    user.Name = "admin";
        //    user.Password = DataHelper.Hash("admin");
        //    user.DisplayName = "管理员";
        //    user.RoleID = 1;
        //    user.IsEnable = true;
        //    user.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}模版项数据！", typeof(TEntity).Name);
        //}
        #endregion

        #region 高级查询
        /// <summary>
        /// 查询满足条件的记录集，分页、排序
        /// </summary>
        /// <param name="templateid">模版编号</param>
        /// <param name="key">关键字</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体集</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public static EntityList<TEntity> Search(Int32 templateid, String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindAll(SearchWhere(templateid, key), orderClause, null, startRowIndex, maximumRows);
        }

        /// <summary>
        /// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
        /// </summary>
        /// <param name="templateid">模版编号</param>
        /// <param name="key">关键字</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>记录数</returns>
        public static Int32 SearchCount(Int32 templateid, String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCount(SearchWhere(templateid, key), null, null, 0, 0);
        }

        /// <summary>
        /// 构造搜索条件
        /// </summary>
        /// <param name="templateid">模版编号</param>
        /// <param name="key">关键字</param>
        /// <returns></returns>
        private static String SearchWhere(Int32 templateid, String key)
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

            exp = exp & templateid > 0 & _.TemplateID.Equal(templateid);

            return exp;
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        /// <summary>根据路径创建，自动识别或创建父级</summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static TEntity CreateItem(String path)
        {
            if (String.IsNullOrEmpty(path)) return null;

            String[] ss = path.Split(@"\");
            // 必须至少包含两级，目录和模版项
            if (ss.Length < 2) return null;

            // 先解决父级
            Template parent = null;
            if (ss.Length <= 1)
                parent = Template.Root;
            else
                parent = Template.Create(String.Join(@"\", ss, 0, ss.Length - 1));

            // 如果找不到父级，就有问题了
            if (parent == null) return null;

            String name = ss[ss.Length - 1];
            TEntity entity = FindByTemplateIDAndName(parent.ID, name);
            if (entity != null) return entity;

            entity = new TEntity();
            entity.TemplateID = parent.ID;
            entity.Name = name;
            entity.Save();

            return entity;
        }

        /// <summary>复制子项</summary>
        /// <param name="src"></param>
        public void CopyContent(TEntity src)
        {
            Extends["Content"] = src.Content;
        }
        #endregion
    }
}