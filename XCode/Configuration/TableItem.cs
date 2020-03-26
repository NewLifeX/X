using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Collections;
using XCode.DataAccessLayer;

namespace XCode.Configuration
{
    /// <summary>数据表元数据</summary>
    public class TableItem
    {
        #region 特性
        /// <summary>实体类型</summary>
        public Type EntityType { get; }

        /// <summary>绑定表特性</summary>
        private BindTableAttribute _Table;

        /// <summary>绑定索引特性</summary>
        private BindIndexAttribute[] _Indexes;

        private DescriptionAttribute _Description;
        /// <summary>说明</summary>
        public String Description
        {
            get
            {
                if (_Description != null && !String.IsNullOrEmpty(_Description.Description)) return _Description.Description;
                if (_Table != null && !String.IsNullOrEmpty(_Table.Description)) return _Table.Description;

                return null;
            }
        }
        #endregion

        #region 属性
        private String _TableName;
        /// <summary>表名</summary>
        public String TableName
        {
            get
            {
                //if (_TableName.IsNullOrEmpty()) _TableName = GetTableName(_Table);
                if (_TableName.IsNullOrEmpty()) _TableName = _Table?.Name ?? EntityType.Name;

                return _TableName;
            }
            set { _TableName = value; DataTable.TableName = value; }
        }

        //private String GetTableName(BindTableAttribute table)
        //{
        //    var name = table != null ? table.Name : EntityType.Name;

        //    // 检查自动表前缀
        //    var dal = DAL.Create(ConnName);
        //    var pf = dal.Db.TablePrefix;
        //    if (!pf.IsNullOrEmpty() && !name.StartsWithIgnoreCase(pf)) name = pf + name;

        //    return name;
        //}

        private String _ConnName;
        /// <summary>连接名</summary>
        public String ConnName
        {
            get
            {
                if (_ConnName.IsNullOrEmpty())
                {
                    var connName = _Table?.ConnName;

                    var str = FindConnMap(connName, EntityType);
                    if (!str.IsNullOrEmpty())
                    {
                        DAL.WriteLog($"实体 {EntityType.FullName}/{connName} 映射到 {str}");

                        connName = str;
                    }

                    _ConnName = connName;
                }
                return _ConnName;
            }
            set { _ConnName = value; }
        }

        private static List<String> _ConnMaps;
        /// <summary>连接名映射</summary>
        private static List<String> ConnMaps
        {
            get
            {
                // 加锁，并且先实例化本地变量，最后再赋值，避免返回空集合
                // 原来的写法存在线程冲突，可能第一个线程实例化列表后，还来不及填充，后续线程就已经把集合拿走
                if (_ConnMaps != null) return _ConnMaps;
                lock (typeof(TableItem))
                {
                    if (_ConnMaps != null) return _ConnMaps;

                    var list = new List<String>();
                    var str = Setting.Current.ConnMaps;
                    if (String.IsNullOrEmpty(str)) return _ConnMaps = list;

                    var ss = str.Split(",");
                    foreach (var item in ss)
                    {
                        if (list.Contains(item.Trim())) continue;

                        if (item.Contains("#") && !item.EndsWith("#") ||
                            item.Contains("@") && !item.EndsWith("@")) list.Add(item.Trim());
                    }
                    return _ConnMaps = list;
                }
            }
        }

        /// <summary>根据连接名和类名查找连接名映射</summary>
        /// <param name="connName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static String FindConnMap(String connName, Type type)
        {
            var name1 = connName + "#";
            var name2 = type.FullName + "@";
            var name3 = type.Name + "@";

            foreach (var item in ConnMaps)
            {
                if (item.StartsWith(name1)) return item.Substring(name1.Length);
                if (item.StartsWith(name2)) return item.Substring(name2.Length);
                if (item.StartsWith(name3)) return item.Substring(name3.Length);
            }
            return null;
        }
        #endregion

        #region 扩展属性
        /// <summary>数据字段</summary>
        [XmlArray]
        [Description("数据字段")]
        public FieldItem[] Fields { get; private set; }

        /// <summary>所有字段</summary>
        [XmlIgnore]
        public FieldItem[] AllFields { get; private set; }

        /// <summary>标识列</summary>
        [XmlIgnore]
        public FieldItem Identity { get; private set; }

        /// <summary>主键。不会返回null</summary>
        [XmlIgnore]
        public FieldItem[] PrimaryKeys { get; private set; }

        /// <summary>主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
        public FieldItem Master { get; private set; }

