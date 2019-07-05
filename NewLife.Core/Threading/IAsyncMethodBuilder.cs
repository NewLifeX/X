using System;

#if NET4
namespace System.Runtime.CompilerServices
{
	internal interface IAsyncMethodBuilder
	{
		void PreBoxInitialization();
	}
}
#endif