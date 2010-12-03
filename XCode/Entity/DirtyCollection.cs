using System;
using System.Collections.Generic;
using System.Text;

namespace XCode
{
    /// <summary>
    /// 脏属性集合
    /// </summary>
    [Serializable]
    public class DirtyCollection : Dictionary<String, Boolean>
    {
        /// <summary>
        /// 获取或设置与指定的属性是否有脏数据。
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public new Boolean this[String item]
        {
            get
            {
                if (ContainsKey(item) && base[item])
                    return true;
                else
                    return false;
            }
            set
            {
                base[item] = value;
            }
        }
    }
}
