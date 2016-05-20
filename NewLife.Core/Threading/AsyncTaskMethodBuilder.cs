using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>表示生成器，用于返回任务的异步方法。</summary>
    public struct AsyncTaskMethodBuilder : IAsyncMethodBuilder
    {
        private static readonly TaskCompletionSource<VoidTaskResult> s_cachedCompleted = AsyncTaskMethodBuilder<VoidTaskResult>.s_defaultResultTask;

        private AsyncTaskMethodBuilder<VoidTaskResult> m_builder;

        /// <summary>获取此生成器的任务。</summary>
        public Task Task { get { return this.m_builder.Task; } }

        private object ObjectIdForDebugger
        {
            get
            {
                return this.Task;
            }
        }

        /// <summary>创建类的实例</summary>
        /// <returns></returns>
        public static AsyncTaskMethodBuilder Create()
        {
            return default(AsyncTaskMethodBuilder);
        }

        /// <summary>开始运行有关联状态机的生成器。</summary>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="stateMachine"></param>
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            this.m_builder.Start<TStateMachine>(ref stateMachine);
        }

        /// <summary>一个生成器与指定的状态机关联。</summary>
        /// <param name="stateMachine"></param>
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            this.m_builder.SetStateMachine(stateMachine);
        }

        void IAsyncMethodBuilder.PreBoxInitialization()
        {
            Task arg_06_0 = this.Task;
        }

        /// <summary>指定的 awaiter 完成时，安排状态机，以继续下一操作。</summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            this.m_builder.AwaitOnCompleted<TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
        }

        /// <summary>指定的 awaiter 完成时，安排状态机，以继续下一操作。此方法可从部分受信任的代码调用。</summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            this.m_builder.AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
        }

        /// <summary>将任务标记为已成功完成。</summary>
        public void SetResult()
        {
            this.m_builder.SetResult(AsyncTaskMethodBuilder.s_cachedCompleted);
        }

        /// <summary>标记此任务为失败并绑定指定的异常至此任务。</summary>
        /// <param name="exception"></param>
        public void SetException(Exception exception)
        {
            this.m_builder.SetException(exception);
        }

        internal void SetNotificationForWaitCompletion(bool enabled)
        {
            this.m_builder.SetNotificationForWaitCompletion(enabled);
        }
    }

    /// <summary>表示异步方法的生成器，该生成器将返回任务并提供结果的参数。</summary>
    /// <typeparam name="TResult"></typeparam>
    public struct AsyncTaskMethodBuilder<TResult> : IAsyncMethodBuilder
    {
        internal static readonly TaskCompletionSource<TResult> s_defaultResultTask;

        private AsyncMethodBuilderCore m_coreState;

        private Task<TResult> m_task;

        private TaskCompletionSource<TResult> m_taskCompletionSource;

        internal TaskCompletionSource<TResult> CompletionSource
        {
            get
            {
                var tcs = this.m_taskCompletionSource;
                if (tcs == null)
                {
                    tcs = (this.m_taskCompletionSource = new TaskCompletionSource<TResult>());
                    this.m_task = tcs.Task;
                }
                return tcs;
            }
        }

        /// <summary>获取此生成器的任务。</summary>
        public Task<TResult> Task
        {
            get
            {
                var completionSource = this.CompletionSource;
                return completionSource.Task;
            }
        }

        private object ObjectIdForDebugger
        {
            get
            {
                return this.Task;
            }
        }

        static AsyncTaskMethodBuilder()
        {
            AsyncTaskMethodBuilder<TResult>.s_defaultResultTask = AsyncMethodTaskCache<TResult>.CreateCompleted(default(TResult));
            try
            {
                AsyncVoidMethodBuilder.PreventUnobservedTaskExceptions();
            }
            catch
            {
            }
        }

        /// <summary>创建类实例</summary>
        /// <returns></returns>
        public static AsyncTaskMethodBuilder<TResult> Create()
        {
            return default(AsyncTaskMethodBuilder<TResult>);
        }

        /// <summary>开始运行有关联状态机的生成器。</summary>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="stateMachine"></param>
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            this.m_coreState.Start<TStateMachine>(ref stateMachine);
        }

        /// <summary>一个生成器与指定的状态机关联。</summary>
        /// <param name="stateMachine"></param>
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            this.m_coreState.SetStateMachine(stateMachine);
        }

        void IAsyncMethodBuilder.PreBoxInitialization()
        {
            Task<TResult> arg_06_0 = this.Task;
        }

        /// <summary>指定的 awaiter 完成时，安排状态机，以继续下一操作。</summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            try
            {
                Action completionAction = this.m_coreState.GetCompletionAction<AsyncTaskMethodBuilder<TResult>, TStateMachine>(ref this, ref stateMachine);
                awaiter.OnCompleted(completionAction);
            }
            catch (Exception exception)
            {
                AsyncServices.ThrowAsync(exception, null);
            }
        }

        /// <summary>指定的 awaiter 完成时，安排状态机，以继续下一操作。此方法可从部分受信任的代码调用。</summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            try
            {
                Action completionAction = this.m_coreState.GetCompletionAction<AsyncTaskMethodBuilder<TResult>, TStateMachine>(ref this, ref stateMachine);
                awaiter.UnsafeOnCompleted(completionAction);
            }
            catch (Exception exception)
            {
                AsyncServices.ThrowAsync(exception, null);
            }
        }

        /// <summary>将任务标记为已成功完成。</summary>
        /// <param name="result"></param>
        public void SetResult(TResult result)
        {
            var taskCompletionSource = this.m_taskCompletionSource;
            if (taskCompletionSource == null)
            {
                this.m_taskCompletionSource = this.GetTaskForResult(result);
                this.m_task = this.m_taskCompletionSource.Task;
                return;
            }
            if (!taskCompletionSource.TrySetResult(result))
            {
                throw new InvalidOperationException("The Task was already completed.");
            }
        }

        internal void SetResult(TaskCompletionSource<TResult> completedTask)
        {
            if (this.m_taskCompletionSource == null)
            {
                this.m_taskCompletionSource = completedTask;
                this.m_task = this.m_taskCompletionSource.Task;
                return;
            }
            this.SetResult(default(TResult));
        }

        /// <summary>标记此任务为失败并绑定指定的异常至此任务。</summary>
        /// <param name="exception"></param>
        public void SetException(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            var completionSource = this.CompletionSource;
            if (!((exception is OperationCanceledException) ? completionSource.TrySetCanceled() : completionSource.TrySetException(exception)))
            {
                throw new InvalidOperationException("The Task was already completed.");
            }
        }

        internal void SetNotificationForWaitCompletion(bool enabled)
        {
        }

        private TaskCompletionSource<TResult> GetTaskForResult(TResult result)
        {
            var singleton = AsyncMethodTaskCache<TResult>.Singleton;
            if (singleton == null)
                return AsyncMethodTaskCache<TResult>.CreateCompleted(result);

            return singleton.FromResult(result);
        }
    }
}
