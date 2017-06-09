using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLife.Expressions
{
    /*
     * （1）若取出的字符是操作数，则分析出完整的运算数，该操作数直接送入S2栈
     * （2）若取出的字符是运算符，则将该运算符与S1栈栈顶元素比较，如果该运算符优先级大于S1栈栈顶运算符优先级，则将该运算符进S1栈，否则，将S1栈的栈顶运算符弹出，送入S2栈中，直至S1栈栈顶运算符低于（不包括等于）该运算符优先级，最后将该运算符送入S1栈。
     * （3）若取出的字符是“（”，则直接送入S1栈顶。
     * （4）若取出的字符是“）”，则将距离S1栈栈顶最近的“（”之间的运算符，逐个出栈，依次送入S2栈，此时抛弃“（”。
     * （5）重复上面的1~4步，直至处理完所有的输入字符
     * （6）若取出的字符是“#”，则将S1栈内所有运算符（不包括“#”），逐个出栈，依次送入S2栈。
     */

    /// <summary>逆波兰表达式</summary>
    public abstract class RpnExpression
    {
        /// <summary>左括号</summary>
        public static readonly Char LeftBracket = '(';
        /// <summary>右括号</summary>
        public static readonly Char RightBracket = ')';
        /// <summary>连接符</summary>
        public static readonly Char JoinChar = ',';
        /// <summary>空格</summary>
        public static readonly Char EmptyChar = ' ';

        /// <summary>操作符数组</summary>
        public Char[] OperationChars { get; protected set; }

        /// <summary>是否括号</summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public Boolean IsBracket(String ch)
        {
            return ch == LeftBracket.ToString() || ch == RightBracket.ToString();
        }

        /// <summary>是否括号</summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public Boolean IsBracket(Char ch)
        {
            return ch == LeftBracket || ch == RightBracket;
        }

        /// <summary>计算操作等级</summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public abstract Int32 GetOperationLevel(String op);

        /// <summary>是否括号匹配</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Boolean IsBracketMatch(String expression)
        {
            if (String.IsNullOrWhiteSpace(expression)) return true;

            var stack = new Stack<Char>();
            for (var i = 0; i < expression.Length; i++)
            {
                var ch = expression[i];
                if (!IsBracket(ch)) continue;

                // 左括号压栈
                if (ch == LeftBracket)
                {
                    stack.Push(LeftBracket);
                }
                // 右括号弹栈
                else
                {
                    // 无法匹配则失败
                    if (stack.Count == 0) return false;
                    if (stack.Pop() != LeftBracket) return false;
                }
            }

            return stack.Count == 0;
        }

        /// <summary>适配器和替换</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected virtual String AdapteAndReplace(String expression) { return expression; }

        /// <summary>值</summary>
        public String Value { get; private set; }

        /// <summary>将中缀表达式转换为逆波兰表达式</summary>
        /// <param name="expression">标准中缀表达式</param>
        /// <returns>标准逆波兰表达式</returns>
        public String ToExpression(String expression)
        {
            if (String.IsNullOrWhiteSpace(expression)) return String.Empty;

            var val = AdapteAndReplace(expression);
            Value = val;

            if (String.IsNullOrWhiteSpace(val)) return String.Empty;

            if (!IsBracketMatch(val)) throw new ArgumentException("括号不匹配！");

            var arr = val.Split(OperationChars, StringSplitOptions.RemoveEmptyEntries);
            if (!IsValid(arr)) return String.Empty;

            var ops = new Stack<String>();
            var outs = new Stack<String>();
            var idx = 0;
            var p = 0;

            while (idx < val.Length)
            {
                var ch = val.Substring(idx, 1);
                var level = GetOperationLevel(ch);

                if (ch == EmptyChar.ToString())
                {
                    idx++;
                    continue;
                }

                // 操作数入栈
                if (level < 0)
                {
                    outs.Push(arr[p]);
                    idx += arr[p].Length;
                    p++;
                    continue;
                }

                // 运算符入栈
                if (ops.Count == 0)
                {
                    ops.Push(ch);
                    idx++;
                    continue;
                }

                // 括号
                if (IsBracket(ch))
                {
                    // 左括号入栈
                    if (ch == LeftBracket.ToString())
                    {
                        ops.Push(ch);
                        idx++;
                    }
                    else
                    {
                        // 处理（）,括号里面不存在任何内容的情况
                        if (ops.Peek() == LeftBracket.ToString())
                        {
                            idx++;
                            ops.Pop();
                            continue;
                        }

                        idx++;
                        // 处理右括号，一直检测到左括号
                        while (ops.Peek() != LeftBracket.ToString())
                        {
                            outs.Push(ops.Pop());

                            if (ops.Count == 0) break;
                        }

                        // 删除左括号
                        if (ops.Count == 0) throw new ArgumentException("括号不匹配！");

                        ops.Pop();
                    }

                    continue;
                }

                var operation = ops.Peek();

                // 运算字符比运算符堆栈最后的级别高 直接推入运算符堆栈
                if (level > GetOperationLevel(operation))
                {
                    ops.Push(ch);
                    idx++;
                }
                else
                {
                    // 运算字符不高于运算符堆栈最后的级别，则将运算符堆栈出栈，直到比其高为止
                    while (level <= GetOperationLevel(operation))
                    {
                        outs.Push(operation);
                        ops.Pop();

                        if (ops.Count == 0) break;

                        operation = ops.Peek();
                    }

                    ops.Push(ch);
                    idx++;
                }
            }

            while (ops.Count > 0)
            {
                outs.Push(ops.Pop());
            }

            if (outs.Count == 0) return String.Empty;

            return String.Join(JoinChar.ToString(), outs.ToArray().Reverse());
        }

        /// <summary>是否有效</summary>
        /// <param name="splitArray"></param>
        /// <returns></returns>
        public abstract Boolean IsValid(String[] splitArray);

        /// <summary>编译计算</summary>
        /// <param name="expression"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public abstract Object Complie(String expression, params Object[] args);
    }
}