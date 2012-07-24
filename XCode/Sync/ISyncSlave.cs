using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.Sync
{
    /// <summary>同步框架从方</summary>
    public interface ISyncSlave
    {
        #region 属性
        /// <summary>最后修改时间。包括修改同步状态为假删除</summary>
        DateTime LastUpdate { get; set; }

        /// <summary>最后同步时间。包括向主方询问数据是否已删除</summary>
        DateTime LastSync { get; set; }

        /// <summary>同步状态。默认0添加1删除2</summary>
        Int32 SyncStatus { get; set; }
        #endregion
    }
}