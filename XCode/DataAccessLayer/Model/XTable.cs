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
    /// 表构架
    /// </summary>
    [DebuggerDisplay("ID={ID} Name={Name} Description={Description}")]
    [Serializable]
    [XmlRoot("Table")]
    //[XmlInclude(typeof(XField))]
    //[XmlInclude(typeof(XIndex))]
    //[XmlInclude(typeof(XRelation))]
    class XTable : IDataTable, ICloneable
    {
        #region 基本属性
        private Int32 _ID;
        /// <summary>
        /// 编号
        /// </summary>
        [XmlAttribute]
        [Description("编号")]
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private String _Name;
        /// <summary>
        /// 表名
        /// </summary>
        [XmlAttribute]
        [Description("表名")]
        public String Name { get { return _Name; } set { _Name = value; _Alias = null; } }

        private String _Alias;
        /// <summary>
        /// 别名
        /// </summary>
        [XmlAttribute]
        [Description("别名")]
        public String Alias { get { return _Alias ?? (_Alias = GetAlias(Name)); } set { _Alias = value; } }

        private String _Description;
        /// <summary>
        /// 表说明
        /// </summary>
        [XmlAttribute]
        [Description("表说明")]
        public String Description { get { return _Description; } set { _Description = value; } }

        private Boolean _IsView = false;
        /// <summary>
        /// 是否视图
        /// </summary>
        [XmlAttribute]
        [Description("是否视图")]
        public Boolean IsView { get { return _IsView; } set { _IsView = value; } }

        private String _Owner;
        /// <summary>所有者</summary>
        [XmlAttribute]
        [Description("所有者")]
        public String Owner { get { return _Owner; } set { _Owner = value; } }

        private DatabaseType _DbType;
        /// <summary>数据库类型</summary>
        [XmlAttribute]
        [Description("数据库类型")]
        public DatabaseType DbType { get { return _DbType; } set { _DbType = value; } }
        #endregion

        #region 扩展属性
        private List<IDataColumn> _Columns;
        /// <summary>
        /// 字段集合。
        /// </summary>
        [XmlArray("Columns")]
        [Description("字段集合")]
        public List<IDataColumn> Columns { get { return _Columns ?? (_Columns = new List<IDataColumn>()); } }

        private List<IDataRelation> _ForeignKeys;
        /// <summary>
        /// 外键集合。
        /// </summary>
        [XmlArray]
        [Description("外键集合")]
        public List<IDataRelation> Relations { get { return _ForeignKeys ?? (_ForeignKeys = new List<IDataRelation>()); } }

        private List<IDataIndex> _Indexes;
        /// <summary>
        /// 字段集合。
        /// </summary>
        [XmlArray]
        [Description("索引集合")]
        public List<IDataIndex> Indexes { get { return _Indexes ?? (_Indexes = new List<IDataIndex>()); } }
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
            if (table != null && Columns != null)
            {
                //List<IDataColumn> list = new List<IDataColumn>();
                foreach (IDataColumn item in Columns)
                {
                    table.Columns.Add(item.Clone(table));
                }
                //table.Columns = list.ToArray();
            }
            return table;
        }
        #endregion

        #region 辅助
        /// <summary>
        /// 获取别名
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String GetAlias(String name)
        {
            //TODO 很多时候，这个别名就是表名
            return name;
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
    }
}