using System;
using NewLife.Reflection;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>MS系列数据库分页算法</summary>
    public static class MSPageSplit
    {
        /// <summary>分页算法</summary>
        /// <remarks>
        /// builder里面必须含有排序，否则就要通过key指定主键，否则大部分算法不能使用，会导致逻辑数据排序不正确。
        /// 其实，一般数据都是按照聚集索引排序，而聚集索引刚好也就是主键。
        /// 所以，只要设置的Key顺序跟主键顺序一致，就没有问题。
        /// 如果，Key指定了跟主键不一致的顺序，那么查询语句一定要指定同样的排序。
        /// </remarks>
        /// <param name="builder"></param>
        /// <param name="startRowIndex"></param>
        /// <param name="maximumRows"></param>
        /// <param name="isSql2005"></param>
        /// <param name="queryCountCallback">查询总记录数的委托，近供DoubleTop使用</param>
        /// <returns></returns>
        public static SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows, Boolean isSql2005, Func<SelectBuilder, Int64> queryCountCallback = null)
        {
            // 从第一行开始，不需要分页
            // 2012.11.08 注释掉首页使用SELECT TOP的方式，此方式在对有重复数据出现的字段排序时，
            // 与Row_Number()的规则不一致，导致出现第一、二页排序出现重复记录。
            // 具体可百度一下【结合TOP N和Row_Number()分页因Order by排序规则不同引起的bug】
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1)
                {
                    return builder;
                }
                else if (builder.KeyIsOrderBy)
                {
                    return builder.Clone().Top(maximumRows);
                }
            }

            if (builder.Keys == null || builder.Keys.Length < 1) throw new XCodeException("分页算法要求指定排序列！" + builder.ToString());

            // 其实，一般数据都是按照聚集索引排序，而聚集索引刚好也就是主键
            // 所以，只要设置的Key顺序跟主键顺序一致，就没有问题
            // 如果，Key指定了跟主键不一致的顺序，那么查询语句一定要指定同样的排序

            //// 如果不指定排序，只能使用TopNotIn，另外三种都会影响结果的顺序
            //if (String.IsNullOrEmpty(builder.OrderBy))
            //{
            //    // 取前面页（经试验Access字符串主键大概在200行以下）并且没有排序的时候，TopNotIn算法应该是最快的
            //    if (startRowIndex < 200) return TopNotIn(builder, startRowIndex, maximumRows);
            //}

            if (isSql2005) return RowNumber(builder, startRowIndex, maximumRows);

            // 必须有排序，且排序字段必须就是数字主键
            if (builder.IsInt && builder.KeyIsOrderBy) return MaxMin(builder, startRowIndex, maximumRows);

            if (maximumRows > 0) return DoubleTop(builder, startRowIndex, maximumRows, queryCountCallback);

            return TopNotIn(builder, startRowIndex, maximumRows);
        }

        /// <summary>最经典的NotIn分页，通用但是效率最差。只需指定一个排序列。</summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        static SelectBuilder TopNotIn(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            if (builder.Keys == null || builder.Keys.Length != 1) throw new ArgumentNullException("Key", "TopNotIn分页算法要求指定单一主键列！" + builder.ToString());

            // 分页标准 Select (20,10,ID)
            // 1，取目标页之前的20行
            // 2，排除前20行之后取10行
            // Select Top 10 * From Table Where ID Not In(Select Top 20 ID From Table)

            // 构建Select Top 20 ID From Table
            var builder1 = builder.Clone().Top(startRowIndex, builder.Key);

            SelectBuilder builder2 = null;
            if (maximumRows < 1)
                builder2 = builder.CloneWithGroupBy("XCode_T0");
            else
                builder2 = builder.Clone().Top(maximumRows);

            builder2.AppendWhereAnd("{0} Not In({1})", builder.Key, builder1);
            // 结果列处理
            builder2.Column = builder.Column;
            // 如果结果列包含有“.”，即有形如tab1.id、tab2.name之类的列时设为获取子查询的全部列
            if ((!string.IsNullOrEmpty(builder2.Column)) && builder2.Column.Contains("."))
            {
                builder2.Column = "*";
            }

            return builder2;
        }

        /// <summary>双Top分页，因为没有使用not in，性能比NotIn要好。语句必须有排序，不需额外指定排序列</summary>
        /// <param name="builder"></param>
        /// <param name="startRowIndex"></param>
        /// <param name="maximumRows"></param>
        /// <param name="queryCountCallback">查询总记录数的委托，近供DoubleTop使用</param>
        /// <returns></returns>
        static SelectBuilder DoubleTop(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows, Func<SelectBuilder, Int64> queryCountCallback)
        {
            if (builder.Keys == null) throw new ArgumentNullException("Key", "DoubleTop分页算法要求指定排序列！" + builder.ToString());

            // 采用DoubleTop分页，最后一页可能有问题，需要特殊处理
            if (queryCountCallback != null)
            {
                // 查询总记录数，计算是否最后一页
                var count = queryCountCallback(builder);
                // 数据不足
                if (count <= startRowIndex) return null;
                // 刚好相等的就不必处理了
                if (startRowIndex + maximumRows > count) maximumRows = count - startRowIndex;
            }

            // 分页标准 Select (20,10,ID Desc)
            // 1，按原始排序取20+10行，此时目标页位于底部
            // 2，倒序过来取10行，得到目标页，但是顺序是反的
            // 3，再倒序一次
            // 显然，原始语句必须有排序，否则无法倒序。另外，也不能处理maximumRows<1的情况
            // Select * From (Select Top 10 * From (Select Top 20+10 * From Table Order By ID Desc) Order By ID Asc) Order By ID Desc

            // 找到排序，优先采用排序字句来做双Top排序
            var orderby = builder.OrderBy ?? builder.KeyOrder;
            Boolean[] isdescs = null;
            var keys = SelectBuilder.Split(orderby, out isdescs);

            // 把排序反过来
            var isdescs2 = new Boolean[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                if (isdescs != null && isdescs.Length > i)
                    isdescs2[i] = !isdescs[i];
                else
                    isdescs2[i] = true;
            }
            var reversekeyorder = SelectBuilder.Join(keys, isdescs2);

            // 构建Select Top 20 * From Table Order By ID Asc
            var builder1 = builder.Clone().AppendColumn(keys).Top(startRowIndex + maximumRows);
            // 必须加一个排序，否则会被优化掉而导致出错
            if (String.IsNullOrEmpty(builder1.OrderBy)) builder1.OrderBy = builder1.KeyOrder;

            var builder2 = builder1.AsChild("XCode_T0").Top(maximumRows);
            // 要反向排序
            builder2.OrderBy = reversekeyorder;

            var builder3 = builder2.AsChild("XCode_T1");
            // 结果列处理
            builder3.Column = builder.Column;
            // 如果结果列包含有“.”，即有形如tab1.id、tab2.name之类的列时设为获取子查询的全部列
            if ((!string.IsNullOrEmpty(builder3.Column)) && builder3.Column.Contains("."))
            {
                builder3.Column = "*";
            }
            // 让结果正向排序
            builder3.OrderBy = orderby;

            return builder3;
        }

        /// <summary>按唯一数字最大最小分页，性能很好。必须指定一个数字型排序列。</summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        static SelectBuilder MaxMin(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            if (builder.Keys == null || builder.Keys.Length != 1) throw new ArgumentNullException("Key", "TopNotIn分页算法要求指定单一主键列！" + builder.ToString());

            // 分页标准 Select (20,10,ID Desc)
            // Select Top 10 * From Table Where ID>(Select max(ID) From (Select Top 20 ID From Table Order By ID) Order By ID Desc) Order By ID Desc

            var builder1 = builder.Clone().Top(startRowIndex, builder.Key);
            var builder2 = builder1.AsChild("XCode_T0");
            builder2.Column = String.Format("{0}({1})", builder.IsDesc ? "Min" : "Max", builder.Key);

            SelectBuilder builder3 = null;
            if (maximumRows < 1)
                builder3 = builder.CloneWithGroupBy("XCode_T1");
            else
                builder3 = builder.Clone().Top(maximumRows);

            // 如果本来有Where字句，加上And，当然，要区分情况按是否有必要加圆括号
            builder3.AppendWhereAnd("{0}{1}({2})", builder.Key, builder.IsDesc ? "<" : ">", builder2);

            return builder3;
        }

        /// <summary>RowNumber分页算法</summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns></returns>
        static SelectBuilder RowNumber(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            //if (maximumRows < 1)
            //    sql = String.Format("Select * From (Select *, row_number() over({2}) as rowNumber From {1}) XCode_Temp_b Where rowNumber>={0}", startRowIndex + 1, sql, orderBy);
            //else
            //    sql = String.Format("Select * From (Select *, row_number() over({3}) as rowNumber From {1}) XCode_Temp_b Where rowNumber Between {0} And {2}", startRowIndex + 1, sql, startRowIndex + maximumRows, orderBy);

            // 如果包含分组，则必须作为子查询
            var builder1 = builder.CloneWithGroupBy("XCode_T0");
            builder1.Column = String.Format("{0}, row_number() over(Order By {1}) as rowNumber", builder.ColumnOrDefault, builder.OrderBy ?? builder.KeyOrder);

            var builder2 = builder1.AsChild("XCode_T1");
            // 结果列处理
            builder2.Column = builder.Column;
            // 如果结果列包含有“.”，即有形如tab1.id、tab2.name之类的列时设为获取子查询的全部列
            if ((!string.IsNullOrEmpty(builder2.Column)) && builder2.Column.Contains("."))
            {
                builder2.Column = "*";
            }

            // row_number()直接影响了排序，这里不再需要
            builder2.OrderBy = null;
            if (maximumRows < 1)
                builder2.Where = String.Format("rowNumber>={0}", startRowIndex + 1);
            else
                builder2.Where = String.Format("rowNumber Between {0} And {1}", startRowIndex + 1, startRowIndex + maximumRows);

            return builder2;
        }

        static SelectBuilder Top(this SelectBuilder builder, Int64 top, String keyColumn = null)
        {
            if (!String.IsNullOrEmpty(keyColumn)) builder.Column = keyColumn;
            if (String.IsNullOrEmpty(builder.Column)) builder.Column = "*";
            builder.Column = String.Format("Top {0} {1}", top, builder.Column);
            return builder;
        }
    }
}