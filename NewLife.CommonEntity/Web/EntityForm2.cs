using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;
using XCode;
using XCode.Accessors;
using XCode.Configuration;

namespace NewLife.CommonEntity.Web
{
    /// <summary>第二代实体表单</summary>
    /// <remarks>
    /// 作为第二代实体表单，必须解决几个问题：
    /// 1，不能使用泛型；
    /// 2，不能占用页面基类；
    /// </remarks>
    public class EntityForm2
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
        /// <summary>
        /// 指定控件容器和实体类型，实例化一个实体表单
        /// </summary>
        /// <param name="container"></param>
        /// <param name="type"></param>
        public EntityForm2(Control container, Type type)
        {
            if (container == null)
            {
                if (HttpContext.Current.Handler is Page) container = HttpContext.Current.Handler as Page;
            }

            Container = container;
            EntityType = type;

            Init();
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
                        .SetConfig(EntityAccessorOptions.Container, Container)
                        .SetConfig(EntityAccessorOptions.ItemPrefix, ItemPrefix);
                }
                return _Accessor;
            }
            set { _Accessor = value; }
        }

        private IEntityOperate _Factory;
        /// <summary>实体操作者</summary>
        public IEntityOperate Factory { get { return _Factory; } set { _Factory = value; } }

        /// <summary>页面</summary>
        protected Page Page { get { return Container.Page; } }

        /// <summary>响应</summary>
        protected HttpResponse Response { get { return Container.Page.Response; } }

        /// <summary>保存按钮，查找名为btnSave或UpdateButton（兼容旧版本）的按钮，如果没找到，将使用第一个使用了提交行为的按钮</summary>
        protected virtual Control SaveButton
        {
            get
            {
                Control control = FindControl("btnSave");
                if (control != null) return control;

                control = FindControl("UpdateButton");
                if (control != null) return control;

                // 随便找一个按钮
                Button btn = ControlHelper.FindControl<Button>(Page, null);
                if (btn != null && btn.UseSubmitBehavior) return btn;

                return null;
            }
        }

        /// <summary>是否空主键</summary>
        protected virtual Boolean IsNullKey
        {
            get
            {
                Type type = Factory.Unique.Type;
                if (type == typeof(Int32))
                    return (Int32)(Object)EntityID <= 0;
                else if (type == typeof(String))
                    return String.IsNullOrEmpty((String)(Object)EntityID);
                else
                    throw new NotSupportedException("仅支持整数和字符串类型！");
            }
        }

        private Boolean _CanSave;
        /// <summary>是否有权限保存数据</summary>
        public virtual Boolean CanSave { get { return _CanSave; } set { _CanSave = value; } }
        #endregion

        #region 实体相关
        private String _KeyName;
        /// <summary>键名</summary>
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

        /// <summary>主键</summary>
        public virtual Object EntityID
        {
            get
            {
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

        private IEntity _Entity;
        /// <summary>数据实体</summary>
        public virtual IEntity Entity
        {
            get
            {
                if (_Entity == null)
                {
                    if (OnGetEntity != null)
                    {
                        EventArgs<Object, IEntity> e = new EventArgs<object, IEntity>(EntityID, null);
                        OnGetEntity(this, e);
                        _Entity = e.Arg2;
                    }
                    if (_Entity == null) _Entity = Factory.FindByKeyForEdit(EntityID);
                }
                return _Entity;
            }
            set { _Entity = value; }
        }

        /// <summary>获取数据实体，允许页面重载改变实体</summary>
        public event EventHandler<EventArgs<Object, IEntity>> OnGetEntity;
        #endregion

        #region 生命周期
        void Init()
        {
            Page.PreLoad += new EventHandler(OnPreLoad);
            Page.LoadComplete += new EventHandler(OnLoadComplete);
        }

        void OnPreLoad(object sender, EventArgs e)
        {
            // 判断实体
            if (Entity == null)
            {
                String msg = null;
                if (IsNullKey)
                    msg = String.Format("参数错误！无法取得编号为{0}的{1}！可能未设置自增主键！", EntityID, Factory.TableName);
                else
                    msg = String.Format("参数错误！无法取得编号为{0}的{1}！", EntityID, Factory.TableName);

                WebHelper.Alert(msg);
                Response.Write(msg);
                Response.End();
                return;
            }

            Control btn = SaveButton;
            if (!Page.IsPostBack)
            {
                if (btn != null)
                {
                    // 添加/编辑 按钮需要添加/编辑权限
                    //if (IsNullKey)
                    //    btn.Visible = Acquire(PermissionFlags.Insert);
                    //else
                    //    btn.Visible = Acquire(PermissionFlags.Update);

                    btn.Visible = CanSave;

                    if (btn is IButtonControl) (btn as IButtonControl).Text = IsNullKey ? "新增" : "更新";
                }

                SetForm();
            }
            else
            {
                if (btn != null && btn is IButtonControl)
                    (btn as IButtonControl).Click += delegate { Accessor.Read(Entity); if (ValidForm()) SaveFormWithTrans(); };
                // 这里还不能保存表单，因为使用者习惯性在Load事件里面写业务代码，所以只能在Load完成后保存
                //else if (Page.AutoPostBackControl == null)
                //{
                //    GetForm();
                //    if (ValidForm()) SaveFormWithTrans();
                //}
            }
        }

        void OnLoadComplete(object sender, EventArgs e)
        {
            if (Page.IsPostBack && Page.AutoPostBackControl == null)
            {
                Control btn = SaveButton;
                if (btn == null || !(btn is IButtonControl))
                {
                    Accessor.Read(Entity);
                    if (ValidForm()) SaveFormWithTrans();
                }
            }
        }
        #endregion

        #region 方法
        /// <summary>把实体的属性设置到控件上</summary>
        protected virtual void SetForm()
        {
            // 是否有权限保存数据
            Boolean canSave = CanSave;

            Accessor.Write(Entity);
        }

        /// <summary>
        /// 验证表单，返回是否有效数据
        /// </summary>
        /// <returns></returns>
        protected virtual Boolean ValidForm()
        {
            foreach (FieldItem item in Factory.Fields)
            {
                Control control = FindControlByField(item);
                if (control == null) continue;

                if (!ValidFormItem(item, control)) return false;
            }
            return true;
        }

        /// <summary>
        /// 验证表单项
        /// </summary>
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
                        control.Focus();
                        return false;
                    }
                }
                else if (field.Type == typeof(DateTime))
                {
                    DateTime d = (DateTime)Entity[field.Name];
                    if (d == DateTime.MinValue || d == DateTime.MaxValue)
                    {
                        WebHelper.Alert(String.Format("{0}不能为空！", field.DisplayName));
                        control.Focus();
                        return false;
                    }
                }
            }

            return true;
        }

        private void SaveFormWithTrans()
        {
            Factory.BeginTransaction();
            try
            {
                SaveForm();

                Factory.Commit();

                SaveFormSuccess();
            }
            catch (Exception ex)
            {
                Factory.Rollback();

                SaveFormUnsuccess(ex);
            }
        }

        /// <summary>
        /// 保存表单，把实体保存到数据库，当前方法处于事务保护之中
        /// </summary>
        protected virtual void SaveForm()
        {
            Entity.Save();
        }

        /// <summary>
        /// 保存成功
        /// </summary>
        protected virtual void SaveFormSuccess()
        {
            // 这个地方需要考虑一个问题，就是列表页查询之后再打开某记录进行编辑，编辑成功后，如果强行的reload列表页，浏览器会循环是否重新提交
            // 经测试，可以找到列表页的那个查询按钮，模拟点击一次它，实际上就是让ASP.Net列表页回发一次，可以解决这个问题
            Page.ClientScript.RegisterStartupScript(this.GetType(), "alert", @"alert('成功！');
(function(){
    var load=window.onload;
    window.onload=function(){
        if(load) load();
        parent.Dialog.CloseAndRefresh(frameElement);
    };
})();
", true);
        }

        /// <summary>
        /// 保存失败
        /// </summary>
        /// <param name="ex"></param>
        protected virtual void SaveFormUnsuccess(Exception ex)
        {
            // 如果是参数异常，参数名可能就是字段名，可以定位到具体控件
            ArgumentException ae = ex as ArgumentException;
            if (ae != null && !String.IsNullOrEmpty(ae.ParamName))
            {
                Control control = FindControl(ItemPrefix + ae.ParamName);
                if (control != null) control.Focus();
            }

            WebHelper.Alert("失败！" + ex.Message);
        }
        #endregion

        #region 辅助
        /// <summary>
        /// 查找表单控件
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual Control FindControl(string id)
        {
            Control control = ControlHelper.FindControlInPage<Control>(id);
            if (control != null) return control;

            control = Container.FindControl(id);
            if (control != null) return control;

            return ControlHelper.FindControl<Control>(Page.Form, id);
        }

        /// <summary>
        /// 查找字段对应的控件
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected virtual Control FindControlByField(FieldItem field)
        {
            return FindControl(ItemPrefix + field.Name);
        }
        #endregion
    }
}