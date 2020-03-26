using System;
using System.Runtime.InteropServices;

#if NET4
namespace System.Runtime.CompilerServices
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal struct VoidTaskResult
	{
	}
}
#endif