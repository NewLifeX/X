using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Log;
using NewLife.Model;
using NewLife.Web;

namespace XCode.Membership
{
    /// <summary>性别</summary>
    public enum SexKinds
    {
        /// <summary>未知</summary>
        未知 = 0,

        /// <summary>男</summary>
        男 = 1,

        /// <summary>女</summary>
        女 = 2
    }

    /// <summary>管理员</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class UserX : User<UserX> { }

    /// <summary>管理员</summary>
    /// <remarks>
    /// 基础实体类应该是只有一个泛型参数的，需要用到别的类型时，可以继承一个，也可以通过虚拟重载等手段让基类实现
    /// </remarks>
    /// <typeparam name="TEntity">管理员类型</typeparam>
    public abstract partial class User<TEntity> : LogEntity<TEntity>, IUser, IAuthUser, IIdentity
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

            // 单对象缓存
            var sc = Meta.SingleCache;
            sc.FindSlaveKeyMethod = k => Find(__.Name, k);
            sc.GetSlaveKeyMethod = e => e.Name;
        }

        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal protected override void InitData()
        {
            base.InitData();

            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}用户数据……", typeof(TEntity).Name);

            Add("admin", null, 1, "管理员");
            //Add("poweruser", null, 2, "高级用户");
            //Add("user", null, 3, "普通用户");

            if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}用户数据！", typeof(TEntity).Name);
        }

        /// <summary>验证</summary>
        /// <param name="isNew"></param>
        public override void Valid(Boolean isNew)
        {
            base.Valid(isNew);

            if (Name.IsNullOrEmpty()) throw new ArgumentNullException(__.Name, "用户名不能为空！");
            //if (RoleID < 1) throw new ArgumentNullException(__.RoleID, "没有指定角色！");

            var pass = Password;
            if (isNew)
            {
                if (!pass.IsNullOrEmpty() && pass.Length != 32) Password = pass.MD5();
            }
            else
            {
                // 编辑修改密码
                if (IsDirty(__.Password))
                {
                    if (!pass.IsNullOrEmpty())
                        Password = pass.MD5();
                    else
                        Dirtys[__.Password] = false;
                }
            }

            // 重新整理角色
            var ids = GetRoleIDs();
            if (ids.Length > 0)
            {
                RoleID = ids[0];
                var str = ids.Skip(1).Join();
                if (!str.IsNullOrEmpty()) str = "," + str + ",";
                RoleIDs = str;
            }
        }

        /// <summary>删除用户</summary>
        /// <returns></returns>
        protected override Int32 OnDelete()
        {
            if (Meta.Count <= 1 && FindCount() <= 1) throw new Exception("必须保留至少一个可用账号！");

            return base.OnDelete();
        }
        #endregion

        #region 扩展属性
        /// <summary>当前登录用户</summary>
        [Obsolete]
        public static TEntity Current
        {
            get
            {
#if !__CORE__
                var key = "Admin";
                var ss = HttpContext.Current?.Session;
                if (ss == null) return null;
                var ms = HttpContext.Current.Items;

                // 从Session中获取
                return ss[key] as TEntity;
                //if (ss[key] is TEntity entity) return entity;

                //// 设置一个陷阱，避免重复计算Cookie
                //if (ms[key] != null) return null;

                //// 从Cookie中获取
                //entity = GetCookie(key);
                //if (entity != null)
                //    ss[key] = entity;
                //else
                //    ms[key] = "1";

                //return entity;
#else
                return null;
#endif
            }
            set
            {
#if !__CORE__
                var key = "Admin";
                var ss = HttpContext.Current?.Session;
                if (ss == null) return;

                // 特殊处理注销
                if (value == null)
                {
                    if (ss[key] is TEntity entity) WriteLog("注销", entity.Name);

                    // 修改Session
                    ss.Remove(key);
                }
                else
                {
                    // 修改Session
                    ss[key] = value;
                }

                //// 修改Cookie
                //SetCookie(key, value);
#else
                // 特殊处理注销
                if (value == null)
                {
                    var entity = Current;
                    if (entity != null) WriteLog("注销", entity.Name);
                }
#endif
            }
        }

        /// <summary>友好名字</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual String FriendName => String.IsNullOrEmpty(DisplayName) ? Name : DisplayName;

        /// <summary>物理地址</summary>
        [DisplayName("物理地址")]
        //[BindRelation(__.LastLoginIP)]
        [XmlIgnore, ScriptIgnore]
        public String LastLoginAddress => LastLoginIP.IPToAddress();
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindByID(Int32 id)
        {
            if (id <= 0) return null;

            if (Meta.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 实体缓存
            return Meta.SingleCache[id];
        }

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TEntity FindByName(String name)
        {
            if (name.IsNullOrEmpty()) return null;

            if (Meta.Count < 1000) return Meta.Cache.Find(e => e.Name.EqualIgnoreCase(name));

            // 单对象缓存
            return Meta.SingleCache.GetItemWithSlaveKey(name) as TEntity;
        }

        /// <summary>根据邮箱地址查找</summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        public static TEntity FindByMail(String mail)
        {
            if (mail.IsNullOrEmpty()) return null;

            if (Meta.Count >= 1000)
                return Find(__.Mail, mail);
            else // 实体缓存
                return Meta.Cache.Find(e => e.Mail.EqualIgnoreCase(mail));
        }

        /// <summary>根据手机号码查找</summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static TEntity FindByMobile(String mobile)
        {
            if (mobile.IsNullOrEmpty()) return null;

            if (Meta.Count >= 1000)
                return Find(__.Mobile, mobile);
            else // 实体缓存
                return Meta.Cache.Find(e => e.Mobile.EqualIgnoreCase(mobile));
        }

        /// <summary>根据唯一代码查找</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static TEntity FindByCode(String code)
        {
            if (code.IsNullOrEmpty()) return null;

            if (Meta.Count >= 1000)
                return Find(__.Code, code);
            else // 实体缓存
                return Meta.Cache.Find(e => e.Code.EqualIgnoreCase(code));
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="key"></param>
        /// <param name="roleId"></param>
        /// <param name="isEnable"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IList<TEntity> Search(String key, Int32 roleId, Boolean? isEnable, Pager p) => Search(key, roleId, isEnable, DateTime.MinValue, DateTime.MinValue, p);

        /// <summary>高级查询</summary>
        /// <param name="key"></param>
        /// <param name="roleId"></param>
        /// <param name="isEnable"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IList<TEntity> Search(String key, Int32 roleId, Boolean? isEnable, DateTime start, DateTime end, Pager p)
        {
            var exp = _.LastLogin.Between(start, end);
            if (roleId > 0) exp &= _.RoleID == roleId | _.RoleIDs.Contains("," + roleId + ",");
            if (isEnable != null) exp &= _.Enable == isEnable;

            // 先精确查询，再模糊
            if (!key.IsNullOrEmpty())
            {
                var list = FindAll(exp & (_.Code == key | _.Name == key | _.DisplayName == key | _.Mail == key | _.Mobile == key), p);
                if (list.Count > 0) return list;

                exp &= (_.Code.Contains(key) | _.Name.Contains(key) | _.DisplayName.Contains(key) | _.Mail.Contains(key) | _.Mobile.Contains(key));
            }

            return FindAll(exp, p);
        }
        #endregion

        #region 扩展操作
        /// <summary>添加用户，如果存在则直接返回</summary>
        /// <param name="name"></param>
        /// <param name="pass"></param>
        /// <param name="roleid"></param>
        /// <param name="display"></param>
        /// <returns></returns>
        public static TEntity Add(String name, String pass, Int32 roleid = 1, String display = null)
        {
            //var entity = Find(_.Name == name);
            //if (entity != null) return entity;

            if (pass.IsNullOrEmpty()) pass = name;

            var entity = new TEntity
            {
                Name = name,
                Password = pass.MD5(),
                DisplayName = display,
                RoleID = roleid,
                Enable = true
            };

            entity.Save();

            return entity;
        }

        /// <summary>已重载。显示友好名字</summary>
        /// <returns></returns>
        public override String ToString() => FriendName;
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
#if !__CORE__
                //if (rememberme && user != null)
                //{
                //    var cookie = HttpContext.Current.Response.Cookies["Admin"];
                //    if (cookie != null) cookie.Expires = DateTime.Now.Date.AddYears(1);
                //}
#endif
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

            // 过滤帐号中的空格，防止出现无操作无法登录的情况
            var account = username.Trim();
            //var user = FindByName(account);
            // 登录时必须从数据库查找用户，缓存中的用户对象密码字段可能为空
            var user = Find(__.Name, account);
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
                        for (var i = 0; i < hashTimes; i++)
                        {
                            p = p.MD5();
                        }
                    }
                    if (!p.EqualIgnoreCase(user.Password)) throw new EntityException("密码不正确！");
                }
                else
                {
                    var p = user.Password;
                    for (var i = 0; i > hashTimes; i--)
                    {
                        p = p.MD5();
                    }
                    if (!p.EqualIgnoreCase(password)) throw new EntityException("密码不正确！");
                }
            }
            else
            {
                if (hashTimes > 0)
                {
                    var p = password;
                    if (!String.IsNullOrEmpty(p))
                    {
                        for (var i = 0; i < hashTimes; i++)
                        {
                            p = p.MD5();
                        }
                    }
                    password = p;
                }
                user.Password = password;
            }

            //Current = user;

            user.SaveLoginInfo();

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

            Online = true;

            return Update();
        }

        /// <summary>注销</summary>
        public virtual void Logout()
        {
            //var user = Current;
            var user = this;
            if (user != null)
            {
                user.Online = false;
                user.SaveAsync();
            }

            //Current = null;
            //Thread.CurrentPrincipal = null;
        }

        /// <summary>注册用户。第一注册用户自动抢管理员</summary>
        public virtual void Register()
        {
            using (var tran = Meta.CreateTrans())
            {
                //!!! 第一个用户注册时，如果只有一个默认admin账号，则自动抢管理员
                if (Meta.Count < 3 && FindCount() <= 1)
                {
                    var list = FindAll();
                    if (list.Count == 0 || list.Count == 1 && list[0].DisableAdmin())
                    {
                        RoleID = 1;
                        Enable = true;
                    }
                }

                RegisterTime = DateTime.Now;
                RegisterIP = WebHelper.UserHost;

                Insert();

                tran.Commit();
            }
        }

        /// <summary>禁用默认管理员</summary>
        /// <returns></returns>
        private Boolean DisableAdmin()
        {
            if (!Name.EqualIgnoreCase("admin")) return false;
            if (!Password.EqualIgnoreCase("admin".MD5())) return false;

            Enable = false;
            RoleID = 4;

            Save();

            return true;
        }

