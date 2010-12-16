using System;
using System.IO;
using System.Web;
using System.Web.UI;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity.Web
{
    /// <summary>
    /// 页面基类
    /// </summary>
    public class WebPageBase : WebPageBase<Administrator> { }

    /// <summary>
    /// 指定具体管理员类的页面基类
    /// </summary>
    /// <typeparam name="TAdminEntity"></typeparam>
    public class WebPageBase<TAdminEntity> : System.Web.UI.Page
        where TAdminEntity : Administrator<TAdminEntity>, new()
    {
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
        /// 检查权限
        /// </summary>
        /// <returns></returns>
        public virtual Boolean CheckPermission()
        {
            String name = PermissionName;
            if (String.IsNullOrEmpty(name)) return false;

            if (Administrator<TAdminEntity>.Current == null) return false;

            return Administrator<TAdminEntity>.Current.HasMenu(name);
        }

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
                return true;
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
    }
}
