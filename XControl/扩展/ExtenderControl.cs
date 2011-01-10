using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using System.ComponentModel;

namespace XControl
{
    /// <summary>
    /// 泛型扩展控件基类，泛型指定目标控件类型
    /// </summary>
    /// <typeparam name="TTargetControl"></typeparam>
    public abstract class ExtenderControl<TTargetControl> : ExtenderControl where TTargetControl : Control
    {
        #region 属性
        //private TTargetControl _TargetControl;
        /// <summary>目标控件</summary>
        public TTargetControl TargetControl
        {
            get { return FindControl(TargetControlID) as TTargetControl; }
            //set { _TargetControl = value; }
        }
        #endregion
    }

    /// <summary>
    /// 扩展控件基类
    /// </summary>
    public abstract class ExtenderControl : Control
    {
        #region 属性
        private String _TargetControlID;
        /// <summary>目标控件ID</summary>
        [IDReferenceProperty]
        [WebCategory("Behavior")]
        [Description("目标控件ID")]
        public String TargetControlID
        {
            get { return _TargetControlID; }
            set { _TargetControlID = value; }
        }

        /// <summary>
        /// 已重载。用于隐藏Visible属性
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override bool Visible
        {
            get { return base.Visible; }
            set { throw new NotImplementedException(); }
        }

        /// <summary>启用</summary>
        public Boolean Enabled
        {
            get { return GetPropertyValue<Boolean>("Enabled", true); }
            set { SetPropertyValue<Boolean>("Enabled", value); }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 根据ID查找控件
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override Control FindControl(string id)
        {
            Control control = base.FindControl(id);
            if (control != null) return control;

            // 处理容器
            for (Control container = NamingContainer; container != null; container = container.NamingContainer)
            {
                control = container.FindControl(id);
                if (control != null) return control;
            }

            return null;
        }
        #endregion

        #region PropertySupportMethods
        //[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "V", Justification = "V stands for value")]
        //[SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "T", Justification = "V stands for value")]
        /// <summary>
        /// 获取属性值
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        protected V GetPropertyValue<V>(string propertyName, V nullValue)
        {
            if (ViewState[propertyName] == null)
            {
                return nullValue;
            }
            return (V)ViewState[propertyName];
        }

        //[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "V", Justification = "V stands for value")]
        //[SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "T", Justification = "V stands for value")]
        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        protected void SetPropertyValue<V>(string propertyName, V value)
        {
            ViewState[propertyName] = value;
        }
        #endregion
    }
}