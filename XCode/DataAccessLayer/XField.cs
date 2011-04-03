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
    public class XField : ICloneable, IXmlSerializable
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
        public String Name { get { return _Name; } set { _Name = value; } }

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

        #region 扩展属性
        [NonSerialized]
        private XTable _Table;
        /// <summary>表架构</summary>
        [XmlIgnore]
        public XTable Table
        {
            get { return _Table; }
            internal set { _Table = value; }
        }
        #endregion

        #region 方法
        ///// <summary>
        ///// 取得经过修饰的属性名，由子类实现
        ///// </summary>
        ///// <returns></returns>
        //public virtual String GetPropertyName()
        //{
        //    return Name;
        //}

        ///// <summary>
        ///// 取得经过修饰的属性说明，由子类实现
        ///// </summary>
        ///// <returns></returns>
        //public virtual String GetPropertyDescription()
        //{
        //    return Description;
        //}
        #endregion

        #region 中英对照表
        /// <summary>
        /// 英文名
        /// </summary>
        public static readonly String[] ENames = new String[] { "ID", "Name", "DataType", "FieldType", "RawType", "Identity", "PrimaryKey", "Length", "NumOfByte", "Digit", "Nullable", "Default", "Description" };

        /// <summary>
        /// 中文名
        /// </summary>
        public static readonly String[] CNames = new String[] { "字段序号", "字段名", "数据类型", "类型", "原始数据类型", "标识", "主键", "长度", "占用字节数", "小数位数", "允许空", "默认值", "字段说明" };
        #endregion

        #region 属性信息
        private static IList<PropertyInfo> _PropertyInfos;
        /// <summary>
        /// 属性信息
        /// </summary>
        private static IList<PropertyInfo> PropertyInfos
        {
            get
            {
                if (_PropertyInfos != null) return _PropertyInfos;
                _PropertyInfos = new List<PropertyInfo>(typeof(XField).GetProperties());
                return _PropertyInfos;
            }
        }
        #endregion

        #region 加载数据
        /// <summary>
        /// 英文名转中文名
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String CNameByEName(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;
            for (Int32 i = 0; i < ENames.Length; i++)
            {
                if (ENames[i] == name) return CNames[i];
            }
            return null;
        }

        /// <summary>
        /// 中文名转英文名
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String ENameByCName(String name)
        {
            if (String.IsNullOrEmpty(name)) return null;
            for (Int32 i = 0; i < CNames.Length; i++)
            {
                if (CNames[i] == name) return ENames[i];
            }
            return null;
        }
        #endregion

        #region 比较
        /// <summary>
        /// 重载相等操作符
        /// </summary>
        public static bool operator ==(XField field1, XField field2)
        {
            return Object.Equals(field1, field2);
        }
        /// <summary>
        /// 重载不等操作符
        /// </summary>
        public static bool operator !=(XField field1, XField field2)
        {
            return !(field1 == field2);//调用==，取反
        }

        /// <summary>
        /// 用作特定类型的哈希函数。
        /// </summary>
        /// <returns></returns>
        public override Int32 GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// 确定指定的 Object 是否等于当前的 Object。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            XField field = obj as XField;
            if (field == null) return false;

            if (this.Name != field.Name) return false;
            if (this.DataType != field.DataType) return false;
            if (this.Identity != field.Identity) return false;
            if (this.PrimaryKey != field.PrimaryKey) return false;
            if (this.Length != field.Length) return false;
            if (this.NumOfByte != field.NumOfByte) return false;
            if (this.Scale != field.Scale) return false;
            if (this.Nullable != field.Nullable) return false;
            if (this.Default != field.Default) return false;
            if (this.Description != field.Description) return false;

            return true;
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
        /// /// 克隆
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
        public XField Clone(XTable table)
        {
            XField field = base.MemberwiseClone() as XField;
            field.Table = table;
            return field;
        }
        #endregion

        #region IXmlSerializable 成员
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            foreach (PropertyInfoX item in TypeX.Create(this.GetType()).Properties)
            {
                if (!item.Property.CanRead) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item.Member, false) != null) continue;

                String v = reader.GetAttribute(item.Name);
                if (String.IsNullOrEmpty(v)) continue;

                Object obj = null;
                if (item.Type == typeof(Type))
                    obj = TypeX.GetType(v);
                else
                    obj = Convert.ChangeType(v, item.Type);
                item.SetValue(this, obj);
            }
            reader.Skip();
        }

        static XField def = new XField();

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            foreach (PropertyInfoX item in TypeX.Create(this.GetType()).Properties)
            {
                if (!item.Property.CanWrite) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item.Member, false) != null) continue;

                Object obj = item.GetValue(this);
                // 默认值不参与序列化，节省空间
                if (Object.Equals(obj, item.GetValue(def))) continue;

                if (item.Type == typeof(Type)) obj = (obj as Type).Name;
                writer.WriteAttributeString(item.Name, obj == null ? null : obj.ToString());
            }
        }
        #endregion
    }
}