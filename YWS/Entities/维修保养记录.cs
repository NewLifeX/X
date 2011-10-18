/*
 * XCoder v4.3.2011.0915
 * 作者：nnhy/NEWLIFE
 * 时间：2011-09-28 11:04:30
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.YWS.Entities
{
    /// <summary>维修保养记录</summary>
    [Serializable]
    [DataObject]
    [Description("维修保养记录")]
    [BindIndex("IX_Maintenance", true, "CustomerID,MachineID")]
    [BindIndex("PK__Maintena__3214EC270EA330E9", true, "ID")]
    [BindIndex("IX_Maintenance_CustomerID", false, "CustomerID")]
    [BindIndex("IX_Maintenance_MachineID", false, "MachineID")]
    [BindRelation("CustomerID", false, "Customer", "ID")]
    [BindRelation("MachineID", false, "Machine", "ID")]
    [BindTable("Maintenance", Description = "维修保养记录", ConnName = "YWS", DbType = DatabaseType.SqlServer)]
    public partial class Maintenance : IMaintenance
    
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 10)]
        [BindColumn(1, "ID", "编号", null, "int", 10, 0, false)]
        public Int32 ID
        {
            get { return _ID; }
            set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } }
        }

        private Int32 _CustomerID;
        /// <summary>客户编号</summary>
        [DisplayName("客户编号")]
        [Description("客户编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(2, "CustomerID", "客户编号", null, "int", 10, 0, false)]
        public Int32 CustomerID
        {
            get { return _CustomerID; }
            set { if (OnPropertyChanging("CustomerID", value)) { _CustomerID = value; OnPropertyChanged("CustomerID"); } }
        }

        private Int32 _MachineID;
        /// <summary>机器编号</summary>
        [DisplayName("机器编号")]
        [Description("机器编号")]
        [DataObjectField(false, false, true, 10)]
        [BindColumn(3, "MachineID", "机器编号", null, "int", 10, 0, false)]
        public Int32 MachineID
        {
            get { return _MachineID; }
            set { if (OnPropertyChanging("MachineID", value)) { _MachineID = value; OnPropertyChanged("MachineID"); } }
        }

        private String _Technician;
        /// <summary>技术员</summary>
        [DisplayName("技术员")]
        [Description("技术员")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "Technician", "技术员", null, "nvarchar(50)", 0, 0, true)]
        public String Technician
        {
            get { return _Technician; }
            set { if (OnPropertyChanging("Technician", value)) { _Technician = value; OnPropertyChanged("Technician"); } }
        }

        private String _Reason;
        /// <summary>故障原因</summary>
        [DisplayName("故障原因")]
        [Description("故障原因")]
        [DataObjectField(false, false, true, 1073741823)]
        [BindColumn(5, "Reason", "故障原因", null, "ntext", 0, 0, true)]
        public String Reason
        {
            get { return _Reason; }
            set { if (OnPropertyChanging("Reason", value)) { _Reason = value; OnPropertyChanged("Reason"); } }
        }

        private String _Fittings;
        /// <summary>更换配件</summary>
        [DisplayName("更换配件")]
        [Description("更换配件")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn(6, "Fittings", "更换配件", null, "nvarchar(200)", 0, 0, true)]
        public String Fittings
        {
            get { return _Fittings; }
            set { if (OnPropertyChanging("Fittings", value)) { _Fittings = value; OnPropertyChanged("Fittings"); } }
        }

        private String _Propose;
        /// <summary>改进建议</summary>
        [DisplayName("改进建议")]
        [Description("改进建议")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn(7, "Propose", "改进建议", null, "nvarchar(200)", 0, 0, true)]
        public String Propose
        {
            get { return _Propose; }
            set { if (OnPropertyChanging("Propose", value)) { _Propose = value; OnPropertyChanged("Propose"); } }
        }

        private String _Remark;
        /// <summary>维修备注</summary>
        [DisplayName("维修备注")]
        [Description("维修备注")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn(8, "Remark", "维修备注", null, "nvarchar(200)", 0, 0, true)]
        public String Remark
        {
            get { return _Remark; }
            set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } }
        }

        private DateTime _AddTime;
        /// <summary>添加时间</summary>
        [DisplayName("添加时间")]
        [Description("添加时间")]
        [DataObjectField(false, false, true, 3)]
        [BindColumn(9, "AddTime", "添加时间", null, "datetime", 3, 0, false)]
        public DateTime AddTime
        {
            get { return _AddTime; }
            set { if (OnPropertyChanging("AddTime", value)) { _AddTime = value; OnPropertyChanged("AddTime"); } }
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
                    case "ID" : return _ID;
                    case "CustomerID" : return _CustomerID;
                    case "MachineID" : return _MachineID;
                    case "Technician" : return _Technician;
                    case "Reason" : return _Reason;
                    case "Fittings" : return _Fittings;
                    case "Propose" : return _Propose;
                    case "Remark" : return _Remark;
                    case "AddTime" : return _AddTime;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID" : _ID = Convert.ToInt32(value); break;
                    case "CustomerID" : _CustomerID = Convert.ToInt32(value); break;
                    case "MachineID" : _MachineID = Convert.ToInt32(value); break;
                    case "Technician" : _Technician = Convert.ToString(value); break;
                    case "Reason" : _Reason = Convert.ToString(value); break;
                    case "Fittings" : _Fittings = Convert.ToString(value); break;
                    case "Propose" : _Propose = Convert.ToString(value); break;
                    case "Remark" : _Remark = Convert.ToString(value); break;
                    case "AddTime" : _AddTime = Convert.ToDateTime(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得维修保养记录字段信息的快捷方式</summary>
        public class _
        {
            ///<summary>编号</summary>
            public static readonly FieldItem ID = Meta.Table.FindByName("ID");

            ///<summary>客户编号</summary>
            public static readonly FieldItem CustomerID = Meta.Table.FindByName("CustomerID");

            ///<summary>机器编号</summary>
            public static readonly FieldItem MachineID = Meta.Table.FindByName("MachineID");

            ///<summary>技术员</summary>
            public static readonly FieldItem Technician = Meta.Table.FindByName("Technician");

            ///<summary>故障原因</summary>
            public static readonly FieldItem Reason = Meta.Table.FindByName("Reason");

            ///<summary>更换配件</summary>
            public static readonly FieldItem Fittings = Meta.Table.FindByName("Fittings");

            ///<summary>改进建议</summary>
            public static readonly FieldItem Propose = Meta.Table.FindByName("Propose");

            ///<summary>维修备注</summary>
            public static readonly FieldItem Remark = Meta.Table.FindByName("Remark");

            ///<summary>添加时间</summary>
            public static readonly FieldItem AddTime = Meta.Table.FindByName("AddTime");
        }
        #endregion
    }

    /// <summary>维修保养记录接口</summary>
    public partial interface IMaintenance
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>客户编号</summary>
        Int32 CustomerID { get; set; }

        /// <summary>机器编号</summary>
        Int32 MachineID { get; set; }

        /// <summary>技术员</summary>
        String Technician { get; set; }

        /// <summary>故障原因</summary>
        String Reason { get; set; }

        /// <summary>更换配件</summary>
        String Fittings { get; set; }

        /// <summary>改进建议</summary>
        String Propose { get; set; }

        /// <summary>维修备注</summary>
        String Remark { get; set; }

        /// <summary>添加时间</summary>
        DateTime AddTime { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}