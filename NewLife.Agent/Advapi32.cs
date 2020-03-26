using System;
using System.Runtime.InteropServices;

namespace NewLife.Agent
{
    class Advapi32
    {
        internal class AcceptOptions
        {
            internal const int ACCEPT_POWEREVENT = 64;

            internal const int ACCEPT_PAUSE_CONTINUE = 2;

            internal const int ACCEPT_SESSIONCHANGE = 128;

            internal const int ACCEPT_SHUTDOWN = 4;

            internal const int ACCEPT_STOP = 1;
        }

        internal class ControlOptions
        {
            internal const int CONTROL_CONTINUE = 3;

            internal const int CONTROL_INTERROGATE = 4;

            internal const int CONTROL_PAUSE = 2;

            internal const int CONTROL_POWEREVENT = 13;

            internal const int CONTROL_SESSIONCHANGE = 14;

            internal const int CONTROL_SHUTDOWN = 5;

            internal const int CONTROL_STOP = 1;
        }

        internal class ServiceConfigOptions
        {
            internal const int SERVICE_CONFIG_DESCRIPTION = 1;

            internal const int SERVICE_CONFIG_FAILURE_ACTIONS = 2;

            internal const int SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3;
        }

        internal class ServiceOptions
        {
            internal const int SERVICE_QUERY_CONFIG = 1;

            internal const int SERVICE_CHANGE_CONFIG = 2;

            internal const int SERVICE_QUERY_STATUS = 4;

            internal const int SERVICE_ENUMERATE_DEPENDENTS = 8;

            internal const int SERVICE_START = 16;

            internal const int SERVICE_STOP = 32;

            internal const int SERVICE_PAUSE_CONTINUE = 64;

            internal const int SERVICE_INTERROGATE = 128;

            internal const int SERVICE_USER_DEFINED_CONTROL = 256;

            internal const int SERVICE_ALL_ACCESS = 983551;

            internal const int STANDARD_RIGHTS_DELETE = 65536;

            internal const int STANDARD_RIGHTS_REQUIRED = 983040;
        }

        internal class ServiceTypeOptions
        {
            internal const int SERVICE_TYPE_ADAPTER = 4;

            internal const int SERVICE_TYPE_FILE_SYSTEM_DRIVER = 2;

            internal const int SERVICE_TYPE_INTERACTIVE_PROCESS = 256;

            internal const int SERVICE_TYPE_KERNEL_DRIVER = 1;

            internal const int SERVICE_TYPE_RECOGNIZER_DRIVER = 8;

            internal const int SERVICE_TYPE_WIN32_OWN_PROCESS = 16;

            internal const int SERVICE_TYPE_WIN32_SHARE_PROCESS = 32;

            internal const int SERVICE_TYPE_WIN32 = 48;

            internal const int SERVICE_TYPE_DRIVER = 11;

            internal const int SERVICE_TYPE_ALL = 319;
        }

        internal class ServiceAccessOptions
        {
            internal const int ACCESS_TYPE_CHANGE_CONFIG = 2;

            internal const int ACCESS_TYPE_ENUMERATE_DEPENDENTS = 8;

            internal const int ACCESS_TYPE_INTERROGATE = 128;

            internal const int ACCESS_TYPE_PAUSE_CONTINUE = 64;

            internal const int ACCESS_TYPE_QUERY_CONFIG = 1;

            internal const int ACCESS_TYPE_QUERY_STATUS = 4;

            internal const int ACCESS_TYPE_START = 16;

            internal const int ACCESS_TYPE_STOP = 32;

            internal const int ACCESS_TYPE_USER_DEFINED_CONTROL = 256;

            internal const int ACCESS_TYPE_ALL = 983551;
        }

        internal class ServiceStartModes
        {
            internal const int START_TYPE_BOOT = 0;

            internal const int START_TYPE_SYSTEM = 1;

            internal const int START_TYPE_AUTO = 2;

