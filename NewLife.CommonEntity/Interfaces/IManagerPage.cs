using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using System.Web;
using NewLife.Web;
using System.Web.UI.WebControls;
using System.Linq;

namespace NewLife.CommonEntity
{
    /// <summary>管理页接口，用于控制页面权限等</summary>
    public interface IManagerPage
    {
        /// <summary>
        /// 使用控件容器和实体类初始化接口
        /// </summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        void Init(Control container, Type entityType);
    }

    public class ManagerPage : IManagerPage
    {
        #region 属性
        private Control _Container;
        /// <summary>容器</summary>
        public Control Container
        {
            get { return _Container; }
            set { _Container = value; }
        }

        /// <summary>页面</summary>
        protected Page Page { get { return Container.Page; } }

        private Type _EntityType;
        /// <summary>实体类</summary>
        public Type EntityType
        {
            get { return _EntityType; }
            set { _EntityType = value; }
        }
        #endregion

        #region IManagerPage 成员
        /// <summary>
        /// 使用控件容器和实体类初始化接口
        /// </summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        public void Init(Control container, Type entityType)
        {
            if (container == null)
            {
                if (HttpContext.Current.Handler is Page) container = HttpContext.Current.Handler as Page;
            }

            Container = container;
            EntityType = entityType;

            Init();
        }

        #endregion

        #region 生命周期
        void Init()
        {
            Page.PreLoad += new EventHandler(OnPreLoad);
            Page.LoadComplete += new EventHandler(OnLoadComplete);
        }

        void OnPreLoad(object sender, EventArgs e)
        {

        }

        void OnLoadComplete(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                // 添加按钮需要添加权限
                Control lbAdd = ControlHelper.FindControlInPage<Control>("lbAdd");
                if (lbAdd != null) lbAdd.Visible = Acquire(PermissionFlags.Insert);

                // 最后一列是删除列，需要删除权限
                GridView gv = ControlHelper.FindControlInPage<GridView>("gv");
                if (gv != null)
                {
                    DataControlField dcf = gv.Columns[gv.Columns.Count - 1];
                    if (dcf != null && dcf.HeaderText.Contains("删除")) dcf.Visible = Acquire(PermissionFlags.Delete);

                    dcf = gv.Columns[gv.Columns.Count - 2];
                    if (dcf != null && dcf.HeaderText.Contains("编辑"))
                    {
                        if (!Acquire(PermissionFlags.Update))
                        {
                            dcf.HeaderText = "查看";
                            if (dcf is HyperLinkField) (dcf as HyperLinkField).Text = "查看";
                        }
                    }
                }
            }
        }
        #endregion
    }
}