using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

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
        /// <param name="ops"></param>
        /// <param name="sops"></param>
        /// <returns></returns>
        static Expression Build(Node node, Double[] numbers, List<Func<Expression, Expression, Expression>> ops, List<Func<Expression, Expression>> sops)
        {
            var iNum = 0;
            var iOprt = 0;

            Func<Node, Expression> f = null;
            f = n =>
            {
                if (n == null)
                {
                    Expression exp = Expression.Constant(numbers[iNum]);
                    if (sops[iNum] != null) exp = sops[iNum](exp);
                    iNum++;
                    return exp;
                }

                var left = f(n.Left);
                var right = f(n.Right);
                return ops[iOprt++](left, right);
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

        static IEnumerable<IEnumerable<Func<Expression, Expression>>> OperatorPermute(Int32 count)
        {
            var ops = new List<Func<Expression, Expression>>();
            // 有一个空的
            ops.Add(null);
            //foreach (var mi in typeof(Math).GetMethods())
            //{
            //    if (mi.ReturnParameter.ParameterType != typeof(Double)) continue;

            //    var pis = mi.GetParameters();
            //    if (pis != null && pis.Length == 1 && pis[0].ParameterType == typeof(Double))
            //    {
            //        Func<Expression, Expression> func = left => Expression.Call(mi, left);
            //        ops.Add(func);
            //    }
            //}
            ops.Add(left => Expression.Call(typeof(Math).GetMethod("Sqrt"), left));
            //ops.Add(left => Expression.Call(typeof(Math).GetMethod("Sin"), left));
            //ops.Add(left => Expression.Call(typeof(Math).GetMethod("Cos"), left));
            //ops.Add(left => Expression.Call(typeof(Math).GetMethod("Tan"), left));
            ops.Add(left => Expression.Call(typeof(BinaryTree).GetMethod(nameof(Cbrt), BindingFlags.NonPublic | BindingFlags.Static), left));

            if (count == 2)
                return from operator1 in ops
                       from operator2 in ops
                       select new[] { operator1, operator2 };

            if (count == 3)
                return from operator1 in ops
                       from operator2 in ops
                       from operator3 in ops
                       select new[] { operator1, operator2, operator3 };

            return from operator1 in ops
                   from operator2 in ops
                   from operator3 in ops
                   from operator4 in ops
                   select new[] { operator1, operator2, operator3, operator4 };
        }

        static Expression Sqrt(Expression left)
        {
            return Expression.Call(typeof(Math).GetMethod("Sqrt"), left);
        }

        /// <summary>立方根</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static Double Cbrt(Double value)
        {
            return Math.Pow(value, 1.0d / 3);
        }

        /// <summary>数学运算</summary>
        /// <param name="numbers"></param>
        /// <param name="result"></param>
        public static String[] Execute(Double[] numbers, Double result)
        {
            var rs = new List<String>();
            var operators = new List<Func<Expression, Expression, Expression>> { Expression.Add, Expression.Subtract, Expression.Multiply, Expression.Divide, Expression.Modulo, Expression.Power };
            var size = numbers.Length;
            var opss = OperatorPermute(operators, size - 1);
            var nodes = GetAll(size - 1);
            var sopss = OperatorPermute(size);
            // 所有二元运算符重新组合
            Parallel.ForEach(opss, ops =>
            {
                // 二叉树表示所有括号重新组合
                foreach (var node in nodes)
                {
                    // 数字所有组合
                    foreach (var nums in FullPermute(numbers))
                    {
                        // 所有一元运算符重新组合
                        foreach (var sops in sopss)
                        {
                            var exp = Build(node, nums, ops.ToList(), sops.ToList());
                            var compiled = Expression.Lambda<Func<Double>>(exp).Compile();

                            if (Math.Abs(compiled() - result) < 0.000001) rs.Add(exp + "");
                        }
                    }
                }
            });

            return rs.Distinct().ToArray();
        }
    }
}