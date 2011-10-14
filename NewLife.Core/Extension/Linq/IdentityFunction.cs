using System;
using System.Runtime;
using NewLife.Reflection;
namespace System.Linq
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
        public IdentityFunction()
        {
        }
    }
}