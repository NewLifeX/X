using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 表模型
    /// </summary>
    [DebuggerDisplay("ID={ID} Name={Name} Description={Description}")]
    [Serializable]
    [DisplayName("表模型")]
    [Description("表模型")]
    [XmlRoot("Table")]
    class XTable : IDataTable, ICloneable, IXmlSerializable
    {
        #region 基本属性
        private Int32 _ID;
        /// <summary>
        /// 编号
        /// </summary>
        [XmlAttribute]
        [DisplayName("编号")]
        [Description("编号")]
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private String _Name;
        /// <summary>
        /// 表名
        /// </summary>
        [XmlAttribute]
        [DisplayName("表名")]
        [Description("表名")]
        public String Name { get { return _Name; } set { _Name = value; _Alias = null; } }

        private String _Alias;
        /// <summary>
        /// 别名
        /// </summary>
        [XmlAttribute]
        [DisplayName("别名")]
        [Description("别名")]
        public String Alias { get { return _Alias ?? (_Alias = ModelHelper.GetAlias(Name)); } set { _Alias = value; } }

        private String _Description;
        /// <summary>
        /// 表说明
        /// </summary>
        [XmlAttribute]
        [DisplayName("表说明")]
        [Description("表说明")]
        public String Description { get { return _Description; } set { _Description = value; } }

        private Boolean _IsView = false;
        /// <summary>
        /// 是否视图
        /// </summary>
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
        /// <summary>
        /// 字段集合。
        /// </summary>
        [XmlArray("Columns")]
        [Category("集合")]
        [DisplayName("字段集合")]
        [Description("字段集合")]
        public List<IDataColumn> Columns { get { return _Columns ?? (_Columns = new List<IDataColumn>()); } }

        private List<IDataRelation> _Relations;
        /// <summary>
        /// 关系集合。
        /// </summary>
        [XmlArray]
        [Category("集合")]
        [DisplayName("关系集合")]
        [Description("关系集合")]
        public List<IDataRelation> Relations { get { return _Relations ?? (_Relations = new List<IDataRelation>()); } }

        private List<IDataIndex> _Indexes;
        /// <summary>
        /// 索引集合。
        /// </summary>
        [XmlArray]
        [Category("集合")]
        [DisplayName("索引集合")]
        [Description("索引集合")]
        public List<IDataIndex> Indexes { get { return _Indexes ?? (_Indexes = new List<IDataIndex>()); } }

        //private IDataColumn[] _PrimaryKeys;
        /// <summary>主键集合</summary>
        [XmlIgnore]
        public IDataColumn[] PrimaryKeys
        {
            get
            {
                List<IDataColumn> list = Columns.FindAll(item => item.PrimaryKey);
                return list == null || list.Count < 1 ? null : list.ToArray();
            }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        public XTable() { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="name"></param>
        public XTable(String name) { Name = name; }
        #endregion

        #region 方法
        /// <summary>
        /// 创建字段
        /// </summary>
        /// <returns></returns>
        public virtual IDataColumn CreateColumn()
        {
            return XField.Create(this);
        }

        /// <summary>
        /// 创建外键
        /// </summary>
        /// <returns></returns>
        public virtual IDataRelation CreateRelation()
        {
            XRelation fk = new XRelation();
            fk.Table = this;
            return fk;
        }

        /// <summary>
        /// 创建索引
        /// </summary>
        /// <returns></returns>
        public virtual IDataIndex CreateIndex()
        {
            XIndex idx = new XIndex();
            idx.Table = this;
            return idx;
        }

        /// <summary>
        /// 根据字段名获取字段
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual IDataColumn GetColumn(String name)
        {
            return ModelHelper.GetColumn(this, name);
        }

        /// <summary>
        /// 根据字段名数组获取字段数组
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public virtual IDataColumn[] GetColumns(String[] names)
        {
            return ModelHelper.GetColumns(this, names);
        }

        /// <summary>
        /// 连接另一个表，处理两表间关系
        /// </summary>
        /// <param name="table"></param>
        public virtual void Connect(IDataTable table)
        {
            ModelHelper.Connect(this, table);
        }

        /// <summary>
        /// 修正数据
        /// </summary>
        public virtual void Fix()
        {
            ModelHelper.Fix(this);
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!String.IsNullOrEmpty(Description))
                return String.Format("{0}({1})", Description, Name);
            else
                return Name;
        }
        #endregion

        #region 静态方法
        #endregion

        #region 导入导出
        /// <summary>
        /// 导出
        /// </summary>
        /// <returns></returns>
        public String Export()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XTable));
            using (StringWriter sw = new StringWriter())
            {
                serializer.Serialize(sw, this);
                return sw.ToString();
            }
        }

        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static XTable Import(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            XmlSerializer serializer = new XmlSerializer(typeof(XTable));
            using (StringReader sr = new StringReader(xml))
            {
                return serializer.Deserialize(sr) as XTable;
            }
        }
        #endregion

        #region ICloneable 成员
        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public XTable Clone()
        {
            XTable table = base.MemberwiseClone() as XTable;
            //if (table != null)
            //{
            //    if (Columns != null)
            //    {
            //        foreach (IDataColumn item in Columns)
            //        {
            //            table.Columns.Add(item.Clone(table));
            //        }
            //    }
            //}
            return table;
        }
        #endregion

        #region IAccessor 成员
        //bool IAccessor.Read(IReader reader)
        //{
        //    //XmlReaderX xr = reader as XmlReaderX;
        //    //if (xr != null)
        //    //{
        //    //    xr.Settings.MemberAsAttribute = true;
        //    //}

        //    return false;
        //}

        //bool IAccessor.ReadComplete(IReader reader, bool success)
        //{
        //    return success;
        //}

        //bool IAccessor.Write(IWriter writer)
        //{
        //    //XmlWriterX xw = writer as XmlWriterX;
        //    //if (xw != null)
        //    //{
        //    //    xw.Settings.MemberAsAttribute = true;
        //    //}

        //    writer.OnMemberWriting += new EventHandler<WriteMemberEventArgs>(writer_OnMemberWriting);

        //    return false;
        //}

        //void writer_OnMemberWriting(object sender, WriteMemberEventArgs e)
        //{
        //    //if (e.Member.Type == typeof(IDataColumn[]))
        //    //{
        //    //    e.Member.Name = "";
        //    //}
        //}

        //bool IAccessor.WriteComplete(IWriter writer, bool success)
        //{
        //    return success;
        //}
        #endregion

        #region IXmlSerializable 成员
        /// <summary>
        /// 获取架构
        /// </summary>
        /// <returns></returns>
        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        /// <summary>
        /// 读取
        /// </summary>
        /// <param name="reader"></param>
        public virtual void ReadXml(XmlReader reader)
        {
            //reader.ReadStartElement();

            // 读属性
            if (reader.HasAttributes)
            {
                reader.MoveToFirstAttribute();
                do
                {
                    switch (reader.Name)
                    {
                        case "ID":
                            ID = reader.ReadContentAsInt();
                            break;
                        case "Name":
                            Name = reader.ReadContentAsString();
                            break;
                        case "Alias":
                            Alias = reader.ReadContentAsString();
                            break;
                        case "Owner":
                            Owner = reader.ReadContentAsString();
                            break;
                        case "DbType":
                            DbType = (DatabaseType)Enum.Parse(typeof(DatabaseType), reader.ReadContentAsString());
                            break;
                        case "IsView":
                            IsView = Boolean.Parse(reader.ReadContentAsString());
                            break;
                        case "Description":
                            Description = reader.ReadContentAsString();
                            break;
                        default:
                            break;
                    }
                } while (reader.MoveToNextAttribute());
            }

            reader.ReadStartElement();

            // 读字段
            reader.MoveToElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                switch (reader.Name)
                {
                    case "Columns":
                        reader.ReadStartElement();
                        while (reader.IsStartElement())
                        {
                            IDataColumn dc = CreateColumn();
                            (dc as IXmlSerializable).ReadXml(reader);
                            Columns.Add(dc);
                        }
                        reader.ReadEndElement();
                        break;
                    case "Indexes":
                        reader.ReadStartElement();
                        while (reader.IsStartElement())
                        {
                            IDataIndex di = CreateIndex();
                            (di as IXmlSerializable).ReadXml(reader);
                            Indexes.Add(di);
                        }
                        reader.ReadEndElement();
                        break;
                    case "Relations":
                        reader.ReadStartElement();
                        while (reader.IsStartElement())
                        {
                            IDataRelation dr = CreateRelation();
                            (dr as IXmlSerializable).ReadXml(reader);
                            Relations.Add(dr);
                        }
                        reader.ReadEndElement();
                        break;
                    default:
                        break;
                }
            }

            //reader.ReadEndElement();
            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteXml(XmlWriter writer)
        {
            //writer.WriteStartElement("Table");

            // 写属性
            writer.WriteAttributeString("ID", ID.ToString());
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Alias", Alias);
            if (!String.IsNullOrEmpty(Owner)) writer.WriteAttributeString("Owner", Owner);
            writer.WriteAttributeString("DbType", DbType.ToString());
            writer.WriteAttributeString("IsView", IsView.ToString());
            if (!String.IsNullOrEmpty(Description)) writer.WriteAttributeString("Description", Description);

            // 写字段
            if (Columns != null && Columns.Count > 0 && Columns[0] is IXmlSerializable)
            {
                writer.WriteStartElement("Columns");
                foreach (IXmlSerializable item in Columns)
                {
                    writer.WriteStartElement("Column");
                    item.WriteXml(writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            if (Indexes != null && Indexes.Count > 0 && Indexes[0] is IXmlSerializable)
            {
                writer.WriteStartElement("Indexes");
                foreach (IXmlSerializable item in Indexes)
                {
                    writer.WriteStartElement("Index");
                    item.WriteXml(writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            if (Relations != null && Relations.Count > 0 && Relations[0] is IXmlSerializable)
            {
                writer.WriteStartElement("Relations");
                foreach (IXmlSerializable item in Relations)
                {
                    writer.WriteStartElement("Relation");
                    item.WriteXml(writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            //writer.WriteEndElement();
        }
        #endregion
    }
}