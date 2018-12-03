using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Data
{
    /// <summary>数据表</summary>
    public class DbTable : IEnumerable<DbRow>, ICloneable
    {
        #region 属性
        /// <summary>数据列</summary>
        public String[] Columns { get; set; }

        /// <summary>数据列类型</summary>
        public Type[] Types { get; set; }

        /// <summary>数据行</summary>
        public IList<Object[]> Rows { get; set; }

        /// <summary>总函数</summary>
        public Int32 Total { get; set; }
        #endregion

        #region 构造
        #endregion

        #region 从数据库读取
        /// <summary>读取数据</summary>
        /// <param name="dr"></param>
        public void Read(IDataReader dr)
        {
            var count = dr.FieldCount;

            // 字段
            var cs = new String[count];
            var ts = new Type[count];
            for (var i = 0; i < count; i++)
            {
                if (cs[i] == null) cs[i] = dr.GetName(i);
                if (ts[i] == null) ts[i] = dr.GetFieldType(i);
            }
            Columns = cs;
            Types = ts;

            // 数据
            var rs = new List<Object[]>();
            while (dr.Read())
            {
                var row = new Object[count];
                for (var i = 0; i < count; i++)
                {
                    var val = dr[i];
                    if (val == DBNull.Value) val = GetDefault(ts[i].GetTypeCode());
                    row[i] = val;
                }
                rs.Add(row);
            }
            Rows = rs;

            Total = rs.Count;
        }
        #endregion

        #region 二进制读取
        private const Byte _Ver = 1;

        /// <summary>从数据流读取</summary>
        /// <param name="stream"></param>
        public void Read(Stream stream)
        {
            var bn = new Binary
            {
                EncodeInt = true,
                Stream = stream,
            };

            // 读取头部
            ReadHeader(bn);

            // 读取全部数据
            ReadData(bn, Total);
        }

        /// <summary>读取头部</summary>
        /// <param name="bn"></param>
        public void ReadHeader(Binary bn)
        {
            // 头部，幻数、版本和压缩标记
            var magic = bn.ReadBytes(14).ToStr();
            if (magic.Trim() != "NewLifeDbTable") throw new InvalidDataException();

            var ver = bn.Read<Byte>();
            var flag = bn.Read<Byte>();

            // 写入头部
            var count = bn.Read<Int32>();
            var cs = new String[count];
            var ts = new Type[count];
            for (var i = 0; i < count; i++)
            {
                cs[i] = bn.Read<String>();
                var tc = (TypeCode)bn.Read<Byte>();
                ts[i] = tc.ToString().GetTypeEx(false);
            }
            Columns = cs;
            Types = ts;

            Total = bn.ReadBytes(4).ToInt();
        }

        /// <summary>读取数据</summary>
        /// <param name="bn"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        public Int32 ReadData(Binary bn, Int32 rows)
        {
            if (rows <= 0) return 0;

            var ts = Types;
            var count = ts.Length;

            var total = 0;
            var rs = new List<Object[]>(rows);
            for (var k = 0; k < rows; k++)
            {
                if (bn.Stream.Position >= bn.Stream.Length) break;

                var row = new Object[count];
                for (var i = 0; i < count; i++)
                {
                    row[i] = bn.Read(ts[i]);
                }
                rs.Add(row);
                total++;
            }
            Rows = rs;

            return total;
        }

        /// <summary>读取</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public Boolean Read(Packet pk)
        {
            if (pk == null || pk.Total == 0) return false;

            Read(pk.GetStream());

            return true;
        }
        #endregion

        #region 二进制写入
        /// <summary>写入数据流</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            var bn = new Binary
            {
                EncodeInt = true,
                Stream = stream,
            };

            // 写入数据体
            var rs = Rows;
            Total = rs == null ? 0 : rs.Count;

            // 写入头部
            WriteHeader(bn);

            // 写入数据行
            WriteData(bn);
        }

        /// <summary>写入头部到数据流</summary>
        /// <param name="bn"></param>
        public void WriteHeader(Binary bn)
        {
            var cs = Columns;
            var ts = Types;

            // 头部，幻数、版本和压缩标记
            bn.Write("NewLifeDbTable".GetBytes(), 0, 14);
            bn.Write(_Ver);
            bn.Write(0);

            // 写入头部
            var count = cs.Length;
            bn.Write(count);
            for (var i = 0; i < count; i++)
            {
                bn.Write(cs[i]);
                bn.Write((Byte)ts[i].GetTypeCode());
            }

            // 数据行数
            bn.Write(Total.GetBytes(), 0, 4);
        }

        /// <summary>写入数据部分到数据流</summary>
        /// <param name="bn"></param>
        public void WriteData(Binary bn)
        {
            var ts = Types;
            var rs = Rows;

            // 写入数据
            foreach (var row in rs)
            {
                for (var i = 0; i < row.Length; i++)
                {
                    bn.Write(row[i], ts[i]);
                }
            }
        }

        /// <summary>转数据包</summary>
        /// <returns></returns>
        public Packet ToPacket()
        {
            var ms = new MemoryStream
            {
                Position = 8
            };

            Write(ms);

            ms.Position = 8;
            return new Packet(ms);
        }
        #endregion

        #region 获取
        /// <summary>读取指定行的字段值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public T Get<T>(Int32 row, String name)
        {
            if (!TryGet<T>(row, name, out var value)) return default(T);

            return value;
        }

        /// <summary>尝试读取指定行的字段值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryGet<T>(Int32 row, String name, out T value)
        {
            value = default(T);
            var rs = Rows;

            if (row < 0 || row >= rs.Count || name.IsNullOrEmpty()) return false;

            var col = GetColumn(name);
            if (col < 0) return false;

            value = rs[row][col].ChangeType<T>();

            return true;
        }

        /// <summary>根据名称找字段序号</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Int32 GetColumn(String name)
        {
            var cs = Columns;
            if (cs == null) return -1;

            for (var i = 0; i < cs.Length; i++)
            {
                if (cs[i].EqualIgnoreCase(name)) return i;
            }

            return -1;
        }
        #endregion

        #region 辅助
        /// <summary>数据集</summary>
        /// <returns></returns>
        public override String ToString() => "DbTable[{0}][{1}]".F(Columns == null ? 0 : Columns.Length, Rows == null ? 0 : Rows.Count);

        private static IDictionary<TypeCode, Object> _Defs;
        private static Object GetDefault(TypeCode tc)
        {
            if (_Defs == null)
            {
                var dic = new Dictionary<TypeCode, Object>();
                foreach (TypeCode item in Enum.GetValues(typeof(TypeCode)))
                {
                    Object val = null;
                    switch (item)
                    {
                        case TypeCode.Boolean:
                            val = false;
                            break;
                        case TypeCode.Char:
                            val = (Char)0;
                            break;
                        case TypeCode.SByte:
                            val = (SByte)0;
                            break;
                        case TypeCode.Byte:
                            val = (Byte)0;
                            break;
                        case TypeCode.Int16:
                            val = (Int16)0;
                            break;
                        case TypeCode.UInt16:
                            val = (UInt16)0;
                            break;
                        case TypeCode.Int32:
                            val = 0;
                            break;
                        case TypeCode.UInt32:
                            val = (UInt32)0;
                            break;
                        case TypeCode.Int64:
                            val = (Int64)0;
                            break;
                        case TypeCode.UInt64:
                            val = (UInt64)0;
                            break;
                        case TypeCode.Single:
                            val = (Single)0;
                            break;
                        case TypeCode.Double:
                            val = (Double)0;
                            break;
                        case TypeCode.Decimal:
                            val = (Decimal)0;
                            break;
                        case TypeCode.DateTime:
                            val = DateTime.MinValue;
                            break;
                        case TypeCode.Empty:
                        case TypeCode.Object:
                        case TypeCode.DBNull:
                        case TypeCode.String:
                        default:
                            val = null;
                            break;
                    }
                    dic[item] = val;
                }
                _Defs = dic;
            }

            return _Defs[tc];
        }

        Object ICloneable.Clone() => Clone();

        /// <summary>克隆</summary>
        /// <returns></returns>
        public DbTable Clone()
        {
            var dt = new DbTable
            {
                Columns = Columns,
                Types = Types,
                Rows = Rows,
                Total = Total
            };

            return dt;
        }
        #endregion

        #region 枚举
        /// <summary>获取枚举</summary>
        /// <returns></returns>
        public IEnumerator<DbRow> GetEnumerator() => new DbEnumerator { Table = this };

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        struct DbEnumerator : IEnumerator<DbRow>
        {
            public DbTable Table { get; set; }

            private Int32 _row;
            private DbRow _Current;
            public DbRow Current => _Current;

            Object IEnumerator.Current => _Current;

            public Boolean MoveNext()
            {
                var rs = Table?.Rows;
                if (rs == null || rs.Count == 0) return false;

                if (_row < 0 || _row >= rs.Count)
                {
                    _Current = default(DbRow);
                    return false;
                }

                _Current = new DbRow(Table, _row);

                _row++;

                return true;
            }

            public void Reset()
            {
                _Current = default(DbRow);
                _row = -1;
            }

            public void Dispose() { }
        }
        #endregion
    }
}