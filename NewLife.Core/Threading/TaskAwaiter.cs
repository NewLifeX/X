using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Runtime.CompilerServices
{
    /// <summary>表示等待完成的异步任务的对象，并提供结果的参数。</summary>
    /// <typeparam name="TResult"></typeparam>
    public struct TaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
    {
        private readonly Task<TResult> m_task;

        /// <summary>获取一个值，该值指示异步任务是否已完成。</summary>
        public bool IsCompleted
        {
            get
            {
                return this.m_task.IsCompleted;
            }
        }

        internal TaskAwaiter(Task<TResult> task)
        {
            Contract.Assert(task != null, null);
            this.m_task = task;
        }

        /// <summary>将操作设置为当前对象停止等待异步任务完成时执行。</summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(this.m_task, continuation, true);
        }

        /// <summary>计划与此 awaiter 相关异步任务的延续操作。</summary>
        /// <param name="continuation"></param>
        public void UnsafeOnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(this.m_task, continuation, true);
        }

        /// <summary>异步任务完成后关闭等待任务。</summary>
        /// <returns></returns>
        public TResult GetResult()
        {
            TaskAwaiter.ValidateEnd(this.m_task);
            return this.m_task.Result;
        }
    }

    /// <summary>提供对象，其等待异步任务的完成。</summary>
    public struct TaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        internal const bool CONTINUE_ON_CAPTURED_CONTEXT_DEFAULT = true;

        private const string InvalidOperationException_TaskNotCompleted = "The task has not yet completed.";

        private readonly Task m_task;

        /// <summary>获取一个值，该值指示异步任务是否已完成。</summary>
        public bool IsCompleted
        {
            get
            {
                return this.m_task.IsCompleted;
            }
        }

        private static bool IsValidLocationForInlining
        {
            get
            {
                var current = SynchronizationContext.Current;
                return (current == null || current.GetType() == typeof(SynchronizationContext)) && TaskScheduler.Current == TaskScheduler.Default;
            }
        }

        internal TaskAwaiter(Task task)
        {
            Contract.Assert(task != null, null);
            this.m_task = task;
        }

        /// <summary>将操作设置为当前对象停止等待异步任务完成时执行。</summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(this.m_task, continuation, true);
        }

        /// <summary>计划与此 awaiter 相关异步任务的延续操作。</summary>
        /// <param name="continuation"></param>
        public void UnsafeOnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(this.m_task, continuation, true);
        }

        /// <summary>异步任务完成后关闭等待任务。</summary>
        /// <returns></returns>
        public void GetResult()
        {
            TaskAwaiter.ValidateEnd(this.m_task);
        }

        internal static void ValidateEnd(Task task)
        {
            if (task.Status != TaskStatus.RanToCompletion)
            {
                TaskAwaiter.HandleNonSuccess(task);
            }
        }

        private static void HandleNonSuccess(Task task)
        {
            if (!task.IsCompleted)
            {
                try
                {
                    task.Wait();
                }
                catch
                {
                }
            }
            if (task.Status != TaskStatus.RanToCompletion)
            {
                TaskAwaiter.ThrowForNonSuccess(task);
            }
        }

        private static void ThrowForNonSuccess(Task task)
        {
            Contract.Assert(task.Status != TaskStatus.RanToCompletion, null);
            switch (task.Status)
            {
                case TaskStatus.Canceled:
                    throw new TaskCanceledException(task);
                case TaskStatus.Faulted:
                    throw TaskAwaiter.PrepareExceptionForRethrow(task.Exception.InnerException);
                default:
                    throw new InvalidOperationException("The task has not yet completed.");
            }
        }

        internal static void OnCompletedInternal(Task task, Action continuation, bool continueOnCapturedContext)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException("continuation");
            }
            var sc = continueOnCapturedContext ? SynchronizationContext.Current : null;
            if (sc != null && sc.GetType() != typeof(SynchronizationContext))
            {
                task.ContinueWith(delegate (Task param0)
                {
                    try
                    {
                        sc.Post(delegate (object state)
                        {
                            ((Action)state).Invoke();
                        }, continuation);
                    }
                    catch (Exception exception)
                    {
                        AsyncServices.ThrowAsync(exception, null);
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                return;
            }
            TaskScheduler taskScheduler = continueOnCapturedContext ? TaskScheduler.Current : TaskScheduler.Default;
            if (task.IsCompleted)
            {
                Task.Factory.StartNew(delegate (object s)
                {
                    ((Action)s).Invoke();
                }, continuation, CancellationToken.None, 0, taskScheduler);
                return;
            }
            if (taskScheduler != TaskScheduler.Default)
            {
                task.ContinueWith(delegate (Task _)
                {
                    TaskAwaiter.RunNoException(continuation);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, taskScheduler);
                return;
            }
            task.ContinueWith(delegate (Task param0)
            {
                if (TaskAwaiter.IsValidLocationForInlining)
                {
                    TaskAwaiter.RunNoException(continuation);
                    return;
                }
                Task.Factory.StartNew(delegate (object s)
                {
                    TaskAwaiter.RunNoException((Action)s);
                }, continuation, CancellationToken.None, 0, TaskScheduler.Default);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private static void RunNoException(Action continuation)
        {
            try
            {
                continuation.Invoke();
            }
            catch (Exception exception)
            {
                AsyncServices.ThrowAsync(exception, null);
            }
        }

        internal static Exception PrepareExceptionForRethrow(Exception exc)
        {
            return exc;
        }
    }
}