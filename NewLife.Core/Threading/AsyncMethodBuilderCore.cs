using System;
using System.Diagnostics;

#if NET4
namespace System.Runtime.CompilerServices
{
    internal struct AsyncMethodBuilderCore
    {
        private sealed class MoveNextRunner
        {
            private readonly ExecutionContextLightup m_context;

            internal IAsyncStateMachine m_stateMachine;

            private static Action<object> s_invokeMoveNext;

            internal MoveNextRunner(ExecutionContextLightup context)
            {
                m_context = context;
            }

            internal void Run()
            {
                if (m_context != null)
                {
                    try
                    {
                        Action<object> action = s_invokeMoveNext;
                        if (action == null)
                        {
                            action = (s_invokeMoveNext = new Action<object>(InvokeMoveNext));
                        }
                        if (m_context == null)
                        {
                            action.Invoke(m_stateMachine);
                        }
                        else
                        {
                            ExecutionContextLightup.Instance.Run(m_context, action, m_stateMachine);
                        }
                        return;
                    }
                    finally
                    {
                        if (m_context != null)
                        {
                            m_context.Dispose();
                        }
                    }
                }
                m_stateMachine.MoveNext();
            }

            private static void InvokeMoveNext(object stateMachine)
            {
                ((IAsyncStateMachine)stateMachine).MoveNext();
            }
        }

        internal IAsyncStateMachine m_stateMachine;

        [DebuggerStepThrough]
        internal void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            if (stateMachine == null)
            {
                throw new ArgumentNullException("stateMachine");
            }
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            if (stateMachine == null)
            {
                throw new ArgumentNullException("stateMachine");
            }
            if (m_stateMachine != null)
            {
                throw new InvalidOperationException("The builder was not properly initialized.");
            }
            m_stateMachine = stateMachine;
        }

        internal Action GetCompletionAction<TMethodBuilder, TStateMachine>(ref TMethodBuilder builder, ref TStateMachine stateMachine) where TMethodBuilder : IAsyncMethodBuilder where TStateMachine : IAsyncStateMachine
        {
            var context = ExecutionContextLightup.Instance.Capture();
            var moveNextRunner = new MoveNextRunner(context);
            var result = new Action(moveNextRunner.Run);
            if (m_stateMachine == null)
            {
                builder.PreBoxInitialization();
                m_stateMachine = stateMachine;
                m_stateMachine.SetStateMachine(m_stateMachine);
            }
            moveNextRunner.m_stateMachine = m_stateMachine;
            return result;
        }
    }
}
#endif