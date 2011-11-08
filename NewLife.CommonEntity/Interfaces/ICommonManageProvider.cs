using System;
using System.Web.UI;
using System.Web;

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

        #region 方法
        /// <summary>当前用户</summary>
        new IAdministrator Current { get; }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        new IAdministrator FindByID(Object userid);

        /// <summary>
        /// 根据用户帐号查找
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        new IAdministrator FindByAccount(String account);

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        new IAdministrator Login(String account, String password);
        #endregion

        #region 页面和表单
        /// <summary>
        /// 创建管理页控制器
        /// </summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        IManagerPage CreatePage(Control container, Type entityType);

        /// <summary>
        /// 创建实体表单控制器
        /// </summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        IEntityForm CreateForm(Control container, Type entityType);
        #endregion

        #region 菜单
        /// <summary>菜单根</summary>
        IMenu MenuRoot { get; }
        #endregion
    }

    /// <summary>通用实体类管理提供者</summary>
    public class CommonManageProvider : CommonManageProvider<Administrator> { }

    /// <summary>通用实体类管理提供者</summary>
    /// <typeparam name="TAdministrator">管理员类</typeparam>
    public class CommonManageProvider<TAdministrator> : ICommonManageProvider where TAdministrator : Administrator<TAdministrator>, new()
    {
        #region 静态实例
        /// <summary>当前提供者</summary>
        public static ICommonManageProvider Provider { get { return CommonService.Resolve<ICommonManageProvider>(); } }
        #endregion

        #region IManageProvider 接口
        /// <summary>管理用户类</summary>
        Type IManageProvider.ManageUserType { get { return AdminstratorType; } }

        /// <summary>当前用户</summary>
        IManageUser IManageProvider.Current { get { return Current; } }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        IManageUser IManageProvider.FindByID(Object userid) { return FindByID((Int32)userid); }

        /// <summary>
        /// 根据用户帐号查找
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        IManageUser IManageProvider.FindByAccount(String account) { return FindByAccount(account); }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        IManageUser IManageProvider.Login(String account, String password) { return Login(account, password); }
        #endregion

        #region 类型
        /// <summary>管理员类</summary>
        public virtual Type AdminstratorType { get { return typeof(TAdministrator); } }

        /// <summary>日志类</summary>
        public virtual Type LogType { get { return typeof(Log); } }

        /// <summary>菜单类</summary>
        public virtual Type MenuType { get { return typeof(Menu); } }

        /// <summary>角色类</summary>
        public virtual Type RoleType { get { return typeof(Role); } }

        /// <summary>权限类</summary>
        public virtual Type RoleMenuType { get { return typeof(RoleMenu); } }
        #endregion

        #region ICommonManageProvider接口
        /// <summary>当前用户</summary>
        IAdministrator ICommonManageProvider.Current { get { return Current; } }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        IAdministrator ICommonManageProvider.FindByID(Object userid) { return FindByID((Int32)userid); }

        /// <summary>
        /// 根据用户帐号查找
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        IAdministrator ICommonManageProvider.FindByAccount(String account) { return FindByAccount(account); }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        IAdministrator ICommonManageProvider.Login(String account, String password) { return Login(account, password); }
        #endregion

        #region 方法
        /// <summary>当前用户</summary>
        public virtual TAdministrator Current { get { return Administrator<TAdministrator>.Current; } }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public virtual TAdministrator FindByID(Int32 userid) { return Administrator<TAdministrator>.FindByID(userid); }

        /// <summary>
        /// 根据用户帐号查找
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public virtual TAdministrator FindByAccount(String account) { return Administrator<TAdministrator>.FindByName(account); }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual TAdministrator Login(String account, String password) { return Administrator<TAdministrator>.Login(account, password); }
        #endregion

        #region 页面和表单
        /// <summary>
        /// 创建管理页控制器
        /// </summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public virtual IManagerPage CreatePage(Control container, Type entityType)
        {
            Object key = typeof(IManagerPage);
            if (HttpContext.Current.Items[key] != null) return HttpContext.Current.Items[key] as IManagerPage;

            IManagerPage page = CommonService.Resolve<IManagerPage>();
            page.Init(container, entityType);

            HttpContext.Current.Items[key] = page;

            return page;
        }

        /// <summary>
        /// 创建实体表单控制器
        /// </summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public virtual IEntityForm CreateForm(Control container, Type entityType)
        {
            Object key = typeof(IEntityForm);
            if (HttpContext.Current.Items[key] != null) return HttpContext.Current.Items[key] as IEntityForm;

            IEntityForm form = CommonService.Resolve<IEntityForm>();
            form.Init(container, entityType);

            HttpContext.Current.Items[key] = form;

            return form;
        }
        #endregion

        #region 菜单
        /// <summary>菜单根</summary>
        IMenu ICommonManageProvider.MenuRoot { get { return Menu.Root; } }
        #endregion
    }
}