using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

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
    [Obsolete("UserX=>User")]
    public class UserX : User { }

    /// <summary>管理员</summary>
    /// <remarks>
    /// 基础实体类应该是只有一个泛型参数的，需要用到别的类型时，可以继承一个，也可以通过虚拟重载等手段让基类实现
    /// </remarks>
    public partial class User : LogEntity<User>, IUser, IAuthUser, IIdentity
    {
        #region 对象操作
        static User()
        {
            //// 用于引发基类的静态构造函数
            //var entity = new TEntity();

            //!!! 曾经这里导致产生死锁
            // 这里是静态构造函数，访问Factory引发EntityFactory.CreateOperate，
            // 里面的EnsureInit会等待实体类实例化完成，实体类的静态构造函数还卡在这里
            // 不过这不是理由，同一个线程遇到同一个锁不会堵塞
            // 发生死锁的可能性是这里引发EnsureInit，而另一个线程提前引发EnsureInit拿到锁
            Meta.Factory.AdditionalFields.Add(__.Logins);
            //Meta.Factory.FullInsert = false;

            // 单对象缓存
            var sc = Meta.SingleCache;
            sc.FindSlaveKeyMethod = k => Find(__.Name, k);
            sc.GetSlaveKeyMethod = e => e.Name;
        }

        /// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal protected override void InitData()
        {
            if (Meta.Count > 0) return;

            if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}用户数据……", typeof(User).Name);

            Add("admin", null, 1, "管理员");
            //Add("poweruser", null, 2, "高级用户");
            //Add("user", null, 3, "普通用户");

            if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}用户数据！", typeof(User).Name);
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
                RoleIds = str;
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
        /// <summary>物理地址</summary>
        [DisplayName("物理地址")]
        public String LastLoginAddress => LastLoginIP.IPToAddress();

        /// <summary>部门</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public Department Department => Extends.Get(nameof(Department), k => Department.FindByID(DepartmentID));

        /// <summary>部门</summary>
        [Map(__.DepartmentID, typeof(Department), __.ID)]
        public String DepartmentName => Department?.ToString();

        ///// <summary>兼容旧版角色组</summary>
        //[Obsolete("=>RoleIds")]
        //public String RoleIDs { get => RoleIds; set => RoleIds = value; }
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static User FindByID(Int32 id)
        {
            if (id <= 0) return null;

            if (Meta.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 实体缓存
            return Meta.SingleCache[id];
        }

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static User FindByName(String name)
        {
            if (name.IsNullOrEmpty()) return null;

            if (Meta.Count < 1000) return Meta.Cache.Find(e => e.Name.EqualIgnoreCase(name));

            // 单对象缓存
            return Meta.SingleCache.GetItemWithSlaveKey(name) as User;
        }

        /// <summary>根据邮箱地址查找</summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        public static User FindByMail(String mail)
        {
            if (mail.IsNullOrEmpty()) return null;

            if (Meta.Count < 1000) return Meta.Cache.Find(e => e.Mail.EqualIgnoreCase(mail));

            return Find(__.Mail, mail);
        }

        /// <summary>根据手机号码查找</summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static User FindByMobile(String mobile)
        {
            if (mobile.IsNullOrEmpty()) return null;

            if (Meta.Count < 1000) return Meta.Cache.Find(e => e.Mobile == mobile);

            return Find(__.Mobile, mobile);
        }

        /// <summary>根据唯一代码查找</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static User FindByCode(String code)
        {
            if (code.IsNullOrEmpty()) return null;

            if (Meta.Count < 1000) return Meta.Cache.Find(e => e.Code.EqualIgnoreCase(code));

            return Find(__.Code, code);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="key"></param>
        /// <param name="roleId"></param>
        /// <param name="isEnable"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IList<User> Search(String key, Int32 roleId, Boolean? isEnable, PageParameter p) => Search(key, roleId, isEnable, DateTime.MinValue, DateTime.MinValue, p);

        /// <summary>高级查询</summary>
        /// <param name="key"></param>
        /// <param name="roleId"></param>
        /// <param name="isEnable"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IList<User> Search(String key, Int32 roleId, Boolean? isEnable, DateTime start, DateTime end, PageParameter p)
        {
            var exp = _.LastLogin.Between(start, end);
            if (roleId > 0) exp &= _.RoleID == roleId | _.RoleIds.Contains("," + roleId + ",");
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

        /// <summary>高级搜索</summary>
        /// <param name="roleId">角色</param>
        /// <param name="departmentId">部门</param>
        /// <param name="enable">启用</param>
        /// <param name="start">登录时间开始</param>
        /// <param name="end">登录时间结束</param>
        /// <param name="key">关键字，搜索代码、名称、昵称、手机、邮箱</param>
        /// <param name="page"></param>
        /// <returns></returns>
        public static IList<User> Search(Int32 roleId, Int32 departmentId, Boolean? enable, DateTime start, DateTime end, String key, PageParameter page)
        {
            var exp = new WhereExpression();
            if (roleId >= 0) exp &= _.RoleID == roleId | _.RoleIds.Contains("," + roleId + ",");
            if (departmentId >= 0) exp &= _.DepartmentID == departmentId;
            if (enable != null) exp &= _.Enable == enable.Value;
            exp &= _.LastLogin.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.Code.StartsWith(key) | _.Name.StartsWith(key) | _.DisplayName.StartsWith(key) | _.Mobile.StartsWith(key) | _.Mail.StartsWith(key);

            return FindAll(exp, page);
        }

        /// <summary>高级搜索</summary>
        /// <param name="roleIds">角色</param>
        /// <param name="departmentIds">部门</param>
        /// <param name="enable">启用</param>
        /// <param name="start">登录时间开始</param>
        /// <param name="end">登录时间结束</param>
        /// <param name="key">关键字，搜索代码、名称、昵称、手机、邮箱</param>
        /// <param name="page"></param>
        /// <returns></returns>
        public static IList<User> Search(Int32[] roleIds, Int32[] departmentIds, Boolean? enable, DateTime start, DateTime end, String key, PageParameter page)
        {
            var exp = new WhereExpression();
            if (roleIds != null && roleIds.Length > 0) exp &= _.RoleID.In(roleIds) | _.RoleIds.Contains("," + roleIds.Join(",") + ",");
            if (departmentIds != null && departmentIds.Length > 0) exp &= _.DepartmentID.In(departmentIds);
            if (enable != null) exp &= _.Enable == enable.Value;
            exp &= _.LastLogin.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.Code.StartsWith(key) | _.Name.StartsWith(key) | _.DisplayName.StartsWith(key) | _.Mobile.StartsWith(key) | _.Mail.StartsWith(key);

            return FindAll(exp, page);
        }
        #endregion

        #region 扩展操作
        /// <summary>添加用户，如果存在则直接返回</summary>
        /// <param name="name"></param>
        /// <param name="pass"></param>
        /// <param name="roleid"></param>
        /// <param name="display"></param>
        /// <returns></returns>
        public static User Add(String name, String pass, Int32 roleid = 1, String display = null)
        {
            //var entity = Find(_.Name == name);
            //if (entity != null) return entity;

            if (pass.IsNullOrEmpty()) pass = name;

            var entity = new User
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
        public override String ToString() => DisplayName.IsNullOrEmpty() ? Name : DisplayName;
        #endregion

        #region 业务
        /// <summary>登录。借助回调来验证密码</summary>
        /// <param name="username"></param>
        /// <param name="onValid"></param>
        /// <returns></returns>
        public static User Login(String username, Action<User> onValid)
        {
            if (String.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            if (onValid == null) throw new ArgumentNullException(nameof(onValid));

            try
            {
                // 过滤帐号中的空格，防止出现无操作无法登录的情况
                var account = username.Trim();
                //var user = FindByName(account);
                // 登录时必须从数据库查找用户，缓存中的用户对象密码字段可能为空
                var user = Find(__.Name, account);
                if (user == null) throw new EntityException("帐号{0}不存在！", account);

                if (!user.Enable) throw new EntityException("账号{0}被禁用！", account);

                // 验证用户
                onValid(user);

                user.SaveLoginInfo();

                WriteLog("登录", true, username);

                return user;
            }
            catch (Exception ex)
            {
                WriteLog("登录", false, username + "登录失败！" + ex.Message);
                throw;
            }
        }

        /// <summary>登录</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="rememberme">是否记住密码</param>
        /// <returns></returns>
        public static User Login(String username, String password, Boolean rememberme = false)
        {
            if (String.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            //if (String.IsNullOrEmpty(password)) throw new ArgumentNullException("password");

            try
            {
                return Login(username, password, 1);
            }
            catch (Exception ex)
            {
                WriteLog("登录", false, username + "登录失败！" + ex.Message);
                throw;
            }
        }

        static User Login(String username, String password, Int32 hashTimes)
        {
            if (String.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username), "该帐号不存在！");

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
                WriteLog("自动登录", true, username);
            else
                WriteLog("登录", true, username);

            return user;
        }

        /// <summary>保存登录信息</summary>
        /// <returns></returns>
        protected virtual Int32 SaveLoginInfo()
        {
            Logins++;
            LastLogin = DateTime.Now;
            var ip = ManageProvider.UserHost;
            if (!ip.IsNullOrEmpty()) LastLoginIP = ip;

            Online = true;

            return Update();
        }

        /// <summary>注销</summary>
        public virtual void Logout()
        {
            //var user = Current;
            //var user = this;
            //if (user != null)
            //{
            //    user.Online = false;
            //    user.SaveAsync();
            //}

            //Current = null;
            //Thread.CurrentPrincipal = null;
        }

        /// <summary>注册用户。第一注册用户自动抢管理员</summary>
        public virtual void Register()
        {
            using var tran = Meta.CreateTrans();
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
            RegisterIP = ManageProvider.UserHost;

            Insert();

            tran.Commit();
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
        #endregion

        #region 权限
        /// <summary>角色</summary>
        /// <remarks>扩展属性不缓存空对象，一般来说，每个管理员都有对应的角色，如果没有，可能是在初始化</remarks>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public virtual IRole Role => Extends.Get(nameof(Role), k => ManageProvider.Get<IRole>()?.FindByID(RoleID));

        /// <summary>角色集合</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public virtual IRole[] Roles => Extends.Get(nameof(Roles), k => GetRoleIDs().Select(e => ManageProvider.Get<IRole>()?.FindByID(e)).Where(e => e != null).ToArray());

        /// <summary>获取角色列表。主角色在前，其它角色升序在后</summary>
        /// <returns></returns>
        public virtual Int32[] GetRoleIDs()
        {
            var ids = RoleIds.SplitAsInt().OrderBy(e => e).ToList();
            if (RoleID > 0) ids.Insert(0, RoleID);

            return ids.Distinct().ToArray();
        }

        /// <summary>角色名</summary>
        [DisplayName("角色")]
        [Map(__.RoleID, typeof(RoleMapProvider))]
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
            Key = EntityType.AsFactory().Unique?.Name;
        }
    }

    /// <summary>用户</summary>
    public partial interface IUser
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称。登录用户名</summary>
        String Name { get; set; }

        /// <summary>密码</summary>
        String Password { get; set; }

        /// <summary>昵称</summary>
        String DisplayName { get; set; }

        /// <summary>性别。未知、男、女</summary>
        SexKinds Sex { get; set; }

        /// <summary>邮件</summary>
        String Mail { get; set; }

        /// <summary>手机</summary>
        String Mobile { get; set; }

        /// <summary>代码。身份证、员工编号等</summary>
        String Code { get; set; }

        /// <summary>头像</summary>
        String Avatar { get; set; }

        /// <summary>角色。主要角色</summary>
        Int32 RoleID { get; set; }

        /// <summary>角色组。次要角色集合</summary>
        String RoleIds { get; set; }

        /// <summary>部门。组织机构</summary>
        Int32 DepartmentID { get; set; }

        /// <summary>在线</summary>
        Boolean Online { get; set; }

        /// <summary>启用</summary>
        Boolean Enable { get; set; }

        /// <summary>登录次数</summary>
        Int32 Logins { get; set; }

        /// <summary>最后登录</summary>
        DateTime LastLogin { get; set; }

        /// <summary>最后登录IP</summary>
        String LastLoginIP { get; set; }

        /// <summary>注册时间</summary>
        DateTime RegisterTime { get; set; }

        /// <summary>注册IP</summary>
        String RegisterIP { get; set; }

        /// <summary>扩展1</summary>
        Int32 Ex1 { get; set; }

        /// <summary>扩展2</summary>
        Int32 Ex2 { get; set; }

        /// <summary>扩展3</summary>
        Double Ex3 { get; set; }

        /// <summary>扩展4</summary>
        String Ex4 { get; set; }

        /// <summary>扩展5</summary>
        String Ex5 { get; set; }

        /// <summary>扩展6</summary>
        String Ex6 { get; set; }

        /// <summary>更新者</summary>
        String UpdateUser { get; set; }

        /// <summary>更新用户</summary>
        Int32 UpdateUserID { get; set; }

        /// <summary>更新地址</summary>
        String UpdateIP { get; set; }

        /// <summary>更新时间</summary>
        DateTime UpdateTime { get; set; }

        /// <summary>备注</summary>
        String Remark { get; set; }
        #endregion

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