using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Log;
using NewLife.Web;
using XCode;
using XCode.Accessors;
using XCode.Configuration;
using XCode.Exceptions;

namespace NewLife.CommonEntity.Web
{
    /// <summary>第二代实体表单，在Page_Load之前给表单控件赋值，在Page_Load之后从表单控件取值并保存</summary>
    /// <remarks>
    /// 在PreLoad阶段处理表单，SetForm把实体对象的字段数据设置到表单控件上，GetForm把表单控件的数据取回到实体字段中。
    /// 之所以选择PreLoad阶段，是因为这是在Page_Load之前最早能拿到Request等数据的阶段，这样子使用者就可以在Page_Load中处理已经赋值完成的表单。
    /// 保存表单时，PreLoad阶段仅仅是给保存按钮设置点击事件，真正的保存动作在点击事件里面，因为在保存表单之前，使用者可能在Page_Load里面对控件进行处理。
    /// 所以，在Page_Load之前给表单控件赋值，在Page_Load之后从表单控件取值并保存。
    ///
    /// 另外，DropDownList等数据绑定控件绑定ObjectDataSource时，会在OnPreRender阶段执行绑定，其中的开关是RequiresDataBinding。
    /// 绑定方法DataBind会导致RequiresDataBinding=false，这样子人工DataBind之后，控件就不会在OnPreRender阶段自动绑定了。
    /// 实体表单在SetForm时有个机制，如果遇到数据绑定的列表控件，会调用一次DataBind，取得列表值，然后再赋值，本以为这样子可以避免OnPreRender阶段的自动绑定。
    /// 经查实，数据绑定控件会在PreLoad时间里面检查页面，如果页面不是回发或者关闭了ViewState，都会导致RequiresDataBinding=true。
    /// 数据绑定控件在OnInit阶段绑定PreLoad事件，而EntityForm在OnPreInit阶段绑定，比数据绑定控件更早，导致了EntityForm的PreLoad先执行，先DataBind，而后控件还是会设置RequiresDataBinding=true。
    /// 因此，EntityForm调整为在InitComplete阶段绑定PreLoad事件。
    /// </remarks>
    public class EntityForm2 : IEntityForm
    {
        #region 属性

        private Control _Container;
        /// <summary>容器</summary>
        public Control Container
        {
            get { return _Container; }
            set { _Container = value; }
        }

        private Type _EntityType;
        /// <summary>实体类型</summary>
        public Type EntityType
        {
            get { return _EntityType; }
            set { _EntityType = value; }
        }

        private String _ItemPrefix = "frm";
        /// <summary>表单项名字前缀，默认frm</summary>
        public virtual String ItemPrefix
        {
            get { return _ItemPrefix; }
            set { _ItemPrefix = value; Accessor = null; }
        }

        #endregion

        #region 构造

        /// <summary>实例化一个实体表单</summary>
        public EntityForm2() { }

        /// <summary>指定控件容器和实体类型，实例化一个实体表单</summary>
        /// <param name="container"></param>
        /// <param name="type"></param>
        public EntityForm2(Control container, Type type)
        {
            (this as IEntityForm).Init(container, type);
        }

        #endregion

        #region 扩展属性

        private IEntityAccessor _Accessor;
        /// <summary>访问器</summary>
        public IEntityAccessor Accessor
        {
            get
            {
                if (_Accessor == null)
                {
                    _Accessor = EntityAccessorFactory.Create(EntityAccessorTypes.WebForm)
                        .SetConfig(EntityAccessorOptions.AllFields, true)
                        .SetConfig(EntityAccessorOptions.Container, Container)
                        .SetConfig(EntityAccessorOptions.ItemPrefix, ItemPrefix);
                }
                return _Accessor;
            }
            set { _Accessor = value; }
        }

        private IEntityOperate _Factory;
        /// <summary>实体操作者</summary>
        public IEntityOperate Factory { get { return _Factory ?? (_Factory = EntityFactory.CreateOperate(EntityType)); } set { _Factory = value; } }

