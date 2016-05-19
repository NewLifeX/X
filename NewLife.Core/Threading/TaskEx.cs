using Microsoft.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace System.Threading.Tasks
{
    internal static class TaskEx
    {
        private const string ArgumentOutOfRange_TimeoutNonNegativeOrMinusOne = "The timeout must be non-negative or -1, and it must be less than or equal to Int32.MaxValue.";

        private static Task s_preCompletedTask = FromResult<bool>(false);

        public static Task Run(Action action)
        {
            return Run(action, CancellationToken.None);
        }

        public static Task Run(Action action, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(action, cancellationToken, 0, TaskScheduler.Default);
        }

        public static Task<TResult> Run<TResult>(Func<TResult> function)
        {
            return Run<TResult>(function, CancellationToken.None);
        }

        public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew<TResult>(function, cancellationToken, 0, TaskScheduler.Default);
        }

        public static Task Run(Func<Task> function)
        {
            return Run(function, CancellationToken.None);
        }

        public static Task Run(Func<Task> function, CancellationToken cancellationToken)
        {
            return TaskExtensions.Unwrap(Run<Task>(function, cancellationToken));
        }

        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function)
        {
            return Run<TResult>(function, CancellationToken.None);
        }

        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken)
        {
            return TaskExtensions.Unwrap<TResult>(Run<Task<TResult>>(function, cancellationToken));
        }

        public static Task Delay(int dueTime)
        {
            return Delay(dueTime, CancellationToken.None);
        }

        public static Task Delay(TimeSpan dueTime)
        {
            return Delay(dueTime, CancellationToken.None);
        }

        public static Task Delay(TimeSpan dueTime, CancellationToken cancellationToken)
        {
            long num = (long)dueTime.TotalMilliseconds;
            if (num < -1L || num > 2147483647L)
            {
                throw new ArgumentOutOfRangeException("dueTime", "The timeout must be non-negative or -1, and it must be less than or equal to Int32.MaxValue.");
            }
            Contract.EndContractBlock();
            return Delay((int)num, cancellationToken);
        }

        public static Task Delay(int dueTime, CancellationToken cancellationToken)
        {
            if (dueTime < -1)
            {
                throw new ArgumentOutOfRangeException("dueTime", "The timeout must be non-negative or -1, and it must be less than or equal to Int32.MaxValue.");
            }
            Contract.EndContractBlock();
            if (cancellationToken.IsCancellationRequested)
            {
                return new Task(delegate
                {
                }, cancellationToken);
            }
            if (dueTime == 0)
            {
                return s_preCompletedTask;
            }
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            CancellationTokenRegistration ctr = default(CancellationTokenRegistration);
            Timer timer = null;
            timer = new Timer(delegate (object state)
            {
                ctr.Dispose();
                timer.Dispose();
                tcs.TrySetResult(true);
                TimerManager.Remove(timer);
            }, null, -1, -1);
            TimerManager.Add(timer);
            if (cancellationToken.CanBeCanceled)
            {
                ctr = cancellationToken.Register(delegate
                {
                    timer.Dispose();
                    tcs.TrySetCanceled();
                    TimerManager.Remove(timer);
                });
            }
            timer.Change(dueTime, -1);
            return tcs.Task;
        }

        public static Task WhenAll(params Task[] tasks)
        {
            return WhenAll((IEnumerable<Task>)tasks);
        }

        public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
        {
            return WhenAll<TResult>((IEnumerable<Task<TResult>>)tasks);
        }

        public static Task WhenAll(IEnumerable<Task> tasks)
        {
            return WhenAllCore<object>(tasks, delegate (Task[] completedTasks, TaskCompletionSource<object> tcs)
            {
                tcs.TrySetResult(null);
            });
        }

        public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            return WhenAllCore<TResult[]>(Enumerable.Cast<Task>(tasks), delegate (Task[] completedTasks, TaskCompletionSource<TResult[]> tcs)
            {
                tcs.TrySetResult(Enumerable.ToArray<TResult>(Enumerable.Select<Task, TResult>(completedTasks, (Task t) => ((Task<TResult>)t).Result)));
            });
        }

        private static Task<TResult> WhenAllCore<TResult>(IEnumerable<Task> tasks, Action<Task[], TaskCompletionSource<TResult>> setResultAction)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException("tasks");
            }
            Contract.EndContractBlock();
            Contract.Assert(setResultAction != null, null);
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            Task[] array = (tasks as Task[]) ?? Enumerable.ToArray<Task>(tasks);
            if (array.Length == 0)
            {
                setResultAction.Invoke(array, tcs);
            }
            else
            {
                Task.Factory.ContinueWhenAll(array, delegate (Task[] completedTasks)
                {
                    List<Exception> list = null;
                    bool flag = false;
                    for (int i = 0; i < completedTasks.Length; i++)
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

        public static Task<Task> WhenAny(params Task[] tasks)
        {
            return WhenAny((IEnumerable<Task>)tasks);
        }

        public static Task<Task> WhenAny(IEnumerable<Task> tasks)
        {
            if (tasks == null) throw new ArgumentNullException("tasks");

            Contract.EndContractBlock();
            var tcs = new TaskCompletionSource<Task>();
            Task.Factory.ContinueWhenAny<bool>((tasks as Task[]) ?? Enumerable.ToArray<Task>(tasks), (Task completed) => tcs.TrySetResult(completed), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return tcs.Task;
        }

        public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)
        {
            return WhenAny<TResult>((IEnumerable<Task<TResult>>)tasks);
        }

        public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            if (tasks == null) throw new ArgumentNullException("tasks");

            Contract.EndContractBlock();
            var tcs = new TaskCompletionSource<Task<TResult>>();
            Task.Factory.ContinueWhenAny<TResult, bool>((tasks as Task<TResult>[]) ?? Enumerable.ToArray<Task<TResult>>(tasks), (Task<TResult> completed) => tcs.TrySetResult(completed), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return tcs.Task;
        }

        public static Task<TResult> FromResult<TResult>(TResult result)
        {
            TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>(result);
            taskCompletionSource.TrySetResult(result);
            return taskCompletionSource.Task;
        }

        public static YieldAwaitable Yield()
        {
            return default(YieldAwaitable);
        }

        private static void AddPotentiallyUnwrappedExceptions(ref List<Exception> targetList, Exception exception)
        {
            AggregateException ex = exception as AggregateException;
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
    }
}
