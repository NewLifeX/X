using System;

#if NET4
namespace System.Runtime.CompilerServices
{
    /// <summary>表示为异步方法生成的状态机。此类别仅供编译器使用。</summary>
	public interface IAsyncStateMachine
	{
        /// <summary>移动此状态机至其下一个状态。</summary>
		void MoveNext();

        /// <summary>使用堆分配的副本配置该状态机。</summary>
        /// <param name="stateMachine"></param>
		void SetStateMachine(IAsyncStateMachine stateMachine);
	}
}
#endif