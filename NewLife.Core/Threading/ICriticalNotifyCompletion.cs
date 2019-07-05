using System;

#if NET4
namespace System.Runtime.CompilerServices
{
    /// <summary>表示等候程序，其计划等待操作完成时的后续部分。</summary>
	public interface ICriticalNotifyCompletion : INotifyCompletion
	{
        /// <summary>计划实例完成时调用的延续操作。</summary>
        /// <param name="continuation">要在操作完成时调用的操作。</param>
		void UnsafeOnCompleted(Action continuation);
	}
}

#endif