        /// <summary>页面</summary>
        protected Page Page { get { return Container.Page; } }

        /// <summary>响应</summary>
        protected HttpResponse Response { get { return HttpContext.Current.Response; } }

        private Control _SaveButton;
        /// <summary>保存按钮，查找名为btnSave或UpdateButton（兼容旧版本）的按钮</summary>
        protected virtual Control SaveButton
        {
            get
            {
                if (_SaveButton != null) return _SaveButton;

                _SaveButton = FindControl("btnSave");
                if (_SaveButton == null) _SaveButton = FindControl("UpdateButton");

                //// 随便找一个按钮
                //Button btn = ControlHelper.FindControl<Button>(Container, null);
                //if (btn != null && btn.UseSubmitBehavior) return btn;

                return _SaveButton;
            }
            set { _SaveButton = value; }
        }

        private Control _CopyButton;
        /// <summary>保存按钮，查找名为btnCopy的按钮</summary>
        protected virtual Control CopyButton
        {
            get
            {
                if (_CopyButton != null) return _CopyButton;

                _CopyButton = FindControl("btnCopy");

                return _CopyButton;
            }
            set { _CopyButton = value; }
        }

        /// <summary>是否空主键</summary>
        public virtual Boolean IsNew
        {
            get
            {
                Type type = Factory.Unique.Type;
                Object eid = EntityID;
                if (type == typeof(Int32) || type == typeof(Int16) || type == typeof(Int64))
                    return eid != null ? Convert.ToInt64(eid) <= 0 : true;
                else if (type == typeof(String))
                    return eid != null ? String.IsNullOrEmpty((String)eid) : true;
                else
                    throw new NotSupportedException("仅支持整数和字符串类型！");
            }
        }

        private Boolean _CanSave = true;
        /// <summary>是否有权限保存数据</summary>
        public virtual Boolean CanSave { get { return _CanSave; } set { _CanSave = value; } }

        private IManagePage _ManagePage;
        /// <summary>管理页。用于控制权限</summary>
        public virtual IManagePage ManagePage { get { return _ManagePage ?? (_ManagePage = ManageProvider.Provider.GetService<IManagePage>()); } set { _ManagePage = value; } }
        #endregion

        #region 事件

        /// <summary>获取数据实体，允许页面重载改变实体</summary>
        public event EventHandler<EntityFormEventArgs> OnGetEntity;

        /// <summary>把实体数据设置到表单后触发</summary>
        public event EventHandler<EntityFormEventArgs> OnSetForm;

        /// <summary>从表单上读取实体数据后触发</summary>
        public event EventHandler<EntityFormEventArgs> OnGetForm;

        /// <summary>验证时触发</summary>
        public event EventHandler<EntityFormEventArgs> OnValid;

        /// <summary>保存前触发，位于事务保护内</summary>
        public event EventHandler<EntityFormEventArgs> OnSaving;

        /// <summary>保存成功后触发，位于事务保护外</summary>
        public event EventHandler<EntityFormEventArgs> OnSaveSuccess;

        /// <summary>保存失败后触发，位于事务保护外</summary>
        public event EventHandler<EntityFormEventArgs> OnSaveFailure;

        #endregion

        #region 实体相关

        private String _KeyName;
        /// <summary>键名。使用者可以通过给KeyName置空来避免内部自动根据Request[KeyName]取值</summary>
        public virtual String KeyName
        {
            get
            {
                if (_KeyName != null) return _KeyName;

                if (Factory.Unique != null)
                    _KeyName = Factory.Unique.Name;
                else
                {
                    FieldItem[] fis = Factory.Table.PrimaryKeys;
                    if (fis != null && fis.Length > 1)
                    {
                        if (XTrace.Debug) XTrace.WriteLine("实体表单默认不支持多主键（实体类{0}），需要手工给Entity赋值！", EntityType.Name);
                    }
                }

                return _KeyName;
            }
            set { _KeyName = value; }
        }

