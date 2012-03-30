using System;
using System.Web.UI;
using XCode;
using XCode.Accessors;
using System.ComponentModel;

namespace NewLife.CommonEntity
{
    /// <summary>实体表单接口</summary>
    public interface IEntityForm
    {
        #region 初始化
        /// <summary>使用控件容器和实体类初始化接口</summary>>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        IEntityForm Init(Control container, Type entityType);
        #endregion

        #region 属性
        /// <summary>访问器</summary>
        IEntityAccessor Accessor { get; set; }

        /// <summary>主键</summary>
        Object EntityID { get; }

        /// <summary>数据实体</summary>
        IEntity Entity { get; set; }

        /// <summary>是否新增</summary>
        Boolean IsNew { get; }
        #endregion

        #region 方法
        /// <summary>把实体的属性设置到控件上</summary>
        void SetForm();

        /// <summary>从表单上读取实体数据</summary>
        void GetForm();

        /// <summary>验证表单，返回是否有效数据，决定是否保存表单数据</summary>
        /// <returns></returns>
        Boolean ValidForm();

        /// <summary>保存表单，把实体保存到数据库</summary>
        void SaveForm();
        #endregion

        #region 事件
        /// <summary>获取数据实体，允许页面重载改变实体</summary>
        event EventHandler<EntityFormEventArgs> OnGetEntity;

        /// <summary>把实体数据设置到表单后触发</summary>
        event EventHandler<EntityFormEventArgs> OnSetForm;

        /// <summary>从表单上读取实体数据后触发</summary>
        event EventHandler<EntityFormEventArgs> OnGetForm;

        /// <summary>验证时触发。可通过设置Cancel=true使得验证失败</summary>
        event EventHandler<EntityFormEventArgs> OnValid;

        /// <summary>保存前触发，位于事务保护内。可通过设置Cancel=true使得后续不调用SaveForm</summary>
        event EventHandler<EntityFormEventArgs> OnSaving;

        /// <summary>保存成功后触发，位于事务保护外。可通过设置Cancel=true使得不显示默认提示</summary>
        event EventHandler<EntityFormEventArgs> OnSaveSuccess;

        /// <summary>保存失败后触发，位于事务保护外。可通过设置Cancel=true使得不显示默认提示</summary>
        event EventHandler<EntityFormEventArgs> OnSaveFailure;
        #endregion
    }

    /// <summary>实体表单事件参数</summary>
    public class EntityFormEventArgs : CancelEventArgs
    {
        #region 属性
        private Exception _Error;
        /// <summary>异常</summary>
        public Exception Error { get { return _Error; } set { _Error = value; } }
        #endregion
    }
}