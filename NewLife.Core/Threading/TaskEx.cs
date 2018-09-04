using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using NewLife.Threading;

namespace System.Threading.Tasks
{
    /// <summary>任务扩展</summary>
    public static class TaskEx
    {
        #region 异步执行
        /// <summary>公平调度的工厂</summary>
        public static TaskFactory Factory { get; } = new TaskFactory(TaskCreationOptions.PreferFairness, TaskContinuationOptions.PreferFairness);

        /// <summary>异步执行</summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Task Run(Action action) => Run(action, CancellationToken.None);

        /// <summary>异步执行</summary>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task Run(Action action, CancellationToken cancellationToken)
        {
            return Factory.StartNew(action, cancellationToken, 0, TaskScheduler.Default);
            //return ThreadPoolX.Instance.QueueTask(action);
        }

        /// <summary>异步执行</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        public static Task<TResult> Run<TResult>(Func<TResult> function) => Run(function, CancellationToken.None);

        /// <summary>异步执行</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken)
        {
            return Factory.StartNew(function, cancellationToken, 0, TaskScheduler.Default);
            //return ThreadPoolX.Instance.QueueTask(token => function(), cancellationToken);
        }

        /// <summary>异步执行</summary>
        /// <param name="function"></param>
        /// <returns></returns>
        public static Task Run(Func<Task> function) => Run(function, CancellationToken.None);

        /// <summary>异步执行</summary>
        /// <param name="function"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task Run(Func<Task> function, CancellationToken cancellationToken) => TaskExtensions.Unwrap(Run<Task>(function, cancellationToken));

        /// <summary>异步执行</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function) => Run(function, CancellationToken.None);

        /// <summary>异步执行</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken) => TaskExtensions.Unwrap(Run<Task<TResult>>(function, cancellationToken));
        #endregion

#if NET4
        private const String ArgumentOutOfRange_TimeoutNonNegativeOrMinusOne = "The timeout must be non-negative or -1, and it must be less than or equal to Int32.MaxValue.";

        private static Task s_preCompletedTask = FromResult(false);

        /// <summary></summary>
        /// <param name="dueTime"></param>
        /// <returns></returns>
        public static Task Delay(Int32 dueTime) => Delay(dueTime, CancellationToken.None);

        /// <summary></summary>
        /// <param name="dueTime"></param>
        /// <returns></returns>
        public static Task Delay(TimeSpan dueTime) => Delay(dueTime, CancellationToken.None);

        /// <summary></summary>
        /// <param name="dueTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task Delay(TimeSpan dueTime, CancellationToken cancellationToken)
        {
            Int64 num = (Int64)dueTime.TotalMilliseconds;
            if (num < -1L || num > 2147483647L)
            {
                throw new ArgumentOutOfRangeException("dueTime", "The timeout must be non-negative or -1, and it must be less than or equal to Int32.MaxValue.");
            }
            Contract.EndContractBlock();
            return Delay((Int32)num, cancellationToken);
        }

        /// <summary></summary>
        /// <param name="dueTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task Delay(Int32 dueTime, CancellationToken cancellationToken)
        {
            if (dueTime < -1) throw new ArgumentOutOfRangeException("dueTime", "The timeout must be non-negative or -1, and it must be less than or equal to Int32.MaxValue.");

            Contract.EndContractBlock();
            if (cancellationToken.IsCancellationRequested) return new Task(() => { }, cancellationToken);

            if (dueTime == 0) return s_preCompletedTask;

            var tcs = new TaskCompletionSource<Boolean>();
            var ctr = default(CancellationTokenRegistration);
            Timer timer = null;
            timer = new Timer(state =>
            {
                ctr.Dispose();
                timer.Dispose();
                tcs.TrySetResult(true);
                TimerManager.Remove(timer);
            }, null, -1, -1);
            TimerManager.Add(timer);
            if (cancellationToken.CanBeCanceled)
            {
                ctr = cancellationToken.Register(() =>
                {
                    timer.Dispose();
                    tcs.TrySetCanceled();
                    TimerManager.Remove(timer);
                });
            }
            timer.Change(dueTime, -1);
            return tcs.Task;
        }

        /// <summary></summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static Task WhenAll(params Task[] tasks) => WhenAll((IEnumerable<Task>)tasks);

        /// <summary></summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks) => WhenAll((IEnumerable<Task<TResult>>)tasks);

