using System;

namespace NewLife.Model
{
    /// <summary>用户接口</summary>
    public interface IManageUser
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>昵称</summary>
        String NickName { get; set; }

        /// <summary>启用</summary>
        Boolean Enable { get; set; }
        #endregion
    }
}