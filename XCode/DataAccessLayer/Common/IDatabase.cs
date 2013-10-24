using System;
using System.Data.Common;
using NewLife;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库接口</summary>
    /// <remarks>
    /// 抽象数据库的功能特点。
    /// 对于每一个连接字符串配置，都有一个数据库实例，而不是每个数据库类型一个实例，因为同类型数据库不同版本行为不同。
    /// </remarks>
    public interface IDatabase : IDisposable2
    {
        #region 属性
        /// <summary>数据库类型</summary>
        DatabaseType DbType { get; }

        /// <summary>数据库提供者工厂</summary>
        DbProviderFactory Factory { get; }

        /// <summary>链接名</summary>
        String ConnName { get; set; }

        /// <summary>链接字符串</summary>
        String ConnectionString { get; set; }

        /// <summary>拥有者</summary>
        String Owner { get; set; }

        /// <summary>数据库服务器版本</summary>
        String ServerVersion { get; }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        IDbSession CreateSession();

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        IMetaData CreateMetaData();

        /// <summary>是否支持该提供者所描述的数据库</summary>
        /// <param name="providerName">提供者</param>
        /// <returns></returns>
        Boolean Support(String providerName);
        #endregion

        #region 分页
        /// <summary>构造分页SQL</summary>
        /// <remarks>
        /// 两个构造分页SQL的方法，区别就在于查询生成器能够构造出来更好的分页语句，尽可能的避免子查询。
        /// MS体系的分页精髓就在于唯一键，当唯一键带有Asc/Desc/Unkown等排序结尾时，就采用最大最小值分页，否则使用较次的TopNotIn分页。
        /// TopNotIn分页和MaxMin分页的弊端就在于无法完美的支持GroupBy查询分页，只能查到第一页，往后分页就不行了，因为没有主键。
        /// </remarks>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        String PageSplit(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn);

        /// <summary>构造分页SQL</summary>
        /// <remarks>
        /// 两个构造分页SQL的方法，区别就在于查询生成器能够构造出来更好的分页语句，尽可能的避免子查询。
        /// MS体系的分页精髓就在于唯一键，当唯一键带有Asc/Desc/Unkown等排序结尾时，就采用最大最小值分页，否则使用较次的TopNotIn分页。
        /// TopNotIn分页和MaxMin分页的弊端就在于无法完美的支持GroupBy查询分页，只能查到第一页，往后分页就不行了，因为没有主键。
        /// </remarks>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        SelectBuilder PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows);
        #endregion

        #region 数据库特性
        /// <summary>当前时间函数</summary>
        String DateTimeNow { get; }

        /// <summary>最小时间</summary>
        DateTime DateTimeMin { get; }

        /// <summary>长文本长度</summary>
        Int32 LongTextLength { get; }

        /// <summary>获取Guid的函数</summary>
        String NewGuid { get; }

        /// <summary>格式化时间为SQL字符串</summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        String FormatDateTime(DateTime dateTime);

        /// <summary>格式化名称，如果不是关键字，则原样返回</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        String FormatName(String name);

        /// <summary>格式化数据为SQL数据</summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        String FormatValue(IDataColumn field, Object value);

        /// <summary>格式化标识列，返回插入数据时所用的表达式，如果字段本身支持自增，则返回空</summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        String FormatIdentity(IDataColumn field, Object value);

        /// <summary>格式化参数名</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        String FormatParameterName(String name);

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        String StringConcat(String left, String right);
        #endregion
    }
}