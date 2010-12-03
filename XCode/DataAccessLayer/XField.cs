using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 字段构架
    /// </summary>
    [Serializable]
    public class XField
    {
        #region 属性
        private Int32 _ID;
        /// <summary>
        /// 顺序编号
        /// </summary>
        [XmlAttribute]
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private String _Name;
        /// <summary>
        /// 名称
        /// </summary>
        [XmlAttribute]
        public String Name { get { return _Name; } set { _Name = value; } }

        private Type _DataType;
        /// <summary>
        /// 数据类型
        /// </summary>
        [XmlAttribute]
        public Type DataType { get { return _DataType; } set { _DataType = value; } }

        /// <summary>
        /// 字段类型
        /// </summary>
        public String FieldType { get { return DataType == null ? null : DataType.Name; } }

        private Boolean _Identity;
        /// <summary>
        /// 标识
        /// </summary>
        [XmlAttribute]
        public Boolean Identity { get { return _Identity; } set { _Identity = value; } }

        private Boolean _PrimaryKey;
        /// <summary>
        /// 主键
        /// </summary>
        [XmlAttribute]
        public Boolean PrimaryKey { get { return _PrimaryKey; } set { _PrimaryKey = value; } }

        private Int32 _Length;
        /// <summary>
        /// 长度
        /// </summary>
        [XmlAttribute]
        public Int32 Length { get { return _Length; } set { _Length = value; } }

        private Int32 _NumOfByte;
        /// <summary>
        /// 字节数
        /// </summary>
        [XmlAttribute]
        public Int32 NumOfByte { get { return _NumOfByte; } set { _NumOfByte = value; } }

        private Int32 _Digit;
        /// <summary>
        /// 位数
        /// </summary>
        [XmlAttribute]
        public Int32 Digit { get { return _Digit; } set { _Digit = value; } }

        private Boolean _Nullable;
        /// <summary>
        /// 允许空
        /// </summary>
        [XmlAttribute]
        public Boolean Nullable { get { return _Nullable; } set { _Nullable = value; } }

        private String _Default;
        /// <summary>
        /// 默认值
        /// </summary>
        [XmlAttribute]
        public String Default { get { return _Default; } set { _Default = value; } }

        private String _Description;
        /// <summary>
        /// 说明
        /// </summary>
        [XmlAttribute]
        public String Description { get { return _Description; } set { _Description = value; } }
        #endregion

        #region 构造
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
        private XTable _Table;
        /// <summary>表架构</summary>
        public XTable Table
        {
            get { return _Table; }
            private set { _Table = value; }
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
        public static readonly String[] ENames = new String[] { "ID", "Name", "DataType", "FieldType", "Identity", "PrimaryKey", "Length", "NumOfByte", "Digit", "Nullable", "Default", "Description" };
        
        /// <summary>
        /// 中文名
        /// </summary>
        public static readonly String[] CNames = new String[] { "字段序号", "字段名", "数据类型", "类型", "标识", "主键", "长度", "占用字节数", "小数位数", "允许空", "默认值", "字段说明" };
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
            if (this.Digit != field.Digit) return false;
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
            return String.Format("ID={0} Name={1} FieldType={2} Description={3}", ID, Name, FieldType, Description);
        }
        #endregion
    }
}