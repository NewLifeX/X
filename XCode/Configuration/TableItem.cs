using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Configuration
{
    /// <summary>数据表元数据</summary>
    public class TableItem
    {
        #region 特性
        private Type _EntityType;
        /// <summary>实体类型</summary>
        public Type EntityType { get { return _EntityType; } }

        /// <summary>绑定表特性</summary>
        private BindTableAttribute _Table;

        /// <summary>绑定索引特性</summary>
        private BindIndexAttribute[] _Indexes;

        /// <summary>绑定关系特性</summary>
        private BindRelationAttribute[] _Relations;

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

        /// <summary>模型检查模式</summary>
        private ModelCheckModeAttribute _ModelCheckMode;
        #endregion

        #region 属性
        private String _TableName;
        /// <summary>表名</summary>
        public String TableName
        {
            get
            {
                if (String.IsNullOrEmpty(_TableName))
                {
                    var table = _Table;
                    var str = table != null ? table.Name : EntityType.Name;
                    var conn = ConnName;

                    if (conn != null && DAL.ConnStrs.ContainsKey(conn))
                    {
                        // 特殊处理Oracle数据库，在表名前加上方案名（用户名）
                        var dal = DAL.Create(conn);
                        if (dal != null && !str.Contains("."))
                        {
                            // 角色名作为点前缀来约束表名，支持所有数据库
                            //if (dal.DbType == DatabaseType.Oracle)
                            {
                                // 加上用户名
                                var ocsb = dal.Db.Factory.CreateConnectionStringBuilder();
                                ocsb.ConnectionString = dal.ConnStr;
                                if (ocsb.ContainsKey("Role")) str = (String)ocsb["Role"] + "." + str;
                            }
                        }
                    }
                    _TableName = str;
                }
                return _TableName;
            }
            set { _TableName = value; DataTable.TableName = value; }
        }

        private String _ConnName;
        /// <summary>连接名</summary>
        public String ConnName
        {
            get
            {
                if (String.IsNullOrEmpty(_ConnName))
                {
                    String connName = null;
                    if (_Table != null) connName = _Table.ConnName;

                    String str = FindConnMap(connName, EntityType.Name);
                    _ConnName = String.IsNullOrEmpty(str) ? connName : str;
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
                    String str = Config.GetMutilConfig<String>(null, "XCode.ConnMaps", "XCodeConnMaps");
                    if (String.IsNullOrEmpty(str)) return _ConnMaps = list;
                    String[] ss = str.Split(",");
                    foreach (String item in ss)
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
        /// <param name="className"></param>
        /// <returns></returns>
        private static String FindConnMap(String connName, String className)
        {
            String name1 = connName + "#";
            String name2 = className + "@";

            foreach (String item in ConnMaps)
            {
                if (item.StartsWith(name1)) return item.Substring(name1.Length);
                if (item.StartsWith(name2)) return item.Substring(name2.Length);
            }
            return null;
        }
        #endregion

        #region 扩展属性
        private FieldItem[] _Fields;
        /// <summary>数据字段</summary>
        [XmlArray]
        [Description("数据字段")]
        public FieldItem[] Fields { get { return _Fields; } }

        private FieldItem[] _AllFields;
        /// <summary>所有字段</summary>
        [XmlIgnore]
        public FieldItem[] AllFields { get { return _AllFields; } }

        private FieldItem _Identity;
        /// <summary>标识列</summary>
        [XmlIgnore]
        public FieldItem Identity { get { return _Identity; } }

        private FieldItem[] _PrimaryKeys;
        /// <summary>主键</summary>
        [XmlIgnore]
        public FieldItem[] PrimaryKeys { get { return _PrimaryKeys; } }

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

        private IDataTable _DataTable;
        /// <summary>数据表架构</summary>
        [XmlIgnore]
        public IDataTable DataTable { get { return _DataTable; } }

        /// <summary>模型检查模式</summary>
        public ModelCheckModes ModelCheckMode { get { return _ModelCheckMode != null ? _ModelCheckMode.Mode : ModelCheckModes.CheckAllTablesWhenInit; } }
        #endregion

        #region 构造
        private TableItem(Type type)
        {
            _EntityType = type;
            _Table = type.GetCustomAttribute<BindTableAttribute>(true);
            if (_Table == null) throw new ArgumentOutOfRangeException("type", "类型" + type + "没有" + typeof(BindTableAttribute).Name + "特性！");

            _Indexes = type.GetCustomAttributes<BindIndexAttribute>(true);
            _Relations = type.GetCustomAttributes<BindRelationAttribute>(true);
            _Description = type.GetCustomAttribute<DescriptionAttribute>(true);
            _ModelCheckMode = type.GetCustomAttribute<ModelCheckModeAttribute>(true);

            InitFields();
        }

        static DictionaryCache<Type, TableItem> cache = new DictionaryCache<Type, TableItem>();
        /// <summary>创建</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static TableItem Create(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            // 不能给没有BindTableAttribute特性的类型创建TableItem，否则可能会在InitFields中抛出异常
            return cache.GetItem(type, key => key.GetCustomAttribute<BindTableAttribute>(true) != null ? new TableItem(key) : null);
        }

        void InitFields()
        {
            var bt = _Table;
            var table = DAL.CreateTable();
            _DataTable = table;
            table.TableName = bt.Name;
            table.Name = EntityType.Name;
            table.DbType = bt.DbType;
            table.IsView = bt.IsView;
            table.Description = Description;

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
                if (fi.IsIdentity) _Identity = fi;
            }
            if (_Indexes != null && _Indexes.Length > 0)
            {
                foreach (var item in _Indexes)
                {
                    var di = table.CreateIndex();
                    item.Fill(di);

                    if (ModelHelper.GetIndex(table, di.Columns) != null) continue;

                    // 如果索引全部就是主键，无需创建索引
                    if (table.GetColumns(di.Columns).All(e => e.PrimaryKey)) continue;

                    table.Indexes.Add(di);
                }
            }
            if (_Relations != null && _Relations.Length > 0)
            {
                foreach (var item in _Relations)
                {
                    var dr = table.CreateRelation();
                    item.Fill(dr);

                    if (table.GetRelation(dr) == null) table.Relations.Add(dr);
                }
            }

            // 不允许为null
            _AllFields = allfields.ToArray();
            _Fields = fields.ToArray();
            _PrimaryKeys = pkeys.ToArray();
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
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var item = list[i];
                        if (item.DeclaringType == t && !item.IsDataObjectField)
                        {
                            stack.Push(item);
                            list.RemoveAt(i);
                        }
                    }
                    // 有数据属性的
                    for (int i = list.Count - 1; i >= 0; i--)
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
        /// <summary>根据名称查找</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public Field FindByName(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            foreach (var item in Fields)
            {
                if (item.Name.EqualIgnoreCase(name)) return item as Field;
            }

            foreach (var item in Fields)
            {
                if (item.ColumnName.EqualIgnoreCase(name)) return item as Field;
            }

            return null;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (String.IsNullOrEmpty(Description))
                return TableName;
            else
                return String.Format("{0}（{1}）", TableName, Description);
        }
        #endregion
    }
}