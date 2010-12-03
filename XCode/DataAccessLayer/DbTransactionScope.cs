using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库事务代码块
    /// </summary>
    public class DbTransactionScope : IDisposable
    {
        private IDatabase _DB;
        /// <summary>数据库</summary>
        public IDatabase DB
        {
            get { return _DB; }
            set { _DB = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        public DbTransactionScope(IDatabase db)
        {
        }

        #region IDisposable 成员
        private Boolean disposed = false;
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;


        }
        #endregion
    }
}
