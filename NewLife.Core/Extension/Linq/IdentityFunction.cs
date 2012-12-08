#if !NET4
using System;
using System.Runtime;
using NewLife.Reflection;

namespace NewLife.Linq
{
    internal class IdentityFunction<TElement>
    {
        public static Func<TElement, TElement> Instance
        {
            get
            {
                return (TElement x) => x;
            }
        }
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public IdentityFunction() { }
    }
}
#endif