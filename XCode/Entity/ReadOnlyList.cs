using System;
using System.Collections.Generic;
using System.Text;

namespace XCode
{
    /// <summary>
    /// 只读列表。
    /// </summary>
    /// <remarks>
    /// 拥有检测是否改变的功能，包括数量改变。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    internal class ReadOnlyList<T> : List<T>
    {
        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        public ReadOnlyList()
        {
            UpdateBackup();
        }

        /// <summary>
        /// 使用指定的枚举数实例化一个只读列表
        /// </summary>
        /// <param name="collection"></param>
        public ReadOnlyList(IEnumerable<T> collection)
            : base(collection)
        {
            UpdateBackup();
        }
        #endregion

        #region 属性
        private List<T> _Backup;
        /// <summary>备份</summary>
        public List<T> Backup
        {
            get { return _Backup; }
            private set { _Backup = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 更新备份
        /// </summary>
        public void UpdateBackup()
        {
            Backup = new List<T>(this);
        }

        ///// <summary>
        ///// 检查改变
        ///// </summary>
        ///// <returns></returns>
        //public Boolean CheckChange()
        //{
        //    if (Count != Backup.Count) return true;

        //    //for (int i = 0; i < Count; i++)
        //    //{
        //    //    if (!Object.Equals(this[i], Backup[i])) return true;
        //    //}

        //    return false;
        //}
        /// <summary>
        /// 是否改变
        /// </summary>
        public Boolean Changed { get { return Count != Backup.Count; } }

        /// <summary>
        /// 保持副本不会被改变
        /// </summary>
        /// <returns></returns>
        public ReadOnlyList<T> Keep()
        {
            if (Changed)
                return new ReadOnlyList<T>(Backup);
            else
                return this;
        }
        #endregion
    }
}