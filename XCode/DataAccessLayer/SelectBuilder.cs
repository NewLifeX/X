using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NewLife.Caching;
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
        public String Key
        {
            get { return Keys != null && Keys.Length > 0 ? Keys[0] : null; }
            set { Keys = new String[] { value }; }
        }

        /// <summary>分页主键组</summary>
        public String[] Keys { get; set; }

        /// <summary>是否降序</summary>
        public Boolean IsDesc
        {
            get { return IsDescs != null && IsDescs.Length > 0 ? IsDescs[0] : false; }
            set { IsDescs = new Boolean[] { value }; }
        }

        /// <summary>主键组是否降序</summary>
        public Boolean[] IsDescs { get; set; }

        /// <summary>是否整数自增主键</summary>
        public Boolean IsInt { get; set; }

        /// <summary>分页主键排序</summary>
        public String KeyOrder
        {
            get
            {
                if (Keys == null || Keys.Length < 1) return null;

                return Join(Keys, IsDescs);
            }
        }

        /// <summary>分页主键反排序</summary>
        public String ReverseKeyOrder
        {
            get
            {
                if (Keys == null || Keys.Length < 1) return null;

                // 把排序反过来
                var isdescs = new Boolean[Keys.Length];
                for (var i = 0; i < isdescs.Length; i++)
                {
                    if (IsDescs != null && IsDescs.Length > i)
                        isdescs[i] = !IsDescs[i];
                    else
                        isdescs[i] = true;
                }
                return Join(Keys, isdescs);
            }
        }

        /// <summary>排序字段是否唯一且就是主键</summary>
        public Boolean KeyIsOrderBy
        {
            get
            {
                if (String.IsNullOrEmpty(Key)) return false;

                var keys = Split(OrderBy, out var isdescs);

                return keys != null && keys.Length == 1 && keys[0].EqualIgnoreCase(Key);
            }
        }
        #endregion

        #region 构造
        /// <summary>实例化一个SQL语句</summary>
        public SelectBuilder()
        {
        }
        #endregion

        #region SQL查询语句基本部分
        /// <summary>选择列</summary>
        public String Column { get; set; }

        /// <summary>数据表</summary>
        public String Table { get; set; }

        private static Regex reg_gb = new Regex(@"\bgroup\b\s*\bby\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private String _Where;
        /// <summary>条件</summary>
        public String Where
        {
            get { return _Where; }
            set
            {
                _Where = value;

                // 里面可能含有分组
                if (!String.IsNullOrEmpty(_Where))
                {
                    var where = _Where.ToLower();
                    if (where.Contains("group") && where.Contains("by"))
                    {
                        var match = reg_gb.Match(_Where);
                        if (match != null && match.Success)
                        {
                            var gb = _Where.Substring(match.Index + match.Length).Trim();
                            if (String.IsNullOrEmpty(GroupBy))
                                GroupBy = gb;
                            else
                                GroupBy += ", " + gb;

                            _Where = _Where.Substring(0, match.Index).Trim();
                        }
                    }
                }

                if (_Where == "1=1") _Where = null;
            }
        }

        /// <summary>分组</summary>
        public String GroupBy { get; set; }

        /// <summary>分组条件</summary>
        public String Having { get; set; }

        private String _OrderBy;
        /// <summary>排序</summary>
        /// <remarks>给排序赋值时，如果没有指定分页主键，则自动采用排序中的字段</remarks>
        public String OrderBy
        {
            get { return _OrderBy; }
            set
            {
                _OrderBy = value;

                // 分析排序字句，从中分析出分页用的主键
                if (!String.IsNullOrEmpty(_OrderBy))
                {
                    var keys = Split(_OrderBy, out var isdescs);

                    if (keys != null && keys.Length > 0)
                    {
                        // 2012-02-16 排序字句里面可能包含有SQLite等的分页字句，不能随便的优化
                        //// 如果排序不包含括号，可以优化排序
                        //if (!_OrderBy.Contains("(")) _OrderBy = Join(keys, isdescs);

                        if (Keys == null || Keys.Length < 1)
                        {
                            Keys = keys;
                            IsDescs = isdescs;
                        }
                    }
                }
            }
        }

        /// <summary>分页用的Limit语句</summary>
        public String Limit { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>选择列，为空时为*</summary>
        public String ColumnOrDefault => Column.IsNullOrEmpty() ? "*" : Column;

        /// <summary>选择列，去除聚合，为空时为*</summary>
        public String ColumnNoAggregate
        {
            get
            {
                if (Column.IsNullOrEmpty() || Column == "*") return "*";

                // 分解重构，把聚合函数干掉
                var ss = Column.Split(",");
                var sb = Pool.StringBuilder.Get();
                foreach (var item in ss)
                {
                    if (sb.Length > 0) sb.Append(", ");

                    // 聚合函数有三种，等号、AS、空格
                    if (item.Contains("="))
                        sb.Append(item.Substring(null, "="));
                    else if (item.ToLower().Contains(" as "))
                    {
                        var p = item.LastIndexOf(' ');
                        sb.Append(item.Substring(p + 1));
                    }
                    else if (item.Contains(" "))
                    {
                        var p = item.LastIndexOf(' ');
                        sb.Append(item.Substring(p + 1));
                    }
                    else
                        sb.Append(item);
                }

                return sb.Put(true);
            }
        }

        /// <summary>参数集合</summary>
        public List<IDataParameter> Parameters { get; set; } = new List<IDataParameter>();
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

        static Regex regexSelect = new Regex(SelectRegex, RegexOptions.Compiled);

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

        /// <summary>缓存存储</summary>
        public static ICache Store { get; set; } = NewLife.Caching.Cache.Default;

        /// <summary>根据SQL创建，带缓存</summary>
        /// <remarks>
        /// 对于非常复杂的查询语句，正则平衡的处理器消耗很大
        /// </remarks>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static SelectBuilder Create(String sql)
        {
            var key = $"SelectBuilder#{sql}";

            var sb = Store.Get<SelectBuilder>(key);
            if (sb != null) return sb;

            sb = new SelectBuilder();
            sb.Parse(sql);

            Store.Set(key, sb, 10 * 60);

            return sb;
        }
        #endregion

        #region 导出SQL
        /// <summary>已重写。获取本Builder所分析的SQL语句</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var sb = Pool.StringBuilder.Get();
            sb.Append("Select ");
            sb.Append(ColumnOrDefault);
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
                Column = Column,
                Table = Table,
                // 直接拷贝字段，避免属性set时触发分析代码
                _Where = _Where,
                _OrderBy = _OrderBy,
                GroupBy = GroupBy,
                Having = Having,

                Keys = Keys,
                IsDescs = IsDescs,
                IsInt = IsInt
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
                    Where = String.Format("({0}) And ", Where);
                else
                    Where += " And ";
            }
            Where += String.Format(format, args);

            return this;
        }

        /// <summary>增加多个字段，必须是当前表普通字段，如果内部是*则不加</summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public SelectBuilder AppendColumn(params String[] columns)
        {
            if (ColumnOrDefault != "*" && columns != null && columns.Length > 0)
            {
                if (String.IsNullOrEmpty(Column))
                    Column = String.Join(",", columns.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
                else
                {
                    // 检查是否已存在该字段
                    var selects = Column.Split(',');
                    selects = selects.Concat(columns).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                    Column = String.Join(",", selects);
                }
            }
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
            if (trimOrder) hasOrderWithoutTop = !String.IsNullOrEmpty(t.OrderBy) && !ColumnOrDefault.StartsWithIgnoreCase("top ");
            if (hasOrderWithoutTop)
            {
                t = Clone();
                t.OrderBy = null;
            }

            var builder = new SelectBuilder();
            if (String.IsNullOrEmpty(alias))
                builder.Table = String.Format("({0})", t.ToString());
            else
                builder.Table = String.Format("({0}) {1}", t.ToString(), alias);

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
        /// <summary>拆分排序字句</summary>
        /// <param name="orderby"></param>
        /// <param name="isdescs"></param>
        /// <returns></returns>
        public static String[] Split(String orderby, out Boolean[] isdescs)
        {
            isdescs = null;
            if (orderby.IsNullOrWhiteSpace()) return null;
            //2014-01-04 Modify by Apex
            //处理order by带有函数的情况，避免分隔时将函数拆分导致错误
            foreach (Match match in Regex.Matches(orderby, @"\([^\)]*\)", RegexOptions.Singleline))
            {
                orderby = orderby.Replace(match.Value, match.Value.Replace(",", "★"));
            }
            var ss = orderby.Trim().Split(",");
            if (ss == null || ss.Length < 1) return null;

            var keys = new String[ss.Length];
            isdescs = new Boolean[ss.Length];

            for (var i = 0; i < ss.Length; i++)
            {
                var ss2 = ss[i].Trim().Split(' ');
                // 拆分名称和排序，不知道是否存在多余一个空格的情况
                if (ss2 != null && ss2.Length > 0)
                {
                    keys[i] = ss2[0].Replace("★", ",");
                    if (ss2.Length > 1 && ss2[1].EqualIgnoreCase("desc")) isdescs[i] = true;
                }
            }
            return keys;
        }

        /// <summary>连接排序字句</summary>
        /// <param name="keys"></param>
        /// <param name="isdescs"></param>
        /// <returns></returns>
        public static String Join(String[] keys, Boolean[] isdescs)
        {
            if (keys == null || keys.Length < 1) return null;

            if (keys.Length == 1) return isdescs != null && isdescs.Length > 0 && isdescs[0] ? keys[0] + " Desc" : keys[0];

            var sb = Pool.StringBuilder.Get();
            for (var i = 0; i < keys.Length; i++)
            {
                if (sb.Length > 0) sb.Append(", ");

                sb.Append(keys[i]);
                if (isdescs != null && isdescs.Length > i && isdescs[i]) sb.Append(" Desc");
            }
            return sb.Put(true);
        }

        internal SelectBuilder Top(Int64 top, String keyColumn = null)
        {
            var builder = this;
            if (!String.IsNullOrEmpty(keyColumn)) builder.Column = keyColumn;
            if (String.IsNullOrEmpty(builder.Column)) builder.Column = "*";
            builder.Column = String.Format("Top {0} {1}", top, builder.Column);

            return builder;
        }
        #endregion

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(SelectBuilder obj)
        {
            return !obj.Equals(null) ? obj.ToString() : null;
        }
        #endregion
    }
}