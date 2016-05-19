using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Runtime.CompilerServices
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    internal struct YieldAwaitable
    {
        [StructLayout(LayoutKind.Sequential, Size = 1)]
        public struct YieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            private static readonly Task s_completed = TaskEx.FromResult<int>(0);

            public bool IsCompleted
            {
                get
                {
                    return false;
                }
            }

            public void OnCompleted(Action continuation)
            {
                YieldAwaitable.YieldAwaiter.s_completed.GetAwaiter().OnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                YieldAwaitable.YieldAwaiter.s_completed.GetAwaiter().UnsafeOnCompleted(continuation);
            }

            public void GetResult()
            {
            }
        }

        public YieldAwaitable.YieldAwaiter GetAwaiter()
        {
            return default(YieldAwaitable.YieldAwaiter);
        }
    }
}