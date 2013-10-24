using System;
using System.Text;
using Microsoft.CSharp;
using NewLife.Collections;
using NewLife.Configuration;
using XCode.Model;

#if NET4
using System.Collections.Generic;
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace XCode.DataAccessLayer
{
    /// <summary>模型解析器接口。解决名称大小写、去前缀、关键字等多个问题</summary>
    public interface IModelResolver
    {
        #region 名称处理
        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。另外，避免一个表中的字段别名重名</summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        String GetName(IDataColumn dc);

        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        String GetName(String name);

        /// <summary>根据字段名等信息计算索引的名称</summary>
        /// <param name="di"></param>
        /// <returns></returns>
        String GetName(IDataIndex di);

        /// <summary>去除前缀。默认去除第一个_前面部分，去除tbl和table前缀</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        String CutPrefix(String name);

        /// <summary>自动处理大小写</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        String FixWord(String name);

        ///// <summary>是否关键字</summary>
        ///// <param name="name">名称</param>
        ///// <returns></returns>
        //Boolean IsKeyWord(String name);

        /// <summary>获取显示名，如果描述不存在，则使用名称，否则使用描述前面部分，句号（中英文皆可）、换行分隔</summary>
        /// <param name="name">名称</param>
        /// <param name="description"></param>
        /// <returns></returns>
        String GetDisplayName(String name, String description);
        #endregion

        #region 模型处理
        /// <summary>连接两个表。
        /// 实际上是猜测它们之间的关系，根据一个字段名是否等于另一个表的表名加某个字段名来判断是否存在关系。</summary>
        /// <param name="table"></param>
        /// <param name="rtable"></param>
        IDataTable Connect(IDataTable table, IDataTable rtable);

        /// <summary>猜测表间关系</summary>
        /// <param name="table"></param>
        /// <param name="rtable"></param>
        /// <param name="rname"></param>
        /// <param name="column"></param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        Boolean GuessRelation(IDataTable table, IDataTable rtable, String rname, IDataColumn column, String name);

        /// <summary>修正数据</summary>
        /// <param name="table"></param>
        IDataTable Fix(IDataTable table);

        /// <summary>修正数据列</summary>
        /// <param name="column"></param>
        IDataColumn Fix(IDataColumn column);
        #endregion

        #region 设置
        /// <summary>是否ID作为id的格式化，否则使用原名。默认使用ID</summary>
        Boolean UseID { get; set; }

        /// <summary>是否自动去除前缀。默认启用</summary>
        Boolean AutoCutPrefix { get; set; }

        /// <summary>是否自动去除字段前面的表名。默认启用</summary>
        Boolean AutoCutTableName { get; set; }

        /// <summary>是否自动纠正大小写。默认启用</summary>
        Boolean AutoFixWord { get; set; }

        /// <summary>要过滤的前缀</summary>
        String[] FilterPrefixs { get; set; }
        #endregion
    }

    /// <summary>模型解析器。解决名称大小写、去前缀、关键字等多个问题</summary>
    public class ModelResolver : IModelResolver
    {
        #region 名称处理
        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。另外，避免一个表中的字段别名重名</summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        public virtual String GetName(IDataColumn dc)
        {
            var name = dc.ColumnName;
            // 对于自增字段，如果强制使用ID，并且字段名以ID结尾，则直接取用ID
            if (dc.Identity && UseID && name.EndsWith("ID", StringComparison.OrdinalIgnoreCase)) return "ID";

            #region 先去掉表前缀
            var dt = dc.Table;
            if (dt != null && AutoCutTableName)
            {
                //if (name.StartsWith(dt.Name, StringComparison.OrdinalIgnoreCase))
                //    name = name.Substring(dt.Name.Length);
                //else if (name.StartsWith(dt.Alias, StringComparison.OrdinalIgnoreCase))
                //    name = name.Substring(dt.Alias.Length);
                var pfs = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                if (!dt.TableName.IsNullOrWhiteSpace()) pfs.Add(dt.TableName);
                // 如果包括下划线，再分割
                if (dt.TableName.Contains("_"))
                {
                    foreach (var item in dt.TableName.Split("_"))
                    {
                        if (item != null && item.Length >= 2 && !pfs.Contains(item)) pfs.Add(item);
                    }
                }
                if (!dt.Name.IsNullOrWhiteSpace() && !pfs.Contains(dt.Name))
                {
                    pfs.Add(dt.Name);
                    // 如果包括下划线，再分割
                    if (dt.Name.Contains("_"))
                    {
                        foreach (var item in dt.Name.Split("_"))
                        {
                            if (item != null && item.Length >= 2 && !pfs.Contains(item)) pfs.Add(item);
                        }
                    }
                }

                foreach (var item in pfs)
                {
                    if (name.StartsWith(item, StringComparison.OrdinalIgnoreCase) && name.Length != item.Length) name = name.Substring(item.Length);
                }
                if (name[0] == '_') name = name.Substring(1);
            }
            #endregion

            name = GetName(name);
            if (dt != null)
            {
                var lastname = name;
                var index = 0;
                var cs = dt.Columns;
                for (int i = 0; i < cs.Count; i++)
                {
                    var item = cs[i];
                    if (item != dc && item.ColumnName != dc.ColumnName)
                    {
                        // 对于小于当前的采用别名，对于大于当前的，采用字段名，保证同名有优先级
                        if (lastname.EqualIgnoreCase(item.ID < dc.ID ? item.Name : item.ColumnName))
                        {
                            lastname = name + ++index;
                            // 从头开始
                            i = -1;
                        }
                    }
                }
                name = lastname;
            }
            return name;
        }

        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual String GetName(String name)
        {
            if (String.IsNullOrEmpty(name)) return name;

            name = name.Replace("$", null);
            name = name.Replace("(", null);
            name = name.Replace(")", null);
            name = name.Replace("（", null);
            name = name.Replace("）", null);
            name = name.Replace(" ", null);
            name = name.Replace("　", null);
            name = name.Replace("/", "_");
            name = name.Replace("\\", "_");
            if (name[0] == '_') name = name.Substring(1);

            // 很多时候，这个别名就是表名
            //return FixWord(CutPrefix(name));
            //if (AutoCutPrefix) name = CutPrefix(name);
            name = CutPrefix(name);
            if (AutoFixWord) name = FixWord(name);
            if (name[0] == '_') name = name.Substring(1);
            return name;
        }

        /// <summary>根据字段名等信息计算索引的名称</summary>
        /// <param name="di"></param>
        /// <returns></returns>
        public virtual String GetName(IDataIndex di)
        {
            if (di.Columns == null || di.Columns.Length < 1) return null;

            var sb = new StringBuilder();
            if (di.PrimaryKey)
                sb.Append("PK");
            else if (di.Unique)
                sb.Append("IU");
            else
                sb.Append("IX");

            if (di.Table != null)
            {
                sb.Append("_");
                sb.Append(di.Table.TableName);
            }
            for (int i = 0; i < di.Columns.Length; i++)
            {
                sb.Append("_");
                sb.Append(di.Columns[i]);
            }
            return sb.ToString();
        }

        /// <summary>去除前缀。默认去除第一个_前面部分，去除tbl和table前缀</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual String CutPrefix(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            var old = name;
            foreach (var s in FilterPrefixs)
            {
                if (name.StartsWith(s, StringComparison.OrdinalIgnoreCase) && name.Length != s.Length)
                {
                    var str = name.Substring(s.Length);
                    if (!IsKeyWord(str)) name = str;
                }
                else if (name.EndsWith(s, StringComparison.OrdinalIgnoreCase) && name.Length != s.Length)
                {
                    var str = name.Substring(0, name.Length - s.Length);
                    if (!IsKeyWord(str)) name = str;
                }
            }

            // 自动去掉前缀，如果上面有过滤，这里是不能去除的，否则可能过度
            if (AutoCutPrefix && name == old)
            {
                Int32 n = name.IndexOf("_");
                // _后至少要有2个字母，并且后一个不能是_
                if (n >= 0 && n < name.Length - 2 && name[n + 1] != '_')
                {
                    String str = name.Substring(n + 1);
                    if (!IsKeyWord(str)) name = str;
                }
            }

            return name;
        }

        /// <summary>自动处理大小写</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual String FixWord(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            if (UseID && name.Equals("ID", StringComparison.OrdinalIgnoreCase)) return "ID";

            if (name.Length <= 2) return name;

            // 如果包括下划线，特殊处理
            if (name.Contains("_"))
            {
                var ss = name.Split('_');
                for (int i = 0; i < ss.Length; i++)
                {
                    if (!ss[i].IsNullOrWhiteSpace()) ss[i] = FixWord(ss[i]);
                }
                return String.Join("_", ss);
            }

            Int32 lowerCount = 0;
            Int32 upperCount = 0;
            foreach (var item in name)
            {
                if (item >= 'a' && item <= 'z')
                    lowerCount++;
                else if (item >= 'A' && item <= 'Z')
                    upperCount++;
            }

            //没有或者只有一个小写字母的，需要修正
            //没有大写的，也要修正
            if (lowerCount <= 1 || upperCount < 1)
            {
                name = name.ToLower();
                Char c = name[0];
                if (c >= 'a' && c <= 'z') c = (Char)(c - 'a' + 'A');
                name = c + name.Substring(1);
            }
            else
            {
                Char c = name[0];
                if (c >= 'a' && c <= 'z')
                {
                    c = (Char)(c - 'a' + 'A');
                    name = c + name.Substring(1);
                }
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

        /// <summary>代码生成器</summary>
        private static CSharpCodeProvider _CG = new CSharpCodeProvider();

        /// <summary>是否关键字</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        static Boolean IsKeyWord(String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            // 特殊处理item
            if (String.Equals(name, "item", StringComparison.OrdinalIgnoreCase)) return true;

            // 只要有大写字母，就不是关键字
            if (name.Any(c => c >= 'A' && c <= 'Z')) return false;

            return !_CG.IsValidIdentifier(name);
        }

        /// <summary>获取显示名，如果描述不存在，则使用名称，否则使用描述前面部分，句号（中英文皆可）、换行分隔</summary>
        /// <param name="name">名称</param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual String GetDisplayName(String name, String description)
        {
            if (String.IsNullOrEmpty(description)) return name;

            name = description.Trim();
            var p = name.IndexOfAny(new Char[] { '.', '。', '\r', '\n' });
            // p=0表示符号在第一位，不考虑
            if (p > 0) name = name.Substring(0, p).Trim();

            name = name.Replace("$", null);
            name = name.Replace("(", null);
            name = name.Replace(")", null);
            name = name.Replace("（", null);
            name = name.Replace("）", null);
            name = name.Replace(" ", null);
            name = name.Replace("　", null);
            name = name.Replace("/", "_");
            name = name.Replace("\\", "_");
            if (name[0] == '_') name = name.Substring(1);

            return name;
        }
        #endregion

        #region 模型处理
        /// <summary>连接两个表。
        /// 实际上是猜测它们之间的关系，根据一个字段名是否等于另一个表的表名加某个字段名来判断是否存在关系。</summary>
        /// <param name="table"></param>
        /// <param name="rtable"></param>
        public virtual IDataTable Connect(IDataTable table, IDataTable rtable)
        {
            foreach (var dc in table.Columns)
            {
                if (dc.PrimaryKey || dc.Identity) continue;

                if (GuessRelation(table, rtable, rtable.TableName, dc, dc.ColumnName)) continue;
                if (!dc.ColumnName.EqualIgnoreCase(dc.Name))
                {
                    if (GuessRelation(table, rtable, rtable.TableName, dc, dc.Name)) continue;
                }

                //if (String.Equals(rtable.Alias, rtable.Name, StringComparison.OrdinalIgnoreCase)) continue;
                if (rtable.TableName.EqualIgnoreCase(rtable.Name)) continue;

                // 如果表2的别名和名称不同，还要继续
                if (GuessRelation(table, rtable, rtable.Name, dc, dc.ColumnName)) continue;
                if (!dc.ColumnName.EqualIgnoreCase(dc.Name))
                {
                    if (GuessRelation(table, rtable, rtable.Name, dc, dc.Name)) continue;
                }
            }

            return table;
        }

        /// <summary>猜测表间关系</summary>
        /// <param name="table"></param>
        /// <param name="rtable"></param>
        /// <param name="rname"></param>
        /// <param name="column"></param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual Boolean GuessRelation(IDataTable table, IDataTable rtable, String rname, IDataColumn column, String name)
        {
            if (name.Length <= rtable.TableName.Length || !name.StartsWith(rtable.TableName, StringComparison.OrdinalIgnoreCase)) return false;

            var key = name.Substring(rtable.TableName.Length);
            var dc = rtable.GetColumn(key);
            // 猜测两表关联关系时，两个字段的类型也必须一致
            if (dc == null || dc.DataType != column.DataType) return false;

            // 建立关系
            var dr = table.CreateRelation();
            dr.Column = column.ColumnName;
            dr.RelationTable = rtable.TableName;
            dr.RelationColumn = dc.ColumnName;
            // 表关系这里一般是多对一，比如管理员的RoleID=>Role+Role.ID，对于索引来说，不是唯一的
            dr.Unique = false;
            // 当然，如果这个字段column有唯一索引，那么，这里也是唯一的。这就是典型的一对一
            if (column.PrimaryKey || column.Identity)
                dr.Unique = true;
            else
            {
                var di = table.GetIndex(column.ColumnName);
                if (di != null && di.Unique) dr.Unique = true;
            }

            dr.Computed = true;
            if (table.GetRelation(dr) == null) table.Relations.Add(dr);

            // 给另一方建立关系
            //foreach (IDataRelation item in rtable.Relations)
            //{
            //    if (item.Column == dc.Name && item.RelationTable == table.Name && item.RelationColumn == column.Name) return dr;
            //}
            if (rtable.GetRelation(dc.ColumnName, table.TableName, column.ColumnName) != null) return true;

            dr = rtable.CreateRelation();
            dr.Column = dc.ColumnName;
            dr.RelationTable = table.TableName;
            dr.RelationColumn = column.ColumnName;
            // 那么这里就是唯一的啦
            dr.Unique = true;
            // 当然，如果字段dc不是主键，也没有唯一索引，那么关系就不是唯一的。这就是典型的多对多
            if (!dc.PrimaryKey && !dc.Identity)
            {
                var di = rtable.GetIndex(dc.ColumnName);
                // 没有索引，或者索引不是唯一的
                if (di == null || !di.Unique) dr.Unique = false;
            }

            dr.Computed = true;
            if (rtable.GetRelation(dr) == null) rtable.Relations.Add(dr);

            return true;
        }

        /// <summary>修正数据</summary>
        /// <param name="table"></param>
        public virtual IDataTable Fix(IDataTable table)
        {
            // 根据单字段索引修正对应的关系
            FixRelationBySingleIndex(table);

            // 给所有关系字段建立索引
            CreateIndexForRelation(table);

            // 从索引中修正主键
            FixPrimaryByIndex(table);

            #region 最后修复主键
            if (table.PrimaryKeys.Length < 1)
            {
                // 自增作为主键，然后是ID/Guid/UID，最后默认使用第一个
                // 没办法，如果没有主键，整个实体层都会面临大问题！
                IDataColumn dc = null;
                if ((dc = table.Columns.FirstOrDefault(c => c.Identity)) != null)
                    dc.PrimaryKey = true;
                //else if ((dc = table.Columns.FirstOrDefault(c => c.Is("ID"))) != null)
                //    dc.PrimaryKey = true;
                //else if ((dc = table.Columns.FirstOrDefault(c => c.Is("Guid"))) != null)
                //    dc.PrimaryKey = true;
                //else if ((dc = table.Columns.FirstOrDefault(c => c.Is("UID"))) != null)
                //    dc.PrimaryKey = true;
                //else if ((dc = table.Columns.FirstOrDefault()) != null)
                //    dc.PrimaryKey = true;
            }
            #endregion

            // 给非主键的自增字段建立唯一索引
            CreateUniqueIndexForIdentity(table);

            // 索引应该具有跟字段一样的唯一和主键约束
            FixIndex(table);

            #region 修正可能错误的别名
            //var ns = new List<String>();
            //ns.Add(table.Alias);
            //foreach (var item in table.Columns)
            //{
            //    if (ns.Contains(item.Alias) || IsKeyWord(item.Alias))
            //    {
            //        // 通过加数字的方式，解决关键字问题
            //        for (int i = 2; i < table.Columns.Count; i++)
            //        {
            //            var name = item.Alias + i;
            //            // 加了数字后，不可能是关键字
            //            if (!ns.Contains(name))
            //            {
            //                item.Alias = name;
            //                break;
            //            }
            //        }
            //    }

            //    ns.Add(item.Alias);
            //}
            foreach (var dc in table.Columns)
            {
                dc.Fix();
            }
            #endregion

            return table;
        }

        /// <summary>根据单字段索引修正对应的关系</summary>
        /// <param name="table"></param>
        protected virtual void FixRelationBySingleIndex(IDataTable table)
        {
            // 给所有单字段索引建立关系，特别是一对一关系
            foreach (var item in table.Indexes)
            {
                if (item.Columns == null || item.Columns.Length != 1) continue;

                var dr = table.GetRelation(item.Columns[0]);
                if (dr == null) continue;

                dr.Unique = item.Unique;
                // 跟关系有关联的索引
                dr.Computed = item.Computed;
            }
        }

        /// <summary>给所有关系字段建立索引</summary>
        /// <param name="table"></param>
        protected virtual void CreateIndexForRelation(IDataTable table)
        {
            foreach (var dr in table.Relations)
            {
                // 跳过主键
                var dc = table.GetColumn(dr.Column);
                if (dc == null || dc.PrimaryKey) continue;

                if (table.GetIndex(dr.Column) == null)
                {
                    var di = table.CreateIndex();
                    di.Columns = new String[] { dr.Column };
                    // 这两个的关系，唯一性
                    di.Unique = dr.Unique;
                    di.Computed = true;
                    table.Indexes.Add(di);
                }
            }
        }

        /// <summary>从索引中修正主键</summary>
        /// <param name="table"></param>
        protected virtual void FixPrimaryByIndex(IDataTable table)
        {
            var pks = table.PrimaryKeys;
            if (pks == null || pks.Length < 1)
            {
                // 在索引中找唯一索引作为主键
                foreach (var item in table.Indexes)
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
                foreach (var item in table.Indexes)
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
                foreach (var item in table.Indexes)
                {
                    if (item.Columns == null || item.Columns.Length < 1) continue;

                    pks = table.GetColumns(item.Columns);
                    if (pks != null && pks.Length > 0) Array.ForEach<IDataColumn>(pks, dc => dc.PrimaryKey = true);
                }
            }
        }

        /// <summary>给非主键的自增字段建立唯一索引</summary>
        /// <param name="table"></param>
        protected virtual void CreateUniqueIndexForIdentity(IDataTable table)
        {
            foreach (var dc in table.Columns)
            {
                if (dc.Identity && !dc.PrimaryKey)
                {
                    var di = table.GetIndex(dc.ColumnName);
                    if (di == null)
                    {
                        di = table.CreateIndex();
                        di.Columns = new String[] { dc.ColumnName };
                        di.Computed = true;
                    }
                    // 不管是不是原来有的索引，都要唯一
                    di.Unique = true;
                }
            }
        }

        /// <summary>索引应该具有跟字段一样的唯一和主键约束</summary>
        /// <param name="table"></param>
        protected virtual void FixIndex(IDataTable table)
        {
            // 主要针对MSSQL2000
            foreach (var di in table.Indexes)
            {
                if (di.Columns == null) continue;

                var dcs = table.GetColumns(di.Columns);
                if (dcs == null || dcs.Length <= 0) continue;

                if (!di.Unique) di.Unique = dcs.All(dc => dc.Identity);
                if (!di.PrimaryKey) di.PrimaryKey = dcs.All(dc => dc.PrimaryKey);
            }
        }

        /// <summary>修正数据列</summary>
        /// <param name="column"></param>
        public virtual IDataColumn Fix(IDataColumn column)
        {
            return column;
        }
        #endregion

        #region 静态实例
        /// <summary>当前名称解析器</summary>
        public static IModelResolver Current { get { return XCodeService.ResolveInstance<IModelResolver>(); } }
        #endregion

        #region 设置
        private Boolean? _UseID;
        /// <summary>是否ID作为id的格式化，否则使用原名。默认使用ID</summary>
        public Boolean UseID { get { return _UseID != null ? _UseID.Value : (_UseID = Config.GetConfig<Boolean>("XCode.Model.UseID", true)).Value; } set { _UseID = value; } }

        private Boolean? _AutoCutPrefix;
        /// <summary>是否自动去除前缀。默认启用</summary>
        public Boolean AutoCutPrefix { get { return _AutoCutPrefix != null ? _AutoCutPrefix.Value : (_AutoCutPrefix = Config.GetConfig<Boolean>("XCode.Model.AutoCutPrefix", true)).Value; } set { _AutoCutPrefix = value; } }

        private Boolean? _AutoCutTableName;
        /// <summary>是否自动去除字段前面的表名。默认启用</summary>
        public Boolean AutoCutTableName { get { return _AutoCutTableName != null ? _AutoCutTableName.Value : (_AutoCutTableName = Config.GetConfig<Boolean>("XCode.Model.AutoCutTableName", true)).Value; } set { _AutoCutTableName = value; } }

        private Boolean? _AutoFixWord;
        /// <summary>是否自动纠正大小写。默认启用</summary>
        public Boolean AutoFixWord { get { return _AutoFixWord != null ? _AutoFixWord.Value : (_AutoFixWord = Config.GetConfig<Boolean>("XCode.Model.AutoFixWord", true)).Value; } set { _AutoFixWord = value; } }

        private String[] _FilterPrefixs;
        /// <summary>要过滤的前缀</summary>
        public String[] FilterPrefixs { get { return _FilterPrefixs ?? (_FilterPrefixs = Config.GetConfigSplit<String>("XCode.Model.FilterPrefixs", null, new String[] { "tbl", "table" })); } set { _FilterPrefixs = value; } }
        #endregion
    }
}