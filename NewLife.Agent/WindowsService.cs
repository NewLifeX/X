using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using NewLife.Log;
using static NewLife.Agent.Advapi32;

namespace NewLife.Agent
{
    /// <summary>Windows服务</summary>
    public class WindowsService : Host
    {
        private IHostedService _service;
        private SERVICE_STATUS _status;
        private Int32 _acceptedCommands;

        /// <summary>开始执行服务</summary>
        /// <param name="service"></param>
        public override void Run(IHostedService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            _service = service;

            var num = Marshal.SizeOf(typeof(SERVICE_TABLE_ENTRY));
            var intPtr = Marshal.AllocHGlobal(checked((1 + 1) * num));
            try
            {
                // Win32OwnProcess/StartPending
                _status.serviceType = 16;
                _status.currentState = ServiceControllerStatus.StartPending;
                _status.controlsAccepted = 0;
                _status.win32ExitCode = 0;
                _status.serviceSpecificExitCode = 0;
                _status.checkPoint = 0;
                _status.waitHint = 0;

                // CanStop | CanShutdown | CanPauseAndContinue | CanHandlePowerEvent | CanHandleSessionChangeEvent
                //_acceptedCommands = 1 | 4 | 2 | 64 | 128;
                // CanStop | CanShutdown
                _acceptedCommands = 1 | 4;

                SERVICE_TABLE_ENTRY result = default;
                result.callback = ServiceMainCallback;
                result.name = Marshal.StringToHGlobalUni(service.ServiceName);
                Marshal.StructureToPtr(result, intPtr, false);

                SERVICE_TABLE_ENTRY result2 = default;
                result2.callback = null;
                result2.name = (IntPtr)0;
                Marshal.StructureToPtr(result2, intPtr + num, false);

                /*
                 * 如果StartServiceCtrlDispatcher函数执行成功，调用线程（也就是服务进程的主线程）不会返回，直到所有的服务进入到SERVICE_STOPPED状态。
                 * 调用线程扮演着控制分发的角色，干这样的事情：
                 * 1、在新的服务启动时启动新线程去调用服务主函数（主意：服务的任务是在新线程中做的）；
                 * 2、当服务有请求时（注意：请求是由SCM发给它的），调用它对应的处理函数（主意：这相当于主线程“陷入”了，它在等待控制消息并对消息做处理）。
                 */

                XTrace.WriteLine("启动服务 {0}", service.ServiceName);

                var flag = StartServiceCtrlDispatcher(intPtr);
                if (!flag) XTrace.WriteLine("服务启动失败！");
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private unsafe void ServiceMainCallback(Int32 argCount, IntPtr argPointer)
        {
            XTrace.WriteLine("ServiceMainCallback");

            fixed (SERVICE_STATUS* status = &_status)
            {
                // 我们直接忽略传入参数 argCount/argPointer

                _statusHandle = RegisterServiceCtrlHandlerEx(_service.ServiceName, ServiceCommandCallbackEx, IntPtr.Zero);

                _status.controlsAccepted = _acceptedCommands;
                if ((_status.controlsAccepted & 1) != 0)
                {
                    _status.controlsAccepted |= 4;
                }
                _status.currentState = ServiceControllerStatus.StartPending;
                if (SetServiceStatus(_statusHandle, status))
                {
                    // 使用线程池启动服务Start函数，并等待信号量
                    _startCompletedSignal = new ManualResetEvent(initialState: false);
                    ThreadPool.QueueUserWorkItem(ServiceQueuedMainCallback, null);
                    _startCompletedSignal.WaitOne();

                    // 设置服务状态
                    if (!SetServiceStatus(_statusHandle, status))
                    {
                        XTrace.WriteLine("启动服务{0}失败，{1}", _service.ServiceName, new Win32Exception().Message);

                        _status.currentState = ServiceControllerStatus.Stopped;
                        SetServiceStatus(_statusHandle, status);
                    }
                }
            }
        }

        private ManualResetEvent _startCompletedSignal;
        private void ServiceQueuedMainCallback(Object state)
        {
            //var args = (String[])state;
            try
            {
                //OnStart(args);
                var source = new CancellationTokenSource();
                _service.StartAsync(source.Token);
                //WriteLogEntry(SR.StartSuccessful);
                _status.checkPoint = 0;
                _status.waitHint = 0;
                _status.currentState = ServiceControllerStatus.Running;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);

                _status.currentState = ServiceControllerStatus.Stopped;
            }
            _startCompletedSignal.Set();
        }

        private IntPtr _statusHandle;
        private unsafe Int32 ServiceCommandCallbackEx(Int32 command, Int32 eventType, IntPtr eventData, IntPtr eventContext)
        {
            XTrace.WriteLine("ServiceCommandCallbackEx(command={0}, eventType={1}, eventData={2}, eventContext={3}", command, eventType, eventData, eventContext);

            // Power | SessionChange
            if (command == ControlOptions.CONTROL_POWEREVENT || command == ControlOptions.CONTROL_SESSIONCHANGE) return 0;

            fixed (SERVICE_STATUS* status = &_status)
            {
                if (command == ControlOptions.CONTROL_INTERROGATE)
                {
                    SetServiceStatus(_statusHandle, status);
                }
                else if (_status.currentState != ServiceControllerStatus.ContinuePending &&
                    _status.currentState != ServiceControllerStatus.StartPending &&
                    _status.currentState != ServiceControllerStatus.StopPending &&
                    _status.currentState != ServiceControllerStatus.PausePending)
                {
                    switch (command)
                    {
                        //case ControlOptions.CONTROL_CONTINUE:
                        //    if (_status.currentState == ServiceControllerStatus.Paused)
                        //    {
                        //        _status.currentState = ServiceControllerStatus.ContinuePending;
                        //        SetServiceStatus(_statusHandle, status);
                        //        //ThreadPool.QueueUserWorkItem(delegate
                        //        //{
                        //        //    DeferredContinue();
                        //        //});
                        //    }
                        //    break;
                        //case ControlOptions.CONTROL_PAUSE:
                        //    if (_status.currentState == ServiceControllerStatus.Running)
                        //    {
                        //        _status.currentState = ServiceControllerStatus.PausePending;
                        //        SetServiceStatus(_statusHandle, status);
                        //        //ThreadPool.QueueUserWorkItem(delegate
                        //        //{
                        //        //    DeferredPause();
                        //        //});
                        //    }
                        //    break;
                        case ControlOptions.CONTROL_STOP:
                            var currentState = _status.currentState;
                            if (_status.currentState == ServiceControllerStatus.Paused ||
                                _status.currentState == ServiceControllerStatus.Running)
                            {
                                // 设置为StopPending，然后线程池去执行停止
                                _status.currentState = ServiceControllerStatus.StopPending;
                                SetServiceStatus(_statusHandle, status);
                                _status.currentState = currentState;

                                ThreadPool.QueueUserWorkItem(s => DeferredStop());
                            }
                            break;
                        case ControlOptions.CONTROL_SHUTDOWN:
                            if (_status.currentState == ServiceControllerStatus.Paused ||
                                _status.currentState == ServiceControllerStatus.Running)
                            {
                                _status.checkPoint = 0;
                                _status.waitHint = 0;
                                _status.currentState = ServiceControllerStatus.Stopped;
                                SetServiceStatus(_statusHandle, status);

                                ThreadPool.QueueUserWorkItem(s => DeferredStop());
                            }
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

            return 0;
        }

        private unsafe void DeferredStop()
        {
            fixed (SERVICE_STATUS* status = &_status)
            {
                var currentState = _status.currentState;
                _status.checkPoint = 0;
                _status.waitHint = 0;
                _status.currentState = ServiceControllerStatus.StopPending;
                SetServiceStatus(_statusHandle, status);
                try
                {
                    var source = new CancellationTokenSource();
                    _service.StopAsync(source.Token);

                    _status.currentState = ServiceControllerStatus.Stopped;
                    SetServiceStatus(_statusHandle, status);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);

                    _status.currentState = currentState;
                    SetServiceStatus(_statusHandle, status);
                }
            }
        }

        #region 服务状态和控制
        /// <summary>服务是否已安装</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean IsInstalled(String serviceName)
        {
            using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_CONNECT));
            if (manager == null || manager.IsInvalid) return false;

            using var service = new SafeServiceHandle(OpenService(manager, serviceName, ServiceOptions.SERVICE_QUERY_CONFIG));
            if (service == null || service.IsInvalid) return false;

            return true;
        }

        /// <summary>服务是否已启动</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override unsafe Boolean IsRunning(String serviceName)
        {
            using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_CONNECT));
            if (manager == null || manager.IsInvalid) return false;

