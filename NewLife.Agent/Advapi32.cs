using System;
using System.Runtime.InteropServices;

namespace NewLife.Agent
{
    internal class SafeServiceHandle : SafeHandle
    {
        public override Boolean IsInvalid
        {
            get
            {
                if (!(DangerousGetHandle() == IntPtr.Zero))
                {
                    return DangerousGetHandle() == new IntPtr(-1);
                }
                return true;
            }
        }

        internal SafeServiceHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        protected override Boolean ReleaseHandle() => Advapi32.CloseServiceHandle(handle);
    }

    class Advapi32
    {
        internal class ControlOptions
        {
            internal const Int32 CONTROL_CONTINUE = 3;

            internal const Int32 CONTROL_INTERROGATE = 4;

            internal const Int32 CONTROL_PAUSE = 2;

            internal const Int32 CONTROL_POWEREVENT = 13;

            internal const Int32 CONTROL_SESSIONCHANGE = 14;

            internal const Int32 CONTROL_SHUTDOWN = 5;

            internal const Int32 CONTROL_STOP = 1;
        }

        internal class ServiceOptions
        {
            internal const Int32 SERVICE_QUERY_CONFIG = 1;

            internal const Int32 SERVICE_CHANGE_CONFIG = 2;

            internal const Int32 SERVICE_QUERY_STATUS = 4;

            internal const Int32 SERVICE_ENUMERATE_DEPENDENTS = 8;

            internal const Int32 SERVICE_START = 16;

            internal const Int32 SERVICE_STOP = 32;

            internal const Int32 SERVICE_PAUSE_CONTINUE = 64;

            internal const Int32 SERVICE_INTERROGATE = 128;

            internal const Int32 SERVICE_USER_DEFINED_CONTROL = 256;

            internal const Int32 SERVICE_ALL_ACCESS = 983551;

            internal const Int32 STANDARD_RIGHTS_DELETE = 65536;

            internal const Int32 STANDARD_RIGHTS_REQUIRED = 983040;
        }

        internal class ServiceControllerOptions
        {
            internal const Int32 SC_ENUM_PROCESS_INFO = 0;

            internal const Int32 SC_MANAGER_CONNECT = 1;

            internal const Int32 SC_MANAGER_CREATE_SERVICE = 2;

            internal const Int32 SC_MANAGER_ENUMERATE_SERVICE = 4;

            internal const Int32 SC_MANAGER_LOCK = 8;

            internal const Int32 SC_MANAGER_MODIFY_BOOT_CONFIG = 32;

            internal const Int32 SC_MANAGER_QUERY_LOCK_STATUS = 16;

            internal const Int32 SC_MANAGER_ALL = 983103;
        }

        internal struct SERVICE_STATUS
        {
            public Int32 serviceType;

            public ServiceControllerStatus currentState;

            public Int32 controlsAccepted;

            public Int32 win32ExitCode;

            public Int32 serviceSpecificExitCode;

            public Int32 checkPoint;

            public Int32 waitHint;
        }

        public enum ServiceControllerStatus : Int32
        {
            ContinuePending = 5,
            Paused = 7,
            PausePending = 6,
            Running = 4,
            StartPending = 2,
            Stopped = 1,
            StopPending = 3
        }

        public delegate void ServiceMainCallback(Int32 argCount, IntPtr argPointer);

        public struct SERVICE_TABLE_ENTRY
        {
            public IntPtr name;

            public ServiceMainCallback callback;
        }

        public struct SERVICE_DESCRIPTION
        {
            public String Description;
        }

        public delegate Int32 ServiceControlCallbackEx(Int32 control, Int32 eventType, IntPtr eventData, IntPtr eventContext);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Boolean CloseServiceHandle(IntPtr handle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern Boolean ControlService(SafeServiceHandle serviceHandle, Int32 control, SERVICE_STATUS* pStatus);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenSCManagerW", SetLastError = true)]
        internal static extern IntPtr OpenSCManager(String machineName, String databaseName, Int32 access);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenServiceW", SetLastError = true)]
        internal static extern IntPtr OpenService(SafeServiceHandle databaseHandle, String serviceName, Int32 access);

        [DllImport("Advapi32.dll")]
        internal static extern IntPtr CreateService(SafeServiceHandle databaseHandle, String lpSvcName, String lpDisplayName,
                                                    Int32 dwDesiredAccess, Int32 dwServiceType, Int32 dwStartType,
                                                    Int32 dwErrorControl, String lpPathName, String lpLoadOrderGroup,
                                                    Int32 lpdwTagId, String lpDependencies, String lpServiceStartName,
                                                    String lpPassword);
        [DllImport("advapi32.dll")]
        public static extern Int32 DeleteService(SafeServiceHandle serviceHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern Boolean QueryServiceStatus(SafeServiceHandle serviceHandle, SERVICE_STATUS* pStatus);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "StartServiceW", SetLastError = true)]
        internal static extern Boolean StartService(SafeServiceHandle serviceHandle, Int32 argNum, IntPtr argPtrs);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public unsafe static extern Boolean SetServiceStatus(IntPtr serviceStatusHandle, SERVICE_STATUS* status);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr RegisterServiceCtrlHandlerEx(String serviceName, ServiceControlCallbackEx callback, IntPtr userData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern Boolean StartServiceCtrlDispatcher(IntPtr entry);

        [DllImport("advapi32.dll")]
        public static extern Boolean ChangeServiceConfig2(SafeServiceHandle serviceHandle, Int32 dwInfoLevel, IntPtr pInfo);
    }
}