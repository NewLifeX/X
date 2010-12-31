using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Data.Common;

namespace XCode.DataAccessLayer
{
    abstract class DatabaseMeta : IDatabaseMeta
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。外部DAL数据库类请使用Other
        /// </summary>
        public abstract DatabaseType DbType { get; }

        /// <summary>工厂</summary>
        public abstract DbProviderFactory Factory { get; }
        #endregion

        #region 分页
        /// <summary>
        /// 构造分页SQL
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public virtual String PageSplit(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0 && maximumRows < 1) return sql;

            #region Max/Min分页
            // 如果要使用max/min分页法，首先keyColumn必须有asc或者desc
            if (!String.IsNullOrEmpty(keyColumn))
            {
                String kc = keyColumn.ToLower();
                if (kc.EndsWith(" desc") || kc.EndsWith(" asc") || kc.EndsWith(" unknown"))
                {
                    String str = PageSplitMaxMin(sql, startRowIndex, maximumRows, keyColumn);
                    if (!String.IsNullOrEmpty(str)) return str;
                    keyColumn = keyColumn.Substring(0, keyColumn.IndexOf(" "));
                }
            }
            #endregion

            //检查简单SQL。为了让生成分页SQL更短
            String tablename = CheckSimpleSQL(sql);
            if (tablename != sql)
                sql = tablename;
            else
                sql = String.Format("({0}) XCode_Temp_a", sql);

            // 取第一页也不用分页。把这代码放到这里，主要是数字分页中要自己处理这种情况
            if (startRowIndex <= 0 && maximumRows > 0)
                return String.Format("Select Top {0} * From {1}", maximumRows, sql);

            if (String.IsNullOrEmpty(keyColumn)) throw new ArgumentNullException("keyColumn", "这里用的not in分页算法要求指定主键列！");

