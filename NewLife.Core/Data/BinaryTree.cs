using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NewLife.Data
{
    /// <summary>二叉树</summary>
    public class BinaryTree
    {
        class Node
        {
            public Node Left { get; private set; }
            public Node Right { get; private set; }
            public Node(Node left, Node right)
            {
                Left = left;
                Right = right;
            }
        }

        /// <summary>遍历所有二叉树</summary>
        /// <param name="size"></param>
        /// <returns></returns>
        static IEnumerable<Node> GetAll(Int32 size)
        {
            if (size == 0) return new Node[] { null };

            return from i in Enumerable.Range(0, size)
                   from left in GetAll(i)
                   from right in GetAll(size - 1 - i)
                   select new Node(left, right);
        }

        /// <summary>构建表达式树</summary>
        /// <param name="node"></param>
        /// <param name="numbers"></param>
        /// <param name="operators"></param>
        /// <returns></returns>
        static Expression Build(Node node, Double[] numbers, List<Func<Expression, Expression, Expression>> operators)
        {
            var iNum = 0;
            var iOprt = 0;

            Func<Node, Expression> f = null;
            f = n =>
            {
                if (n == null) return Expression.Constant(numbers[iNum++]);

                var left = f(n.Left);
                var right = f(n.Right);
                return operators[iOprt++](left, right);
            };
            return f(node);
        }

        /// <summary>遍历全排列</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <returns></returns>
        static IEnumerable<T[]> FullPermute<T>(T[] arr)
        {
            if (arr.Length == 1) return EnumerableOfOneElement(arr);

            IEnumerable<T[]> result = null;
            // 依次抽取一个，其它元素降维递归
            foreach (var item in arr)
            {
                var bak = arr.ToList();
                bak.Remove(item);

                // 其它元素降维递归
                foreach (var elm in FullPermute(bak.ToArray()))
                {
                    var list = new List<T> { item };
                    list.AddRange(elm);

                    var seq = EnumerableOfOneElement(list.ToArray());
                    if (result == null)
                        result = seq;
                    else
                        result = result.Union(seq);
                }
            }
            return result;
        }

        static IEnumerable<T> EnumerableOfOneElement<T>(T element) { yield return element; }

        /// <summary>从4种运算符中挑选3个运算符</summary>
        /// <param name="operators"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        static IEnumerable<IEnumerable<Func<Expression, Expression, Expression>>> OperatorPermute(List<Func<Expression, Expression, Expression>> operators, Int32 count)
        {
            if (count == 2)
                return from operator1 in operators
                       from operator2 in operators
                       select new[] { operator1, operator2 };

            return from operator1 in operators
                   from operator2 in operators
                   from operator3 in operators
                   select new[] { operator1, operator2, operator3 };
        }

        static Expression Sqrt(Expression left, Expression right)
        {
            return Expression.Call(typeof(Math).GetMethod("Sqrt"), left);
        }

        /// <summary>数学运算</summary>
        /// <param name="numbers"></param>
        /// <param name="result"></param>
        public static String[] Execute(Double[] numbers, Double result)
        {
            var rs = new List<String>();
            var operators = new List<Func<Expression, Expression, Expression>> { Expression.Add, Expression.Subtract, Expression.Multiply, Expression.Divide, Expression.Modulo, Expression.Power, Sqrt };
            var size = numbers.Length - 1;
            var ops = OperatorPermute(operators, size);
            var nodes = GetAll(size);
            foreach (var op in ops)
            {
                foreach (var node in nodes)
                {
                    foreach (var nums in FullPermute(numbers))
                    {
                        var exp = Build(node, nums, op.ToList());
                        var compiled = Expression.Lambda<Func<Double>>(exp).Compile();

                        if (Math.Abs(compiled() - result) < 0.000001) rs.Add(exp + "");
                    }
                }
            }

            return rs.Distinct().ToArray();
        }
    }
}