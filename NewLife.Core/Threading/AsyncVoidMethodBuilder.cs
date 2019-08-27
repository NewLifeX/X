using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

#if NET4
namespace System.Runtime.CompilerServices
{
    /// <summary> ��ʾ�����������ڲ�����ֵ���첽������</summary>
    public struct AsyncVoidMethodBuilder : IAsyncMethodBuilder
    {
        private readonly SynchronizationContext m_synchronizationContext;

        private AsyncMethodBuilderCore m_coreState;

        private object m_objectIdForDebugger;

        private static int s_preventUnobservedTaskExceptionsInvoked;

        private object ObjectIdForDebugger
        {
            get
            {
                if (m_objectIdForDebugger == null)
                {
                    m_objectIdForDebugger = new object();
                }
                return m_objectIdForDebugger;
            }
        }

        static AsyncVoidMethodBuilder()
        {
            try
            {
                PreventUnobservedTaskExceptions();
            }
            catch
            {
            }
        }

        internal static void PreventUnobservedTaskExceptions()
        {
            if (Interlocked.CompareExchange(ref s_preventUnobservedTaskExceptionsInvoked, 1, 0) == 0)
            {
                TaskScheduler.UnobservedTaskException += (s, e) => { e.SetObserved(); };
            }
        }

        /// <summary>������ʵ��</summary>
        /// <returns></returns>
        public static AsyncVoidMethodBuilder Create()
        {
            return new AsyncVoidMethodBuilder(SynchronizationContext.Current);
        }

        private AsyncVoidMethodBuilder(SynchronizationContext synchronizationContext)
        {
            m_synchronizationContext = synchronizationContext;
            if (synchronizationContext != null)
            {
                synchronizationContext.OperationStarted();
            }
            m_coreState = default(AsyncMethodBuilderCore);
            m_objectIdForDebugger = null;
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

        /// <summary>��Ǵ˷���������Ϊ�ɹ���ɡ�</summary>
        public void SetResult()
        {
            if (m_synchronizationContext != null)
            {
                NotifySynchronizationContextOfCompletion();
            }
        }

        /// <summary>��һ���쳣�󶨵��÷�����������</summary>
        /// <param name="exception"></param>
        public void SetException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            if (m_synchronizationContext != null)
            {
                try
                {
                    AsyncServices.ThrowAsync(exception, m_synchronizationContext);
                    return;
                }
                finally
                {
                    NotifySynchronizationContextOfCompletion();
                }
            }
            AsyncServices.ThrowAsync(exception, null);
        }

        private void NotifySynchronizationContextOfCompletion()
        {
            try
            {
                m_synchronizationContext.OperationCompleted();
            }
            catch (Exception exception)
            {
                AsyncServices.ThrowAsync(exception, null);
            }
        }
    }
}
#endif