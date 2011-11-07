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

        /// <summary>获取数据实体，允许页面重载改变实体</summary>
        event EventHandler<EventArgs<Object, IEntity>> OnGetEntity;
    }
}