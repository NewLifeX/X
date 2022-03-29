using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TDengineDriver;
using TD = TDengineDriver.TDengine;

namespace XCode.TDengine
{
    /// <summary>数据读取器</summary>
    internal class TDengineDataReader : DbDataReader
    {
        #region 属性
        private readonly TDengineCommand _command;
        private readonly CommandBehavior _behavior;
        private readonly List<TDengineMeta> _metas = null;
        private IntPtr _handler;
        private IntPtr _data;

        /// <summary>深度</summary>
        public override Int32 Depth => 0;

        private readonly Int32 _fieldCount;
        /// <summary>字段数</summary>
        public override Int32 FieldCount => _fieldCount;

        private Int32 _rows;
        /// <summary>是否有数据行</summary>
        public override Boolean HasRows => _rows > 0;

        /// <summary>是否已关闭</summary>
        public override Boolean IsClosed => _handler == IntPtr.Zero;

        /// <summary>影响行数</summary>
        public override Int32 RecordsAffected => _rows = TD.AffectRows(_handler);

        /// <summary>名称读取</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Object this[String name] => this[GetOrdinal(name)];

        /// <summary>序号读取</summary>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        public override Object this[Int32 ordinal] => GetValue(ordinal);
        #endregion

        #region 构造
        internal TDengineDataReader(TDengineCommand command, CommandBehavior behavior, IntPtr handler)
        {
            _command = command;
            _behavior = behavior;
            _handler = handler;

            _metas = TD.FetchFields(handler);
            _fieldCount = TD.FieldCount(handler);
            _rows = TD.AffectRows(handler);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            if (disposing) Close();

            base.Dispose(disposing);
        }
        #endregion

        #region 方法
        /// <summary>读取</summary>
        /// <returns></returns>
        public override Boolean Read()
        {
            if (_handler == IntPtr.Zero) throw new InvalidOperationException("读取器已关闭");

            _data = TD.FetchRows(_handler);
            return _data != IntPtr.Zero;
        }

        /// <summary>下一个结果集</summary>
        /// <returns></returns>
        public override Boolean NextResult() => Read();

        /// <summary>关闭读取器</summary>
        public override void Close()
        {
            if (_behavior.HasFlag(CommandBehavior.CloseConnection)) _command.Connection.Close();

            if (_handler != IntPtr.Zero) TD.FreeResult(_handler);
            _handler = IntPtr.Zero;
        }

        /// <summary>获取字段名</summary>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        public override String GetName(Int32 ordinal) => _metas[ordinal].name;

        /// <summary>获取序号</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Int32 GetOrdinal(String name) => _metas.IndexOf(_metas.FirstOrDefault(m => m.name == name));

        /// <summary>获取类型名</summary>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        public override String GetDataTypeName(Int32 ordinal) => GetFieldType(ordinal).Name;

        /// <summary>获取字段类型</summary>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        public override Type GetFieldType(Int32 ordinal)
        {
            var type = (TDengineDataType)_metas[ordinal].type switch
            {
                TDengineDataType.TSDB_DATA_TYPE_BOOL => typeof(Boolean),
                TDengineDataType.TSDB_DATA_TYPE_TINYINT => typeof(SByte),
                TDengineDataType.TSDB_DATA_TYPE_UTINYINT => typeof(Byte),
                TDengineDataType.TSDB_DATA_TYPE_SMALLINT => typeof(Int16),
                TDengineDataType.TSDB_DATA_TYPE_USMALLINT => typeof(UInt16),
                TDengineDataType.TSDB_DATA_TYPE_INT => typeof(Int32),
                TDengineDataType.TSDB_DATA_TYPE_UINT => typeof(UInt32),
                TDengineDataType.TSDB_DATA_TYPE_BIGINT => typeof(Int64),
                TDengineDataType.TSDB_DATA_TYPE_UBIGINT => typeof(UInt64),
                TDengineDataType.TSDB_DATA_TYPE_FLOAT => typeof(Single),
                TDengineDataType.TSDB_DATA_TYPE_DOUBLE => typeof(Double),
                TDengineDataType.TSDB_DATA_TYPE_BINARY => typeof(String),
                TDengineDataType.TSDB_DATA_TYPE_TIMESTAMP => typeof(DateTime),
                TDengineDataType.TSDB_DATA_TYPE_NCHAR => typeof(String),
                _ => typeof(DBNull),
            };
            return type;
        }

        /// <summary>是否空类型</summary>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        public override Boolean IsDBNull(Int32 ordinal) => GetValue(ordinal) == DBNull.Value;

