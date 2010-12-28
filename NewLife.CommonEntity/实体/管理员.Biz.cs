using System;
using System.ComponentModel;
using System.Web;
using System.Xml.Serialization;
using NewLife.Log;
using NewLife.Security;
using NewLife.Web;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 管理员
    /// </summary>
    /// <typeparam name="TEntity">管理员实体类</typeparam>
    /// <typeparam name="TRoleEntity">角色实体类</typeparam>
    /// <typeparam name="TMenuEntity">菜单实体类</typeparam>
    /// <typeparam name="TRoleMenuEntity">角色菜单实体类</typeparam>
    /// <typeparam name="TLogEntity">日志实体类</typeparam>
    public partial class Administrator<TEntity, TRoleEntity, TMenuEntity, TRoleMenuEntity, TLogEntity> : Administrator<TEntity>
        where TEntity : Administrator<TEntity, TRoleEntity, TMenuEntity, TRoleMenuEntity, TLogEntity>, new()
        where TRoleEntity : Role<TRoleEntity, TMenuEntity, TRoleMenuEntity>, new()
        where TMenuEntity : Menu<TMenuEntity>, new()
        where TRoleMenuEntity : RoleMenu<TRoleMenuEntity>, new()
        where TLogEntity : Log<TLogEntity>, new()
    {
        #region 对象操作
        static Administrator()
        {
            // 初始化数据
            Int32 count = RoleMenu<TRoleMenuEntity>.Meta.Count;
            if (count <= 1)
            {
                if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}授权数据……", typeof(TEntity).Name);

                try
                {
                    Int32 id = 1;
                    EntityList<TRoleEntity> rs = Role<TRoleEntity>.Meta.Cache.Entities;
                    if (rs != null && rs.Count > 0)
                    {
                        id = rs[0].ID;
                    }

                    // 授权访问所有菜单
                    //EntityList<TMenuEntity> ms = Menu<TMenuEntity>.Meta.Cache.Entities;
                    EntityList<TMenuEntity> ms = Menu<TMenuEntity>.FindAll();
                    EntityList<TRoleMenuEntity> rms = RoleMenu<TRoleMenuEntity>.FindAllByRoleID(id);
                    foreach (TMenuEntity item in ms)
                    {
                        // 是否已存在
                        if (rms != null && rms.Find(RoleMenu<TRoleMenuEntity>._.MenuID, item.ID) != null) continue;

                        //TRoleMenuEntity entity = new TRoleMenuEntity();
                        //entity.RoleID = id;
                        //entity.MenuID = item.ID;
                        TRoleMenuEntity entity = RoleMenu<TRoleMenuEntity>.Create(id, item.ID);
                        entity.Save();
                    }

                    if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}授权数据！", typeof(TEntity).Name);
                }
                catch (Exception ex)
                {
                    if (XTrace.Debug) XTrace.WriteLine("初始化{0}授权数据失败！{1}", typeof(TEntity).Name, ex.ToString());
                }
            }

            // 给菜单类设置一个默认管理员对象，用于写日志
            if (DefaultAdministrator == null) DefaultAdministrator = new TEntity();
            Menu<TMenuEntity>.DefaultAdministrator = DefaultAdministrator;
        }

        /// <summary>
        /// 已重载。调用Save时写日志，而调用Insert和Update时不写日志
        /// </summary>
        /// <returns></returns>
        public override int Save()
        {
            if (ID == 0)
                WriteLog("添加", Name);
            else
                WriteLog("修改", Name);

            return base.Save();
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override int Delete()
        {
            WriteLog("删除", Name);

            return base.Delete();
        }
        #endregion

        /// <summary>角色</summary>
        [XmlIgnore]
        public virtual TRoleEntity Role
        {
            get { return GetExtend<TRoleEntity, TRoleEntity>("Role", delegate { return Role<TRoleEntity, TMenuEntity, TRoleMenuEntity>.FindByID(RoleID); }); }
            set { SetExtend<TRoleEntity>("Role", value); }
        }

        /// <summary>
        /// 角色名
        /// </summary>
        public virtual String RoleName { get { return Role == null ? null : Role.Name; } set { } }

        /// <summary>
        /// 根据权限名（权限路径）找到权限菜单实体
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override IEntity FindPermissionMenu(string name)
        {
            //return Menu<TMenuEntity>.FindForPerssion(name);
            TMenuEntity menu = Menu<TMenuEntity>.FindForPerssion(name);
            //if (menu == null)
            //{
            //    //IEntity log = Menu<TMenuEntity>.CreateLog("检查权限");
            //    //log["Remark"] = String.Format("系统中没有[{0}]的权限项", name);
            //    //log.Save();
            //}

            // 找不到的时候，修改当前页面
            if (menu == null && Menu<TMenuEntity>.Current != null)
            {
                if (Menu<TMenuEntity>.Current.ResetName(name)) menu = Menu<TMenuEntity>.Current;
            }

            return menu;
        }

        ///// <summary>
        ///// 拥有指定菜单的权限
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //public Boolean HasMenu(String name)
        //{
        //    if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

        //    TMenuEntity menu = FindPermissionMenu(name) as TMenuEntity;
        //    if (menu == null) return false;

        //    return Acquire(menu.ID, PermissionFlags.None);

        //    //Boolean rs = Role.HasMenu(name);
        //    //// 没有权限，检查是否有这个权限项，如果没有，则写入日志提醒管理员
        //    //if (!rs)
        //    //{
        //    //    TMenuEntity menu = Menu<TMenuEntity>.FindForPerssion(name);
        //    //    if (menu == null)
        //    //    {
        //    //        IEntity log = CreateLog(this.GetType(), "检查权限");
        //    //        log["Remark"] = String.Format("系统中没有[{0}]的权限项", name);
        //    //        log.Save();
        //    //    }
        //    //}
        //    //return rs;
        //}

        ///// <summary>
        ///// 拥有指定菜单的权限
        ///// </summary>
        ///// <param name="menuID"></param>
        ///// <returns></returns>
        //public override Boolean HasMenu(Int32 menuID)
        //{
        //    return Role != null && Role.HasMenu(menuID);
        //}

        ///// <summary>
        ///// 申请指定菜单指定操作的权限
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="flag"></param>
        ///// <returns></returns>
        //public override bool Acquire(String name, PermissionFlags flag)
        //{
        //    if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

        //    if (Role == null) return false;

        //    //TRoleEntity entity = Role;
        //    //if (entity == null) return false;

        //    // 申请权限
        //    return entity.Acquire(menuID, flag);
        //}

        /// <summary>
        /// 申请指定菜单指定操作的权限
        /// </summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public override Boolean Acquire(Int32 menuID, PermissionFlags flag)
        {
            if (menuID <= 0) throw new ArgumentNullException("menuID");

            TRoleEntity entity = Role;
            if (entity == null) return false;

            // 申请权限
            return entity.Acquire(menuID, flag);
        }

        ///// <summary>
        ///// 写日志
        ///// </summary>
        ///// <param name="action"></param>
        //protected override void WriteLog(string action)
        //{
        //    Log<TLogEntity> log = Log<TLogEntity>.Create(this.GetType(), action);
        //    log.UserID = ID;
        //    log.UserName = DisplayName;
        //    log.Save();
        //}

        /// <summary>
        /// 创建当前管理员的日志实体
        /// </summary>
        /// <param name="type"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public override IEntity CreateLog(Type type, string action)
        {
            Log<TLogEntity> log = Log<TLogEntity>.Create(this.GetType(), action);
            log.UserID = ID;
            log.UserName = FriendName;

            return log;
        }
    }

    /// <summary>
    /// 管理员
    /// </summary>
    /// <remarks>
    /// 基础实体类应该是只有一个泛型参数的，
    /// 需要用到别的类型时，可以继承一个，
    /// 也可以通过虚拟重载等手段让基类实现
    /// </remarks>
    /// <typeparam name="TEntity">管理员类型</typeparam>
    public abstract partial class Administrator<TEntity> : CommonEntityBase<TEntity>, IAdministrator
        where TEntity : Administrator<TEntity>, new()
    {
        #region 对象操作
        static Administrator()
        {
            // 给基类设置一个默认管理员对象，用于写日志
            TEntity entity = new TEntity();
            DefaultAdministrator = entity;

            //// 设置缓存时间为一个小时
            //Meta.Cache.Expriod = 60 * 60;

            // 初始化数据
            if (Meta.Count < 1)
            {
                if (XTrace.Debug) XTrace.WriteLine("开始初始化{0}管理员数据……", typeof(TEntity).Name);

                TEntity user = new TEntity();
                user.Name = "admin";
                user.Password = DataHelper.Hash("admin");
                user.DisplayName = "管理员";
                user.RoleID = 1;
                user.IsEnable = true;
                user.Insert();

                if (XTrace.Debug) XTrace.WriteLine("完成初始化{0}管理员数据！", typeof(TEntity).Name);
            }
        }
        #endregion

        #region 扩展属性
        static HttpState<TEntity> _httpState;
        /// <summary>
        /// Http状态，子类可以重新给HttpState赋值，以控制保存Http状态的过程
        /// </summary>
        public static HttpState<TEntity> HttpState
        {
            get
            {
                if (_httpState != null) return _httpState;
                _httpState = new HttpState<TEntity>("Admin");
                _httpState.CookieToEntity = new Converter<HttpCookie, TEntity>(delegate(HttpCookie cookie)
                {
                    if (cookie == null) return null;

                    String user = cookie["u"];
                    String pass = cookie["p"];
                    if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(pass)) return null;

                    try
                    {
                        return Login(user, pass, -1);
                    }
                    catch (Exception ex)
                    {
                        WriteLog("登录", user + "登录失败！" + ex.Message);
                        return null;
                    }
                });
                _httpState.EntityToCookie = new Converter<TEntity, HttpCookie>(delegate(TEntity entity)
                {
                    HttpCookie cookie = HttpContext.Current.Response.Cookies[_httpState.Key];
                    if (entity != null)
                    {
                        cookie["u"] = entity.Name;
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
            get { return HttpState.Current; }
            set { HttpState.Current = value; }
        }

        /// <summary>当前登录用户，不带自动登录</summary>
        public static TEntity CurrentNoAutoLogin
        {
            get { return HttpState.Get(null, null); }
            //set { HttpState.Current = value; }
        }

        /// <summary>
        /// 友好名字
        /// </summary>
        public virtual String FriendName
        {
            get
            {
                return String.IsNullOrEmpty(DisplayName) ? Name : DisplayName;
            }
        }
        #endregion

        #region 扩展查询
        /// <summary>
        /// 根据主键查询一个管理员实体对象用于表单编辑
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
                entity.IsEnable = true;
            }
            return entity;
        }

        /// <summary>
        /// 根据名称查找
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEntity FindByName(String name)
        {
            if (String.IsNullOrEmpty(name) || Meta.Cache.Entities == null || Meta.Cache.Entities.Count < 1) return null;

            return Meta.Cache.Entities.Find(_.Name, name);
        }

        /// <summary>
        /// 根据SSOUserID查找
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TEntity FindBySSOUserID(Int32 id)
        {
            return Meta.Cache.Entities.Find(_.SSOUserID, id);
        }

        /// <summary>
        /// 根据SSOUserID查找所有帐户
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EntityList<TEntity> FindAllBySSOUserID(Int32 id)
        {
            return Meta.Cache.Entities.FindAll(_.SSOUserID, id);
        }
        #endregion

        #region 扩展操作
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //[DataObjectMethod(DataObjectMethodType.Update, false)]
        //public static Int32 UpdateRole(TEntity obj)
        //{
        //    TEntity update = FindByKey(obj.ID);
        //    if (!string.IsNullOrEmpty(obj.Password))
        //    {
        //        update.Password = DataHelper.Hash(obj.Password);
        //    }
        //    update.RoleID = obj.RoleID;
        //    update.IsEnable = obj.IsEnable;
        //    return update.Update();
        //}
        #endregion

        #region 业务
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static TEntity Login(String username, String password)
        {
            try
            {
                return Login(username, password, 1);
            }
            catch (Exception ex)
            {
                WriteLog("登录", username + "登录失败！" + ex.Message);
                throw;
            }
        }

        static TEntity Login(String username, String password, Int32 hashTimes)
        {
            if (String.IsNullOrEmpty(username)) return null;

            TEntity user = FindByName(username);
            if (user == null) return null;

            if (!user.IsEnable) throw new Exception("账号被禁用！");

            if (hashTimes > 0)
            {
                String p = password;
                for (int i = 0; i < hashTimes; i++)
                {
                    p = DataHelper.Hash(p);
                }
                if (!String.Equals(user.Password, p)) throw new Exception("密码不正确！");
            }
            else
            {
                String p = user.Password;
                for (int i = 0; i > hashTimes; i--)
                {
                    p = DataHelper.Hash(p);
                }
                if (!String.Equals(p, password)) throw new Exception("密码不正确！");
            }

            user.Logins++;
            user.LastLogin = DateTime.Now;
            user.LastLoginIP = WebHelper.UserHost;
            user.Update();

            Current = user;

            if (hashTimes == -1)
                WriteLog("自动登录", username);
            else
                WriteLog("登录", username);

            return user;
        }

        /// <summary>
        /// 注销
        /// </summary>
        public void Logout()
        {
            WriteLog("注销", null);
            Current = null;
        }

        /// <summary>
        /// 根据权限名（权限路径）找到权限菜单实体
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract IEntity FindPermissionMenu(String name);

        /// <summary>
        /// 拥有指定菜单的权限
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Boolean HasMenu(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            IEntity menu = FindPermissionMenu(name);
            if (menu == null) return false;

            return Acquire((Int32)menu["ID"], PermissionFlags.None);
        }

        ///// <summary>
        ///// 拥有指定菜单的权限
        ///// </summary>
        ///// <param name="menuID"></param>
        ///// <returns></returns>
        //public virtual Boolean HasMenu(Int32 menuID) { return false; }

        ///// <summary>
        ///// 申请指定菜单指定操作的权限
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="flag"></param>
        ///// <returns></returns>
        //public virtual Boolean Acquire(String name, PermissionFlags flag) { return false; }

        /// <summary>
        /// 申请指定菜单指定操作的权限
        /// </summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public abstract Boolean Acquire(Int32 menuID, PermissionFlags flag);

        ///// <summary>
        ///// 为当前管理员对象写日志
        ///// </summary>
        ///// <param name="action"></param>
        //protected void WriteLog(String action)
        //{
        //    IEntity log = CreateLog(this.GetType(), action);
        //    if (log != null) log.Save();
        //}

        /// <summary>
        /// 创建当前管理员的日志实体
        /// </summary>
        /// <param name="type"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public abstract IEntity CreateLog(Type type, String action);

        ///// <summary>
        ///// 创建指定类型指定动作的日志实体
        ///// </summary>
        ///// <param name="type"></param>
        ///// <param name="action"></param>
        ///// <returns></returns>
        //IEntity IAdministrator.CreateLog(Type type, String action) { return CreateLog(type, action); }
        #endregion
    }
}