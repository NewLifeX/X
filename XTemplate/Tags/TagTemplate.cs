using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace XTemplate.Tags
{
    /// <summary>标签模版</summary>
    public class TagTemplate
    {
        static Regex _tag = new Regex(@"", RegexOptions.Compiled);

        /// <summary></summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public String Process(String str)
        {
            return str;
        }
    }
}