using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.ComponentModel;
using NewLife.Collections;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 表构架
    /// </summary>
    [DebuggerDisplay("ID={ID} Name={Name} Description={Description}")]
    [Serializable]
    public class XTable : ICloneable
    {
        #region 属性
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
        public String Owner
        {
            get { return _Owner; }
            set { _Owner = value; }
        }

        private DatabaseType _DbType;
        /// <summary>数据库类型</summary>
        [XmlAttribute]
        [Description("数据库类型")]
        public DatabaseType DbType
        {
            get { return _DbType; }
            set { _DbType = value; }
        }
        #endregion

        private List<XField> _Fields;
        /// <summary>
        /// 字段集合。
        /// </summary>
        [XmlArray]
        [Description("字段集合")]
        public List<XField> Fields { get { return _Fields ?? (_Fields = new List<XField>()); } set { _Fields = value; } }

        private List<Triplet<String, String, String>> _Foreigns;
        /// <summary>
        /// 外键集合。
        /// </summary>
        [XmlArray]
        [Description("外键集合")]
        public List<Triplet<String, String, String>> Foreigns { get { return _Foreigns ?? (_Foreigns = new List<Triplet<String, String, String>>()); } set { _Foreigns = value; } }

        private List<XIndex> _Indexes;
        /// <summary>
        /// 字段集合。
        /// </summary>
        [XmlArray]
        [Description("索引集合")]
        public List<XIndex> Indexes { get { return _Indexes ?? (_Indexes = new List<XIndex>()); } set { _Indexes = value; } }
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
        public virtual XField CreateField()
        {
            return XField.Create(this);
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

        #region 比较
        ///// <summary>
        ///// 重载相等操作符
        ///// </summary>
        //public static bool operator ==(XTable table1, XTable table2)
        //{
        //    return Object.Equals(table1, table2);
        //}
        ///// <summary>
        ///// 重载不等操作符
        ///// </summary>
        //public static bool operator !=(XTable table1, XTable table2)
        //{
        //    return !(table1 == table2);//调用==，取反
        //}

        ///// <summary>
        ///// 用作特定类型的哈希函数。
        ///// </summary>
        ///// <returns></returns>
        //public override Int32 GetHashCode()
        //{
        //    return base.GetHashCode();
        //}

        ///// <summary>
        ///// 确定指定的 Object 是否等于当前的 Object。
        ///// </summary>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //public override bool Equals(object obj)
        //{
        //    if (obj == null) return false;
        //    XTable table = obj as XTable;
        //    if (table == null) return false;

        //    if (this.Name != table.Name) return false;
        //    if (this.Description != table.Description) return false;
        //    if (this.IsView != table.IsView) return false;

        //    //比较字段
        //    List<XField> list1 = new List<XField>(Fields);
        //    List<XField> list2 = new List<XField>(table.Fields);
        //    foreach (XField item in list1)
        //    {
        //        XField match = null;
        //        //在第二个列表里面找该字段
        //        foreach (XField elm in list2)
        //        {
        //            if (item == elm)
        //            {
        //                match = elm;
        //                break;
        //            }
        //        }
        //        //如果找不到，表明第二个列表没有该字段
        //        if (match == null) return false;
        //        list2.Remove(match);
        //    }
        //    //如果第二个列表还不为空，表明字段数不对应
        //    if (list2.Count > 0) return false;

        //    return true;
        //}
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
        /// /// 克隆
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
            if (table != null && Fields != null)
            {
                table.Fields = new List<XField>();
                foreach (XField item in Fields)
                {
                    table.Fields.Add(item.Clone(table));
                }
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
    }
}