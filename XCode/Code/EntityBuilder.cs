using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCode.Code
{
    /// <summary>实体类生成器</summary>
    public class EntityBuilder : ClassBuilder
    {
        /// <summary>实例化</summary>
        public EntityBuilder()
        {
            Usings.Add("XCode");
            Usings.Add("XCode.Configuration");
            Usings.Add("XCode.DataAccessLayer");
        }
    }
}