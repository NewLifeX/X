using System;
using System.ComponentModel;
using NewLife.CommonEntity;
using XCode;
using XCode.DataAccessLayer;

namespace NewLife.YWS.Entities
{
    /// <summary>管理员</summary>
    [Serializable]
    [DataObject]
    [Description("管理员")]
    [BindIndex("IX_Administrator_Name", true, "Name")]
    [BindIndex("IX_Administrator_RoleID", false, "RoleID")]
    [BindIndex("PK__Admin__3214EC277F60ED59", true, "ID")]
    [BindTable("Admin", Description = "管理员", ConnName = "YWS", DbType = DatabaseType.SqlServer)]
    public class Admin : Administrator<Admin, Role, Menu, RoleMenu, NewLife.CommonEntity.Log>
    {
        private String _Phone;
        /// <summary>电话</summary>
        [DisplayName("电话")]
        [Description("电话")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(4, "Phone", "电话", null, "nvarchar(50)", 0, 0, true)]
        public String Phone
        {
            get { return _Phone; }
            set { if (OnPropertyChanging("Phone", value)) { _Phone = value; OnPropertyChanged("Phone"); } }
        }

        private String _QQ;
        /// <summary>QQ</summary>
        [DisplayName("QQ")]
        [Description("QQ")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(5, "QQ", "QQ", null, "nvarchar(50)", 0, 0, true)]
        public String QQ
        {
            get { return _QQ; }
            set { if (OnPropertyChanging("QQ", value)) { _QQ = value; OnPropertyChanged("QQ"); } }
        }

        private String _MSN;
        /// <summary>MSN</summary>
        [DisplayName("MSN")]
        [Description("MSN")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(6, "MSN", "MSN", null, "nvarchar(50)", 0, 0, true)]
        public String MSN
        {
            get { return _MSN; }
            set { if (OnPropertyChanging("MSN", value)) { _MSN = value; OnPropertyChanged("MSN"); } }
        }

        private String _Email;
        /// <summary>邮件</summary>
        [DisplayName("邮件")]
        [Description("邮件")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn(7, "Email", "邮件", null, "nvarchar(50)", 0, 0, true)]
        public String Email
        {
            get { return _Email; }
            set { if (OnPropertyChanging("Email", value)) { _Email = value; OnPropertyChanged("Email"); } }
        }
    }
}