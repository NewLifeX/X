/*
 * XCoder v3.2.2010.1014
 * 作者：nnhy/NEWLIFE
 * 时间：2010-10-20 09:54:05
 * 版权：版权所有 (C) 新生命开发团队 2010
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 菜单
    /// </summary>
    [Serializable]
    [DataObject]
    [Description("菜单")]
    [BindTable("Menu", Description = "菜单", ConnName = "Common")]
    public partial class Menu<TEntity>
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

        private String _Name;
        /// <summary>
        /// 名称
        /// </summary>
        [Description("名称")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Name", Description = "名称", DefaultValue = "", Order = 2)]
        public String Name
        {
            get { return _Name; }
            set { if (OnPropertyChange("Name", value)) _Name = value; }
        }

        private Int32 _ParentID;
        /// <summary>
        /// 父编号
        /// </summary>
        [Description("父编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn("ParentID", Description = "父编号", DefaultValue = "", Order = 3)]
        public Int32 ParentID
        {
            get { return _ParentID; }
            set { if (OnPropertyChange("ParentID", value)) _ParentID = value; }
        }

        private String _Permission;
        /// <summary>
        /// 权限
        /// </summary>
        [Description("权限")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Permission", Description = "权限", DefaultValue = "", Order = 4)]
        public String Permission
        {
            get { return _Permission; }
            set { if (OnPropertyChange("Permission", value)) _Permission = value; }
        }

        private String _Url;
        /// <summary>
        /// 链接
        /// </summary>
        [Description("链接")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Url", Description = "链接", DefaultValue = "", Order = 5)]
        public String Url
        {
            get { return _Url; }
            set { if (OnPropertyChange("Url", value)) _Url = value; }
        }

        private Int32 _Sort;
        /// <summary>
        /// 序号
        /// </summary>
        [Description("序号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn("Sort", Description = "序号", DefaultValue = "", Order = 6)]
        public Int32 Sort
        {
            get { return _Sort; }
            set { if (OnPropertyChange("Sort", value)) _Sort = value; }
        }

        private Boolean _IsShow;
        /// <summary>
        /// 是否显示
        /// </summary>
        [Description("是否显示")]
        [DataObjectField(false, false, true, 1)]
        [BindColumn("IsShow", Description = "是否显示", DefaultValue = "", Order = 7)]
        public Boolean IsShow
        {
            get { return _IsShow; }
            set { if (OnPropertyChange("IsShow", value)) _IsShow = value; }
        }

        private String _Remark;
        /// <summary>
        /// 备注
        /// </summary>
        [Description("备注")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Remark", Description = "备注", DefaultValue = "", Order = 8)]
        public String Remark
        {
            get { return _Remark; }
            set { if (OnPropertyChange("Remark", value)) _Remark = value; }
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
                    case "Name": return Name;
                    case "ParentID": return ParentID;
                    case "Permission": return Permission;
                    case "Url": return Url;
                    case "Sort": return Sort;
                    case "IsShow": return IsShow;
                    case "Remark": return Remark;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = Convert.ToInt32(value); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "ParentID": _ParentID = Convert.ToInt32(value); break;
                    case "Permission": _Permission = Convert.ToString(value); break;
                    case "Url": _Url = Convert.ToString(value); break;
                    case "Sort": _Sort = Convert.ToInt32(value); break;
                    case "IsShow": _IsShow = Convert.ToBoolean(value); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>
        /// 取得菜单字段名的快捷方式
        /// </summary>
        public class _
        {
            ///<summary>
            /// 编号
            ///</summary>
            public const String ID = "ID";

            ///<summary>
            /// 名称
            ///</summary>
            public const String Name = "Name";

            ///<summary>
            /// 父编号
            ///</summary>
            public const String ParentID = "ParentID";

            ///<summary>
            /// 权限
            ///</summary>
            public const String Permission = "Permission";

            ///<summary>
            /// 链接
            ///</summary>
            public const String Url = "Url";

            ///<summary>
            /// 序号
            ///</summary>
            public const String Sort = "Sort";

            ///<summary>
            /// 是否显示
            ///</summary>
            public const String IsShow = "IsShow";

            ///<summary>
            /// 备注
            ///</summary>
            public const String Remark = "Remark";
        }
        #endregion
    }
}