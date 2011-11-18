using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace XControl
{
    /// <summary>
    /// 菜单项
    /// </summary>
    public class MenuItem
    {
        #region 属性

        private String _Text = "无文本";

        /// <summary>
        /// 菜单项文本
        /// </summary>
        public String Text
        {
            get { return _Text; }
            set { _Text = value; }
        }

        private String _Url = "#";
        /// <summary>
        /// 菜单项连接
        /// </summary>
        [DefaultValue("#"), WebCategory("MenuItem"), WebSysDescription("MenuItem_Url")]
        public String Url
        {
            get { return _Url; }
            set { _Url = value; }
        }

        private String _OnClick;
        /// <summary>
        /// 菜单项事件
        /// </summary>
        public String OnClick
        {
            get { return _OnClick; }
            set { _OnClick = value; }
        }

        private String _IConCss;
        /// <summary>
        /// 菜单项ICon样式
        /// </summary>
        public String IConCss
        {
            get { return _IConCss; }
            set { _IConCss = value; }
        }
        #endregion

        #region 内部属性

        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="original"></param>
        protected MenuItem(MenuItem original)
        {
            this.Text = original.Text;
            this.Url = original.Url;
            this.OnClick = original.OnClick;
            this.IConCss = original.IConCss;
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public MenuItem()
        { }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="text"></param>
        /// <param name="url"></param>
        /// <param name="onclick"></param>
        /// <param name="icon"></param>
        public MenuItem(String text, String url, String onclick, String icon)
        {
            this.Text = text;
            this.Url = url;
            this.OnClick = onclick;
            this.IConCss = icon;
        }
        #endregion


        #region 接口实现
        ///// <summary>
        ///// 重构
        ///// </summary>
        ///// <returns></returns>
        //protected virtual MenuItem Clone()
        //{
        //    return new MenuItem(this);
        //}

        ///// <summary>
        ///// Clone
        ///// </summary>
        ///// <returns></returns>
        //object ICloneable.Clone()
        //{
        //    return this.Clone();
        //}

        ///// <summary>
        ///// LoadViewState
        ///// </summary>
        ///// <param name="savedState"></param>
        //void IStateManager.LoadViewState(object savedState)
        //{
        //    LoadViewState(savedState);
        //}

        ///// <summary>
        ///// IStateManager
        ///// </summary>
        ///// <returns></returns>
        //object IStateManager.SaveViewState()
        //{
        //    return SaveViewState();
        //}

        ///// <summary>
        ///// TrackViewState
        ///// </summary>
        //void IStateManager.TrackViewState()
        //{
        //    TrackViewState();
        //}

        ///// <summary>
        ///// IsTrackingViewState
        ///// </summary>
        //bool IStateManager.IsTrackingViewState
        //{
        //    get
        //    {
        //        return this.IsTrackingViewState;
        //    }
        //}

        ///// <summary>
        ///// 服务器控件跟踪其视图状态更改
        ///// </summary>
        //protected virtual void TrackViewState()
        //{
        //    this._tracking = true;
        //    if (ViewState != null)
        //    {
        //        ViewState.TrackViewState();
        //    }
        //}

        ///// <summary>
        ///// 服务器控件是否正在跟踪其视图状态更改
        ///// </summary>
        //protected bool IsTrackingViewState
        //{
        //    get
        //    {
        //        return _tracking;
        //    }
        //}

        ///// <summary>
        ///// 将服务器控件的视图状态更改保存到 System.Object
        ///// </summary>
        ///// <returns>包含视图状态更改的 System.Object</returns>
        //protected virtual object SaveViewState()
        //{
        //    if (ViewState == null)
        //    {
        //        return null;
        //    }
        //    return ViewState.SaveViewState();
        //}

        ///// <summary>
        ///// 加载服务器控件以前保存的控件视图状态
        ///// </summary>
        ///// <param name="savedState"></param>
        //protected virtual void LoadViewState(object savedState)
        //{
        //    if (savedState != null)
        //    {
        //        ViewState.LoadViewState(savedState);
        //    }
        //}
        #endregion


    }

}