        /// <summary>主键。如果实体已经存在，则使用实体的主键值。因为有些时候实体是由外部赋值的</summary>
        public virtual Object EntityID
        {
            get
            {
                // 如果实体已经存在，则使用实体的主键值。
                if (_Entity != null && Factory.Unique != null) return _Entity[Factory.Unique.Name];

                // 使用者可以通过给KeyName置空来避免内部自动根据Request[KeyName]取值
                if (String.IsNullOrEmpty(KeyName)) return null;

                String str = HttpContext.Current.Request[KeyName];
                if (String.IsNullOrEmpty(str)) return null;

                FieldItem fi = Factory.Unique;
                if (fi != null)
                {
                    Type type = Factory.Unique.Type;
                    if (type == typeof(Int32) || type == typeof(Int64))
                    {
                        Int32 id = 0;
                        if (!Int32.TryParse(str, out id)) id = 0;
                        return (Object)id;
                    }
                    else if (type == typeof(String))
                    {
                        return (Object)str;
                    }
                }
                throw new NotSupportedException("仅支持整数和字符串类型！");
            }
        }

        private Boolean _isGettingEntity;
        private IEntity _Entity;
        /// <summary>数据实体。使用者可以通过给KeyName置空来避免内部自动根据Request[KeyName]取值</summary>
        public virtual IEntity Entity
        {
            get
            {
                if (_Entity == null && !_isGettingEntity)
                {
                    _isGettingEntity = true;

                    Object eid = EntityID;

                    // 使用者可以通过给KeyName置空来避免内部自动根据Request[KeyName]取值
                    if (!String.IsNullOrEmpty(KeyName)) _Entity = Factory.FindByKeyForEdit(eid);

                    // 外部可以通过OnGetEntity时间直接修改Entity
                    if (OnGetEntity != null) OnGetEntity(this, new EntityFormEventArgs());

                    if (_Entity == null && !String.IsNullOrEmpty(KeyName)) _Entity = Factory.FindByKeyForEdit(eid);

                    // 把Request参数读入到实体里面
                    FillEntityWithRequest(_Entity);
                    //将主键脏数据标记为不修改
                    _Entity.Dirtys.Remove(Factory.Unique.Name);

                    _isGettingEntity = false;
                }
                return _Entity;
            }
            set { _Entity = value; }
        }

        /// <summary>使用Request参数填充entity</summary>
        /// <param name="entity"></param>
        protected virtual void FillEntityWithRequest(IEntity entity)
        {
            if (entity == null || HttpContext.Current == null || HttpContext.Current.Request == null) return;

            // 借助Http实体访问器，直接把Request参数读入到实体里面
            IEntityAccessor accessor = EntityAccessorFactory.Create(EntityAccessorTypes.Http);
            // 这里的异常不需要暴露到外部
            try
            {
                accessor.Read(entity);
            }
            catch { }
        }

        #endregion

        #region 生命周期

        Boolean hasInit = false;

        private void Init()
        {
            if (hasInit) return;
            hasInit = true;

            Page.InitComplete += new EventHandler(Page_InitComplete);
            //Page.PreLoad += new EventHandler(OnPreLoad);
            //Page.LoadComplete += new EventHandler(OnLoadComplete);
        }

        private void Page_InitComplete(object sender, EventArgs e)
        {
            Page.PreLoad += new EventHandler(OnPreLoad);
        }

