using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Log;
using NewLife.Web;

namespace NewLife.CommonEntity
{
    /// <summary>管理页接口，用于控制页面权限等</summary>
    public interface IManagerPage
    {
        /// <summary>
        /// 使用控件容器和实体类初始化接口
        /// </summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        IManagerPage Init(Control container, Type entityType);

        /// <summary>导航 分为三级：栏目－子栏目－页面</summary>
        String Navigation { get; }

        /// <summary>当前管理员</summary>
        IAdministrator Current { get; }

        /// <summary>本页菜单</summary>
        IMenu CurrentMenu { get; set; }

        /// <summary>是否检查权限</summary>
        Boolean ValidatePermission { get; set; }

        /// <summary>申请指定操作的权限</summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        Boolean Acquire(PermissionFlags flag);

        /// <summary>申请指定权限项中指定操作的权限</summary>
        /// <param name="name"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        Boolean Acquire(String name, PermissionFlags flag);
    }

    /// <summary>管理页，用于控制页面权限等</summary>
    public class ManagerPage : IManagerPage
    {
        #region 属性
        private Control _Container;
        /// <summary>容器</summary>
        public Control Container
        {
            get { return _Container; }
            set { _Container = value; }
        }

        /// <summary>页面</summary>
        protected Page Page { get { return Container.Page; } }

        private Type _EntityType;
        /// <summary>实体类</summary>
        public Type EntityType
        {
            get { return _EntityType; }
            set { _EntityType = value; }
        }
        #endregion

        #region IManagerPage 成员
        /// <summary>
        /// 使用控件容器和实体类初始化接口
        /// </summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        public IManagerPage Init(Control container, Type entityType)
        {
            if (container == null)
            {
                if (HttpContext.Current.Handler is Page) container = HttpContext.Current.Handler as Page;
            }

            Container = container;
            EntityType = entityType;

            Init();

            return this;
        }
        #endregion

        #region 生命周期
        void Init()
        {
            Page.PreLoad += new EventHandler(OnPreLoad);
            Page.LoadComplete += new EventHandler(OnLoadComplete);
            //Page.PreRender += new EventHandler(Page_PreRender);
        }

