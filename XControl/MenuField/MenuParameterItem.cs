using System;
using System.ComponentModel;
using System.Web.UI;

namespace XControl
{
    /// <summary>菜单项</summary>
    public class MenuParameterItem : IStateManager, IViewState
    {
        #region 属性

        /// <summary>菜单项文本</summary>
        [DefaultValue("#")]
        public String Text
        {
            get { return (string)((IViewState)this).ViewState["Text"]; }
            set { ((IViewState)this).ViewState["Text"] = value; }
        }

        /// <summary>菜单项连接</summary>
        [DefaultValue("#"), WebCategory("MenuItem"), WebSysDescription("MenuItem_Url")]
        public String Url
        {
            get { return (string)((IViewState)this).ViewState["Url"]; }
            set { ((IViewState)this).ViewState["Url"] = value; }
        }

        /// <summary>菜单项事件</summary>
        public String OnClick
        {
            get { return (string)((IViewState)this).ViewState["OnClick"]; }
            set { ((IViewState)this).ViewState["OnClick"] = value; }
        }

        /// <summary>菜单项ICon样式</summary>
        public String IConCss
        {
            get { return (string)((IViewState)this).ViewState["IConCss"]; }
            set { ((IViewState)this).ViewState["IConCss"] = value; }
        }

        #endregion

        #region 内部属性

        private StateBag _ViewState;
        StateBag IViewState.ViewState
        {
            get
            {
                if (_ViewState == null)
                {
                    _ViewState = new StateBag();
                    if (IsTrackingViewState)
                    {
                        ((IStateManager)_ViewState).TrackViewState();
                    }
                }
                return _ViewState;
            }
        }

        #endregion

        #region 构造方法

        /// <summary>构造方法</summary>
        /// <param name="original"></param>
        protected MenuParameterItem(MenuParameterItem original)
        {
            this.Text = original.Text;
            this.Url = original.Url;
            this.OnClick = original.OnClick;
            this.IConCss = original.IConCss;
        }

        /// <summary>构造方法</summary>
        public MenuParameterItem()
        { }

        /// <summary>构造方法</summary>
        /// <param name="text"></param>
        /// <param name="url"></param>
        /// <param name="onclick"></param>
        /// <param name="icon"></param>
        public MenuParameterItem(String text, String url, String onclick, String icon)
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

        #region 实现IStateManager接口

        private bool _IsTrackingViewState;
        /// <summary>
        /// 实现IStateManager接口
        /// </summary>
        public bool IsTrackingViewState
        {
            get { return _IsTrackingViewState; }
        }
        /// <summary>
        /// 实现IStateManager接口
        /// </summary>
        /// <param name="state"></param>
        public void LoadViewState(object state)
        {
            var st = (object[])state;
            if (st.Length != 1) throw new Exception("无效的MenuParameterItem视图状态");
            ((IStateManager)((IViewState)this).ViewState).LoadViewState(st[0]);
        }
        /// <summary>
        /// 实现IStateManager接口
        /// </summary>
        /// <returns></returns>
        public object SaveViewState()
        {
            var st = new object[1];
            if (_ViewState != null)
            {
                st[0] = ((IStateManager)((IViewState)this).ViewState).SaveViewState();
            }
            return st;
        }
        /// <summary>
        /// 实现IStateManager接口
        /// </summary>
        public void TrackViewState()
        {
            _IsTrackingViewState = true;
            if (_ViewState != null)
            {
                ((IStateManager)((IViewState)this).ViewState).TrackViewState();
            }
        }

        #endregion
    }
}