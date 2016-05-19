using System;
using System.Diagnostics;

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
				this.m_context = context;
			}

			internal void Run()
			{
				if (this.m_context != null)
				{
					try
					{
						Action<object> action = AsyncMethodBuilderCore.MoveNextRunner.s_invokeMoveNext;
						if (action == null)
						{
							action = (AsyncMethodBuilderCore.MoveNextRunner.s_invokeMoveNext = new Action<object>(AsyncMethodBuilderCore.MoveNextRunner.InvokeMoveNext));
						}
						if (this.m_context == null)
						{
							action.Invoke(this.m_stateMachine);
						}
						else
						{
							ExecutionContextLightup.Instance.Run(this.m_context, action, this.m_stateMachine);
						}
						return;
					}
					finally
					{
						if (this.m_context != null)
						{
							this.m_context.Dispose();
						}
					}
				}
				this.m_stateMachine.MoveNext();
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
			if (this.m_stateMachine != null)
			{
				throw new InvalidOperationException("The builder was not properly initialized.");
			}
			this.m_stateMachine = stateMachine;
		}

		internal Action GetCompletionAction<TMethodBuilder, TStateMachine>(ref TMethodBuilder builder, ref TStateMachine stateMachine) where TMethodBuilder : IAsyncMethodBuilder where TStateMachine : IAsyncStateMachine
		{
			ExecutionContextLightup context = ExecutionContextLightup.Instance.Capture();
			AsyncMethodBuilderCore.MoveNextRunner moveNextRunner = new AsyncMethodBuilderCore.MoveNextRunner(context);
			Action result = new Action(moveNextRunner.Run);
			if (this.m_stateMachine == null)
			{
				builder.PreBoxInitialization();
				this.m_stateMachine = stateMachine;
				this.m_stateMachine.SetStateMachine(this.m_stateMachine);
			}
			moveNextRunner.m_stateMachine = this.m_stateMachine;
			return result;
		}
	}
}
