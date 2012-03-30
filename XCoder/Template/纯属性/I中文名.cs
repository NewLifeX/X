using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace <#=Config.NameSpace#>
{
	/// <summary><#=Table.Description#>接口</summary>
	public interface I<#=Table.Alias#>
	{
		#region 属性<#
		foreach(IDataColumn Field in Table.Columns)
		{
#>
		/// <summary><#=Field.Description#></summary>
		<#=Field.DataType.Name#> <#=Field.Alias#> { get; set; }
<#
		}
#>		#endregion

		#region 获取/设置 字段值
		/// <summary>获取/设置 字段值。</summary>
		/// <param name="name">字段名</param>
		/// <returns></returns>
		Object this[String name] { get; set; }
		#endregion
	}
}