        void OnPreLoad(object sender, EventArgs e)
        {
            try
            {
                if (!CheckLogin())
                {
                    Response.StatusCode = 403;
                    Response.StatusDescription = "没有登录！";
                    Response.Write("没有登录！");
                    Response.End();
                }
                else if (!CheckPermission())
                {
                    Response.StatusCode = 403;
                    //Response.SubStatusCode = 15;
                    Response.StatusDescription = "没有权限访问该页！";
                    Response.Write("没有权限访问该页！");
                    Response.End();
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            if (!Page.IsPostBack)
            {
                GridView gv = ControlHelper.FindControlInPage<GridView>("gv");
                if (gv == null && EntityType != null) gv = ControlHelper.FindControlInPage<GridView>("gv" + EntityType.Name);
                if (gv == null) gv = ControlHelper.FindControlInPage<GridView>(null);
                // 最后一列是删除列，需要删除权限
                if (gv != null) gv.Columns[gv.Columns.Count - 1].Visible = Acquire(PermissionFlags.Delete);

                Label lbAdd = ControlHelper.FindControlInPage<Label>("lbAdd");
                // 添加按钮需要添加权限
                if (lbAdd != null) lbAdd.Visible = Acquire(PermissionFlags.Insert);
            }

            ObjectDataSource ods = ControlHelper.FindControlInPage<ObjectDataSource>("ods");
            if (ods == null && EntityType != null) ods = ControlHelper.FindControlInPage<ObjectDataSource>("ods" + EntityType.Name);
            if (ods == null) ods = ControlHelper.FindControlInPage<ObjectDataSource>(null);
            if (ods != null) FixObjectDataSource(ods);
        }

        void OnLoadComplete(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                // 添加按钮需要添加权限
                Control lbAdd = ControlHelper.FindControlInPage<Control>("lbAdd");
                if (lbAdd != null) lbAdd.Visible = Acquire(PermissionFlags.Insert);

                // 最后一列是删除列，需要删除权限
                GridView gv = ControlHelper.FindControlInPage<GridView>("gv");
                if (gv != null)
                {
                    DataControlField dcf = gv.Columns[gv.Columns.Count - 1];
                    if (dcf != null && dcf.HeaderText.Contains("删除")) dcf.Visible = Acquire(PermissionFlags.Delete);

                    dcf = gv.Columns[gv.Columns.Count - 2];
                    if (dcf != null && dcf.HeaderText.Contains("编辑"))
                    {
                        if (!Acquire(PermissionFlags.Update))
                        {
                            dcf.HeaderText = "查看";
                            if (dcf is HyperLinkField) (dcf as HyperLinkField).Text = "查看";
                        }
                    }
                }
            }
        }

        void OnRender(object sender, EventArgs e)
        {
        }
        #endregion

        #region 页面属性
        /// <summary>请求</summary>
        public HttpRequest Request { get { return Page.Request; } }

        /// <summary>响应</summary>
        public HttpResponse Response { get { return Page.Response; } }

        /// <summary>当前管理员</summary>
        public virtual IAdministrator Current { get { return CommonManageProvider.Provider.Current; } }

        /// <summary>导航 分为三级：栏目－子栏目－页面</summary>
        public virtual String Navigation
        {
            get
            {
                if (CurrentMenu == null) return null;

                return CurrentMenu.GetFullPath(true, " - ", item => String.Format("[{0}]", item.Name));
            }
        }

        private List<String> hasLoaded = new List<String>();
        private IMenu _CurrentMenu;
        /// <summary>本页菜单</summary>
        public virtual IMenu CurrentMenu
        {
            get
            {
                if (_CurrentMenu == null && !hasLoaded.Contains("CurrentMenu"))
                {
                    _CurrentMenu = Current.FindPermissionMenu(PermissionName);
                    hasLoaded.Add("CurrentMenu");
                }
                return _CurrentMenu;
            }
            set { _CurrentMenu = value; }
        }
        #endregion

        #region 权限控制
        private Boolean _ValidatePermission = true;
        /// <summary>是否检查权限</summary>
        public virtual Boolean ValidatePermission { get { return _ValidatePermission; } set { _ValidatePermission = value; } }

        /// <summary>权限名。默认是页面标题</summary>
        public virtual String PermissionName
        {
            get
            {
                // 默认使用标题
                if (!String.IsNullOrEmpty(Page.Title)) return Page.Title;

                // 计算 目录/文件 的形式
                String p = Page.Request.PhysicalPath;
                String dirName = new DirectoryInfo(Path.GetDirectoryName(p)).Name;
                String fileName = Path.GetFileNameWithoutExtension(p);

                return String.Format(@"{0}/{1}", dirName, fileName);
            }
        }

        /// <summary>
        /// 检查是否已登录
        /// </summary>
        /// <returns></returns>
        public virtual Boolean CheckLogin()
        {
            // 当前管理员
            IAdministrator entity = Current;
            if (entity == null) return false;

            return true;
        }

        /// <summary>
        /// 检查权限，实际上就是Acquire(PermissionFlags.None)
        /// </summary>
        /// <returns></returns>
        public virtual Boolean CheckPermission()
        {
            if (!ValidatePermission) return true;

            return Acquire(PermissionFlags.None);
        }

        /// <summary>
        /// 申请指定操作的权限
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public virtual Boolean Acquire(PermissionFlags flag)
        {
            // 当前管理员
            IAdministrator admin = Current;
            if (admin == null) return false;

            IMenu menu = CurrentMenu;
            if (menu == null) return false;

            return admin.Acquire(menu.ID, flag);
        }

        /// <summary>
        /// 申请指定操作的权限
        /// </summary>
        /// <param name="name"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public virtual Boolean Acquire(String name, PermissionFlags flag)
        {
            // 当前管理员
            IAdministrator admin = Current;
            if (admin == null) return false;

            return admin.Acquire(name, flag);
        }

        /// <summary>
        /// 申请指定操作的权限
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Boolean Acquire(String name)
        {
            return Acquire(name, PermissionFlags.None);
        }
        #endregion

        #region ObjectDataSource完善
        /// <summary>
        /// 如果没有设置TypeName，则说明该控件还没有人工控制，采用自动控制
        /// </summary>
        /// <param name="ods"></param>
        void FixObjectDataSource(ObjectDataSource ods)
        {
            if (!String.IsNullOrEmpty(ods.TypeName)) return;

            if (EntityType != null) ods.DataObjectTypeName = ods.TypeName = EntityType.FullName;
            if (String.IsNullOrEmpty(ods.SelectMethod))
            {
                ods.SelectMethod = "Search";
                ods.EnablePaging = true;
                if (String.IsNullOrEmpty(ods.SelectCountMethod)) ods.SelectCountMethod = "SearchCount";
                if (String.IsNullOrEmpty(ods.SortParameterName)) ods.SortParameterName = "orderClause";
                if (String.IsNullOrEmpty(ods.UpdateMethod)) ods.UpdateMethod = "Update";
                if (String.IsNullOrEmpty(ods.DeleteMethod)) ods.DeleteMethod = "Delete";
            }
        }
        #endregion
    }
}