#if !__CORE__
        //static Boolean _isInGetCookie;
        //static TEntity GetCookie(String key)
        //{
        //    if (_isInGetCookie) return null;

        //    var cookie = HttpContext.Current.Request.Cookies[key];
        //    if (cookie == null) return null;

        //    var user = HttpUtility.UrlDecode(cookie["u"]);
        //    var pass = cookie["p"];
        //    if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(pass)) return null;

        //    _isInGetCookie = true;
        //    try
        //    {
        //        return Login(user, pass, -1);
        //    }
        //    catch (DbException ex)
        //    {
        //        XTrace.WriteLine("{0}登录失败！{1}", user, ex);
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteLog("登录", user + "登录失败！" + ex.Message);
        //        return null;
        //    }
        //    finally { _isInGetCookie = false; }
        //}

        //static void SetCookie(String key, TEntity entity)
        //{
        //    var context = HttpContext.Current;
        //    var res = context?.Response;
        //    if (res == null) return;

        //    var reqcookie = context.Request.Cookies[key];
        //    if (entity != null)
        //    {
        //        var user = HttpUtility.UrlEncode(entity.Name);
        //        var pass = !String.IsNullOrEmpty(entity.Password) ? entity.Password.MD5() : null;
        //        if (reqcookie == null || user != reqcookie["u"] || pass != reqcookie["p"])
        //        {
        //            // 只有需要写入Cookie时才设置，否则会清空原来的非会话Cookie
        //            var cookie = res.Cookies[key];
        //            cookie["u"] = user;
        //            cookie["p"] = pass;
        //        }
        //    }
        //    else
        //    {
        //        var cookie = res.Cookies[key];
        //        cookie.Value = null;
        //        cookie.Expires = DateTime.Now.AddYears(-1);
        //        //HttpContext.Current.Response.Cookies.Remove(key);
        //    }
        //}
