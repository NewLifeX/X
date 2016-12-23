using System;
using System.Threading;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Queue.Scheduling
{
    /// <summary>Represent a background worker that will repeatedly execute a specific method.
    /// </summary>
    public class Worker
    {
        private readonly object _lockObject = new object();
        private readonly string _actionName;
        private readonly Action _action;
        private readonly ILog _logger;
        private Status _status;

        /// <summary>Returns the action name of the current worker.
        /// </summary>
        public string ActionName
        {
            get { return _actionName; }
        }

        /// <summary>Initialize a new worker with the specified action.
        /// </summary>
        /// <param name="actionName">The action name.</param>
        /// <param name="action">The action to run by the worker.</param>
        public Worker(string actionName, Action action)
        {
            _actionName = actionName;
            _action = action;
            _status = Status.Initial;
            _logger = ObjectContainer.Current.Resolve<ILog>();
        }

        /// <summary>Start the worker if it is not running.
        /// </summary>
        public Worker Start()
        {
            lock (_lockObject)
            {
                if (_status == Status.Running) return this;

                _status = Status.Running;
                new Thread(Loop)
                {
                    Name = string.Format("{0}.Worker", _actionName),
                    IsBackground = true
                }.Start(this);

                return this;
            }
        }
        /// <summary>Request to stop the worker.
        /// </summary>
        public Worker Stop()
        {
            lock (_lockObject)
            {
                if (_status == Status.StopRequested) return this;

                _status = Status.StopRequested;

                return this;
            }
        }

        private void Loop(object data)
        {
            var worker = (Worker)data;

            while (worker._status == Status.Running)
            {
                try
                {
                    _action();
                }
                catch (ThreadAbortException)
                {
                    _logger.Info("Worker thread caught ThreadAbortException, try to resetting, actionName:{0}", _actionName);
                    Thread.ResetAbort();
                    _logger.Info("Worker thread ThreadAbortException resetted, actionName:{0}", _actionName);
                }
                catch (Exception ex)
                {
                    _logger.Error(string.Format("Worker thread has exception, actionName:{0}", _actionName), ex);
                }
            }
        }

        enum Status
        {
            Initial,
            Running,
            StopRequested
        }
    }
}
