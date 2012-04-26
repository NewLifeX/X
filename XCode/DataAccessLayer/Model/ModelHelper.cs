using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Microsoft.CSharp;
using NewLife.Linq;
#if NET4
using System.Linq;
#endif

namespace XCode.DataAccessLayer
{
    /// <summary>数据模型扩展</summary>
    public static class ModelHelper
    {
        #region 模型扩展方法
        /// <summary>根据字段名获取字段</summary>
        /// <param name="table"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IDataColumn GetColumn(this IDataTable table, String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            //foreach (IDataColumn item in table.Columns)
            //{
            //    if (String.Equals(name, item.Name, StringComparison.OrdinalIgnoreCase)) return item;
            //}

            //foreach (IDataColumn item in table.Columns)
            //{
            //    if (String.Equals(name, item.Alias, StringComparison.OrdinalIgnoreCase)) return item;
            //}
            //return null;

            return table.Columns.FirstOrDefault(c => c.Is(name));
        }

        /// <summary>根据字段名数组获取字段数组</summary>
        /// <param name="table"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public static IDataColumn[] GetColumns(this IDataTable table, String[] names)
        {
            if (names == null || names.Length < 1) return null;

            //List<IDataColumn> list = new List<IDataColumn>();
            //foreach (String item in names)
            //{
            //    IDataColumn dc = table.GetColumn(item);
            //    if (dc != null) list.Add(dc);
            //}

            //if (list.Count < 1) return null;
            //return list.ToArray();

            return table.Columns.Where(c => names.Any(n => c.Is(n))).ToArray();
        }

