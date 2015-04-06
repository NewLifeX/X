using System;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using NewLife.Log;
using NewLife.Security;
using NewLife.Web;

namespace XCode.Membership
{
    /// <summary>管理员</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class User : User<User> { }

    /// <summary>管理员</summary>
    /// <remarks>
    /// 基础实体类应该是只有一个泛型参数的，需要用到别的类型时，可以继承一个，也可以通过虚拟重载等手段让基类实现
    /// </remarks>
    /// <typeparam name="TEntity">管理员类型</typeparam>
    public abstract partial class User<TEntity> : EntityBase<TEntity>, IUser, IManageUser//, IPrincipal//, IIdentity
        where TEntity : User<TEntity>, new()
    {
        #region 对象操作
        static User()
        {
            // 用于引发基类的静态构造函数
            var entity = new TEntity();

            //!!! 曾经这里导致产生死锁
            // 这里是静态构造函数，访问Factory引发EntityFactory.CreateOperate，
            // 里面的EnsureInit会等待实体类实例化完成，实体类的静态构造函数还卡在这里
            // 不过这不是理由，同一个线程遇到同一个锁不会堵塞
            // 发生死锁的可能性是这里引发EnsureInit，而另一个线程提前引发EnsureInit拿到锁
            Meta.Factory.AdditionalFields.Add(__.Logins);
        }

        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}管理员数据……", typeof(TEntity).Name);

            var user = new TEntity();
            user.Name = "admin";
            user.Password = DataHelper.Hash("admin");
            user.DisplayName = "管理员";
            user.RoleID = 1;
            user.Roles = "管理员";
            user.Enable = true;
            user.Insert();