        /// <summary></summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnPreLoad(object sender, EventArgs e)
        {
            IEntity entity = null;
            try
            {
                entity = Entity;
            }
            catch (XCodeException ex)
            {
                // 由下面自行处理,而不是抛出这个异常
                if (!ex.Message.Contains("参数错误！无法取得编号为")) throw;
            }
            // 判断实体
            if (entity == null)
            {
                String msg = null;
                Object eid = EntityID;
                if (IsNew)
                    msg = String.Format("参数错误！无法取得编号为{0}的{2}({1})！可能未设置自增主键！", eid, Factory.TableName, Factory.Table.Description);
                else
                    msg = String.Format("参数错误！无法取得编号为{0}的{2}({1})！", eid, Factory.TableName, Factory.Table.Description);

                WebHelper.Alert(msg);
                Response.Write(msg);
                Response.End();
                return;
            }

            Control btn = SaveButton;
            Control btncopy = CopyButton;
            if (!Page.IsPostBack)
            {
                // 尝试获取页面控制器，如果取得，则可以控制权限
                //IManagePage manager = ManageProvider.Provider.GetService<IManagePage>();
                var manager = ManagePage;
                if (manager != null && manager.Container != null && manager.ValidatePermission)
                {
                    CanSave = entity.IsNullKey && manager.Acquire(PermissionFlags.Insert) || manager.Acquire(PermissionFlags.Update);

                    // 复制只需要新增权限
                    if (btncopy != null) btncopy.Visible = manager.Acquire(PermissionFlags.Insert);
                }

                // 新增数据时，不显示复制按钮
                if (IsNew && btncopy != null) btncopy.Visible = false;

                if (btn != null)
                {
                    btn.Visible = CanSave;

                    if (btn is IButtonControl) (btn as IButtonControl).Text = entity.IsNullKey ? "新增" : "更新";
                }

                //利用js控制按钮点击状态
                //2013-1-14 @宁波-小董，注释下面2句：
                //原因：这里在XCode默认网站后台没有问题，但在其他网站后台，如果利用js对表单进行验证，就会出现错误，
                //验证不通过，这里也会执行js代码，“正在提交”，然后页面就死掉了
                //不知道要怎么修改才能使得页面验证不通过时，这个就不执行。
                //RegButtonOnClientClick(btn);
                //RegButtonOnClientClick(btncopy);

                SetForm();
            }
            else
            {
                // 如果外部设置了按钮事件，则这里不再设置
                if (btn != null && btn is IButtonControl && ControlHelper.FindEventHandler(btn, "Click") == null)
                    (btn as IButtonControl).Click += delegate
                    {
                        GetForm();
                        if (ValidForm()) SaveFormWithTrans();
                    };
                // 这里还不能保存表单，因为使用者习惯性在Load事件里面写业务代码，所以只能在Load完成后保存
                //else if (Page.AutoPostBackControl == null)
                //{
                //    GetForm();
                //    if (ValidForm()) SaveFormWithTrans();
                //}
                if (btncopy != null && btncopy is IButtonControl && ControlHelper.FindEventHandler(btncopy, "Click") == null)
                    (btncopy as IButtonControl).Click += delegate
                    {
                        GetForm();

                        // 清空主键，变成新增
                        IEntityOperate eop = EntityFactory.CreateOperate(Entity.GetType());
                        foreach (var item in eop.Fields)
                        {
                            if (item.PrimaryKey || item.IsIdentity) Entity[item.Name] = null;
                        }

                        if (ValidForm()) SaveFormWithTrans();
                    };
            }
        }

        //void OnLoadComplete(object sender, EventArgs e)
        //{
        //    if (Page.IsPostBack && Page.AutoPostBackControl == null)
        //    {
        //        Control btn = SaveButton;
        //        if (btn == null || !(btn is IButtonControl))
        //        {
        //            Accessor.Read(Entity);
        //            if (ValidForm()) SaveFormWithTrans();
        //        }
        //    }
        //}

        #endregion

        #region 方法

        /// <summary>把实体的属性设置到控件上</summary>
        public virtual void SetForm()
        {
            Accessor.OnWriteItem += new EventHandler<EntityAccessorEventArgs>(Accessor_OnWrite);
            Accessor.Write(Entity);

            if (OnSetForm != null) OnSetForm(this, new EntityFormEventArgs());
        }

