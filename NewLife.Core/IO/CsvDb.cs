using System.Reflection;
using System.Text;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.IO;

/// <summary>Csv文件轻量级数据库</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/csv_db
/// 适用于大量数据需要快速追加、快速查找，很少修改和删除的场景。
/// 在桌面客户端中，关系型数据库SQLite很容易因非法关机而损坏，本数据库能跳过损坏行，自动恢复。
/// 
/// 中间插入和删除时，需要移动尾部数据，性能较差。
/// 
/// 本设计不支持线程安全，务必确保单线程操作。
/// </remarks>
/// <typeparam name="T"></typeparam>
public class CsvDb<T> : DisposeBase where T : new()
{
    #region 静态缓存（反射开销优化）
    // 只反射一次，降低读写频率较高场景的成本
    private static readonly PropertyInfo[] _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    // 统一使用序列化名（可能来自特性），修复原先头部写入使用属性名导致与读取不一致的缺陷
    private static readonly String[] _propertyNames = _properties.Select(SerialHelper.GetName).ToArray();
    #endregion

    #region 属性
    /// <summary>文件名</summary>
    public String? FileName { get; set; }

    /// <summary>文件编码，默认utf8</summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>实体比较器</summary>
    public IEqualityComparer<T> Comparer { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化Csv文件数据库</summary>
    public CsvDb() => Comparer = EqualityComparer<T>.Default;

    /// <summary>实例化Csv文件数据库</summary>
    /// <param name="comparer">自定义比较器委托，用于判断两实体是否相等</param>
    public CsvDb(Func<T?, T?, Boolean> comparer) => Comparer = new MyComparer(comparer);

    /// <summary>销毁时自动提交未提交事务</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        // 若仍有缓存事务，自动提交（保持历史兼容行为）
        Commit();
    }
    #endregion

    #region 基础方法（事务）
    private List<T>? _cache;
    /// <summary>开启事务，便于批量处理数据。读取全部数据进入内存，后续 Add/Remove/Set 仅操作缓存。</summary>
    public void BeginTransaction() => _cache ??= FindAll().ToList();

    /// <summary>提交事务，把缓存数据写入磁盘（覆盖原文件）。提交后清空缓存。</summary>
    public void Commit()
    {
        if (_cache == null) return;

        Write(_cache, false);
        _cache = null;
    }

    /// <summary>回滚事务，放弃缓存的全部修改，不写回磁盘。</summary>
    public void Rollback() => _cache = null;
    #endregion

    #region 添删改查
    /// <summary>批量写入数据（高性能）</summary>
    /// <param name="models">要写入的数据</param>
    /// <param name="append">是否附加在尾部。为 false 时从头写入，覆盖已有数据</param>
    public void Write(IEnumerable<T> models, Boolean append)
    {
        if (append && (models is ICollection<T> collection && collection.Count == 0)) return;

        var file = GetFile();
        file.EnsureDirectory(true);

        using var fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        if (append) fs.Position = fs.Length;

        using var csv = new CsvFile(fs, true) { Encoding = Encoding };

        // 首次写入文件头。需要正确处理协变逆变问题，兼容.NET2.0
        if (fs.Position == 0) csv.WriteLine(_propertyNames);

        // 写入数据
        foreach (var item in models)
        {
            if (item is IModel src)
                csv.WriteLine(_properties.Select(e => src[e.Name]));
            else if (item != null)
                csv.WriteLine(_properties.Select(e => item.GetValue(e)));
        }

        // 截断原有多余内容（覆盖写场景）
        fs.SetLength(fs.Position);
        fs.Flush();
    }

    /// <summary>尾部插入数据，性能极好</summary>
    /// <param name="model"></param>
    public void Add(T model)
    {
        if (_cache != null)
            _cache.Add(model);
        else
            Write([model], true);
    }

    /// <summary>尾部插入数据，性能极好</summary>
    /// <param name="models"></param>
    public void Add(IEnumerable<T> models)
    {
        if (_cache != null)
            _cache.AddRange(models);
        else
            Write(models, true);
    }

    /// <summary>删除数据，性能很差，全部读取剔除后保存</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public Int32 Remove(T model) => Remove([model]);

    /// <summary>删除数据，性能很差，全部读取剔除后保存</summary>
    /// <param name="models"></param>
    /// <returns></returns>
    public Int32 Remove(IEnumerable<T> models)
    {
        if (models == null) return 0;
        if (Comparer == null) throw new ArgumentNullException(nameof(Comparer));

        var arr = models as ICollection<T> ?? models.ToList();
        if (arr.Count == 0) return 0;

        return Remove(x => arr.Any(y => Comparer.Equals(x, y)));
    }

