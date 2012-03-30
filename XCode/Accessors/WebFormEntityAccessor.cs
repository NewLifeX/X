using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Reflection;
using NewLife.Web;
using XCode.Common;
using XCode.Configuration;

namespace XCode.Accessors
{
    /// <summary>WebForm实体访问器</summary>
    class WebFormEntityAccessor : EntityAccessorBase
    {
        #region 属性
        private Control _Container;
        /// <summary>页面</summary>
        public Control Container
        {
            get { return _Container; }
            private set { _Container = value; }
        }

        private Int64 _MaxLength = 10 * 1024 * 1024;
        /// <summary>最大文件大小，默认10M</summary>
        public Int64 MaxLength
        {
            get { return _MaxLength; }
            set { _MaxLength = value; }
        }

        private String _ItemPrefix = "frm";
        /// <summary>前缀</summary>
        public String ItemPrefix
        {
            get { return _ItemPrefix; }
            set { _ItemPrefix = value; }
        }
        #endregion

        #region 设置
        /// <summary>设置参数。返回自身，方便链式写法。</summary>>
        /// <param name="name">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public override IEntityAccessor SetConfig(string name, object value)
        {
            if (name.EqualIgnoreCase(EntityAccessorOptions.Container))
                Container = value as Control;
            else if (name.EqualIgnoreCase(EntityAccessorOptions.MaxLength))
                MaxLength = (Int64)value;
            else if (name.EqualIgnoreCase(EntityAccessorOptions.ItemPrefix))
                ItemPrefix = (String)value;

            return base.SetConfig(name, value);
        }
        #endregion

        #region 读取
        /// <summary>外部=>实体，从外部读取指定实体字段的信息</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected override void ReadItem(IEntity entity, FieldItem item)
        {
            Control control = FindControlByField(item);
            if (control == null) return;

            //try
            //{
            GetFormItem(entity, item, control);
            //}
            //catch (Exception ex)
            //{
            //    throw new XCodeException("读取" + item.Name + "的数据时出错！" + ex.Message, ex);
            //}
        }

        /// <summary>把控件的值设置到实体属性上</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItem(IEntity entity, FieldItem field, Control control)
        {
            if (field == null || control == null) return;

            if (control is WebControl)
            {
                WebControl wc = control as WebControl;

                // 分控件处理
                if (wc is TextBox)
                    GetFormItemTextBox(entity, field, wc as TextBox);
                else if (wc is Label)
                    GetFormItemLabel(entity, field, wc as Label);
                else if (wc is RadioButton)
                    GetFormItemRadioButton(entity, field, wc as RadioButton);
                else if (wc is CheckBox)
                    GetFormItemCheckBox(entity, field, wc as CheckBox);
                else if (wc is ListControl)
                    GetFormItemListControl(entity, field, wc as ListControl);
                else
                {
                    Object v = null;
                    if (GetControlValue(control, out v) && !Object.Equals(entity[field.Name], v)) SetEntityItem(entity, field, v);
                }
            }
            else
            {
                Object v = null;
                if (GetControlValue(control, out v) && !Object.Equals(entity[field.Name], v)) SetEntityItem(entity, field, v);
            }
        }

        void SetEntityItem(IEntity entity, FieldItem field, Object value)
        {
            // 先转为目标类型
            value = TypeX.ChangeType(value, field.Type);
            // 如果是字符串，并且为空，则让它等于实体里面的值，避免影响脏数据
            if (field.Type == typeof(String) && String.IsNullOrEmpty((String)value) && String.IsNullOrEmpty((String)entity[field.Name])) value = entity[field.Name];
            entity.SetItem(field.Name, value);
        }

        /// <summary>文本框</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemTextBox(IEntity entity, FieldItem field, TextBox control)
        {
            //String v = control.Text;
            //if (!Object.Equals(entity[field.Name], v)) SetEntityItem(field, v);

            Object v = null;
            if (!GetControlValue(control, out v)) v = control.Text;
            if (!Object.Equals(entity[field.Name], v))
            {
                // 如果是密码输入框，并且为空，不需要设置
                if (!String.IsNullOrEmpty("" + v) || control.TextMode != TextBoxMode.Password) SetEntityItem(entity, field, v);
            }
        }

        /// <summary>标签，不做任何操作</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemLabel(IEntity entity, FieldItem field, Label control)
        {

        }