        private void Accessor_OnWrite(object sender, EntityAccessorEventArgs e)
        {
            WebControl wc = ControlHelper.FindControlInPage<WebControl>(ItemPrefix + e.Field.Name);
            if (wc == null) return;

            if (!CanSave)
            {
                if (wc is TextBox)
                    (wc as TextBox).ReadOnly = !CanSave;
                else
                    wc.Enabled = CanSave;
            }
        }

        /// <summary>从表单上读取实体数据</summary>
        public virtual void GetForm()
        {
            Accessor.Read(Entity);

            if (OnGetForm != null) OnGetForm(this, new EntityFormEventArgs());
        }

        /// <summary>验证表单，返回是否有效数据，决定是否保存表单数据</summary>
        /// <returns></returns>
        public virtual Boolean ValidForm()
        {
            foreach (FieldItem item in Factory.Fields)
            {
                Control control = FindControlByField(item);
                if (control == null) continue;

                if (!ValidFormItem(item, control)) return false;
            }

            if (OnValid != null)
            {
                var e = new EntityFormEventArgs() { Cancel = false };
                OnValid(this, e);
                if (e.Cancel) return false;
            }

            return true;
        }

        /// <summary>验证表单项</summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <returns></returns>
        protected virtual Boolean ValidFormItem(FieldItem field, Control control)
        {
            // 必填项
            if (!field.IsNullable)
            {
                if (field.Type == typeof(String))
                {
                    if (String.IsNullOrEmpty((String)Entity[field.Name]))
                    {
                        WebHelper.Alert(String.Format("{0}不能为空！", field.DisplayName));
                        if (!(control is HiddenField)) control.Focus();
                        return false;
                    }
                }
                else if (field.Type == typeof(DateTime))
                {
                    DateTime d = (DateTime)Entity[field.Name];
                    if (d == DateTime.MinValue || d == DateTime.MaxValue)
                    {
                        WebHelper.Alert(String.Format("{0}不能为空！", field.DisplayName));
                        if (!(control is HiddenField)) control.Focus();
                        return false;
                    }
                }
            }

            return true;
        }

