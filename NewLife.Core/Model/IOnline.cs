using System;

namespace NewLife.Model
{
    /// <summary>在线接口，用户在线离线</summary>
    public interface IOnline
    {
        #region 属性
        /// <summary>会员</summary>
        Int32 UserID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>类型</summary>
        String Type { get; set; }

        /// <summary>会话</summary>
        Int32 SessionID { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

        /// <summary>创建地址</summary>
        String CreateIP { get; set; }

        /// <summary>更新时间</summary>
        DateTime UpdateTime { get; set; }
        #endregion

        /// <summary>异步保存。实现延迟保存，大事务保存。主要面向日志表和频繁更新的在线记录表</summary>
        /// <param name="msDelay">延迟保存的时间。默认0ms近实时保存</param>
        /// <returns>是否成功加入异步队列</returns>
        Boolean SaveAsync(Int32 msDelay = 0);

        /// <summary>从数据库中删除该对象</summary>
        /// <returns></returns>
        Int32 Delete();

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();
    }
}