        public override Boolean GetBoolean(Int32 ordinal) => Marshal.ReadByte(GetValuePtr(ordinal)) != 0;
        public override Byte GetByte(Int32 ordinal) => Marshal.ReadByte(GetValuePtr(ordinal));
        public override Char GetChar(Int32 ordinal) => GetFieldValue<Char>(ordinal);
        public override DateTime GetDateTime(Int32 ordinal) => Marshal.ReadInt64(GetValuePtr(ordinal)).ToDateTime().ToLocalTime();
        public virtual DateTimeOffset GetDateTimeOffset(Int32 ordinal) => GetDateTime(ordinal);
        public virtual TimeSpan GetTimeSpan(Int32 ordinal) => TimeSpan.FromMilliseconds(Marshal.ReadInt64(GetValuePtr(ordinal)));
        public override Decimal GetDecimal(Int32 ordinal) => (Decimal)GetValue(ordinal);
        public override Double GetDouble(Int32 ordinal) => (Double)GetValue(ordinal);
        public override Single GetFloat(Int32 ordinal) => (Single)GetValue(ordinal);
        public override Guid GetGuid(Int32 ordinal) => GetFieldValue<Guid>(ordinal);
        public override Int16 GetInt16(Int32 ordinal) => (Int16)GetValue(ordinal);
        public UInt16 GetUInt16(Int32 ordinal) => (UInt16)GetValue(ordinal);
        public override Int32 GetInt32(Int32 ordinal) => (Int32)GetValue(ordinal);
        public UInt32 GetUInt32(Int32 ordinal) => (UInt32)GetValue(ordinal);
        public override Int64 GetInt64(Int32 ordinal) => (Int64)GetValue(ordinal);
        public UInt64 GetUInt64(Int32 ordinal) => (UInt64)GetValue(ordinal);
        public override String GetString(Int32 ordinal) => (String)GetValue(ordinal);

        public override Int64 GetBytes(Int32 ordinal, Int64 dataOffset, Byte[] buffer, Int32 bufferOffset, Int32 length)
        {
            var buf = new Byte[length + bufferOffset];
            Marshal.Copy(GetValuePtr(ordinal), buf, (Int32)dataOffset, length + bufferOffset);
            Array.Copy(buf, bufferOffset, buffer, 0, length);
            return length;
        }

        public override Int64 GetChars(Int32 ordinal, Int64 dataOffset, Char[] buffer, Int32 bufferOffset, Int32 length)
           => throw new NotSupportedException();

        /// <summary>获取字段值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        public override T GetFieldValue<T>(Int32 ordinal) => (T)Convert.ChangeType(GetValue(ordinal), typeof(T));
        #endregion

        #region 辅助
        /// <summary>迭代</summary>
        /// <returns></returns>
        public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

        public IntPtr GetValuePtr(Int32 ordinal) => Marshal.ReadIntPtr(_data, IntPtr.Size * ordinal);

        public static Encoding GetEncoding(Byte[] buf)
        {
            var encodings = new List<Encoding> { Encoding.UTF8, Encoding.BigEndianUnicode, Encoding.Unicode, Encoding.Default };
            var e936 = Encoding.GetEncoding(936);
            if (e936 != null) encodings.Add(e936);

            foreach (var enc in encodings)
            {
                var ps = enc.GetPreamble();
                if (buf.Take(ps.Length).SequenceEqual(ps)) return enc;
            }

            return Encoding.UTF8;
        }

        /// <summary>获取数值</summary>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        public override Object GetValue(Int32 ordinal)
        {
            var data = Marshal.ReadIntPtr(_data, IntPtr.Size * ordinal);
            if (data == IntPtr.Zero) return DBNull.Value;

            var meta = _metas[ordinal];
            var type = (TDengineDataType)meta.type;
            Object rs = type switch
            {
                TDengineDataType.TSDB_DATA_TYPE_BOOL => Marshal.ReadByte(data) != 0,
                TDengineDataType.TSDB_DATA_TYPE_TINYINT => (SByte)Marshal.ReadByte(data),
                TDengineDataType.TSDB_DATA_TYPE_UTINYINT => Marshal.ReadByte(data),
                TDengineDataType.TSDB_DATA_TYPE_SMALLINT => Marshal.ReadInt16(data),
                TDengineDataType.TSDB_DATA_TYPE_USMALLINT => (UInt16)Marshal.ReadInt16(data),
                TDengineDataType.TSDB_DATA_TYPE_INT => Marshal.ReadInt32(data),
                TDengineDataType.TSDB_DATA_TYPE_UINT => (UInt32)Marshal.ReadInt32(data),
                TDengineDataType.TSDB_DATA_TYPE_BIGINT => Marshal.ReadInt64(data),
                TDengineDataType.TSDB_DATA_TYPE_UBIGINT => (UInt64)Marshal.ReadInt64(data),
                TDengineDataType.TSDB_DATA_TYPE_FLOAT => (Single)Marshal.PtrToStructure(data, typeof(Single)),
                TDengineDataType.TSDB_DATA_TYPE_DOUBLE => (Double)Marshal.PtrToStructure(data, typeof(Double)),
                TDengineDataType.TSDB_DATA_TYPE_BINARY => Marshal.PtrToStringAnsi(data, meta.size)?.TrimEnd('\0'),
                TDengineDataType.TSDB_DATA_TYPE_TIMESTAMP => Marshal.ReadInt64(data).ToDateTime().ToLocalTime(),
                _ => DBNull.Value,
            };

            if (type == TDengineDataType.TSDB_DATA_TYPE_NCHAR)
            {
                if (meta.size > 0)
                {
                    var buf = new Byte[meta.size];
                    Marshal.Copy(data, buf, 0, meta.size);

                    rs = GetEncoding(buf).GetString(buf);
                }
                else
                    rs = String.Empty;
            }

            return rs;
        }

