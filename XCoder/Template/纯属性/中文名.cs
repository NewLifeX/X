using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace <#=Config.NameSpace#>
{
    /// <summary><#=Table.Description#></summary>
    [Serializable]
    [DataObject]
    [Description("<#=Table.Description#>")]
    public partial class <#=Table.Name#>
    {
        #region 属性<#
        foreach(IDataColumn Field in Table.Columns)
        {
#>
        private <#=Field.DataType.Name#> _<#=Field.Name#>;
        /// <summary><#=Field.Description#></summary>
        [DisplayName("<#=Field.DisplayName#>")]
        [Description("<#=Field.Description#>")]
        [DataObjectField(<#=Field.PrimaryKey.ToString().ToLower()#>, <#=Field.Identity.ToString().ToLower()#>, <#=Field.Nullable.ToString().ToLower()#>, <#=Field.Length#>)]
        public <#=Field.DataType.Name#> <#=Field.Name#> { get { return _<#=Field.Name#>; } set { _<#=Field.Name#> = value; } }
<#
        }
#>		#endregion

        #region 获取/设置 字段值
        /// <summary>
        /// 获取/设置 字段值。
        /// 一个索引，基类使用反射实现。
        /// 派生实体类可重写该索引，以避免反射带来的性能损耗
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public Object this[String name]
        {
            get
            {
                switch (name)
                {<#
        foreach(IDataColumn Field in Table.Columns)
        {
#>
                    case "<#=Field.Name#>" : return _<#=Field.Name#>;<#
        }
#>
                    default: return null;
                }
            }
            set
            {
                switch (name)
                {<#
    Type conv=typeof(Convert);
    foreach(IDataColumn Field in Table.Columns)
    {
        if(conv.GetMethod("To"+Field.DataType.Name, new Type[]{typeof(Object)})!=null){
#>
                    case "<#=Field.Name#>" : _<#=Field.Name#> = Convert.To<#=Field.DataType.Name#>(value); break;<#
        }else{
#>
                    case "<#=Field.Name#>" : _<#=Field.Name#> = (<#=Field.DataType.Name#>)value; break;<#
        }
    }
#>
                }
            }
        }
        #endregion
    }
}