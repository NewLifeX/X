using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#if NET4
namespace Microsoft.Runtime.CompilerServices
{
    /// <summary>表示等待完成的异步任务的对象，并提供结果的参数。</summary>
    /// <typeparam name="TResult"></typeparam>
    public struct TaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
    {
        private readonly Task<TResult> m_task;

        /// <summary>获取一个值，该值指示异步任务是否已完成。</summary>
        public bool IsCompleted { get { return m_task.IsCompleted; } }

        internal TaskAwaiter(Task<TResult> task)
        {
            Contract.Assert(task != null, null);
            m_task = task;
        }

        /// <summary>将操作设置为当前对象停止等待异步任务完成时执行。</summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(m_task, continuation, true);
        }

        /// <summary>计划与此 awaiter 相关异步任务的延续操作。</summary>
        /// <param name="continuation"></param>
        public void UnsafeOnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(m_task, continuation, true);
        }

        /// <summary>异步任务完成后关闭等待任务。</summary>
        /// <returns></returns>
        public TResult GetResult()
        {
            TaskAwaiter.ValidateEnd(m_task);
            return m_task.Result;
        }
    }

    /// <summary>提供对象，其等待异步任务的完成。</summary>
    public struct TaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        internal const bool CONTINUE_ON_CAPTURED_CONTEXT_DEFAULT = true;

        private const string InvalidOperationException_TaskNotCompleted = "The task has not yet completed.";

        private readonly Task m_task;

        /// <summary>获取一个值，该值指示异步任务是否已完成。</summary>
        public bool IsCompleted { get { return m_task.IsCompleted; } }

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
            m_task = task;
        }

        /// <summary>将操作设置为当前对象停止等待异步任务完成时执行。</summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            OnCompletedInternal(m_task, continuation, true);
        }

        /// <summary>计划与此 awaiter 相关异步任务的延续操作。</summary>
        /// <param name="continuation"></param>
        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompletedInternal(m_task, continuation, true);
        }

        /// <summary>异步任务完成后关闭等待任务。</summary>
        /// <returns></returns>
        public void GetResult()
        {
            ValidateEnd(m_task);
        }

        internal static void ValidateEnd(Task task)
        {
            if (task.Status != TaskStatus.RanToCompletion)
            {
                HandleNonSuccess(task);
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
                catch { }
            }
            if (task.Status != TaskStatus.RanToCompletion)
            {
                ThrowForNonSuccess(task);
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
                    throw PrepareExceptionForRethrow(task.Exception.InnerException);
                default:
                    throw new InvalidOperationException("The task has not yet completed.");
            }
        }

        internal static void OnCompletedInternal(Task task, Action continuation, bool continueOnCapturedContext)
        {
            if (continuation == null) throw new ArgumentNullException("continuation");

            var sc = continueOnCapturedContext ? SynchronizationContext.Current : null;
            if (sc != null && sc.GetType() != typeof(SynchronizationContext))
            {
                task.ContinueWith(t =>
                {
                    try
                    {
                        sc.Post(state => ((Action)state).Invoke(), continuation);
                    }
                    catch (Exception exception)
                    {
                        AsyncServices.ThrowAsync(exception, null);
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                return;
            }
            var taskScheduler = continueOnCapturedContext ? TaskScheduler.Current : TaskScheduler.Default;
            if (task.IsCompleted)
            {
                Task.Factory.StartNew(s => ((Action)s).Invoke(), continuation, CancellationToken.None, 0, taskScheduler);
                return;
            }
            if (taskScheduler != TaskScheduler.Default)
            {
                task.ContinueWith(_ => RunNoException(continuation), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, taskScheduler);
                return;
            }
            task.ContinueWith(_ =>
            {
                if (IsValidLocationForInlining)
                {
                    RunNoException(continuation);
                    return;
                }
                Task.Factory.StartNew(s => RunNoException((Action)s), continuation, CancellationToken.None, 0, TaskScheduler.Default);
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
            // 打包后再向外抛出异常，避免直接抛出打断了异常调用栈
            return new AggregateException(exc);
        }
    }
}
#endif