using System;
using System.ComponentModel.Design;
using System.Web.UI.Design;
using System.Windows.Forms;
using Control = System.Web.UI.Control;

namespace XControl
{
    /// <summary>扩展控件设计时</summary>
    /// <typeparam name="TControl">扩展控件</typeparam>
    /// <typeparam name="TTargetControl">目标控件</typeparam>
    public class ExtenderControlDesigner<TControl, TTargetControl> : ExtenderControlDesigner
        where TControl : ExtenderControl<TTargetControl>
        where TTargetControl : Control
    {
        #region 属性
        /// <summary>目标控件</summary>
        public TTargetControl TargetControl
        {
            get
            {
                ExtenderControl<TTargetControl> ext = this.Component as ExtenderControl<TTargetControl>;
                if (ext == null) return null;

                return ext.TargetControl;
            }
        }
        #endregion

        #region 重载
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override void UpdateDesignTimeHtml()
        {
            base.UpdateDesignTimeHtml();

            TTargetControl tc = TargetControl;
            if (tc != null) UpdateTargetDesignTimeHtml(tc);
        }
        #endregion
    }

    /// <summary>扩展控件设计时</summary>
    public class ExtenderControlDesigner : ControlDesigner
    {
        #region 重载
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string GetDesignTimeHtml()
        {
            return CreatePlaceHolderDesignTimeHtml();
        }
        #endregion

        #region 更新目标设计时
        /// <summary>更新目标设计时</summary>
        /// <param name="component"></param>
        public static void UpdateTargetDesignTimeHtml(Control component)
        {
            if (component == null) return;

            Cursor current = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                ControlDesigner.InvokeTransactedChange(component, UpdateTargetDesignTimeHtmlCallback, component, "更新目标设计时");
            }
            finally
            {
                Cursor.Current = current;
            }
        }

        static Boolean UpdateTargetDesignTimeHtmlCallback(Object state)
        {
            Control component = state as Control;
            if (component == null || component.Site == null) return false;

            IDesignerHost service = component.Site.GetService(typeof(IDesignerHost)) as IDesignerHost;
            if (service == null) return false;

            ControlDesigner designer = service.GetDesigner(component) as ControlDesigner;
            if (designer == null) return false;

            designer.UpdateDesignTimeHtml();

            return true;
        }
        #endregion
    }
}