        private void SaveFormWithTrans()
        {
            var eop = Factory;
            eop.BeginTransaction();
            Exception _ex = null;
            try
            {
                Boolean cancel = false;
                if (OnSaving != null)
                {
                    var e = new EntityFormEventArgs() { Cancel = false };
                    //表单OnSaving事件取消仅仅是用于用户已经提前保存过表单，防止重复保存的情况
                    //所以即使取消保存，依然会正常的进如数据保存成功提示
                    //如果需要进行数据校验，请在OnValid中进行
                    OnSaving(this, e);
                    if (e.Cancel) cancel = true;
                }

                if (!cancel) SaveForm();

                eop.Commit();
            }
            catch (Exception ex)
            {
                eop.Rollback();
                _ex = ex;
            }
            try
            {
                if (_ex == null)
                {
                    SaveFormSuccess();
                }
                else
                {
                    SaveFormFailure(_ex);
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                Page.ClientScript.RegisterStartupScript(this.GetType(), "SaveFormError",
                    "alert('保存后触发" + _ex == null ? "完成" : "失败" + "事件发生了异常,请检查日志!');",
                    true);
            }
        }

        /// <summary>保存表单，把实体保存到数据库</summary>
        void IEntityForm.SaveForm() { SaveFormWithTrans(); }

        /// <summary>保存表单，把实体保存到数据库，当前方法处于事务保护之中</summary>
        protected virtual void SaveForm() { Entity.Save(); }

        /// <summary>保存成功</summary>
        protected virtual void SaveFormSuccess()
        {
            if (OnSaveSuccess != null)
            {
                var e = new EntityFormEventArgs() { Cancel = false };
                OnSaveSuccess(this, e);
                if (e.Cancel) return;
            }

            // 这个地方需要考虑一个问题，就是列表页查询之后再打开某记录进行编辑，编辑成功后，如果强行的reload列表页，浏览器会循环是否重新提交
            // 经测试，可以找到列表页的那个查询按钮，模拟点击一次它，实际上就是让ASP.Net列表页回发一次，可以解决这个问题
            Page.ClientScript.RegisterStartupScript(this.GetType(), "alert", @"alert('成功！');
(function(){
    var load=window.onload;
    window.onload=function(){
        try{
            if(load) load();
            parent.Dialog.CloseAndRefresh(frameElement);
        }catch(e){};
    };
})();
", true);
        }

        /// <summary>保存失败</summary>
        /// <param name="ex"></param>
        protected virtual void SaveFormFailure(Exception ex)
        {
            if (OnSaveFailure != null)
            {
                var e = new EntityFormEventArgs() { Cancel = false, Error = ex };
                OnSaveFailure(this, e);
                if (e.Cancel) return;
            }

            // 如果是参数异常，参数名可能就是字段名，可以定位到具体控件
            ArgumentException ae = ex as ArgumentException;
            if (ae != null && !String.IsNullOrEmpty(ae.ParamName))
            {
                Control control = FindControl(ItemPrefix + ae.ParamName);
                if (control != null && !(control is HiddenField)) control.Focus();
            }

            WebHelper.Alert("失败！" + ex.Message);
        }

        /// <summary>
        /// 设置保存按钮名称
        /// </summary>
        /// <param name="text"></param>
        public virtual void SetSaveButtonText(String text)
        {
            SetControlMemberValue(SaveButton, "Text", text);
        }

        /// <summary>
        /// 设置另存为按钮名称
        /// </summary>
        /// <param name="text"></param>
        public virtual void SetCopyButtonText(String text)
        {
            SetControlMemberValue(CopyButton, "Text", text);
        }

        #endregion

        #region 辅助

        /// <summary>查找表单控件</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual Control FindControl(string id)
        {
            Control control = ControlHelper.FindControlByField<Control>(Container, id);
            if (control != null) return control;

            control = Container.FindControl(id);
            if (control != null) return control;

            return ControlHelper.FindControl<Control>(Container, id);
        }

        /// <summary>查找字段对应的控件</summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected virtual Control FindControlByField(FieldItem field)
        {
            return FindControl(ItemPrefix + field.Name);
        }

        /// <summary>
        /// 设置按钮状态脚本
        /// </summary>
        /// <param name="btn"></param>
        protected virtual void RegButtonOnClientClick(Control btn)
        {
            //利用js控制按钮点击状态
            if (btn != null && btn.Visible == true)
            {
                String btnClientOnclick = "javascript:var self=this;setTimeout(function(){self.disabled=true;self.value='正在提交'},0);";

                if (btn is Button || btn is LinkButton)
                    SetControlMemberValue(btn, "OnClientClick", btnClientOnclick);

                //if (btn is Button)
                //    (btn as Button).OnClientClick = btnClientOnclick;
                //else if (btn is LinkButton)
                //    (btn as LinkButton).OnClientClick = btnClientOnclick;
            }
        }

        /// <summary>
        /// 设置控件成员值
        /// </summary>
        /// <param name="control"></param>
        /// <param name="fieldName"></param>
        /// <param name="text"></param>
        private void SetControlMemberValue(Control control, String fieldName, String text)
        {
            if (control == null) return;

            NewLife.Reflection.MemberInfoX.Create(control.GetType(), fieldName).SetValue(control, text);
        }

        #endregion

        #region IEntityForm 成员

        /// <summary>使用控件容器和实体类初始化接口</summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        IEntityForm IEntityForm.Init(Control container, Type entityType)
        {
            if (container == null)
            {
                if (HttpContext.Current.Handler is Page) container = HttpContext.Current.Handler as Page;
            }

            Container = container;
            EntityType = entityType;

            Init();

            return this;
        }

        #endregion
    }
}