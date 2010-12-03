using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace XControl
{
    /// <summary>
    /// 分页控件
    /// </summary>
    [SupportsEventValidation]
    [Themeable(true)]
    [PersistChildren(false)]
    [ParseChildren(true)]
    [Designer(typeof(DataPagerDesigner))]
    public class DataPager : CompositeControl, INamingContainer, IPostBackContainer, IPostBackEventHandler, IPagedDataSource
    {
        #region 属性
        /// <summary>
        /// 页数
        /// </summary>
        [Browsable(false), WebSysDescription("GridView_PageCount"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Int32 PageCount
        {
            get
            {
                if (TotalRowCount <= 0) return 1;
                return (Int32)Math.Ceiling((Double)TotalRowCount / PageSize);
            }
        }

        /// <summary>
        /// 页数
        /// </summary>
        [WebCategory("Paging"), Browsable(false), DefaultValue(0), Description("总记录数"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Int32 TotalRowCount
        {
            get
            {
                object obj2 = ViewState["TotalRowCount"];
                if (obj2 != null) return (Int32)obj2;

                return 0;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                ViewState["TotalRowCount"] = value;

                ChildControlsCreated = false;
            }
        }

        /// <summary>
        /// 缓存页数
        /// </summary>
        [WebCategory("Paging"), Browsable(true), DefaultValue(true), Description("是否缓存总记录数")]
        public virtual Boolean TotalRowCountCache
        {
            get
            {
                object obj2 = ViewState["TotalRowCountCache"];
                if (obj2 != null) return (Boolean)obj2;

                return true;
            }
            set
            {
                ViewState["TotalRowCountCache"] = value;
            }
        }

        //private Int32 _PageIndex;
        /// <summary>
        /// 当前页
        /// </summary>
        [WebCategory("Paging"), Browsable(true), DefaultValue(0), WebSysDescription("GridView_PageIndex")]
        public virtual Int32 PageIndex
        {
            get
            {
                object obj2 = ViewState["PageIndex"];
                if (obj2 != null) return (Int32)obj2;

                return 0;

                //return _PageIndex;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                ViewState["PageIndex"] = value;

                //_PageIndex = value;
            }
        }

        /// <summary>
        /// 当前页，等于PageIndex+1
        /// </summary>
        public Int32 PageIndex2 { get { return PageIndex + 1; } set { PageIndex = value - 1; } }

        private Int32 _PageSize = 10;
        /// <summary>
        /// 页大小
        /// </summary>
        [WebCategory("Paging"), WebSysDescription("GridView_PageSize"), DefaultValue(10)]
        public virtual Int32 PageSize
        {
            get
            {
                //object obj2 = ViewState["PageSize"];
                //if (obj2 != null) return (Int32)obj2;

                //return 10;

                return _PageSize;
            }
            set
            {
                //if (value < 1) throw new ArgumentOutOfRangeException("value");

                //ViewState["PageSize"] = value;

                _PageSize = value;
            }
        }

        PagerSettings _pagerSettings;
        /// <summary>
        /// 分页设置
        /// </summary>
        [WebCategory("Paging"), WebSysDescription("GridView_PagerSettings"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content), NotifyParentProperty(true), PersistenceMode(PersistenceMode.InnerProperty)]
        public virtual PagerSettings PagerSettings
        {
            get
            {
                if (_pagerSettings == null)
                {
                    _pagerSettings = new PagerSettings();
                    if (base.IsTrackingViewState)
                    {
                        ((IStateManager)_pagerSettings).TrackViewState();
                    }
                    //_pagerSettings.PropertyChanged += new EventHandler(OnPagerPropertyChanged);
                }
                return _pagerSettings;
            }
        }

        TableItemStyle _pagerStyle;
        /// <summary>
        /// 分页样式
        /// </summary>
        [WebCategory("Styles"), WebSysDescription("WebControl_PagerStyle"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content), NotifyParentProperty(true), PersistenceMode(PersistenceMode.InnerProperty)]
        public TableItemStyle PagerStyle
        {
            get
            {
                if (_pagerStyle == null)
                {
                    _pagerStyle = new TableItemStyle();
                    if (base.IsTrackingViewState)
                    {
                        ((IStateManager)_pagerStyle).TrackViewState();
                    }
                }
                return _pagerStyle;
            }
        }

        ITemplate _pagerTemplate;
        /// <summary>
        /// 分页模版
        /// </summary>
        [WebSysDescription("View_PagerTemplate"), Browsable(false), DefaultValue((string)null), PersistenceMode(PersistenceMode.InnerProperty), TemplateContainer(typeof(DataPagerItem))]
        public virtual ITemplate PagerTemplate
        {
            get
            {
                return _pagerTemplate;
            }
            set
            {
                _pagerTemplate = value;
            }
        }

        String _DataSourceID;
        /// <summary>
        /// 数据源
        /// </summary>
        [WebSysDescription("BaseDataBoundControl_DataSourceID")]
        [DefaultValue("")]
        [IDReferenceProperty(typeof(DataSourceControl))]
        [WebCategory("Data")]
        public String DataSourceID
        {
            get
            {
                //object obj2 = this.ViewState["DataSourceID"];
                //if (obj2 != null) return (string)obj2;

                //return string.Empty;

                return _DataSourceID;
            }
            set
            {
                //this.ViewState["DataSourceID"] = value;

                //_DataSourceID = value;

                if (value != DataSourceID)
                {
                    if (value == SR.GetString("DataSourceIDChromeConverter_NewDataSource"))
                    {
                        //this.CreateDataSource();
                        value = string.Empty;
                    }
                    else
                    {
                        if (value == SR.GetString("DataSourceIDChromeConverter_NoDataSource"))
                        {
                            value = string.Empty;
                        }
                        //RequiresDataBinding = true;
                    }
                    //ViewState["DataSourceID"] = value;
                    _DataSourceID = value;
                }
            }
        }

        //private static Int32 tid = 0;
        //public Int32 TID = ++tid;

        //public DataPager()
        //{
        //    XTrace.WriteLine("DataPager {0}", TID);
        //    XTrace.DebugStack();
        //}
        #endregion

        #region 事件
        private static readonly Object EventPageIndexChanged = new Object();
        /// <summary>
        /// 处理分页操作之后发生
        /// </summary>
        [WebSysDescription("GridView_OnPageIndexChanged"), WebCategory("Action")]
        public event EventHandler PageIndexChanged
        {
            add
            {
                base.Events.AddHandler(EventPageIndexChanged, value);
            }
            remove
            {
                base.Events.RemoveHandler(EventPageIndexChanged, value);
            }
        }

        private static readonly Object EventPageIndexChanging = new Object();
        /// <summary>
        /// 处理分页操作之前发生
        /// </summary>
        [WebSysDescription("GridView_OnPageIndexChanging"), WebCategory("Action")]
        public event GridViewPageEventHandler PageIndexChanging
        {
            add
            {
                base.Events.AddHandler(EventPageIndexChanging, value);
            }
            remove
            {
                base.Events.RemoveHandler(EventPageIndexChanging, value);
            }
        }

        private static readonly Object EventCommand = new Object();
        /// <summary>
        /// 当单击控件中的按钮时发生
        /// </summary>
        [WebSysDescription("GridView_OnRowCommand"), WebCategory("Action")]
        public event CommandEventHandler PageCommand
        {
            add
            {
                base.Events.AddHandler(EventCommand, value);
            }
            remove
            {
                base.Events.RemoveHandler(EventCommand, value);
            }
        }

        /// <summary>
        /// 引发 PageIndexChanging 事件
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPageIndexChanging(GridViewPageEventArgs e)
        {
            //bool isBoundUsingDataSourceID = base.IsBoundUsingDataSourceID;
            GridViewPageEventHandler handler = (GridViewPageEventHandler)base.Events[EventPageIndexChanging];
            if (handler != null)
            {
                handler(this, e);
            }
            //else if (!isBoundUsingDataSourceID && !e.Cancel)
            //{
            //    throw new HttpException(SR.GetString("GridView_UnhandledEvent", new object[] { ID, "PageIndexChanging" }));
            //}
        }

        /// <summary>
        /// 引发 PageIndexChanged 事件
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPageIndexChanged(EventArgs e)
        {
            ChildControlsCreated = false;

            EventHandler handler = (EventHandler)base.Events[EventPageIndexChanged];
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// 引发 Command 事件
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCommand(CommandEventArgs e)
        {
            CommandEventHandler handler = (CommandEventHandler)base.Events[EventCommand];
            if (handler != null)
            {
                handler(this, e);
            }
        }
        #endregion

        #region 子控件
        /// <summary>
        /// 建立子控件
        /// </summary>
        protected override void CreateChildControls()
        {
            // 当且仅当总记录数TotalRowCount准备好之后才创建子控件
            if (TotalRowCount <= 0) return;

            Controls.Clear();

            IPagedDataSource pagedDataSource = this;

            PagerSettings pagerSettings = PagerSettings;
            if (PagerTemplate != null)
            {
                // 分页模版输出
                DataPagerItem item = new DataPagerItem();
                PagerTemplate.InstantiateIn(item);

                Controls.Add(item);

                // 必须在加入Controls后再绑定，否则会因为没有关联Page而失败
                // 每次完成创建子控件后马上绑定
                item.DataItem = this;
                item.DataBind();
                item.DataItem = null;
            }
            else
            {
                // 分页控件输出
                Table child = new Table();
                TableRow row2 = new TableRow();
                switch (pagerSettings.Mode)
                {
                    case PagerButtons.NextPrevious:
                        CreateNextPrevPager(row2, pagedDataSource, false);
                        break;

                    case PagerButtons.Numeric:
                        CreateNumericPager(row2, pagedDataSource, false);
                        break;

                    case PagerButtons.NextPreviousFirstLast:
                        CreateNextPrevPager(row2, pagedDataSource, true);
                        break;

                    case PagerButtons.NumericFirstLast:
                        CreateNumericPager(row2, pagedDataSource, true);
                        break;
                }

                child.Rows.Add(row2);
                Controls.Add(child);
            }
        }

        ///// <summary>
        ///// 已重写。保证创建子控件
        ///// </summary>
        //public override ControlCollection Controls
        //{
        //    get
        //    {
        //        this.EnsureChildControls();
        //        return base.Controls;
        //    }
        //}
        #endregion

        #region 四种分页
        private void CreateNextPrevPager(TableRow row, IPagedDataSource pagedDataSource, bool addFirstLastPageButtons)
        {
            PagerSettings pagerSettings = PagerSettings;
            string previousPageImageUrl = pagerSettings.PreviousPageImageUrl;
            string nextPageImageUrl = pagerSettings.NextPageImageUrl;
            bool isFirstPage = pagedDataSource.IsFirstPage;
            bool isLastPage = pagedDataSource.IsLastPage;
            if (addFirstLastPageButtons && !isFirstPage)
            {
                IButtonControl control;
                TableCell cell = new TableCell();
                row.Cells.Add(cell);
                string firstPageImageUrl = pagerSettings.FirstPageImageUrl;
                if (firstPageImageUrl.Length > 0)
                {
                    control = new DataControlImageButton(this);
                    ((DataControlImageButton)control).ImageUrl = firstPageImageUrl;
                    ((DataControlImageButton)control).AlternateText = HttpUtility.HtmlDecode(pagerSettings.FirstPageText);
                    ((DataControlImageButton)control).EnableCallback(BuildCallbackArgument(0));
                }
                else
                {
                    control = new DataControlPagerLinkButton(this);
                    ((DataControlPagerLinkButton)control).Text = pagerSettings.FirstPageText;
                    ((DataControlPagerLinkButton)control).EnableCallback(BuildCallbackArgument(0));
                }
                control.CommandName = "Page";
                control.CommandArgument = "First";
                cell.Controls.Add((Control)control);
            }
            if (!isFirstPage)
            {
                IButtonControl control2;
                TableCell cell2 = new TableCell();
                row.Cells.Add(cell2);
                if (previousPageImageUrl.Length > 0)
                {
                    control2 = new DataControlImageButton(this);
                    ((DataControlImageButton)control2).ImageUrl = previousPageImageUrl;
                    ((DataControlImageButton)control2).AlternateText = HttpUtility.HtmlDecode(pagerSettings.PreviousPageText);
                    ((DataControlImageButton)control2).EnableCallback(BuildCallbackArgument(PageIndex - 1));
                }
                else
                {
                    control2 = new DataControlPagerLinkButton(this);
                    ((DataControlPagerLinkButton)control2).Text = pagerSettings.PreviousPageText;
                    ((DataControlPagerLinkButton)control2).EnableCallback(BuildCallbackArgument(PageIndex - 1));
                }
                control2.CommandName = "Page";
                control2.CommandArgument = "Prev";
                cell2.Controls.Add((Control)control2);
            }
            if (!isLastPage)
            {
                IButtonControl control3;
                TableCell cell3 = new TableCell();
                row.Cells.Add(cell3);
                if (nextPageImageUrl.Length > 0)
                {
                    control3 = new DataControlImageButton(this);
                    ((DataControlImageButton)control3).ImageUrl = nextPageImageUrl;
                    ((DataControlImageButton)control3).AlternateText = HttpUtility.HtmlDecode(pagerSettings.NextPageText);
                    ((DataControlImageButton)control3).EnableCallback(BuildCallbackArgument(PageIndex + 1));
                }
                else
                {
                    control3 = new DataControlPagerLinkButton(this);
                    ((DataControlPagerLinkButton)control3).Text = pagerSettings.NextPageText;
                    ((DataControlPagerLinkButton)control3).EnableCallback(BuildCallbackArgument(PageIndex + 1));
                }
                control3.CommandName = "Page";
                control3.CommandArgument = "Next";
                cell3.Controls.Add((Control)control3);
            }
            if (addFirstLastPageButtons && !isLastPage)
            {
                IButtonControl control4;
                TableCell cell4 = new TableCell();
                row.Cells.Add(cell4);
                string lastPageImageUrl = pagerSettings.LastPageImageUrl;
                if (lastPageImageUrl.Length > 0)
                {
                    control4 = new DataControlImageButton(this);
                    ((DataControlImageButton)control4).ImageUrl = lastPageImageUrl;
                    ((DataControlImageButton)control4).AlternateText = HttpUtility.HtmlDecode(pagerSettings.LastPageText);
                    ((DataControlImageButton)control4).EnableCallback(BuildCallbackArgument(pagedDataSource.PageCount - 1));
                }
                else
                {
                    control4 = new DataControlPagerLinkButton(this);
                    ((DataControlPagerLinkButton)control4).Text = pagerSettings.LastPageText;
                    ((DataControlPagerLinkButton)control4).EnableCallback(BuildCallbackArgument(pagedDataSource.PageCount - 1));
                }
                control4.CommandName = "Page";
                control4.CommandArgument = "Last";
                cell4.Controls.Add((Control)control4);
            }
        }

        private void CreateNumericPager(TableRow row, IPagedDataSource pagedDataSource, bool addFirstLastPageButtons)
        {
            LinkButton button;
            PagerSettings pagerSettings = PagerSettings;
            int pageCount = pagedDataSource.PageCount;
            int num2 = pagedDataSource.PageIndex + 1;
            int pageButtonCount = pagerSettings.PageButtonCount;
            int num4 = pageButtonCount;
            Int32 FirstDisplayedPageIndex = 0;
            int num5 = FirstDisplayedPageIndex + 1;
            if (pageCount < num4)
            {
                num4 = pageCount;
            }
            int num6 = 1;
            int pageIndex = num4;
            if (num2 > pageIndex)
            {
                int num8 = pagedDataSource.PageIndex / pageButtonCount;
                bool flag = ((num2 - num5) >= 0) && ((num2 - num5) < pageButtonCount);
                if ((num5 > 0) && flag)
                {
                    num6 = num5;
                }
                else
                {
                    num6 = (num8 * pageButtonCount) + 1;
                }
                pageIndex = (num6 + pageButtonCount) - 1;
                if (pageIndex > pageCount)
                {
                    pageIndex = pageCount;
                }
                if (((pageIndex - num6) + 1) < pageButtonCount)
                {
                    num6 = Math.Max(1, (pageIndex - pageButtonCount) + 1);
                }
                FirstDisplayedPageIndex = num6 - 1;
            }
            if ((addFirstLastPageButtons && (num2 != 1)) && (num6 != 1))
            {
                IButtonControl control;
                TableCell cell = new TableCell();
                row.Cells.Add(cell);
                string firstPageImageUrl = pagerSettings.FirstPageImageUrl;
                if (firstPageImageUrl.Length > 0)
                {
                    control = new DataControlImageButton(this);
                    ((DataControlImageButton)control).ImageUrl = firstPageImageUrl;
                    ((DataControlImageButton)control).AlternateText = HttpUtility.HtmlDecode(pagerSettings.FirstPageText);
                    ((DataControlImageButton)control).EnableCallback(BuildCallbackArgument(0));
                }
                else
                {
                    control = new DataControlPagerLinkButton(this);
                    ((DataControlPagerLinkButton)control).Text = pagerSettings.FirstPageText;
                    ((DataControlPagerLinkButton)control).EnableCallback(BuildCallbackArgument(0));
                }
                control.CommandName = "Page";
                control.CommandArgument = "First";
                cell.Controls.Add((Control)control);
            }
            if (num6 != 1)
            {
                TableCell cell2 = new TableCell();
                row.Cells.Add(cell2);
                button = new DataControlPagerLinkButton(this);
                button.Text = "...";
                button.CommandName = "Page";
                button.CommandArgument = (num6 - 1).ToString(NumberFormatInfo.InvariantInfo);
                ((DataControlPagerLinkButton)button).EnableCallback(BuildCallbackArgument(num6 - 2));
                cell2.Controls.Add(button);
            }
            for (int i = num6; i <= pageIndex; i++)
            {
                TableCell cell3 = new TableCell();
                row.Cells.Add(cell3);
                string str2 = i.ToString(NumberFormatInfo.InvariantInfo);
                if (i == num2)
                {
                    Label child = new Label();
                    child.Text = str2;
                    cell3.Controls.Add(child);
                }
                else
                {
                    button = new DataControlPagerLinkButton(this);
                    button.Text = str2;
                    button.CommandName = "Page";
                    button.CommandArgument = str2;
                    ((DataControlPagerLinkButton)button).EnableCallback(BuildCallbackArgument(i - 1));
                    cell3.Controls.Add(button);
                }
            }
            if (pageCount > pageIndex)
            {
                TableCell cell4 = new TableCell();
                row.Cells.Add(cell4);
                button = new DataControlPagerLinkButton(this);
                button.Text = "...";
                button.CommandName = "Page";
                button.CommandArgument = (pageIndex + 1).ToString(NumberFormatInfo.InvariantInfo);
                ((DataControlPagerLinkButton)button).EnableCallback(BuildCallbackArgument(pageIndex));
                cell4.Controls.Add(button);
            }
            bool flag2 = pageIndex == pageCount;
            if ((addFirstLastPageButtons && (num2 != pageCount)) && !flag2)
            {
                IButtonControl control2;
                TableCell cell5 = new TableCell();
                row.Cells.Add(cell5);
                string lastPageImageUrl = pagerSettings.LastPageImageUrl;
                if (lastPageImageUrl.Length > 0)
                {
                    control2 = new DataControlImageButton(this);
                    ((DataControlImageButton)control2).ImageUrl = lastPageImageUrl;
                    ((DataControlImageButton)control2).AlternateText = HttpUtility.HtmlDecode(pagerSettings.LastPageText);
                    ((DataControlImageButton)control2).EnableCallback(BuildCallbackArgument(pagedDataSource.PageCount - 1));
                }
                else
                {
                    control2 = new DataControlPagerLinkButton(this);
                    ((DataControlPagerLinkButton)control2).Text = pagerSettings.LastPageText;
                    ((DataControlPagerLinkButton)control2).EnableCallback(BuildCallbackArgument(pagedDataSource.PageCount - 1));
                }
                control2.CommandName = "Page";
                control2.CommandArgument = "Last";
                cell5.Controls.Add((Control)control2);
            }
        }

        private string BuildCallbackArgument(int pageIndex)
        {
            return string.Concat(new object[] { "\"", pageIndex, "|\"" });
        }
        #endregion

        #region 分页
        /// <summary>
        /// 处理分页事件
        /// </summary>
        /// <param name="newPage"></param>
        private void HandlePage(int newPage)
        {
            GridViewPageEventArgs e = new GridViewPageEventArgs(newPage);
            OnPageIndexChanging(e);
            if (!e.Cancel)
            {
                if (e.NewPageIndex <= -1) return;

                if ((e.NewPageIndex >= PageCount) && (PageIndex == (PageCount - 1))) return;

                PageIndex = e.NewPageIndex;
                OnPageIndexChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// 仅处理分页事件
        /// </summary>
        /// <param name="e"></param>
        /// <param name="causesValidation"></param>
        /// <param name="validationGroup"></param>
        /// <returns></returns>
        private bool HandleEvent(EventArgs e, bool causesValidation, string validationGroup)
        {
            bool flag = false;
            if (causesValidation) Page.Validate(validationGroup);

            CommandEventArgs args = e as CommandEventArgs;
            if (args == null) return flag;

            OnCommand(args);
            flag = true;
            string commandName = args.CommandName;
            if (!StringUtil.EqualsIgnoreCase(commandName, "Page")) return flag;

            string commandArgument = (string)args.CommandArgument;
            int pageIndex = PageIndex;
            if (StringUtil.EqualsIgnoreCase(commandArgument, "Next"))
            {
                pageIndex++;
            }
            else if (StringUtil.EqualsIgnoreCase(commandArgument, "Prev"))
            {
                pageIndex--;
            }
            else if (StringUtil.EqualsIgnoreCase(commandArgument, "First"))
            {
                pageIndex = 0;
            }
            else if (StringUtil.EqualsIgnoreCase(commandArgument, "Last"))
            {
                pageIndex = PageCount - 1;
            }
            else
            {
                pageIndex = Convert.ToInt32(commandArgument, CultureInfo.InvariantCulture) - 1;
            }
            HandlePage(pageIndex);
            return flag;
        }

        /// <summary>
        /// 已重载。确定 Web 服务器控件的事件是否沿页的用户界面 (UI) 服务器控件层次结构向上传递。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        protected override bool OnBubbleEvent(object source, EventArgs e)
        {
            bool causesValidation = false;
            string validationGroup = string.Empty;
            CommandEventArgs args = e as CommandEventArgs;
            if (args != null)
            {
                //IButtonControl commandSource = args.CommandSource as IButtonControl;
                //if (commandSource != null)
                //{
                //    causesValidation = commandSource.CausesValidation;
                //    validationGroup = commandSource.ValidationGroup;
                //}
            }
            return HandleEvent(e, causesValidation, validationGroup);
        }

        /// <summary>
        /// 当 DataPager 控件回发到服务器时引发此控件的合适的事件。
        /// </summary>
        /// <param name="eventArgument"></param>
        protected virtual void RaisePostBackEvent(string eventArgument)
        {
            ValidateEvent(UniqueID, eventArgument);
            int index = eventArgument.IndexOf('$');
            if (index >= 0)
            {
                CommandEventArgs originalArgs = new CommandEventArgs(eventArgument.Substring(0, index), eventArgument.Substring(index + 1));
                //CommandEventArgs e = new CommandEventArgs(null, this, originalArgs);
                //HandleEvent(e, false, string.Empty);
                HandleEvent(originalArgs, false, string.Empty);
            }
        }

        internal void ValidateEvent(string uniqueID, string eventArgument)
        {
            //if ((Page != null) && SupportsEventValidation)
            //if (Page != null)
            //{
            //    Page.ClientScript.ValidateEvent(uniqueID, eventArgument);
            //}
        }

        //protected virtual void RaiseCallbackEvent(string eventArgument)
        //{
        //    string[] strArray = eventArgument.Split(new char[] { '|' });
        //    IStateFormatter stateFormatter = StateFormatter;
        //    base.ValidateEvent(UniqueID, "\"" + strArray[0] + "|" + strArray[1] + "|" + strArray[2] + "|" + strArray[3] + "\"");
        //    LoadHiddenFieldState(strArray[4], strArray[5], strArray[6], strArray[7]);
        //    int num = int.Parse(strArray[0], CultureInfo.InvariantCulture);
        //    string serializedState = strArray[2];
        //    int.Parse(strArray[1], CultureInfo.InvariantCulture);
        //    if (num == PageIndex)
        //    {
        //        _pageIndex = 0;
        //    }
        //    else
        //    {
        //        _pageIndex = num;
        //    }
        //    DataBind();
        //}
        #endregion

        #region 绑定
        //private Boolean _RequiresDataBinding;
        ///// <summary>请求绑定</summary>
        //protected Boolean RequiresDataBinding
        //{
        //    get { return _RequiresDataBinding; }
        //    set { _RequiresDataBinding = value; }
        //}

        ///// <summary>
        ///// 确保绑定
        ///// </summary>
        //protected void EnsureDataBound()
        //{
        //    if (RequiresDataBinding || DesignMode) DataBind();
        //}

        void BindDataSource()
        {
            if (String.IsNullOrEmpty(DataSourceID)) return;

            // 找到数据源
            //Control control = Helper.FindControlUp<ObjectDataSource>(this, DataSourceID);
            //BindDataSource(control as ObjectDataSource);
            //ObjectDataSource control = Helper.FindControlUp<ObjectDataSource>(this, DataSourceID);
            ObjectDataSource control = Page.FindControl(DataSourceID) as ObjectDataSource;
            if (control == null) control = Helper.FindControlUp<ObjectDataSource>(this, DataSourceID);
            BindDataSource(control);
        }

        Boolean hasBindDataSource = false;
        void BindDataSource(ObjectDataSource ods)
        {
            if (hasBindDataSource) return;
            hasBindDataSource = true;

            if (ods == null) return;

            ods.Selecting += new ObjectDataSourceSelectingEventHandler(ods_Selecting);
            ods.Selected += new ObjectDataSourceStatusEventHandler(ods_Selected);
        }

        void ods_Selecting(object sender, ObjectDataSourceSelectingEventArgs e)
        {
            if (!e.ExecutingSelectCount)
            {
                e.Arguments.StartRowIndex = StartRowIndex;
                e.Arguments.MaximumRows = PageSize;

                // 如果首次打开或者不缓存总记录数，要求查询记录数
                if (!Page.IsPostBack || !TotalRowCountCache) e.Arguments.RetrieveTotalRowCount = true;
            }
        }

        void ods_Selected(object sender, ObjectDataSourceStatusEventArgs e)
        {
            if (e.ReturnValue is Int32) TotalRowCount = (Int32)e.ReturnValue;
        }
        #endregion

        #region IPostBackContainer
        PostBackOptions IPostBackContainer.GetPostBackOptions(IButtonControl buttonControl)
        {
            if (buttonControl == null) throw new ArgumentNullException("buttonControl");

            if (buttonControl.CausesValidation)
            {
                throw new InvalidOperationException(SR.GetString("CannotUseParentPostBackWhenValidating", new object[] { base.GetType().Name, ID }));
            }
            PostBackOptions options = new PostBackOptions(this, buttonControl.CommandName + "$" + buttonControl.CommandArgument);
            options.RequiresJavaScriptProtocol = true;
            return options;
        }
        #endregion

        #region IPostBackEventHandler
        void IPostBackEventHandler.RaisePostBackEvent(string eventArgument)
        {
            RaisePostBackEvent(eventArgument);
        }
        #endregion

        #region 重载
        ///// <summary>
        ///// 初始化时发生
        ///// </summary>
        ///// <param name="e"></param>
        //protected override void OnInit(EventArgs e)
        //{
        //    base.OnInit(e);
        //    if (Page != null) Page.RegisterRequiresControlState(this);
        //}

        ///// <summary>
        ///// 加载控件状态
        ///// </summary>
        ///// <param name="savedState"></param>
        //protected override void LoadControlState(object savedState)
        //{
        //    _pageIndex = 0;
        //    _pageCount = -1;
        //    object[] objArray = savedState as object[];
        //    if (objArray != null)
        //    {
        //        base.LoadControlState(objArray[0]);
        //        if (objArray[1] != null)
        //        {
        //            _pageIndex = (int)objArray[1];
        //        }
        //        if (objArray[2] != null)
        //        {
        //            _pageCount = (int)objArray[2];
        //        }
        //    }
        //    else
        //    {
        //        base.LoadControlState(null);
        //    }
        //}

        ///// <summary>
        ///// 保存控件状态
        ///// </summary>
        ///// <returns></returns>
        //protected override object SaveControlState()
        //{
        //    object obj2 = base.SaveControlState();
        //    if (obj2 != null || _pageIndex != 0 || _pageCount != -1)
        //    {
        //        return new object[] { obj2, _pageIndex, _pageCount, };
        //    }
        //    return true;
        //}

        /// <summary>
        /// 加载ViewState
        /// </summary>
        /// <param name="savedState"></param>
        protected override void LoadViewState(object savedState)
        {
            if (savedState != null)
            {
                object[] objArray = (object[])savedState;
                base.LoadViewState(objArray[0]);
                if (objArray[1] != null)
                {
                    ((IStateManager)PagerStyle).LoadViewState(objArray[1]);
                }
                if (objArray[2] != null)
                {
                    ((IStateManager)PagerSettings).LoadViewState(objArray[2]);
                }
                if (objArray[3] != null)
                {
                    ((IStateManager)base.ControlStyle).LoadViewState(objArray[3]);
                }
            }
            else
            {
                base.LoadViewState(null);
            }
        }

        /// <summary>
        /// 保存ViewState
        /// </summary>
        /// <returns></returns>
        protected override object SaveViewState()
        {
            object obj2 = base.SaveViewState();
            object obj4 = (_pagerStyle != null) ? ((IStateManager)_pagerStyle).SaveViewState() : null;
            object obj12 = (_pagerSettings != null) ? ((IStateManager)_pagerSettings).SaveViewState() : null;
            object obj13 = base.ControlStyleCreated ? ((IStateManager)base.ControlStyle).SaveViewState() : null;
            return new object[] { obj2, obj4, obj12, obj13 };
        }

        /// <summary>
        /// 检查ViewState
        /// </summary>
        protected override void TrackViewState()
        {
            base.TrackViewState();
            if (_pagerStyle != null)
            {
                ((IStateManager)_pagerStyle).TrackViewState();
            }
            if (_pagerSettings != null)
            {
                ((IStateManager)_pagerSettings).TrackViewState();
            }
            if (base.ControlStyleCreated)
            {
                ((IStateManager)base.ControlStyle).TrackViewState();
            }
        }

        /// <summary>
        /// 已重写。保证建立子控件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            //base.EnsureChildControls();
            //EnsureDataBound();

            base.OnPreRender(e);
        }

        /// <summary>
        /// 已重写。保证建立子控件
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(HtmlTextWriter writer)
        {
            base.EnsureChildControls();
            //EnsureDataBound();

            base.Render(writer);

            //RequiresDataBinding = false;

            //if (Page.IsPostBack) DataBind();
        }

        /// <summary>
        /// 加载时触发
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            //// 如果没有启用ViewState，回发的时候需要重新绑定
            //if (Page.IsPostBack && !EnableViewState) RequiresDataBinding = true;
            //if (!Page.IsPostBack) RequiresDataBinding = true;

            //BindDataSource();

            //RequiresDataBinding = true;
        }

        /// <summary>
        /// 初始化时触发
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            BindDataSource();
        }

        /// <summary>
        /// 已重写。不输出开始标签
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            //base.RenderBeginTag(writer);
        }

        /// <summary>
        /// 已重写。不输出结束标签
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
            //base.RenderEndTag(writer);
        }

        ///// <summary>
        ///// 已重写。绑定数据源
        ///// </summary>
        ///// <param name="e"></param>
        //protected override void OnInit(EventArgs e)
        //{
        //    base.OnInit(e);

        //    BindDataSource();
        //}
        #endregion

        #region IPagedDataSource
        /// <summary>
        /// 是否第一页
        /// </summary>
        public bool IsFirstPage
        {
            get { return PageIndex == 0; }
        }

        /// <summary>
        /// 是否最后一页
        /// </summary>
        public bool IsLastPage
        {
            get { return PageIndex == PageCount - 1; }
        }

        /// <summary>
        /// 当前页开始行
        /// </summary>
        public Int32 StartRowIndex
        {
            get { return PageIndex * PageSize; }
        }
        #endregion
    }
}
