using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;

namespace <#=Config.NameSpace#>
{
	/// <summary><#=Table.Description#></summary><#
foreach(IDataIndex di in Table.Indexes){#>
    [BindIndex("<#=di.Name#>", <#=di.Unique.ToString().ToLower()#>, "<#=String.Join(",", di.Columns)#>")]<#
}
foreach(IDataRelation dr in Table.Relations){#>
    [BindRelation("<#=dr.Column#>", <#=dr.Unique.ToString().ToLower()#>, "<#=dr.RelationTable#>", "<#=dr.RelationColumn#>")]<#}#>
	public partial class <#=Table.Alias#> : Entity<<#=Table.Alias#>>
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

		#region 扩展属性<#
if(Table.Relations!=null && Table.Relations.Count>0)
{
	foreach(IDataRelation dr in Table.Relations)
    {
		IDataTable rtable = FindTable(dr.RelationTable);
		if (rtable == null) continue;

        IDataColumn rcolumn = rtable.GetColumn(dr.RelationColumn);
        if(rcolumn == null) continue;

        IDataColumn field = Table.GetColumn(dr.Column);

		String className = rtable.Alias;
        String keyName=className;

        if(!dr.Unique)
        {
#>

        [NonSerialized]
		private <#=className#> _<#=keyName#>;
		/// <summary>该<#=Table.Description#>所对应的<#=rtable.Description#></summary>
		[XmlIgnore]
		public <#=className#> <#=keyName#>
		{
			get
			{
				<#
                if(field.DataType == typeof(String)){
                #>if (_<#=keyName#> == null && !String.IsNullOrEmpty(<#=field.Alias#>) && !Dirtys.ContainsKey("<#=keyName#>"))<#
                }else{#>if (_<#=keyName#> == null && <#=field.Alias#> > 0 && !Dirtys.ContainsKey("<#=keyName#>"))<#
                }#>
                {
					_<#=keyName#> = <#=className#>.FindBy<#=rcolumn.Alias#>(<#=field.Alias#>);
					Dirtys["<#=keyName#>"] = true;
				}
				return _<#=keyName#>;
			}
			set { _<#=keyName#> = value; }
		}<#
        }else
        {
            keyName+="s";
#>

        [NonSerialized]
		private EntityList<<#=className#>> _<#=keyName#>;
		/// <summary>该<#=Table.Description#>所拥有的<#=rtable.Description#>集合</summary>
		[XmlIgnore]
		public EntityList<<#=className#>> <#=keyName#>
		{
			get
			{
				<#
                if(field.DataType == typeof(String)){
                #>if (_<#=keyName#> == null && !String.IsNullOrEmpty(<#=field.Alias#>) && !Dirtys.ContainsKey("<#=keyName#>"))<#
                }else{#>if (_<#=keyName#> == null && <#=field.Alias#> > 0 && !Dirtys.ContainsKey("<#=keyName#>"))<#
                }#>
                {
					_<#=keyName#> = <#=className#>.FindAllBy<#=rcolumn.Alias#>(<#=field.Alias#>);
					Dirtys["<#=keyName#>"] = true;
				}
				return _<#=keyName#>;
			}
			set { _<#=keyName#> = value; }
		}<#
        }
	}
}
#>
		#endregion

		#region 扩展查询<#
if(Table.PrimaryKeys!=null&&Table.PrimaryKeys.Length>0)
{
#>
		/// <summary>
		/// 根据主键查询一个<#=Table.Description#>实体对象用于表单编辑
		/// </summary>
<#
Int32 n=0;
StringBuilder sb1=new StringBuilder();
StringBuilder sb2=new StringBuilder();
StringBuilder sb3=new StringBuilder();

		foreach(IDataColumn Field in Table.PrimaryKeys)
	    {
            if(sb1.Length>0) sb1.Append(", ");
            sb1.Append(Field.DataType.Name+" ");
            String argName=Field.Alias.ToLower();
            if(argName==Field.Alias) argName="__"+Field.Alias;
            sb1.Append(argName);
   
            if(sb2.Length>0) sb2.Append(", ");
            sb2.Append("_."+Field.Alias);
            
            if(sb3.Length>0) sb3.Append(", ");
            sb3.Append(argName);

#>		///<param name="__<#=Field.Alias#>"><#=Field.Description#></param>
<#  
	    }
#>		/// <returns></returns>
		[DataObjectMethod(DataObjectMethodType.Select, false)]
		public static <#=ClassName#> FindByKeyForEdit(<#=sb1#>)
		{
			<#=ClassName#> entity = Find(new String[]{<#=sb2#>}, new Object[]{<#=sb3#>});
			if (entity == null)
			{
				entity = new <#=ClassName#>();
			}
			return entity;
		}     
<#
}
foreach (IDataColumn Field in Table.Columns){
    String pname=Field.Alias;
    if (pname.Equals("ID", StringComparison.OrdinalIgnoreCase)){
#>
		/// <summary>
		/// 根据<#=Field.Description#>查找
		/// </summary>
		/// <param name="__<#=pname#>"></param>
		/// <returns></returns>
		public static <#=ClassName#> FindByID(Int32 __<#=pname#>)
		{
			return Find(_.<#=pname#>, __<#=pname#>);
			// 实体缓存
			//return Meta.Cache.Entities.Find(_.<#=pname#>, __<#=pname#>);
			// 单对象缓存
			//return Meta.SingleCache[__<#=pname#>];
		}
<#
    }
    else if(pname.Equals("Name", StringComparison.OrdinalIgnoreCase)){
#>
		/// <summary>
		/// 根据<#=Field.Description#>查找
		/// </summary>
		/// <param name="__<#=pname#>"></param>
		/// <returns></returns>
		public static <#=ClassName#> FindByName(String __<#=pname#>)
		{
			return Find(_.<#=pname#>, __<#=pname#>);
			// 实体缓存
			//return Meta.Cache.Entities.Find(_.<#=pname#>, __<#=pname#>);
			// 单对象缓存
			//return Meta.SingleCache[__<#=pname#>];
		}
<#
    }
}
#>		#endregion

		#region 高级查询
		/// <summary>
		/// 查询满足条件的记录集，分页、排序
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="orderClause">排序，不带Order By</param>
		/// <param name="startRowIndex">开始行，0表示第一行</param>
		/// <param name="maximumRows">最大返回行数，0表示所有行</param>
		/// <returns>实体集</returns>
		[DataObjectMethod(DataObjectMethodType.Select, true)]
		public static EntityList<<#=ClassName#>> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
		{
		    return FindAll(SearchWhere(key), orderClause, null, startRowIndex, maximumRows);
		}

		/// <summary>
		/// 查询满足条件的记录总数，分页和排序无效，带参数是因为ObjectDataSource要求它跟Search统一
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="orderClause">排序，不带Order By</param>
		/// <param name="startRowIndex">开始行，0表示第一行</param>
		/// <param name="maximumRows">最大返回行数，0表示所有行</param>
		/// <returns>记录数</returns>
		public static Int32 SearchCount(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
		{
		    return FindCount(SearchWhere(key), null, null, 0, 0);
		}

		/// <summary>
		/// 构造搜索条件
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns></returns>
		private static String SearchWhere(String key)
		{
            StringBuilder sb = new StringBuilder();
            sb.Append("1=1");

            if (!String.IsNullOrEmpty(key))
            {
                key = key.Replace("'", "''");
                String[] keys = key.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < keys.Length; i++)
                {
                    if (sb.Length > 0) sb.Append(" And ");

                    if (keys.Length > 1) sb.Append("(");
                    Int32 n = 0;
                    foreach (FieldItem item in Meta.Fields)
                    {
                        if (item.Property.PropertyType != typeof(String)) continue;
                        // 只要前五项
                        if (++n > 5) break;

                        if (n > 1) sb.Append(" Or ");
                        sb.AppendFormat("{0} like '%{1}%'", Meta.FormatKeyWord(item.Name), keys[i]);
                    }
                    if (keys.Length > 1) sb.Append(")");
                }
            }

            if (sb.Length == "1=1".Length) return null;
            return sb.ToString();
		}
		#endregion

		#region 扩展操作
		#endregion

		#region 业务
		#endregion
	}
}