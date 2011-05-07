using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Log;
using XCode;
using XCode.DataAccessLayer;
using System.Threading;
using NewLife.Web;
using System.Security.Principal;

namespace NewLife.CommonEntity.Web
{
    ///// <summary>
    ///// 页面基类
    ///// </summary>
    //public class WebPageBase : WebPageBase<Administrator, Menu> { }

    /// <summary>
    /// 指定具体管理员类和菜单类的页面基类
    /// </summary>
    /// <typeparam name="TAdminEntity"></typeparam>
    /// <typeparam name="TMenuEntity"></typeparam>
    public class WebPageBase<TAdminEntity, TMenuEntity> : WebPageBase<TAdminEntity>
        where TAdminEntity : Administrator<TAdminEntity>, new()
        where TMenuEntity : Menu<TMenuEntity>, new()
    {
        #region 菜单
        /// <summary>
        /// 导航 分为三级：栏目－子栏目－页面
        /// </summary>
        public virtual String Navigation
        {
            get
            {
                if (MyMenu == null) return null;

                // 无限路径
                EntityList<TMenuEntity> list = MyMenu.GetFullPath(true);
                //StringBuilder sb = new StringBuilder();
                //foreach (TMenuEntity item in list)
                //{
                //    if (sb.Length > 0) sb.Append(" - ");
                //    sb.AppendFormat("[{0}]", item.Name);
                //}

                //return sb.ToString();

                return MyMenu.GetFullPath(true, " - ", delegate(TMenuEntity item)
                {
                    return String.Format("[{0}]", item.Name);
                });
            }
        }

        private List<String> hasLoaded = new List<String>();
        private TMenuEntity _MyMenu;
        /// <summary>本页菜单</summary>
        public virtual TMenuEntity MyMenu
        {
            get
            {
                if (_MyMenu == null && !hasLoaded.Contains("MyMenu"))
                {
                    _MyMenu = Menu<TMenuEntity>.FindForPerssion(PermissionName);
                    if (_MyMenu == null) _MyMenu = Menu<TMenuEntity>.Current;
                    hasLoaded.Add("MyMenu");
                }
                return _MyMenu;
            }
            set { _MyMenu = value; }
        }
        #endregion

        #region 权限控制
        /// <summary>
        /// 申请指定操作的权限
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public override Boolean Acquire(PermissionFlags flag)
        {
            if (MyMenu == null) return base.Acquire(flag);

            // 当前管理员
            IAdministrator entity = Current;
            if (entity == null) return false;

            return entity.Acquire(MyMenu.ID, flag);
        }
        #endregion
    }

    /// <summary>
    /// 指定具体管理员类的页面基类
    /// </summary>
    /// <typeparam name="TAdminEntity"></typeparam>
    public class WebPageBase<TAdminEntity> : WebPageBase
        where TAdminEntity : Administrator<TAdminEntity>, new()
    {
        /// <summary>
        /// 当前管理员
        /// </summary>
        public override IAdministrator Current
        {
            get
            {
                return Administrator<TAdminEntity>.Current;
            }
        }
    }

    /// <summary>
    /// 页面基类
    /// </summary>
    public class WebPageBase : System.Web.UI.Page
    {
        #region 权限控制
        private Boolean _ValidatePermission = true;
        /// <summary>是否检查权限</summary>
        [Obsolete("后续版本将不再支持该属性，请重写CheckPermission来判断是否验证授权！")]
        public virtual Boolean ValidatePermission
        {
            get { return _ValidatePermission; }
            set { _ValidatePermission = value; }
        }

        /// <summary>
        /// 权限名。默认是页面标题
        /// </summary>
        public virtual String PermissionName
        {
            get
            {
                // 默认使用标题
                if (!String.IsNullOrEmpty(Title)) return Title;

                // 计算 目录/文件 的形式
                String p = Request.PhysicalPath;
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
            return Acquire(PermissionFlags.None);
        }

        /// <summary>
        /// 申请指定操作的权限
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public virtual Boolean Acquire(PermissionFlags flag)
        {
            String name = PermissionName;
            if (String.IsNullOrEmpty(name)) return false;

            // 当前管理员
            IAdministrator entity = Current;
            if (entity == null) return false;

            //return entity.HasMenu(name);

            // 当前权限菜单
            IEntity menu = entity.FindPermissionMenu(name);
            if (menu == null) return false;

            return entity.Acquire((Int32)menu["ID"], flag);
        }
        #endregion

        #region 登录用户控制
        /// <summary>
        /// Http状态，名称必须和管理员类中一致
        /// </summary>
        static HttpState<IAdministrator> http = new HttpState<IAdministrator>("Admin");
        /// <summary>
        /// 当前管理员
        /// </summary>
        public virtual IAdministrator Current
        {
            get
            {
                //return http == null ? null : http.Current;
                return (IAdministrator)Thread.CurrentPrincipal;
            }
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreLoad(EventArgs e)
        {
            //Thread.CurrentPrincipal = (IPrincipal)Current;
            Thread.CurrentPrincipal = (IPrincipal)http.Current;

            Unload += new EventHandler(WebPageBase_Unload);

            base.OnPreLoad(e);

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
                    Response.SubStatusCode = 15;
                    Response.StatusDescription = "没有权限访问该页！";
                    Response.Write("没有权限访问该页！");
                    Response.End();
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.ToString());
            }
        }

        void WebPageBase_Unload(object sender, EventArgs e)
        {
            Thread.CurrentPrincipal = null;
        }
        #endregion

        #region 运行时输出
        private Int32 StartQueryTimes = DAL.QueryTimes;
        private Int32 StartExecuteTimes = DAL.ExecuteTimes;

        /// <summary>
        /// 是否输出执行时间
        /// </summary>
        [Obsolete("后续版本将不再支持该属性，请重写CheckPermission来判断是否验证授权！")]
        protected virtual Boolean IsWriteRunTime
        {
            get
            {
                if (!Request.PhysicalPath.EndsWith(".aspx", StringComparison.Ordinal)) return false;
                return XTrace.Debug;
            }
        }

        /// <summary>
        /// 执行时间字符串
        /// </summary>
        [Obsolete("后续版本将不再支持该属性，请重写CheckPermission来判断是否验证授权！")]
        protected virtual String RunTimeString { get { return "查询{0}次，执行{1}次，耗时{2}毫秒！"; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(HtmlTextWriter writer)
        {
            Literal lt = FindControl("RunTime") as Literal;
            if (lt != null) WriteRunTime();

            base.Render(writer);

            if (lt == null) WriteRunTime();
        }

        /// <summary>
        /// 输出运行时间
        /// </summary>
        protected virtual void WriteRunTime()
        {
            if (!Request.PhysicalPath.EndsWith(".aspx", StringComparison.Ordinal)) return;
            if (!XTrace.Debug) return;

            //判断是否为Ajax 异步请求，以排除“Sys.WebForms.PageRequestManagerParserErrorException: 未能分析从服务器收到的消息 ”异常
            if (Request.Headers["X-MicrosoftAjax"] != null || Request.Headers["x-requested-with"] != null) return;

            TimeSpan ts = DateTime.Now - HttpContext.Current.Timestamp;

            String str = String.Format("查询{0}次，执行{1}次，耗时{2}毫秒！", DAL.QueryTimes - StartQueryTimes, DAL.ExecuteTimes - StartExecuteTimes, ts.TotalMilliseconds);

            Literal lt = FindControl("RunTime") as Literal;
            if (lt != null)
                lt.Text = str;
            else
                Response.Write(str);

        }
        #endregion
    }
}