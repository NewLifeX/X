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
        /// <summary>服务是否已安装</summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public override Boolean IsInstalled(String serviceName)
        {
            using var manager = new SafeServiceHandle(Advapi32.OpenSCManager(null, null, Advapi32.ServiceControllerOptions.SC_MANAGER_CONNECT));
            if (manager == null || manager.IsInvalid) return false;

            using var service = new SafeServiceHandle(Advapi32.OpenService(manager, serviceName, Advapi32.ServiceOptions.SERVICE_QUERY_CONFIG));
            if (service == null || service.IsInvalid) return false;

            return true;
        }

        /// <summary>服务是否已启动</summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public override unsafe Boolean IsRunning(String serviceName)
        {
            using var manager = new SafeServiceHandle(Advapi32.OpenSCManager(null, null, Advapi32.ServiceControllerOptions.SC_MANAGER_CONNECT));
            if (manager == null || manager.IsInvalid) return false;

            using var service = new SafeServiceHandle(Advapi32.OpenService(manager, serviceName, Advapi32.ServiceOptions.SERVICE_QUERY_STATUS));
            if (service == null || service.IsInvalid) return false;

            Advapi32.SERVICE_STATUS status = default;
            if (!Advapi32.QueryServiceStatus(service, &status))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return status.currentState == 4;
        }

        /// <summary>安装服务</summary>
        /// <param name="serviceName"></param>
        /// <param name="displayName"></param>
        /// <param name="binPath"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public override Boolean Install(String serviceName, String displayName, String binPath, String description)
        {
            using var manager = new SafeServiceHandle(Advapi32.OpenSCManager(null, null, Advapi32.ServiceControllerOptions.SC_MANAGER_CREATE_SERVICE));
            if (manager.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            using var service = new SafeServiceHandle(Advapi32.CreateService(manager, serviceName, displayName, Advapi32.ServiceOptions.SERVICE_ALL_ACCESS, 0x10, 2, 1, binPath, null, 0, null, null, null));
            if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            return true;
        }

        /// <summary>卸载服务</summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public override unsafe Boolean Uninstall(String serviceName)
        {
            using var manager = new SafeServiceHandle(Advapi32.OpenSCManager(null, null, Advapi32.ServiceControllerOptions.SC_MANAGER_ALL));
            if (manager.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            using var service = new SafeServiceHandle(Advapi32.OpenService(manager, serviceName, Advapi32.ServiceOptions.SERVICE_STOP | Advapi32.ServiceOptions.STANDARD_RIGHTS_DELETE));
            if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            Advapi32.SERVICE_STATUS status = default;
            Advapi32.ControlService(service, Advapi32.ControlOptions.CONTROL_STOP, &status);

            return Advapi32.DeleteService(service) != 0;
        }

        /// <summary>启动服务</summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public override Boolean Start(String serviceName)
        {
            //GCHandle val = default;
            //try
            //{
            using var manager = new SafeServiceHandle(Advapi32.OpenSCManager(null, null, Advapi32.ServiceControllerOptions.SC_MANAGER_CONNECT));
            if (manager.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            using var service = new SafeServiceHandle(Advapi32.OpenService(manager, serviceName, Advapi32.ServiceOptions.SERVICE_START));
            if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            //var array = new IntPtr[0];
            //val = GCHandle.Alloc(array, GCHandleType.Pinned);
            //if (!Advapi32.StartService(service, 0, val.AddrOfPinnedObject()))
            //    throw new Win32Exception(Marshal.GetLastWin32Error());
            if (!Advapi32.StartService(service, 0, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return true;
            //}
            //finally
            //{
            //    if (val.IsAllocated) val.Free();
            //}
        }

        /// <summary>停止服务</summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public override unsafe Boolean Stop(String serviceName)
        {
            using var manager = new SafeServiceHandle(Advapi32.OpenSCManager(null, null, Advapi32.ServiceControllerOptions.SC_MANAGER_ALL));
            if (manager.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            using var service = new SafeServiceHandle(Advapi32.OpenService(manager, serviceName, Advapi32.ServiceOptions.SERVICE_STOP));
            if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            Advapi32.SERVICE_STATUS status = default;
            if (!Advapi32.ControlService(service, Advapi32.ControlOptions.CONTROL_STOP, &status))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return true;
        }
        #endregion
    }
}