using System;
using System.Threading.Tasks;

#if NET4
namespace System.Runtime.CompilerServices
{
    internal class AsyncMethodTaskCache<TResult>
    {
        private sealed class AsyncMethodBooleanTaskCache : AsyncMethodTaskCache<bool>
        {
            internal readonly TaskCompletionSource<bool> m_true = AsyncMethodTaskCache<bool>.CreateCompleted(true);

            internal readonly TaskCompletionSource<bool> m_false = AsyncMethodTaskCache<bool>.CreateCompleted(false);

            internal sealed override TaskCompletionSource<bool> FromResult(bool result)
            {
                if (!result) return m_false;

                return m_true;
            }
        }

        private sealed class AsyncMethodInt32TaskCache : AsyncMethodTaskCache<int>
        {
            internal const int INCLUSIVE_INT32_MIN = -1;

            internal const int EXCLUSIVE_INT32_MAX = 9;

            internal static readonly TaskCompletionSource<int>[] Int32Tasks = CreateInt32Tasks();

            private static TaskCompletionSource<int>[] CreateInt32Tasks()
            {
                var array = new TaskCompletionSource<int>[10];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = CreateCompleted(i + -1);
                }
                return array;
            }

            internal sealed override TaskCompletionSource<int> FromResult(int result)
            {
                if (result < -1 || result >= 9) return CreateCompleted(result);

                return Int32Tasks[result - -1];
            }
        }

        internal static readonly AsyncMethodTaskCache<TResult> Singleton = CreateCache();

        internal static TaskCompletionSource<TResult> CreateCompleted(TResult result)
        {
            var tcs = new TaskCompletionSource<TResult>();
            tcs.TrySetResult(result);
            return tcs;
        }

        private static AsyncMethodTaskCache<TResult> CreateCache()
        {
            var type = typeof(TResult);
            if (type == typeof(bool)) return (AsyncMethodTaskCache<TResult>)(Object)new AsyncMethodBooleanTaskCache();

            if (type == typeof(int)) return (AsyncMethodTaskCache<TResult>)(Object)new AsyncMethodInt32TaskCache();

            return null;
        }

        internal virtual TaskCompletionSource<TResult> FromResult(TResult result)
        {
            return CreateCompleted(result);
        }
    }
}
#endif