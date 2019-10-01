using System.Diagnostics;
using System.Threading.Tasks;

#if NET4
namespace System.Runtime.CompilerServices
{
    /// <summary>��ʾ�����������ڷ���������첽������</summary>
    public struct AsyncTaskMethodBuilder : IAsyncMethodBuilder
    {
        private static readonly TaskCompletionSource<VoidTaskResult> s_cachedCompleted = AsyncTaskMethodBuilder<VoidTaskResult>.s_defaultResultTask;

        private AsyncTaskMethodBuilder<VoidTaskResult> m_builder;

        /// <summary>��ȡ��������������</summary>
        public Task Task { get { return m_builder.Task; } }

        private object ObjectIdForDebugger
        {
            get
            {
                return Task;
            }
        }

        /// <summary>�������ʵ��</summary>
        /// <returns></returns>
        public static AsyncTaskMethodBuilder Create()
        {
            return default(AsyncTaskMethodBuilder);
        }

        /// <summary>��ʼ�����й���״̬������������</summary>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="stateMachine"></param>
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            m_builder.Start(ref stateMachine);
        }

        /// <summary>һ����������ָ����״̬��������</summary>
        /// <param name="stateMachine"></param>
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            m_builder.SetStateMachine(stateMachine);
        }

        void IAsyncMethodBuilder.PreBoxInitialization()
        {
            Task arg_06_0 = Task;
        }

        /// <summary>ָ���� awaiter ���ʱ������״̬�����Լ�����һ������</summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            m_builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        /// <summary>ָ���� awaiter ���ʱ������״̬�����Լ�����һ�������˷����ɴӲ��������εĴ�����á�</summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            m_builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        /// <summary>��������Ϊ�ѳɹ���ɡ�</summary>
        public void SetResult()
        {
            m_builder.SetResult(s_cachedCompleted);
        }

        /// <summary>��Ǵ�����Ϊʧ�ܲ���ָ�����쳣��������</summary>
        /// <param name="exception"></param>
        public void SetException(Exception exception)
        {
            m_builder.SetException(exception);
        }

        internal void SetNotificationForWaitCompletion(bool enabled)
        {
            m_builder.SetNotificationForWaitCompletion(enabled);
        }
    }

    /// <summary>��ʾ�첽���������������������������������ṩ����Ĳ�����</summary>
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
                var tcs = m_taskCompletionSource;
                if (tcs == null)
                {
                    tcs = (m_taskCompletionSource = new TaskCompletionSource<TResult>());
                    m_task = tcs.Task;
                }
                return tcs;
            }
        }

        /// <summary>��ȡ��������������</summary>
        public Task<TResult> Task
        {
            get
            {
                var completionSource = CompletionSource;
                return completionSource.Task;
            }
        }

        private object ObjectIdForDebugger { get { return Task; } }

        static AsyncTaskMethodBuilder()
        {
            s_defaultResultTask = AsyncMethodTaskCache<TResult>.CreateCompleted(default(TResult));
            try
            {
                AsyncVoidMethodBuilder.PreventUnobservedTaskExceptions();
            }
            catch
            {
            }
        }

        /// <summary>������ʵ��</summary>
        /// <returns></returns>
        public static AsyncTaskMethodBuilder<TResult> Create()
        {
            return default(AsyncTaskMethodBuilder<TResult>);
        }

        /// <summary>��ʼ�����й���״̬������������</summary>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="stateMachine"></param>
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            m_coreState.Start(ref stateMachine);
        }

        /// <summary>һ����������ָ����״̬��������</summary>
        /// <param name="stateMachine"></param>
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            m_coreState.SetStateMachine(stateMachine);
        }

        void IAsyncMethodBuilder.PreBoxInitialization()
        {
            Task<TResult> arg_06_0 = Task;
        }

        /// <summary>ָ���� awaiter ���ʱ������״̬�����Լ�����һ������</summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            try
            {
                Action completionAction = m_coreState.GetCompletionAction(ref this, ref stateMachine);
                awaiter.OnCompleted(completionAction);
            }
            catch (Exception exception)
            {
                AsyncServices.ThrowAsync(exception, null);
            }
        }

        /// <summary>ָ���� awaiter ���ʱ������״̬�����Լ�����һ�������˷����ɴӲ��������εĴ�����á�</summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            try
            {
                Action completionAction = m_coreState.GetCompletionAction(ref this, ref stateMachine);
                awaiter.UnsafeOnCompleted(completionAction);
            }
            catch (Exception exception)
            {
                AsyncServices.ThrowAsync(exception, null);
            }
        }

        /// <summary>��������Ϊ�ѳɹ���ɡ�</summary>
        /// <param name="result"></param>
        public void SetResult(TResult result)
        {
            var taskCompletionSource = m_taskCompletionSource;
            if (taskCompletionSource == null)
            {
                m_taskCompletionSource = GetTaskForResult(result);
                m_task = m_taskCompletionSource.Task;
                return;
            }
            if (!taskCompletionSource.TrySetResult(result))
            {
                throw new InvalidOperationException("The Task was already completed.");
            }
        }

        internal void SetResult(TaskCompletionSource<TResult> completedTask)
        {
            if (m_taskCompletionSource == null)
            {
                m_taskCompletionSource = completedTask;
                m_task = m_taskCompletionSource.Task;
                return;
            }
            SetResult(default(TResult));
        }

        /// <summary>��Ǵ�����Ϊʧ�ܲ���ָ�����쳣��������</summary>
        /// <param name="exception"></param>
        public void SetException(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            var completionSource = CompletionSource;
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
#endif