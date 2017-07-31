using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Web;

namespace XCode.Membership
{
    /// <summary>管理模块</summary>
    public class ManagerModule : IHttpModule
    {
        #region 提供者
        /// <summary>当前用户提供者</summary>
        public IManageProvider Provider { get; set; }
        #endregion

        #region 构造函数
        /// <summary>实例化</summary>
        public ManagerModule() : this(null) { }

        /// <summary>实例化</summary>
        /// <param name="provider"></param>
        public ManagerModule(IManageProvider provider)
        {
            Provider = provider ?? ManageProvider.Provider;
        }
        #endregion

        /// <summary>销毁</summary>
        public void Dispose() { }

        /// <summary>初始化</summary>
        /// <param name="app"></param>
        public void Init(HttpApplication app)
        {
            app.PostAuthenticateRequest += new EventHandler(OnEnter);
            //app.EndRequest += new EventHandler(OnLeave);
        }

        private void OnEnter(Object source, EventArgs eventArgs)
        {
            var ctx = HttpContext.Current;
            if (ctx == null) return;

            var user = Provider.Current;
            if (user == null) return;

            var id = user as IIdentity;
            if (id == null) return;

            // 角色列表
            var roles = new List<String>();
            if (user is IUser) roles.Add((user as IUser).RoleName);

            ctx.User = new GenericPrincipal(id, roles.ToArray());
        }

        //private void OnLeave(Object source, EventArgs eventArgs)
        //{
        //}
    }
}