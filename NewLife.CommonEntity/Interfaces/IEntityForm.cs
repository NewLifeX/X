using System;
using System.Web.UI;
using XCode;
using XCode.Accessors;

namespace NewLife.CommonEntity
{
    /// <summary>实体表单接口</summary>
    public interface IEntityForm
    {
        /// <summary>
        /// 使用控件容器和实体类初始化接口
        /// </summary>
        /// <param name="container"></param>
        /// <param name="entityType"></param>
        void Init(Control container, Type entityType);

        /// <summary>访问器</summary>
        IEntityAccessor Accessor { get; set; }

        /// <summary>主键</summary>
        Object EntityID { get; }

        /// <summary>数据实体</summary>
        IEntity Entity { get; set; }

        /// <summary>是否新增</summary>
        Boolean IsNew { get; }

        #region 事件
        /// <summary>获取数据实体，允许页面重载改变实体</summary>
        event EventHandler<EventArgs<Object, IEntity>> OnGetEntity;

        /// <summary>把实体数据设置到表单后触发</summary>
        event EventHandler<EventArgs<IEntity>> OnSetForm;

        /// <summary>从表单上读取实体数据后触发</summary>
        event EventHandler<EventArgs<IEntity>> OnGetForm;

        /// <summary>验证时触发</summary>
        event EventHandler<EventArgs<IEntity>> OnValid;

        /// <summary>保存前触发，位于事务保护内</summary>
        event EventHandler<EventArgs<IEntity>> OnSaving;

        /// <summary>保存成功后触发，位于事务保护外</summary>
        event EventHandler<EventArgs<IEntity>> OnSaveSuccess;

        /// <summary>保存失败后触发，位于事务保护外</summary>
        event EventHandler<EventArgs<IEntity, Exception>> OnSaveFailure;

        /// <summary>从实体对象读取指定实体字段的信息后触发</summary>
        event EventHandler<EntityAccessorEventArgs> OnRead;

        /// <summary>把指定实体字段的信息写入到实体对象后触发</summary>
        event EventHandler<EntityAccessorEventArgs> OnWrite;
        #endregion
    }
}