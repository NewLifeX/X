using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OracleClient;
using System.Text.RegularExpressions;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// Oracle数据库
    /// </summary>
    internal class Oracle : Database
    {
        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { return OracleClientFactory.Instance; }
        }

        /// <summary>
        /// 已重写。获取分页
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">主键列。用于not in分页</param>
        /// <returns></returns>
        public override String PageSplit(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            // return base.Query(sql, startRowIndex, maximumRows, key);
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1)
                    return sql;
                else
                    return String.Format("Select * From ({1}) XCode_Temp_a Where rownum<={0}", maximumRows + 1, sql);
            }
            if (maximumRows < 1)
                sql = String.Format("Select * From ({1}) XCode_Temp_a Where rownum>={0}", startRowIndex + 1, sql);
            else
                sql = String.Format("Select * From (Select XCode_Temp_a.*, rownum as my_rownum From ({1}) XCode_Temp_a Where rownum<={2}) XCode_Temp_b Where my_rownum>={0}", startRowIndex + 1, sql, startRowIndex + maximumRows);
            //sql = String.Format("Select * From ({1}) a Where rownum>={0} and rownum<={2}", startRowIndex, sql, startRowIndex + maximumRows - 1);
            return sql;
        }

        public override string PageSplit(SelectBuilder builder, int startRowIndex, int maximumRows, string keyColumn)
        {
            return PageSplit(builder.ToString(), startRowIndex, maximumRows, keyColumn);
        }

        /// <summary>
        /// 返回数据库类型。外部DAL数据库类请使用Other
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.Oracle; }
        }

        #region 基本方法 查询/执行
        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int32 InsertAndGetIdentity(String sql)
        {
            throw new NotSupportedException("Oracle数据库不支持插入后返回新增行的自动编号！");
        }
        #endregion

        /// <summary>
        /// 取得所有表构架
        /// </summary>
        /// <returns></returns>
        public override List<XTable> GetTables()
        {
            List<XTable> list = null;
            try
            {
                String user = Owner;
                if (String.IsNullOrEmpty(user))
                {
                    Regex reg = new Regex(@";user id=\b(\w+)\b;", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                    Match m = reg.Match(Conn.ConnectionString);
                    if (m != null) user = m.Groups[1].Value;
                }

                if (String.Equals(user, "system")) user = null;

                DataTable dt = GetSchema("Tables", new String[] { user });
                list = new List<XTable>();
                if (dt != null && dt.Rows != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow drTable in dt.Rows)
                    {
                        if (drTable["TYPE"].ToString() != "User") continue;

                        XTable xt = new XTable();
                        xt.ID = list.Count + 1;
                        xt.Name = drTable["TABLE_NAME"].ToString();
                        xt.Owner = drTable["OWNER"].ToString();
                        xt.Fields = GetFields(xt);

                        list.Add(xt);
                    }
                }
            }
            catch (DbException ex)
            {
                throw new XDbException(this, "取得所有表构架出错！", ex);
            }

            return list;
        }

        /// <summary>
        /// 取得指定表的所有列构架
        /// </summary>
        /// <param name="xt"></param>
        /// <returns></returns>
        protected override List<XField> GetFields(XTable xt)
        {
            //DataColumnCollection columns = GetColumns(xt.Name);
            DataTable dt = GetSchema("Columns", new String[] { xt.Owner, xt.Name });

            List<XField> list = new List<XField>();
            DataRow[] drs = dt.Select("", "ID");
            List<String> pks = GetPrimaryKeys(xt);
            foreach (DataRow dr in drs)
            {
                XField xf = xt.CreateField();
                xf.ID = Int32.Parse(dr["ID"].ToString());
                xf.Name = dr["COLUMN_NAME"].ToString();
                xf.DataType = FieldTypeToClassType(dr["DATATYPE"].ToString());
                xf.Identity = false;

                //if (columns != null && columns.Contains(xf.Name))
                //{
                //    DataColumn dc = columns[xf.Name];
                //    xf.DataType = dc.DataType;
                //}

                xf.Length = dr["LENGTH"] == DBNull.Value ? 0 : Int32.Parse(dr["LENGTH"].ToString());
                xf.Digit = dr["SCALE"] == DBNull.Value ? 0 : Int32.Parse(dr["SCALE"].ToString());

                xf.PrimaryKey = pks != null && pks.Contains(xf.Name);

                if (Type.GetTypeCode(xf.DataType) == TypeCode.Int32 && xf.Digit > 0)
                {
                    xf.DataType = typeof(Double);
                }
                else if (Type.GetTypeCode(xf.DataType) == TypeCode.DateTime)
                {
                    //xf.Length = dr["DATETIME_PRECISION"] == DBNull.Value ? 0 : Int32.Parse(dr["DATETIME_PRECISION"].ToString());
                    xf.NumOfByte = 0;
                    xf.Digit = 0;
                }
                else
                {
                    //if (dr["DATA_TYPE"].ToString() == "130" && dr["COLUMN_FLAGS"].ToString() == "234") //备注类型
                    //{
                    //    xf.Length = Int32.MaxValue;
                    //    xf.NumOfByte = Int32.MaxValue;
                    //}
                    //else
                    {
                        xf.Length = dr["LENGTH"] == DBNull.Value ? 0 : Int32.Parse(dr["LENGTH"].ToString());
                        xf.NumOfByte = 0;
                    }
                    xf.Digit = 0;
                }

                try
                {
                    xf.Nullable = Boolean.Parse(dr["NULLABLE"].ToString());
                }
                catch
                {
                    xf.Nullable = dr["NULLABLE"].ToString() == "Y";
                }

                list.Add(xf);
            }

            return list;
        }

        #region 字段类型到数据类型对照表
        /// <summary>
        /// 字段类型到数据类型对照表
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public override Type FieldTypeToClassType(String type)
        {
            switch (type)
            {
                case "CHAR":
                case "VARCHAR2":
                case "NCHAR":
                case "NVARCHAR2":
                case "CLOB":
                case "NCLOB":
                    return typeof(String);
                case "NUMBER":
                    return typeof(Int32);
                case "FLOAT":
                    return typeof(Double);
                case "DATE":
                case "TIMESTAMP":
                case "TIMESTAMP(6)":
                    return typeof(DateTime);
                case "LONG":
                case "LOB":
                case "RAW":
                case "BLOB":
                    return typeof(Byte[]);
                default:
                    return typeof(String);
            }
        }
        #endregion

        #region 数据库特性
        /// <summary>
        /// 已重载。格式化时间
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public override string FormatDateTime(DateTime dateTime)
        {
            return String.Format("To_Date('{0}', 'YYYYMMDDHH24MISS')", dateTime.ToString("yyyyMMddhhmmss"));
        }

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord">表名</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            //return String.Format("\"{0}\"", keyWord);

            if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");

            Int32 pos = keyWord.LastIndexOf(".");

            if (pos < 0) return "\"" + keyWord + "\"";

            String tn = keyWord.Substring(pos + 1);
            if (tn.StartsWith("\"")) return keyWord;

            return keyWord.Substring(0, pos + 1) + "\"" + tn + "\"";
        }
        #endregion
    }
}