using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web.Hosting;
using System.Security.Permissions;
using System.Globalization;
using System.Web;
using System.Security.Principal;

namespace NewLife.Net.Http
{
    sealed class Host : MarshalByRefObject, IRegisteredObject
    {
        private bool _disableDirectoryListing;
        private string _installPath;
        private string _lowerCasedClientScriptPathWithTrailingSlash;
        private string _lowerCasedVirtualPath;
        private string _lowerCasedVirtualPathWithTrailingSlash;
        private volatile int _pendingCallsCount;
        private string _physicalClientScriptPath;
        private string _physicalPath;
        private int _port;
        private bool _requireAuthentication;
        private WebServer _server;
        private string _virtualPath;

        public Host()
        {
            HostingEnvironment.RegisterObject(this);
        }

        private void AddPendingCall() { Interlocked.Increment(ref _pendingCallsCount); }

        public void Configure(WebServer server, int port, string virtualPath, string physicalPath, bool requireAuthentication) { Configure(server, port, virtualPath, physicalPath, requireAuthentication, false); }

        public void Configure(WebServer server, int port, string virtualPath, string physicalPath, bool requireAuthentication, bool disableDirectoryListing)
        {
            _server = server;
            _port = port;
            _installPath = null;
            _virtualPath = virtualPath;
            _requireAuthentication = requireAuthentication;
            _disableDirectoryListing = disableDirectoryListing;
            _lowerCasedVirtualPath = CultureInfo.InvariantCulture.TextInfo.ToLower(_virtualPath);
            _lowerCasedVirtualPathWithTrailingSlash = virtualPath.EndsWith("/", StringComparison.Ordinal) ? virtualPath : (virtualPath + "/");
            _lowerCasedVirtualPathWithTrailingSlash = CultureInfo.InvariantCulture.TextInfo.ToLower(_lowerCasedVirtualPathWithTrailingSlash);
            _physicalPath = physicalPath;
            _physicalClientScriptPath = HttpRuntime.AspClientScriptPhysicalPath + @"\";
            _lowerCasedClientScriptPathWithTrailingSlash = CultureInfo.InvariantCulture.TextInfo.ToLower(HttpRuntime.AspClientScriptVirtualPath + "/");
        }

        public SecurityIdentifier GetProcessSID()
        {
            using (WindowsIdentity identity = new WindowsIdentity(_server.ProcessToken))
            {
                return identity.User;
            }
        }

        public IntPtr GetProcessToken()
        {
            new SecurityPermission(PermissionState.Unrestricted).Assert();
            return _server.ProcessToken;
        }

        public string GetProcessUser() { return _server.ProcessUser; }

        public override object InitializeLifetimeService() { return null; }

        public bool IsVirtualPathAppPath(string path)
        {
            if (path == null) return false;
            path = CultureInfo.InvariantCulture.TextInfo.ToLower(path);
            if (!(path == _lowerCasedVirtualPath)) return path == _lowerCasedVirtualPathWithTrailingSlash;
            return true;
        }

        public bool IsVirtualPathInApp(string path)
        {
            bool flag;
            return IsVirtualPathInApp(path, out flag);
        }

        public bool IsVirtualPathInApp(string path, out bool isClientScriptPath)
        {
            isClientScriptPath = false;
            if (path != null)
            {
                path = CultureInfo.InvariantCulture.TextInfo.ToLower(path);
                if (_virtualPath == "/" && path.StartsWith("/", StringComparison.Ordinal))
                {
                    if (path.StartsWith(_lowerCasedClientScriptPathWithTrailingSlash, StringComparison.Ordinal)) isClientScriptPath = true;
                    return true;
                }
                if (path.StartsWith(_lowerCasedVirtualPathWithTrailingSlash, StringComparison.Ordinal)) return true;
                if (path == _lowerCasedVirtualPath) return true;
                if (path.StartsWith(_lowerCasedClientScriptPathWithTrailingSlash, StringComparison.Ordinal))
                {
                    isClientScriptPath = true;
                    return true;
                }
            }
            return false;
        }

        public void ProcessRequest(WebSession conn)
        {
            AddPendingCall();
            try
            {
                new WebRequest(this, conn).Process();
            }
            finally
            {
                RemovePendingCall();
            }
        }

        private void RemovePendingCall() { Interlocked.Decrement(ref _pendingCallsCount); }

        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        public void Shutdown() { HostingEnvironment.InitiateShutdown(); }

        void IRegisteredObject.Stop(bool immediate)
        {
            if (_server != null) _server.HostStopped();
            WaitForPendingCallsToFinish();
            HostingEnvironment.UnregisterObject(this);
        }

        private void WaitForPendingCallsToFinish()
        {
            while (_pendingCallsCount > 0)
            {
                Thread.Sleep(250);
            }
        }

        public bool DisableDirectoryListing { get { return _disableDirectoryListing; } }

        public string InstallPath { get { return _installPath; } }

        public string NormalizedClientScriptPath { get { return _lowerCasedClientScriptPathWithTrailingSlash; } }

        public string NormalizedVirtualPath { get { return _lowerCasedVirtualPathWithTrailingSlash; } }

        public string PhysicalClientScriptPath { get { return _physicalClientScriptPath; } }

        public string PhysicalPath { get { return _physicalPath; } }

        public int Port { get { return _port; } }

        public bool RequireAuthentication { get { return _requireAuthentication; } }

        public string VirtualPath { get { return _virtualPath; } }
    }
}