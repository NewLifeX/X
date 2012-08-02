using System;
#if NET4
using System.Linq;
#endif
using NewLife.Linq;
using NewLife.Reflection;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Sync
{
    /// <summary>同步框架从方接口</summary>
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

        /// <summary>获取所有未同步的旧数据</summary>
        /// <param name="now"></param>
        /// <param name="start"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        ISyncSlaveEntity[] GetAllOld(DateTime now, Int32 start, Int32 max);

        /// <summary>获取最后同步时间</summary>
        /// <returns></returns>
        DateTime GetLastSync();

        /// <summary>根据主键查找</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        ISyncSlaveEntity FindByKey(Object key);

        /// <summary>创建一个空白实体</summary>
        /// <returns></returns>
        ISyncSlaveEntity Create();

        /// <summary>获取要同步的字段名</summary>
        /// <returns></returns>
        String[] GetNames();
        #endregion
    }

    /// <summary>同步框架从方实体接口，由从方实体类实现</summary>
    public interface ISyncSlaveEntity : IIndexAccessor
    {
        #region 属性
        /// <summary>唯一标识数据的键值</summary>
        Object Key { get; }

        /// <summary>最后修改时间。包括修改同步状态为假删除</summary>
        DateTime LastUpdate { get; set; }

        /// <summary>最后同步时间。包括向主方询问数据是否已删除</summary>
        DateTime LastSync { get; set; }

        ///// <summary>同步状态。默认0添加1删除2</summary>
        //Int32 SyncStatus { get; set; }
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

    /// <summary>同步框架从方，实体类默认实现。要求实体类实现<see cref="ISyncSlaveEntity"/>接口</summary>
    public class SyncSlave : ISyncSlave
    {
        #region 属性
        private IEntityOperate _Facotry;
        /// <summary>工厂</summary>
        public IEntityOperate Facotry { get { return _Facotry; } set { _Facotry = value; } }

        /// <summary>主键名</summary>
        protected virtual String KeyName { get { return Facotry.Unique.Name; } }

        private String _LastUpdateName = "LastUpdate";
        /// <summary>最后更新字段名</summary>
        public virtual String LastUpdateName { get { return _LastUpdateName; } set { _LastUpdateName = value; } }

        private String _LastSyncName = "LastSync";
        /// <summary>最后更新字段名</summary>
        public virtual String LastSyncName { get { return _LastSyncName; } set { _LastSyncName = value; } }

        /// <summary>最后更新字段名。先硬编码，不考虑可变</summary>
        protected virtual Field LastUpdateField { get { return Facotry.Table.FindByName(LastUpdateName); } }

        /// <summary>最后同步字段名</summary>
        protected virtual Field LastSyncField { get { return Facotry.Table.FindByName(LastSyncName); } }
        #endregion

        #region 方法
        /// <summary>获取所有新添加的数据</summary>
        /// <param name="start"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public virtual ISyncSlaveEntity[] GetAllNew(Int32 start, Int32 max)
        {
            var dal = DAL.Create(Facotry.ConnName);
            return GetAll(LastSyncField <= dal.Db.DateTimeMin | LastSyncField == null, start, max);
        }

        /// <summary>获取所有删除的数据</summary>
        /// <param name="start"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public virtual ISyncSlaveEntity[] GetAllDelete(Int32 start, Int32 max)
        {
            var dal = DAL.Create(Facotry.ConnName);
            return GetAll(LastUpdateField <= dal.Db.DateTimeMin | LastUpdateField == null, start, max);
        }

        /// <summary>获取所有未同步的旧数据</summary>
        /// <param name="now"></param>
        /// <param name="start"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public virtual ISyncSlaveEntity[] GetAllOld(DateTime now, Int32 start, Int32 max)
        {
            //var dal = DAL.Create(Facotry.ConnName);
            return GetAll(LastSyncField < now, start, max);
        }

        ISyncSlaveEntity[] GetAll(String where, Int32 start, Int32 max)
        {
            var list = Facotry.FindAll(where, null, null, start, max);
            if (list == null || list.Count < 1) return null;

            var rs = new ISyncSlaveEntity[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                rs[i] = list[i] as ISyncSlaveEntity;
            }
            return rs;
        }

        /// <summary>获取最后同步时间</summary>
        /// <returns></returns>
        public virtual DateTime GetLastSync()
        {
            var dal = DAL.Create(Facotry.ConnName);
            // 有效同步时间升序，取一个，即为最小值
            var list = Facotry.FindAll(LastSyncField > dal.Db.DateTimeMin, LastSyncField.Asc(), null, 0, 1);
            if (list == null || list.Count < 1) return DateTime.MinValue;

            return (DateTime)list[0][LastSyncField];
        }

        /// <summary>根据主键查找</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual ISyncSlaveEntity FindByKey(Object key) { return Facotry.FindByKey(key) as ISyncSlaveEntity; }

        /// <summary>创建一个空白实体</summary>
        /// <returns></returns>
        public virtual ISyncSlaveEntity Create() { return Facotry.Create() as ISyncSlaveEntity; }

        /// <summary>获取要同步的字段名</summary>
        /// <returns></returns>
        public virtual String[] GetNames() { return Facotry.FieldNames.ToArray(); }
        #endregion
    }
}