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
    public class SQLRunEvent : IEvent
    {
        /// <summary>
        /// 
        /// </summary>
        public long RunTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Sql { get; set; }
    }
}
