using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using System.ComponentModel;
using System.Web;
using System.Security.Permissions;

namespace XControl
{
    /// <summary>分页模版项</summary>
    [ToolboxItem(false)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public class DataPagerItem : Control, IDataItemContainer, INamingContainer
    {
        private Object _DataItem;
        /// <summary>数据项</summary>
        public Object DataItem
        {
            get { return _DataItem; }
            set { _DataItem = value; }
        }

        private Int32 _DataItemIndex;
        /// <summary>数据索引</summary>
        public Int32 DataItemIndex
        {
            get { return _DataItemIndex; }
            set { _DataItemIndex = value; }
        }

        private Int32 _DisplayIndex;
        /// <summary>显示索引</summary>
        public Int32 DisplayIndex
        {
            get { return _DisplayIndex; }
            set { _DisplayIndex = value; }
        }
    }
}
