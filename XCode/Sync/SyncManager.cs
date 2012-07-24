using System;
using System.Collections.Generic;
using NewLife.Collections;

namespace XCode.Sync
{
    /// <summary>同步管理器</summary>
    public class SyncManager
    {
        #region 属性
        private ISyncMaster _Master;
        /// <summary>同步框架主方，数据提供者。</summary>
        public ISyncMaster Master { get { return _Master; } set { _Master = value; } }

        private ISyncSlave _Slave;
        /// <summary>同步框架从方，数据消费者</summary>
        public ISyncSlave Slave { get { return _Slave; } set { _Slave = value; } }

        private Int32 _BatchSize = 100;
        /// <summary>同步批大小</summary>
        public Int32 BatchSize { get { return _BatchSize; } set { _BatchSize = value; } }

        private ICollection<String> _Names;
        /// <summary>字段集合</summary>
        public ICollection<String> Names { get { return _Names ?? (_Names = new HashSet<String>(StringComparer.OrdinalIgnoreCase)); } set { _Names = value; } }
        #endregion

        #region 方法
        /// <summary>开始处理</summary>
        public void DoWork()
        {
            // 先处理本地添加的数据，因为可能需要修改主键。如果不是先处理添加，自增字段很有可能出现跟提供者一样的主键
            ProcessNew();

            // 在处理本地删除的数据
            ProcessDelete();

            // 最后处理更新的数据
            ProcessItems();
        }

        void ProcessNew()
        {
            var index = 0;
            while (true)
            {
                var arr = Slave.GetAllNew(index, BatchSize);
                if (arr == null || arr.Length < 1) break;

                // 提交
                var rs = Convert(Master.Insert(Convert(arr)));

                // 修正本地。
                // 这里不得不考虑一个问题，比如本地ID=3，提交到提供者后，ID=5，如果马上更新本地为ID=5，而本地刚好又有ID=5，将会很麻烦
                // 采用降序应该可以解决问题
                if (rs != null && rs.Length > 0)
                {
                    for (int i = arr.Length - 1; i >= 0; i--)
                    {
                        if (rs[i] != null) arr[i].ChangeKey(rs[i].Key);
                    }
                }

                // 更新同步时间
                var now = DateTime.Now;
                foreach (var item in arr)
                {
                    item.LastSync = now;
                    item.Save();
                }

                index += arr.Length;
            }
        }

        void ProcessDelete()
        {
            var index = 0;
            while (true)
            {
                var arr = Slave.GetAllDelete(index, BatchSize);
                if (arr == null || arr.Length < 1) break;

                // 准备要删除的主键
                var keys = new Object[arr.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    keys[i] = arr[i].Key;
                }
                // 提交
                var rs = Master.Delete(keys);

                // 删除本地
                if (rs != null && rs.Length > 0)
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (rs[i]) arr[i].Delete();
                    }
                }

                index += arr.Length;
            }
        }

        void ProcessItems()
        {
            var last = Slave.GetLastSync();
            var index = 0;
            while (true)
            {
                var rs = Master.CheckUpdate(last, index, BatchSize);
                if (rs == null || rs.Length < 1) break;

                foreach (var item in rs)
                {
                    ProcessItem(item);
                }

                index += rs.Length;
            }
        }

        private void ProcessItem(ISyncMasterEntity remote)
        {
            var local = Slave.FindByKey(remote.Key);
            // 本地不存在，新增；
            // 如果找到，但是同步时间为最小值，表示从未同步，那是新数据，碰巧主键与提供者的某条数据一致，可能性很小
            if (local == null || local.LastSync <= DateTime.MinValue)
            {
                AddItem(remote);
                return;
            }

            // 本地没有修改
            if (local.LastUpdate < local.LastSync)
            {

            }

            //switch (local.SyncStatus)
            //{
            //    case SyncStatus.None:
            //        break;
            //    case SyncStatus.Update:
            //        break;
            //    case SyncStatus.Insert:
            //        break;
            //    case SyncStatus.Delete:
            //        break;
            //    default:
            //        break;
            //}
        }

        void AddItem(ISyncMasterEntity remote)
        {

        }
        #endregion

        #region 事件
        ///// <summary>删除冲突。默认将会</summary>
        //public event EventHandler<CancelEventArgs> DeleteConflict;
        #endregion

        #region 转换
        ISyncMasterEntity[] Convert(ISyncSlaveEntity[] arr)
        {
            var rs = new ISyncMasterEntity[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                rs[i] = Convert(arr[i]);
            }
            return rs;
        }

        ISyncSlaveEntity[] Convert(ISyncMasterEntity[] arr)
        {
            var rs = new ISyncSlaveEntity[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                rs[i] = Convert(arr[i]);
            }
            return rs;
        }

        ISyncMasterEntity Convert(ISyncSlaveEntity entity)
        {
            var rs = Master.Create();
            foreach (var item in Names)
            {
                rs[item] = entity[item];
            }
            return rs;
        }

        ISyncSlaveEntity Convert(ISyncMasterEntity entity)
        {
            var rs = Slave.Create();
            foreach (var item in Names)
            {
                rs[item] = entity[item];
            }
            return rs;
        }
        #endregion
    }
}