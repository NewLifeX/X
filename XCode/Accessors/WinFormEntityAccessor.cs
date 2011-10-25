using System;
using System.Windows.Forms;
using NewLife;
using NewLife.Reflection;
using XCode.Configuration;
using NewLife.Web;
using System.Reflection;

namespace XCode.Accessors
{
    /// <summary>
    /// WinForm实体访问器
    /// </summary>
    class WinFormEntityAccessor : EntityAccessorBase
    {
        #region 属性
        private Control _Container;
        /// <summary>容器</summary>
        public Control Container
        {
            get { return _Container; }
            set { _Container = value; }
        }

        private ToolTip _ToolTip;
        /// <summary>提示信息</summary>
        public ToolTip ToolTip
        {
            get { return _ToolTip; }
            set { _ToolTip = value; }
        }

        private Boolean _IsFindChildForm;
        /// <summary>
        /// 是否在子窗体中查询
        /// 这里泛指Form嵌套Form
        /// </summary>
        public Boolean IsFindChildForm
        {
            get { return _IsFindChildForm; }
            set { _IsFindChildForm = value; }
        }

        private String _ItemPrefix = "frm";
        /// <summary>前缀</summary>
        public String ItemPrefix
        {
            get { return _ItemPrefix; }
            set { _ItemPrefix = value; }
        }
        #endregion

        #region 方法
        ///// <summary>
        ///// 实例化一个WinForm实体访问器
        ///// </summary>
        ///// <param name="container"></param>
        //public WinFormEntityAccessor(Control container)
        //{
        //    if (container == null) throw new ArgumentNullException("page");

        //    Container = container;
        //}

        /// <summary>
        /// 设置参数。返回自身，方便链式写法。
        /// </summary>
        /// <param name="name">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public override IEntityAccessor SetConfig(string name, object value)
        {
            if (name.EqualIgnoreCase("Container"))
            {
                Container = value as Control;
                ToolTip = FindToolTipParentForm();
            }
            if (name.EqualIgnoreCase("Parent")) Container = value as Control;
            if (name.EqualIgnoreCase("ItemPrefix")) ItemPrefix = (String)value;
            if (name.EqualIgnoreCase("IsFindChildForm")) IsFindChildForm = (Boolean)value;
            return base.SetConfig(name, value);
        }

        #endregion

        #region 读取
        /// <summary>
        /// 外部=>实体，从外部读取指定实体字段的信息
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected override void OnReadItem(IEntity entity, FieldItem item)
        {
            Control control = FindControlByField(item);
            if (control == null) return;

            try
            {
                GetFormItem(entity, item, control);                
            }
            catch (Exception ex)
            {
                throw new Exception("设置" + item.Name + "的数据时出错！" + ex.Message);
            }
        }

        /// <summary>
        /// 
        /// 是否应考滤值转换，如列表，单选，多选其显示文字或写value不同
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void GetFormItem(IEntity entity, FieldItem field, Control control)
        {
            if (field == null || control == null) return;

            if (control is System.Windows.Forms.Control)
            {
                //文本框、RichTextBox ,MaskedTextBox  
                if (control is TextBoxBase)
                    GetTextBoxBase(entity, field, control as TextBoxBase);
                //单选、多选
                else if (control is ButtonBase)
                    GetButtonBase(entity, field, control as ButtonBase);
                //日期和时间
                else if (control is DateTimePicker)
                    GetDateTimePicker(entity, field, control as DateTimePicker);
                //数字
                else if (control is NumericUpDown)
                    GetNumericUpDown(entity, field, control as NumericUpDown);
                //ListBox、ComboBox、CheckedListBox 
                else if (control is ListControl)
                    GetListControl(entity, field, control as ListControl);
                //Label
                else if (control is Label)
                    GetLabel(entity, field, control as Label);
                else
                    throw new Exception("不受支持的控件类型！");

                //是否需要从中读出记录集
                //ListView
                //DataGridView
            }

        }

        /// <summary>
        /// 设置实体类值
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        void SetEntityItem(IEntity entity, FieldItem field, Object value)
        {
            // 先转为目标类型
            value = TypeX.ChangeType(value, field.Type);
            // 如果是字符串，并且为空，则让它等于实体里面的值，避免影响脏数据
            if (field.Type == typeof(String) && String.IsNullOrEmpty((String)value) && String.IsNullOrEmpty((String)entity[field.Name])) value = entity[field.Name];
            entity.SetItem(field.Name, value);
        }

        /// <summary>
        /// 获取TextBoxBase填充实体类
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="readOrwrite">读或写</param>
        private void GetTextBoxBase(IEntity entity, FieldItem field, TextBoxBase control)
        {
            Object v = control.Text;
            if (!Object.Equals(entity[field.Name], v)) SetEntityItem(entity, field, v);
        }

