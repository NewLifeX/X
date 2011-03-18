using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.DataAccessLayer;

namespace <#=Config.NameSpace#>
{
	/// <summary>
	/// <#=ClassDescription#>
	/// </summary>
	[Serializable]
	[DataObject]
	[Description("<#=ClassDescription#>")]
	[BindTable("<#=Table.Name#>", Description = "<#=ClassDescription#>", ConnName = "<#=Config.EntityConnName#>", DbType = DatabaseType.<#=Table.DbType#>)]
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
		[DataObjectField(<#=Field.PrimaryKey.ToString().ToLower()#>, <#=Field.Identity.ToString().ToLower()#>, <#=Field.Nullable.ToString().ToLower()#>, <#=Field.Length#>)]
		[BindColumn(<#=Field.ID#>, "<#=Field.Name#>", "<#=GetPropertyDescription(Field)#>", "<#=Field.Default#>", "<#=Field.RawType#>", <#=Field.Precision#>, <#=Field.Scale#>, <#=Field.IsUnicode.ToString().ToLower()#>)]
		public <#=Field.FieldType#> <#=GetPropertyName(Field)#>
		{
			get { return _<#=GetPropertyName(Field)#>; }
			set { if (OnPropertyChange("<#=GetPropertyName(Field)#>", value)) _<#=GetPropertyName(Field)#> = value; }
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
		public override Object this[String name]
		{
			get
			{
				switch (name)
				{<#
	foreach(XField Field in Table.Fields)
	{
#>
					case "<#=GetPropertyName(Field)#>" : return _<#=GetPropertyName(Field)#>;<#
	}
#>
					default: return base[name];
				}
			}
			set
			{
				switch (name)
				{<#
Type conv=typeof(Convert);
	foreach(XField Field in Table.Fields)
	{ 
if(conv.GetMethod("To"+Field.FieldType, new Type[]{typeof(Object)})!=null){
#>
					case "<#=GetPropertyName(Field)#>" : _<#=GetPropertyName(Field)#> = Convert.To<#=Field.FieldType#>(value); break;<#
}else{
#>
					case "<#=GetPropertyName(Field)#>" : _<#=GetPropertyName(Field)#> = (<#=Field.FieldType#>)value; break;<#
	}
}
#>
					default: base[name] = value; break;
				}
			}
		}
		#endregion

		#region 字段名
		/// <summary>
		/// 取得<#=ClassDescription#>字段名的快捷方式
		/// </summary>
        [CLSCompliant(false)]
		public class _
		{<#
	   foreach(XField Field in Table.Fields)
	  {
#>
			///<summary>
			/// <#=GetPropertyDescription(Field)#>
			///</summary>
			public const String <#=GetPropertyName(Field)#> = "<#=Field.Name#>";
<#
	  }
#>		}
		#endregion
	}
}