            if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}管理员数据！", typeof(TEntity).Name);
        }

        /// <summary>验证</summary>
        /// <param name="isNew"></param>
        public override void Valid(bool isNew)
        {
            base.Valid(isNew);

            if (String.IsNullOrEmpty(Name)) throw new ArgumentNullException(__.Name, "用户名不能为空！");
            if (RoleID < 1) throw new ArgumentNullException(__.RoleID, "没有指定角色！");

            if (isNew)
            {
                if (!String.IsNullOrEmpty(Password) && Password.Length != 32) Password = Password.MD5();
            }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override int Delete()
        {
            String name = Name;
            if (String.IsNullOrEmpty(name))
            {
                var entity = Find("ID", ID);
                if (entity != null) name = entity.Name;
            }
            WriteLog("删除", name);

            return base.Delete();
        }
        #endregion

        #region 扩展属性
        /// <summary>当前登录用户</summary>
        public static TEntity Current
        {
            get
            {
                var key = "Admin";

                if (HttpContext.Current == null) return null;
                var Session = HttpContext.Current.Session;
                if (Session == null) return null;

                // 从Session中获取
                var entity = Session[key] as TEntity;
                if (entity != null) return entity;

                // 设置一个陷阱，避免重复计算Cookie
                if (Session[key] != null) return null;

                // 从Cookie中获取
                entity = GetCookie(key);
                if (entity != null)
                    Session[key] = entity;
                else
                    Session[key] = "1";

                return entity;
            }
            set
            {
                var key = "Admin";
                var Session = HttpContext.Current.Session;

                // 特殊处理注销
                if (value == null)
                {
                    var entity = Current;
                    if (entity != null) WriteLog("注销", entity.Name);

                    // 修改Session
                    if (Session != null) Session.Remove(key);
                }
                else
                {
                    // 修改Session
                    if (Session != null) Session[key] = value;
                }

                // 修改Cookie
                SetCookie(key, value);
            }
        }

        ///// <summary>当前登录用户，不带自动登录</summary>
        //public static TEntity CurrentNoAutoLogin { get { return HttpState.Get(null, null); } }

        /// <summary>友好名字</summary>
        public virtual String FriendName { get { return String.IsNullOrEmpty(DisplayName) ? Name : DisplayName; } }
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (Meta.Count >= 1000)
                return Find(__.ID, id);
            else // 实体缓存
                return Meta.Cache.Entities.Find(__.ID, id);
        }

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindByName(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            if (Meta.Count >= 1000)
                return Find(__.Name, name);
            else // 实体缓存
                return Meta.Cache.Entities.Find(__.Name, name);
        }

        /// <summary>根据邮箱地址查找</summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        public static TEntity FindByMail(String mail)
        {
            if (Meta.Count >= 1000)
                return Find(__.Mail, mail);
            else // 实体缓存
                return Meta.Cache.Entities.Find(__.Mail, mail);
        }

        /// <summary>根据手机号码查找</summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public static TEntity FindByPhone(String phone)
        {
            if (Meta.Count >= 1000)
                return Find(__.Phone, phone);
            else // 实体缓存
                return Meta.Cache.Entities.Find(__.Phone, phone);
        }

        /// <summary>根据唯一代码查找</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static TEntity FindByCode(String code)
        {
            if (Meta.Count >= 1000)
                return Find(__.Code, code);
            else // 实体缓存
                return Meta.Cache.Entities.Find(__.Code, code);
        }

        /// <summary>查询满足条件的记录集，分页、排序</summary>
        /// <param name="key">关键字</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体集</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public static EntityList<TEntity> Search(String key, Int32 roleId, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindAll(SearchWhere(key, roleId), orderClause, null, startRowIndex, maximumRows);
        }

        /// <summary>查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一</summary>
        /// <param name="key">关键字</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>记录数</returns>
        public static Int32 SearchCount(String key, Int32 roleId, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCount(SearchWhere(key, roleId), null, null, 0, 0);
        }

        /// <summary>查询满足条件的记录集，分页、排序</summary>
        /// <param name="key">关键字</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="isEnable">是否启用</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>实体集</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public static EntityList<TEntity> Search(String key, Int32 roleId, Boolean? isEnable, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindAll(SearchWhere(key, roleId, isEnable), orderClause, null, startRowIndex, maximumRows);
        }

        /// <summary>查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一</summary>
        /// <param name="key">关键字</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="isEnable">是否启用</param>
        /// <param name="orderClause">排序，不带Order By</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>记录数</returns>
        public static Int32 SearchCount(String key, Int32 roleId, Boolean? isEnable, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        {
            return FindCount(SearchWhere(key, roleId, isEnable), null, null, 0, 0);
        }

        /// <summary>构造搜索条件</summary>
        /// <param name="key">关键字</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="isEnable">是否启用</param>
        /// <returns></returns>
        private static String SearchWhere(String key, Int32 roleId, Boolean? isEnable = null)
        {
            //var exp = new WhereExpression();

            //// SearchWhereByKeys系列方法用于构建针对字符串字段的模糊搜索
            //if (!String.IsNullOrEmpty(key)) exp &= SearchWhereByKey(key);

            //if (roleId > 0) exp &= _.RoleID == roleId;
            //if (isEnable != null) exp &= _.IsEnable == isEnable.Value;

            //var where = exp.ToString();
            //XTrace.WriteLine(where);

            var exp2 = SearchWhereByKey(key) & _.RoleID == roleId & _.Enable == isEnable;
            //var where2 = exp2.SetStrict().GetString();
            //XTrace.WriteLine(where2);

            return exp2.SetStrict();
        }
        #endregion

        #region 扩展操作
        /// <summary>已重载。显示友好名字</summary>
        /// <returns></returns>
        public override string ToString() { return FriendName; }
        #endregion

        #region 业务
        /// <summary>登录</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="rememberme">是否记住密码</param>
        /// <returns></returns>
        public static TEntity Login(String username, String password, Boolean rememberme = false)
        {
            if (String.IsNullOrEmpty(username)) throw new ArgumentNullException("username");
            //if (String.IsNullOrEmpty(password)) throw new ArgumentNullException("password");

            try
            {
                var user = Login(username, password, 1);
                if (rememberme && user != null)
                {
                    var cookie = HttpContext.Current.Response.Cookies["Admin"];
                    if (cookie != null) cookie.Expires = DateTime.Now.Date.AddYears(1);
                }
                return user;
            }
            catch (Exception ex)
            {
                WriteLog("登录", username + "登录失败！" + ex.Message);
                throw;
            }
        }

        static TEntity Login(String username, String password, Int32 hashTimes)
        {
            if (String.IsNullOrEmpty(username)) throw new ArgumentNullException("username", "该帐号不存在！");

            //过滤帐号中的空格，防止出现无操作无法登录的情况
            var account = username.Trim();
            var user = FindByName(account);
            if (user == null) throw new EntityException("帐号{0}不存在！", account);

            if (!user.Enable) throw new EntityException("账号{0}被禁用！", account);

            // 数据库为空密码，任何密码均可登录
            if (!String.IsNullOrEmpty(user.Password))
            {
                if (hashTimes > 0)
                {
                    var p = password;
                    if (!String.IsNullOrEmpty(p))
                    {
                        for (int i = 0; i < hashTimes; i++)
                        {
                            p = DataHelper.Hash(p);
                        }
                    }
                    if (!p.EqualIgnoreCase(user.Password)) throw new EntityException("密码不正确！");
                }
                else
                {
                    var p = user.Password;
                    for (int i = 0; i > hashTimes; i--)
                    {
                        p = DataHelper.Hash(p);
                    }
                    if (!p.EqualIgnoreCase(password)) throw new EntityException("密码不正确！");
                }
            }

            user.SaveLoginInfo();

            Current = user;

            if (hashTimes == -1)
                WriteLog("自动登录", username);
            else
                WriteLog("登录", username);

            return user;
        }

        /// <summary>保存登录信息</summary>
        /// <returns></returns>
        protected virtual Int32 SaveLoginInfo()
        {
            Logins++;
            LastLogin = DateTime.Now;
            var ip = WebHelper.UserHost;
            if (!String.IsNullOrEmpty(ip)) LastLoginIP = ip;

            return Update();
        }

        /// <summary>注销</summary>
        public virtual void Logout()
        {
            Current = null;
            //Thread.CurrentPrincipal = null;
        }

        /// <summary>注册用户。默认写日志后Insert，注册仅保存基本信息，需要扩展的同学请重载</summary>
        public virtual void Register()
        {
            if (RoleID == 0)
            {
                // 填写角色。最后一个普通角色，如果没有，再管理员角色
                var eop = ManageProvider.GetFactory<IRole>();
                var list = eop.FindAllWithCache().Cast<IRole>();
                var role = list.LastOrDefault(e => !e.IsSystem);
                if (role == null) role = list.LastOrDefault();

                RoleID = role.ID;
                Roles = role.Name;
            }

            Insert();
        }

        static Boolean _isInGetCookie;
        static TEntity GetCookie(String key)
        {
            if (_isInGetCookie) return null;

            var cookie = HttpContext.Current.Request.Cookies[key];
            if (cookie == null) return null;

            var user = HttpUtility.UrlDecode(cookie["u"]);
            var pass = cookie["p"];
            if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(pass)) return null;

            _isInGetCookie = true;
            try
            {
                return Login(user, pass, -1);
            }
            catch (DbException ex)
            {
                XTrace.WriteLine("{0}登录失败！{1}", user, ex);
                return null;
            }
            catch (Exception ex)
            {
                WriteLog("登录", user + "登录失败！" + ex.Message);
                return null;
            }
            finally { _isInGetCookie = false; }
        }

        static void SetCookie(String key, TEntity entity)
        {
            var context = HttpContext.Current;
            var res = context.Response;
            var reqcookie = context.Request.Cookies[key];
            if (entity != null)
            {
                var user = HttpUtility.UrlEncode(entity.Name);
                var pass = !String.IsNullOrEmpty(entity.Password) ? DataHelper.Hash(entity.Password) : null;
                if (reqcookie == null || user != reqcookie["u"] || pass != reqcookie["p"])
                {
                    // 只有需要写入Cookie时才设置，否则会清空原来的非会话Cookie
                    var cookie = res.Cookies[key];
                    cookie["u"] = user;
                    cookie["p"] = pass;
                }
            }
            else
            {
                var cookie = res.Cookies[key];
                cookie.Value = null;
                cookie.Expires = DateTime.Now.AddYears(-1);
                //HttpContext.Current.Response.Cookies.Remove(key);
            }
        }

        /// <summary>拥有指定菜单的权限</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual Boolean HasMenu(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            var menu = FindPermissionMenu(name);
            if (menu == null) return false;

            return Acquire(menu.ID, PermissionFlags.None);
        }

        /// <summary>申请指定菜单指定操作的权限</summary>
        /// <param name="name">名称</param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public virtual Boolean Acquire(String name, PermissionFlags flag)
        {
            var menu = FindPermissionMenu(name);
            if (menu == null) return false;

            return Acquire(menu.ID, flag);
        }

        /// <summary>申请指定菜单指定操作的权限</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual Boolean Acquire(String name)
        {
            return Acquire(name, PermissionFlags.None);
        }

        /// <summary>申请指定菜单指定操作的权限</summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public virtual Boolean Acquire(Int32 menuID, PermissionFlags flag)
        {
            if (menuID <= 0) throw new ArgumentNullException("menuID");

            var entity = (this as IUser).Role;
            if (entity == null) return false;

            // 申请权限
            //return entity.Acquire(menuID, flag);
            return true;
        }
        #endregion

        #region 权限日志
        /// <summary>角色</summary>
        /// <remarks>扩展属性不缓存空对象，一般来说，每个管理员都有对应的角色，如果没有，可能是在初始化</remarks>
        [XmlIgnore]
        public virtual IRole Role
        {
            get
            {
                if (RoleID <= 0) return null;

                var role = ManageProvider.Get<IRole>();

                return role.FindByID(RoleID);
            }
        }

        /// <summary>角色名</summary>
        public virtual String RoleName { get { return Role == null ? null : Role.Name; } set { } }

        /// <summary>根据权限名（权限路径）找到权限菜单实体</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public IMenu FindPermissionMenu(string name)
        {
            var factory = ManageProvider.Get<IMenu>();
            // 优先使用当前页，除非当前页与权限名不同
            var entity = factory.Current;
            if (entity != null && entity.Permission == name) return entity;

            // 根据权限名找
            return factory.FindForPerssion(name);
        }
        #endregion

        #region IManageUser 成员
        /// <summary>编号</summary>
        object IManageUser.Uid { get { return ID; } }

        /// <summary>密码</summary>
        string IManageUser.Password { get { return Password; } set { Password = value; } }

        /// <summary>是否管理员</summary>
        Boolean IManageUser.IsAdmin { get { return RoleName == "管理员" || RoleName == "超级管理员"; } set { } }
        #endregion
    }

    public partial interface IUser
    {
        /// <summary>友好名字</summary>
        String FriendName { get; }

        /// <summary>角色</summary>
        IRole Role { get; /*set;*/ }

        /// <summary>角色名</summary>
        String RoleName { get; set; }

        /// <summary>根据权限名（权限路径）找到权限菜单实体</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        IMenu FindPermissionMenu(String name);

        /// <summary>申请指定菜单指定操作的权限</summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        Boolean Acquire(Int32 menuID, PermissionFlags flag);

        /// <summary>申请指定菜单指定操作的权限</summary>
        /// <param name="name">名称</param>
        /// <param name="flag"></param>
        /// <returns></returns>
        Boolean Acquire(String name, PermissionFlags flag);

        /// <summary>申请指定菜单指定操作的权限</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        Boolean Acquire(String name);

        /// <summary>注销</summary>
        void Logout();

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();
    }
}