        /// <summary>
        /// 获取ButtonBase填充实体类
        /// 支持RadioButton,CheckBox
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void GetButtonBase(IEntity entity, FieldItem field, ButtonBase control)
        {
            Object v;
            if (control is RadioButton)
                v = ((RadioButton)control).Checked;
            else if (control is CheckBox)
                v = ((CheckBox)control).Checked;
            else
                throw new Exception("不接爱Button控件！");

            if (field.Type == typeof(Int32))
                v = (Boolean)v ? 1 : 0;

            if (!Object.Equals(entity[field.Name], v)) SetEntityItem(entity, field, v);
        }

        /// <summary>
        /// 获取DateTimePicker填充实体类
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void GetDateTimePicker(IEntity entity, FieldItem field, DateTimePicker control)
        {
            Object v = control.Value;
            if (!Object.Equals(entity[field.Name], v)) SetEntityItem(entity, field, v);
        }

        /// <summary>
        /// 获取NumericUpDown填充实体类
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void GetNumericUpDown(IEntity entity, FieldItem field, NumericUpDown control)
        {
            Object v = control.Value;
            if (!Object.Equals(entity[field.Name], v)) SetEntityItem(entity, field, v);
        }

        /// <summary>
        /// 获取ListControl填充实体类
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void GetListControl(IEntity entity, FieldItem field, ListControl control)
        {
            Object v = control.SelectedValue;
            if (v == null)
                v = control.Text;
            if (!Object.Equals(entity[field.Name], v)) SetEntityItem(entity, field, v);
        }

        /// <summary>
        /// 获取Label填充实体类
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void GetLabel(IEntity entity, FieldItem field, Label control)
        {
            Object v = control.Text;
            if (!Object.Equals(entity[field.Name], v)) SetEntityItem(entity, field, v);
        }
        #endregion

        #region 写入
        /// <summary>
        /// 实体=>外部，把指定实体字段的信息写入到外部
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected override void OnWriteItem(IEntity entity, FieldItem item)
        {
            Control control = FindControlByField(item);
            if (control == null) return;

            Boolean canSave = true;
            try
            {
                SetFormItem(entity, item, control, canSave);
            }
            catch (Exception ex)
            {
                throw new Exception("设置" + item.Name + "的数据时出错！" + ex.Message);
            }
        }

        /// <summary>
        /// 把实体成员的值设置到控件上
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        protected virtual void SetFormItem(IEntity entity, FieldItem field, Control control, Boolean canSave)
        {
            if (field == null || control == null) return;

            String toolTip = String.IsNullOrEmpty(field.Description) ? field.Name : field.Description;
            if (field.IsNullable)
                toolTip = String.Format("请填写{0}！", toolTip);
            else
                toolTip = String.Format("必须填写{0}！", toolTip);


            if (control is Control)
            {
                Control wc = control as Control;

                // 设置ToolTip
                SetToolTip(wc, toolTip);

                //// 必填项
                //if (!field.IsNullable) SetNotAllowNull(field, control, canSave);

                // 设置只读，只有不能保存时才设置，因为一般都不是只读，而前端可能自己设置了控件为只读，这种情况下这里再设置就会修改前端的意思
                if (!canSave)
                {
                    SetControlEnable(wc, canSave);
                }

                //文本框、RichTextBox ,MaskedTextBox  
                if (control is TextBoxBase)
                    SetTextBoxBase(entity, field, control as TextBoxBase);
                //单选、多选
                else if (control is ButtonBase)
                    SetButtonBase(entity, field, control as ButtonBase);
                //日期和时间
                else if (control is DateTimePicker)
                    SetDateTimePicker(entity, field, control as DateTimePicker);
                //数字
                else if (control is NumericUpDown)
                    SetNumericUpDown(entity, field, control as NumericUpDown);
                //ListBox、ComboBox、CheckedListBox 
                else if (control is ListControl)
                    SetListControl(entity, field, control as ListControl);
                //Label
                else if (control is Label)
                    SetLabel(entity, field, control as Label);
                else
                    throw new Exception("不受支持的控件类型！");
            }
        }

        /// <summary>
        /// 将实体信息添充至TextBoxBase
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void SetTextBoxBase(IEntity entity, FieldItem field, TextBoxBase control)
        {
            Type type = field.Type;
            Object value = entity[field.Name];

            control.Text = value.ToString();
        }

        /// <summary>
        /// 将实体信息添充至ButtonBase
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void SetButtonBase(IEntity entity, FieldItem field, ButtonBase control)
        {
            Type type = field.Type;
            Object value = entity[field.Name];

            Boolean isChecked = false;

            if (type == typeof(bool))
                isChecked = (Boolean)value;
            else if (type == typeof(int))
                isChecked = (int)value == 0 ? false : true;
            else if (type == typeof(string))
            {
                String valueString = (String)value;
                if (String.IsNullOrEmpty(valueString))
                    isChecked = false;
                else
                    isChecked = valueString == "1" || valueString.EqualIgnoreCase("true") ? true : false;
            }

            if (control is RadioButton)
                ((RadioButton)control).Checked = isChecked;
            else if (control is CheckBox)
                ((CheckBox)control).Checked = isChecked;
            else
                throw new Exception("不接爱Button控件！");

        }