        /// <summary>判断表是否等于指定名字</summary>
        /// <param name="table"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Boolean Is(this IDataTable table, String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            return table.Name.EqualIgnoreCase(name) || table.Alias.EqualIgnoreCase(name);
        }

        /// <summary>判断字段是否等于指定名字</summary>
        /// <param name="column"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Boolean Is(this IDataColumn column, String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            return column.Name.EqualIgnoreCase(name) || column.Alias.EqualIgnoreCase(name);
        }

        /// <summary>根据字段名找索引</summary>
        /// <param name="table"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public static IDataIndex GetIndex(this IDataTable table, params String[] columnNames)
        {
            if (table == null || table.Indexes == null || table.Indexes.Count < 1) return null;

            IDataIndex di = table.Indexes.FirstOrDefault(
                e => e.Columns != null &&
                    e.Columns.Length == columnNames.Length &&
                    !e.Columns.Except(columnNames, StringComparer.OrdinalIgnoreCase).Any());
            if (di != null) return di;

            // 用别名再试一次
            IDataColumn[] columns = table.GetColumns(columnNames);
            if (columns == null || columns.Length < 1) return null;
            columnNames = columns.Select(e => e.Alias).ToArray();
            di = table.Indexes.FirstOrDefault(
                e => e.Columns != null &&
                    e.Columns.Length == columnNames.Length &&
                    !e.Columns.Except(columnNames, StringComparer.OrdinalIgnoreCase).Any());
            if (di != null) return di;

            return null;
        }

        //private static Boolean CompareStringArray(String[] arr1, String[] arr2)
        //{
        //    arr1 = prepare(arr1);
        //    arr2 = prepare(arr2);
        //    if (arr1 == arr2) return true;
        //    if (arr1.Length != arr2.Length) return false;

        //    for (int i = 0; i < arr1.Length; i++)
        //    {
        //        if (arr1[i] != arr2[i]) return false;
        //    }

        //    //for (int i = 0; i < arr1.Length; i++)
        //    //{
        //    //    Boolean b = false;
        //    //    for (int j = 0; j < arr2.Length; j++)
        //    //    {
        //    //        if (String.Equals(arr1[i], arr2[j], StringComparison.OrdinalIgnoreCase))
        //    //        {
        //    //            b = true;
        //    //            // 清空该项，不再跟后续项匹配
        //    //            arr2[j] = null;
        //    //            break;
        //    //        }
        //    //    }
        //    //    // 只要有一个找不到对应项，就是不存在
        //    //    if (!b) return false;
        //    //}

        //    return true;
        //}

        //private static String[] prepare(String[] arr)
        //{
        //    if (arr == null || arr.Length < 1) return null;

        //    List<String> list = new List<string>();
        //    for (int i = 0; i < arr.Length; i++)
        //    {
        //        String item = arr[i] == null ? "" : arr[i].ToLower();
        //        if (!list.Contains(item)) list.Add(item);
        //    }
        //    return list.ToArray();
        //}

        /// <summary>根据字段从指定表中查找关系</summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static IDataRelation GetRelation(this IDataTable table, String columnName)
        {
            //return table.Relations.FirstOrDefault(e => e.Column.EqualIgnoreCase(columnName));
            foreach (IDataRelation item in table.Relations)
            {
                if (String.Equals(item.Column, columnName, StringComparison.OrdinalIgnoreCase)) return item;
            }

            return null;
        }

        /// <summary>根据字段、关联表、关联字段从指定表中查找关系</summary>
        /// <param name="table"></param>
        /// <param name="dr"></param>
        /// <returns></returns>
        public static IDataRelation GetRelation(this IDataTable table, IDataRelation dr)
        {
            return table.GetRelation(dr.Column, dr.RelationTable, dr.RelationColumn);
        }

        /// <summary>根据字段、关联表、关联字段从指定表中查找关系</summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <param name="rtableName"></param>
        /// <param name="rcolumnName"></param>
        /// <returns></returns>
        public static IDataRelation GetRelation(this IDataTable table, String columnName, String rtableName, String rcolumnName)
        {
            foreach (IDataRelation item in table.Relations)
            {
                if (item.Column == columnName && item.RelationTable == rtableName && item.RelationColumn == rcolumnName) return item;
            }

            return null;
        }
        #endregion

        #region 模型扩展业务方法
        /// <summary>连接两个表，实际上是猜测它们之间的关系，根据一个字段名是否等于另一个表的表名加某个字段名来判断是否存在关系。</summary>
        /// <param name="table"></param>
        /// <param name="rtable"></param>
        public static void Connect(this IDataTable table, IDataTable rtable)
        {
            foreach (IDataColumn dc in table.Columns)
            {
                if (dc.PrimaryKey || dc.Identity) continue;

                if (FindRelation(table, rtable, rtable.Name, dc, dc.Name) != null) continue;
                if (!dc.Name.EqualIgnoreCase(dc.Alias))
                {
                    if (FindRelation(table, rtable, rtable.Name, dc, dc.Alias) != null) continue;
                }

                //if (String.Equals(rtable.Alias, rtable.Name, StringComparison.OrdinalIgnoreCase)) continue;
                if (rtable.Name.EqualIgnoreCase(rtable.Alias)) continue;

                // 如果表2的别名和名称不同，还要继续
                if (FindRelation(table, rtable, rtable.Alias, dc, dc.Name) != null) continue;
                if (!dc.Name.EqualIgnoreCase(dc.Alias))
                {
                    if (FindRelation(table, rtable, rtable.Alias, dc, dc.Alias) != null) continue;
                }
            }
        }

        static IDataRelation FindRelation(IDataTable table, IDataTable rtable, String rname, IDataColumn column, String name)
        {
            if (name.Length <= rtable.Name.Length || !name.StartsWith(rtable.Name, StringComparison.OrdinalIgnoreCase)) return null;

            String key = name.Substring(rtable.Name.Length);
            IDataColumn dc = rtable.GetColumn(key);
            // 猜测两表关联关系时，两个字段的类型也必须一致
            if (dc == null || dc.DataType != column.DataType) return null;

            // 建立关系
            IDataRelation dr = table.CreateRelation();
            dr.Column = column.Name;
            dr.RelationTable = rtable.Name;
            dr.RelationColumn = dc.Name;
            // 表关系这里一般是多对一，比如管理员的RoleID=>Role+Role.ID，对于索引来说，不是唯一的
            dr.Unique = false;
            // 当然，如果这个字段column有唯一索引，那么，这里也是唯一的。这就是典型的一对一
            if (column.PrimaryKey || column.Identity)
                dr.Unique = true;
            else
            {
                IDataIndex di = GetIndex(table, column.Name);
                if (di != null && di.Unique) dr.Unique = true;
            }

            dr.Computed = true;
            table.Relations.Add(dr);

            // 给另一方建立关系
            //foreach (IDataRelation item in rtable.Relations)
            //{
            //    if (item.Column == dc.Name && item.RelationTable == table.Name && item.RelationColumn == column.Name) return dr;
            //}
            if (rtable.GetRelation(dc.Name, table.Name, column.Name) != null) return dr;

            dr = rtable.CreateRelation();
            dr.Column = dc.Name;
            dr.RelationTable = table.Name;
            dr.RelationColumn = column.Name;
            // 那么这里就是唯一的啦
            dr.Unique = true;
            // 当然，如果字段dc不是主键，也没有唯一索引，那么关系就不是唯一的。这就是典型的多对多
            if (!dc.PrimaryKey && !dc.Identity)
            {
                IDataIndex di = GetIndex(rtable, dc.Name);
                // 没有索引，或者索引不是唯一的
                if (di == null || !di.Unique) dr.Unique = false;
            }

            dr.Computed = true;
            rtable.Relations.Add(dr);

            return dr;
        }

        /// <summary>修正数据</summary>
        /// <param name="table"></param>
        public static void Fix(this IDataTable table)
        {
            #region 根据单字段索引修正对应的关系
            // 给所有单字段索引建立关系，特别是一对一关系
            foreach (IDataIndex item in table.Indexes)
            {
                if (item.Columns == null || item.Columns.Length != 1) continue;

                IDataRelation dr = table.GetRelation(item.Columns[0]);
                if (dr == null) continue;

                dr.Unique = item.Unique;
                // 跟关系有关联的索引
                dr.Computed = item.Computed;
            }
            #endregion

            #region 给所有关系字段建立索引
            foreach (IDataRelation dr in table.Relations)
            {
                // 跳过主键
                IDataColumn dc = table.GetColumn(dr.Column);
                if (dc == null || dc.PrimaryKey) continue;

                if (table.GetIndex(dr.Column) == null)
                {
                    IDataIndex di = table.CreateIndex();
                    di.Columns = new String[] { dr.Column };
                    // 这两个的关系，唯一性
                    di.Unique = dr.Unique;
                    di.Computed = true;
                    table.Indexes.Add(di);
                }
            }
            #endregion

            #region 从索引中修正主键
            IDataColumn[] pks = table.PrimaryKeys;
            if (pks == null || pks.Length < 1)
            {
                // 在索引中找唯一索引作为主键
                foreach (IDataIndex item in table.Indexes)
                {
                    if (!item.PrimaryKey || item.Columns == null || item.Columns.Length < 1) continue;

                    pks = table.GetColumns(item.Columns);
                    if (pks != null && pks.Length > 0) Array.ForEach<IDataColumn>(pks, dc => dc.PrimaryKey = true);
                }
            }
            pks = table.PrimaryKeys;
            if (pks == null || pks.Length < 1)
            {
                // 在索引中找唯一索引作为主键
                foreach (IDataIndex item in table.Indexes)
                {
                    if (!item.Unique || item.Columns == null || item.Columns.Length < 1) continue;

                    pks = table.GetColumns(item.Columns);
                    if (pks != null && pks.Length > 0) Array.ForEach<IDataColumn>(pks, dc => dc.PrimaryKey = true);
                }
            }
            pks = table.PrimaryKeys;
            if (pks == null || pks.Length < 1)
            {
                // 如果还没有主键，把第一个索引作为主键
                foreach (IDataIndex item in table.Indexes)
                {
                    if (item.Columns == null || item.Columns.Length < 1) continue;

                    pks = table.GetColumns(item.Columns);
                    if (pks != null && pks.Length > 0) Array.ForEach<IDataColumn>(pks, dc => dc.PrimaryKey = true);
                }
            }
            #endregion

            #region 最后修复主键
            if (table.PrimaryKeys.Length < 1)
            {
                // 自增作为主键，然后是ID/Guid/UID，最后默认使用第一个
                // 没办法，如果没有主键，整个实体层都会面临大问题！
                IDataColumn dc = null;
                if ((dc = table.Columns.FirstOrDefault(c => c.Identity)) != null)
                    dc.PrimaryKey = true;
                else if ((dc = table.Columns.FirstOrDefault(c => c.Is("ID"))) != null)
                    dc.PrimaryKey = true;
                else if ((dc = table.Columns.FirstOrDefault(c => c.Is("Guid"))) != null)
                    dc.PrimaryKey = true;
                else if ((dc = table.Columns.FirstOrDefault(c => c.Is("UID"))) != null)
                    dc.PrimaryKey = true;
                else if ((dc = table.Columns.FirstOrDefault()) != null)
                    dc.PrimaryKey = true;
            }
            #endregion

            #region 给非主键的自增字段建立唯一索引
            foreach (IDataColumn dc in table.Columns)
            {
                if (dc.Identity && !dc.PrimaryKey)
                {
                    IDataIndex di = GetIndex(table, dc.Name);
                    if (di == null)
                    {
                        di = table.CreateIndex();
                        di.Columns = new String[] { dc.Name };
                        di.Computed = true;
                    }
                    // 不管是不是原来有的索引，都要唯一
                    di.Unique = true;
                }
            }
            #endregion

            #region 索引应该具有跟字段一样的唯一和主键约束
            // 主要针对MSSQL2000
            foreach (var di in table.Indexes)
            {
                if (di.Columns == null) continue;

                var dcs = table.GetColumns(di.Columns);
                if (dcs == null || dcs.Length <= 0) continue;

                if (!di.Unique) di.Unique = dcs.All(dc => dc.Identity);
                if (!di.PrimaryKey) di.PrimaryKey = dcs.All(dc => dc.PrimaryKey);
            }
            #endregion

            #region 修正可能错误的别名
            List<String> ns = new List<string>();
            ns.Add(table.Alias);
            foreach (IDataColumn item in table.Columns)
            {
                if (ns.Contains(item.Alias) || IsKeyWord(item.Alias))
                {
                    // 通过加数字的方式，解决关键字问题
                    for (int i = 2; i < table.Columns.Count; i++)
                    {
                        String name = item.Alias + i;
                        // 加了数字后，不可能是关键字
                        if (!ns.Contains(name))
                        {
                            item.Alias = name;
                            break;
                        }
                    }
                }

                ns.Add(item.Alias);
            }
            #endregion
        }
        #endregion

        #region 复制扩展方法
        /// <summary>复制数据表到另一个数据表，不复制数据列、索引和关系</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataTable CopyFrom(this IDataTable src, IDataTable des)
        {
            src.ID = des.ID;
            src.Name = des.Name;
            src.Alias = des.Alias;
            src.Owner = des.Owner;
            src.DbType = des.DbType;
            src.IsView = des.IsView;
            src.Description = des.Description;

            return src;
        }

        /// <summary>复制数据表到另一个数据表，复制所有数据列、索引和关系</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <param name="resetColumnID">是否重置列ID</param>
        /// <returns></returns>
        public static IDataTable CopyAllFrom(this IDataTable src, IDataTable des, Boolean resetColumnID = false)
        {
            src.CopyFrom(des);
            src.Columns.AddRange(des.Columns.Select(i => src.CreateColumn().CopyFrom(i)));
            src.Indexes.AddRange(des.Indexes.Select(i => src.CreateIndex().CopyFrom(i)));
            src.Relations.AddRange(des.Relations.Select(i => src.CreateRelation().CopyFrom(i)));
            // 重载ID
            if (resetColumnID) src.Columns.ForEach((it, i) => it.ID = i + 1);

            return src;
        }

        /// <summary>赋值数据列到另一个数据列</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataColumn CopyFrom(this IDataColumn src, IDataColumn des)
        {
            src.ID = des.ID;
            src.Name = des.Name;
            src.Alias = des.Alias;
            src.DataType = des.DataType;
            src.RawType = des.RawType;
            src.Identity = des.Identity;
            src.PrimaryKey = des.PrimaryKey;
            src.Length = des.Length;
            src.NumOfByte = des.NumOfByte;
            src.Precision = des.Precision;
            src.Scale = des.Scale;
            src.Nullable = des.Nullable;
            src.IsUnicode = des.IsUnicode;
            src.Default = des.Default;
            src.Description = des.Description;

            return src;
        }

        /// <summary>赋值数据列到另一个数据列</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataIndex CopyFrom(this IDataIndex src, IDataIndex des)
        {
            src.Name = des.Name;
            src.Columns = des.Columns;
            src.Unique = des.Unique;
            src.PrimaryKey = des.PrimaryKey;
            src.Computed = des.Computed;

            return src;
        }

        /// <summary>赋值数据列到另一个数据列</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataRelation CopyFrom(this IDataRelation src, IDataRelation des)
        {
            src.Column = des.Column;
            src.RelationTable = des.RelationTable;
            src.RelationColumn = des.RelationColumn;
            src.Unique = des.Unique;
            src.Computed = des.Computed;

            return src;
        }
        #endregion

        #region 辅助
        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。另外，避免一个表中的字段别名重名</summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        public static String GetAlias(IDataColumn dc)
        {
            var name = GetAlias(dc.Name);
            var df = dc as XField;
            if (df != null && dc.Table != null)
            {
                var lastname = name;
                var index = 0;
                var cs = dc.Table.Columns;
                for (int i = 0; i < cs.Count; i++)
                {
                    var item = cs[i] as XField;
                    if (item != df && item.Name != df.Name)
                    {
                        if (lastname.EqualIgnoreCase(item._Alias))
                        {
                            lastname = name + ++index;
                            // 从头开始
                            i = -1;
                        }
                    }
                }
                lastname = name;
            }
            return name;
        }

        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String GetAlias(String name)
        {
            if (String.IsNullOrEmpty(name)) return name;

            name = name.Replace("$", null);
            name = name.Replace("(", null);
            name = name.Replace(")", null);
            name = name.Replace("（", null);
            name = name.Replace("）", null);
            name = name.Replace(" ", null);
            name = name.Replace("　", null);

            // 很多时候，这个别名就是表名
            return FixWord(CutPrefix(name));
        }

        static String CutPrefix(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            // 自动去掉前缀
            Int32 n = name.IndexOf("_");
            // _后至少要有2个字母，并且后一个不能是_
            if (n >= 0 && n < name.Length - 2 && name[n + 1] != '_')
            {
                String str = name.Substring(n + 1);
                if (!IsKeyWord(str)) name = str;
            }

            String[] ss = new String[] { "tbl", "table" };
            foreach (String s in ss)
            {
                if (name.StartsWith(s))
                {
                    String str = name.Substring(s.Length);
                    if (!IsKeyWord(str)) name = str;
                }
                else if (name.EndsWith(s))
                {
                    String str = name.Substring(0, name.Length - s.Length);
                    if (!IsKeyWord(str)) name = str;
                }
            }

            return name;
        }

        /// <summary>自动处理大小写</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static String FixWord(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            if (name.Equals("ID", StringComparison.OrdinalIgnoreCase)) return "ID";

            if (name.Length <= 2) return name;

            Int32 count1 = 0;
            Int32 count2 = 0;
            foreach (Char item in name.ToCharArray())
            {
                if (item >= 'a' && item <= 'z')
                    count1++;
                else if (item >= 'A' && item <= 'Z')
                    count2++;
            }

            //没有或者只有一个小写字母的，需要修正
            //没有大写的，也要修正
            if (count1 <= 1 || count2 < 1)
            {
                name = name.ToLower();
                Char c = name[0];
                if (c >= 'a' && c <= 'z') c = (Char)(c - 'a' + 'A');
                name = c + name.Substring(1);
            }

            //处理Is开头的，第三个字母要大写
            if (name.StartsWith("Is") && name.Length >= 3)
            {
                Char c = name[2];
                if (c >= 'a' && c <= 'z')
                {
                    c = (Char)(c - 'a' + 'A');
                    name = name.Substring(0, 2) + c + name.Substring(3);
                }
            }

            return name;
        }

        private static CodeDomProvider[] _CGS;
        /// <summary>代码生成器</summary>
        public static CodeDomProvider[] CGS
        {
            get
            {
                if (_CGS == null)
                {
                    _CGS = new CodeDomProvider[] { new CSharpCodeProvider()/*, new VBCodeProvider()*/ };
                }
                return _CGS;
            }
        }

        static Boolean IsKeyWord(String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            // 特殊处理item
            if (String.Equals(name, "item", StringComparison.OrdinalIgnoreCase)) return true;

            // 只要有大写字母，就不是关键字
            if (name.Any(c => c >= 'A' && c <= 'Z')) return false;

            foreach (CodeDomProvider item in CGS)
            {
                if (!item.IsValidIdentifier(name)) return true;
            }

            return false;
        }

        /// <summary>获取显示名，如果描述不存在，则使用名称，否则使用描述前面部分，句号（中英文皆可）分隔</summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static String GetDisplayName(String name, String description)
        {
            if (String.IsNullOrEmpty(description)) return name;

            String str = description.Trim();
            Int32 p = str.IndexOfAny(new Char[] { '.', '。', '\r', '\n' });
            // p=0表示符号在第一位，不考虑
            if (p > 0) str = str.Substring(0, p).Trim();

            return str;
        }
        #endregion
    }
}