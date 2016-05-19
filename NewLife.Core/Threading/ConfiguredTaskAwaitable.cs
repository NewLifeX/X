using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Runtime.CompilerServices
{
    internal struct ConfiguredTaskAwaitable
    {
        public struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private readonly Task m_task;

            private readonly bool m_continueOnCapturedContext;

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

            public void OnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext);
            }

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

        public ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter()
        {
            return this.m_configuredTaskAwaiter;
        }
    }

    internal struct ConfiguredTaskAwaitable<TResult>
    {
        public struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private readonly Task<TResult> m_task;

            private readonly bool m_continueOnCapturedContext;

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

            public void OnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext);
            }

            public TResult GetResult()
            {
                TaskAwaiter.ValidateEnd(this.m_task);
                return this.m_task.Result;
            }
        }

        private readonly ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter m_configuredTaskAwaiter;

        internal ConfiguredTaskAwaitable(Task<TResult> task, bool continueOnCapturedContext)
        {
            this.m_configuredTaskAwaiter = new ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter(task, continueOnCapturedContext);
        }

        public ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter GetAwaiter()
        {
            return this.m_configuredTaskAwaiter;
        }
    }
}