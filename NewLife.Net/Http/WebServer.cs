using System;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Web.Hosting;
using NewLife.Net.Sockets;

namespace NewLife.Net.Http
{
    /// <summary>可承载ASP.Net的Web服务器</summary>
    public class WebServer : NetServer<WebSession>
    {
        //TODO 未实现Web服务端

        #region 属性
        //private Dictionary<Int32, HttpSession> _Sessions;
        ///// <summary>会话集合</summary>
        //public IDictionary<Int32, HttpSession> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<int, HttpSession>()); } }

        private String _PhysicalPath = AppDomain.CurrentDomain.BaseDirectory;
        /// <summary>物理路径。默认当前路径。</summary>
        public String PhysicalPath { get { return _PhysicalPath; } set { _PhysicalPath = value; } }

        private String _VirtualPath = "/";
        /// <summary>序列路径。默认根路径/</summary>
        public String VirtualPath { get { return _VirtualPath; } set { _VirtualPath = value; } }

        private Boolean _RequireAuthentication;
        /// <summary>要求验证</summary>
        public Boolean RequireAuthentication { get { return _RequireAuthentication; } set { _RequireAuthentication = value; } }

        private Boolean _DisableDirectoryListing;
        /// <summary>关闭目录浏览</summary>
        public Boolean DisableDirectoryListing { get { return _DisableDirectoryListing; } set { _DisableDirectoryListing = value; } }

        private ApplicationManager _AppManager;
        /// <summary>应用管理器</summary>
        public ApplicationManager AppManager { get { return _AppManager ?? (_AppManager = ApplicationManager.GetApplicationManager()); } }

        private Host _Host;
        /// <summary>属性说明</summary>
        private Host Host
        {
            get
            {
                // 这里本应该注意线程安全，但是因为冲突可能性太小，故不处理
                if (_Host == null)
                {
                    String appId = (VirtualPath + PhysicalPath).ToLowerInvariant().GetHashCode().ToString("x", CultureInfo.InvariantCulture);
                    var host = (Host)AppManager.CreateObject(appId, typeof(Host), VirtualPath, PhysicalPath, false);
                    host.Configure(this, Port, VirtualPath, PhysicalPath, RequireAuthentication, DisableDirectoryListing);
                    _Host = host;

                }
                return _Host;
            }
        }
        #endregion

        #region 进程信息
        private IntPtr _ProcessToken;
        /// <summary>进程标识</summary>
        public IntPtr ProcessToken { get { return _ProcessToken; } }

        private String _ProcessUser;
        /// <summary>进程用户</summary>
        public String ProcessUser { get { return _ProcessUser; } }

        private void ObtainProcessToken()
        {
            if (ImpersonateSelf(2))
            {
                OpenThreadToken(GetCurrentThread(), 0xf01ff, true, ref _ProcessToken);
                RevertToSelf();
                _ProcessUser = WindowsIdentity.GetCurrent().Name;
            }
        }
        [DllImport("ADVAPI32.DLL", SetLastError = true)]
        private static extern bool ImpersonateSelf(int level);
        [DllImport("KERNEL32.DLL", SetLastError = true)]
        private static extern IntPtr GetCurrentThread();
        [DllImport("ADVAPI32.DLL", SetLastError = true)]
        private static extern int OpenThreadToken(IntPtr thread, int access, bool openAsSelf, ref IntPtr hToken);
        [DllImport("ADVAPI32.DLL", SetLastError = true)]
        private static extern int RevertToSelf();
        #endregion

        #region 构造
        /// <summary>实例化一个Web服务器</summary>
        public WebServer()
        {
            Port = 23;
            ProtocolType = ProtocolType.Tcp;

            ObtainProcessToken();
        }
        #endregion

        #region 会话
        /// <summary>添加会话。子类可以在添加会话前对会话进行一些处理</summary>
        /// <param name="session"></param>
        protected override void AddSession(INetSession session)
        {
            var s = session as WebSession;
            if (s != null) s.Server = this;

            base.AddSession(session);
        }
        #endregion

        #region 方法
        internal void HostStopped() { _Host = null; }
        #endregion
    }
}