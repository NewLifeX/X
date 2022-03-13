using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using NewLife;
using NewLife.Collections;

namespace XCode.DataAccessLayer
{
    /// <summary>SQL查询语句生成器</summary>
    /// <remarks>
    /// 查询语句的复杂性，使得多个地方使用起来极为不方面。
    /// 应该以本类作为查询对象，直接从最上层深入到最下层
    /// </remarks>
    public class SelectBuilder
    {
        #region 属性
        /// <summary>分页主键</summary>
        public String Key { get; set; }

        /// <summary>选择列</summary>
        public String Column { get; set; }

        /// <summary>数据表</summary>
        public String Table { get; set; }

        private String _Where;
        /// <summary>条件</summary>
        public String Where { get => _Where; set => _Where = ParseWhere(value); }

        /// <summary>分组</summary>
        public String GroupBy { get; set; }

        /// <summary>分组条件</summary>
        public String Having { get; set; }

        private String _OrderBy;
        /// <summary>排序</summary>
        /// <remarks>给排序赋值时，如果没有指定分页主键，则自动采用排序中的字段</remarks>
        public String OrderBy { get => _OrderBy; set => _OrderBy = ParseOrderBy(value); }

        /// <summary>分页用的Limit语句</summary>
        public String Limit { get; set; }

        /// <summary>参数集合</summary>
        public List<IDataParameter> Parameters { get; set; } = new();
        #endregion

        #region 构造
        /// <summary>实例化一个SQL语句</summary>
        public SelectBuilder() { }

        /// <summary>实例化一个SQL语句</summary>
        /// <param name="sql"></param>
        public SelectBuilder(String sql) => Parse(sql);
        #endregion

        #region 导入SQL
        private const String SelectRegex = @"(?isx-m)
^
\s*\bSelect\s+(?<选择列>(?>[^()]+?|\((?<Open>)|\)(?<-Open>))*?(?(Open)(?!)))
\s+From\s+(?<数据表>(?>[^()]+?|\((?<Open>)|\)(?<-Open>))*?(?(Open)(?!)))
(?:\s+Where\s+(?<条件>(?>[^()]+?|\((?<Open>)|\)(?<-Open>))*?(?(Open)(?!))))?
(?:\s+Group\s+By\s+(?<分组>(?>[^()]+?|\((?<Open>)|\)(?<-Open>))*?(?(Open)(?!))))?
(?:\s+Having\s+(?<分组条件>(?>[^()]+?|\((?<Open>)|\)(?<-Open>))*?(?(Open)(?!))))?
(?:\s+Order\s+By\s+(?<排序>(?>[^()]+?|\((?<Open>)|\)(?<-Open>))*?(?(Open)(?!))))?
$";
        // 可以单独识别SQLite等数据库分页的Limit字句，但那样加大了复杂性
        // (?:\s+(?<Limit>(?>Limit|Rows)\s+(\d+\s*(?>,|To|Offset)\s*)?\d+))?

        // 如果字符串内容里面含有圆括号，这个正则将无法正常工作，字符串边界的单引号也不好用平衡组，可以考虑在匹配前先用正则替换掉字符串

        static readonly Regex regexSelect = new(SelectRegex, RegexOptions.Compiled);

        /// <summary>分析一条SQL</summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public Boolean Parse(String sql)
        {
            var m = regexSelect.Match(sql);
            if (m != null && m.Success)
            {
                Column = (m.Groups["选择列"].Value + "").Trim();
                Table = (m.Groups["数据表"].Value + "").Trim();
                Where = (m.Groups["条件"].Value + "").Trim();
                GroupBy = (m.Groups["分组"].Value + "").Trim();
                Having = (m.Groups["分组条件"].Value + "").Trim();
                OrderBy = (m.Groups["排序"].Value + "").Trim();
                //Limit = m.Groups["Limit"].Value;

                return true;
            }

            return false;
        }

