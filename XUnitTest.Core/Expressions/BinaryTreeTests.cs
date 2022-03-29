using System;
using NewLife.Data;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Expressions
{
    public class BinaryTreeTests
    {
        [Fact]
        public void Test1()
        {
            XTrace.WriteLine("开始二叉树运算");

            var nums = new Double[] { 5, 5, 5, 5 };
            var bt = new BinaryTree();
            var ss = bt.Execute(nums, 24);
            XTrace.WriteLine("共有结果：{0}", ss.Length);
            foreach (var item in ss)
            {
                XTrace.WriteLine(item);
            }
            Assert.Single(ss);
            Assert.Equal("((5 * 5) - (5 / 5))", ss[0]);
        }

        [Fact]
        public void Test2()
        {
            XTrace.WriteLine("开始二叉树运算");

            var nums = new Double[] { 1, 2, 3, 4 };
            var bt = new BinaryTree();
            var ss = bt.Execute(nums, 24);
            XTrace.WriteLine("共有结果：{0}", ss.Length);
            //foreach (var item in ss)
            //{
            //    XTrace.WriteLine(item);
            //}
            Assert.Equal(307, ss.Length);
            Assert.Equal("(4 * (1 + (2 + 3)))", ss[0]);
        }

        [Fact]
        public void Test22()
        {
            XTrace.WriteLine("开始二叉树运算");

            var nums = new Double[] { 1, 2, 3, 4 };
            var bt = new BinaryTree();
            bt.Operations.Add("Sqrt");
            //bt.Operations.Add("Cbrt");
            var ss = bt.Execute(nums, 24);
            XTrace.WriteLine("共有结果：{0}", ss.Length);
            //foreach (var item in ss)
            //{
            //    XTrace.WriteLine(item);
            //}

            Assert.Equal(654, ss.Length);
            Assert.Equal("(4 * (1 + (2 + 3)))", ss[0]);
        }
    }
}