        /// <summary>获取数值</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public override Int32 GetValues(Object[] values)
        {
            var count = 0;
            for (var i = 0; i < _fieldCount; i++)
            {
                var obj = GetValue(i);
                if (obj != null)
                {
                    values[i] = obj;
                    count++;
                }
            }
            return count;
        }

        /// <summary>获取架构表</summary>
        /// <returns></returns>
        public override DataTable GetSchemaTable()
        {
            var table = new DataTable("SchemaTable");
            if (_metas != null && _metas.Count > 0)
            {
                var name = new DataColumn(SchemaTableColumn.ColumnName, typeof(String));
                var ordinal = new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof(Int32));
                var size = new DataColumn(SchemaTableColumn.ColumnSize, typeof(Int32));
                var precision = new DataColumn(SchemaTableColumn.NumericPrecision, typeof(Int16));
                var scale = new DataColumn(SchemaTableColumn.NumericScale, typeof(Int16));

                var type = new DataColumn(SchemaTableColumn.DataType, typeof(Type));
                var typeName = new DataColumn("DataTypeName", typeof(String));

                var isLong = new DataColumn(SchemaTableColumn.IsLong, typeof(Boolean));
                var allowDBNull = new DataColumn(SchemaTableColumn.AllowDBNull, typeof(Boolean));

                var unique = new DataColumn(SchemaTableColumn.IsUnique, typeof(Boolean));
                var key = new DataColumn(SchemaTableColumn.IsKey, typeof(Boolean));
                var autoIncrement = new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement, typeof(Boolean));

                var baseCatalogName = new DataColumn(SchemaTableOptionalColumn.BaseCatalogName, typeof(String));
                var baseSchemaName = new DataColumn(SchemaTableColumn.BaseSchemaName, typeof(String));
                var baseTableName = new DataColumn(SchemaTableColumn.BaseTableName, typeof(String));
                var baseColumnName = new DataColumn(SchemaTableColumn.BaseColumnName, typeof(String));

                var baseServerName = new DataColumn(SchemaTableOptionalColumn.BaseServerName, typeof(String));
                var aliased = new DataColumn(SchemaTableColumn.IsAliased, typeof(Boolean));
                var expression = new DataColumn(SchemaTableColumn.IsExpression, typeof(Boolean));

                var columns = table.Columns;

                columns.Add(name);
                columns.Add(ordinal);
                columns.Add(size);
                columns.Add(precision);
                columns.Add(scale);
                columns.Add(unique);
                columns.Add(key);
                columns.Add(baseServerName);
                columns.Add(baseCatalogName);
                columns.Add(baseColumnName);
                columns.Add(baseSchemaName);
                columns.Add(baseTableName);
                columns.Add(type);
                columns.Add(typeName);
                columns.Add(allowDBNull);
                columns.Add(aliased);
                columns.Add(expression);
                columns.Add(autoIncrement);
                columns.Add(isLong);

                for (var i = 0; i < _metas.Count; i++)
                {
                    var row = table.NewRow();

                    row[name] = GetName(i);
                    row[ordinal] = i;
                    row[size] = _metas[i].size;
                    row[precision] = DBNull.Value;
                    row[scale] = DBNull.Value;
                    row[baseServerName] = _command.Connection.DataSource;
                    row[baseCatalogName] = _command.Connection.Database;
                    var columnName = GetName(i);
                    row[baseColumnName] = columnName;
                    row[baseSchemaName] = DBNull.Value;
                    row[baseTableName] = String.Empty;
                    row[type] = GetFieldType(i);
                    row[typeName] = GetDataTypeName(i);
                    row[aliased] = columnName != GetName(i);
                    row[expression] = columnName == null;
                    row[isLong] = DBNull.Value;
                    if (i == 0)
                    {
                        row[key] = true;
                        row[type] = GetFieldType(i);
                        row[typeName] = GetDataTypeName(i);
                    }
                    table.Rows.Add(row);
                }
            }

            return table;
        }
        #endregion
    }
}