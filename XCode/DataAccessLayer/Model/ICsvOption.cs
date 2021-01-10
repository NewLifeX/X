using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// Csv导出选项目。
    /// 导出为Csv文件时，将长数字串转为字符串
    /// </summary>
    public interface ICsvOption
    {
        #region 属性
        /// <summary>
        /// 要标记的列名
        /// </summary>
        String Column { get; set; }
        /// <summary>标记位置</summary>
        AppendPositionEnum MarkPosition { get; set; }
        /// <summary>左侧标记符号</summary>
        String AppendLeftTag { get; set; }
        /// <summary>右侧标记符号</summary>
        String AppendRightTag { get; set; }
        
        /// <summary>
        /// 克隆
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        #endregion
        ICsvOption Clone(IDataColumn column);
    }
}
