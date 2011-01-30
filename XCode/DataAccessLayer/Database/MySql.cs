using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Web;
using System.Data;

namespace XCode.DataAccessLayer
{
    class MySql : DbBase
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.MySql; }
        }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>
        /// 提供者工厂
        /// </summary>
        static DbProviderFactory dbProviderFactory
        {
            get
            {
                if (_dbProviderFactory == null) _dbProviderFactory = GetProviderFactory("MySql.Data.dll", "MySql.Data.MySqlClient.MySqlClientFactory");

                return _dbProviderFactory;
            }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { return dbProviderFactory; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 创建数据库会话
        /// </summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession()
        {
            return new MySqlSession();
        }

        /// <summary>
        /// 创建元数据对象
        /// </summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new MySqlMetaData();
        }
        #endregion

        #region 分页
        /// <summary>
        /// 已重写。获取分页
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">主键列。用于not in分页</param>
        /// <returns></returns>
        public override string PageSplit(string sql, Int32 startRowIndex, Int32 maximumRows, string keyColumn)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1)
                    return sql;
                else
                    return String.Format("{0} limit {1}", sql, maximumRows);
            }
            if (maximumRows < 1)
                throw new NotSupportedException("不支持取第几条数据之后的所有数据！");
            else
                sql = String.Format("{0} limit {1}, {2}", sql, startRowIndex, maximumRows);
            return sql;
        }

        public override string PageSplit(SelectBuilder builder, int startRowIndex, int maximumRows, string keyColumn)
        {
            return PageSplit(builder.ToString(), startRowIndex, maximumRows, keyColumn);
        }
        #endregion
    }

    /// <summary>
    /// MySql数据库
    /// </summary>
    internal class MySqlSession : DbSession
    {
        #region 基本方法 查询/执行
        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(string sql)
        {
            ExecuteTimes++;
            //SQLServer写法
            sql = "SET NOCOUNT ON;" + sql + ";Select LAST_INSERT_ID()";
            if (Debug) WriteLog(sql);
            try
            {
                DbCommand cmd = PrepareCommand();
                cmd.CommandText = sql;
                return Int64.Parse(cmd.ExecuteScalar().ToString());
            }
            catch (DbException ex)
            {
                throw OnException(ex, sql);
            }
            finally { AutoClose(); }
        }
        #endregion
    }

    /// <summary>
    /// MySql元数据
    /// </summary>
    class MySqlMetaData : DbMetaData
    {
        protected override void FixField(XField field, DataRow dr)
        {
            // 修正原始类型
            String rawType = null;
            if (TryGetDataRowValue<String>(dr, "COLUMN_TYPE", out rawType)) field.RawType = rawType;

            // 修正自增字段
            String extra = null;
            if (TryGetDataRowValue<String>(dr, "EXTRA", out extra) && extra == "auto_increment") field.Identity = true;

            base.FixField(field, dr);
        }

        protected override DataRow[] FindDataType(XField field, string typeName, bool? isLong)
        {
            DataRow[] drs = base.FindDataType(field, typeName, isLong);
            if (drs != null && drs.Length > 1)
            {
                // 无符号/有符号
                if (!String.IsNullOrEmpty(field.RawType))
                {
                    Boolean IsUnsigned = field.RawType.ToLower().Contains("unsigned");

                    foreach (DataRow dr in drs)
                    {
                        String format = GetDataRowValue<String>(dr, "CreateFormat");

                        if (IsUnsigned && format.ToLower().Contains("unsigned"))
                            return new DataRow[] { dr };
                        else if (!IsUnsigned && !format.ToLower().Contains("unsigned"))
                            return new DataRow[] { dr };
                    }
                }
            }
            return drs;
        }

        //protected override void SetFieldType(XField field, string typeName)
        //{
        //    DataTable dt = DataTypes;
        //    if (dt == null) return;

        //    DataRow[] drs = FindDataType(field, typeName, null);
        //    if (drs == null || drs.Length < 1) return;

        //    // 修正原始类型
        //    String rawType = null;
        //    if (TryGetDataRowValue<String>(drs[0], "COLUMN_TYPE", out rawType)) field.RawType = rawType;

        //    base.SetFieldType(field, typeName);
        //}
    }
}