        /// <summary>
        /// 将实体信息添充至DateTimePicker
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void SetDateTimePicker(IEntity entity, FieldItem field, DateTimePicker control)
        {
            Type type = field.Type;
            Object value = entity[field.Name];

            DateTime valueDateTime = DateTime.MinValue;
            if (type != typeof(DateTime))
                DateTime.TryParse(value.ToString(), out valueDateTime);

            control.Value = valueDateTime;
        }

        /// <summary>
        /// 将实体信息添充至NumericUpDown
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void SetNumericUpDown(IEntity entity, FieldItem field, NumericUpDown control)
        {
            Type type = field.Type;
            Object value = entity[field.Name];

            Decimal valuedecimal;
            Decimal.TryParse(value.ToString(), out valuedecimal);

            control.Value = valuedecimal;
        }

        /// <summary>
        /// 将实体信息添充至ListControl
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void SetListControl(IEntity entity, FieldItem field, ListControl control)
        {
            Type type = field.Type;
            Object value = entity[field.Name];

            if (type == typeof(int))
                control.SelectedIndex = (int)value;
            else
            {
                control.SelectedValue = value;

                if (control.SelectedIndex == -1)
                {
                    if (control is ListBox)
                    {
                        (control as ListBox).SelectedItem = value;
                    }
                    else if (control is ComboBox)
                    {
                        (control as ComboBox).SelectedItem = value;
                    }
                }
            }
        }

        /// <summary>
        /// 将实体信息添充至Label
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        private void SetLabel(IEntity entity, FieldItem field, Label control)
        {
            Type type = field.Type;
            Object value = entity[field.Name];
            if (type == typeof(DateTime))
                control.Text = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
            else
                control.Text = value.ToString();
        }
        #endregion

        #region 辅助        /// <summary>
        /// 查找字段对应的控件
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected virtual Control FindControlByField(FieldItem field)
        {
            String name = ItemPrefix + field.Name;
            //Control control = FieldInfoX.GetValue<Control>(Container, name);
            //TODO:这里可能极为不完善，需要找到WinForm中控件默认命名方式

            return FindControlInContainer(name);
        }

        /// <summary>
        /// 在页面查找指定ID的控件
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Control FindControlInContainer(String name)
        {
            return FindControlByName(Container, name);
        }

        /// <summary>
        /// 按名称查询
        /// </summary>
        /// <param name="control"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public Control FindControlByName(Control control, String name)
        {
            if (control == null || control.Controls.Count == 0 || String.IsNullOrEmpty(name)) return null;

            Control r = null;

            foreach (Control item in control.Controls)
            {
                //不在子Form中查找
                if (IsFindChildForm && item is Form)
                    continue;

                if (item.Name.Equals(name))
                    r = item;
                else
                    r = FindControlByName(item, name);

                if (r != null)
                    break;
            }

            return r;
        }

        /// <summary>
        /// 在窗体中查询ToolTip
        /// </summary>
        /// <returns></returns>
        private ToolTip FindToolTipParentForm()
        {
            //在窗里中查找ToolTip实例 
            ToolTip r = null;

            if (Container == null) throw new Exception("Container 为空！");

            //查找窗体
            Control form = Container is Form ? Container : Container.FindForm();

            if (form == null) throw new Exception("ToolTip 父窗口查询失败！");

            FieldInfo toolTipField = null;
            FieldInfo[] fields = form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (fields != null && fields.Length > 0)
            {
                Type toolTip = typeof(System.Windows.Forms.ToolTip);
                foreach (FieldInfo item in fields)
                {
                    if (item.FieldType == toolTip)
                    {
                        toolTipField = item;
                        break;
                    }
                }
            }

            if (toolTipField != null)
                r = toolTipField.GetValue(form) as ToolTip;
            return null;

        }

        /// <summary>
        /// 设置ToolTip
        /// </summary>
        /// <param name="control"></param>
        /// <param name="caption"></param>
        private void SetToolTip(Control control, String caption)
        {
            if (ToolTip == null || control == null || String.IsNullOrEmpty(caption)) return;

            //检查生复设置
            if (String.IsNullOrEmpty(ToolTip.GetToolTip(control)))
                ToolTip.SetToolTip(control, caption);

        }

        /// <summary>
        /// 设置控件Enable值
        /// </summary>
        /// <param name="control"></param>
        /// <param name="canSave"></param>
        private void SetControlEnable(Control control, Boolean canSave)
        {

            //文本框、RichTextBox ,MaskedTextBox  
            if (control is TextBoxBase)
                (control as TextBoxBase).Enabled = canSave;
            //单选、多选
            else if (control is ButtonBase)
                (control as ButtonBase).Enabled = canSave;
            //日期和时间
            else if (control is DateTimePicker)
                (control as DateTimePicker).Enabled = canSave;
            //数字
            else if (control is NumericUpDown)
                (control as NumericUpDown).Enabled = canSave;
            //ListBox、ComboBox、CheckedListBox 
            else if (control is ListControl)
                (control as ListControl).Enabled = canSave;
            //Label
            else if (control is Label)
                (control as Label).Enabled = canSave;

        }
        #endregion
    }
}