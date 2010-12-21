/*
 * 模板更新：适用于XCode v3.0.2009.0608，属性中增加OnPropertyChange，用于控制脏数据
 * 代码生成：2010-06-23 13:58:04
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 地区
    /// </summary>
    [Serializable]
    [DataObject]
    [Description("地区")]
    [BindTable("Area", Description = "地区", ConnName = "Common")]
    public partial class Area<TEntity>
    {
        #region 属性
        private Int32 _ID;
        /// <summary>
        /// 编号
        /// </summary>
        [Description("编号")]
        [DataObjectField(true, true, false, 10)]
        [BindColumn("ID", Description = "编号", DefaultValue = "", Order = 1)]
        public Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChange("ID", value)) _ID = value; }
        }

        private Int32 _Code;
        /// <summary>
        /// 代码
        /// </summary>
        [Description("代码")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn("Code", Description = "代码", DefaultValue = "", Order = 2)]
        public Int32 Code
        {
            get { return _Code; }
            set { if (OnPropertyChange("Code", value)) _Code = value; }
        }

        private String _Name;
        /// <summary>
        /// 名称
        /// </summary>
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn("Name", Description = "名称", DefaultValue = "", Order = 3)]
        public String Name
        {
            get { return _Name; }
            set { if (OnPropertyChange("Name", value)) _Name = value; }
        }

        private Int32 _ParentCode;
        /// <summary>
        /// 父地区代码
        /// </summary>
        [Description("父地区代码")]
        [DataObjectField(false, false, false, 10)]
        [BindColumn("ParentCode", Description = "父地区代码", DefaultValue = "0", Order = 4)]
        public Int32 ParentCode
        {
            get { return _ParentCode; }
            set { if (OnPropertyChange("ParentCode", value)) _ParentCode = value; }
        }

        private String _Description;
        /// <summary>
        /// 描述
        /// </summary>
        [Description("描述")]
        [DataObjectField(false, false, true, 1073741823)]
        [BindColumn("Description", Description = "描述", DefaultValue = "", Order = 5)]
        public String Description
        {
            get { return _Description; }
            set { if (OnPropertyChange("Description", value)) _Description = value; }
        }
        #endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// 一个索引，基类使用反射实现。
        /// 派生实体类可重写该索引，以避免反射带来的性能损耗
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public override Object this[String name]
        {
            get
            {
                switch (name)
                {
                    case "ID": return ID;
                    case "Code": return Code;
                    case "Name": return Name;
                    case "ParentCode": return ParentCode;
                    case "Description": return Description;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = Convert.ToInt32(value); break;
                    case "Code": _Code = Convert.ToInt32(value); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "ParentCode": _ParentCode = Convert.ToInt32(value); break;
                    case "Description": _Description = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>
        /// 取得字段名的快捷方式
        /// </summary>
        public class _
        {
            /// <summary>
            /// 编号
            /// </summary>
            public const String ID = "ID";

            /// <summary>
            /// 代码
            /// </summary>
            public const String Code = "Code";

            /// <summary>
            /// 名称
            /// </summary>
            public const String Name = "Name";

            /// <summary>
            /// 父地区代码
            /// </summary>
            public const String ParentCode = "ParentCode";

            /// <summary>
            /// 描述
            /// </summary>
            public const String Description = "Description";
        }
        #endregion
    }
}