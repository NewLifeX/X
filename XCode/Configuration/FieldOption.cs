using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCode.Configuration
{
    /// <summary>字段选项</summary>
    public class FieldOption
    {
        /// <summary>是否使用所有字段。默认true，除了基础数据字段外，包括使用扩展属性</summary>
        public Boolean AllFields { get; set; } = true;

        /// <summary>是否使用显示名。默认false，使用英文名，否则使用中文显示名</summary>
        public Boolean DisplayName { get; set; }
    }
}