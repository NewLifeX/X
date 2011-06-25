using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Configuration;
using NewLife.IO;
using NewLife.Log;
using XCode.DataAccessLayer;

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
        //#region 菜单
        ///// <summary>
        ///// 导航 分为三级：栏目－子栏目－页面
        ///// </summary>
        //public virtual String Navigation
        //{
        //    get
        //    {
        //        if (MyMenu == null) return null;

        //        // 无限路径
        //        EntityList<TMenuEntity> list = MyMenu.GetFullPath(true);
        //        //StringBuilder sb = new StringBuilder();
        //        //foreach (TMenuEntity item in list)
        //        //{
        //        //    if (sb.Length > 0) sb.Append(" - ");
        //        //    sb.AppendFormat("[{0}]", item.Name);
        //        //}

        //        //return sb.ToString();

        //        return MyMenu.GetFullPath(true, " - ", delegate(TMenuEntity item)
        //        {
        //            return String.Format("[{0}]", item.Name);
        //        });
        //    }
        //}

        //private List<String> hasLoaded = new List<String>();
        //private TMenuEntity _MyMenu;
        ///// <summary>本页菜单</summary>
        //public virtual TMenuEntity MyMenu
        //{
        //    get
        //    {
        //        if (_MyMenu == null && !hasLoaded.Contains("MyMenu"))
        //        {
        //            _MyMenu = Menu<TMenuEntity>.FindForPerssion(PermissionName);
        //            if (_MyMenu == null) _MyMenu = Menu<TMenuEntity>.Current;
        //            hasLoaded.Add("MyMenu");
        //        }
        //        return _MyMenu;
        //    }
        //    set { _MyMenu = value; }
        //}
        //#endregion

        //#region 权限控制
        ///// <summary>
        ///// 申请指定操作的权限
        ///// </summary>
        ///// <param name="flag"></param>
        ///// <returns></returns>
        //public override Boolean Acquire(PermissionFlags flag)
        //{
        //    if (MyMenu == null) return base.Acquire(flag);

        //    // 当前管理员
        //    IAdministrator entity = Current;
        //    if (entity == null) return false;

        //    return entity.Acquire(MyMenu.ID, flag);
        //}
        //#endregion
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
                //EntityList<TMenuEntity> list = MyMenu.GetFullPath(true);
                //StringBuilder sb = new StringBuilder();
                //foreach (TMenuEntity item in list)
                //{
                //    if (sb.Length > 0) sb.Append(" - ");
                //    sb.AppendFormat("[{0}]", item.Name);
                //}

                //return sb.ToString();

                return MyMenu.GetFullPath(true, " - ", delegate(IMenu item)
                {
                    return String.Format("[{0}]", item.Name);
                });
            }
        }

        private List<String> hasLoaded = new List<String>();
        private IMenu _MyMenu;
        /// <summary>本页菜单</summary>
        public virtual IMenu MyMenu
        {
            get
            {
                if (_MyMenu == null && !hasLoaded.Contains("MyMenu"))
                {
                    //_MyMenu = Menu<TMenuEntity>.FindForPerssion(PermissionName);
                    //_MyMenu = EntityShip.Invoke<IMenu>("FindForPerssion", PermissionName) as IMenu;

                    _MyMenu = Current.FindPermissionMenu(PermissionName);
                    //if (_MyMenu == null) _MyMenu = Menu.CurrentMenu;
                    hasLoaded.Add("MyMenu");
                }
                return _MyMenu;
            }
            set { _MyMenu = value; }
        }
        #endregion

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
            // 当前管理员
            IAdministrator admin = Current;
            if (admin == null) return false;

            IMenu menu = MyMenu;
            //if (menu == null)
            //{
            //    String name = PermissionName;
            //    if (String.IsNullOrEmpty(name)) return false;

            //    // 当前权限菜单
            //    menu = admin.FindPermissionMenu(name);
            //}

            if (menu == null) return false;

            return admin.Acquire(menu.ID, flag);
        }
        #endregion

        #region 登录用户控制
        ///// <summary>
        ///// Http状态，名称必须和管理员类中一致
        ///// </summary>
        //static HttpState<IAdministrator> http = new HttpState<IAdministrator>("Admin");
        /// <summary>
        /// 当前管理员
        /// </summary>
        public virtual IAdministrator Current
        {
            get
            {
                //return http == null ? null : http.Current;
                //return (IAdministrator)Thread.CurrentPrincipal;

                return Administrator.CurrentAdministrator;
            }
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreLoad(EventArgs e)
        {
            CheckStarting();

            Thread.CurrentPrincipal = (IPrincipal)Current;
            //Thread.CurrentPrincipal = (IPrincipal)http.Current;

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
                    //Response.SubStatusCode = 15;
                    Response.StatusDescription = "没有权限访问该页！";
                    Response.Write("没有权限访问该页！");
                    Response.End();
                }
            }
            catch (ThreadAbortException) { }
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

        #region 压缩ViewState
        /// <summary>
        /// 设定序列化后的字符串长度为多少后启用压缩
        /// </summary>
        private static Int32 LimitLength = 1096;

        /// <summary>
        /// 是否压缩ViewState
        /// </summary>
        protected virtual Boolean CompressViewState { get { return Config.GetConfig<Boolean>("NewLife.CommonEntity.CompressViewState", true); } }

        /// <summary>
        /// 重写保存页的所有视图状态信息
        /// </summary>
        /// <param name="state">要在其中存储视图状态信息的对象</param>
        protected override void SavePageStateToPersistenceMedium(Object state)
        {
            if (!CompressViewState)
            {
                base.SavePageStateToPersistenceMedium(state);
                return;
            }

            MemoryStream ms = new MemoryStream();
            new LosFormatter().Serialize(ms, state);

            String vs = null;

            //判断序列化对象的字符串长度是否超出定义的长度界限
            if (ms.Length > LimitLength)
            {
                MemoryStream ms2 = new MemoryStream();
                // 必须移到第一位，否则后面读不到数据
                ms.Position = 0;
                IOHelper.Compress(ms, ms2);
                vs = "1$" + Convert.ToBase64String(ms2.ToArray());
            }
            else
                vs = Convert.ToBase64String(ms.ToArray());

            //注册在页面储存ViewState状态的隐藏文本框，并将内容写入这个文本框
            ClientScript.RegisterHiddenField("__VSTATE", vs);
        }

        /// <summary>
        /// 重写将所有保存的视图状态信息加载到页面对象
        /// </summary>
        /// <returns>保存的视图状态</returns>
        protected override Object LoadPageStateFromPersistenceMedium()
        {
            if (!CompressViewState) return base.LoadPageStateFromPersistenceMedium();

            //使用Request方法获取序列化的ViewState字符串
            String vs = Request.Form.Get("__VSTATE");

            Byte[] bts = null;

            if (vs.StartsWith("1$"))
                bts = IOHelper.Decompress(Convert.FromBase64String(vs.Substring(2)));
            else
                bts = Convert.FromBase64String(vs);

            //将指定的视图状态值转换为有限对象序列化 (LOS) 格式化的对象
            return new LosFormatter().Deserialize(new MemoryStream(bts));
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