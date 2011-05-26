using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Configuration
{
    /// <summary>
    /// 表元数据
    /// </summary>
    public class TableItem
    {
        #region 特性
        private Type _EntityType;
        /// <summary>实体类型</summary>
        public Type EntityType
        {
            get { return _EntityType; }
        }

        private BindTableAttribute _Table;
        /// <summary>绑定表特性</summary>
        public BindTableAttribute Table { get { return _Table; } }

        private DescriptionAttribute _Description;
        /// <summary>说明</summary>
        public String Description
        {
            get
            {
                if (_Description != null && !String.IsNullOrEmpty(_Description.Description)) return _Description.Description;
                if (Table != null && !String.IsNullOrEmpty(Table.Description)) return Table.Description;

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
                if (String.IsNullOrEmpty(_TableName))
                {
                    BindTableAttribute table = Table;
                    String str;
                    if (table != null)
                        str = table.Name;
                    else
                        str = EntityType.Name;

                    // 特殊处理Oracle数据库，在表名前加上方案名（用户名）
                    DAL dal = DAL.Create(ConnName);
                    if (dal != null && !str.Contains("."))
                    {
                        if (dal.DbType == DatabaseType.Oracle)
                        {
                            // 加上用户名
                            String UserID = (dal.Db as Oracle).UserID;
                            if (!String.IsNullOrEmpty(UserID)) str = UserID + "." + str;
                        }
                    }
                    _TableName = str;
                }
                return _TableName;
            }
            //set { _TableName = value; }
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
                    if (Table != null) connName = Table.ConnName;

                    String str = FindConnMap(connName, EntityType.Name);
                    _ConnName = String.IsNullOrEmpty(str) ? connName : str;
                }
                return _ConnName;
            }
            //set { _ConnName = value; }
        }

        private static List<String> _ConnMaps;
        /// <summary>
        /// 连接名映射
        /// </summary>
        private static List<String> ConnMaps
        {
            get
            {
                if (_ConnMaps != null) return _ConnMaps;

                _ConnMaps = new List<String>();
                String str = Config.GetConfig<String>("XCode.ConnMaps", Config.GetConfig<String>("XCodeConnMaps"));
                if (String.IsNullOrEmpty(str)) return _ConnMaps;
                String[] ss = str.Split(',');
                foreach (String item in ss)
                {
                    if (item.Contains("#") && !item.EndsWith("#") ||
                        item.Contains("@") && !item.EndsWith("@")) _ConnMaps.Add(item.Trim());
                }
                return _ConnMaps;
            }
        }

        /// <summary>
        /// 根据连接名和类名查找连接名映射
        /// </summary>
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
        public FieldItem[] Fields
        {
            get { InitFields(); return _Fields; }
        }

        private FieldItem[] _AllFields;
        /// <summary>所有字段</summary>
        [XmlIgnore]
        public FieldItem[] AllFields
        {
            get { InitFields(); return _AllFields; }
        }

        private FieldItem _Identity;
        /// <summary>标识列</summary>
        [XmlIgnore]
        public FieldItem Identity
        {
            get { InitFields(); return _Identity; }
        }

        private FieldItem[] _PrimaryKeys;
        /// <summary>主键</summary>
        [XmlIgnore]
        public FieldItem[] PrimaryKeys
        {
            get { InitFields(); return _PrimaryKeys; }
        }

        Boolean hasInitFields = false;
        void InitFields()
        {
            if (hasInitFields) return;
            hasInitFields = true;

            List<FieldItem> list1 = new List<FieldItem>();
            List<FieldItem> list2 = new List<FieldItem>();
            List<FieldItem> list3 = new List<FieldItem>();
            PropertyInfo[] pis = EntityType.GetProperties();
            foreach (PropertyInfo item in pis)
            {
                // 排除索引器
                if (item.GetIndexParameters().Length > 0) continue;

                FieldItem fi = new FieldItem(item);
                list1.Add(fi);

                if (fi.DataObjectField != null) list2.Add(fi);
                if (fi.PrimaryKey) list3.Add(fi);
                if (fi.IsIdentity) _Identity = fi;
            }
            if (list1 != null && list1.Count > 0) _AllFields = list1.ToArray();
            if (list2 != null && list2.Count > 0) _Fields = list2.ToArray();
            if (list3 != null && list3.Count > 0) _PrimaryKeys = list3.ToArray();
        }

        private IList<String> _FieldNames;
        /// <summary>字段名集合</summary>
        [XmlIgnore]
        public IList<String> FieldNames
        {
            get
            {
                if (_FieldNames != null) return _FieldNames;

                List<String> list = new List<String>();
                foreach (FieldItem item in Fields)
                {
                    if (!list.Contains(item.Name)) list.Add(item.Name);
                }
                _FieldNames = new ReadOnlyCollection<String>(list);

                return _FieldNames;
            }
        }

        private XTable _XTable;
        /// <summary>数据表架构</summary>
        [XmlIgnore]
        public XTable XTable
        {
            get
            {
                if (_XTable == null)
                {
                    BindTableAttribute bt = Table;
                    XTable table = new XTable();
                    table.Name = bt.Name;
                    table.DbType = bt.DbType;
                    table.Description = bt.Description;

                    table.Fields = new List<XField>();
                    foreach (FieldItem fi in Fields)
                    {
                        XField f = table.CreateField();
                        fi.Fill(f);

                        table.Fields.Add(f);
                    }

                    _XTable = table;
                }
                return _XTable;
            }
        }
        #endregion

        #region 构造
        private TableItem(Type type)
        {
            _EntityType = type;
            _Table = BindTableAttribute.GetCustomAttribute(EntityType);
            _Description = DescriptionAttribute.GetCustomAttribute(EntityType, typeof(DescriptionAttribute)) as DescriptionAttribute;
        }

        static DictionaryCache<Type, TableItem> cache = new DictionaryCache<Type, TableItem>();
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TableItem Create(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (BindTableAttribute.GetCustomAttribute(type) == null)
                throw new ArgumentOutOfRangeException("type", "类型" + type + "没有" + typeof(BindTableAttribute).Name + "特性！");

            return cache.GetItem(type, key => new TableItem(key));
        }
        #endregion

        #region 方法
        /// <summary>
        /// 根据名称查找
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FieldItem FindByName(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            foreach (FieldItem item in Fields)
            {
                if (String.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)) return item;
            }

            foreach (FieldItem item in Fields)
            {
                if (String.Equals(item.ColumnName, name, StringComparison.OrdinalIgnoreCase)) return item;
            }

            return null;
        }
        #endregion
    }
}