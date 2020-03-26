using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NewLife.Log;

namespace NewLife.Agent
{
    /// <summary>Windows服务</summary>
    public class WindowsService : Host
    {
        private ServiceBase _service;
        private Advapi32.SERVICE_STATUS _status;
        private Int32 _acceptedCommands;

        public void Run(ServiceBase service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            _service = service;

            var num = Marshal.SizeOf(typeof(Advapi32.SERVICE_TABLE_ENTRY));
            var intPtr = Marshal.AllocHGlobal(checked((1 + 1) * num));
            try
            {
                //Advapi32.SERVICE_TABLE_ENTRY[] array = new Advapi32.SERVICE_TABLE_ENTRY[1];
                //IntPtr ptr;

                _status.serviceType = 16;
                _status.currentState = 2;
                _status.controlsAccepted = 0;
                _status.win32ExitCode = 0;
                _status.serviceSpecificExitCode = 0;
                _status.checkPoint = 0;
                _status.waitHint = 0;

                //services[i].Initialize(multipleServices);
                //array[i] = services[i].GetEntry();
                Advapi32.SERVICE_TABLE_ENTRY result = default;
                result.callback = ServiceMainCallback;
                result.name = Marshal.StringToHGlobalUni(service.ServiceName);
                //array[i] = result;
                var ptr = intPtr + num * 1;
                Marshal.StructureToPtr(result, ptr, false);

                Advapi32.SERVICE_TABLE_ENTRY structure = default;
                structure.callback = null;
                structure.name = (IntPtr)0;
                ptr = intPtr + num;
                Marshal.StructureToPtr(structure, ptr, false);
                var flag = Advapi32.StartServiceCtrlDispatcher(intPtr);
                //foreach (ServiceBase serviceBase in services)
                //{
                //    if (serviceBase._startFailedException != null)
                //    {
                //        serviceBase._startFailedException.Throw();
                //    }
                //}
                //string p = "";
                //if (!flag)
                //{
                //    p = new Win32Exception().Message;
                //    Console.WriteLine(System.SR.CantStartFromCommandLine);
                //}
                //foreach (ServiceBase serviceBase2 in services)
                //{
                //    ((Component)serviceBase2).Dispose();
                //    if (!flag)
                //    {
                //        serviceBase2.WriteLogEntry(System.SR.Format(System.SR.StartFailed, p), error: true);
                //    }
                //}
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        unsafe void ServiceMainCallback(Int32 argCount, IntPtr argPointer)
        {
            fixed (Advapi32.SERVICE_STATUS* status = &_status)
            {
                String[] array = null;
                if (argCount > 0)
                {
                    var ptr = (Char**)argPointer.ToPointer();
                    array = new String[argCount - 1];
                    for (var i = 0; i < array.Length; i++)
                    {
                        ptr++;
                        array[i] = Marshal.PtrToStringUni((IntPtr)(void*)(*ptr));
                    }
                }
                //if (!_initialized)
                //{
                //    Initialize(multipleServices: true);
                //}
                _statusHandle = Advapi32.RegisterServiceCtrlHandlerEx(_service.ServiceName, ServiceCommandCallbackEx, (IntPtr)0);
                //_nameFrozen = true;
                //if (_statusHandle == (IntPtr)0)
                //{
                //    string message = new Win32Exception().Message;
                //    WriteLogEntry(SR.Format(SR.StartFailed, message), error: true);
                //}
                _status.controlsAccepted = _acceptedCommands;
                //_commandPropsFrozen = true;
                if ((_status.controlsAccepted & 1) != 0)
                {
                    _status.controlsAccepted |= 4;
                }
                _status.currentState = 2;
                if (Advapi32.SetServiceStatus(_statusHandle, status))
                {
                    _startCompletedSignal = new ManualResetEvent(initialState: false);
                    //_startFailedException = null;
                    ThreadPool.QueueUserWorkItem(ServiceQueuedMainCallback, array);
                    _startCompletedSignal.WaitOne();
                    //if (_startFailedException != null && _status.win32ExitCode == 0)
                    //{
                    //    _status.win32ExitCode = 1064;
                    //}
                    if (!Advapi32.SetServiceStatus(_statusHandle, status))
                    {
                        //WriteLogEntry(SR.Format(SR.StartFailed, new Win32Exception().Message), error: true);
                        _status.currentState = 1;
                        Advapi32.SetServiceStatus(_statusHandle, status);
                    }
                }
            }
        }

        private IntPtr _statusHandle;
        private Int32 ServiceCommandCallbackEx(Int32 command, Int32 eventType, IntPtr eventData, IntPtr eventContext)
        {
            switch (command)
            {
                //case 13:
                //    ThreadPool.QueueUserWorkItem(delegate
                //    {
                //        DeferredPowerEvent(eventType, eventData);
                //    });
                //    break;
                //case 14:
                //    {
                //        Advapi32.WTSSESSION_NOTIFICATION sessionNotification = new Advapi32.WTSSESSION_NOTIFICATION();
                //        Marshal.PtrToStructure(eventData, sessionNotification);
                //        ThreadPool.QueueUserWorkItem(delegate
                //        {
                //            DeferredSessionChange(eventType, sessionNotification.sessionId);
                //        });
                //        break;
                //    }
                default:
                    ServiceCommandCallback(command);
                    break;
            }
            return 0;
        }

        private unsafe void ServiceCommandCallback(Int32 command)
        {
            fixed (Advapi32.SERVICE_STATUS* status = &_status)
            {
                if (command == 4)
                {
                    Advapi32.SetServiceStatus(_statusHandle, status);
                }
                else if (_status.currentState != 5 && _status.currentState != 2 && _status.currentState != 3 && _status.currentState != 6)
                {
                    switch (command)
                    {
                        case 3:
                            if (_status.currentState == 7)
                            {
                                _status.currentState = 5;
                                Advapi32.SetServiceStatus(_statusHandle, status);
                                //ThreadPool.QueueUserWorkItem(delegate
                                //{
                                //    DeferredContinue();
                                //});
                            }
                            break;
                        case 2:
                            if (_status.currentState == 4)
                            {
                                _status.currentState = 6;
                                Advapi32.SetServiceStatus(_statusHandle, status);
                                //ThreadPool.QueueUserWorkItem(delegate
                                //{
                                //    DeferredPause();
                                //});
                            }
                            break;
                        case 1:
                            {
                                var currentState = _status.currentState;
                                if (_status.currentState == 7 || _status.currentState == 4)
                                {
                                    _status.currentState = 3;
                                    Advapi32.SetServiceStatus(_statusHandle, status);
                                    _status.currentState = currentState;
                                    ThreadPool.QueueUserWorkItem(delegate
                                    {
                                        DeferredStop();
                                    });
                                }
                                break;
                            }
                        case 5:
                            //ThreadPool.QueueUserWorkItem(delegate
                            //{
                            //    DeferredShutdown();
                            //});
                            break;
                        default:
                            //ThreadPool.QueueUserWorkItem(delegate
                            //{
                            //    DeferredCustomCommand(command);
                            //});
                            break;
                    }
                }
            }
        }

        private unsafe void DeferredStop()
        {
            fixed (Advapi32.SERVICE_STATUS* status = &_status)
            {
                var currentState = _status.currentState;
                _status.checkPoint = 0;
                _status.waitHint = 0;
                _status.currentState = 3;
                Advapi32.SetServiceStatus(_statusHandle, status);
                try
                {
                    //OnStop();
                    var source = new CancellationTokenSource();
                    _service.StopAsync(source.Token);
                    //WriteLogEntry(SR.StopSuccessful);
                    _status.currentState = 1;
                    Advapi32.SetServiceStatus(_statusHandle, status);
                }
                catch (Exception p)
                {
                    _status.currentState = currentState;
                    Advapi32.SetServiceStatus(_statusHandle, status);
                    //WriteLogEntry(SR.Format(SR.StopFailed, p), error: true);
                    throw;
                }
            }
        }

        private ManualResetEvent _startCompletedSignal;
        private void ServiceQueuedMainCallback(Object state)
        {
            var args = (String[])state;
            try
            {
                //OnStart(args);
                var source = new CancellationTokenSource();
                _service.StartAsync(source.Token);
                //WriteLogEntry(SR.StartSuccessful);
                _status.checkPoint = 0;
                _status.waitHint = 0;
                _status.currentState = 4;
            }
            catch (Exception ex)
            {
                //WriteLogEntry(SR.Format(SR.StartFailed, ex), error: true);
                _status.currentState = 1;
                //_startFailedException = ExceptionDispatchInfo.Capture(ex);
            }
            _startCompletedSignal.Set();
        }

        #region 服务状态
        /// <summary>获取托管服务</summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public override IHostedService GetService(String serviceName)
        {
            try
            {
                return ServiceController.GetService(serviceName);
            }
            catch { return null; }
        }

        class ServiceController : DisposeBase, IHostedService
        {
            private SafeServiceHandle _manager;
            private SafeServiceHandle _service;

            public static unsafe ServiceController GetService(String serviceName)
            {
                var manager = new SafeServiceHandle(Advapi32.OpenSCManager(null, null, 1));
                if (manager == null || manager.IsInvalid) return null;

                var service = new SafeServiceHandle(Advapi32.OpenService(manager, serviceName, 4));
                if (service == null || service.IsInvalid) return null;

                return new ServiceController { _manager = manager, _service = service };
            }

            protected override void Dispose(Boolean disposing)
            {
                base.Dispose(disposing);

                _manager.TryDispose();
                _service.TryDispose();
            }

            public Boolean Running => GetStatus(_service) == ServiceControllerStatus.Running;

        }

        private static unsafe ServiceControllerStatus GetStatus(SafeServiceHandle serviceHandle)
        {
            Advapi32.SERVICE_STATUS sERVICE_STATUS = default;
            if (!Advapi32.QueryServiceStatus(serviceHandle, &sERVICE_STATUS))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return (ServiceControllerStatus)sERVICE_STATUS.currentState;
        }
        enum ServiceControllerStatus
        {
            ContinuePending = 5,
            Paused = 7,
            PausePending = 6,
            Running = 4,
            StartPending = 2,
            Stopped = 1,
            StopPending = 3
        }

        private static SafeServiceHandle GetServiceHandle(String serviceName, Int32 desiredAccess)
        {
            using var _serviceManagerHandle = GetDataBaseHandleWithAccess(".", 1);
            var safeServiceHandle = new SafeServiceHandle(Advapi32.OpenService(_serviceManagerHandle, serviceName, desiredAccess));

            return safeServiceHandle;
        }

        private static Advapi32.ENUM_SERVICE_STATUS_PROCESS[] GetServices(String machineName, Int32 serviceType, String group)
        {
            var resumeHandle = 0;
            var dataBaseHandleWithAccess = GetDataBaseHandleWithAccess(machineName, 4);
            try
            {
                Advapi32.EnumServicesStatusEx(dataBaseHandleWithAccess, 0, serviceType, 3, IntPtr.Zero, 0, out var bytesNeeded, out var servicesReturned, ref resumeHandle, group);
                var intPtr = Marshal.AllocHGlobal((IntPtr)bytesNeeded);
                try
                {
                    Advapi32.EnumServicesStatusEx(dataBaseHandleWithAccess, 0, serviceType, 3, intPtr, bytesNeeded, out bytesNeeded, out servicesReturned, ref resumeHandle, group);
                    var array = new Advapi32.ENUM_SERVICE_STATUS_PROCESS[servicesReturned];
                    for (var i = 0; i < servicesReturned; i++)
                    {
                        var ptr = (IntPtr)((Int64)intPtr + i * Marshal.SizeOf(typeof(Advapi32.ENUM_SERVICE_STATUS_PROCESS)));
                        var eNUM_SERVICE_STATUS_PROCESS = new Advapi32.ENUM_SERVICE_STATUS_PROCESS();
                        Marshal.PtrToStructure(ptr, eNUM_SERVICE_STATUS_PROCESS);
                        array[i] = eNUM_SERVICE_STATUS_PROCESS;
                    }
                    return array;
                }
                finally
                {
                    Marshal.FreeHGlobal(intPtr);
                }
            }
            finally
            {
                ((IDisposable)dataBaseHandleWithAccess)?.Dispose();
            }
        }

        private static SafeServiceHandle GetDataBaseHandleWithAccess(String machineName, Int32 serviceControlManagerAccess)
        {
            var safeServiceHandle = !machineName.Equals(".") && machineName.Length != 0 ?
                new SafeServiceHandle(Advapi32.OpenSCManager(machineName, null, serviceControlManagerAccess)) :
                new SafeServiceHandle(Advapi32.OpenSCManager(null, null, serviceControlManagerAccess));
            //if (safeServiceHandle.IsInvalid)
            //{
            //    Exception innerException = new Win32Exception(Marshal.GetLastWin32Error());
            //    throw new InvalidOperationException($"OpenSC {machineName}", innerException);
            //}
            return safeServiceHandle;
        }
        #endregion

        #region 服务控制
        public override void Install(IHostedService service)
        {
            var svc = service as ServiceBase;
            var name = svc.ServiceName;
            if (String.IsNullOrEmpty(name)) throw new Exception("未指定服务名！");

            if (name.Length < name.GetBytes().Length) throw new Exception("服务名不能是中文！");

            name = name.Replace(" ", "_");
            // win7及以上系统时才提示
            if (Environment.OSVersion.Version.Major >= 6) XTrace.WriteLine("在win7/win2008及更高系统中，可能需要管理员权限执行才能安装/卸载服务。");

            var exe = GetExeName();

            // 兼容dotnet
            var args = Environment.GetCommandLineArgs();
            if (args.Length >= 1 && Path.GetFileName(exe).EqualIgnoreCase("dotnet", "dotnet.exe"))
                exe += " " + args[0].GetFullPath();
            //else
            //    exe = exe.GetFullPath();

            var bin = GetBinPath(exe);
            RunSC($"create {name} BinPath= \"{bin}\" start= auto DisplayName= \"{svc.DisplayName}\"");
            if (!svc.Description.IsNullOrEmpty()) RunSC($"description {name} \"{svc.Description}\"");
        }

        /// <summary>Exe程序名</summary>
        static String GetExeName()
        {
            var p = Process.GetCurrentProcess();
            var filename = p.MainModule.FileName;
            //filename = Path.GetFileName(filename);
            filename = filename.Replace(".vshost.", ".");

            return filename;
        }

        public override void Uninstall(String serviceName)
        {
            Stop(serviceName);

            RunSC("Delete " + serviceName);
        }

        public override void Start(String serviceName)
        {
            RunCmd("net start " + serviceName, false, true);
        }

        public override void Stop(String serviceName)
        {
            RunCmd("net stop " + serviceName, false, true);
        }

        /// <summary>获取安装服务的命令参数</summary>
        /// <param name="exe"></param>
        /// <returns></returns>
        protected virtual String GetBinPath(String exe) => $"{exe} -s";

        /// <summary>执行一个命令</summary>
        /// <param name="cmd"></param>
        /// <param name="showWindow"></param>
        /// <param name="waitForExit"></param>
        internal static void RunCmd(String cmd, Boolean showWindow, Boolean waitForExit)
        {
            XTrace.WriteLine("RunCmd " + cmd);

            var p = new Process();
            var si = new ProcessStartInfo();
            var path = Environment.SystemDirectory;
            path = Path.Combine(path, @"cmd.exe");
            si.FileName = path;
            if (!cmd.StartsWith(@"/")) cmd = @"/c " + cmd;
            si.Arguments = cmd;
            si.UseShellExecute = false;
            si.CreateNoWindow = !showWindow;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            p.StartInfo = si;

            p.Start();
            if (waitForExit)
            {
                p.WaitForExit();

                var str = p.StandardOutput.ReadToEnd();
                if (!String.IsNullOrEmpty(str)) XTrace.WriteLine(str.Trim(new Char[] { '\r', '\n', '\t' }).Trim());
                str = p.StandardError.ReadToEnd();
                if (!String.IsNullOrEmpty(str)) XTrace.WriteLine(str.Trim(new Char[] { '\r', '\n', '\t' }).Trim());
            }
        }

        /// <summary>执行SC命令</summary>
        /// <param name="cmd"></param>
        internal static void RunSC(String cmd)
        {
            var path = Environment.SystemDirectory;
            path = Path.Combine(path, @"sc.exe");
            if (!File.Exists(path)) path = "sc.exe";
            if (!File.Exists(path)) return;

            RunCmd(path + " " + cmd, false, true);
        }
        #endregion
    }
}