            internal const int START_TYPE_DEMAND = 3;

            internal const int START_TYPE_DISABLED = 4;
        }

        internal class ServiceState
        {
            internal const int SERVICE_ACTIVE = 1;

            internal const int SERVICE_INACTIVE = 2;

            internal const int SERVICE_STATE_ALL = 3;
        }

        internal class StatusOptions
        {
            internal const int STATUS_ACTIVE = 1;

            internal const int STATUS_INACTIVE = 2;

            internal const int STATUS_ALL = 3;
        }

        internal class ServiceControlStatus
        {
            internal const int STATE_CONTINUE_PENDING = 5;

            internal const int STATE_PAUSED = 7;

            internal const int STATE_PAUSE_PENDING = 6;

            internal const int STATE_RUNNING = 4;

            internal const int STATE_START_PENDING = 2;

            internal const int STATE_STOPPED = 1;

            internal const int STATE_STOP_PENDING = 3;

            internal const int ERROR_EXCEPTION_IN_SERVICE = 1064;
        }

        internal class ServiceStartErrorModes
        {
            internal const int ERROR_CONTROL_CRITICAL = 3;

            internal const int ERROR_CONTROL_IGNORE = 0;

            internal const int ERROR_CONTROL_NORMAL = 1;

            internal const int ERROR_CONTROL_SEVERE = 2;
        }

        internal class ServiceControllerOptions
        {
            internal const int SC_ENUM_PROCESS_INFO = 0;

            internal const int SC_MANAGER_CONNECT = 1;

            internal const int SC_MANAGER_CREATE_SERVICE = 2;

            internal const int SC_MANAGER_ENUMERATE_SERVICE = 4;

            internal const int SC_MANAGER_LOCK = 8;

            internal const int SC_MANAGER_MODIFY_BOOT_CONFIG = 32;

            internal const int SC_MANAGER_QUERY_LOCK_STATUS = 16;

            internal const int SC_MANAGER_ALL = 983103;
        }

        internal class PowerBroadcastStatus
        {
            internal const int PBT_APMBATTERYLOW = 9;

            internal const int PBT_APMOEMEVENT = 11;

            internal const int PBT_APMPOWERSTATUSCHANGE = 10;

            internal const int PBT_APMQUERYSUSPEND = 0;

            internal const int PBT_APMQUERYSUSPENDFAILED = 2;

            internal const int PBT_APMRESUMEAUTOMATIC = 18;

            internal const int PBT_APMRESUMECRITICAL = 6;

            internal const int PBT_APMRESUMESUSPEND = 7;

            internal const int PBT_APMSUSPEND = 4;
        }

        internal class SessionStateChange
        {
            internal const int WTS_CONSOLE_CONNECT = 1;

            internal const int WTS_CONSOLE_DISCONNECT = 2;

            internal const int WTS_REMOTE_CONNECT = 3;

            internal const int WTS_REMOTE_DISCONNECT = 4;

            internal const int WTS_SESSION_LOGON = 5;

            internal const int WTS_SESSION_LOGOFF = 6;

            internal const int WTS_SESSION_LOCK = 7;

            internal const int WTS_SESSION_UNLOCK = 8;

