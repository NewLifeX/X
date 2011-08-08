using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.Common
{
    /// <summary>
    /// 助手类
    /// </summary>
    static class Helper
    {
        public static Boolean IsIntType(Type type)
        {
            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    break;
            }

            return false;
        }
    }
}