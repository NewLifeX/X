using System;
using System.Collections.Generic;
using System.Text;

namespace XCode
{
    /// <summary>脏属性集合</summary>
    /// <remarks>实现IDictionary接口，为了让使用者能直接使用重载了的索引器</remarks>
    [Serializable]
    class DirtyCollection : Dictionary<String, Boolean>, IDictionary<String, Boolean>
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
                //if (ContainsKey(item) && base[item])
                //    return true;
                //else
                //    return false;
                return ContainsKey(item) && base[item];
            }
            set
            {
                base[item] = value;
            }
        }
    }
}
