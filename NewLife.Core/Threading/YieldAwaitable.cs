using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#if NET4
namespace Microsoft.Runtime.CompilerServices
{
    /// <summary>yield</summary>
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct YieldAwaitable
    {
        /// <summary></summary>
        [StructLayout(LayoutKind.Sequential, Size = 1)]
        public struct YieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private static readonly Task s_completed = TaskEx.FromResult<int>(0);

            /// <summary></summary>
            public bool IsCompleted { get { return false; } }

            /// <summary></summary>
            /// <param name="continuation"></param>
            public void OnCompleted(Action continuation)
            {
                s_completed.GetAwaiter().OnCompleted(continuation);
            }

            /// <summary></summary>
            /// <param name="continuation"></param>
            public void UnsafeOnCompleted(Action continuation)
            {
                s_completed.GetAwaiter().UnsafeOnCompleted(continuation);
            }

            /// <summary></summary>
            public void GetResult() { }
        }

        /// <summary></summary>
        /// <returns></returns>
        public YieldAwaiter GetAwaiter()
        {
            return default(YieldAwaiter);
        }
    }
}
#endif