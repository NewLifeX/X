using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace XTemplate.Templating
{
    /// <summary>
    /// 模版分析器
    /// </summary>
    internal static class TemplateParser
    {
        #region 分析模版的正则表达式
        private static MatchEvaluator escapeReplacingEvaluator;
        private static Regex templateParsingRegex;
        private const String ma = @"(?<=([^\\]|^)(\\\\)*)";
        private const String startTag = @"(?<=([^\\]|^)(\\\\)*)<#";
        private const String endTag = @"(?<=[^\\](\\\\)*)#>";

        static TemplateParser()
        {
            escapeReplacingEvaluator = delegate(Match match)
            {
                if (match.Success && (match.Value != null))
                {
                    Int32 length = (Int32)Math.Floor((double)match.Value.Length / 2.0);
                    return match.Value.Substring(0, length);
                }
                return String.Empty;
            };

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(@"(?<text>^(\\\\)+)(?=<#)|");
            sb.AppendFormat(@"{0}@(?<directive>.*?){1}|", startTag, endTag);
            sb.AppendFormat(@"{0}\!(?<member>.*?){1}|", startTag, endTag);
            sb.AppendFormat(@"{0}=(?<expression>.*?){1}|", startTag, endTag);
            sb.AppendFormat(@"{0}(?<statement>.*?){1}|", startTag, endTag);
            sb.AppendFormat(@"(?<text>.+?)(?=((?<=[^\\](\\\\)*)<#))|");
            sb.AppendFormat("(?<text>.+)(?=$)");
            //sb.AppendLine(@"(?<text>^(\\\\)+)(?=<\#)|");
            //sb.AppendLine(@"(?<=([^\\]|^)(\\\\)*)<\#@(?<directive>.*?)(?<=[^\\](\\\\)*)\#>|");
            //sb.AppendLine(@"(?<=([^\\]|^)(\\\\)*)<\#\!(?<member>.*?)(?<=[^\\](\\\\)*)\#>|");
            //sb.AppendLine(@"(?<=([^\\]|^)(\\\\)*)<\#=(?<expression>.*?)(?<=[^\\](\\\\)*)\#>|");
            //sb.AppendLine(@"(?<=([^\\]|^)(\\\\)*)<\#(?<statement>.*?)(?<=[^\\](\\\\)*)\#>|");
            //sb.AppendLine(@"(?<text>.+?)(?=((?<=[^\\](\\\\)*)<\#))|");
            //sb.AppendLine("(?<text>.+)(?=$)");
            templateParsingRegex = new Regex(sb.ToString(), RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        }
        #endregion

        #region 分析模版
        private static Regex unescapedTagFindingRegex = new Regex(@"(^|[^\\])(\\\\)*(<\#|\#>)", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        /// <summary>
        /// 把模版分割成块
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static List<Block> Parse(String name, String content)
        {
            if (content == null) throw new ArgumentNullException("content");

            List<Block> blocks = new List<Block>();
            foreach (Match match in templateParsingRegex.Matches(content))
            {
                Block item = new Block();
                Group group = null;
                if ((group = match.Groups["text"]).Success)
                    item.Type = BlockType.Text;
                else if ((group = match.Groups["directive"]).Success)
                    item.Type = BlockType.Directive;
                else if ((group = match.Groups["member"]).Success)
                    item.Type = BlockType.Member;
                else if ((group = match.Groups["expression"]).Success)
                    item.Type = BlockType.Expression;
                else if ((group = match.Groups["statement"]).Success)
                    item.Type = BlockType.Statement;

                if (group != null && group.Success)
                {
                    item.Text = group.Value;
                    item.Name = name;
                    blocks.Add(item);
                }
            }
            InsertPosition(blocks);

            foreach (Block block in blocks)
            {
                if (unescapedTagFindingRegex.Match(block.Text).Success) throw new TemplateException(block, "不可识别的标记！");
            }

            StripEscapeCharacters(blocks);
            return blocks;
        }

        private static Regex newlineFindingRegex = new Regex(Environment.NewLine, RegexOptions.Singleline | RegexOptions.Compiled);
        /// <summary>
        /// 插入位置信息
        /// </summary>
        /// <param name="blocks"></param>
        private static void InsertPosition(List<Block> blocks)
        {
            Int32 i = 1;
            Int32 j = 1;
            foreach (Block block in blocks)
            {
                // 类成员以<#!开始，指令以<#@开始，表达式以<#=开始，所以它们的列数加3
                if (block.Type == BlockType.Member ||
                    block.Type == BlockType.Directive ||
                    block.Type == BlockType.Expression)
                {
                    j += 3;
                }
                else if (block.Type == BlockType.Statement)
                {
                    // 代码块以<#开始
                    j += 2;
                }
                block.StartLine = i;
                block.StartColumn = j;

                // 计算换行
                MatchCollection matchs = newlineFindingRegex.Matches(block.Text);
                i += matchs.Count;
                if (matchs.Count > 0)
                {
                    // 有换行的存在，从新计算列数，以最后一行的最后列数作为整个块的最后列数
                    j = ((block.Text.Length - matchs[matchs.Count - 1].Index) - Environment.NewLine.Length) + 1;
                }
                else
                {
                    j += block.Text.Length;
                }
                block.EndLine = i;
                block.EndColumn = j;

                // 非占位符块时，列数加2，因为它们都以#>结尾
                if (block.Type != BlockType.Text) j += 2;
            }
        }

        private static Regex escapeFindingRegex = new Regex(@"\\+(?=<\\#)|\\+(?=\\#>)", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static Regex eolEscapeFindingRegex = new Regex(@"\\+(?=$)", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        /// <summary>
        /// 对编码的字符进行解码
        /// </summary>
        /// <param name="blocks"></param>
        private static void StripEscapeCharacters(List<Block> blocks)
        {
            for (Int32 i = 0; i < blocks.Count; i++)
            {
                Block block = blocks[i];
                block.Text = escapeFindingRegex.Replace(block.Text, escapeReplacingEvaluator);
                if (i != (blocks.Count - 1))
                {
                    block.Text = eolEscapeFindingRegex.Replace(block.Text, escapeReplacingEvaluator);
                }
            }
        }

        ///// <summary>
        ///// 检查块顺序是否有问题
        ///// </summary>
        ///// <param name="blocks"></param>
        //private static void CheckBlockSequence(List<Block> blocks)
        //{
        //    Boolean isMemberFeature = false;
        //    foreach (Block block in blocks)
        //    {
        //        if (!isMemberFeature)
        //        {
        //            if (block.Type == BlockType.Member) isMemberFeature = true;
        //        }
        //        else if ((block.Type == BlockType.Directive) || (block.Type == BlockType.Statement))
        //        {
        //            throw new TemplateException(block, "类成员定义后不可以有指令和语句！");
        //        }
        //    }
        //    if (isMemberFeature)
        //    {
        //        Block block2 = blocks[blocks.Count - 1];
        //        if ((block2.Type != BlockType.Member) && ((block2.Type != BlockType.Text) || !allNewlineRegex.Match(block2.Text).Success))
        //        {
        //            throw new TemplateException(block2, "类成员定义后只可以有文本或全是换行的代码语句！");
        //        }
        //    }
        //}
        #endregion

        #region 分析指令
        private static Regex directiveEscapeFindingRegex = new Regex("\\\\+(?=\")|\\\\+(?=$)", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static Regex directiveParsingRegex = new Regex("(?<pname>\\S+?)\\s*=\\s*\"(?<pvalue>.*?)(?<=[^\\\\](\\\\\\\\)*)\"|(?<name>\\S+)", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        /// <summary>
        /// 分析指令块
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static Directive ParseDirectiveBlock(Block block)
        {
            if (block == null) throw new ArgumentNullException("block");

            if (!ValidateDirectiveString(block)) throw new TemplateException(block, "指令格式错误！");

            MatchCollection matchs = directiveParsingRegex.Matches(block.Text);
            String directiveName = null;
            Dictionary<String, String> parameters = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in matchs)
            {
                Group group;
                if ((group = match.Groups["name"]).Success)
                {
                    directiveName = group.Value;
                }
                else
                {
                    String key = null;
                    String valueString = null;
                    if ((group = match.Groups["pname"]).Success) key = group.Value;
                    if ((group = match.Groups["pvalue"]).Success) valueString = group.Value;

                    if ((key != null) && (valueString != null))
                    {
                        if (parameters.ContainsKey(key)) throw new TemplateException(block, String.Format("指令中已存在名为[{0}]的参数！", key));

                        valueString = directiveEscapeFindingRegex.Replace(valueString, escapeReplacingEvaluator);
                        parameters.Add(key, valueString);
                    }
                }
            }
            if (directiveName != null) return new Directive(directiveName, parameters, block);

            return null;
        }

        private static Regex nameValidatingRegex = new Regex(@"^\s*[\w\.]+\s+", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static Regex paramValueValidatingRegex = new Regex("[\\w\\.]+\\s*=\\s*\"(.*?)(?<=[^\\\\](\\\\\\\\)*)\"\\s*", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        /// <summary>
        /// 验证指令字符串格式是否正确
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private static Boolean ValidateDirectiveString(Block block)
        {
            Match match = nameValidatingRegex.Match(block.Text);
            if (!match.Success) return false;

            Int32 length = match.Length;
            MatchCollection matchs = paramValueValidatingRegex.Matches(block.Text);
            if (matchs.Count == 0) return false;

            foreach (Match match2 in matchs)
            {
                if (match2.Index != length) return false;

                length += match2.Length;
            }
            if (length != block.Text.Length) return false;

            return true;
        }
        #endregion

        #region 优化处理
        private static Regex allNewlineRegex = new Regex(@"^\s*$", RegexOptions.Singleline | RegexOptions.Compiled);
        private static Regex newlineAtLineStartRegex = new Regex(@"^[ \t]*((\r\n)|\n)", RegexOptions.Singleline | RegexOptions.Compiled);
        private static Regex newlineAtLineEndRegex = new Regex(@"(?=(\r\n)|\n)[ \t]*$", RegexOptions.Singleline | RegexOptions.Compiled);
        /// <summary>
        /// 删除多余的换行
        /// </summary>
        /// <remarks>
        /// 本方法的目的是为了让模版的编写更加随意灵活，有以下功能：
        /// 1，文本后面如果是语句代码段或者类成员代码段，允许忽略代码段前的一个换行和空白符 (?=(\r\n)|\n)[ \t]*$
        /// 2，文本前面如果是语句代码段或者类成员代码段，允许忽略代码段后面的空白以及一个换行符 ^[ \t]*((\r\n)|\n)
        /// 3，语句代码段和类成员代码段，允许忽略之间的空白和换行 ^\s*$
        /// </remarks>
        /// <param name="blocks"></param>
        internal static void StripExtraNewlines(List<Block> blocks)
        {
            for (Int32 i = 0; i < blocks.Count; i++)
            {
                Block block = blocks[i];
                if (block.Type != BlockType.Text) continue;

                if (i > 0)
                {
                    Block last = blocks[i - 1];
                    if (last.Type != BlockType.Expression && last.Type != BlockType.Text)
                    {
                        // 占位符块，不是第一块，前一块又不是表达式和占位符时，忽略一个换行
                        block.Text = newlineAtLineStartRegex.Replace(block.Text, String.Empty);
                    }
                    if (last.Type == BlockType.Member && (i == blocks.Count - 1 || blocks[i + 1].Type == BlockType.Member))
                    {
                        // 占位符块，不是第一块，前一块和后一块都是类结构时，忽略由换行组成的占位符
                        block.Text = allNewlineRegex.Replace(block.Text, String.Empty);
                    }
                }
                if (i < blocks.Count - 1)
                {
                    Block next = blocks[i + 1];
                    if (next.Type != BlockType.Expression && next.Type != BlockType.Text)
                    {
                        // 占位符块，不是最后一块，下一块又不是表达式和占位符时，忽略一个换行
                        block.Text = newlineAtLineEndRegex.Replace(block.Text, String.Empty);
                    }
                }
            }
            Predicate<Block> match = delegate(Block b)
            {
                // 类成员代码块可能需要空的结束符
                if (b.Type == BlockType.Member) return false;
                return String.IsNullOrEmpty(b.Text);
            };
            // 删除空块
            blocks.RemoveAll(match);
        }
        #endregion
    }
}