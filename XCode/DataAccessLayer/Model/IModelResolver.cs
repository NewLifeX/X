using System;
using System.Linq;
using System.Text;
using NewLife.Collections;
using XCode.Model;

namespace XCode.DataAccessLayer
{
    /// <summary>模型解析器接口。解决名称大小写、去前缀、关键字等多个问题</summary>
    public interface IModelResolver
    {
        #region 名称处理
        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        String GetName(String name);

        /// <summary>根据字段名等信息计算索引的名称</summary>
        /// <param name="di"></param>
        /// <returns></returns>
        String GetName(IDataIndex di);

        /// <summary>获取显示名，如果描述不存在，则使用名称，否则使用描述前面部分，句号（中英文皆可）、换行分隔</summary>
        /// <param name="name">名称</param>
        /// <param name="description"></param>
        /// <returns></returns>
        String GetDisplayName(String name, String description);
        #endregion

        #region 模型处理
        /// <summary>修正数据</summary>
        /// <param name="table"></param>
        IDataTable Fix(IDataTable table);

        /// <summary>修正数据列</summary>
        /// <param name="column"></param>
        IDataColumn Fix(IDataColumn column);
        #endregion
    }

    /// <summary>模型解析器。解决名称大小写、去前缀、关键字等多个问题</summary>
    public class ModelResolver : IModelResolver
    {
        #region 名称处理
        /// <summary>获取别名。过滤特殊符号，过滤_之类的前缀。</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual String GetName(String name)
        {
            if (name.IsNullOrEmpty()) return name;

            name = name.Replace("$", null);
            name = name.Replace("(", null);
            name = name.Replace(")", null);
            name = name.Replace("（", null);
            name = name.Replace("）", null);
            name = name.Replace(" ", null);
            name = name.Replace("　", null);
            name = name.Replace("/", "_");
            name = name.Replace("\\", "_");

            // 全大写或全小写名字，格式化为驼峰格式  包含下划线的表名和字段名生成类时自动去掉下划线
            if (name.Contains("_"))//(  name == name.ToUpper() || name == name.ToLower()))//
            {
                var ns = name.Split("_");
                var sb = Pool.StringBuilder.Get();
                foreach (var item in ns)
                {
                    if (item.EqualIgnoreCase("ID"))
                        sb.Append("ID");
                    else
                    {
                        // 首字母大小写，其它小写
                        sb.Append(item.Substring(0, 1).ToUpper());
                        sb.Append(item.Substring(1).ToLower());
                    }
                }
                name = sb.Put(true);
            }
            else if (name != "ID" && name.Length > 2 && (name == name.ToUpper() || name == name.ToLower()))
            {
                name = name.Substring(0, 1).ToUpper() + name.Substring(1).ToLower();
            }

            return name;
        }

        /// <summary>根据字段名等信息计算索引的名称</summary>
        /// <param name="di"></param>
        /// <returns></returns>
        public virtual String GetName(IDataIndex di)
        {
            if (di.Columns == null || di.Columns.Length < 1) return null;

            var sb = Pool.StringBuilder.Get();
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
            for (var i = 0; i < di.Columns.Length; i++)
            {
                sb.Append("_");
                sb.Append(di.Columns[i]);
            }
            return sb.Put(true);
        }

