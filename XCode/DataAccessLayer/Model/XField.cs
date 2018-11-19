using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>字段构架</summary>
    [Serializable]
    [DisplayName("字段模型")]
    [Description("字段模型")]
    [XmlRoot("Column")]
    class XField : SerializableDataMember, IDataColumn, ICloneable
    {
        #region 属性
        /// <summary>名称</summary>
        [XmlAttribute]
        [DisplayName("名称")]
        [Description("名称")]
        public String Name { get; set; }

        /// <summary>列名</summary>
        [XmlAttribute]
        [DisplayName("列名")]
        [Description("列名")]
        public String ColumnName { get; set; }

        /// <summary>数据类型</summary>
        [XmlAttribute]
        [DisplayName("数据类型")]
        [Description("数据类型")]
        public Type DataType { get; set; }

        /// <summary>字段类型</summary>
        [XmlIgnore]
        [DisplayName("字段类型")]
        [Description("字段类型")]
        public String FieldType { get { return DataType?.Name; } set { DataType = value.GetTypeEx(); } }

        /// <summary>原始数据类型</summary>
        [XmlAttribute]
        [DisplayName("原始类型")]
        [Description("原始类型")]
        public String RawType { get; set; }

        /// <summary>标识</summary>
        [XmlAttribute]
        [DisplayName("标识")]
        [Description("标识")]
        public Boolean Identity { get; set; }

        /// <summary>主键</summary>
        [XmlAttribute]
        [DisplayName("主键")]
        [Description("主键")]
        public Boolean PrimaryKey { get; set; }

        /// <summary>是否主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
        [XmlAttribute]
        [DisplayName("主字段")]
        [Description("主字段")]
        public Boolean Master { get; set; }

        /// <summary>长度</summary>
        [XmlAttribute]
        [DisplayName("长度")]
        [Description("长度")]
        public Int32 Length { get; set; }

        /// <summary>精度</summary>
        //[XmlIgnore]
        [DisplayName("精度")]
        [Description("精度")]
        public Int32 Precision { get; set; }

        /// <summary>位数</summary>
        //[XmlIgnore]
        [DisplayName("位数")]
        [Description("位数")]
        public Int32 Scale { get; set; }

        /// <summary>允许空</summary>
        [XmlAttribute]
        [DisplayName("允许空")]
        [Description("允许空")]
        public Boolean Nullable { get; set; }

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
        #endregion

        #region 扩展属性
        /// <summary>表</summary>
        [XmlIgnore]
        public IDataTable Table { get; set; }

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
        }

        /// <summary>扩展属性</summary>
        [XmlIgnore]
        [Category("扩展")]
        [DisplayName("扩展属性")]
        [Description("扩展属性")]
        public IDictionary<String, String> Properties { get; } = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public XField() { }
        #endregion

        #region 方法
        /// <summary>重新计算修正别名。避免与其它字段名或表名相同，避免关键字</summary>
        /// <returns></returns>
        public IDataColumn Fix()
        {
            return ModelResolver.Current.Fix(this);
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (!String.IsNullOrEmpty(DisplayName) && DisplayName != Name)
                return String.Format("Name={0} FieldType={1} RawType={2} DisplayName={3}", ColumnName, FieldType, RawType, DisplayName);
            else
                return String.Format("Name={0} FieldType={1} RawType={2}", ColumnName, FieldType, RawType);
        }
        #endregion

        #region ICloneable 成员
        /// <summary>克隆</summary>
        /// <returns></returns>
        Object ICloneable.Clone() => Clone(Table);

        /// <summary>克隆</summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public IDataColumn Clone(IDataTable table)
        {
            var field = base.MemberwiseClone() as XField;
            field.Table = table;
            //field.Fix();

            return field;
        }
        #endregion
    }
}