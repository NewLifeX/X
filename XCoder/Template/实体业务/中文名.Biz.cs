using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using NewLife.Log;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace <#=Config.NameSpace#>
{<#
    String tdis=Table.DisplayName;
    if(!String.IsNullOrEmpty(tdis)) tdis=tdis.Replace("\r\n"," ").Replace("\\", "\\\\").Replace("'", "").Replace("\"", "");
    String tdes=Table.Description;
    if(!String.IsNullOrEmpty(tdes)) tdes=tdes.Replace("\r\n"," ").Replace("\\", "\\\\").Replace("'", "").Replace("\"", "");
    if(String.IsNullOrEmpty(tdis)) tdis=tdes;

    String myClassName = Config.RenderGenEntity ? "TEntity" : Table.Name;

    String baseType = !String.IsNullOrEmpty(Table.BaseType) ? Table.BaseType : Config.BaseClass;
    if(Config.RenderGenEntity)
{#>
    /// <summary><#=tdis#></summary><# if(tdis!=tdes){#>
    /// <remarks><#=tdes#></remarks><#}#>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class <#=Table.Name#> : <#=Table.Name#><<#=Table.Name#>> { }
    <#
}#>
    /// <summary><#=tdis#></summary><# if(tdis!=tdes){#>
    /// <remarks><#=tdes#></remarks><#}#><#
if(Config.RenderGenEntity){#>
    public partial class <#=Table.Name#><TEntity> : <#=baseType#><TEntity> where TEntity : <#=Table.Name#><TEntity>, new()<#
}else{#>
    public partial class <#=Table.Name#> : <#=baseType#><<#=Table.Name#>><#
}#>
    {
        #region 对象操作<#@include Name="对象操作.xt"#>        #endregion

        #region 扩展属性<#@include Name="扩展属性.xt"#>        #endregion

        #region 扩展查询<#@include Name="扩展查询.xt"#>        #endregion

        #region 高级查询
        // 以下为自定义高级查询的例子

        /// <summary>查询满足条件的记录集，分页、排序</summary>
        /// <param name="userid">用户编号</param>
        /// <param name="start">开始时间</param>
        /// <param name="end">结束时间</param>
        /// <param name="key">关键字</param>
        /// <param name="param">分页排序参数，同时返回满足条件的总记录数</param>
        /// <returns>实体集</returns>
        public static EntityList<<#=myClassName#>> Search(Int32 userid, DateTime start, DateTime end, String key, PageParameter param)
        {
            // WhereExpression重载&和|运算符，作为And和Or的替代
            // SearchWhereByKeys系列方法用于构建针对字符串字段的模糊搜索，第二个参数可指定要搜索的字段
            var exp = SearchWhereByKeys(key, null, null);

            // 以下仅为演示，Field（继承自FieldItem）重载了==、!=、>、<、>=、<=等运算符
            //if (userid > 0) exp &= _.OperatorID == userid;
            //if (isSign != null) exp &= _.IsSign == isSign.Value;
            //exp &= _.OccurTime.Between(start, end); // 大于等于start，小于end，当start/end大于MinValue时有效

            return FindAll(exp, param);
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        #endregion
    }
}