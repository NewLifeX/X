using System;
using System.ComponentModel;
using System.Web.UI;

namespace XControl
{
    /// <summary>菜单模版项</summary>
    public class MenuTemplateItem : IStateManager, IViewState
    {
        /// <summary>条件值</summary>
        public String ConditionFieldValue
        {
            get { return (string)((IViewState)this).ViewState["ConditionFieldValue"]; }
            set { ((IViewState)this).ViewState["ConditionFieldValue"] = value; }
        }

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

        /// <summary>属性说明</summary>
        [WebSysDescription("TemplateField_InsertItemTemplate"), PersistenceMode(PersistenceMode.InnerProperty), TemplateContainer(typeof(IDataItemContainer), BindingDirection.TwoWay), DefaultValue((string)null), Browsable(false)]
        public ITemplate Template
        {
            get { return (ITemplate)((IViewState)this).ViewState["Template"]; }
            set { ((IViewState)this).ViewState["Template"] = value; }
        }

        //[PersistenceMode(PersistenceMode.InnerProperty), Browsable(false)]
        //public virtual String HeaderTemplate
        //{
        //    get
        //    {
        //        return null;
        //    }
        //    set
        //    {
        //    }
        //}

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
            if (st.Length != 1) throw new Exception("无效的MenuTemplateItem视图状态");
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