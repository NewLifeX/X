using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using NewLife.CommonEntity;
using NewLife.Log;
using NewLife.Web;
using XCode;
using XCode.Configuration;

namespace <#=Config.NameSpace#>
{<#
    String tdis=Table.DisplayName;
    if(!String.IsNullOrEmpty(tdis)) tdis=tdis.Replace("\r\n"," ").Replace("\\", "\\\\").Replace("'", "").Replace("\"", "");
    String tdes=Table.Description;
    if(!String.IsNullOrEmpty(tdes)) tdes=tdes.Replace("\r\n"," ").Replace("\\", "\\\\").Replace("'", "").Replace("\"", "");
    if(String.IsNullOrEmpty(tdis)) tdis=tdes;
    if(Config.RenderGenEntity){#>
    /// <summary><#=tdis#></summary><# if(tdis!=tdes){#>
    /// <remarks><#=tdes#></remarks><#}#>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class <#=Table.Name#> : <#=Table.Name#><<#=Table.Name#>> { }
    <#
}#>
    /// <summary><#=tdis#></summary><# if(tdis!=tdes){#>
    /// <remarks><#=tdes#></remarks><#}#><#
if(!Config.RenderGenEntity){#>
    public partial class <#=Table.Name#> : <#=Config.BaseClass#><<#=Table.Name#>><#
}else{#>
    public partial class <#=Table.Name#><TEntity> : <#=Config.BaseClass#><TEntity> where TEntity : <#=Table.Name#><TEntity>, new()<#
}#>
    {
        #region 对象操作<#@include Name="对象操作.xt"#>        #endregion

        #region 扩展属性<#@include Name="扩展属性.xt"#>        #endregion

        #region 扩展查询<#@include Name="扩展查询.xt"#>        #endregion

        #region 高级查询
        // 以下为自定义高级查询的例子

        ///// <summary>
        ///// 查询满足条件的记录集，分页、排序
        ///// </summary>
        ///// <param name="key">关键字</param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>实体集</returns>
        //[DataObjectMethod(DataObjectMethodType.Select, true)]
        //public static EntityList<<#=Config.RenderGenEntity?"TEntity":Table.Name#>> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindAll(SearchWhere(key), orderClause, null, startRowIndex, maximumRows);
        //}

        ///// <summary>
        ///// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
        ///// </summary>
        ///// <param name="key">关键字</param>
        ///// <param name="orderClause">排序，不带Order By</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>记录数</returns>
        //public static Int32 SearchCount(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return FindCount(SearchWhere(key), null, null, 0, 0);
        //}

        /// <summary>构造搜索条件</summary>
        /// <param name="key">关键字</param>
        /// <returns></returns>
        private static String SearchWhere(String key)
        {
            // WhereExpression重载&和|运算符，作为And和Or的替代
            // SearchWhereByKeys系列方法用于构建针对字符串字段的模糊搜索
            var exp = SearchWhereByKeys(key, null);

            // 以下仅为演示，Field（继承自FieldItem）重载了==、!=、>、<、>=、<=等运算符（第4行）
            //if (userid > 0) exp &= _.OperatorID == userid;
            //if (isSign != null) exp &= _.IsSign == isSign.Value;
            //if (start > DateTime.MinValue) exp &= _.OccurTime >= start;
            //if (end > DateTime.MinValue) exp &= _.OccurTime < end.AddDays(1).Date;

            return exp;
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        #endregion
    }
}