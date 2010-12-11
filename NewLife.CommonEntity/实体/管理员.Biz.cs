using System;
using System.ComponentModel;
using System.Web;
using System.Xml.Serialization;
using NewLife.Log;
using NewLife.Web;
using XCode;
using NewLife.Security;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 管理员
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TRoleEntity"></typeparam>
    /// <typeparam name="TMenuEntity"></typeparam>
    /// <typeparam name="TRoleMenuEntity"></typeparam>
    /// <typeparam name="TLogEntity"></typeparam>
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
                if (XTrace.Debug) XTrace.WriteLine("开始初始化授权数据……");

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

                        TRoleMenuEntity entity = new TRoleMenuEntity();
                        entity.RoleID = id;
                        entity.MenuID = item.ID;
                        entity.Save();
                    }

                    if (XTrace.Debug) XTrace.WriteLine("完成初始化授权数据！");
                }
                catch (Exception ex)
                {
                    if (XTrace.Debug) XTrace.WriteLine("初始化授权数据失败！" + ex.ToString());
                }
            }
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
        /// 拥有指定菜单的权限
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Boolean HasMenu(String name)
        {
            if (Role == null) return false;

            return Role.HasMenu(name);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="action"></param>
        protected override void WriteLog(string action)
        {
            Log<TLogEntity> log = Log<TLogEntity>.Create(this.GetType(), action);
            log.UserID = ID;
            log.UserName = DisplayName;
            log.Save();
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
    public partial class Administrator<TEntity> : Entity<TEntity>
        where TEntity : Administrator<TEntity>, new()
    {
        #region 对象操作
        static Administrator()
        {
            //// 设置缓存时间为一个小时
            //Meta.Cache.Expriod = 60 * 60;

            // 初始化数据
            if (Meta.Count < 1)
            {
                if (XTrace.Debug) XTrace.WriteLine("开始初始化管理员数据……");

                TEntity user = new TEntity();
                user.Name = "admin";
                user.Password = DataHelper.Hash("admin");
                user.DisplayName = "管理员";
                user.RoleID = 1;
                user.IsEnable = true;
                user.Insert();

                if (XTrace.Debug) XTrace.WriteLine("完成初始化管理员数据！");
            }
        }
        #endregion

        #region 扩展属性
        private const String CurrentKey = "Admin";
        static HttpState<TEntity> _httpState;
        /// <summary>
        /// Http状态
        /// </summary>
        public static HttpState<TEntity> HttpState
        {
            get
            {
                if (_httpState != null) return _httpState;
                _httpState = new HttpState<TEntity>();
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
                    catch { return null; }
                });
                _httpState.EntityToCookie = new Converter<TEntity, HttpCookie>(delegate(TEntity entity)
                {
                    HttpCookie cookie = HttpContext.Current.Response.Cookies[CurrentKey];
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
        }

        /// <summary>当前登录用户</summary>
        public static TEntity Current
        {
            get { return HttpState.Current; }
            set { HttpState.Current = value; }
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
        #endregion

        #region 扩展操作
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Update, false)]
        public static Int32 UpdateRole(TEntity obj)
        {
            TEntity update = FindByKey(obj.ID);
            if (!string.IsNullOrEmpty(obj.Password))
            {
                update.Password = DataHelper.Hash(obj.Password);
            }
            update.RoleID = obj.RoleID;
            update.IsEnable = obj.IsEnable;
            return update.Update();
        }
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
            return Login(username, password, 1);
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

            user.WriteLog("登录");

            return user;
        }

        /// <summary>
        /// 注销
        /// </summary>
        public void Logout()
        {
            WriteLog("注销");
            Current = null;
        }

        /// <summary>
        /// 拥有指定菜单的权限
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Boolean HasMenu(String name) { return false; }

        /// <summary>
        /// 为当前管理员对象写日志
        /// </summary>
        /// <param name="action"></param>
        protected virtual void WriteLog(String action) { }
        #endregion
    }
}