using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using System.Collections.Generic;
using NewLife.Reflection;
using NewLife.Exceptions;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity.Web
{
    /// <summary>
    /// 实体表单基类
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <typeparam name="TEntity">表单实体类</typeparam>
    public class EntityForm<TKey, TEntity> : EntityForm<TKey, TEntity, Administrator, Menu>
        where TEntity : Entity<TEntity>, new()
    { }

    /// <summary>
    /// 实体表单基类
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <typeparam name="TEntity">表单实体类</typeparam>
    /// <typeparam name="TAdminEntity">管理员类</typeparam>
    /// <typeparam name="TMenuEntity">菜单类</typeparam>
    public class EntityForm<TKey, TEntity, TAdminEntity, TMenuEntity> : WebPageBase<TAdminEntity, TMenuEntity>
        where TEntity : Entity<TEntity>, new()
        where TAdminEntity : Administrator<TAdminEntity>, new()
        where TMenuEntity : Menu<TMenuEntity>, new()
    {
        #region 基本属性
        /// <summary>
        /// 主键名称，字符串默认返回Guid，其它默认返回ID
        /// </summary>
        protected virtual String EntityKeyName
        {
            get
            {
                if (Entity<TEntity>.Meta.Unique != null) return Entity<TEntity>.Meta.Unique.Name;

                Type type = typeof(TKey);
                if (type == typeof(Int32))
                    return "ID";
                else if (type == typeof(String))
                    return "Guid";
                else
                    return "ID";
            }
        }

        /// <summary>主键</summary>
        public TKey EntityID
        {
            get
            {
                String str = Request[EntityKeyName];
                if (String.IsNullOrEmpty(str)) return default(TKey);

                Type type = typeof(TKey);
                if (type == typeof(Int32))
                {
                    Int32 id = 0;
                    if (!Int32.TryParse(str, out id)) id = 0;
                    return (TKey)(Object)id;
                }
                else if (type == typeof(String))
                {
                    return (TKey)(Object)str;
                }
                else
                    throw new NotSupportedException("仅支持整数和字符串类型！");
            }
        }

        private TEntity _Entity;
        /// <summary>数据实体</summary>
        public virtual TEntity Entity
        {
            get { return _Entity ?? (_Entity = Entity<TEntity>.FindByKeyForEdit(EntityID)); }
            set { _Entity = value; }
        }

        /// <summary>
        /// 表单项名字前缀，默认frm
        /// </summary>
        protected virtual String FormItemPrefix { get { return "frm"; } }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 保存按钮，查找名为btnSave或UpdateButton（兼容旧版本）的按钮，如果没找到，将使用第一个使用了提交行为的按钮
        /// </summary>
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

        /// <summary>
        /// 是否空主键
        /// </summary>
        protected virtual Boolean IsNullKey
        {
            get
            {
                Type type = typeof(TKey);
                if (type == typeof(Int32))
                {
                    return (Int32)(Object)EntityID <= 0;
                }
                else if (type == typeof(String))
                {
                    return String.IsNullOrEmpty((String)(Object)EntityID);
                }
                else
                    throw new NotSupportedException("仅支持整数和字符串类型！");
            }
        }
        #endregion

        #region 事件
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreLoad(EventArgs e)
        {
            base.OnPreLoad(e);

            // 判断实体
            if (Entity == null)
            {
                String msg = null;
                if (IsNullKey)
                    msg = String.Format("参数错误！无法取得编号为{0}的{1}！可能未设置自增主键！", EntityID, Entity<TEntity>.Meta.TableName);
                else
                    msg = String.Format("参数错误！无法取得编号为{0}的{1}！", EntityID, Entity<TEntity>.Meta.TableName);

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
                    if (IsNullKey)
                        btn.Visible = Acquire(PermissionFlags.Insert);
                    else
                        btn.Visible = Acquire(PermissionFlags.Update);

                    if (btn is IButtonControl) (btn as IButtonControl).Text = IsNullKey ? "新增" : "更新";
                }

                SetForm();
            }
            else
            {
                if (btn != null && btn is IButtonControl)
                    (btn as IButtonControl).Click += delegate { GetForm(); if (ValidForm()) SaveFormWithTrans(); };
                else if (Page.AutoPostBackControl == null)
                {
                    GetForm();
                    if (ValidForm()) SaveFormWithTrans();
                }
            }
        }

        /// <summary>
        /// 查找表单控件
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override Control FindControl(string id)
        {
            Control control = base.FindControl(id);
            if (control != null) return control;

            return ControlHelper.FindControl<Control>(Page.Form, id);
        }
        #endregion

        #region 加载
        /// <summary>
        /// 把实体的属性设置到控件上
        /// </summary>
        protected virtual void SetForm()
        {
            // 是否有权限保存数据
            Boolean canSave = IsNullKey && Acquire(PermissionFlags.Insert) || Acquire(PermissionFlags.Update);

            foreach (FieldItem item in Entity<TEntity>.Meta.Fields)
            {
                Control control = Page.FindControl(FormItemPrefix + item.Name);
                if (control == null) continue;

                SetFormItem(item, control, canSave);
            }
        }

        /// <summary>
        /// 把实体成员的值设置到控件上
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItem(FieldItem field, Control control, Boolean canSave)
        {
            if (field == null || control == null) return;

            if (control is WebControl)
            {
                WebControl wc = control as WebControl;

                // 设置ToolTip
                if (String.IsNullOrEmpty(wc.ToolTip))
                {
                    String des = String.IsNullOrEmpty(field.DisplayName) ? field.Name : field.DisplayName;
                    wc.ToolTip = String.Format("请填写{0}！", des);
                }

                // 必填项
                if (!field.IsNullable) SetNotAllowNull(field, control, canSave);

                // 设置只读
                if (wc is TextBox)
                    (wc as TextBox).ReadOnly = !canSave;
                else
                    wc.Enabled = canSave;

                // 分控件处理
                if (wc is TextBox)
                    SetFormItemTextBox(field, wc as TextBox, canSave);
                else if (wc is Label)
                    SetFormItemLabel(field, wc as Label, canSave);
                else if (wc is CheckBox)
                    SetFormItemCheckBox(field, wc as CheckBox, canSave);
                else if (wc is DropDownList)
                    SetFormItemDropDownList(field, wc as DropDownList, canSave);
                else if (wc is RadioButton)
                    SetFormItemRadioButton(field, wc as RadioButton, canSave);
                else
                {
                    PropertyInfoX pix = PropertyInfoX.Create(control.GetType(), "Text");
                    if (pix != null) pix.SetValue(control, Entity[field.Name]);
                }
            }
        }

        /// <summary>
        /// 文本框
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItemTextBox(FieldItem field, TextBox control, Boolean canSave)
        {
            Type type = field.Type;
            if (type == typeof(DateTime))
            {
                DateTime d = (DateTime)Entity[field.Name];
                if (IsNullKey && d == DateTime.MinValue) d = DateTime.Now;
                control.Text = d.ToString("yyyy-MM-dd HH:mm:ss");
                //else
                //    control.Text = null;
            }
            else
                control.Text = String.Empty + Entity[field.Name];
        }

        /// <summary>
        /// 标签
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItemLabel(FieldItem field, Label control, Boolean canSave)
        {
            control.Text = String.Empty + Entity[field.Name];
        }

        /// <summary>
        /// 复选框
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItemCheckBox(FieldItem field, CheckBox control, Boolean canSave)
        {
            Type type = field.Type;
            if (type == typeof(Boolean))
                control.Checked = (Boolean)Entity[field.Name];
            else if (type == typeof(Int32))
                control.Checked = (Int32)Entity[field.Name] != 0;
            else
                control.Checked = Entity[field.Name] != null;
        }

        /// <summary>
        /// 下拉框
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItemDropDownList(FieldItem field, DropDownList control, Boolean canSave)
        {
            if (control.Items.Count < 1) control.DataBind();
            if (control.Items.Count < 1) return;

            String value = String.Empty + Entity[field.Name];

            ListItem li = control.Items.FindByValue(value);
            if (li != null)
                li.Selected = true;
            else
            {
                li = new ListItem(value, value);
                control.Items.Add(li);
                li.Selected = true;
            }
        }

        /// <summary>
        /// 单选框
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItemRadioButton(FieldItem field, RadioButton control, Boolean canSave)
        {
            List<RadioButton> list = new List<RadioButton>();
            // 找到同一级同组名的所有单选
            foreach (Control item in control.Parent.Controls)
            {
                if (!(item is RadioButton)) continue;

                RadioButton rb = item as RadioButton;
                if (rb.GroupName != control.GroupName) continue;

                list.Add(rb);
            }
            if (list.Count < 1) return;

            String value = String.Empty + Entity[field.Name];

            foreach (RadioButton item in list)
            {
                item.Checked = item.Text == value;
            }
        }

        /// <summary>
        /// 设置控件的不允许空
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetNotAllowNull(FieldItem field, Control control, Boolean canSave)
        {
            if (field.IsNullable) return;

            LiteralControl lc = new LiteralControl();
            lc.Text = "<font colore='red'>*</font>";

            Int32 p = control.Parent.Controls.IndexOf(control);
            // 有时候可能无法添加，但是不影响使用，应该屏蔽异常
            try
            {
                control.Parent.Controls.AddAt(p + 1, lc);
            }
            catch { }
        }
        #endregion

        #region 读取
        /// <summary>
        /// 读取控件的数据保存到实体中去
        /// </summary>
        protected virtual void GetForm()
        {
            foreach (FieldItem item in Entity<TEntity>.Meta.Fields)
            {
                Control control = Page.FindControl(FormItemPrefix + item.Name);
                if (control == null) continue;

                GetFormItem(item, control);
            }
        }

        /// <summary>
        /// 把控件的值设置到实体属性上
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItem(FieldItem field, Control control)
        {
            if (field == null || control == null) return;

            if (control is WebControl)
            {
                WebControl wc = control as WebControl;

                // 分控件处理
                if (wc is TextBox)
                    GetFormItemTextBox(field, wc as TextBox);
                else if (wc is Label)
                    GetFormItemLabel(field, wc as Label);
                else if (wc is CheckBox)
                    GetFormItemCheckBox(field, wc as CheckBox);
                else if (wc is DropDownList)
                    GetFormItemDropDownList(field, wc as DropDownList);
                else if (wc is RadioButton)
                    GetFormItemRadioButton(field, wc as RadioButton);
                else
                {
                    PropertyInfoX pix = PropertyInfoX.Create(control.GetType(), "Text");
                    if (pix != null)
                    {
                        Object v = pix.GetValue(control);
                        if (!Object.Equals(Entity[field.Name], v)) Entity.SetItem(field.Name, v);
                    }
                }
            }
        }

        /// <summary>
        /// 文本框
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemTextBox(FieldItem field, TextBox control)
        {
            String v = control.Text;
            if (!Object.Equals(Entity[field.Name], v)) Entity.SetItem(field.Name, v);
        }

        /// <summary>
        /// 标签，不做任何操作
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemLabel(FieldItem field, Label control)
        {

        }

        /// <summary>
        /// 复选框
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemCheckBox(FieldItem field, CheckBox control)
        {
            Type type = field.Type;
            Object v;
            if (type == typeof(Boolean))
                v = control.Checked;
            else if (type == typeof(Int32))
                v = control.Checked ? 1 : 0;
            else
                v = control.Checked;

            if (!Object.Equals(Entity[field.Name], v)) Entity.SetItem(field.Name, v);
        }

        /// <summary>
        /// 下拉列表
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemDropDownList(FieldItem field, DropDownList control)
        {
            //if (String.IsNullOrEmpty(control.SelectedValue)) return;

            String v = control.SelectedValue;
            if (!Object.Equals(Entity[field.Name], v)) Entity.SetItem(field.Name, v);
        }

        /// <summary>
        /// 单选框
        /// </summary>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemRadioButton(FieldItem field, RadioButton control)
        {
            List<RadioButton> list = new List<RadioButton>();
            // 找到同一级同组名的所有单选
            foreach (Control item in control.Parent.Controls)
            {
                if (!(item is RadioButton)) continue;

                RadioButton rb = item as RadioButton;
                if (rb.GroupName != control.GroupName) continue;

                list.Add(rb);
            }
            if (list.Count < 1) return;

            foreach (RadioButton item in list)
            {
                if (item.Checked)
                {
                    if (!Object.Equals(Entity[field.Name], item.Text)) Entity.SetItem(field.Name, item.Text);
                }
            }
        }
        #endregion

        #region 验证
        /// <summary>
        /// 验证表单，返回是否有效数据
        /// </summary>
        /// <returns></returns>
        protected virtual Boolean ValidForm()
        {
            foreach (FieldItem item in Entity<TEntity>.Meta.Fields)
            {
                Control control = Page.FindControl(FormItemPrefix + item.Name);
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
                        WebHelper.Alert(String.Format("{0}不能为空！", String.IsNullOrEmpty(field.DisplayName) ? field.Name : field.DisplayName));
                        control.Focus();
                        return false;
                    }
                }
                else if (field.Type == typeof(DateTime))
                {
                    DateTime d = (DateTime)Entity[field.Name];
                    if (d == DateTime.MinValue || d == DateTime.MaxValue)
                    {
                        WebHelper.Alert(String.Format("{0}不能为空！", String.IsNullOrEmpty(field.DisplayName) ? field.Name : field.DisplayName));
                        control.Focus();
                        return false;
                    }
                }
            }

            return true;
        }
        #endregion

        #region 保存
        private void SaveFormWithTrans()
        {
            Entity<TEntity>.Meta.BeginTrans();
            try
            {
                SaveForm();

                Entity<TEntity>.Meta.Commit();

                SaveFormSuccess();
            }
            catch (Exception ex)
            {
                Entity<TEntity>.Meta.Rollback();

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
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('成功！');parent.Dialog.CloseAndRefresh(frameElement);", true);
        }

        /// <summary>
        /// 保存失败
        /// </summary>
        /// <param name="ex"></param>
        protected virtual void SaveFormUnsuccess(Exception ex)
        {
            WebHelper.Alert("失败！" + ex.Message);
        }
        #endregion
    }
}