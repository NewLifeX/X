using System;
using System.Text;
using System.Text.RegularExpressions;
using NewLife;

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
            get { return _Keys != null && _Keys.Length > 0 ? _Keys[0] : null; }
            set { _Keys = new String[] { value }; }
        }

        private String[] _Keys;
        /// <summary>分页主键组</summary>
        public String[] Keys
        {
            get { return _Keys; }
            set { _Keys = value; }
        }

        /// <summary>是否降序</summary>
        public Boolean IsDesc
        {
            get { return _IsDescs != null && _IsDescs.Length > 0 ? _IsDescs[0] : false; }
            set { _IsDescs = new Boolean[] { value }; }
        }

        private Boolean[] _IsDescs;
        /// <summary>主键组是否降序</summary>
        public Boolean[] IsDescs
        {
            get { return _IsDescs; }
            set { _IsDescs = value; }
        }

        private Boolean _IsInt;
        /// <summary>是否整数自增主键</summary>
        public Boolean IsInt
        {
            get { return _IsInt; }
            set { _IsInt = value; }
        }

        /// <summary>分页主键排序</summary>
        public String KeyOrder
        {
            get
            {
                if (_Keys == null || _Keys.Length < 1) return null;

                if (_Keys.Length == 1) return _IsDescs != null && _IsDescs.Length > 0 && _IsDescs[0] ? _Keys[0] + " Desc" : _Keys[0];

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < _Keys.Length; i++)
                {
                    if (sb.Length > 0) sb.Append(", ");

                    sb.Append(_Keys[i]);
                    if (_IsDescs != null && _IsDescs.Length > i && _IsDescs[i]) sb.Append(" Desc");
                }
                return sb.ToString();
            }
        }

        /// <summary>分页主键反排序</summary>
        public String ReverseKeyOrder
        {
            get
            {
                if (_Keys == null || _Keys.Length < 1) return null;

                if (_Keys.Length == 1) return _IsDescs != null && _IsDescs.Length > 0 && _IsDescs[0] ? _Keys[0] : _Keys[0] + " Desc";

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < _Keys.Length; i++)
                {
                    if (sb.Length > 0) sb.Append(", ");

                    sb.Append(_Keys[i]);
                    if (!(_IsDescs != null && _IsDescs.Length > i && _IsDescs[i])) sb.Append(" Desc");
                }
                return sb.ToString();
            }
        }
        #endregion

        #region SQL查询语句基本部分
        private String _Column;
        /// <summary>选择列</summary>
        public String Column { get { return _Column; } set { _Column = value; } }

        private String _Table;
        /// <summary>数据表</summary>
        public String Table { get { return _Table; } set { _Table = value; } }

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
                    String where = _Where.ToLower();
                    if (where.Contains("group") && where.Contains("by"))
                    {
                        Match match = reg_gb.Match(_Where);
                        if (match != null && match.Success)
                        {
                            String gb = _Where.Substring(match.Index + match.Length).Trim();
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

        private String _GroupBy;
        /// <summary>分组</summary>
        public String GroupBy { get { return _GroupBy; } set { _GroupBy = value; } }

        private String _Having;
        /// <summary>分组条件</summary>
        public String Having { get { return _Having; } set { _Having = value; } }

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
                if (!String.IsNullOrEmpty(_OrderBy) && (Keys == null || Keys.Length < 1))
                {
                    String[] ss = _OrderBy.Trim().Split(',');
                    // 拆分名称和排序，不知道是否存在多余一个空格的情况
                    if (ss != null && ss.Length > 0)
                    {
                        String[] keys = new String[ss.Length];
                        Boolean[] bs = new Boolean[ss.Length];

                        for (int i = 0; i < ss.Length; i++)
                        {
                            String[] ss2 = ss[i].Trim().Split(' ');
                            // 拆分名称和排序，不知道是否存在多余一个空格的情况
                            if (ss2 != null && ss2.Length > 0)
                            {
                                keys[i] = ss2[0];
                                if (ss2.Length > 1 && ss2[1].EqualIgnoreCase("desc")) bs[i] = true;
                            }
                        }

                        Keys = keys;
                        IsDescs = bs;
                    }
                }
            }
        }
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
        static Regex regexSelect = new Regex(SelectRegex, RegexOptions.Compiled);

        /// <summary>
        /// 分析一条SQL
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public Boolean Parse(String sql)
        {
            Regex reg = new Regex(SelectRegex, RegexOptions.IgnoreCase);
            //MatchCollection ms = reg.Matches(sql);
            //if (ms != null && ms.Count > 0 && ms[0].Success)
            //{
            //    Match m = ms[0];
            Match m = reg.Match(sql);
            if (m != null && m.Success)
            {
                Column = m.Groups["选择列"].Value;
                Table = m.Groups["数据表"].Value;
                Where = m.Groups["条件"].Value;
                GroupBy = m.Groups["分组"].Value;
                Having = m.Groups["分组条件"].Value;
                OrderBy = m.Groups["排序"].Value;

                return true;
            }

            return false;
        }
        #endregion

        #region 导出SQL
        /// <summary>
        /// 已重写。获取本Builder所分析的SQL语句
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Select ");
            sb.Append(String.IsNullOrEmpty(Column) ? "*" : Column);
            sb.Append(" From ");
            sb.Append(Table);
            if (!String.IsNullOrEmpty(Where)) sb.Append(" Where " + Where);
            if (!String.IsNullOrEmpty(GroupBy)) sb.Append(" Group By " + GroupBy);
            if (!String.IsNullOrEmpty(Having)) sb.Append(" Having " + Having);
            if (!String.IsNullOrEmpty(OrderBy)) sb.Append(" Order By " + OrderBy);

            return sb.ToString();
        }

        /// <summary>
        /// 获取记录数的语句
        /// </summary>
        /// <returns></returns>
        public virtual SelectBuilder SelectCount()
        {
            SelectBuilder sb = this.Clone();
            sb.OrderBy = null;
            // 包含GroupBy时，作为子查询
            if (!String.IsNullOrEmpty(GroupBy)) sb.Table = String.Format("({0}) as SqlBuilder_T0", sb.ToString());
            sb.Column = "Count(*)";
            return sb;
        }
        #endregion

        #region 方法
        /// <summary>克隆</summary>
        /// <returns></returns>
        public SelectBuilder Clone()
        {
            SelectBuilder sb = new SelectBuilder();
            sb.Column = this.Column;
            sb.Table = this.Table;
            // 直接拷贝字段，避免属性set时触发分析代码
            sb._Where = this._Where;
            sb._OrderBy = this._OrderBy;
            sb.GroupBy = this.GroupBy;
            sb.Having = this.Having;

            sb.Keys = this.Keys;
            sb.IsDescs = this.IsDescs;
            sb.IsInt = this.IsInt;

            return sb;
        }

        /// <summary>
        /// 增加Where条件
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public SelectBuilder AppendWhereAnd(String format, params Object[] args)
        {
            if (!String.IsNullOrEmpty(Where))
            {
                if (Where.Contains(" ") && Where.ToLower().Contains("or"))
                    Where = String.Format("({0}) And ", Where);
                else
                    Where += " And ";
            }
            Where += String.Format(format, args);

            return this;
        }
        #endregion
    }
}