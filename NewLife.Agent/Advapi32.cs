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
                if (!((this).DangerousGetHandle() == IntPtr.Zero))
                {
                    return (this).DangerousGetHandle() == new IntPtr(-1);
                }
                return true;
            }
        }

        internal SafeServiceHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        protected override Boolean ReleaseHandle()
        {
            return Advapi32.CloseServiceHandle(base.handle);
        }
    }

    class Advapi32
    {
        internal class AcceptOptions
        {
            internal const Int32 ACCEPT_POWEREVENT = 64;

            internal const Int32 ACCEPT_PAUSE_CONTINUE = 2;

            internal const Int32 ACCEPT_SESSIONCHANGE = 128;

            internal const Int32 ACCEPT_SHUTDOWN = 4;

            internal const Int32 ACCEPT_STOP = 1;
        }

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

        internal class ServiceConfigOptions
        {
            internal const Int32 SERVICE_CONFIG_DESCRIPTION = 1;

            internal const Int32 SERVICE_CONFIG_FAILURE_ACTIONS = 2;

            internal const Int32 SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3;
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

        internal class ServiceTypeOptions
        {
            internal const Int32 SERVICE_TYPE_ADAPTER = 4;

            internal const Int32 SERVICE_TYPE_FILE_SYSTEM_DRIVER = 2;

            internal const Int32 SERVICE_TYPE_INTERACTIVE_PROCESS = 256;

            internal const Int32 SERVICE_TYPE_KERNEL_DRIVER = 1;

            internal const Int32 SERVICE_TYPE_RECOGNIZER_DRIVER = 8;

            internal const Int32 SERVICE_TYPE_WIN32_OWN_PROCESS = 16;

            internal const Int32 SERVICE_TYPE_WIN32_SHARE_PROCESS = 32;

            internal const Int32 SERVICE_TYPE_WIN32 = 48;

            internal const Int32 SERVICE_TYPE_DRIVER = 11;

            internal const Int32 SERVICE_TYPE_ALL = 319;
        }

        internal class ServiceAccessOptions
        {
            internal const Int32 ACCESS_TYPE_CHANGE_CONFIG = 2;

            internal const Int32 ACCESS_TYPE_ENUMERATE_DEPENDENTS = 8;

            internal const Int32 ACCESS_TYPE_INTERROGATE = 128;

            internal const Int32 ACCESS_TYPE_PAUSE_CONTINUE = 64;

            internal const Int32 ACCESS_TYPE_QUERY_CONFIG = 1;

            internal const Int32 ACCESS_TYPE_QUERY_STATUS = 4;

            internal const Int32 ACCESS_TYPE_START = 16;

            internal const Int32 ACCESS_TYPE_STOP = 32;

            internal const Int32 ACCESS_TYPE_USER_DEFINED_CONTROL = 256;

            internal const Int32 ACCESS_TYPE_ALL = 983551;
        }

        internal class ServiceStartModes
        {
            internal const Int32 START_TYPE_BOOT = 0;

            internal const Int32 START_TYPE_SYSTEM = 1;

            internal const Int32 START_TYPE_AUTO = 2;

            internal const Int32 START_TYPE_DEMAND = 3;

            internal const Int32 START_TYPE_DISABLED = 4;
        }

        internal class ServiceState
        {
            internal const Int32 SERVICE_ACTIVE = 1;

            internal const Int32 SERVICE_INACTIVE = 2;

            internal const Int32 SERVICE_STATE_ALL = 3;
        }

        internal class StatusOptions
        {
            internal const Int32 STATUS_ACTIVE = 1;

            internal const Int32 STATUS_INACTIVE = 2;

            internal const Int32 STATUS_ALL = 3;
        }

        internal class ServiceControlStatus
        {
            internal const Int32 STATE_CONTINUE_PENDING = 5;

            internal const Int32 STATE_PAUSED = 7;

            internal const Int32 STATE_PAUSE_PENDING = 6;

            internal const Int32 STATE_RUNNING = 4;

            internal const Int32 STATE_START_PENDING = 2;

            internal const Int32 STATE_STOPPED = 1;

            internal const Int32 STATE_STOP_PENDING = 3;

            internal const Int32 ERROR_EXCEPTION_IN_SERVICE = 1064;
        }

        internal class ServiceStartErrorModes
        {
            internal const Int32 ERROR_CONTROL_CRITICAL = 3;

            internal const Int32 ERROR_CONTROL_IGNORE = 0;

            internal const Int32 ERROR_CONTROL_NORMAL = 1;

            internal const Int32 ERROR_CONTROL_SEVERE = 2;
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

        internal class PowerBroadcastStatus
        {
            internal const Int32 PBT_APMBATTERYLOW = 9;

            internal const Int32 PBT_APMOEMEVENT = 11;

            internal const Int32 PBT_APMPOWERSTATUSCHANGE = 10;

            internal const Int32 PBT_APMQUERYSUSPEND = 0;

            internal const Int32 PBT_APMQUERYSUSPENDFAILED = 2;

            internal const Int32 PBT_APMRESUMEAUTOMATIC = 18;

            internal const Int32 PBT_APMRESUMECRITICAL = 6;

            internal const Int32 PBT_APMRESUMESUSPEND = 7;

            internal const Int32 PBT_APMSUSPEND = 4;
        }

        internal class SessionStateChange
        {
            internal const Int32 WTS_CONSOLE_CONNECT = 1;

            internal const Int32 WTS_CONSOLE_DISCONNECT = 2;

            internal const Int32 WTS_REMOTE_CONNECT = 3;

            internal const Int32 WTS_REMOTE_DISCONNECT = 4;

            internal const Int32 WTS_SESSION_LOGON = 5;

            internal const Int32 WTS_SESSION_LOGOFF = 6;

            internal const Int32 WTS_SESSION_LOCK = 7;

            internal const Int32 WTS_SESSION_UNLOCK = 8;

            internal const Int32 WTS_SESSION_REMOTE_CONTROL = 9;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal class ENUM_SERVICE_STATUS
        {
            internal String serviceName;

            internal String displayName;

            internal Int32 serviceType;

            internal Int32 currentState;

            internal Int32 controlsAccepted;

            internal Int32 win32ExitCode;

            internal Int32 serviceSpecificExitCode;

            internal Int32 checkPoint;

            internal Int32 waitHint;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal class ENUM_SERVICE_STATUS_PROCESS
        {
            internal String serviceName;

            internal String displayName;

            internal Int32 serviceType;

            internal Int32 currentState;

            internal Int32 controlsAccepted;

            internal Int32 win32ExitCode;

            internal Int32 serviceSpecificExitCode;

            internal Int32 checkPoint;

            internal Int32 waitHint;

            internal Int32 processID;

            internal Int32 serviceFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class QUERY_SERVICE_CONFIG
        {
            internal Int32 dwServiceType;

            internal Int32 dwStartType;

            internal Int32 dwErrorControl;

            internal unsafe Char* lpBinaryPathName;

            internal unsafe Char* lpLoadOrderGroup;

            internal Int32 dwTagId;

            internal unsafe Char* lpDependencies;

            internal unsafe Char* lpServiceStartName;

            internal unsafe Char* lpDisplayName;
        }

        internal struct SERVICE_STATUS
        {
            public Int32 serviceType;

            public Int32 currentState;

            public Int32 controlsAccepted;

            public Int32 win32ExitCode;

            public Int32 serviceSpecificExitCode;

            public Int32 checkPoint;

            public Int32 waitHint;
        }

        public delegate void ServiceMainCallback(Int32 argCount, IntPtr argPointer);

        public struct SERVICE_TABLE_ENTRY
        {
            public IntPtr name;

            public ServiceMainCallback callback;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class WTSSESSION_NOTIFICATION
        {
            public Int32 size;

            public Int32 sessionId;
        }

        public delegate Int32 ServiceControlCallbackEx(Int32 control, Int32 eventType, IntPtr eventData, IntPtr eventContext);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Boolean CloseServiceHandle(IntPtr handle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern Boolean ControlService(SafeServiceHandle serviceHandle, Int32 control, SERVICE_STATUS* pStatus);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDependentServicesW", SetLastError = true)]
        internal static extern Boolean EnumDependentServices(SafeServiceHandle serviceHandle, Int32 serviceState, IntPtr bufferOfENUM_SERVICE_STATUS, Int32 bufSize, ref Int32 bytesNeeded, ref Int32 numEnumerated);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumServicesStatusExW", SetLastError = true)]
        internal static extern Boolean EnumServicesStatusEx(SafeServiceHandle databaseHandle, Int32 infolevel, Int32 serviceType, Int32 serviceState, IntPtr status, Int32 size, out Int32 bytesNeeded, out Int32 servicesReturned, ref Int32 resumeHandle, String group);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetServiceDisplayNameW", SetLastError = true)]
        internal unsafe static extern Boolean GetServiceDisplayName(SafeServiceHandle SCMHandle, String serviceName, Char* displayName, ref Int32 displayNameLength);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetServiceKeyNameW", SetLastError = true)]
        internal unsafe static extern Boolean GetServiceKeyName(SafeServiceHandle SCMHandle, String displayName, Char* KeyName, ref Int32 KeyNameLength);

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
        public static extern int DeleteService(SafeServiceHandle serviceHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "QueryServiceConfigW", SetLastError = true)]
        internal static extern Boolean QueryServiceConfig(SafeServiceHandle serviceHandle, IntPtr queryServiceConfigPtr, Int32 bufferSize, out Int32 bytesNeeded);

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
    }
}