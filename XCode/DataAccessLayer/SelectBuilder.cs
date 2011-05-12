using System;
using System.Text;
using System.Text.RegularExpressions;


namespace XCode.DataAccessLayer
{
    /// <summary>
    /// SQL查询语句生成器
    /// </summary>
    /// <remarks>查询语句的复杂性，使得多个地方使用起来极为不方面。
    /// 应该以本类作为查询对象，直接从最上层深入到最下层</remarks>
    public class SelectBuilder
    {
        #region 属性
        private DatabaseType _DbType = DatabaseType.Access;
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DatabaseType DbType
        {
            get { return _DbType; }
            set { _DbType = value; }
        }

        //private Boolean _IsLock;
        ///// <summary>是否锁定，禁止修改</summary>
        //public Boolean IsLock
        //{
        //    get { return _IsLock; }
        //    private set { _IsLock = value; }
        //}
        #endregion

        #region SQL查询语句基本部分
        private String _Column;
        /// <summary>
        /// 选择列
        /// </summary>
        public String Column
        {
            get { return _Column; }
            set { OnChange("Column", value); _Column = value; }
        }

        private String _Table;
        /// <summary>
        /// 数据表
        /// </summary>
        public String Table
        {
            get { return _Table; }
            set { OnChange("Table", value); _Table = value; }
        }

        private String _Where;
        /// <summary>
        /// 条件
        /// </summary>
        public String Where
        {
            get { return _Where; }
            set { OnChange("Where", value); _Where = value; }
        }

        private String _GroupBy;
        /// <summary>
        /// 分组
        /// </summary>
        public String GroupBy
        {
            get { return _GroupBy; }
            set { OnChange("GroupBy", value); _GroupBy = value; }
        }

        private String _Having;
        /// <summary>
        /// 分组条件
        /// </summary>
        public String Having
        {
            get { return _Having; }
            set { OnChange("Having", value); _Having = value; }
        }

        private String _OrderBy;
        /// <summary>
        /// 排序
        /// </summary>
        public String OrderBy
        {
            get { return _OrderBy; }
            set { OnChange("OrderBy", value); _OrderBy = value; }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public SelectBuilder()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbType"></param>
        public SelectBuilder(DatabaseType dbType)
        {
            DbType = dbType;
        }

        //todo 使用正则平衡组分析sql
        //todo 使用静态创建器，优化对象的创建

        ///// <summary>
        ///// 通过分析一条SQL语句来初始化一个实例
        ///// </summary>
        ///// <param name="dbType">数据库类型</param>
        ///// <param name="sql">要分析的SQL语句</param>
        ///// <param name="isLock">是否锁定</param>
        //public SqlBuilder(DatabaseType dbType, String sql, Boolean isLock)
        //{
        //    DbType = dbType;
        //    Parse(sql);
        //    IsLock = isLock;
        //}

        //private static Dictionary<String, SqlBuilder> _SqlBuilderCache = new Dictionary<string, SqlBuilder>();
        ///// <summary>
        ///// 通过分析一条SQL语句来初始化一个实例
        ///// </summary>
        ///// <param name="dbType"></param>
        ///// <param name="sql"></param>
        ///// <returns></returns>
        //public static SqlBuilder Create(DatabaseType dbType, String sql)
        //{
        //    String Key = String.Format("{0}_{1}", dbType, sql);
        //    if (_SqlBuilderCache.ContainsKey(Key)) return _SqlBuilderCache[Key];
        //    lock (_SqlBuilderCache)
        //    {
        //        if (_SqlBuilderCache.ContainsKey(Key)) return _SqlBuilderCache[Key];
        //        SqlBuilder sb = new SqlBuilder(dbType, sql, true);
        //        _SqlBuilderCache.Add(Key, sb);
        //        return sb;
        //    }
        //}
        #endregion

        #region 导入SQL
        private const String SelectRegex = @"(?isx-m)
^
\bSelect\s+(?<选择列>(?>[^()]+?|\((?<Open>)|\)(?<-Open>))*?(?(Open)(?!)))
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
            MatchCollection ms = reg.Matches(sql);
            if (ms != null && ms.Count > 0 && ms[0].Success)
            {
                Match m = ms[0];
                Column = m.Groups["选择列"].Value;
                Table = m.Groups["数据表"].Value;
                Where = m.Groups["条件"].Value;
                GroupBy = m.Groups["分组"].Value;
                Having = m.Groups["分组条件"].Value;
                OrderBy = m.Groups["排序"].Value;

                if (Where == "1=1") Where = null;

                return true;
            }

            return false;

            //return Regex.IsMatch(sql, SelectRegex, RegexOptions.IgnoreCase);
        }
        #endregion

