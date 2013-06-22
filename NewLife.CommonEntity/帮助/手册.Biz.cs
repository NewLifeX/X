/*
 * XCoder v4.8.4548.28140
 * 作者：nnhy/NEWLIFE
 * 时间：2012-06-18 11:52:45
 * 版权：版权所有 (C) 新生命开发团队 2012
*/
﻿using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using XCode;

namespace NewLife.CommonEntity
{
    // 禁止直接使用，要求各个模块自己实现
    ///// <summary>手册</summary>
    //[ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    //public class Manual : Manual<Manual> { }

    /// <summary>手册</summary>
    /// <remarks>
    /// 各个模块应该实现自己的手册类，指定专用的数据库连接。
    /// </remarks>
    public partial class Manual<TEntity> : Entity<TEntity> where TEntity : Manual<TEntity>, new()
    {
        #region 对象操作﻿
        static Manual()
        {
            // 用于引发基类的静态构造函数，所有层次的泛型实体类都应该有一个
            TEntity entity = new TEntity();
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew"></param>
        public override void Valid(Boolean isNew)
        {
            // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
            if (String.IsNullOrEmpty(Summary) && String.IsNullOrEmpty(Content))
                throw new ArgumentNullException(_.Content, _.Summary.DisplayName + "与" + _.Content.DisplayName + "不能同时为空！");
            //if (!isNew && ID < 1) throw new ArgumentOutOfRangeException(_.ID, _.ID.DisplayName + "必须大于0！");

            // 建议先调用基类方法，基类方法会对唯一索引的数据进行验证
            base.Valid(isNew);

            // 在新插入数据或者修改了指定字段时进行唯一性验证，CheckExist内部抛出参数异常
            //if (isNew || Dirtys[_.Name]) CheckExist(_.Name);

            // 处理当前已登录用户信息
            if (!Dirtys[_.UserName] && ManageProvider.Provider.Current != null) UserName = ManageProvider.Provider.Current.Account;
            if (isNew && !Dirtys[_.CreateTime]) CreateTime = DateTime.Now;
            if (!Dirtys[_.UpdateTime]) UpdateTime = DateTime.Now;

            if (!Dirtys[_.Summary] && !String.IsNullOrEmpty(Content)) Summary = Content.Substring(0, 100);
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    base.InitData();

        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    // Meta.Count是快速取得表记录数
        //    if (Meta.Count > 0) return;

        //    // 需要注意的是，如果该方法调用了其它实体类的首次数据库操作，目标实体类的数据初始化将会在同一个线程完成
        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}手册数据……", typeof(TEntity).Name);

        //    var entity = new Manual();
        //    entity.Url = "abc";
        //    entity.Summary = "abc";
        //    entity.Content = "abc";
        //    entity.UserName = "abc";
        //    entity.CreateTime = DateTime.Now;
        //    entity.UpdateTime = DateTime.Now;
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}手册数据！", typeof(TEntity).Name);
        //}


        ///// <summary>已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert</summary>
        ///// <returns></returns>
        //public override Int32 Insert()
        //{
        //    return base.Insert();
        //}

        ///// <summary>已重载。在事务保护范围内处理业务，位于Valid之后</summary>
        ///// <returns></returns>
        //protected override Int32 OnInsert()
        //{
        //    return base.OnInsert();
        //}
        #endregion

        #region 扩展属性﻿
        #endregion

        #region 扩展查询﻿
        /// <summary>根据资源查找</summary>
        /// <param name="url">资源</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByUrl(String url)
        {
            if (Meta.Count >= 1000)
                return Find(_.Url, url);
            else // 实体缓存
                return Meta.Cache.Entities.Find(_.Url, url);
            // 单对象缓存
            //return Meta.SingleCache[url];
        }

        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByID(Int32 id)
        {
            if (Meta.Count >= 1000)
                return Find(_.ID, id);
            else // 实体缓存
                return Meta.Cache.Entities.Find(_.ID, id);
            // 单对象缓存
            //return Meta.SingleCache[id];
        }
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

        /// <summary>构造搜索条件</summary>
        /// <param name="key">关键字</param>
        /// <returns></returns>
        private static String SearchWhere(String key)
        {
            // WhereExpression重载&和|运算符，作为And和Or的替代
            var exp = SearchWhereByKeys(key);

            // 以下仅为演示，2、3行是同一个意思的不同写法，Field（继承自FieldItem）重载了==、!=、>、<、>=、<=等运算符（第4行）
            //exp &= _.Name == "testName"
            //    & !String.IsNullOrEmpty(key) & _.Name == key
            //    .AndIf(!String.IsNullOrEmpty(key), _.Name == key)
            //    | _.ID > 0;

            return exp;
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        /// <summary>当前页面默认帮助</summary>
        public static TEntity Current { get { return FindInPage(null); } }

        /// <summary>在当前页面上查找指定序号的帮助</summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static TEntity FindInPage(String tag)
        {
            if (HttpContext.Current == null) return null;

            var url = HttpContext.Current.Request.Url;
            if (url == null) return null;

            // 取最后三级
            var url2 = url.AbsolutePath;
            var ss = url2.Split("/");
            if (ss == null || ss.Length < 3)
            {
                url2 = "/" + String.Join("/", ss, ss.Length - 3 - 1, 3);
            }

            if (!String.IsNullOrEmpty(tag)) url2 += "#" + tag;

            return FindByUrl(url2);
        }

        /// <summary>绑定到控件</summary>
        /// <param name="control"></param>
        /// <param name="tag"></param>
        public static void Bind(Control control, String tag)
        {
            var entity = FindInPage(tag);
            if (entity == null) return;

            if (control is Label)
            {
                (control as Label).Text = entity.Summary;
            }
            else if (control is Literal)
            {
                (control as Label).Text = entity.Summary;
            }
            else if (control is WebControl)
            {
                (control as WebControl).ToolTip = entity.Summary;
            }
        }
        #endregion
    }
}