            if (maximumRows < 1)
                sql = String.Format("Select * From {1} Where {2} Not In(Select Top {0} {2} From {1})", startRowIndex, sql, keyColumn);
            else
                sql = String.Format("Select Top {0} * From {1} Where {2} Not In(Select Top {3} {2} From {1})", maximumRows, sql, keyColumn, startRowIndex);
            return sql;
        }

        protected String PageSplitMaxMin(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            // 唯一键的顺序。默认为Empty，可以为asc或desc，如果有，则表明主键列是数字唯一列，可以使用max/min分页法
            Boolean isAscOrder = keyColumn.ToLower().EndsWith(" asc");
            // 是否使用max/min分页法
            Boolean canMaxMin = false;

            // 如果sql最外层有排序，且唯一的一个排序字段就是keyColumn时，可用max/min分页法
            // 如果sql最外层没有排序，其排序不是unknown，可用max/min分页法
            MatchCollection ms = Regex.Matches(sql, @"\border\s*by\b([^)]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (ms != null && ms.Count > 0 && ms[0].Index > 0)
            {
                // 取第一页也不用分页。把这代码放到这里，主要是数字分页中要自己处理这种情况
                if (startRowIndex <= 0 && maximumRows > 0)
                    return String.Format("Select Top {0} * From {1}", maximumRows, CheckSimpleSQL(sql));

                keyColumn = keyColumn.Substring(0, keyColumn.IndexOf(" "));
                sql = sql.Substring(0, ms[0].Index);

                String strOrderBy = ms[0].Groups[1].Value.Trim();
                // 只有一个排序字段
                if (!String.IsNullOrEmpty(strOrderBy) && !strOrderBy.Contains(","))
                {
                    // 有asc或者desc。没有时，默认为asc
                    if (strOrderBy.ToLower().EndsWith(" desc"))
                    {
                        String str = strOrderBy.Substring(0, strOrderBy.Length - " desc".Length).Trim();
                        // 排序字段等于keyColumn
                        if (str.ToLower() == keyColumn.ToLower())
                        {
                            isAscOrder = false;
                            canMaxMin = true;
                        }
                    }
                    else if (strOrderBy.ToLower().EndsWith(" asc"))
                    {
                        String str = strOrderBy.Substring(0, strOrderBy.Length - " asc".Length).Trim();
                        // 排序字段等于keyColumn
                        if (str.ToLower() == keyColumn.ToLower())
                        {
                            isAscOrder = true;
                            canMaxMin = true;
                        }
                    }
                    else if (!strOrderBy.Contains(" ")) // 不含空格，是唯一排序字段
                    {
                        // 排序字段等于keyColumn
                        if (strOrderBy.ToLower() == keyColumn.ToLower())
                        {
                            isAscOrder = true;
                            canMaxMin = true;
                        }
                    }
                }
            }
            else
            {
                // 取第一页也不用分页。把这代码放到这里，主要是数字分页中要自己处理这种情况
                if (startRowIndex <= 0 && maximumRows > 0)
                {
                    //数字分页中，业务上一般使用降序，Entity类会给keyColumn指定降序的
                    //但是，在第一页的时候，没有用到keyColumn，而数据库一般默认是升序
                    //这时候就会出现第一页是升序，后面页是降序的情况了。这里改正这个BUG
                    if (keyColumn.ToLower().EndsWith(" desc") || keyColumn.ToLower().EndsWith(" asc"))
                        return String.Format("Select Top {0} * From {1} Order By {2}", maximumRows, CheckSimpleSQL(sql), keyColumn);
                    else
                        return String.Format("Select Top {0} * From {1}", maximumRows, CheckSimpleSQL(sql));
                }

                if (!keyColumn.ToLower().EndsWith(" unknown")) canMaxMin = true;

                keyColumn = keyColumn.Substring(0, keyColumn.IndexOf(" "));
            }

            if (canMaxMin)
            {
                if (maximumRows < 1)
                    sql = String.Format("Select * From {1} Where {2}{3}(Select {4}({2}) From (Select Top {0} {2} From {1} Order By {2} {5}) XCode_Temp_a) Order By {2} {5}", startRowIndex, CheckSimpleSQL(sql), keyColumn, isAscOrder ? ">" : "<", isAscOrder ? "max" : "min", isAscOrder ? "Asc" : "Desc");
                else
                    sql = String.Format("Select Top {0} * From {1} Where {2}{4}(Select {5}({2}) From (Select Top {3} {2} From {1} Order By {2} {6}) XCode_Temp_a) Order By {2} {6}", maximumRows, CheckSimpleSQL(sql), keyColumn, startRowIndex, isAscOrder ? ">" : "<", isAscOrder ? "max" : "min", isAscOrder ? "Asc" : "Desc");
                return sql;
            }
            return null;
        }

        /// <summary>
        /// 检查简单SQL语句，比如Select * From table
        /// </summary>
        /// <param name="sql">待检查SQL语句</param>
        /// <returns>如果是简单SQL语句则返回表名，否则返回子查询(sql) XCode_Temp_a</returns>
        protected static String CheckSimpleSQL(String sql)
        {
            if (String.IsNullOrEmpty(sql)) return sql;

            Regex reg = new Regex(@"^\s*select\s+\*\s+from\s+([\w\[\]\""\""\']+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            MatchCollection ms = reg.Matches(sql);
            if (ms == null || ms.Count < 1 || ms[0].Groups.Count < 2 ||
                String.IsNullOrEmpty(ms[0].Groups[1].Value)) return String.Format("({0}) XCode_Temp_a", sql);
            return ms[0].Groups[1].Value;
        }

        /// <summary>
        /// 构造分页SQL
        /// </summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public virtual String PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            if (String.IsNullOrEmpty(builder.GroupBy) && startRowIndex <= 0 && maximumRows > 0) return PageSplit(builder, maximumRows);

            return PageSplit(builder.ToString(), startRowIndex, maximumRows, keyColumn);
        }

        protected virtual String PageSplit(SelectBuilder builder, Int32 maximumRows)
        {
            SelectBuilder sb = builder.Clone();
            if (String.IsNullOrEmpty(builder.Column)) builder.Column = "*";
            builder.Column = String.Format("Top {0} {1}", maximumRows, builder.Column);
            return builder.ToString();
        }
        #endregion

        #region 数据库特性
        /// <summary>
        /// 当前时间函数
        /// </summary>
        public virtual String DateTimeNow { get { return "now()"; } }

        /// <summary>
        /// 最小时间
        /// </summary>
        public virtual DateTime DateTimeMin { get { return DateTime.MinValue; } }

        /// <summary>
        /// 格式化时间为SQL字符串
        /// </summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public virtual String FormatDateTime(DateTime dateTime)
        {
            return String.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dateTime);
        }

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord">表名</param>
        /// <returns></returns>
        public virtual String FormatKeyWord(String keyWord)
        {
            return keyWord;
        }
        #endregion
    }
}