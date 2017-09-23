using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCode
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlRunEventHandler : IEventHandler<SQLRunEvent>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        public void Handle(SQLRunEvent evt)
        {
            NewLife.Log.XTrace.WriteLine("Sql:{0},Run{1}", evt.Sql,evt.RunTime);
        }
    }
}
