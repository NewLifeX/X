/*
 * XCoder v4.3.2011.0920
 * 作者：nnhy/NEWLIFE
 * 时间：2011-10-18 10:51:07
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using NewLife.CommonEntity.Exceptions;
using NewLife.Log;
using NewLife.Security;
using NewLife.Web;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>用户</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class User : User<User> { }

    /// <summary>用户</summary>
    public partial class User<TEntity> : Entity<TEntity>, IManageUser where TEntity : User<TEntity>, new()
    {
        #region 扩展属性﻿
        static HttpState<TEntity> _httpState;
        /// <summary>Http状态，子类可以重新给HttpState赋值，以控制保存Http状态的过程</summary>
        public static HttpState<TEntity> HttpState
        {
            get
            {
                if (_httpState != null) return _httpState;
                _httpState = new HttpState<TEntity>("User");
                _httpState.CookieToEntity = new Converter<HttpCookie, TEntity>(delegate(HttpCookie cookie)
                {
                    if (cookie == null) return null;

                    String user = HttpUtility.UrlDecode(cookie["u"]);
                    String pass = cookie["p"];
                    if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(pass)) return null;

                    try
                    {
                        return Login(user, pass, -1);
                    }
                    catch //(Exception ex)
                    {
                        //WriteLog("登录", user + "登录失败！" + ex.Message);
                        return null;
                    }
                });
                _httpState.EntityToCookie = new Converter<TEntity, HttpCookie>(delegate(TEntity entity)
                {
                    HttpCookie cookie = HttpContext.Current.Response.Cookies[_httpState.Key];
                    if (entity != null)
                    {
                        cookie["u"] = HttpUtility.UrlEncode(entity.Account);
                        cookie["p"] = DataHelper.Hash(entity.Password);
                    }
                    else
                    {
                        cookie.Value = null;
                    }

                    return cookie;
                });

                return _httpState;
            }
            set { _httpState = value; }
        }

        /// <summary>当前登录用户</summary>
        public static TEntity Current
        {
            get
            {
                TEntity entity = HttpState.Current;
                if (HttpState.Get(null, null) != entity) HttpState.Current = entity;
                return entity;
            }
            set
            {
                HttpState.Current = value;
                //Thread.CurrentPrincipal = (IPrincipal)value;
            }
        }

        /// <summary>当前登录用户，不带自动登录</summary>
        public static TEntity CurrentNoAutoLogin
        {
            get { return HttpState.Get(null, null); }
            //set { HttpState.Current = value; }
        }
        #endregion

        #region 扩展查询﻿
        /// <summary>根据主键查询一个用户实体对象用于表单编辑</summary>
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

        /// <summary>根据账号查找</summary>
        /// <param name="account">账号</param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static TEntity FindByAccount(String account)
        {
            if (Meta.Count >= 1000)
                return Find(new String[] { _.Account }, new Object[] { account });
            else // 实体缓存
                return Meta.Cache.Entities.Find(_.Account, account);
            // 单对象缓存
            //return Meta.SingleCache[account];
        }

        /// <summary>根据编号查找</summary>
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
        static User()
        {
            // 用于引发基类的静态构造函数
            TEntity entity = new TEntity();
        }

        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}用户数据……", typeof(TEntity).Name);

            TEntity user = new TEntity();
            user.Account = "admin";
            user.Password = DataHelper.Hash("admin");
            user.IsAdmin = true;
            user.IsEnable = true;
            user.Insert();

            if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}用户数据！", typeof(TEntity).Name);
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

        ///// <summary>
        ///// 验证数据，通过抛出异常的方式提示验证失败。
        ///// </summary>
        ///// <param name="isNew"></param>
        //public override void Valid(Boolean isNew)
        //{
        //    // 建议先调用基类方法，基类方法会对唯一索引的数据进行验证
        //    base.Valid(isNew);

        //    // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
        //    if (String.IsNullOrEmpty(_.Name)) throw new ArgumentNullException(_.Name, _.Name.Description + "无效！");
        //    if (!isNew && ID < 1) throw new ArgumentOutOfRangeException(_.ID, _.ID.Description + "必须大于0！");

        //    // 在新插入数据或者修改了指定字段时进行唯一性验证，CheckExist内部抛出参数异常
        //    if (isNew || Dirtys[_.Name]) CheckExist(_.Name);
        //    if (isNew || Dirtys[_.Name] || Dirtys[_.DbType]) CheckExist(_.Name, _.DbType);
        //    if ((isNew || Dirtys[_.Name]) && Exist(_.Name)) throw new ArgumentException(_.Name, "值为" + Name + "的" + _.Name.Description + "已存在！");
        //}


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
        /// <summary>登录</summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static TEntity Login(String account, String password)
        {
            if (String.IsNullOrEmpty(account)) throw new ArgumentNullException("account");
            //if (String.IsNullOrEmpty(password)) throw new ArgumentNullException("password");

            return Login(account, password, 1);
        }

        static TEntity Login(String username, String password, Int32 hashTimes)
        {
            if (String.IsNullOrEmpty(username)) return null;

            var user = FindByAccount(username);
            if (user == null) return null;

            // 如果账户被禁用，并且有一个以上账号时，才有效
            if (!user.IsEnable && Meta.Count > 1) throw new EntityException("账户已被禁用！");

            // 数据库为空密码，任何密码均可登录
            if (!String.IsNullOrEmpty(user.Password))
            {
                if (hashTimes > 0)
                {
                    String p = password;
                    if (!String.IsNullOrEmpty(p))
                    {
                        for (int i = 0; i < hashTimes; i++)
                        {
                            p = DataHelper.Hash(p);
                        }
                    }
                    if (!String.Equals(user.Password, p, StringComparison.OrdinalIgnoreCase)) throw new EntityException("密码不正确！");
                }
                else
                {
                    String p = user.Password;
                    for (int i = 0; i > hashTimes; i--)
                    {
                        p = DataHelper.Hash(p);
                    }
                    if (!String.Equals(p, password, StringComparison.OrdinalIgnoreCase)) throw new EntityException("密码不正确！");
                }
            }

            Current = user;

            return user;
        }
        #endregion

        #region IManageUser 成员
        /// <summary>编号</summary>
        object IManageUser.Uid { get { return ID; } }

        ///// <summary>账号</summary>
        //string IManageUser.Account { get { return Account; } set { Account = value; } }

        ///// <summary>密码</summary>
        //string IManageUser.Password { get { return Password; } set { Password = value; } }

        [NonSerialized]
        IDictionary<String, Object> _Properties;
        /// <summary>属性集合</summary>
        IDictionary<String, Object> IManageUser.Properties
        {
            get
            {
                if (_Properties == null)
                {
                    var dic = new Dictionary<String, Object>();
                    foreach (var item in Meta.FieldNames)
                    {
                        dic[item] = this[item];
                    }
                    foreach (var item in Extends)
                    {
                        dic[item.Key] = item.Value;
                    }

                    _Properties = dic;
                }
                return _Properties;
            }
        }
        #endregion
    }

    partial interface IUser
    {
        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();
    }
}