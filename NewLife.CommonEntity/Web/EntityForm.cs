using System;
using System.Collections.Generic;
using System.Text;
using XCode;
using System.Web.UI;
using NewLife.Web;
using System.Web.UI.WebControls;
using XCode.Configuration;

namespace NewLife.CommonEntity.Web
{
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
                String str = Request["KeyName"];
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
        public TEntity Entity
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
        private Boolean IsNullKey
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

        #region 加载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreLoad(EventArgs e)
        {
            base.OnPreLoad(e);

            if (!Page.IsPostBack)
            {
                DataBind();

                Control btn = SaveButton;
                if (btn != null)
                {
                    // 添加/编辑 按钮需要添加/编辑权限
                    if (IsNullKey)
                        btn.Visible = Acquire(PermissionFlags.Insert);
                    else
                        btn.Visible = Acquire(PermissionFlags.Update);
                }
            }
        }

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
                    wc.ToolTip = String.Format("请填写{0}！", field.Column.Description);
                }

                // 设置只读
                if (wc is TextBox)
                    (wc as TextBox).ReadOnly = !canSave;
                else
                    wc.Enabled = canSave;

                // 分控件处理
            }
        }

        protected virtual void SetFormItemTextBox(FieldItem field, TextBox control, Boolean canSave)
        {

        }
        #endregion

        #region 验证
        #endregion

        #region 保存
        #endregion
    }
}