using System;

#if NET4
namespace System.Runtime.CompilerServices
{
    /// <summary>表示操作，其计划等待操作完成时的后续部分。</summary>
	public interface INotifyCompletion
	{
        /// <summary>计划实例完成时调用的延续操作。</summary>
        /// <param name="continuation"></param>
		void OnCompleted(Action continuation);
	}
}
#endif