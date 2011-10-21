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
            if (name.EqualIgnoreCase("Container")) Container = value as Control;
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

            Boolean canSave = true;
            try
            {
                //TODO:参考WebForm
                //GetFormItem(entity, item, control);
            }
            catch (Exception ex)
            {
                //WebHelper.Alert("设置" + item.Name + "的数据时出错！" + ex.Message);
                return;
            }
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

            try
            {
                //TODO:参考WebForm
                //SetFormItem(entity, item, control, canSave);
            }
            catch (Exception ex)
            {
                //WebHelper.Alert("读取" + item.Name + "的数据时出错！" + ex.Message);
                return;
            }
        }
        #endregion

        #region 辅助
        /// <summary>
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


        /// 在页面查找指定ID的控件，采用反射字段的方法，避免遍历Controls引起子控件构造
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
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
        #endregion
    }
}