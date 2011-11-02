using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using System.Web;
using NewLife.Web;
using System.Web.UI.WebControls;
using System.Linq;
using System.IO;
using XCode.DataAccessLayer;
using NewLife.Log;
using NewLife.Configuration;
using System.Threading;

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
        void Init(Control container, Type entityType);
    }

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
        public void Init(Control container, Type entityType)
        {
            if (container == null)
            {
                if (HttpContext.Current.Handler is Page) container = HttpContext.Current.Handler as Page;
            }

            Container = container;
            EntityType = entityType;

            Init();
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
            CheckStarting();

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

        #region 系统启动中
        static Boolean SystemStarted = false;
        /// <summary>
        /// 检查系统是否启动中，如果启动中，则显示进度条
        /// </summary>
        public static void CheckStarting()
        {
            if (HttpContext.Current == null) return;

            HttpRequest Request = HttpContext.Current.Request;
            HttpResponse Response = HttpContext.Current.Response;

            // 在用Flush前用一次Session，避免可能出现的问题
            String sessionid = HttpContext.Current.Session.SessionID;

            // 只处理GET，因为处理POST可能丢失提交的表单数据
            if (Request.HttpMethod != "GET") return;

            if (SystemStarted) return;
            SystemStarted = true;

            #region 输出脚本
            StringBuilder sb = new StringBuilder();
            sb.Append(@"<html><head>
<script language=""javascript"" type=""text/javascript"">
var t_id = setInterval(animate,20);
var pos=0;var dir=2;var len=0;

function animate(){
    var elem = document.getElementById('progress');
    if(elem != null) {
        if (pos==0) len += dir;
        if (len>32 || pos>79) pos += dir;
        if (pos>79) len -= dir;
        if (pos>79 && len==0) pos=0;
        elem.style.left = pos;
        elem.style.width = len;
    }
}
function stopAnimate(){
    clearInterval(t_id);
    var elem = document.getElementById('loader_container');
    elem.style.display='none';
}
</script>

<style>
#loader_container {text-align:center; position:absolute; top:40%; width:100%; left: 0;}
#loader {font-family:Tahoma, Helvetica, sans; font-size:11.5px; color:#000000; background-color:#FFFFFF; padding:10px 0 16px 0; margin:0 auto; display:block; width:130px; border:1px solid #5a667b; text-align:left; z-index:2;}
#progress {height:5px; font-size:1px; width:1px; position:relative; top:1px; left:0px; background-color:#8894a8;}
#loader_bg {background-color:#e4e7eb; position:relative; top:8px; left:8px; height:7px; width:113px; font-size:1px;}
</style>

</head><body>
<div id=loader_container>
    <div id=loader>
    <div align=center>系统正在启动中 ...</div>
    <div id=loader_bg><div id=progress> </div></div>
    </div>
</div>
<div style=""position:absolute; left:1em; top:1em; width:320px; padding:.3em; background:#900; color:#fff; display:none;"">
<strong>系统启动发生异常</strong>
<div id=""start_fail""></div>
</div>

<script type=""text/javascript"" language=""javascript"">
(function(w){
    var xhr;
    if(w.XMLHttpRequest && !w.ActiveXObject){
        xhr=new w.XMLHttpRequest();
    }else{
        try{
            xhr=new w.ActiveXObject('Microsoft.XMLHTTP');
        }catch(e){}
    }

    if(xhr){
        xhr.open('GET','?ajax=1');
        xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
        xhr.onreadystatechange=function(){
            if(xhr.readyState===4){
                //只有返回http 200时才表示正常
                if(xhr.status===200){
                    xhr=null;
                    location.reload();
                }else{
                    //否则输出http状态码和状态说明,以及返回的html
                    stopAnimate();
                    var ele=document.getElementById('start_fail');
                    ele.innerHTML='HTTP '+xhr.status+' '+xhr.statusText+'<br/>'+xhr.responseText;
                    var par=ele.parentNode;
                    if(par){
                        par.style.display='block';
                    }
                }
                xhr=null;
            }
        };
        xhr.send();
    }else{
        // 不支持的浏览器将直接刷新 不再显示动画
        location.reload();
    }
})(window);
</script>
</body></html>");
            //还可以选择使用<script src="?script=1" type="text/javascript"></script>的方式 只是对返回内容有要求

            //sb.AppendLine("function remove_loading() {");
            //sb.AppendLine(" this.clearInterval(t_id);");
            //sb.AppendLine("var targelem = document.getElementById('loader_container');");
            //sb.AppendLine("targelem.style.display='none';");
            //sb.AppendLine("targelem.style.visibility='hidden';");
            //sb.AppendLine("}");
            //sb.AppendLine("document.onload=function(){ location.reload(); }");
            //sb.AppendLine("document.onload=remove_loading;");

            Response.Write(sb.ToString());
            Response.Flush();
            Response.End();
            #endregion
        }
        #endregion
    }
}