            internal const int WTS_SESSION_REMOTE_CONTROL = 9;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal class ENUM_SERVICE_STATUS
        {
            internal string serviceName;

            internal string displayName;

            internal int serviceType;

            internal int currentState;

            internal int controlsAccepted;

            internal int win32ExitCode;

            internal int serviceSpecificExitCode;

            internal int checkPoint;

            internal int waitHint;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal class ENUM_SERVICE_STATUS_PROCESS
        {
            internal string serviceName;

            internal string displayName;

            internal int serviceType;

            internal int currentState;

            internal int controlsAccepted;

            internal int win32ExitCode;

            internal int serviceSpecificExitCode;

            internal int checkPoint;

            internal int waitHint;

            internal int processID;

            internal int serviceFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class QUERY_SERVICE_CONFIG
        {
            internal int dwServiceType;

            internal int dwStartType;

            internal int dwErrorControl;

            internal unsafe char* lpBinaryPathName;

            internal unsafe char* lpLoadOrderGroup;

            internal int dwTagId;

            internal unsafe char* lpDependencies;

            internal unsafe char* lpServiceStartName;

            internal unsafe char* lpDisplayName;
        }

        internal struct SERVICE_STATUS
        {
            public int serviceType;

            public int currentState;

            public int controlsAccepted;

            public int win32ExitCode;

            public int serviceSpecificExitCode;

            public int checkPoint;

            public int waitHint;
        }

        public delegate void ServiceMainCallback(int argCount, IntPtr argPointer);

        public struct SERVICE_TABLE_ENTRY
        {
            public IntPtr name;

            public ServiceMainCallback callback;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class WTSSESSION_NOTIFICATION
        {
            public int size;

            public int sessionId;
        }

        internal class SafeServiceHandle : SafeHandle
        {
            public override bool IsInvalid
            {
                get
                {
                    if (!(((SafeHandle)this).DangerousGetHandle() == IntPtr.Zero))
                    {
                        return ((SafeHandle)this).DangerousGetHandle() == new IntPtr(-1);
                    }
                    return true;
                }
            }

            internal SafeServiceHandle(IntPtr handle)
                : base(IntPtr.Zero, true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                return Advapi32.CloseServiceHandle(base.handle);
            }
        }
        public delegate int ServiceControlCallbackEx(int control, int eventType, IntPtr eventData, IntPtr eventContext);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CloseServiceHandle(IntPtr handle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern bool ControlService(SafeServiceHandle serviceHandle, int control, SERVICE_STATUS* pStatus);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDependentServicesW", SetLastError = true)]
        internal static extern bool EnumDependentServices(SafeServiceHandle serviceHandle, int serviceState, IntPtr bufferOfENUM_SERVICE_STATUS, int bufSize, ref int bytesNeeded, ref int numEnumerated);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumServicesStatusExW", SetLastError = true)]
        internal static extern bool EnumServicesStatusEx(SafeServiceHandle databaseHandle, int infolevel, int serviceType, int serviceState, IntPtr status, int size, out int bytesNeeded, out int servicesReturned, ref int resumeHandle, string group);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetServiceDisplayNameW", SetLastError = true)]
        internal unsafe static extern bool GetServiceDisplayName(SafeServiceHandle SCMHandle, string serviceName, char* displayName, ref int displayNameLength);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetServiceKeyNameW", SetLastError = true)]
        internal unsafe static extern bool GetServiceKeyName(SafeServiceHandle SCMHandle, string displayName, char* KeyName, ref int KeyNameLength);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenSCManagerW", SetLastError = true)]
        internal static extern IntPtr OpenSCManager(string machineName, string databaseName, int access);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenServiceW", SetLastError = true)]
        internal static extern IntPtr OpenService(SafeServiceHandle databaseHandle, string serviceName, int access);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "QueryServiceConfigW", SetLastError = true)]
        internal static extern bool QueryServiceConfig(SafeServiceHandle serviceHandle, IntPtr queryServiceConfigPtr, int bufferSize, out int bytesNeeded);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern bool QueryServiceStatus(SafeServiceHandle serviceHandle, SERVICE_STATUS* pStatus);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "StartServiceW", SetLastError = true)]
        internal static extern bool StartService(SafeServiceHandle serviceHandle, int argNum, IntPtr argPtrs);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public unsafe static extern bool SetServiceStatus(IntPtr serviceStatusHandle, SERVICE_STATUS* status);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr RegisterServiceCtrlHandlerEx(string serviceName, ServiceControlCallbackEx callback, IntPtr userData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool StartServiceCtrlDispatcher(IntPtr entry);
    }
}