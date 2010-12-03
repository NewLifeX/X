using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using System.Web.UI;
using System.ComponentModel;

namespace XControl
{
    /// <summary>
    /// 连接按钮字段
    /// </summary>
    public class LinkButtonField : HyperLinkField
    {
        #region 属性
        /// <summary>客户端点击事件</summary>
        [DefaultValue(""), Themeable(false), WebCategory("Behavior"), WebSysDescription("Button_OnClientClick")]
        public virtual String OnClientClick
        {
            get
            {
                String str = (String)ViewState["OnClientClick"];
                if (str == null) return String.Empty;

                return str;
            }
            set
            {
                ViewState["OnClientClick"] = value;
            }
        }
        #endregion

        /// <summary>
        /// 建立字段
        /// </summary>
        /// <returns></returns>
        protected override DataControlField CreateField()
        {
            return new LinkButtonField();
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="newField"></param>
        protected override void CopyProperties(DataControlField newField)
        {
            ((LinkButtonField)newField).OnClientClick = this.OnClientClick;
            base.CopyProperties(newField);
        }

        /// <summary>
        /// 初始化单元格
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="cellType"></param>
        /// <param name="rowState"></param>
        /// <param name="rowIndex"></param>
        public override void InitializeCell(DataControlFieldCell cell, DataControlCellType cellType, DataControlRowState rowState, int rowIndex)
        {
            base.InitializeCell(cell, cellType, rowState, rowIndex);
            if (cell.Controls.Count < 1) return;

            HyperLink link = cell.Controls[cell.Controls.Count - 1] as HyperLink;
            if (link == null) return;
            link.PreRender += new EventHandler(link_PreRender);

            InitializeControl(link);
        }

        /// <summary>
        /// 初始化链接控件
        /// </summary>
        /// <param name="link"></param>
        protected virtual void InitializeControl(HyperLink link)
        {
            //if (!String.IsNullOrEmpty(OnClientClick)) link.Attributes.Add("onclick", OnClientClick);
        }

        void link_PreRender(object sender, EventArgs e)
        {
            HyperLink link = sender as HyperLink;
            if (link == null) return;

            OnPreRender(link);
        }

        /// <summary>
        /// 呈现控件时
        /// </summary>
        /// <param name="link"></param>
        protected virtual void OnPreRender(HyperLink link)
        {
            if (!String.IsNullOrEmpty(OnClientClick)) link.Attributes.Add("onclick", OnClientClick);
        }
    }
}
