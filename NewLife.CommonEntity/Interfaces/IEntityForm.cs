using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;

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
    }
}