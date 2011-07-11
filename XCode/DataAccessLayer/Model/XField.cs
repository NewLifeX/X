using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 字段构架
    /// </summary>
    [Serializable]
    public class XField : SerializableDataMember, IDataColumn, ICloneable
    {
        #region 属性
        private Int32 _ID;
        /// <summary>
        /// 顺序编号
        /// </summary>
        [XmlAttribute]
        [Description("编号")]
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private String _Name;
        /// <summary>
        /// 名称
        /// </summary>
        [XmlAttribute]
        [Description("名称")]
        public String Name { get { return _Name; } set { _Name = value; _Alias = null; } }

        private String _Alias;
        /// <summary>
        /// 别名
        /// </summary>
        [XmlAttribute]
        [Description("别名")]
        public String Alias { get { return _Alias ?? (_Alias = XTable.GetAlias(Name)); } set { _Alias = value; } }

        private Type _DataType;
        /// <summary>
        /// 数据类型
        /// </summary>
        [XmlAttribute]
        [Description("数据类型")]
        public Type DataType { get { return _DataType; } set { _DataType = value; } }

        /// <summary>
        /// 字段类型
        /// </summary>
        [XmlIgnore]
        [Description("字段类型")]
        public String FieldType { get { return DataType == null ? null : DataType.Name; } }

        private String _RawType;
        /// <summary>
        /// 原始数据类型
        /// </summary>
        [XmlAttribute]
        [Description("原始数据类型")]
        public String RawType { get { return _RawType; } set { _RawType = value; } }

        private Boolean _Identity;
        /// <summary>
        /// 标识
        /// </summary>
        [XmlAttribute]
        [Description("标识")]
        public Boolean Identity { get { return _Identity; } set { _Identity = value; } }

        private Boolean _PrimaryKey;
        /// <summary>
        /// 主键
        /// </summary>
        [XmlAttribute]
        [Description("主键")]
        public Boolean PrimaryKey { get { return _PrimaryKey; } set { _PrimaryKey = value; } }

        private Int32 _Length;
        /// <summary>
        /// 长度
        /// </summary>
        [XmlAttribute]
        [Description("长度")]
        public Int32 Length { get { return _Length; } set { _Length = value; } }

        private Int32 _NumOfByte;
        /// <summary>
        /// 字节数
        /// </summary>
        [XmlAttribute]
        [Description("字节数")]
        public Int32 NumOfByte { get { return _NumOfByte; } set { _NumOfByte = value; } }

        private Int32 _Precision;
        /// <summary>
        /// 精度
        /// </summary>
        [XmlAttribute]
        [Description("精度")]
        public Int32 Precision { get { return _Precision; } set { _Precision = value; } }

        private Int32 _Scale;
        /// <summary>
        /// 位数
        /// </summary>
        [XmlAttribute]
        [Description("位数")]
        public Int32 Scale { get { return _Scale; } set { _Scale = value; } }

        private Boolean _Nullable;
        /// <summary>
        /// 允许空
        /// </summary>
        [XmlAttribute]
        [Description("允许空")]
        public Boolean Nullable { get { return _Nullable; } set { _Nullable = value; } }

        private Boolean _IsUnicode;
        /// <summary>
        /// 是否Unicode
        /// </summary>
        [XmlAttribute]
        [Description("是否Unicode")]
        public Boolean IsUnicode { get { return _IsUnicode; } set { _IsUnicode = value; } }

        private String _Default;
        /// <summary>
        /// 默认值
        /// </summary>
        [XmlAttribute]
        [Description("默认值")]
        public String Default { get { return _Default; } set { _Default = value; } }

        private String _Description;
        /// <summary>
        /// 说明
        /// </summary>
        [XmlAttribute]
        [Description("说明")]
        public String Description { get { return _Description; } set { _Description = value; } }
        #endregion

        #region 扩展属性
        [NonSerialized]
        private XTable _Table;
        /// <summary>表架构</summary>
        [XmlIgnore]
        public XTable Table
        {
            get { return _Table; }
            private set { _Table = value; }
        }

        [XmlIgnore]
        IDataTable IDataColumn.Table { get { return Table; } }
        #endregion

        #region 构造
        private XField() { }

        private XField(XTable table)
        {
            Table = table;
        }

        /// <summary>
        /// 为制定表创建字段
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        internal static XField Create(XTable table)
        {
            if (table == null) throw new ArgumentNullException("table");

            return new XField(table);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("ID={0} Name={1} FieldType={2} RawType={3} Description={4}", ID, Name, FieldType, RawType, Description);
        }
        #endregion

        #region ICloneable 成员
        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone()
        {
            return Clone(Table);
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public IDataColumn Clone(IDataTable table)
        {
            XField field = base.MemberwiseClone() as XField;
            field.Table = table as XTable;
            return field;
        }
        #endregion
    }
}