        private static readonly Regex reg_gb = new(@"\bgroup\b\s*\bby\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        String ParseWhere(String value)
        {
            // 里面可能含有分组
            if (!value.IsNullOrEmpty())
            {
                var where = value.ToLower();
                if (where.Contains("group") && where.Contains("by"))
                {
                    var match = reg_gb.Match(value);
                    if (match != null && match.Success)
                    {
                        var gb = value[(match.Index + match.Length)..].Trim();
                        if (GroupBy.IsNullOrEmpty())
                            GroupBy = gb;
                        else
                            GroupBy += ", " + gb;

                        value = value[..match.Index].Trim();
                    }
                }
            }

            if (value == "1=1") value = null;

            return value?.Trim();
        }

        String ParseOrderBy(String value)
        {
            // 分析排序字句，从中分析出分页用的主键
            if (!value.IsNullOrEmpty() && Key.IsNullOrEmpty())
            {
                var p = value.IndexOfAny(new[] { ',', ' ' });
                if (p > 0) Key = value[..p];
            }

            return value?.Trim();
        }
        #endregion

        #region 导出SQL
        /// <summary>已重写。获取本Builder所分析的SQL语句</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var sb = Pool.StringBuilder.Get();
            sb.Append("Select ");
            sb.Append(Column.IsNullOrEmpty() ? "*" : Column);
            sb.Append(" From ");
            sb.Append(Table);
            if (!Where.IsNullOrEmpty()) sb.Append(" Where " + Where);
            if (!GroupBy.IsNullOrEmpty()) sb.Append(" Group By " + GroupBy);
            if (!Having.IsNullOrEmpty()) sb.Append(" Having " + Having);
            if (!OrderBy.IsNullOrEmpty()) sb.Append(" Order By " + OrderBy);
            if (!Limit.IsNullOrEmpty()) sb.Append(Limit.EnsureStart(" "));

            return sb.Put(true);
        }

        /// <summary>获取记录数的语句</summary>
        /// <returns></returns>
        public virtual SelectBuilder SelectCount()
        {
            //SelectBuilder sb = this.Clone();
            //sb.OrderBy = null;
            //// 包含GroupBy时，作为子查询
            //if (!String.IsNullOrEmpty(GroupBy)) sb.Table = String.Format("({0}) as SqlBuilder_T0", sb.ToString());
            //sb.Column = "Count(*)";
            //return sb;

            // 该BUG由@行走江湖（534163320）发现

            // 包含GroupBy时，作为子查询
            var sb = CloneWithGroupBy("XCode_T0", true);
            sb.Column = "Count(*)";
            sb.OrderBy = null;
            return sb;
        }
        #endregion

        #region 方法
        /// <summary>克隆</summary>
        /// <returns></returns>
        public SelectBuilder Clone()
        {
            var sb = new SelectBuilder
            {
                Key = Key,
                Column = Column,
                Table = Table,

                // 直接拷贝字段，避免属性set时触发分析代码
                _Where = _Where,
                _OrderBy = _OrderBy,

                GroupBy = GroupBy,
                Having = Having,
                Limit = Limit,
            };

            sb.Parameters.AddRange(Parameters);

            return sb;
        }

        /// <summary>增加Where条件</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public SelectBuilder AppendWhereAnd(String format, params Object[] args)
        {
            if (!Where.IsNullOrEmpty())
            {
                if (Where.Contains(" ") && Where.ToLower().Contains("or"))
                    Where = $"({Where}) And ";
                else
                    Where += " And ";
            }
            Where += String.Format(format, args);

            return this;
        }

        /// <summary>作为子查询</summary>
        /// <param name="alias">别名，某些数据库可能需要使用as</param>
        /// <param name="trimOrder">SqlServer需要转移OrderBy到外层，Oracle则不能</param>
        /// <returns></returns>
        public SelectBuilder AsChild(String alias, Boolean trimOrder)
        {
            var t = this;
            // 如果包含排序，则必须有Top，否则去掉
            var hasOrderWithoutTop = false;
            if (trimOrder) hasOrderWithoutTop = !String.IsNullOrEmpty(t.OrderBy) && !Column.StartsWithIgnoreCase("top ");
            if (hasOrderWithoutTop)
            {
                t = Clone();
                t.OrderBy = null;
            }

            var builder = new SelectBuilder();
            if (String.IsNullOrEmpty(alias))
                builder.Table = $"({t})";
            else
                builder.Table = $"({t}) {alias}";

            // 把排序加载外层
            if (hasOrderWithoutTop) builder.OrderBy = OrderBy;

            builder.Parameters.AddRange(Parameters);

            return builder;
        }

        /// <summary>处理可能带GroupBy的克隆，如果带有GroupBy，则必须作为子查询，否则简单克隆即可</summary>
        /// <param name="alias">别名，某些数据库可能需要使用as</param>
        /// <param name="trimOrder">SqlServer需要转移OrderBy到外层，Oracle则不能</param>
        /// <returns></returns>
        public SelectBuilder CloneWithGroupBy(String alias, Boolean trimOrder)
        {
            if (String.IsNullOrEmpty(GroupBy))
                return Clone();
            else
                return AsChild(alias, trimOrder);
        }
        #endregion

        #region 辅助方法
        internal SelectBuilder Top(Int64 top, String keyColumn = null)
        {
            var builder = this;
            if (!String.IsNullOrEmpty(keyColumn)) builder.Column = keyColumn;
            if (String.IsNullOrEmpty(builder.Column)) builder.Column = "*";
            builder.Column = $"Top {top} {builder.Column}";

            return builder;
        }
        #endregion

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(SelectBuilder obj) => !obj.Equals(null) ? obj.ToString() : null;
        #endregion
    }
}