        /// <summary>复选框</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemCheckBox(IEntity entity, FieldItem field, CheckBox control)
        {
            Type type = field.Type;
            Object v;
            if (type == typeof(Boolean))
                v = control.Checked;
            else if (type == typeof(Int32))
                v = control.Checked ? 1 : 0;
            else
                v = control.Checked;

            if (!Object.Equals(entity[field.Name], v)) SetEntityItem(entity, field, v);
        }

        /// <summary>列表框</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemListControl(IEntity entity, FieldItem field, ListControl control)
        {
            //if (String.IsNullOrEmpty(control.SelectedValue)) return;

            String v = control.SelectedValue;
            if (!Object.Equals(entity[field.Name], v)) SetEntityItem(entity, field, v);
        }

        /// <summary>单选框</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemRadioButton(IEntity entity, FieldItem field, RadioButton control)
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

            // 特殊处理数字
            if (field.Type == typeof(Int32))
            {
                Int32 id = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Checked)
                    {
                        id = i;
                        break;
                    }
                }
                if (id >= 0 && !Object.Equals(entity[field.Name], id)) SetEntityItem(entity, field, id);
            }
            else
            {
                foreach (RadioButton item in list)
                {
                    if (item.Checked)
                    {
                        if (!Object.Equals(entity[field.Name], item.Text))
                        {
                            SetEntityItem(entity, field, item.Text);
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region 写入
        /// <summary>实体=>外部，把指定实体字段的信息写入到外部</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected override void WriteItem(IEntity entity, FieldItem item)
        {
            Control control = FindControlByField(item);
            if (control == null) return;

            Boolean canSave = true;
            //try
            //{
            SetFormItem(entity, item, control, canSave);
            //}
            //catch (Exception ex)
            //{
            //    throw new XCodeException("设置" + item.Name + "的数据时出错！" + ex.Message, ex);
            //}
        }

        /// <summary>把实体成员的值设置到控件上</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItem(IEntity entity, FieldItem field, Control control, Boolean canSave)
        {
            if (field == null || control == null) return;

            String toolTip = field.DisplayName;
            if (field.IsNullable)
                toolTip = String.Format("请填写{0}！", toolTip);
            else
                toolTip = String.Format("必须填写{0}！", toolTip);

            if (control is Label) toolTip = null;

            if (control is WebControl)
            {
                WebControl wc = control as WebControl;

                // 设置ToolTip
                if (String.IsNullOrEmpty(wc.ToolTip) && !String.IsNullOrEmpty(toolTip)) wc.ToolTip = toolTip;

                //// 必填项
                //if (!field.IsNullable) SetNotAllowNull(field, control, canSave);

                // 设置只读，只有不能保存时才设置，因为一般都不是只读，而前端可能自己设置了控件为只读，这种情况下这里再设置就会修改前端的意思
                if (!canSave)
                {
                    if (wc is TextBox)
                        (wc as TextBox).ReadOnly = !canSave;
                    else
                        wc.Enabled = canSave;
                }

                // 分控件处理
                if (wc is TextBox)
                    SetFormItemTextBox(entity, field, wc as TextBox, canSave);
                else if (wc is Label)
                    SetFormItemLabel(entity, field, wc as Label, canSave);
                else if (wc is RadioButton)
                    SetFormItemRadioButton(entity, field, wc as RadioButton, canSave);
                else if (wc is CheckBox)
                    SetFormItemCheckBox(entity, field, wc as CheckBox, canSave);
                else if (wc is ListControl)
                    SetFormItemListControl(entity, field, wc as ListControl, canSave);
                else
                {
                    SetControlValue(control, entity[field.Name]);
                }
            }
            else
            {
                SetControlValue(control, entity[field.Name]);

                PropertyInfoX pix = PropertyInfoX.Create(control.GetType(), "ToolTip");
                if (pix != null && String.IsNullOrEmpty((String)pix.GetValue(control)))
                {
                    pix.SetValue(control, toolTip);
                }
            }
        }

        /// <summary>文本框</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItemTextBox(IEntity entity, FieldItem field, TextBox control, Boolean canSave)
        {
            Type type = field.Type;
            Object value = entity[field.Name];
            if (type == typeof(DateTime))
            {
                DateTime d = (DateTime)value;
                // 有时候可能并不需要默认时间
                //if (Helper.IsEntityNullKey(entity) && d == DateTime.MinValue) d = DateTime.Now;
                value = d.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (!SetControlValue(control, value)) control.Text = value != null ? value.ToString() : "";
        }

        /// <summary>标签</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItemLabel(IEntity entity, FieldItem field, Label control, Boolean canSave)
        {
            Type type = field.Type;
            if (type == typeof(DateTime))
            {
                DateTime d = (DateTime)entity[field.Name];
                if (Helper.IsEntityNullKey(entity) && d == DateTime.MinValue) d = DateTime.Now;
                control.Text = d.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if (type == typeof(Decimal))
            {
                Decimal d = (Decimal)entity[field.Name];
                control.Text = d.ToString("c");
            }
            else
                control.Text = String.Empty + entity[field.Name];
        }

        /// <summary>复选框</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItemCheckBox(IEntity entity, FieldItem field, CheckBox control, Boolean canSave)
        {
            Type type = field.Type;
            if (type == typeof(Boolean))
                control.Checked = (Boolean)entity[field.Name];
            else if (type == typeof(Int32))
                control.Checked = (Int32)entity[field.Name] != 0;
            else
                control.Checked = entity[field.Name] != null;
        }

        /// <summary>列表框</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItemListControl(IEntity entity, FieldItem field, ListControl control, Boolean canSave)
        {
            if (control.Items.Count < 1 || !String.IsNullOrEmpty(control.DataSourceID))
            {
                control.DataBind();
                // 这个赋值会影响RequiresDataBinding，而RequiresDataBinding会导致列表控件在OnPreRender阶段重新绑定，造成当前设定的值丢失。
                //control.AppendDataBoundItems = false;
            }
            if (control.Items.Count < 1) return;

            String value = String.Empty + entity[field.Name];

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

        /// <summary>单选框</summary>>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItemRadioButton(IEntity entity, FieldItem field, RadioButton control, Boolean canSave)
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

            // 特殊处理数字
            if (field.Type == typeof(Int32))
            {
                Int32 id = (Int32)entity[field.Name];
                if (id < 0 || id >= list.Count) id = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Checked = (i == id);
                }
            }
            else
            {
                String value = String.Empty + entity[field.Name];

                foreach (RadioButton item in list)
                {
                    item.Checked = item.Text == value;
                }
            }
        }

        /// <summary>设置控件的不允许空</summary>>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetNotAllowNull(FieldItem field, Control control, Boolean canSave)
        {
            if (field.IsNullable) return;
            // Label后面不需要
            if (control is Label) return;

            LiteralControl lc = new LiteralControl();
            lc.Text = "<font style='color:#FF0000;font-size:16pt;'> *</font>";

            Int32 p = control.Parent.Controls.IndexOf(control);
            // 有时候可能无法添加，但是不影响使用，应该屏蔽异常
            try
            {
                control.Parent.Controls.AddAt(p + 1, lc);
            }
            catch { }
        }
        #endregion

        #region 辅助
        static Boolean GetControlValue(Control control, out Object value)
        {
            TypeX tx = control.GetType();
            String name = tx.GetCustomAttributeValue<ControlValuePropertyAttribute, String>();
            PropertyInfoX pix = null;
            if (!String.IsNullOrEmpty(name)) pix = PropertyInfoX.Create(tx.BaseType, name);
            if (pix == null) pix = PropertyInfoX.Create(tx.BaseType, "Value");
            if (pix == null) pix = PropertyInfoX.Create(tx.BaseType, "Text");
            if (pix != null)
            {
                value = pix.GetValue(control);
                return true;
            }

            value = null;
            return false;
        }

        static Boolean SetControlValue(Control control, Object value)
        {
            TypeX tx = control.GetType();
            String name = tx.GetCustomAttributeValue<ControlValuePropertyAttribute, String>();
            PropertyInfoX pix = null;
            if (!String.IsNullOrEmpty(name)) pix = PropertyInfoX.Create(tx.BaseType, name);
            if (pix == null) pix = PropertyInfoX.Create(tx.BaseType, "Value");
            if (pix == null) pix = PropertyInfoX.Create(tx.BaseType, "Text");
            if (pix != null)
            {
                if (value == null && pix.Type.IsValueType) return false;
                pix.SetValue(control, value);
                return true;
            }

            return false;
        }

        /// <summary>查找表单控件</summary>>
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

        /// <summary>查找字段对应的控件</summary>>
        /// <param name="field"></param>
        /// <returns></returns>
        protected virtual Control FindControlByField(FieldItem field)
        {
            return FindControl(ItemPrefix + field.Name);
        }
        #endregion
    }
}