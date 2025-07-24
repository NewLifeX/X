using System.Collections;
using System.Data;
using System.Data.Common;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using NewLife.IO;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Data;

/// <summary>数据表</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/dbtable
/// </remarks>
public class DbTable : IEnumerable<DbRow>, ICloneable, IAccessor
{
    #region 属性
    /// <summary>数据列</summary>
    public String[] Columns { get; set; } = [];

    /// <summary>数据列类型</summary>
    [XmlIgnore, IgnoreDataMember]
    public Type[] Types { get; set; } = [];

    /// <summary>数据行</summary>
    public IList<Object?[]> Rows { get; set; } = [];

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
    public void ReadData(IDataReader dr, Int32[]? fields = null)
    {
        // 字段
        var cs = Columns ?? throw new ArgumentNullException(nameof(Columns));
        var ts = Types ?? throw new ArgumentNullException(nameof(Types));

        fields ??= Enumerable.Range(0, cs.Length).ToArray();

        // 数据
        var rs = new List<Object?[]>();
        while (dr.Read())
        {
            var row = new Object?[fields.Length];
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

    /// <summary>读取数据</summary>
    /// <param name="dr"></param>
    /// <param name="cancellationToken">取消通知</param>
    public async Task ReadAsync(DbDataReader dr, CancellationToken cancellationToken = default)
    {
        ReadHeader(dr);
        await ReadDataAsync(dr, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>读取数据</summary>
    /// <param name="dr">数据读取器</param>
    /// <param name="fields">要读取的字段序列</param>
    /// <param name="cancellationToken">取消通知</param>
    public async Task ReadDataAsync(DbDataReader dr, Int32[]? fields = null, CancellationToken cancellationToken = default)
    {
        // 字段
        var cs = Columns ?? throw new ArgumentNullException(nameof(Columns));
        var ts = Types ?? throw new ArgumentNullException(nameof(Types));

        fields ??= Enumerable.Range(0, cs.Length).ToArray();

        // 数据
        var rs = new List<Object?[]>();
        while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Object?[fields.Length];
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

    #region DataTable互转
    /// <summary>从DataTable读取数据</summary>
    /// <param name="dataTable">数据表</param>
    public Int32 Read(DataTable dataTable)
    {
        if (dataTable == null) throw new ArgumentNullException(nameof(dataTable));

        var cs = new List<String>();
        var ts = new List<Type>();
        foreach (var item in dataTable.Columns)
        {
            if (item is DataColumn dc)
            {
                cs.Add(dc.ColumnName);
                ts.Add(dc.DataType);
            }
        }
        Columns = cs.ToArray();
        Types = ts.ToArray();

        var rs = new List<Object?[]>();
        foreach (var item in dataTable.Rows)
        {
            if (item is DataRow dr)
                rs.Add(dr.ItemArray);
        }
        Rows = rs;

        return rs.Count;
    }

    /// <summary>转换为DataTable</summary>
    /// <returns></returns>
    public DataTable ToDataTable() => Write(new DataTable());

    /// <summary>转换为DataTable</summary>
    /// <param name="dataTable">数据表</param>
    /// <returns></returns>
    public DataTable Write(DataTable dataTable)
    {
        if (dataTable == null) throw new ArgumentNullException(nameof(dataTable));

        var cs = Columns ?? throw new ArgumentNullException(nameof(Columns));
        var ts = Types ?? throw new ArgumentNullException(nameof(Types));

        for (var i = 0; i < cs.Length; i++)
        {
            var dc = new DataColumn(cs[i], ts[i]);

            dataTable.Columns.Add(dc);
        }

        var rs = Rows;
        if (rs != null)
        {
            for (var i = 0; i < rs.Count; i++)
            {
                var dr = dataTable.NewRow();
                dr.ItemArray = rs[i];

                dataTable.Rows.Add(dr);
            }
        }

        return dataTable;
    }
    #endregion

    #region 二进制读取
    private const Byte _Ver = 3;
    private const String MAGIC = "NewLifeDbTable";

    /// <summary>创建二进制序列化器</summary>
    /// <param name="stream">数据流</param>
    /// <returns></returns>
    public Binary CreateBinary(Stream stream) => new(stream) { FullTime = true, EncodeInt = true };

    /// <summary>从数据流读取</summary>
    /// <param name="stream"></param>
    public Int64 Read(Stream stream)
    {
        var bn = CreateBinary(stream);

        // 读取头部
        var rs = ReadHeader(bn);

        // 读取全部数据
        rs += ReadData(bn, Total);

        return rs;
    }

    /// <summary>读取头部。获取列名、类型和行数等信息</summary>
    /// <param name="binary">二进制序列化器</param>
    public Int64 ReadHeader(Binary binary)
    {
        var pStart = binary.Total;

        // 头部，幻数、版本和标记
        var magic = binary.ReadBytes(MAGIC.Length).ToStr();
        if (magic != MAGIC) throw new InvalidDataException();

        var ver = binary.Read<Byte>();
        _ = binary.Read<Byte>();

        // 版本兼容
        if (ver > _Ver) throw new InvalidDataException($"DbTable[ver={_Ver}] Unable to support newer versions [{ver}]");

        // v3开始支持FullTime
        if (ver < 3) binary.FullTime = false;

        // 读取头部
        var count = binary.Read<Int32>();
        var cs = new String[count];
        var ts = new Type[count];
        for (var i = 0; i < count; i++)
        {
            cs[i] = binary.Read<String>() + "";

            // 复杂类型写入类型字符串
            var tc = (TypeCode)binary.Read<Byte>();
            if (tc != TypeCode.Object)
                ts[i] = Type.GetType("System." + tc) ?? typeof(Object);
            else if (ver >= 2)
                ts[i] = Type.GetType(binary.Read<String>() + "") ?? typeof(Object);
        }
        Columns = cs;
        Types = ts;

        Total = binary.ReadBytes(4).ToInt();

        return binary.Total - pStart;
    }

    /// <summary>读取数据</summary>
    /// <param name="binary">二进制序列化器</param>
    /// <param name="rows">行数</param>
    /// <returns></returns>
    public Int64 ReadData(Binary binary, Int32 rows)
    {
        if (rows <= 0) return 0;

        var pStart = binary.Total;

        Rows = ReadRows(binary, rows).ToList();

        return binary.Total - pStart;
    }

    /// <summary>使用迭代器模式读取行数据。调用者可以一边读取一边处理数据</summary>
    /// <param name="binary">二进制序列化器</param>
    /// <param name="rows">行数。传入-1时，循环遍历数据流</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IEnumerable<Object?[]> ReadRows(Binary binary, Int32 rows)
    {
        if (rows == 0) yield break;

        var ts = Types ?? throw new ArgumentNullException(nameof(Types));
        if (rows > 0)
        {
            for (var k = 0; k < rows; k++)
            {
                var row = new Object?[ts.Length];
                for (var i = 0; i < ts.Length; i++)
                {
                    row[i] = binary.Read(ts[i]);
                }
                yield return row;
            }
        }
        else
        {
            while (!binary.EndOfStream)
            {
                var row = new Object?[ts.Length];
                for (var i = 0; i < ts.Length; i++)
                {
                    row[i] = binary.Read(ts[i]);
                }
                yield return row;
            }
        }
    }

    /// <summary>读取</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public Boolean Read(IPacket pk)
    {
        if (pk == null || pk.Length == 0) return false;

        Read(pk.GetStream());

        return true;
    }

    /// <summary>读取</summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public Boolean Read(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
    {
        if (count < 0) count = buffer.Length - offset;

        var ms = new MemoryStream(buffer, offset, count);
        Read(ms);

        return true;
    }

    /// <summary>从文件加载</summary>
    /// <param name="file">文件路径</param>
    /// <param name="compressed">是否压缩</param>
    /// <returns></returns>
    public Int64 LoadFile(String file, Boolean compressed = false) => file.AsFile().OpenRead(compressed, s => Read(s));

    /// <summary>使用迭代器模式加载文件数据。调用者可以一边读取一边处理数据</summary>
    /// <param name="file">文件路径。gz文件自动使用压缩</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IEnumerable<Object?[]> LoadRows(String file)
    {
        if (file.IsNullOrEmpty()) throw new ArgumentNullException(nameof(file));

        using var fs = file.AsFile().OpenRead();
        var bn = CreateBinary(fs);

        // 解压缩
        if (file.EndsWithIgnoreCase(".gz"))
            bn.Stream = new GZipStream(fs, CompressionMode.Decompress);

        ReadHeader(bn);

        // 有些场景生成db文件时，无法在开始写入长度。
        var rows = Total;
        if (rows == 0 && fs.Length > 0) rows = -1;

        return ReadRows(bn, rows);
    }

    Boolean IAccessor.Read(Stream stream, Object? context)
    {
        Read(stream);
        return true;
    }
    #endregion

    #region 二进制写入
    /// <summary>写入数据流</summary>
    /// <param name="stream"></param>
    public Int64 Write(Stream stream)
    {
        var bn = CreateBinary(stream);

        // 写入数据体
        var rs = Rows;
        if (Total == 0 && rs != null) Total = rs.Count;

        // 写入头部
        var bs = WriteHeader(bn);

        // 写入数据行
        bs += WriteData(bn);

        return bs;
    }

    /// <summary>写入头部到数据流</summary>
    /// <param name="binary">二进制序列化器</param>
    public Int64 WriteHeader(Binary binary)
    {
        var cs = Columns ?? throw new ArgumentNullException(nameof(Columns));
        var ts = Types ?? throw new ArgumentNullException(nameof(Types));

        var pStart = binary.Total;

        // 头部，幻数、版本和标记
        var buf = MAGIC.GetBytes();
        binary.Write(buf, 0, buf.Length);
        binary.Write(_Ver);
        binary.Write(0);

        // 写入头部
        var count = cs.Length;
        binary.Write(count);
        for (var i = 0; i < count; i++)
        {
            binary.Write(cs[i]);

            // 复杂类型写入类型字符串
            var code = ts[i].GetTypeCode();
            binary.Write((Byte)code);
            if (code == TypeCode.Object) binary.Write(ts[i].FullName);
        }

        // 数据行数
        binary.Write(Total.GetBytes(), 0, 4);

        return binary.Total - pStart;
    }

    /// <summary>写入数据部分到数据流</summary>
    /// <param name="binary">二进制序列化器</param>
    public Int64 WriteData(Binary binary)
    {
        var ts = Types ?? throw new ArgumentNullException(nameof(Types));
        var rs = Rows;
        if (rs == null) return 0;

        var pStart = binary.Total;

        // 写入数据
        foreach (var row in rs)
        {
            for (var i = 0; i < row.Length; i++)
            {
                binary.Write(row[i], ts[i]);
            }
        }

        return binary.Total - pStart;
    }

    /// <summary>写入数据部分到数据流</summary>
    /// <param name="binary">二进制序列化器</param>
    /// <param name="fields">要写入的字段序列</param>
    public Int64 WriteData(Binary binary, Int32[] fields)
    {
        var ts = Types ?? throw new ArgumentNullException(nameof(Types));
        var rs = Rows;
        if (rs == null) return 0;

        var pStart = binary.Total;

        // 写入数据，按照指定的顺序
        foreach (var row in rs)
        {
            for (var i = 0; i < fields.Length; i++)
            {
                // 找到目标顺序，实际上几乎不可能出现-1
                var idx = fields[i];
                if (idx >= 0)
                    binary.Write(row[idx], ts[idx]);
                else
                    binary.Write(null, ts[idx]);
            }
        }

        return binary.Total - pStart;
    }

    /// <summary>使用迭代器模式写入数据。调用方可以一边处理数据一边写入</summary>
    /// <param name="binary">二进制序列化器</param>
    /// <param name="rows">数据源</param>
    /// <param name="fields">要写入的字段序列</param>
    /// <exception cref="ArgumentNullException"></exception>
    public Int32 WriteRows(Binary binary, IEnumerable<Object?[]> rows, Int32[]? fields = null)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        var ts = Types ?? throw new ArgumentNullException(nameof(Types));

        // 写入数据
        var count = 0;
        foreach (var row in rows)
        {
            WriteRow(binary, row, fields);

            count++;
        }

        return count;
    }

    /// <summary>写入一行数据</summary>
    /// <param name="binary"></param>
    /// <param name="row"></param>
    /// <param name="fields"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public Int64 WriteRow(Binary binary, Object?[] row, Int32[]? fields = null)
    {
        if (row == null) throw new ArgumentNullException(nameof(row));
        var ts = Types ?? throw new ArgumentNullException(nameof(Types));

        var pStart = binary.Total;

        // 写入数据
        if (fields == null)
        {
            for (var i = 0; i < row.Length; i++)
            {
                binary.Write(row[i], ts[i]);
            }
        }
        else
        {
            for (var i = 0; i < fields.Length; i++)
            {
                // 找到目标顺序，实际上几乎不可能出现-1
                var idx = fields[i];
                if (idx >= 0)
                    binary.Write(row[idx], ts[idx]);
                else
                    binary.Write(null, ts[idx]);
            }
        }

        return binary.Total - pStart;
    }

    /// <summary>转数据包</summary>
    /// <returns></returns>
    public IPacket ToPacket()
    {
        // 不确定所需大小，只能使用内存流，再包装为数据包。
        // 头部预留8个字节，方便网络传输时添加协议头。
        var ms = new MemoryStream
        {
            Position = 8
        };

        Write(ms);

        ms.Position = 8;

        // 包装为数据包，直接窃取内存流内部的缓冲区
        return new ArrayPacket(ms);
    }

    /// <summary>保存到文件</summary>
    /// <param name="file"></param>
    /// <param name="compressed">是否压缩</param>
    /// <returns></returns>
    public Int64 SaveFile(String file, Boolean compressed = false) => file.AsFile().OpenWrite(compressed, s => Write(s));

    /// <summary>使用迭代器模式写入多行数据到文件。调用者可以一边处理数据一边写入</summary>
    /// <param name="file">文件路径。gz文件自动使用压缩</param>
    /// <param name="rows">数据源</param>
    /// <param name="fields">要写入的字段序列</param>
    /// <exception cref="ArgumentNullException"></exception>
    public Int32 SaveRows(String file, IEnumerable<Object?[]> rows, Int32[]? fields = null)
    {
        if (file.IsNullOrEmpty()) throw new ArgumentNullException(nameof(file));

        file = file.GetFullPath().EnsureDirectory(true);
        using var fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        var bn = CreateBinary(fs);

        // 压缩
        if (file.EndsWithIgnoreCase(".gz"))
            bn.Stream = new GZipStream(fs, CompressionMode.Compress);

        WriteHeader(bn);
        var count = WriteRows(bn, rows, fields);

        // 如果文件已存在，此处截掉多余部分
        fs.SetLength(fs.Position);

        return count;
    }

    Boolean IAccessor.Write(Stream stream, Object? context)
    {
        Write(stream);
        return true;
    }
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
    public IList<IDictionary<String, Object?>> ToDictionary()
    {
        var list = new List<IDictionary<String, Object?>>();
        var cs = Columns ?? throw new ArgumentNullException(nameof(Columns));
        var rows = Rows;

        if (rows != null)
        {
            foreach (var row in rows)
            {
                var dic = new Dictionary<String, Object?>();
                for (var i = 0; i < cs.Length; i++)
                {
                    dic[cs[i]] = row[i];
                }
                list.Add(dic);
            }
        }

        return list;
    }
    #endregion

    #region Xml序列化
    /// <summary>转Xml字符串</summary>
    /// <returns></returns>
    public String GetXml()
    {
        //var doc = new XmlDocument();
        //var root = doc.CreateElement("DbTable");
        //doc.AppendChild(root);

        //foreach (var row in Rows)
        //{
        //    var dr = doc.CreateElement("Table");
        //    for (var i = 0; i < Columns.Length; i++)
        //    {
        //        var elm = doc.CreateElement(Columns[i]);
        //        elm.InnerText = row[i] + "";
        //        dr.AppendChild(elm);
        //    }
        //    root.AppendChild(dr);
        //}

        //return doc.OuterXml;

        var ms = new MemoryStream();
        WriteXml(ms).Wait(15_000);

        return ms.ToArray().ToStr();
    }

    /// <summary>以Xml格式写入数据流中</summary>
    /// <param name="stream"></param>
    public async Task WriteXml(Stream stream)
    {
        var set = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            ConformanceLevel = ConformanceLevel.Auto,
            Indent = true,
            Async = true,
        };
        using var writer = XmlWriter.Create(stream, set);

        await writer.WriteStartDocumentAsync().ConfigureAwait(false);
        await writer.WriteStartElementAsync(null, "DbTable", null).ConfigureAwait(false);

        var cs = Columns ?? throw new ArgumentNullException(nameof(Columns));
        var ts = Types ?? throw new ArgumentNullException(nameof(Types));
        var rows = Rows;

        if (rows != null)
        {
            foreach (var row in rows)
            {
                await writer.WriteStartElementAsync(null, "Table", null).ConfigureAwait(false);
                for (var i = 0; i < cs.Length; i++)
                {
                    await writer.WriteStartElementAsync(null, cs[i], null).ConfigureAwait(false);

                    if (ts[i] == typeof(Boolean))
                        writer.WriteValue(row[i].ToBoolean());
                    else if (ts[i] == typeof(DateTime))
                        writer.WriteValue(new DateTimeOffset(row[i].ChangeType<DateTime>()));
                    else if (ts[i] == typeof(DateTimeOffset))
                        writer.WriteValue(row[i].ChangeType<DateTimeOffset>());
                    else if (row[i] is IFormattable ft)
                        await writer.WriteStringAsync(ft + "").ConfigureAwait(false);
                    else
                        await writer.WriteStringAsync(row[i] + "").ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                }
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer.WriteEndDocumentAsync().ConfigureAwait(false);
    }
    #endregion

    #region Csv序列化
    /// <summary>保存到Csv文件</summary>
    /// <param name="file"></param>
    public void SaveCsv(String file)
    {
        var cs = Columns ?? throw new ArgumentNullException(nameof(Columns));
        var rows = Rows;

        using var csv = new CsvFile(file, true);
        csv.WriteLine(cs);
        if (rows != null) csv.WriteAll(rows);
    }

    /// <summary>从Csv文件加载</summary>
    /// <param name="file"></param>
    public void LoadCsv(String file)
    {
        using var csv = new CsvFile(file, false);
        var cs = csv.ReadLine();
        if (cs != null) Columns = cs;
        Rows = csv.ReadAll().Cast<Object?[]>().ToList();
    }
    #endregion

    #region 读写模型
    /// <summary>写入模型列表</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="models"></param>
    public void WriteModels<T>(IEnumerable<T> models)
    {
        // 可用属性
        var pis = typeof(T).GetProperties(true);
        pis = pis.Where(e => e.PropertyType.IsBaseType()).ToArray();

        // 头部
        if (Columns == null || Columns.Length == 0)
        {
            Columns = pis.Select(e => SerialHelper.GetName(e)).ToArray();
            Types = pis.Select(e => e.PropertyType).ToArray();
        }

        Rows = Cast<T>(models).ToList();
    }

    /// <summary>模型列表转为对象数组行。支持WriteRows/SaveRows实现一边处理一边写入</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="models"></param>
    /// <returns></returns>
    public IEnumerable<Object?[]> Cast<T>(IEnumerable<T> models)
    {
        // 可用属性
        var pis = typeof(T).GetProperties(true);
        pis = pis.Where(e => e.PropertyType.IsBaseType()).ToArray();

        foreach (var item in models)
        {
            var row = new Object?[Columns.Length];
            for (var i = 0; i < row.Length; i++)
            {
                // 反射取值
                if (pis[i].CanRead)
                {
                    if (item is IModel ext)
                        row[i] = ext[pis[i].Name];
                    else if (item != null)
                        row[i] = item.GetValue(pis[i]);
                }
            }
            yield return row;
        }
    }

    /// <summary>数据表转模型列表。普通反射，便于DAL查询后转任意模型列表</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerable<T> ReadModels<T>()
    {
        foreach (var model in ReadModels(typeof(T)))
        {
            yield return (T)model;
        }
    }

    /// <summary>数据表转模型列表。普通反射，便于DAL查询后转任意模型列表</summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public IEnumerable<Object> ReadModels(Type type)
    {
        var cs = Columns ?? throw new ArgumentNullException(nameof(Columns));
        var rows = Rows;
        if (rows == null) yield break;

        // 可用属性
        var pis = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dic = pis.ToDictionary(e => SerialHelper.GetName(e), e => e, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var model = type.CreateInstance();
            if (model == null) continue;

            for (var i = 0; i < row.Length; i++)
            {
                // 扩展赋值，或 反射赋值
                if (dic.TryGetValue(cs[i], out var pi) && pi.CanWrite)
                {
                    var val = row[i].ChangeType(pi.PropertyType);
                    if (model is IModel ext)
                        ext[pi.Name] = val;
                    else
                        model.SetValue(pi, val);
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
    public T? Get<T>(Int32 row, String name) => !TryGet<T>(row, name, out var value) ? default : value;

    /// <summary>尝试读取指定行的字段值</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="row"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Boolean TryGet<T>(Int32 row, String name, out T? value)
    {
        value = default;
        var rs = Rows;
        if (rs == null) return false;

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

    private static IDictionary<TypeCode, Object?>? _Defs;
    private static Object? GetDefault(TypeCode tc)
    {
        if (_Defs == null)
        {
            var dic = new Dictionary<TypeCode, Object?>();
            foreach (var item in Enum.GetValues(typeof(TypeCode)))
            {
                if (item is not TypeCode tc2) continue;

                Object? val = null;
                val = tc2 switch
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
                dic[tc2] = val;
            }
            _Defs = dic;
        }

        return _Defs.TryGetValue(tc, out var obj) ? obj : null;
    }

    Object ICloneable.Clone() => Clone();

    /// <summary>克隆</summary>
    /// <returns></returns>
    public DbTable Clone()
    {
        var dt = new DbTable
        {
            Columns = Columns.ToArray(),
            Types = Types.ToArray(),
            Rows = Rows.ToList(),
            Total = Total
        };

        return dt;
    }

    /// <summary>获取数据行</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public DbRow GetRow(Int32 index) => new(this, index);
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
        public readonly DbRow Current => _Current;

        readonly Object IEnumerator.Current => _Current;

        public Boolean MoveNext()
        {
            var rs = Table.Rows;
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

        public readonly void Dispose() { }
    }
    #endregion
}