            using var service = new SafeServiceHandle(OpenService(manager, serviceName, ServiceOptions.SERVICE_QUERY_STATUS));
            if (service == null || service.IsInvalid) return false;

            SERVICE_STATUS status = default;
            if (!QueryServiceStatus(service, &status))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return status.currentState == ServiceControllerStatus.Running;
        }

        /// <summary>安装服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <param name="displayName"></param>
        /// <param name="binPath"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public override Boolean Install(String serviceName, String displayName, String binPath, String description)
        {
            XTrace.WriteLine("{0}.Install {1}, {2}, {3}, {4}", GetType().Name, serviceName, displayName, binPath, description);

            using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_CREATE_SERVICE));
            if (manager.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            using var service = new SafeServiceHandle(CreateService(manager, serviceName, displayName, ServiceOptions.SERVICE_ALL_ACCESS, 0x10, 2, 1, binPath, null, 0, null, null, null));
            if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            // 设置描述信息
            if (!description.IsNullOrEmpty())
            {
                SERVICE_DESCRIPTION sd;
                sd.Description = description;
                var lpInfo = Marshal.AllocHGlobal(Marshal.SizeOf(sd));

                try
                {
                    Marshal.StructureToPtr(sd, lpInfo, false);

                    const Int32 SERVICE_CONFIG_DESCRIPTION = 1;
                    ChangeServiceConfig2(service, SERVICE_CONFIG_DESCRIPTION, lpInfo);
                }
                finally
                {
                    Marshal.FreeHGlobal(lpInfo);
                }
            }

            return true;
        }

        /// <summary>卸载服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override unsafe Boolean Remove(String serviceName)
        {
            XTrace.WriteLine("{0}.Remove {1}", GetType().Name, serviceName);

            using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_ALL));
            if (manager.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            using var service = new SafeServiceHandle(OpenService(manager, serviceName, ServiceOptions.SERVICE_STOP | ServiceOptions.STANDARD_RIGHTS_DELETE));
            if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            SERVICE_STATUS status = default;
            ControlService(service, ControlOptions.CONTROL_STOP, &status);

            if (DeleteService(service) == 0) throw new Win32Exception(Marshal.GetLastWin32Error());

            return true;
        }

        /// <summary>启动服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override Boolean Start(String serviceName)
        {
            XTrace.WriteLine("{0}.Start {1}", GetType().Name, serviceName);

            using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_CONNECT));
            if (manager.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            using var service = new SafeServiceHandle(OpenService(manager, serviceName, ServiceOptions.SERVICE_START));
            if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!StartService(service, 0, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return true;
        }

        /// <summary>停止服务</summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        public override unsafe Boolean Stop(String serviceName)
        {
            XTrace.WriteLine("{0}.Stop {1}", GetType().Name, serviceName);

            using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_ALL));
            if (manager.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            using var service = new SafeServiceHandle(OpenService(manager, serviceName, ServiceOptions.SERVICE_STOP));
            if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            SERVICE_STATUS status = default;
            if (!ControlService(service, ControlOptions.CONTROL_STOP, &status))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return true;
        }
        #endregion
    }
}