using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Sdp
{
    /// <summary>SDP 属性</summary>
    public class SdpAttribute
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private String _Value;
        /// <summary>值</summary>
        public String Value { get { return _Value; } set { _Value = value; } }
        #endregion

        #region 方法
        /// <summary>分析</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SdpAttribute Parse(String value)
        {
            if (String.IsNullOrEmpty(value)) throw new ArgumentNullException("value");

            Int32 p = value.IndexOf("=");
            Int32 p2 = value.IndexOf(":", p);

            var entity = new SdpAttribute();
            if (p2 > 0)
            {
                entity.Name = value.Substring(p + 1, p2 - p - 1);
                entity.Value = value.Substring(p2 + 1);
            }
            else
                entity.Name = value.Substring(p + 1);

            return entity;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (String.IsNullOrEmpty(Value))
                return String.Format("a={0}", Name);
            else
                return String.Format("a={0}:{1}", Name, Value);
        }
        #endregion
    }
}