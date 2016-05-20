using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Runtime.CompilerServices
{
    /// <summary>配置任务await</summary>
    public struct ConfiguredTaskAwaitable
    {
        /// <summary>配置任务await</summary>
        public struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private readonly Task m_task;

            private readonly bool m_continueOnCapturedContext;

            /// <summary>是否已完成</summary>
            public bool IsCompleted
            {
                get
                {
                    return this.m_task.IsCompleted;
                }
            }

            internal ConfiguredTaskAwaiter(Task task, bool continueOnCapturedContext)
            {
                Contract.Assert(task != null, null);
                this.m_task = task;
                this.m_continueOnCapturedContext = continueOnCapturedContext;
            }

            /// <summary>指定的 awaiter 完成时，安排状态机，以继续下一操作。</summary>
            /// <param name="continuation"></param>
            public void OnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext);
            }

            /// <summary>指定的 awaiter 完成时，安排状态机，以继续下一操作。此方法可从部分受信任的代码调用。</summary>
            /// <param name="continuation"></param>
            public void UnsafeOnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext);
            }

            /// <summary>将任务标记为已成功完成。</summary>
            public void GetResult()
            {
                TaskAwaiter.ValidateEnd(this.m_task);
            }
        }

        private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter m_configuredTaskAwaiter;

        internal ConfiguredTaskAwaitable(Task task, bool continueOnCapturedContext)
        {
            Contract.Assert(task != null, null);
            this.m_configuredTaskAwaiter = new ConfiguredTaskAwaitable.ConfiguredTaskAwaiter(task, continueOnCapturedContext);
        }

        /// <summary>获取await</summary>
        /// <returns></returns>
        public ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter()
        {
            return this.m_configuredTaskAwaiter;
        }
    }

    /// <summary>配置任务await</summary>
    /// <typeparam name="TResult"></typeparam>
    public struct ConfiguredTaskAwaitable<TResult>
    {
        /// <summary>配置任务await</summary>
        public struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private readonly Task<TResult> m_task;

            private readonly bool m_continueOnCapturedContext;

            /// <summary>是否已完成</summary>
            public bool IsCompleted
            {
                get
                {
                    return this.m_task.IsCompleted;
                }
            }

            internal ConfiguredTaskAwaiter(Task<TResult> task, bool continueOnCapturedContext)
            {
                Contract.Assert(task != null, null);
                this.m_task = task;
                this.m_continueOnCapturedContext = continueOnCapturedContext;
            }

            /// <summary>指定的 awaiter 完成时，安排状态机，以继续下一操作。</summary>
            /// <param name="continuation"></param>
            public void OnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext);
            }

            /// <summary>指定的 awaiter 完成时，安排状态机，以继续下一操作。此方法可从部分受信任的代码调用。</summary>
            /// <param name="continuation"></param>
            public void UnsafeOnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext);
            }

            /// <summary>将任务标记为已成功完成。</summary>
            public TResult GetResult()
            {
                TaskAwaiter.ValidateEnd(this.m_task);
                return this.m_task.Result;
            }
        }

        private readonly ConfiguredTaskAwaiter m_configuredTaskAwaiter;

        internal ConfiguredTaskAwaitable(Task<TResult> task, bool continueOnCapturedContext)
        {
            this.m_configuredTaskAwaiter = new ConfiguredTaskAwaiter(task, continueOnCapturedContext);
        }

        /// <summary>获取await</summary>
        /// <returns></returns>
        public ConfiguredTaskAwaiter GetAwaiter()
        {
            return this.m_configuredTaskAwaiter;
        }
    }
}