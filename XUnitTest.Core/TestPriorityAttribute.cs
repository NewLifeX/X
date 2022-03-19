using System;

namespace XUnitTest
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestPriorityAttribute : Attribute
    {
        public Int32 Priority { get; private set; }

        public TestPriorityAttribute(Int32 priority) => Priority = priority;
    }
}