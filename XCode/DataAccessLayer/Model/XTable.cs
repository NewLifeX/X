using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using NewLife.Collections;
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
        /// <summary>编号</summary>
        [XmlAttribute]
        [DisplayName("编号")]
        [Description("编号")]
        public Int32 ID { get; set; }

        /// <summary>名称</summary>
        [XmlAttribute]
        [DisplayName("名称")]
        [Description("名称")]
        public String Name { get; set; }

        /// <summary>表名</summary>
        [XmlAttribute]
        [DisplayName("表名")]
        [Description("表名")]
        public String TableName { get; set; }

        private String _DisplayName;
        /// <summary>显示名</summary>
        [XmlAttribute]
        [DisplayName("显示名")]
        [Description("显示名")]
        public String DisplayName
        {
            get
            {
                if (String.IsNullOrEmpty(_DisplayName)) _DisplayName = ModelResolver.Current.GetDisplayName(Name, _Description);
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

        /// <summary>是否视图</summary>
        [XmlAttribute]
        [DisplayName("是否视图")]
        [Description("是否视图")]
        public Boolean IsView { get; set; }

        /// <summary>所有者</summary>
        [XmlAttribute]
        [DisplayName("所有者")]
        [Description("所有者")]
        public String Owner { get; set; }

        /// <summary>数据库类型</summary>
        [XmlAttribute]
        [DisplayName("数据库类型")]
        [Description("数据库类型")]
        public DatabaseType DbType { get; set; }

        /// <summary>基类</summary>
        [XmlAttribute]
        [DisplayName("基类")]
        [Description("基类")]
        public String BaseType { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>字段集合。可以是空集合，但不能为null。</summary>
        [XmlIgnore]
        [Category("集合")]
        [DisplayName("字段集合")]
        [Description("字段集合")]
        public List<IDataColumn> Columns { get; private set; }

        /// <summary>关系集合。可以是空集合，但不能为null。</summary>
        [XmlIgnore]
        [Category("集合")]
        [DisplayName("关系集合")]
        [Description("关系集合")]
        public List<IDataRelation> Relations { get; private set; }

        /// <summary>索引集合。可以是空集合，但不能为null。</summary>
        [XmlIgnore]
        [Category("集合")]
        [DisplayName("索引集合")]
        [Description("索引集合")]
        public List<IDataIndex> Indexes { get; private set; }

        /// <summary>主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
        [XmlIgnore]
        public IDataColumn Master { get { return Columns.FirstOrDefault(e => e.Master) ?? Columns.FirstOrDefault(e => e.PrimaryKey); } }

        /// <summary>主键集合。可以是空集合，但不能为null。</summary>
        [XmlIgnore]
        public IDataColumn[] PrimaryKeys { get { return Columns.FindAll(item => item.PrimaryKey).ToArray(); } }

        /// <summary>扩展属性</summary>
        [XmlIgnore]
        [Category("扩展")]
        [DisplayName("扩展属性")]
        [Description("扩展属性")]
        public IDictionary<String, String> Properties { get; private set; }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        public XTable()
        {
            IsView = false;

            Columns = new List<IDataColumn>();
            Relations = new List<IDataRelation>();
            Indexes = new List<IDataIndex>();

            Properties = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>初始化</summary>
        /// <param name="name">名称</param>
        public XTable(String name)
            : this()
        {
            TableName = name;
        }
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
            // 浅表克隆后，集合还是指向旧的，需要重新创建
            table.Columns = new List<IDataColumn>();
            foreach (var item in Columns)
            {
                table.Columns.Add(item.Clone(table));
            }
            table.Relations = new List<IDataRelation>();
            foreach (var item in Relations)
            {
                table.Relations.Add(item.Clone(table));
            }
            table.Indexes = new List<IDataIndex>();
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