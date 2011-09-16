using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using Microsoft.VisualBasic;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 模块助手
    /// </summary>
    public static class ModelHelper
    {
        /// <summary>
        /// 根据字段名获取字段
        /// </summary>
        /// <param name="table"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IDataColumn GetColumn(IDataTable table, String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            foreach (IDataColumn item in table.Columns)
            {
                if (String.Equals(name, item.Name, StringComparison.OrdinalIgnoreCase)) return item;
            }

            foreach (IDataColumn item in table.Columns)
            {
                if (String.Equals(name, item.Alias, StringComparison.OrdinalIgnoreCase)) return item;
            }
            return null;
        }

        /// <summary>
        /// 根据字段名数组获取字段数组
        /// </summary>
        /// <param name="table"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public static IDataColumn[] GetColumns(IDataTable table, String[] names)
        {
            if (names == null || names.Length < 1) return null;

            List<IDataColumn> list = new List<IDataColumn>();
            foreach (String item in names)
            {
                IDataColumn dc = table.GetColumn(item);
                if (dc != null) list.Add(dc);
            }

            if (list.Count < 1) return null;
            return list.ToArray(); ;
        }

        /// <summary>
        /// 连接两个表，实际上是猜测它们之间的关系
        /// </summary>
        /// <param name="table1"></param>
        /// <param name="table2"></param>
        public static void Connect(IDataTable table1, IDataTable table2)
        {
            foreach (IDataColumn dc in table1.Columns)
            {
                if (dc.PrimaryKey || dc.Identity) continue;

                if (FindRelation(table1, table2, table2.Name, dc, dc.Name) != null) continue;
                if (!String.IsNullOrEmpty(dc.Alias) && !String.Equals(dc.Alias, dc.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (FindRelation(table1, table2, table2.Name, dc, dc.Alias) != null) continue;
                }

                if (FindRelation(table1, table2, table2.Alias, dc, dc.Name) != null) continue;
                if (!String.IsNullOrEmpty(dc.Alias) && !String.Equals(dc.Alias, dc.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (FindRelation(table1, table2, table2.Alias, dc, dc.Alias) != null) continue;
                }
            }
        }

        static IDataRelation FindRelation(IDataTable table1, IDataTable rtable, String rname, IDataColumn column, String name)
        {
            if (name.Length <= rtable.Name.Length || !name.StartsWith(rtable.Name, StringComparison.OrdinalIgnoreCase)) return null;

            String key = name.Substring(rtable.Name.Length);
            IDataColumn dc = rtable.GetColumn(key);
            if (dc == null) return null;

            // 建立关系
            IDataRelation dr = table1.CreateRelation();
            dr.Column = column.Name;
            dr.RelationTable = rtable.Name;
            dr.RelationColumn = dc.Name;
            // 表关系这里一般是多对一，比如管理员的RoleID，对于索引来说，不是唯一的
            dr.Unique = false;

            table1.Relations.Add(dr);

            // 给另一方建立关系
            foreach (IDataRelation item in rtable.Relations)
            {
                if (item.Column == dc.Name && item.RelationTable == table1.Name && item.RelationColumn == column.Name) return dr;
            }
            dr = rtable.CreateRelation();
            dr.Column = dc.Name;
            dr.RelationTable = table1.Name;
            dr.RelationColumn = column.Name;
            // 那么这里就是唯一的啦
            dr.Unique = true;

            rtable.Relations.Add(dr);

            return dr;
        }

        /// <summary>
        /// 修正数据
        /// </summary>
        /// <param name="table"></param>
        public static void Fix(IDataTable table)
        {
            #region 给所有关系字段建立索引
            foreach (IDataRelation dr in table.Relations)
            {
                // 跳过主键
                IDataColumn dc = table.GetColumn(dr.Column);
                if (dc == null || dc.PrimaryKey) continue;

                Boolean hasIndex = false;
                foreach (IDataIndex item in table.Indexes)
                {
                    if (item.Columns != null && item.Columns.Length == 1 && String.Equals(item.Columns[0], dr.Column, StringComparison.OrdinalIgnoreCase))
                    {
                        hasIndex = true;
                        break;
                    }
                }
                if (!hasIndex)
                {
                    IDataIndex di = table.CreateIndex();
                    di.Columns = new String[] { dr.Column };
                    // 这两个的关系，唯一性
                    di.Unique = dr.Unique;
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

        /// <summary>
        /// 根据字段名找索引
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public static IDataIndex GetIndex(IDataTable table, String[] columnNames)
        {
            if (table == null || table.Indexes == null || table.Indexes.Count < 1) return null;

            foreach (IDataIndex item in table.Indexes)
            {
                if (item.Columns == null || item.Columns.Length < 1) continue;

                if (CompareStringArray(item.Columns, columnNames)) return item;
            }

            return null;
        }

        private static Boolean CompareStringArray(String[] arr1, String[] arr2)
        {
            arr1 = prepare(arr1);
            arr2 = prepare(arr2);
            if (arr1 == arr2) return true;
            if (arr1.Length != arr2.Length) return false;

            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i]) return false;
            }

            //for (int i = 0; i < arr1.Length; i++)
            //{
            //    Boolean b = false;
            //    for (int j = 0; j < arr2.Length; j++)
            //    {
            //        if (String.Equals(arr1[i], arr2[j], StringComparison.OrdinalIgnoreCase))
            //        {
            //            b = true;
            //            // 清空该项，不再跟后续项匹配
            //            arr2[j] = null;
            //            break;
            //        }
            //    }
            //    // 只要有一个找不到对应项，就是不存在
            //    if (!b) return false;
            //}

            return true;
        }

        private static String[] prepare(String[] arr)
        {
            if (arr == null || arr.Length < 1) return null;

            List<String> list = new List<string>();
            for (int i = 0; i < arr.Length; i++)
            {
                String item = arr[i] == null ? "" : arr[i].ToLower();
                if (!list.Contains(item)) list.Add(item);
            }
            return list.ToArray();
        }

        #region 辅助
        /// <summary>
        /// 获取别名
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String GetAlias(String name)
        {
            // 很多时候，这个别名就是表名
            return FixWord(CutPrefix(name));
        }

        static String CutPrefix(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            // 自动去掉前缀
            Int32 n = name.IndexOf("_");
            // _后至少要有2个字母
            if (n >= 0 && n < name.Length - 2)
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

        /// <summary>
        /// 自动处理大小写
        /// </summary>
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
                    _CGS = new CodeDomProvider[] { new CSharpCodeProvider(), new VBCodeProvider() };
                }
                return _CGS;
            }
        }

        static Boolean IsKeyWord(String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            // 特殊处理item
            if (String.Equals(name, "item", StringComparison.OrdinalIgnoreCase)) return true;

            foreach (CodeDomProvider item in CGS)
            {
                if (!item.IsValidIdentifier(name)) return true;
            }

            return false;
        }
        #endregion
    }
}