        /// <summary></summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static Task WhenAll(IEnumerable<Task> tasks)
        {
            return WhenAllCore<Object>(tasks, (completedTasks, tcs) =>
            {
                tcs.TrySetResult(null);
            });
        }

        /// <summary></summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            return WhenAllCore<TResult[]>(tasks.Cast<Task>(), (completedTasks, tcs) =>
            {
                tcs.TrySetResult(completedTasks.Select(t => ((Task<TResult>)t).Result).ToArray());
            });
        }

        /// <summary></summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="tasks"></param>
        /// <param name="setResultAction"></param>
        /// <returns></returns>
        private static Task<TResult> WhenAllCore<TResult>(IEnumerable<Task> tasks, Action<Task[], TaskCompletionSource<TResult>> setResultAction)
        {
            if (tasks == null) throw new ArgumentNullException("tasks");

            Contract.EndContractBlock();
            Contract.Assert(setResultAction != null, null);
            var tcs = new TaskCompletionSource<TResult>();
            Task[] array = (tasks as Task[]) ?? tasks.ToArray();
            if (array.Length == 0)
            {
                setResultAction.Invoke(array, tcs);
            }
            else
            {
                Task.Factory.ContinueWhenAll(array, delegate (Task[] completedTasks)
                {
                    List<Exception> list = null;
                    Boolean flag = false;
                    for (Int32 i = 0; i < completedTasks.Length; i++)
                    {
                        Task task = completedTasks[i];
                        if (task.IsFaulted)
                        {
                            AddPotentiallyUnwrappedExceptions(ref list, task.Exception);
                        }
                        else if (task.IsCanceled)
                        {
                            flag = true;
                        }
                    }
                    if (list != null && list.Count > 0)
                    {
                        tcs.TrySetException(list);
                        return;
                    }
                    if (flag)
                    {
                        tcs.TrySetCanceled();
                        return;
                    }
                    setResultAction.Invoke(completedTasks, tcs);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            return tcs.Task;
        }

        /// <summary></summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static Task<Task> WhenAny(params Task[] tasks) => WhenAny((IEnumerable<Task>)tasks);

        /// <summary></summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static Task<Task> WhenAny(IEnumerable<Task> tasks)
        {
            if (tasks == null) throw new ArgumentNullException("tasks");

            Contract.EndContractBlock();
            var tcs = new TaskCompletionSource<Task>();
            Task.Factory.ContinueWhenAny<Boolean>((tasks as Task[]) ?? tasks.ToArray(), (Task completed) => tcs.TrySetResult(completed), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return tcs.Task;
        }

        /// <summary></summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks) => WhenAny((IEnumerable<Task<TResult>>)tasks);

        /// <summary></summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            if (tasks == null) throw new ArgumentNullException("tasks");

            Contract.EndContractBlock();
            var tcs = new TaskCompletionSource<Task<TResult>>();
            Task.Factory.ContinueWhenAny((tasks as Task<TResult>[]) ?? tasks.ToArray(), (Task<TResult> completed) => tcs.TrySetResult(completed), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return tcs.Task;
        }

        /// <summary></summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Task<TResult> FromResult<TResult>(TResult result)
        {
            var tcs = new TaskCompletionSource<TResult>(result);
            tcs.TrySetResult(result);
            return tcs.Task;
        }

        /// <summary></summary>
        /// <param name="targetList"></param>
        /// <param name="exception"></param>
        private static void AddPotentiallyUnwrappedExceptions(ref List<Exception> targetList, Exception exception)
        {
            var ex = exception as AggregateException;
            Contract.Assert(exception != null, null);
            Contract.Assert(ex == null || ex.InnerExceptions.Count > 0, null);
            if (targetList == null)
            {
                targetList = new List<Exception>();
            }
            if (ex != null)
            {
                targetList.Add((ex.InnerExceptions.Count == 1) ? exception.InnerException : exception);
                return;
            }
            targetList.Add(exception);
        }
#endif
    }

    /// <summary>任务扩展</summary>
    /// <typeparam name="TResult"></typeparam>
    public class TaskEx<TResult>
    {
        /// <summary>公平调度的工厂</summary>
        public static TaskFactory<TResult> Factory { get; } = new TaskFactory<TResult>(TaskCreationOptions.PreferFairness, TaskContinuationOptions.PreferFairness);
    }
}