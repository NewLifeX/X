using System;
using NewLife.Reflection;

namespace XCode.Sync
{
    /// <summary>同步框架从方</summary>
    public interface ISyncSlave
    {
        #region 方法
        /// <summary>获取所有新添加的数据</summary>
        /// <param name="start"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        ISyncSlaveEntity[] GetAllNew(Int32 start, Int32 max);

        /// <summary>获取所有删除的数据</summary>
        /// <param name="start"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        ISyncSlaveEntity[] GetAllDelete(Int32 start, Int32 max);

        /// <summary>获取最后同步时间</summary>
        /// <returns></returns>
        DateTime GetLastSync();

        /// <summary>根据主键查找</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        ISyncSlaveEntity FindByKey(Object key);

        //ISyncSlaveEntity[] FindAllByKeys(Object[] key);

        /// <summary>创建一个空白实体</summary>
        /// <returns></returns>
        ISyncSlaveEntity Create();
        #endregion
    }

    /// <summary>同步框架从方实体</summary>
    public interface ISyncSlaveEntity : IIndexAccessor
    {
        #region 属性
        /// <summary>唯一标识数据的键值</summary>
        Object Key { get; }

        /// <summary>最后修改时间。包括修改同步状态为假删除</summary>
        DateTime LastUpdate { get; set; }

        /// <summary>最后同步时间。包括向主方询问数据是否已删除</summary>
        DateTime LastSync { get; set; }

        /// <summary>同步状态。默认0添加1删除2</summary>
        Int32 SyncStatus { get; set; }
        #endregion

        #region 方法
        /// <summary>改变主键。本地新增加的数据，在提交到提供方后，可能主键会改变（如自增字段），需要更新本地主键为新主键</summary>
        /// <param name="key"></param>
        void ChangeKey(Object key);

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();

        /// <summary>删除本地数据</summary>
        /// <returns></returns>
        Int32 Delete();
        #endregion
    }

    ///// <summary>同步状态</summary>
    //public enum SyncStatus
    //{
    //    /// <summary>无</summary>
    //    [Description("无")]
    //    None,

    //    /// <summary>修改</summary>
    //    [Description("修改")]
    //    Update,

    //    /// <summary>新增</summary>
    //    [Description("新增")]
    //    Insert,

    //    /// <summary>删除</summary>
    //    [Description("删除")]
    //    Delete
    //}
}