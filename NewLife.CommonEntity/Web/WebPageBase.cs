using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using XCode;
using XCode.DataAccessLayer;
using NewLife.Log;
using NewLife.Web;

namespace NewLife.CommonEntity.Web
{
    /// <summary>
    /// 页面基类
    /// </summary>
    public class WebPageBase : WebPageBase<Administrator, Menu> { }

    /// <summary>
    /// 指定具体管理员类和菜单类的页面基类
    /// </summary>
    /// <typeparam name="TAdminEntity"></typeparam>
    /// <typeparam name="TMenuEntity"></typeparam>
    public class WebPageBase<TAdminEntity, TMenuEntity> : WebPageBase<TAdminEntity>
        where TAdminEntity : IAdministrator
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
                    _MyMenu = Menu<TMenuEntity>.Current;
                    if (_MyMenu == null) _MyMenu = Menu<TMenuEntity>.FindForPerssion(PermissionName);
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
    public class WebPageBase<TAdminEntity> : System.Web.UI.Page
        where TAdminEntity : IAdministrator
    {
        #region 权限控制
        private Boolean _ValidatePermission = true;
        /// <summary>是否检查权限</summary>
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
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreLoad(EventArgs e)
        {
            base.OnPreLoad(e);

            if (ValidatePermission && !CheckPermission())
            {
                Response.StatusCode = 403;
                Response.Write("没有权限访问该页！");
                Response.End();
            }
        }

        /// <summary>
        /// 检查权限，实际上就是Acquire(PermissionFlags.None)
        /// </summary>
        /// <returns></returns>
        public virtual Boolean CheckPermission()
        {
            return Acquire(PermissionFlags.None);
        }

        static HttpState<IAdministrator> http = new HttpState<IAdministrator>(typeof(TAdminEntity).Name + "_HttpStateKey");
        /// <summary>
        /// 当前管理员
        /// </summary>
        public virtual IAdministrator Current
        {
            get
            {
                return http == null ? null : http.Current;
            }
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

        #region 运行时输出
        private Int32 StartQueryTimes = DAL.QueryTimes;
        private Int32 StartExecuteTimes = DAL.ExecuteTimes;

        /// <summary>
        /// 是否输出执行时间
        /// </summary>
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
        protected virtual String RunTimeString { get { return "查询{0}次，执行{1}次，耗时{2}毫秒！"; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(HtmlTextWriter writer)
        {
            base.Render(writer);

            if (!IsWriteRunTime) return;

            TimeSpan ts = DateTime.Now - HttpContext.Current.Timestamp;

            //String str = "<!--查询{0}次，执行{1}次，耗时{2}毫秒！-->";
            //String str = "查询{0}次，执行{1}次，耗时{2}毫秒！";

            Response.Write(String.Format(RunTimeString, DAL.QueryTimes - StartQueryTimes, DAL.ExecuteTimes - StartExecuteTimes, ts.TotalMilliseconds));
        }
        #endregion
    }
}