using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace <#=Config.NameSpace#>
{
	/// <summary><#=Table.Description#></summary>
	[Serializable]
	[DataObject]
	[Description("<#=Table.Description#>")]
    [BindTable("<#=Table.Name#>", Description = "<#=Table.Description#>", ConnName = "<#=Config.EntityConnName#>", DbType = DatabaseType.<#=Table.DbType#>)]
	public partial class <#=Table.Alias#>
	{
		#region 属性<#
		foreach(IDataColumn Field in Table.Columns)
	    {
#>
		private <#=Field.DataType.Name#> _<#=Field.Alias#>;
		/// <summary><#=Field.Description#></summary>
		[Description("<#=Field.Description#>")]
		[DataObjectField(<#=Field.PrimaryKey.ToString().ToLower()#>, <#=Field.Identity.ToString().ToLower()#>, <#=Field.Nullable.ToString().ToLower()#>, <#=Field.Length#>)]
		[BindColumn(<#=Field.ID#>, "<#=Field.Name#>", "<#=Field.Description#>", "<#=Field.Default#>", "<#=Field.RawType#>", <#=Field.Precision#>, <#=Field.Scale#>, <#=Field.IsUnicode.ToString().ToLower()#>)]
		public <#=Field.DataType.Name#> <#=Field.Alias#>
		{
			get { return _<#=Field.Alias#>; }
			set { if (OnPropertyChanging("<#=Field.Alias#>", value)) { _<#=Field.Alias#> = value; OnPropertyChanged("<#=Field.Alias#>"); } }
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
	foreach(IDataColumn Field in Table.Columns)
	{
#>
					case "<#=Field.Alias#>" : return _<#=Field.Alias#>;<#
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
	foreach(IDataColumn Field in Table.Columns)
	{ 
        if(conv.GetMethod("To"+Field.DataType.Name, new Type[]{typeof(Object)})!=null){
#>
					case "<#=Field.Alias#>" : _<#=Field.Alias#> = Convert.To<#=Field.DataType.Name#>(value); break;<#
        }else{
#>
					case "<#=Field.Alias#>" : _<#=Field.Alias#> = (<#=Field.DataType.Name#>)value; break;<#
	    }
    }
#>
					default: base[name] = value; break;
				}
			}
		}
		#endregion

		#region 字段名
		/// <summary>取得<#=Table.Description#>字段信息的快捷方式</summary>
		public class _
		{<#
	   foreach(IDataColumn Field in Table.Columns)
	  {
#>
			///<summary><#=Field.Description#></summary>
            public static readonly FieldItem <#=Field.Alias#> = Meta.Table.FindByName("<#=Field.Alias#>");
<#
	  }
#>		}
		#endregion
    }
}