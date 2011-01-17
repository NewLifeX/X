using System;
using System.ComponentModel.Design;
using System.Web.UI.Design;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using System.Web.UI.Design.WebControls;

namespace XControl
{
    /// <summary>
    /// GridView扩展控件设计时
    /// </summary>
    public class GridViewExtenderDesigner : ExtenderControlDesigner<GridViewExtender, GridView>
    {
        #region 智能标记
        /// <summary>
        /// 已重载。
        /// </summary>
        public override DesignerActionListCollection ActionLists
        {
            get
            {
                DesignerActionListCollection lists = new DesignerActionListCollection();
                lists.AddRange(base.ActionLists);
                lists.Add(new GridViewExtenderActionList(this));
                return lists;
            }
        }

        class GridViewExtenderActionList : DesignerActionList
        {
            private GridViewExtenderDesigner _parent;

            public GridViewExtenderActionList(GridViewExtenderDesigner parent)
                : base(parent.Component)
            {
                _parent = parent;
            }

            /// <summary>
            /// 已重载。
            /// </summary>
            /// <returns></returns>
            public override DesignerActionItemCollection GetSortedActionItems()
            {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                //if (_parent.CanConfigure)
                {
                    DesignerActionMethodItem item = new DesignerActionMethodItem(this, "SetPagerTemplate", "设置分页模版", null, "给目标GridView设置一个默认分页模版", true);
                    item.AllowAssociate = true;
                    items.Add(item);
                }
                return items;
            }

            /// <summary>
            /// 是否自动显示
            /// </summary>
            public override bool AutoShow { get { return true; } set { } }

            void SetPagerTemplate()
            {
                _parent.SetPagerTemplate();
            }
        }
        #endregion

        #region 设置分页模版
        /// <summary>
        /// 设置分页模版
        /// </summary>
        public void SetPagerTemplate()
        {
            Cursor current = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                ControlDesigner.InvokeTransactedChange(base.Component, SetPagerTemplateCallback, null, "设置分页模版");
                UpdateDesignTimeHtml();
            }
            finally
            {
                Cursor.Current = current;
            }
        }

        Boolean SetPagerTemplateCallback(Object state)
        {
            GridViewExtender gve = this.Component as GridViewExtender;
            if (gve == null) return false;

            GridView gv = gve.TargetControl;
            if (gv == null) return false;

            IDesignerHost service = gv.Site.GetService(typeof(IDesignerHost)) as IDesignerHost;
            if (service == null) return false;

            if (gv.PagerTemplate == null || MessageBox.Show("是否覆盖原有模版？", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                //gv.PagerTemplate = ControlParser.ParseTemplate(service, GridViewExtender.PagerTemplateString);

                GridViewDesigner designer = service.GetDesigner(gv) as GridViewDesigner;
                if (designer != null)
                {
                    if (designer.TemplateGroups.Count > 0)
                    {
                        foreach (TemplateGroup item in designer.TemplateGroups)
                        {
                            if (item.GroupName != "PagerTemplate") continue;
                            if (item.Templates == null || item.Templates.Length < 1) continue;

                            item.Templates[0].Content = GridViewExtender.PagerTemplateString;
                        }
                    }
                    designer.UpdateDesignTimeHtml();
                }
            }

            return true;
        }
        #endregion
    }
}