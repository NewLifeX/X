using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Data
{
    /// <summary>数据表</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/dbtable
    /// </remarks>
    public class DbTable : IEnumerable<DbRow>, ICloneable, IAccessor
    {
        #region 属性
        /// <summary>数据列</summary>
        public String[] Columns { get; set; }

        /// <summary>数据列类型</summary>
        [XmlIgnore, IgnoreDataMember]
        public Type[] Types { get; set; }

        /// <summary>数据行</summary>
        public IList<Object[]> Rows { get; set; }

        /// <summary>总行数</summary>
        public Int32 Total { get; set; }
        #endregion

        #region 构造
        #endregion

        #region 从数据库读取
        /// <summary>读取数据</summary>
        /// <param name="dr"></param>
        public void Read(IDataReader dr)
        {
            ReadHeader(dr);
            ReadData(dr);
        }

        /// <summary>读取头部</summary>
        /// <param name="dr"></param>
        public void ReadHeader(IDataReader dr)
        {
            var count = dr.FieldCount;

            // 字段
            var cs = new String[count];
            var ts = new Type[count];
            for (var i = 0; i < count; i++)
            {
                cs[i] = dr.GetName(i);
                ts[i] = dr.GetFieldType(i);
            }
            Columns = cs;
            Types = ts;
        }

        /// <summary>读取数据</summary>
        /// <param name="dr">数据读取器</param>
        /// <param name="fields">要读取的字段序列</param>
        public void ReadData(IDataReader dr, Int32[] fields = null)
        {
            // 字段
            var cs = Columns;
            var ts = Types;

            if (fields == null) fields = Enumerable.Range(0, cs.Length).ToArray();

            // 数据
            var rs = new List<Object[]>();
            while (dr.Read())
            {
                var row = new Object[fields.Length];
                for (var i = 0; i < fields.Length; i++)
                {
                    // MySql在读取0000时间数据时会报错
                    try
                    {
                        var val = dr[fields[i]];

                        if (val == DBNull.Value) val = GetDefault(ts[i].GetTypeCode());
                        row[i] = val;
                    }
                    catch { }
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
            // 头部，幻数、版本和标记
            var magic = bn.ReadBytes(14).ToStr();
            if (magic != "NewLifeDbTable") throw new InvalidDataException();

            var ver = bn.Read<Byte>();
            _ = bn.Read<Byte>();

            // 版本兼容
            if (ver > _Ver) throw new InvalidDataException($"DbTable[ver={_Ver}]无法支持较新的版本[{ver}]");

            // 读取头部
            var count = bn.Read<Int32>();
            var cs = new String[count];
            var ts = new Type[count];
            for (var i = 0; i < count; i++)
            {
                cs[i] = bn.Read<String>();
                var tc = (TypeCode)bn.Read<Byte>();
                ts[i] = Type.GetType("System." + tc);
            }
            Columns = cs;
            Types = ts;

            Total = bn.ReadBytes(4).ToInt();
        }

        /// <summary>读取数据</summary>
        /// <param name="bn"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        public void ReadData(Binary bn, Int32 rows)
        {
            if (rows <= 0) return;

            var ts = Types;
            var rs = new List<Object[]>(rows);
            for (var k = 0; k < rows; k++)
            {
                var row = new Object[ts.Length];
                for (var i = 0; i < ts.Length; i++)
                {
                    row[i] = bn.Read(ts[i]);
                }
                rs.Add(row);
            }
            Rows = rs;
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

        /// <summary>从文件加载</summary>
        /// <param name="file"></param>
        /// <param name="compressed">是否压缩</param>
        /// <returns></returns>
        public Int64 LoadFile(String file, Boolean compressed = false) => file.AsFile().OpenRead(compressed, s => Read(s));

        Boolean IAccessor.Read(Stream stream, Object context) { Read(stream); return true; }
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
            if (Total == 0 && rs != null) Total = rs.Count;

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

            // 头部，幻数、版本和标记
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

        /// <summary>保存到文件</summary>
        /// <param name="file"></param>
        /// <param name="compressed">是否压缩</param>
        /// <returns></returns>
        public void SaveFile(String file, Boolean compressed = false) => file.AsFile().OpenWrite(compressed, s => Write(s));

        Boolean IAccessor.Write(Stream stream, Object context) { Write(stream); return true; }
        #endregion

        #region Json序列化
        /// <summary>转Json字符串</summary>
        /// <param name="indented">是否缩进。默认false</param>
        /// <param name="nullValue">是否写空值。默认true</param>
        /// <param name="camelCase">是否驼峰命名。默认false</param>
        /// <returns></returns>
        public String ToJson(Boolean indented = false, Boolean nullValue = true, Boolean camelCase = false)
        {
            // 先转为名值对象的数组，再进行序列化
            var list = ToDictionary();
            return list.ToJson(indented, nullValue, camelCase);
        }

        /// <summary>转为字典数组形式</summary>
        /// <returns></returns>
        public IList<IDictionary<String, Object>> ToDictionary()
        {
            var list = new List<IDictionary<String, Object>>();
            foreach (var row in Rows)
            {
                var dic = new Dictionary<String, Object>();
                for (var i = 0; i < Columns.Length; i++)
                {
                    dic[Columns[i]] = row[i];
                }
                list.Add(dic);
            }

            return list;
        }
        #endregion

        #region 反射
        /// <summary>写入模型列表</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="models"></param>
        public void WriteModels<T>(IEnumerable<T> models)
        {
            // 可用属性
            var pis = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            pis = pis.Where(e => e.PropertyType.GetTypeCode() != TypeCode.Object).ToArray();

            Rows = new List<Object[]>();
            foreach (var item in models)
            {
                // 头部
                if (Columns == null)
                {
                    Columns = pis.Select(e => SerialHelper.GetName(e)).ToArray();
                    Types = pis.Select(e => e.PropertyType).ToArray();
                }

                var row = new Object[Columns.Length];
                for (var i = 0; i < row.Length; i++)
                {
                    // 反射取值
                    row[i] = pis[i].GetValue(item, null);
                }
                Rows.Add(row);
            }
        }

        /// <summary>数据表转模型列表。普通反射，便于DAL查询后转任意模型列表</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> ReadModels<T>()
        {
            // 可用属性
            var pis = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var dic = pis.ToDictionary(e => SerialHelper.GetName(e), e => e, StringComparer.OrdinalIgnoreCase);

            foreach (var row in Rows)
            {
                var model = (T)typeof(T).CreateInstance();
                for (var i = 0; i < row.Length; i++)
                {
                    // 扩展赋值，或 反射赋值
                    if (dic.TryGetValue(Columns[i], out var pi))
                    {
                        var val = row[i].ChangeType(pi.PropertyType);
                        if (model is IExtend ext)
                            ext[Columns[i]] = val;
                        else
                            pi.SetValue(model, val, null);
                    }
                }

                yield return model;
            }
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
            if (!TryGet<T>(row, name, out var value)) return default;

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
            value = default;
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
        public override String ToString() => $"DbTable[{Columns?.Length}][{Rows?.Count}]";

        private static IDictionary<TypeCode, Object> _Defs;
        private static Object GetDefault(TypeCode tc)
        {
            if (_Defs == null)
            {
                var dic = new Dictionary<TypeCode, Object>();
                foreach (TypeCode item in Enum.GetValues(typeof(TypeCode)))
                {
                    Object val = null;
                    val = item switch
                    {
                        TypeCode.Boolean => false,
                        TypeCode.Char => (Char)0,
                        TypeCode.SByte => (SByte)0,
                        TypeCode.Byte => (Byte)0,
                        TypeCode.Int16 => (Int16)0,
                        TypeCode.UInt16 => (UInt16)0,
                        TypeCode.Int32 => 0,
                        TypeCode.UInt32 => (UInt32)0,
                        TypeCode.Int64 => (Int64)0,
                        TypeCode.UInt64 => (UInt64)0,
                        TypeCode.Single => (Single)0,
                        TypeCode.Double => (Double)0,
                        TypeCode.Decimal => (Decimal)0,
                        TypeCode.DateTime => DateTime.MinValue,
                        _ => null,
                    };
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

        private struct DbEnumerator : IEnumerator<DbRow>
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
                    _Current = default;
                    return false;
                }

                _Current = new DbRow(Table, _row);

                _row++;

                return true;
            }

            public void Reset()
            {
                _Current = default;
                _row = -1;
            }

            public void Dispose() { }
        }
        #endregion
    }
}