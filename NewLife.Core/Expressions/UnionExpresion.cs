using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NewLife.Expressions
{
    /// <summary>与或非表达式</summary>
    public class UnionExpresion : RpnExpression
    {
        /// <summary>位与</summary>
        public static readonly Char AndChar = '&';
        /// <summary>位或</summary>
        public static readonly Char OrChar = '|';

        /// <summary>实例化</summary>
        public UnionExpresion()
        {
            OperationChars = new Char[] { OrChar, AndChar, LeftBracket, RightBracket };
        }

        /// <summary>操作符等级</summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public override Int32 GetOperationLevel(String op)
        {
            switch (op)
            {
                case "|":
                case "&":
                    return 1;
                case "(":
                case ")":
                    return 0;
                default:
                    return -1;
            }
        }

        /// <summary>适配替换</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected override String AdapteAndReplace(String expression)
        {
            if (String.IsNullOrWhiteSpace(expression)) return String.Empty;

            return expression.ToUpper().Replace("AND", AndChar.ToString()).Replace("OR", OrChar.ToString()).Replace(EmptyChar.ToString(), String.Empty);
        }

        /// <summary>容器</summary>
        IList<IndexInfoResult> Container { get; set; }

        /// <summary>解逆波兰表达式</summary>
        /// <param name="expression">标准逆波兰表达式</param>
        /// <param name="args"></param>
        /// <returns>逆波兰表达式的解</returns>
        public override Object Complie(String expression, params Object[] args)
        {
            if (String.IsNullOrWhiteSpace(expression)) return null;

            var arr = expression.Split(new Char[] { JoinChar });

            var codes = new Stack<IndexInfoResult>();

            Container ??= new List<IndexInfoResult>();

            for (var i = 0; i < arr.Length; i++)
            {
                var level = GetOperationLevel(arr[i]);
                if (level < 0)
                {
                    var condition = arr[i].Trim();

                    var result = Container.FirstOrDefault(p => String.Equals(p.Mark, condition));
                    if (result == null) throw new ArgumentNullException(condition);

                    codes.Push(result);
                }
                else if (level > 0)
                {
                    // 为符号则将数字堆栈后两个数据解压并计算，将计算结果压入堆栈
                    if (codes.Count > 1)
                    {
                        var lastValue = codes.Pop();
                        var firstValue = codes.Pop();
                        var result = ComplieRpnExp(firstValue.IndexInfos.Select(p => p.StockCode), lastValue.IndexInfos.Select(p => p.StockCode), arr[i]);

                        var infoResult = new IndexInfoResult("(" + firstValue.Mark + arr[i].Replace("&", " AND ").Replace("|", " OR ") + lastValue.Mark + ")");

                        foreach (var code in result)
                        {
                            infoResult.IndexInfos.Add(new IndexInfo(code));
                        }

                        // 压入计算结果
                        codes.Push(infoResult);
                    }
                }
            }

            return codes.Pop();
        }

        /// <summary>是否有效</summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public override Boolean IsValid(String[] arr)
        {
            if (arr == null || arr.Length == 0) return false;

            var regex = new Regex(@"^#\d+$");
            for (var i = 0; i < arr.Length; i++)
            {
                if (!regex.IsMatch(arr[i].Trim())) throw new ArgumentException("错误数值：" + arr[i] + ".");
            }

            return true;
        }

        /// <summary>所有掩码</summary>
        /// <returns></returns>
        public IEnumerable<String> GetAllMarks()
        {
            if (String.IsNullOrWhiteSpace(Value)) yield return null;

            var regex = new Regex(@"#\d+");
            var collections = regex.Matches(AdapteAndReplace(Value));

            foreach (Match match in collections)
            {
                yield return match.Groups[0].Value;
            }
        }

        private IEnumerable<String> ComplieRpnExp(IEnumerable<String> firstValue,
            IEnumerable<String> lastValue, String operation)
        {
            if (String.IsNullOrWhiteSpace(operation)) return new List<String>();

            if (String.Equals(operation.Trim(), AndChar.ToString())) return GetAndResult(firstValue, lastValue);

            if (String.Equals(operation.Trim(), OrChar.ToString())) return GetOrResult(firstValue, lastValue);

            return new List<String>();
        }

        private IEnumerable<String> GetOrResult(IEnumerable<String> firstValue, IEnumerable<String> lastValue)
        {
            if (firstValue == null) return lastValue ?? new List<String>();

            if (lastValue == null) return firstValue ?? new List<String>();

            return firstValue.Union(lastValue);
        }

        private IEnumerable<String> GetAndResult(IEnumerable<String> firstValue, IEnumerable<String> lastValue)
        {
            if (firstValue == null || lastValue == null) return new List<String>();

            return firstValue.Intersect(lastValue);
        }
    }
}