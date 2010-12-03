using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace <#=Config.NameSpace#>
{
	/// <summary>
	/// <#=ClassDescription#>接口
	/// </summary>
	public interface I<#=ClassName#>
	{
		#region 属性<#
		foreach(XField Field in Table.Fields)
		{
#>
		/// <summary>
		/// <#=GetPropertyDescription(Field)#>
		/// </summary>
		<#=Field.FieldType#> <#=GetPropertyName(Field)#> { get; set; }
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
		Object this[String name] { get; set; }
		#endregion
	}
}