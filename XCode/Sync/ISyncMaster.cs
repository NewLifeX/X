using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Reflection;

namespace XCode.Sync
{
    /// <summary>同步架构主方</summary>
    public interface ISyncMaster
    {
        #region 方法
        Object[] CheckUpdate(DateTime last, Int32 start, Int32 max);

        IIndexAccessor FindByKey(Object key);

        IIndexAccessor[] FindAllByKeys(Object[] key);
        #endregion
    }
}