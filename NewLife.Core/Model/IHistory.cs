using System;

namespace NewLife.Model
{
    /// <summary>历史接口，用户操作历史</summary>
    public interface IHistory
    {
        #region 属性
        /// <summary>会员</summary>
        Int32 UserID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>类型</summary>
        String Type { get; set; }

        /// <summary>操作</summary>
        String Action { get; set; }

        /// <summary>成功</summary>
        Boolean Success { get; set; }

        ///// <summary>创建者</summary>
        //Int32 CreateUserID { get; set; }

        ///// <summary>创建时间</summary>
        //DateTime CreateTime { get; set; }

        /// <summary>创建地址</summary>
        String CreateIP { get; set; }

        /// <summary>内容</summary>
        String Remark { get; set; }
        #endregion

        /// <summary>异步保存。实现延迟保存，大事务保存。主要面向日志表和频繁更新的在线记录表</summary>
        /// <param name="msDelay">延迟保存的时间。默认0ms近实时保存</param>
        /// <returns>是否成功加入异步队列</returns>
        Boolean SaveAsync(Int32 msDelay = 0);
    }
}