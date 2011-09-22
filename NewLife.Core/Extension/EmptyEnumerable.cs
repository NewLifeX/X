using System;
using System.Collections.Generic;
using System.Runtime;
namespace System.Linq
{
    internal class EmptyEnumerable<TElement>
    {
        private static TElement[] instance;
        public static IEnumerable<TElement> Instance
        {
            get
            {
                if (EmptyEnumerable<TElement>.instance == null)
                {
                    EmptyEnumerable<TElement>.instance = new TElement[0];
                }
                return EmptyEnumerable<TElement>.instance;
            }
        }
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public EmptyEnumerable()
        {
        }
    }
}