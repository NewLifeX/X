using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 表构架
    /// </summary>
    [DebuggerDisplay("ID={ID} Name={Name} Description={Description}")]
    [Serializable]
    public class XTable
    {
        #region 属性
        #region 基本属性
        private Int32 _ID;
        /// <summary>
        /// 编号
        /// </summary>
        [XmlAttribute]
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private String _Name;
        /// <summary>
        /// 表名
        /// </summary>
        [XmlAttribute]
        public String Name { get { return _Name; } set { _Name = value; } }

        private String _Description;
        /// <summary>
        /// 表说明
        /// </summary>
        [XmlAttribute]
        public String Description { get { return _Description; } set { _Description = value; } }

        private Boolean _IsView = false;
        /// <summary>
        /// 是否视图
        /// </summary>
        [XmlAttribute]
        public Boolean IsView { get { return _IsView; } set { _IsView = value; } }

        private String _Owner;
        /// <summary>所有者</summary>
        [XmlAttribute]
        public String Owner
        {
            get { return _Owner; }
            set { _Owner = value; }
        }
        #endregion

        private List<XField> _Fields;
        /// <summary>
        /// 字段构架集合。首次使用时才加载。
        /// </summary>
        [XmlArray]
        public List<XField> Fields { get { return _Fields; } set { _Fields = value; } }
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

        ///// <summary>
        ///// 取得经过修饰的类名，由子类重写实现
        ///// </summary>
        ///// <returns></returns>
        //public virtual String GetClassName()
        //{
        //    return Name;
        //}

        ///// <summary>
        ///// 取得经过修饰的类说明，由子类重写实现
        ///// </summary>
        ///// <returns></returns>
        //public virtual String GetClassDescription()
        //{
        //    return Description;
        //}
        #endregion

        #region 比较
        /// <summary>
        /// 重载相等操作符
        /// </summary>
        public static bool operator ==(XTable table1, XTable table2)
        {
            return Object.Equals(table1, table2);
        }
        /// <summary>
        /// 重载不等操作符
        /// </summary>
        public static bool operator !=(XTable table1, XTable table2)
        {
            return !(table1 == table2);//调用==，取反
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
            XTable table = obj as XTable;
            if (table == null) return false;

            if (this.Name != table.Name) return false;
            if (this.Description != table.Description) return false;
            if (this.IsView != table.IsView) return false;

            //比较字段
            List<XField> list1 = new List<XField>(Fields);
            List<XField> list2 = new List<XField>(table.Fields);
            foreach (XField item in list1)
            {
                XField match = null;
                //在第二个列表里面找该字段
                foreach (XField elm in list2)
                {
                    if (item == elm)
                    {
                        match = elm;
                        break;
                    }
                }
                //如果找不到，表明第二个列表没有该字段
                if (match == null) return false;
                list2.Remove(match);
            }
            //如果第二个列表还不为空，表明字段数不对应
            if (list2.Count > 0) return false;

            return true;
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
    }
}