#endif
        #endregion

        #region 权限
        /// <summary>角色</summary>
        /// <remarks>扩展属性不缓存空对象，一般来说，每个管理员都有对应的角色，如果没有，可能是在初始化</remarks>
        [XmlIgnore, ScriptIgnore]
        public virtual IRole Role => Extends.Get(nameof(Role), k => ManageProvider.Get<IRole>()?.FindByID(RoleID));

        /// <summary>角色集合</summary>
        [XmlIgnore, ScriptIgnore]
        public virtual IRole[] Roles => Extends.Get(nameof(Roles), k => GetRoleIDs().Select(e => ManageProvider.Get<IRole>()?.FindByID(e)).Where(e => e != null).ToArray());

        /// <summary>获取角色列表。主角色在前，其它角色升序在后</summary>
        /// <returns></returns>
        public virtual Int32[] GetRoleIDs()
        {
            var ids = RoleIDs.SplitAsInt().OrderBy(e => e).ToList();
            if (RoleID > 0) ids.Insert(0, RoleID);

            return ids.Distinct().ToArray();
        }

        /// <summary>角色名</summary>
        [DisplayName("角色")]
        [Map(__.RoleID, typeof(RoleMapProvider))]
        [XmlIgnore, ScriptIgnore]
        public virtual String RoleName => Role + "";

        /// <summary>用户是否拥有当前菜单的指定权限</summary>
        /// <param name="menu">指定菜单</param>
        /// <param name="flags">是否拥有多个权限中的任意一个，或的关系。如果需要表示与的关系，可以传入一个多权限位合并</param>
        /// <returns></returns>
        public Boolean Has(IMenu menu, params PermissionFlags[] flags)
        {
            if (menu == null) throw new ArgumentNullException(nameof(menu));

            // 角色集合
            var rs = Roles;

            // 如果没有指定权限子项，则指判断是否拥有资源
            if (flags == null || flags.Length == 0) return rs.Any(r => r.Has(menu.ID));

            foreach (var item in flags)
            {
                // 如果判断None，则直接返回
                if (item == PermissionFlags.None) return true;

                // 菜单必须拥有这些权限位才行
                if (menu.Permissions.ContainsKey((Int32)item))
                {
                    //// 如果判断None，则直接返回
                    //if (item == PermissionFlags.None) return true;

                    if (rs.Any(r => r.Has(menu.ID, item))) return true;
                }
            }

            return false;
        }
        #endregion

        #region IManageUser 成员
        /// <summary>昵称</summary>
        String IManageUser.NickName { get => DisplayName; set => DisplayName = value; }

        String IIdentity.Name => Name;

        String IIdentity.AuthenticationType => "XCode";

        Boolean IIdentity.IsAuthenticated => true;
        #endregion
    }

    class RoleMapProvider : MapProvider
    {
        public RoleMapProvider()
        {
            var role = ManageProvider.Get<IRole>();
            EntityType = role.GetType();
            Key = EntityFactory.CreateOperate(EntityType).Unique?.Name;
        }
    }

    public partial interface IUser
    {
        /// <summary>友好名字</summary>
        String FriendName { get; }

        /// <summary>角色</summary>
        IRole Role { get; }

        /// <summary>角色集合</summary>
        IRole[] Roles { get; }

        /// <summary>角色名</summary>
        String RoleName { get; }

        /// <summary>用户是否拥有当前菜单的指定权限</summary>
        /// <param name="menu">指定菜单</param>
        /// <param name="flags">是否拥有多个权限中的任意一个，或的关系。如果需要表示与的关系，可以传入一个多权限位合并</param>
        /// <returns></returns>
        Boolean Has(IMenu menu, params PermissionFlags[] flags);

        /// <summary>注销</summary>
        void Logout();

        /// <summary>保存</summary>
        Int32 Save();
    }
}