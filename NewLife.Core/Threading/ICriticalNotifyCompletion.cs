using System;

#if NET4
namespace System.Runtime.CompilerServices
{
    /// <summary>��ʾ�Ⱥ������ƻ��ȴ��������ʱ�ĺ������֡�</summary>
	public interface ICriticalNotifyCompletion : INotifyCompletion
	{
        /// <summary>�ƻ�ʵ�����ʱ���õ�����������</summary>
        /// <param name="continuation">Ҫ�ڲ������ʱ���õĲ�����</param>
		void UnsafeOnCompleted(Action continuation);
	}
}

#endif