    /// <summary>删除满足条件的数据，性能很差，全部读取剔除后保存</summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public Int32 Remove(Func<T, Boolean> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        lock (this)
        {
            if (_cache != null) return _cache.RemoveAll(x => predicate(x));

            var list = FindAll();
            if (list.Count == 0) return 0;

            var count = list.Count;
            list = list.Where(x => !predicate(x)).ToList();

            // 删除文件，重新写回去
            if (list.Count < count)
            {
                // 如果没有了数据，只写头部
                Write(list, false);
            }

            return count - list.Count;
        }
    }

    /// <summary>清空数据。只写头部</summary>
    public void Clear()
    {
        if (_cache != null)
            _cache.Clear();
        else
            Write([], false);
    }

    /// <summary>更新指定数据行，性能很差，全部读取替换后保存</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public Boolean Update(T model) => Set(model, false);

    /// <summary>设置（添加或更新）指定数据行，性能很差，全部读取替换后保存</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public Boolean Set(T model) => Set(model, true);

    private Boolean Set(T model, Boolean add)
    {
        if (Comparer == null) throw new ArgumentNullException(nameof(Comparer));

        lock (this)
        {
            var list = _cache ?? FindAll();
            if (!add && list.Count == 0) return false;

            // 找到目标数据行，并替换
            var flag = false;
            for (var i = 0; i < list.Count; i++)
            {
                if (Comparer.Equals(model, list[i]))
                {
                    list[i] = model;
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                if (!add) return false;

                list.Add(model);
            }

            // 重新写回去
            if (_cache == null)
            {
                Write(list, false);
            }

            return true;
        }
    }

    /// <summary>查找指定数据行</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public T? Find(T model)
    {
        if (Comparer == null) throw new ArgumentNullException(nameof(Comparer));

        return Query(e => Comparer.Equals(model, e), 1).FirstOrDefault();
    }

    /// <summary>获取满足条件的第一行数据</summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public T? Find(Func<T, Boolean>? predicate) => Query(predicate, 1).FirstOrDefault();

    /// <summary>获取所有数据行</summary>
    /// <returns></returns>
    public IList<T> FindAll() => _cache?.ToList() ?? Query(null).ToList();

    /// <summary>获取满足条件的数据行，性能好，顺序查找</summary>
    /// <param name="predicate"></param>
    /// <param name="count">最多返回行数。默认 -1 表示不限制</param>
    /// <returns></returns>
    public IEnumerable<T> Query(Func<T, Boolean>? predicate, Int32 count = -1)
    {
        // 开启事务时，直接返回缓存数据
        if (_cache != null)
        {
            foreach (var item in _cache)
            {
                if (predicate == null || predicate(item))
                {
                    yield return item;

                    if (--count == 0) break;
                }
            }
            yield break;
        }

        var file = GetFile();
        if (!File.Exists(file)) yield break;

        lock (this)
        {
            using var csv = new CsvFile(file, false) { Encoding = Encoding };

            var headers = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                var ss = csv.ReadLine();
                if (ss == null) break;

                // 头部，名称与序号对应
                if (headers.Count == 0)
                {
                    for (var i = 0; i < ss.Length; i++)
                    {
                        // 避免重复键异常；忽略重复列
                        if (!headers.ContainsKey(ss[i])) headers[ss[i]] = i;
                    }
                    // 无法识别所有字段
                    if (headers.Count == 0) break;
                }
                else
                {
                    var flag = false;
                    var model = new T();
                    try
                    {
                        // 反射构建对象
                        var success = 0;
                        foreach (var pi in _properties)
                        {
                            var name = SerialHelper.GetName(pi);
                            if (pi.CanWrite && headers.TryGetValue(name, out var idx) && idx < ss.Length)
                            {
                                var raw = ss[idx];
                                if (raw == null) continue;

                                // 部分基础类型判断数据有效性
                                if (pi.PropertyType.IsInt() && !Int64.TryParse(raw, out _)) continue;
                                var code = pi.PropertyType.GetTypeCode();
                                switch (code)
                                {
                                    case TypeCode.Single:
                                    case TypeCode.Double:
                                        if (!Double.TryParse(raw, out _)) continue;
                                        break;
                                    case TypeCode.Decimal:
                                        if (!Decimal.TryParse(raw, out _)) continue;
                                        break;
                                    case TypeCode.DateTime:
                                        if (!DateTime.TryParse(raw, out _)) continue;
                                        break;
                                    default:
                                        break;
                                }

                                var value = raw.ChangeType(pi.PropertyType);

                                if (model is IModel dst)
                                    dst[pi.Name] = value;
                                else
                                    model.SetValue(pi, value);

                                success++;
                            }
                        }

                        // 没有任何字段成功匹配，视为损坏行
                        if (success == 0) continue;

                        if (predicate == null || predicate(model))
                        {
                            flag = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        // 读取某一行出错，放弃该行
                        XTrace.WriteException(ex);
                        continue;
                    }

                    if (!flag) continue;

                    yield return model;

                    if (--count == 0) break;
                }
            }
        }
    }

    /// <summary>获取数据行数，性能较好，统计文件行数（除头部）</summary>
    /// <returns></returns>
    public Int32 FindCount()
    {
        if (_cache != null) return _cache.Count;

        lock (this)
        {
            var file = GetFile();
            if (!File.Exists(file)) return 0;

            // 逐行读取统计，避免一次性加载全部内容到内存
            var line = 0;
            using var sr = new StreamReader(file, Encoding);

            if (sr.ReadLine() == null) return 0; // 跳过头部，不存在则返回 0

            while (sr.ReadLine() != null) line++;

            return line;
        }
    }
    #endregion

    #region 辅助
    private String GetFile()
    {
        if (FileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(FileName));

        return FileName.GetFullPath();
    }

    private class MyComparer(Func<T?, T?, Boolean> comparer) : IEqualityComparer<T>
    {
        public Func<T?, T?, Boolean> Comparer = comparer;

        public Boolean Equals(T? x, T? y) => Comparer(x, y);

        public Int32 GetHashCode(T? obj) => obj?.GetHashCode() ?? 0;
    }
    #endregion
}