using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XUnitTest
{
    public class PriorityOrderer : ITestCaseOrderer
    {
        public IEnumerable<T> OrderTestCases<T>(IEnumerable<T> testCases) where T : ITestCase
        {
            var assemblyName = typeof(TestPriorityAttribute).AssemblyQualifiedName!;
            var dic = new SortedDictionary<Int32, List<T>>();
            foreach (var testCase in testCases)
            {
                var priority = 0;
                var atts = testCase.TestMethod.Method.GetCustomAttributes(assemblyName).ToList();
                foreach (var att in atts)
                {
                    var n = att.GetNamedArgument<Int32>(nameof(TestPriorityAttribute.Priority));
                    if (n != 0) priority = n;
                }

                if (!dic.TryGetValue(priority, out var list)) list = dic[priority] = new List<T>();

                list.Add(testCase);
            }

            foreach (var testCase in dic.SelectMany(e => e.Value.OrderBy(x => x.TestMethod.Method.Name)))
            {
                yield return testCase;
            }
        }
    }
}