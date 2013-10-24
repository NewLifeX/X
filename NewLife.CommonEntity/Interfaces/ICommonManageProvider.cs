using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Web.UI;
using NewLife.Reflection;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>通用实体类管理提供者接口</summary>
    public interface ICommonManageProvider : IManageProvider
    {
        #region 类型
        /// <summary>管理员类</summary>
        Type AdminstratorType { get; }

        /// <summary>日志类</summary>
        Type LogType { get; }

        /// <summary>菜单类</summary>
        Type MenuType { get; }

        /// <summary>角色类</summary>
        Type RoleType { get; }

        /// <summary>权限类</summary>
        Type RoleMenuType { get; }
        #endregion

        #region 页面和表单
        /// <summary>创建管理页控制器</summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        [Obsolete("该成员在后续版本中讲不再被支持！")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IManagePage CreatePage(Control container, Type entityType);

        /// <summary>创建实体表单控制器</summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        [Obsolete("该成员在后续版本中讲不再被支持！")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IEntityForm CreateForm(Control container, Type entityType);
        #endregion

        #region 菜单
        /// <summary>菜单根，如果不支持则返回null</summary>
        IMenu MenuRoot { get; }

        /// <summary>根据编号找到菜单</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IMenu FindByMenuID(Int32 id);

        /// <summary>获取指定菜单下，当前用户有权访问的子菜单。</summary>
        /// <param name="menuid"></param>
        /// <returns></returns>
        IList<IMenu> GetMySubMenus(Int32 menuid);
        #endregion
    }

    /// <summary>通用实体类管理提供者</summary>
    public class CommonManageProvider : CommonManageProvider<Administrator> { }

    /// <summary>通用实体类管理提供者</summary>
    /// <typeparam name="TAdministrator">管理员类</typeparam>
    public class CommonManageProvider<TAdministrator> : ManageProvider, ICommonManageProvider where TAdministrator : Administrator<TAdministrator>, new()
    {
        #region 静态实例
        /// <summary>当前提供者</summary>
        public new static ICommonManageProvider Provider { get { return CommonService.ResolveInstance<IManageProvider>() as ICommonManageProvider; } }
        #endregion

        #region IManageProvider 接口
        /// <summary>管理用户类</summary>
        public override Type ManageUserType { get { return AdminstratorType; } }

        /// <summary>当前用户</summary>
        public override IManageUser Current { get { return Administrator<TAdministrator>.Current; } }

        /// <summary>根据用户编号查找</summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public override IManageUser FindByID(Object userid) { return Administrator<TAdministrator>.FindByID((Int32)userid); }

        /// <summary>根据用户帐号查找</summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public override IManageUser FindByAccount(String account) { return Administrator<TAdministrator>.FindByName(account); }

        /// <summary>登录</summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public override IManageUser Login(String account, String password) { return Administrator<TAdministrator>.Login(account, password); }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public CommonManageProvider() { InitType(); }
        #endregion

        #region 类型
        private Type _AdministratorType;
        /// <summary>管理员类</summary>
        public virtual Type AdminstratorType { get { return _AdministratorType; } }

        private Type _LogType;
        /// <summary>日志类</summary>
        public virtual Type LogType { get { return _LogType; } }

        private Type _MenuType;
        /// <summary>菜单类</summary>
        public virtual Type MenuType { get { return _MenuType; } }

        private Type _RoleType;
        /// <summary>角色类</summary>
        public virtual Type RoleType { get { return _RoleType; } }

        private Type _RoleMenuType;
        /// <summary>权限类</summary>
        public virtual Type RoleMenuType { get { return _RoleMenuType; } }

        Int32 hasInit = 0;
        void InitType()
        {
            if (hasInit > 0 || Interlocked.CompareExchange(ref hasInit, 1, 0) != 0) return;

            var btype = typeof(Object);
            var adminType = typeof(Administrator<,,,,>);
            var type = _AdministratorType = typeof(TAdministrator);
            while (type != typeof(Object))
            {
                // 找到五参数泛型，取泛型参数作为各自的类型
                if (type.IsGenericType && type.GetGenericTypeDefinition() == adminType)
                {
                    var ts = type.GetGenericArguments();
                    _RoleType = ts[1];
                    _MenuType = ts[2];
                    _RoleMenuType = ts[3];
                    _LogType = ts[4];
                    break;
                }
                type = type.BaseType;
            }
        }
        #endregion

        #region 页面和表单
        /// <summary>创建管理页控制器</summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("该成员在后续版本中讲不再被支持！")]
        public virtual IManagePage CreatePage(Control container, Type entityType)
        {
            IManagePage page = GetService<IManagePage>();
            page.Init(container, entityType);

            return page;
        }

        /// <summary>创建实体表单控制器</summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("该成员在后续版本中讲不再被支持！")]
        public virtual IEntityForm CreateForm(Control container, Type entityType)
        {
            IEntityForm form = GetService<IEntityForm>();
            form.Init(container, entityType);

            return form;
        }
        #endregion

        #region 菜单
        private IMenu _MenuRoot;
        /// <summary>菜单根</summary>
        public virtual IMenu MenuRoot
        {
            get
            {
                if (_MenuRoot == null) _MenuRoot = MenuType.GetValue("Root") as IMenu;
                return _MenuRoot;
            }
        }

        /// <summary>根据编号找到菜单</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IMenu FindByMenuID(Int32 id)
        {
            if (id < 1) return null;

            var eop = EntityFactory.CreateOperate(MenuType);
            //return eop.FindByKey(id) as IMenu;
            return eop.FindWithCache(eop.Unique.Name, id) as IMenu;
        }

        /// <summary>获取指定菜单下，当前用户有权访问的子菜单。</summary>
        /// <param name="menuid"></param>
        /// <returns></returns>
        public IList<IMenu> GetMySubMenus(Int32 menuid)
        {
            var provider = this as ICommonManageProvider;
            var root = provider.MenuRoot;

            // 当前用户
            var admin = provider.Current as IAdministrator;
            if (admin == null || admin.Role == null) return null;

            IMenu menu = null;

            // 找到菜单
            if (menuid > 0) menu = FindByMenuID(menuid);

            if (menu == null)
            {
                menu = root;
                if (menu == null || menu.Childs == null || menu.Childs.Count < 1) return null;
                menu = menu.Childs[0];
                if (menu == null) return null;
            }

            return admin.Role.GetMySubMenus(menu.ID);
        }
        #endregion
    }
}