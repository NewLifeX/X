using System;

#if NET4
namespace System.Runtime.CompilerServices
{
    /// <summary>��ʾΪ�첽�������ɵ�״̬������������������ʹ�á�</summary>
	public interface IAsyncStateMachine
	{
        /// <summary>�ƶ���״̬��������һ��״̬��</summary>
		void MoveNext();

        /// <summary>ʹ�öѷ���ĸ������ø�״̬����</summary>
        /// <param name="stateMachine"></param>
		void SetStateMachine(IAsyncStateMachine stateMachine);
	}
}
#endif