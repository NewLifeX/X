using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace <#=Config.NameSpace#>
{
	/// <summary>
	/// <#=ClassDescription#>
	/// </summary>
	[Serializable]
	[DataObject]
	[Description("<#=ClassDescription#>")]
	public partial class <#=ClassName#>
	{
		#region 属性<#
        foreach(XField Field in Table.Fields)
        {
#>
		private <#=Field.FieldType#> _<#=GetPropertyName(Field)#>;
		/// <summary>
		/// <#=GetPropertyDescription(Field)#>
		/// </summary>
		[Description("<#=GetPropertyDescription(Field)#>")]
		[DataObjectField(<#=Field.Identity.ToString().ToLower()#>, <#=Field.PrimaryKey.ToString().ToLower()#>, <#=Field.Nullable.ToString().ToLower()#>, <#=Field.Length#>)]
		public <#=Field.FieldType#> <#=GetPropertyName(Field)#>
		{
			get { return _<#=GetPropertyName(Field)#>; }
			set { _<#=GetPropertyName(Field)#> = value; }
		}
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
        foreach(XField Field in Table.Fields)
        {
#>
					case "<#=GetPropertyName(Field)#>": return <#=GetPropertyName(Field)#>;<#
        }
#>
					default: return null;
				}
			}
			set
			{
				switch (name)
				{<#
        foreach(XField Field in Table.Fields)
        {
#>
					case "<#=GetPropertyName(Field)#>": _<#=GetPropertyName(Field)#> = Convert.To<#=Field.FieldType#>(value); break;<#
        }
#>
				}
			}
		}
		#endregion
	}
}