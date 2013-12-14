using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using NewLife.Xml;

namespace XCode.DataAccessLayer
{
    /// <summary>表模型</summary>
    [DebuggerDisplay("ID={ID} Name={Name} Description={Description}")]
    [Serializable]
    [DisplayName("表模型")]
    [Description("表模型")]
    [XmlRoot("Table")]
    class XTable : IDataTable, ICloneable, IXmlSerializable
    {
        #region 基本属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [XmlAttribute]
        [DisplayName("编号")]
        [Description("编号")]
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private String _Name;
        /// <summary>名称</summary>
        [XmlAttribute]
        [DisplayName("名称")]
        [Description("名称")]
        public String Name { get { return !String.IsNullOrEmpty(_Name) ? _Name : (_Name = ModelResolver.Current.GetName(TableName)); } set { _Name = value; } }

        private String _TableName;
        /// <summary>表名</summary>
        [XmlAttribute]
        [DisplayName("表名")]
        [Description("表名")]
        public String TableName { get { return _TableName; } set { _TableName = value; } }

        private String _DisplayName;
        /// <summary>显示名</summary>
        [XmlAttribute]
        [DisplayName("显示名")]
        [Description("显示名")]
        public String DisplayName
        {
            get
            {
                if (String.IsNullOrEmpty(_DisplayName)) _DisplayName = ModelResolver.Current.GetDisplayName(_Name, _Description);
                return _DisplayName;
            }
            set
            {
                if (!String.IsNullOrEmpty(value)) value = value.Replace("\r\n", "。").Replace("\r", " ").Replace("\n", " ");
                _DisplayName = value;

                if (String.IsNullOrEmpty(_Description))
                    _Description = _DisplayName;
                else if (!_Description.StartsWith(_DisplayName))
                    _Description = _DisplayName + "。" + _Description;
            }
        }

        private String _Description;
        /// <summary>描述</summary>
        [XmlAttribute]
        [DisplayName("描述")]
        [Description("描述")]
        public String Description
        {
            get { return _Description; }
            set
            {
                if (!String.IsNullOrEmpty(value)) value = value.Replace("\r\n", "。").Replace("\r", " ").Replace("\n", " ");
                _Description = value;
            }
        }

        private Boolean _IsView = false;
        /// <summary>是否视图</summary>
        [XmlAttribute]
        [DisplayName("是否视图")]
        [Description("是否视图")]
        public Boolean IsView { get { return _IsView; } set { _IsView = value; } }

        private String _Owner;
        /// <summary>所有者</summary>
        [XmlAttribute]
        [DisplayName("所有者")]
        [Description("所有者")]
        public String Owner { get { return _Owner; } set { _Owner = value; } }

        private DatabaseType _DbType;
        /// <summary>数据库类型</summary>
        [XmlAttribute]
        [DisplayName("数据库类型")]
        [Description("数据库类型")]
        public DatabaseType DbType { get { return _DbType; } set { _DbType = value; } }

        private String _BaseType;
        /// <summary>基类</summary>
        [XmlAttribute]
        [DisplayName("基类")]
        [Description("基类")]
        public String BaseType { get { return _BaseType; } set { _BaseType = value; } }
        #endregion

        #region 扩展属性
        private List<IDataColumn> _Columns;
        /// <summary>字段集合。可以是空集合，但不能为null。</summary>
        [XmlArray("Columns")]
        [Category("集合")]
        [DisplayName("字段集合")]
        [Description("字段集合")]
        public List<IDataColumn> Columns { get { return _Columns ?? (_Columns = new List<IDataColumn>()); } }

        private List<IDataRelation> _Relations;
        /// <summary>关系集合。可以是空集合，但不能为null。</summary>
        [XmlArray]
        [Category("集合")]
        [DisplayName("关系集合")]
        [Description("关系集合")]
        public List<IDataRelation> Relations { get { return _Relations ?? (_Relations = new List<IDataRelation>()); } }

        private List<IDataIndex> _Indexes;
        /// <summary>索引集合。可以是空集合，但不能为null。</summary>
        [XmlArray]
        [Category("集合")]
        [DisplayName("索引集合")]
        [Description("索引集合")]
        public List<IDataIndex> Indexes { get { return _Indexes ?? (_Indexes = new List<IDataIndex>()); } }

