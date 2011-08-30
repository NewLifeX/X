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

        //private IDataColumn[] _PrimaryKeys;
        /// <summary>主键集合</summary>
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
            if (String.IsNullOrEmpty(name)) return null;

            foreach (IDataColumn item in Columns)
            {
                if (String.Equals(name, item.Name, StringComparison.OrdinalIgnoreCase)) return item;
            }

            foreach (IDataColumn item in Columns)
            {
                if (String.Equals(name, item.Alias, StringComparison.OrdinalIgnoreCase)) return item;
            }
            return null;
        }

        /// <summary>
        /// 根据字段名数组获取字段数组
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public virtual IDataColumn[] GetColumns(String[] names)
        {
            if (names == null || names.Length < 1) return null;

            List<IDataColumn> list = new List<IDataColumn>();
            foreach (String item in names)
            {
                IDataColumn dc = GetColumn(item);
                if (dc != null) list.Add(dc);
            }

            if (list.Count < 1) return null;
            return list.ToArray(); ;
        }

        /// <summary>
        /// 连接另一个表，处理两表间关系
        /// </summary>
        /// <param name="table"></param>
        public virtual void Connect(IDataTable table)
        {
            foreach (IDataColumn dc in Columns)
            {
                if (dc.PrimaryKey || dc.Identity) continue;

                if (FindRelation(table, table.Name, dc, dc.Name) != null) continue;
                if (!String.IsNullOrEmpty(dc.Alias) && !String.Equals(dc.Alias, dc.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (FindRelation(table, table.Name, dc, dc.Alias) != null) continue;
                }

                if (FindRelation(table, table.Alias, dc, dc.Name) != null) continue;
                if (!String.IsNullOrEmpty(dc.Alias) && !String.Equals(dc.Alias, dc.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (FindRelation(table, table.Alias, dc, dc.Alias) != null) continue;
                }
            }
        }

        IDataRelation FindRelation(IDataTable rtable, String rname, IDataColumn column, String name)
        {
            if (name.Length <= rtable.Name.Length || !name.StartsWith(rtable.Name, StringComparison.OrdinalIgnoreCase)) return null;

            String key = name.Substring(rtable.Name.Length);
            IDataColumn dc = rtable.GetColumn(key);
            if (dc == null) return null;

            // 建立关系
            IDataRelation dr = CreateRelation();
            dr.Column = column.Name;
            dr.RelationTable = rtable.Name;
            dr.RelationColumn = dc.Name;
            // 表关系这里一般是多对一，比如管理员的RoleID，对于索引来说，不是唯一的
            dr.Unique = false;

            Relations.Add(dr);

            // 给另一方建立关系
            dr = rtable.CreateRelation();
            dr.Column = dc.Name;
            dr.RelationTable = Name;
            dr.RelationColumn = column.Name;
            // 那么这里就是唯一的啦
            dr.Unique = true;

            rtable.Relations.Add(dr);

            return dr;
        }

        /// <summary>
        /// 修正数据
        /// </summary>
        public virtual void Fix()
        {
            // 给所有关系字段建立索引
            foreach (IDataRelation dr in Relations)
            {
                // 跳过主键
                IDataColumn dc = GetColumn(dr.Column);
                if (dc == null || dc.PrimaryKey) continue;

                Boolean hasIndex = false;
                foreach (IDataIndex item in Indexes)
                {
                    if (item.Columns != null && item.Columns.Length == 1 && String.Equals(item.Columns[0], dr.Column, StringComparison.OrdinalIgnoreCase))
                    {
                        hasIndex = true;
                        break;
                    }
                }
                if (!hasIndex)
                {
                    IDataIndex di = CreateIndex();
                    di.Columns = new String[] { dr.Column };
                    // 这两个的关系，是反过来的
                    di.Unique = dr.Unique;
                    Indexes.Add(di);
                }
            }

            #region 修正主键
            IDataColumn[] pks = PrimaryKeys;
            if (pks == null || pks.Length < 1)
            {
                // 在索引中找唯一索引作为主键
                foreach (IDataIndex item in Indexes)
                {
                    if (!item.Unique || item.Columns == null || item.Columns.Length < 1) continue;

                    pks = GetColumns(item.Columns);
                    Array.ForEach<IDataColumn>(pks, dc => dc.PrimaryKey = true);
                }
                // 如果还没有主键，把第一个索引作为主键
                if (pks == null || pks.Length < 1)
                {
                    foreach (IDataIndex item in Indexes)
                    {
                        if (item.Columns == null || item.Columns.Length < 1) continue;

                        pks = GetColumns(item.Columns);
                        Array.ForEach<IDataColumn>(pks, dc => dc.PrimaryKey = true);
                    }
                }
            }
            #endregion

            #region 移除主键对应的索引，因为主键会自动创建索引
            pks = PrimaryKeys;
            if (pks != null && pks.Length > 0)
            {
                for (int i = Indexes.Count - 1; i >= 0; i--)
                {
                    // 判断索引的字段是否就是主键的字段
                    // 假设就是，需要在索引字段中找到一个不是主键的字段
                    Boolean b = true;
                    foreach (String item in Indexes[i].Columns)
                    {
                        foreach (IDataColumn pk in pks)
                        {
                            if (!String.Equals(pk.Name, item, StringComparison.OrdinalIgnoreCase))
                            {
                                b = false;
                                break;
                            }
                        }
                        if (!b) break;
                    }
                    if (b) Indexes.RemoveAt(i);
                }
            }
            #endregion
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