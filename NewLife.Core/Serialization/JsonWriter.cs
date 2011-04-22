using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// Json写入器
    /// </summary>
    public class JsonWriter : WriterBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(byte value)
        {
            throw new NotImplementedException();
        }

        #region 写入对象
        /// <summary>
        /// 写入对象。具体读写器可以重载该方法以修改写入对象前后的行为。
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteObject(object value, Type type, WriteObjectCallback callback)
        {
            if (value == null)
            {
                Write("null");
                return true;
            }

            Write("[");
            Boolean rs = base.WriteObject(value, type, callback);
            Write("]");

            return rs;
        }
        #endregion
    }
}