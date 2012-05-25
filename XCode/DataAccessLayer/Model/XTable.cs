using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using XCode.DataAccessLayer.Model;

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
        /// <summary>表名</summary>
        [XmlAttribute]
        [DisplayName("表名")]
        [Description("表名")]
        public String Name { get { return _Name; } set { _Name = value; _Alias = null; } }

        private String _Alias;
        /// <summary>别名</summary>
        [XmlAttribute]
        [DisplayName("别名")]
        [Description("别名")]
        public String Alias { get { return !String.IsNullOrEmpty(_Alias) ? _Alias : (_Alias = ModelResolver.Current.GetAlias(Name)); } set { _Alias = value; } }

        private String _Description;
        /// <summary>表说明</summary>
        [XmlAttribute]
        [DisplayName("表说明")]
        [Description("表说明")]
        public String Description
        {
            get { return _Description; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    value = value.Replace("\r\n", "。")
                        .Replace("\r", " ")
                        .Replace("\n", " ");
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
        public IDataColumn[] PrimaryKeys
        {
            get
            {
                //List<IDataColumn> list = Columns.FindAll(item => item.PrimaryKey);
                //return list == null || list.Count < 1 ? new IDataColumn[0] : list.ToArray();

                return Columns.FindAll(item => item.PrimaryKey).ToArray();
            }
        }

        /// <summary>显示名。如果有Description则使用Description，否则使用Name</summary>
        [XmlIgnore]
        public String DisplayName { get { return ModelResolver.Current.GetDisplayName(Alias ?? Name, Description); } }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        public XTable() { }

        /// <summary>初始化</summary>
        /// <param name="name"></param>
        public XTable(String name) { Name = name; }
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
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual IDataColumn GetColumn(String name)
        {
            return ModelHelper.GetColumn(this, name);
        }

        /// <summary>根据字段名数组获取字段数组</summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public virtual IDataColumn[] GetColumns(String[] names)
        {
            return ModelHelper.GetColumns(this, names);
        }

        /// <summary>连接另一个表，处理两表间关系</summary>
        /// <param name="table"></param>
        public virtual IDataTable Connect(IDataTable table)
        {
            return ModelResolver.Current.Connect(this, table);
        }

        /// <summary>修正数据</summary>
        public virtual IDataTable Fix()
        {
            return ModelResolver.Current.Fix(this);
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!String.IsNullOrEmpty(Description))
                return String.Format("{0}({1})", Name, Description);
            else
                return Name;
        }
        #endregion

        #region 导入导出
        /// <summary>导出</summary>
        /// <returns></returns>
        public String Export()
        {
            var serializer = new XmlSerializer(this.GetType());
            using (var sw = new StringWriter())
            {
                serializer.Serialize(sw, this);
                return sw.ToString();
            }
        }

        /// <summary>导入</summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static XTable Import(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            var serializer = new XmlSerializer(typeof(XTable));
            using (var sr = new StringReader(xml))
            {
                return serializer.Deserialize(sr) as XTable;
            }
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
        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() { return null; }

        /// <summary>读取</summary>
        /// <param name="reader"></param>
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            //reader.ReadStartElement();

            ModelHelper.ReadXml(this, reader);
        }

        /// <summary>写入</summary>
        /// <param name="writer"></param>
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            //writer.WriteStartElement("Table");

            ModelHelper.WriteXml(this, writer);

            //writer.WriteEndElement();
        }
        #endregion
    }
}