        /// <summary>主键集合。可以是空集合，但不能为null。</summary>
        [XmlIgnore]
        public IDataColumn[] PrimaryKeys { get { return Columns.FindAll(item => item.PrimaryKey).ToArray(); } }

        private IDictionary<String, String> _Properties;
        /// <summary>扩展属性</summary>
        [Category("扩展")]
        [DisplayName("扩展属性")]
        [Description("扩展属性")]
        public IDictionary<String, String> Properties { get { return _Properties ?? (_Properties = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase)); } }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        public XTable() { }

        /// <summary>初始化</summary>
        /// <param name="name">名称</param>
        public XTable(String name) { TableName = name; }
        #endregion

        #region 方法
        /// <summary>创建字段</summary>
        /// <returns></returns>
        public virtual IDataColumn CreateColumn()
        {
            var dc = new XField();
            dc.Table = this;
            return dc;
        }

        /// <summary>创建外键</summary>
        /// <returns></returns>
        public virtual IDataRelation CreateRelation()
        {
            var fk = new XRelation();
            fk.Table = this;
            return fk;
        }

        /// <summary>创建索引</summary>
        /// <returns></returns>
        public virtual IDataIndex CreateIndex()
        {
            var idx = new XIndex();
            idx.Table = this;
            return idx;
        }

        /// <summary>根据字段名获取字段</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual IDataColumn GetColumn(String name) { return ModelHelper.GetColumn(this, name); }

        /// <summary>根据字段名数组获取字段数组</summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public virtual IDataColumn[] GetColumns(String[] names) { return ModelHelper.GetColumns(this, names); }

        /// <summary>连接另一个表，处理两表间关系</summary>
        /// <param name="table"></param>
        public virtual IDataTable Connect(IDataTable table) { return ModelResolver.Current.Connect(this, table); }

        /// <summary>修正数据</summary>
        public virtual IDataTable Fix() { return ModelResolver.Current.Fix(this); }

        /// <summary>获取全部字段，包括继承的父类</summary>
        /// <param name="tables">在该表集合里面找父类</param>
        /// <param name="baseFirst">是否父类字段在前</param>
        /// <returns></returns>
        public virtual List<IDataColumn> GetAllColumns(IEnumerable<IDataTable> tables, Boolean baseFirst = true) { return ModelHelper.GetAllColumns(this, tables, baseFirst); }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (String.IsNullOrEmpty(DisplayName))
                return Name;
            else
                return String.Format("{0}({1})", Name, DisplayName);
        }
        #endregion

        #region 导入导出
        /// <summary>导出</summary>
        /// <returns></returns>
        public String Export() { return this.ToXml(); }

        /// <summary>导入</summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static XTable Import(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            return xml.ToXmlEntity<XTable>();
        }
        #endregion

        #region ICloneable 成员
        /// <summary>克隆</summary>
        /// <returns></returns>
        object ICloneable.Clone() { return Clone(); }

        /// <summary>克隆</summary>
        /// <returns></returns>
        public XTable Clone()
        {
            var table = base.MemberwiseClone() as XTable;
            // 浅表克隆后，集合还是指向旧的
            table._Columns = null;
            foreach (var item in Columns)
            {
                table.Columns.Add(item.Clone(table));
            }
            table._Relations = null;
            foreach (var item in Relations)
            {
                table.Relations.Add(item.Clone(table));
            }
            table._Indexes = null;
            foreach (var item in Indexes)
            {
                table.Indexes.Add(item.Clone(table));
            }
            return table;
        }
        #endregion

        #region IXmlSerializable 成员
        /// <summary>获取架构</summary>
        /// <returns></returns>
        XmlSchema IXmlSerializable.GetSchema() { return null; }

        /// <summary>读取</summary>
        /// <param name="reader"></param>
        void IXmlSerializable.ReadXml(XmlReader reader) { ModelHelper.ReadXml(this, reader); }

        /// <summary>写入</summary>
        /// <param name="writer"></param>
        void IXmlSerializable.WriteXml(XmlWriter writer) { ModelHelper.WriteXml(this, writer); }
        #endregion
    }
}