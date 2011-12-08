using System;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 字段构架
    /// </summary>
    [Serializable]
    [DisplayName("字段模型")]
    [Description("字段模型")]
    [XmlRoot("Column")]
    class XField : SerializableDataMember, IDataColumn, ICloneable
    {
        #region 属性
        private Int32 _ID;
        /// <summary>
        /// 顺序编号
        /// </summary>
        [XmlAttribute]
        [DisplayName("编号")]
        [Description("编号")]
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private String _Name;
        /// <summary>
        /// 名称
        /// </summary>
        [XmlAttribute]
        [DisplayName("名称")]
        [Description("名称")]
        public String Name { get { return _Name; } set { _Name = value; _Alias = null; } }

        private String _Alias;
        /// <summary>
        /// 别名
        /// </summary>
        [XmlAttribute]
        [DisplayName("别名")]
        [Description("别名")]
        public String Alias { get { return _Alias ?? (_Alias = ModelHelper.GetAlias(Name)); } set { _Alias = value; } }

        private Type _DataType;
        /// <summary>
        /// 数据类型
        /// </summary>
        [XmlAttribute]
        [DisplayName("数据类型")]
        [Description("数据类型")]
        public Type DataType { get { return _DataType; } set { _DataType = value; } }

        /// <summary>
        /// 字段类型
        /// </summary>
        [XmlIgnore]
        [DisplayName("字段类型")]
        [Description("字段类型")]
        public String FieldType { get { return DataType == null ? null : DataType.Name; } set { _DataType = TypeX.GetType(value); } }

        private String _RawType;
        /// <summary>
        /// 原始数据类型
        /// </summary>
        [XmlAttribute]
        [DisplayName("原始类型")]
        [Description("原始类型")]
        public String RawType { get { return _RawType; } set { _RawType = value; } }

        private Boolean _Identity;
        /// <summary>
        /// 标识
        /// </summary>
        [XmlAttribute]
        [DisplayName("标识")]
        [Description("标识")]
        public Boolean Identity { get { return _Identity; } set { _Identity = value; } }

        private Boolean _PrimaryKey;
        /// <summary>
        /// 主键
        /// </summary>
        [XmlAttribute]
        [DisplayName("主键")]
        [Description("主键")]
        public Boolean PrimaryKey { get { return _PrimaryKey; } set { _PrimaryKey = value; } }

        private Int32 _Length;
        /// <summary>
        /// 长度
        /// </summary>
        [XmlAttribute]
        [DisplayName("长度")]
        [Description("长度")]
        public Int32 Length { get { return _Length; } set { _Length = value; } }

        private Int32 _NumOfByte;
        /// <summary>
        /// 字节数
        /// </summary>
        [XmlAttribute]
        [DisplayName("字节数")]
        [Description("字节数")]
        public Int32 NumOfByte { get { return _NumOfByte; } set { _NumOfByte = value; } }

        private Int32 _Precision;
        /// <summary>
        /// 精度
        /// </summary>
        [XmlAttribute]
        [DisplayName("精度")]
        [Description("精度")]
        public Int32 Precision { get { return _Precision; } set { _Precision = value; } }

        private Int32 _Scale;
        /// <summary>
        /// 位数
        /// </summary>
        [XmlAttribute]
        [DisplayName("位数")]
        [Description("位数")]
        public Int32 Scale { get { return _Scale; } set { _Scale = value; } }

        private Boolean _Nullable;
        /// <summary>
        /// 允许空
        /// </summary>
        [XmlAttribute]
        [DisplayName("允许空")]
        [Description("允许空")]
        public Boolean Nullable { get { return _Nullable; } set { _Nullable = value; } }

        private Boolean _IsUnicode;
        /// <summary>
        /// 是否Unicode
        /// </summary>
        [XmlAttribute]
        [DisplayName("Unicode")]
        [Description("Unicode")]
        public Boolean IsUnicode { get { return _IsUnicode; } set { _IsUnicode = value; } }

        private String _Default;
        /// <summary>
        /// 默认值
        /// </summary>
        [XmlAttribute]
        [DisplayName("默认值")]
        [Description("默认值")]
        public String Default { get { return _Default; } set { _Default = value; } }

        private String _Description;
        /// <summary>
        /// 说明
        /// </summary>
        [XmlAttribute]
        [DisplayName("说明")]
        [Description("说明")]
        public String Description { get { return _Description; } set { _Description = value; } }
        #endregion

        #region 扩展属性
        [NonSerialized]
        private IDataTable _Table;
        /// <summary>表</summary>
        [XmlIgnore]
        public IDataTable Table
        {
            get { return _Table; }
            set { _Table = value; }
        }

        /// <summary>显示名。如果有Description则使用Description，否则使用Name</summary>
        [XmlIgnore]
        public String DisplayName { get { return ModelHelper.GetDisplayName(Alias ?? Name, Description); } }
        #endregion

        #region 构造
        private XField() { }

        private XField(IDataTable table)
        {
            Table = table;
        }

        /// <summary>
        /// 为制定表创建字段
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        internal static XField Create(IDataTable table)
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
            field.Table = table;
            return field;
        }
        #endregion
    }
}