using System;

#if NET40
namespace System.Runtime.CompilerServices
{
	internal interface IAsyncMethodBuilder
	{
		void PreBoxInitialization();
	}
}
#endif