        #region 导出SQL
        private static Regex reg_gb = new Regex(@"\bgroup\b\s*\bby\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //private String _out;
        /// <summary>
        /// 已重写。获取本Builder所分析的SQL语句
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            //if (IsLock && !String.IsNullOrEmpty(_out)) return _out;

            //if (!String.IsNullOrEmpty(Where) && reg_gb.IsMatch(Where)) throw new Exception("请不要在Where中使用GroupBy！");
            if (!String.IsNullOrEmpty(Where))
            {
                Match match = reg_gb.Match(Where);
                if (match != null && match.Success)
                {
                    String gb = Where.Substring(match.Index + match.Length).Trim();
                    if (String.IsNullOrEmpty(GroupBy))
                        GroupBy = gb;
                    else
                        GroupBy += ", " + gb;

                    Where = Where.Substring(0, match.Index).Trim();
                }
            }

            if (Where == "1=1") Where = null;

            StringBuilder sb = new StringBuilder();
            sb.Append("Select ");
            sb.Append(String.IsNullOrEmpty(Column) ? "*" : Column);
            sb.Append(" From ");
            sb.Append(Table);
            if (!String.IsNullOrEmpty(Where)) sb.Append(" Where " + Where);
            if (!String.IsNullOrEmpty(GroupBy)) sb.Append(" Group By " + GroupBy);
            if (!String.IsNullOrEmpty(Having)) sb.Append(" Having " + Having);
            if (!String.IsNullOrEmpty(OrderBy)) sb.Append(" Order By " + OrderBy);
            //_out = RevTranSql(sb.ToString());
            //return _out;
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

        #region 辅助函数
        ///// <summary>
        ///// SQL转义列表
        ///// </summary>
        //private Dictionary<Int32, String> SqlTranList = new Dictionary<Int32, String>();

        ///// <summary>
        ///// 反转义列表
        ///// </summary>
        //private Dictionary<String, String> RevCache = new Dictionary<String, String>();

        ///// <summary>
        ///// SQL转义。去除所有子查询，单引号 以及 双引号
        ///// </summary>
        ///// <param name="sql"></param>
        ///// <returns></returns>
        //private String TranSql(String sql)
        //{
        //    SqlTranList.Clear();
        //    sql = TranSql(sql, @"\(", @"\)");
        //    sql = TranSql(sql, "'", "'");
        //    sql = TranSql(sql, "\"", "\"");
        //    return sql;
        //}

        ///// <summary>
        ///// 转义
        ///// </summary>
        ///// <param name="sql"></param>
        ///// <param name="start"></param>
        ///// <param name="end"></param>
        ///// <returns></returns>
        //private String TranSql(String sql, String start, String end)
        //{
        //    Regex reg = new Regex(String.Format("{0}.*?{1}", start, end), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        //    MatchCollection ms = reg.Matches(sql);
        //    if (ms.Count < 1) return sql;
        //    foreach (Match m in ms)
        //    {
        //        if (!String.IsNullOrEmpty(m.Groups[0].Value))
        //        {
        //            sql = sql.Replace(m.Groups[0].Value, String.Format("#{0}#", SqlTranList.Count));
        //            SqlTranList.Add(SqlTranList.Count, m.Groups[0].Value);
        //        }
        //    }
        //    // 已经完成最内一层的转义，下面递归对外面进行转义
        //    return TranSql(sql, start, end);
        //}

        ///// <summary>
        ///// 反转义SQL
        ///// </summary>
        ///// <param name="sql"></param>
        ///// <returns></returns>
        //private String RevTranSql(String sql)
        //{
        //    Regex reg = new Regex(@"#\d+#");
        //    if (!reg.IsMatch(sql)) return sql;
        //    foreach (Int32 k in SqlTranList.Keys)
        //    {
        //        sql = sql.Replace(String.Format("#{0}#", k), SqlTranList[k]);
        //    }
        //    return RevTranSql(sql);
        //}

        ///// <summary>
        ///// 反转义一层。实现由外到内展开括号
        ///// </summary>
        ///// <param name="str"></param>
        ///// <returns></returns>
        //public String RevTranTop(String str)
        //{
        //    if (RevCache.ContainsKey(str)) return RevCache[str];
        //    lock (this)
        //    {
        //        if (RevCache.ContainsKey(str)) return RevCache[str];
        //        String tem = str;
        //        foreach (Int32 k in SqlTranList.Keys)
        //        {
        //            tem = tem.Replace(String.Format("#{0}#", k), SqlTranList[k]);
        //        }
        //        RevCache.Add(str, tem);
        //        return tem;
        //    }
        //}
        #endregion

        #region 方法
        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public SelectBuilder Clone()
        {
            SelectBuilder sb = new SelectBuilder(DbType);
            sb.Column = this.Column;
            sb.Table = this.Table;
            sb.Where = this.Where;
            sb.OrderBy = this.OrderBy;
            sb.GroupBy = this.GroupBy;
            sb.Having = this.Having;
            return sb;
        }

        private void OnChange(String name, String value)
        {
            //if (IsLock) throw new InvalidOperationException("生成器已被锁定，禁止修改，请使用Clone获取副本后修改。");
        }
        #endregion
    }
}