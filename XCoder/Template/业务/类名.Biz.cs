using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using XCode;

namespace <#=Config.NameSpace#>
{
	/// <summary>
	/// <#=ClassDescription#>
	/// </summary>
	public partial class <#=ClassName#> : Entity<<#=ClassName#>>
	{
		#region 对象操作
		//基类Entity中包含三个对象操作：Insert、Update、Delete
		//你可以重载它们，以改变它们的行为
		//如：
		/*
		/// <summary>
		/// 已重载。把该对象插入到数据库。这里可以做数据插入前的检查
		/// </summary>
		/// <returns>影响的行数</returns>
		public override Int32 Insert()
		{
			return base.Insert();
		}
		 * */
		#endregion
		
		#region 扩展属性
		//TODO: 本类与哪些类有关联，可以在这里放置一个属性，使用延迟加载的方式获取关联对象
<#
		// 探测以ID结尾的字段是否为类名，如果是，则输出扩展属性
		Boolean hasExtendProperty = false;
		foreach(XField field in Table.Fields){
			if (field.DataType != typeof(Int32)) continue;
			if (!field.Name.EndsWith("ID", StringComparison.Ordinal)) continue;
			String tableName = field.Name.Substring(0, field.Name.Length - 2);
			XTable table = FindTable(tableName);
			if (table == null) continue;
			hasExtendProperty = true;
			String className = GetClassName(table);
#>
		private <#=className#> _<#=className#>;
		/// <summary>该<#=ClassDescription#>所对应的<#=GetClassDescription(table)#></summary>
		public <#=className#> <#=className#>
		{
			get
			{
				if (_<#=className#> == null && <#=GetPropertyName(field)#> > 0 && !Dirtys.ContainsKey("<#=className#>"))
				{
					_<#=className#> = <#=className#>.FindByKey(<#=GetPropertyName(field)#>);
					Dirtys.Add("<#=className#>", true);
				}
				return _<#=className#>;
			}
			set { _<#=className#> = value; }
		}<#
		}
		if (!hasExtendProperty){
#>
		/*
		private Category _Category;
		/// <summary>该商品所对应的类别</summary>
		public Category Category
		{
			get
			{
				if (_Category == null && CategoryID > 0 && !Dirtys.ContainKey("Category"))
				{
					_Category = Category.FindByKey(CategoryID);
					Dirtys.Add("Category", true);
				}
				return _Category;
			}
			set { _Category = value; }
		}
		 * */<#}#>
		#endregion

		#region 扩展查询
		/// <summary>
		/// 根据主键查询一个<#=ClassDescription#>实体对象用于表单编辑
		/// </summary>
<#
Int32 n=0;
		foreach(XField Field in Table.Fields)
	   {
		if (!Field.PrimaryKey) continue;    
#>		///<param name="__<#=GetPropertyName(Field)#>"><#=GetPropertyDescription(Field)#></param>
<#  
	   }
#>		/// <returns></returns>
		[DataObjectMethod(DataObjectMethodType.Select, false)]
		public static <#=ClassName#> FindByKeyForEdit(<# n=0;foreach(XField Field in Table.Fields){if(!Field.PrimaryKey) continue;#><#if(n++>0){#>, <#}#><#=Field.FieldType#> __<#=GetPropertyName(Field)#><#}#>)
		{
			<#=ClassName#> entity=Find(new String[]{<#n=0;foreach(XField Field in Table.Fields){if(!Field.PrimaryKey) continue;#><#if(n++>0){#>, <#}#>_.<#=GetPropertyName(Field)#><#}#>}, new Object[]{<#n=0;foreach(XField Field in Table.Fields){if(!Field.PrimaryKey) continue;#><#if(n++>0){#>, <#}#>__<#=GetPropertyName(Field)#><#}#>});
			if (entity == null)
			{
				entity = new <#=ClassName#>();
			}
			return entity;
		}     
<#
foreach (XField Field in Table.Fields){
    if (Field.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)){
#>
		/// <summary>
		/// 根据<#=GetPropertyDescription(Field)#>查找
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static <#=ClassName#> FindByID(Int32 id)
		{
			return Find(_.ID, id);
			// 实体缓存
			//return Meta.Cache.Entities.Find(_.ID, id);
			// 单对象缓存
			//return Meta.SingleCache[id];
		}
<#
    }
    else if(Field.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)){
#>
		/// <summary>
		/// 根据<#=GetPropertyDescription(Field)#>查找
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static <#=ClassName#> FindByName(String name)
		{
			return Find(_.<#=Field.Name#>, name);
			// 实体缓存
			//return Meta.Cache.Entities.Find(_.<#=Field.Name#>, name);
			// 单对象缓存
			//return Meta.SingleCache[name];
		}
<#
    }
}
#>		#endregion

		#region 高级查询
		///// <summary>
		///// 查询满足条件的记录集，分页、排序
		///// </summary>
		///// <param name="key">关键字</param>
		///// <param name="orderClause">排序，不带Order By</param>
		///// <param name="startRowIndex">开始行，0开始</param>
		///// <param name="maximumRows">最大返回行数</param>
		///// <returns>实体集</returns>
		//[DataObjectMethod(DataObjectMethodType.Select, true)]
		//public static EntityList<<#=ClassName#>> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
		//{
		//    return FindAll(SearchWhere(key), orderClause, null, startRowIndex, maximumRows);
		//}

		///// <summary>
		///// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
		///// </summary>
		///// <param name="key">关键字</param>
		///// <param name="orderClause">排序，不带Order By</param>
		///// <param name="startRowIndex">开始行，0开始</param>
		///// <param name="maximumRows">最大返回行数</param>
		///// <returns>记录数</returns>
		//public static Int32 SearchCount(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
		//{
		//    return FindCount(SearchWhere(key), null, null, 0, 0);
		//}

		///// <summary>
		///// 构造搜索条件
		///// </summary>
		///// <param name="key">关键字</param>
		///// <returns></returns>
		//private static String SearchWhere(String key)
		//{
		//    StringBuilder sb = new StringBuilder();
		//    sb.Append("1=1");

		//    if (!String.IsNullOrEmpty(name)) sb.AppendFormat(" And {0} like '%{1}%'", _.Name, name.Replace("'", "''"));

		//    if (sb.ToString() == "1=1")
		//        return null;
		//    else
		//        return sb.ToString();
		//}
		#endregion

		#region 扩展操作
		#endregion

		#region 业务
		#endregion
	}
}