        private ICollection<String> _FieldNames;
        /// <summary>字段名集合，不区分大小写的哈希表存储，外部不要修改元素数据</summary>
        [XmlIgnore]
        public ICollection<String> FieldNames
        {
            get
            {
                if (_FieldNames != null) return _FieldNames;

                var list = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in Fields)
                {
                    if (!list.Contains(item.Name))
                    {
                        list.Add(item.Name);
                        dic.Add(item.Name, item.Name);
                    }
                    else
                        DAL.WriteLog("数据表{0}发现同名但不同大小写的字段{1}和{2}，违反设计原则！", TableName, dic[item.Name], item.Name);
                }
                //_FieldNames = new ReadOnlyCollection<String>(list);
                _FieldNames = list;

                return _FieldNames;
            }
        }

        private ICollection<String> _ExtendFieldNames;
        /// <summary>扩展属性集合，不区分大小写的哈希表存储，外部不要修改元素数据</summary>
        [XmlIgnore]
        public ICollection<String> ExtendFieldNames
        {
            get
            {
                if (_ExtendFieldNames != null) return _ExtendFieldNames;

                var list = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in AllFields)
                {
                    if (!item.IsDataObjectField && !list.Contains(item.Name)) list.Add(item.Name);
                }
                _ExtendFieldNames = list;

                return _ExtendFieldNames;
            }
        }

        /// <summary>数据表架构</summary>
        [XmlIgnore]
        public IDataTable DataTable { get; private set; }

        /// <summary>模型检查模式</summary>
        public ModelCheckModes ModelCheckMode { get; } = ModelCheckModes.CheckAllTablesWhenInit;
        #endregion

        #region 构造
        private TableItem(Type type)
        {
            EntityType = type;
            _Table = type.GetCustomAttribute<BindTableAttribute>(true);
            if (_Table == null) throw new ArgumentOutOfRangeException("type", "类型" + type + "没有" + typeof(BindTableAttribute).Name + "特性！");

            _Indexes = type.GetCustomAttributes<BindIndexAttribute>(true).ToArray();
            //_Relations = type.GetCustomAttributes<BindRelationAttribute>(true).ToArray();
            _Description = type.GetCustomAttribute<DescriptionAttribute>(true);
            var att = type.GetCustomAttribute<ModelCheckModeAttribute>(true);
            if (att != null) ModelCheckMode = att.Mode;

            InitFields();
        }

        static ConcurrentDictionary<Type, TableItem> cache = new ConcurrentDictionary<Type, TableItem>();
        /// <summary>创建</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static TableItem Create(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            // 不能给没有BindTableAttribute特性的类型创建TableItem，否则可能会在InitFields中抛出异常
            return cache.GetOrAdd(type, key => key.GetCustomAttribute<BindTableAttribute>(true) != null ? new TableItem(key) : null);
        }

        void InitFields()
        {
            var bt = _Table;
            var table = DAL.CreateTable();
            DataTable = table;
            table.TableName = bt.Name;
            //// 构建DataTable时也要注意表前缀，避免反向工程用错
            //table.TableName = GetTableName(bt);
            table.Name = EntityType.Name;
            table.DbType = bt.DbType;
            table.IsView = bt.IsView;
            table.Description = Description;
            //table.ConnName = ConnName;

            var allfields = new List<FieldItem>();
            var fields = new List<FieldItem>();
            var pkeys = new List<FieldItem>();
            foreach (var item in GetFields(EntityType))
            {
                var fi = item;
                allfields.Add(fi);

                if (fi.IsDataObjectField)
                {
                    fields.Add(fi);

                    var f = table.CreateColumn();
                    fi.Fill(f);

                    table.Columns.Add(f);
                }

                if (fi.PrimaryKey) pkeys.Add(fi);
                if (fi.IsIdentity) Identity = fi;
                if (fi.Master) Master = fi;
            }
            // 先完成allfields才能专门处理
            foreach (var item in allfields)
            {
                if (!item.IsDynamic)
                {
                    // 如果不是数据字段，则检查绑定关系
                    var map = item.Map;
                    if (map != null)
                    {
                        // 找到被关系映射的字段，拷贝相关属性
                        var fi = allfields.FirstOrDefault(e => e.Name.EqualIgnoreCase(map.Name));
                        if (fi != null)
                        {
                            if (item.OriField == null) item.OriField = fi;
                            if (item.DisplayName.IsNullOrEmpty()) item.DisplayName = fi.DisplayName;
                            if (item.Description.IsNullOrEmpty()) item.Description = fi.Description;
                            item.ColumnName = fi.ColumnName;
                        }
                    }
                }
            }
            if (_Indexes != null && _Indexes.Length > 0)
            {
                foreach (var item in _Indexes)
                {
                    var di = table.CreateIndex();
                    item.Fill(di);

                    if (table.GetIndex(di.Columns) != null) continue;

                    // 如果索引全部就是主键，无需创建索引
                    if (table.GetColumns(di.Columns).All(e => e.PrimaryKey)) continue;

                    table.Indexes.Add(di);
                }
            }

            // 不允许为null
            AllFields = allfields.ToArray();
            Fields = fields.ToArray();
            PrimaryKeys = pkeys.ToArray();
        }

        /// <summary>获取属性，保证基类属性在前</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        IEnumerable<Field> GetFields(Type type)
        {
            // 先拿到所有属性，可能是先排子类，再排父类
            var list = new List<Field>();
            foreach (var item in type.GetProperties())
            {
                if (item.GetIndexParameters().Length <= 0) list.Add(new Field(this, item));
            }

            var att = type.GetCustomAttribute<ModelSortModeAttribute>(true);
            if (att == null || att.Mode == ModelSortModes.BaseFirst)
            {
                // 然后用栈来处理，基类优先
                var stack = new Stack<Field>();
                var t = type;
                while (t != null && t != typeof(EntityBase) && list.Count > 0)
                {
                    // 反序入栈，因为属性可能是顺序的，这里先反序，待会出来再反一次
                    // 没有数据属性的
                    for (var i = list.Count - 1; i >= 0; i--)
                    {
                        var item = list[i];
                        if (item.DeclaringType == t && !item.IsDataObjectField)
                        {
                            stack.Push(item);
                            list.RemoveAt(i);
                        }
                    }
                    // 有数据属性的
                    for (var i = list.Count - 1; i >= 0; i--)
                    {
                        var item = list[i];
                        if (item.DeclaringType == t && item.IsDataObjectField)
                        {
                            stack.Push(item);
                            list.RemoveAt(i);
                        }
                    }
                    t = t.BaseType;
                }
                foreach (var item in stack)
                {
                    yield return item;
                }
            }
            else
            {
                // 子类优先
                var t = type;
                while (t != null && t != typeof(EntityBase) && list.Count > 0)
                {
                    // 有数据属性的
                    foreach (var item in list)
                    {
                        if (item.DeclaringType == t && item.IsDataObjectField) yield return item;
                    }
                    // 没有数据属性的
                    foreach (var item in list)
                    {
                        if (item.DeclaringType == t && !item.IsDataObjectField) yield return item;
                    }
                    t = t.BaseType;
                }
            }
        }
        #endregion

        #region 方法
        private Dictionary<String, Field> _all;

        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public Field FindByName(String name)
        {
            //if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if (name.IsNullOrEmpty()) return null;
            // 特殊处理行号
            if (name.EqualIgnoreCase("RowNumber")) return null;

            // 借助字典，快速搜索数据列
            if (_all == null)
            {
                var dic = new Dictionary<String, Field>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in Fields)
                {
                    if (!dic.ContainsKey(item.Name))
                        dic.Add(item.Name, item as Field);
                }
                foreach (var item in AllFields)
                {
                    if (!dic.ContainsKey(item.Name))
                        dic.Add(item.Name, item as Field);
                    else if (!item.ColumnName.IsNullOrEmpty() && !dic.ContainsKey(item.ColumnName))
                        dic.Add(item.ColumnName, item as Field);
                }

                // 宁可重复计算，也要避免锁
                _all = dic;
            }
            if (_all.TryGetValue(name, out var f)) return f;

            foreach (var item in Fields)
            {
                if (item.Name.EqualIgnoreCase(name)) return item as Field;
            }

            foreach (var item in Fields)
            {
                if (item.ColumnName.EqualIgnoreCase(name)) return item as Field;
            }

            foreach (var item in AllFields)
            {
                if (item.Name.EqualIgnoreCase(name)) return item as Field;
            }

            return null;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (String.IsNullOrEmpty(Description))
                return TableName;
            else
                return String.Format("{0}（{1}）", TableName, Description);
        }
        #endregion

        #region 动态增加字段
        /// <summary>动态增加字段</summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="description"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public TableItem Add(String name, Type type, String description = null, Int32 length = 0)
        {
            var f = new Field(this, name, type, description, length);

            var list = new List<FieldItem>(Fields) { f };
            Fields = list.ToArray();

            list = new List<FieldItem>(AllFields) { f };
            AllFields = list.ToArray();

            var dc = DataTable.CreateColumn();
            f.Fill(dc);

            DataTable.Columns.Add(dc);

            return this;
        }
        #endregion
    }
}