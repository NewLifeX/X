using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库元数据接口
    /// </summary>
    public interface IDatabaseMeta
    {
        #region 属性
        /// <summary>
        /// 数据库类型
        /// </summary>
        DatabaseType DbType { get; }

        /// <summary>
        /// 工厂
        /// </summary>
        DbProviderFactory Factory { get; }
        #endregion

        #region 分页
        /// <summary>
        /// 构造分页SQL
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        String PageSplit(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn);

        /// <summary>
        /// 构造分页SQL
        /// </summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        String PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows, String keyColumn);
        #endregion

        #region 数据库特性
        /// <summary>
        /// 当前时间函数
        /// </summary>
        String DateTimeNow { get; }

        /// <summary>
        /// 最小时间
        /// </summary>
        DateTime DateTimeMin { get; }

        /// <summary>
        /// 格式化时间为SQL字符串
        /// </summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        String FormatDateTime(DateTime dateTime);

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        String FormatKeyWord(String keyWord);
        #endregion
    }
}