        /// <summary>获取显示名，如果描述不存在，则使用名称，否则使用描述前面部分，句号（中英文皆可）、换行分隔</summary>
        /// <param name="name">名称</param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual String GetDisplayName(String name, String description)
        {
            if (String.IsNullOrEmpty(description)) return name;

            name = description.Trim();
            var p = name.IndexOfAny(new Char[] { '.', '。', ',', '，', '(', '（', '\r', '\n' });
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
        /// <summary>修正数据</summary>
        /// <param name="table"></param>
        public virtual IDataTable Fix(IDataTable table)
        {
            // 去除表名两端的空格
            if (!table.TableName.IsNullOrEmpty()) table.TableName = table.TableName.Trim();
            if (table.Name.IsNullOrEmpty()) table.Name = GetName(table.TableName);
            if (!table.Name.IsNullOrEmpty()) table.Name = table.Name.Trim();

            // 去除字段名两端的空格
            foreach (var item in table.Columns)
            {
                if (!item.Name.IsNullOrEmpty()) item.Name = item.Name.Trim();
                if (!item.ColumnName.IsNullOrEmpty()) item.ColumnName = item.ColumnName.Trim();
            }

            // 最后修复主键
            if (table.PrimaryKeys.Length < 1)
            {
                // 自增作为主键，没办法，如果没有主键，整个实体层都会面临大问题！
                var dc = table.Columns.FirstOrDefault(c => c.Identity);
                if (dc != null) dc.PrimaryKey = true;
            }

            // 从索引中修正主键
            FixPrimaryByIndex(table);

            // 给非主键的自增字段建立唯一索引
            CreateUniqueIndexForIdentity(table);

            // 索引应该具有跟字段一样的唯一和主键约束
            FixIndex(table);

            foreach (var di in table.Indexes)
            {
                di.Fix();
            }

            // 修正可能的主字段
            if (!table.Columns.Any(e => e.Master))
            {
                var f = table.Columns.FirstOrDefault(e => e.Name.EqualIgnoreCase("Name", "Title"));
                if (f != null) f.Master = true;
            }

            return table;
        }

        /// <summary>修正数据列</summary>
        /// <param name="column"></param>
        public virtual IDataColumn Fix(IDataColumn column)
        {
            if (column.Name.IsNullOrEmpty()) column.Name = GetName(column.ColumnName);

            return column;
        }

        /// <summary>从索引中修正主键</summary>
        /// <param name="table"></param>
        protected virtual void FixPrimaryByIndex(IDataTable table)
        {
            var pks = table.PrimaryKeys;
            if (pks == null || pks.Length < 1)
            {
                var dis = table.Indexes;
                // 在索引中找唯一索引作为主键
                var di = dis.FirstOrDefault(e => e.PrimaryKey && e.Columns.Length == 1);
                // 在索引中找唯一索引作为主键
                if (di == null) di = dis.FirstOrDefault(e => e.Unique && e.Columns.Length == 1);
                // 如果还没有主键，把第一个索引作为主键
                //if (di == null) di = dis.FirstOrDefault(e => e.Columns.Length == 1);

                // 从索引修正主键
                if (di == null) di = dis.FirstOrDefault(e => e.PrimaryKey);
                if (di == null) di = dis.FirstOrDefault(e => e.Unique);

                if (di != null)
                {
                    var pks2 = table.GetColumns(di.Columns);
                    if (pks2.Length > 0) Array.ForEach(pks2, dc => dc.PrimaryKey = true);
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
                        //di.Computed = true;
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
            var dis = table.Indexes;
            dis.RemoveAll(di => di.Columns == null || di.Columns.Length == 0);

            foreach (var di in dis)
            {
                if (di.Columns == null) continue;

                var dcs = table.GetColumns(di.Columns);
                if (dcs == null || dcs.Length <= 0) continue;

                if (!di.Unique) di.Unique = dcs.All(dc => dc.Identity);
                // 刚好该索引所有字段都是主键时，修正主键
                if (!di.PrimaryKey) di.PrimaryKey = dcs.All(dc => dc.PrimaryKey) && di.Columns.Length == table.Columns.Count(e => e.PrimaryKey);
            }

            // 干掉无效索引
            dis.RemoveAll(di => di.Columns == null || di.Columns.Length == 0 || di.Columns.Length != table.GetColumns(di.Columns).Length);

            // 干掉自增列的索引
            dis.RemoveAll(di => di.Columns.Length == 1 && table.GetColumn(di.Columns[0]) != null && table.GetColumn(di.Columns[0]).Identity);
        }
        #endregion

        #region 静态实例
        /// <summary>当前名称解析器</summary>
        public static IModelResolver Current { get { return XCodeService.Container.ResolveInstance<IModelResolver>(); } }
        #endregion
    }
}