using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NewLife.Expressions
{
    /// <summary>数学表达式</summary>
    public class MathExpression : RpnExpression
    {
        /// <summary>实例化</summary>
        public MathExpression() => OperationChars = new Char[] { '+', '-', '*', '/', '(', ')' };

        /// <summary>计算运算符优先级</summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public override Int32 GetOperationLevel(String op)
        {
            return op switch
            {
                "*" or "/" => 2,
                "+" or "-" => 1,
                "(" or ")" => 0,
                _ => -1,
            };
        }

        /// <summary>适配和替换</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected override String AdapteAndReplace(String expression)
        {
            if (String.IsNullOrWhiteSpace(expression)) return String.Empty;

            return expression.Replace(" ", "");
        }

        /// <summary>解逆波兰表达式</summary>
        /// <param name="expression">标准逆波兰表达式</param>
        /// <param name="args"></param>
        /// <returns>逆波兰表达式的解</returns>
        public override Object Complie(String[] expression, params Object[] args)
        {
            if (expression == null || expression.Length == 0) return 0;

            var arr = expression;

            var stack = new Stack<Double>();
            for (var i = 0; i < arr.Length; i++)
            {
                var level = GetOperationLevel(arr[i]);
                if (level < 0)
                {
                    stack.Push(MathExpression.ToDouble(arr[i]));
                }
                else if (level > 0)
                {
                    // 为符号则将数字堆栈后两个数据解压并计算，将计算结果压入堆栈
                    if (stack.Count > 1)
                    {
                        var lastValue = stack.Pop();
                        var firstValue = stack.Pop();
                        var result = ComplieRpnExp(lastValue, firstValue, arr[i]);

                        //压入计算结果
                        stack.Push(result);
                    }
                }
            }

            return stack.Pop();
        }

        /// <summary>是否有效</summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public override Boolean IsValid(String[] arr)
        {
            if (arr == null || arr.Length == 0) return false;

            var regex = new Regex(@"^\d+$|^\-?\d*\.\d*$");
            for (var index = 0; index < arr.Length; index++)
            {
                if (!regex.IsMatch(arr[index].Trim())) throw new ArgumentException(arr[index]);
            }

            return true;
        }

        /// <summary>转为浮点数</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Double ToDouble(String value)
        {
            if (Double.TryParse(value, out var tempValue)) return tempValue;

            return 0;
        }

        /// <summary>
        /// 计算逆波兰表达式
        /// </summary>
        /// <param name="last">最后压入数字堆栈的数字</param>
        /// <param name="first">首先压入数字堆栈的数字</param>
        /// <param name="op">操作运算符</param>
        /// <returns>返回计算结果</returns>
        private static Double ComplieRpnExp(Double last, Double first, String op)
        {
            return op switch
            {
                "+" => first + last,
                "-" => first - last,
                "*" => first * last,
                "/" => first / last,
                _ => 0,
            };
        }
    }
}