using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.IO
{
    /// <summary>Csv文件轻量级数据库</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/csv_db
    /// 适用于大量数据需要快速存储、快速查找，很少修改和删除的场景。
    /// 在桌面客户端中，关系型数据库SQLite很容易因非法关机而损坏，本数据库能跳过损坏行，自动恢复。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class CsvDb<T> where T : new()
    {
        #region 属性
        /// <summary>文件名</summary>
        public String FileName { get; set; }

        /// <summary>文件编码，默认utf8</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>实体比较器</summary>
        public IEqualityComparer<T> Comparer { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化Csv文件数据库</summary>
        public CsvDb() => Comparer = EqualityComparer<T>.Default;

        /// <summary>实例化Csv文件数据库</summary>
        /// <param name="comparer"></param>
        public CsvDb(Func<T, T, Boolean> comparer) => Comparer = new MyComparer { Comparer = comparer };
        #endregion

        #region 方法
        /// <summary>尾部插入数据，性能极好</summary>
        /// <param name="model"></param>
        public void Add(T model) => Add(new[] { model });

        /// <summary>尾部插入数据，性能极好</summary>
        /// <param name="models"></param>
        public void Add(IEnumerable<T> models) => Write(models, true);

        private void Write(IEnumerable<T> models, Boolean append)
        {
            var file = GetFile();
            file.EnsureDirectory(true);

            using var fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (append) fs.Position = fs.Length;

            using var csv = new CsvFile(fs, true) { Encoding = Encoding };

            var pis = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // 首次写入文件头
            if (fs.Position == 0) csv.WriteLine(pis.Select(e => e.Name));

            // 写入数据
            foreach (var item in models)
            {
                csv.WriteLine(pis.Select(e => e.GetValue(item, null)));
            }

            csv.TryDispose();
            fs.SetLength(fs.Position);
        }

        /// <summary>删除数据，性能很差，全部读取剔除后保存</summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public Int32 Remove(T model) => Remove(new[] { model });

        /// <summary>删除数据，性能很差，全部读取剔除后保存</summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public Int32 Remove(IEnumerable<T> models)
        {
            if (models == null || !models.Any()) return 0;
            if (Comparer == null) throw new ArgumentNullException(nameof(Comparer));

            return Remove(x => models.Any(y => Comparer.Equals(x, y)));
        }

        /// <summary>删除满足条件的数据，性能很差，全部读取剔除后保存</summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public Int32 Remove(Func<T, Boolean> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            lock (this)
            {
                var list = FindAll();
                if (list.Count == 0) return 0;

                var count = list.Count;
                list = list.Where(x => !predicate(x)).ToList();

                // 删除文件，重新写回去
                if (list.Count < count)
                {
                    // 如果没有了数据，直接删除文件
                    if (list.Count == 0)
                    {
                        var file = GetFile();
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            XTrace.WriteException(ex);
                            File.Move(file, file + ".del");
                            return -1;
                        }
                    }
                    else
                    {
                        Write(list, false);
                    }
                }

                return count - list.Count;
            }
        }

        /// <summary>更新指定数据行，性能很差，全部读取替换后保存</summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public Boolean Update(T model)
        {
            if (Comparer == null) throw new ArgumentNullException(nameof(Comparer));

            lock (this)
            {
                var list = FindAll();
                if (list.Count == 0) return false;

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
                if (!flag) return false;

                // 重新写回去
                Write(list, false);

                return true;
            }
        }

        /// <summary>查找指定数据行</summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public T Find(T model)
        {
            if (Comparer == null) throw new ArgumentNullException(nameof(Comparer));

            return FindAll(e => Comparer.Equals(model, e), 1).FirstOrDefault();
        }

        /// <summary>获取满足条件的第一行数据</summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public T Find(Func<T, Boolean> predicate) => FindAll(predicate, 1).FirstOrDefault();

        /// <summary>获取所有数据行</summary>
        /// <returns></returns>
        public IList<T> FindAll() => FindAll(null);

        /// <summary>获取满足条件的数据行，性能好，顺序查找</summary>
        /// <param name="predicate"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IList<T> FindAll(Func<T, Boolean> predicate, Int32 count = -1)
        {
            var file = GetFile();
            if (!File.Exists(file)) return new List<T>();

            lock (this)
            {
                using var csv = new CsvFile(file, false) { Encoding = Encoding };

                var list = new List<T>();
                var headers = new Dictionary<String, Int32>();
                var pis = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                while (true)
                {
                    var ss = csv.ReadLine();
                    if (ss == null) break;

                    // 头部，名称与序号对应
                    if (headers.Count == 0)
                    {
                        for (var i = 0; i < ss.Length; i++)
                        {
                            if (!headers.ContainsKey(ss[i])) headers[ss[i]] = i;
                        }
                        // 无法识别所有字段
                        if (headers.Count == 0) break;
                    }
                    else
                    {
                        try
                        {
                            // 反射构建对象
                            var model = new T();
                            foreach (var pi in pis)
                            {
                                var name = SerialHelper.GetName(pi);
                                if (pi.CanWrite && headers.TryGetValue(name, out var idx) && idx < ss.Length)
                                {
                                    var value = ss[idx].ChangeType(pi.PropertyType);
                                    if (value != null) pi.SetValue(model, value, null);
                                }
                            }

                            if (predicate == null || predicate(model))
                            {
                                list.Add(model);

                                if (--count == 0) break;
                            }
                        }
                        catch (Exception ex)
                        {
                            // 读取某一行出错，放弃该行
                            XTrace.WriteException(ex);
                        }
                    }
                }

                return list;
            }
        }

        /// <summary>获取数据行数，性能极好，文件行数（除头部）</summary>
        /// <returns></returns>
        public Int32 FindCount()
        {
            lock (this)
            {
                var file = GetFile();
                if (!File.Exists(file)) return 0;

                var lines = File.ReadAllLines(file, Encoding);
                if (lines == null || lines.Length <= 1) return 0;

                // 除了头部以外的所有数据行
                return lines.Length - 1;
            }
        }
        #endregion

        #region 辅助
        private String GetFile() => FileName.GetFullPath();

        class MyComparer : IEqualityComparer<T>
        {
            public Func<T, T, Boolean> Comparer;

            public Boolean Equals(T x, T y) => Comparer(x, y);

            public Int32 GetHashCode(T obj) => obj.GetHashCode();
        }
        #endregion
    }
}