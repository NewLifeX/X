using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace XCode.DataAccessLayer
{
    /// <summary>索引</summary>
    [Serializable]
    [DisplayName("索引模型")]
    [Description("索引模型")]
    [XmlRoot("Index")]
    class XIndex : SerializableDataMember, IDataIndex, ICloneable
    {
        #region 属性
        /// <summary>名称</summary>
        [XmlAttribute]
        [DisplayName("名称")]
        [Description("名称")]
        public String Name { get; set; }

        /// <summary>数据列集合</summary>
        [XmlAttribute]
        [DisplayName("数据列集合")]
        [Description("数据列集合")]
        public String[] Columns { get; set; }

        /// <summary>是否唯一</summary>
        [XmlAttribute]
        [DisplayName("唯一")]
        [Description("唯一")]
        public Boolean Unique { get; set; }

        /// <summary>是否主键</summary>
        [XmlAttribute]
        [DisplayName("主键")]
        [Description("主键")]
        public Boolean PrimaryKey { get; set; }

        ///// <summary>是否计算出来的，而不是数据库内置的</summary>
        //[XmlAttribute]
        //[DisplayName("计算")]
        //[Description("是否计算出来的，而不是数据库内置的")]
        //public Boolean Computed { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>表</summary>
        [XmlIgnore]
        public IDataTable Table { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public XIndex()
        {
            Columns = new String[0];
        }
        #endregion

        #region 方法
        /// <summary>修正数据</summary>
        /// <returns></returns>
        public IDataIndex Fix()
        {
            if (Name.IsNullOrEmpty()) Name = ModelResolver.Current.GetName(this);

            return this;
        }
        #endregion

        #region ICloneable 成员
        /// <summary>克隆</summary>
        /// <returns></returns>
        Object ICloneable.Clone() => Clone(Table);

        /// <summary>克隆</summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public IDataIndex Clone(IDataTable table)
        {
            var field = base.MemberwiseClone() as XIndex;
            field.Table = table;

            return field;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (Columns != null && Columns.Length > 0)
                return String.Format("{0}=>{1} {2}", Name, String.Join(",", Columns), Unique ? "U" : "");
            else
                return String.Format("{0} {1}", Name, Unique ? "U" : "");
        }
        #endregion
    }
}