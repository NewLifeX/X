using System;
using System.Runtime.InteropServices;

#if NET40
namespace System.Runtime.CompilerServices
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal struct VoidTaskResult
	{
	}
}
#endif