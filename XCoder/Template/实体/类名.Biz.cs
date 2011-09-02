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

		#region 扩展属性
        <#
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

		#region 扩展查询
<#
// 这里是FindByKeyForEdit，用C#代码拼好了再输出，那样好看点
if(Table.PrimaryKeys!=null&&Table.PrimaryKeys.Length>0)
{
#>
		/// <summary>
		/// 根据主键查询一个<#=Table.Description#>实体对象用于表单编辑
		/// </summary>
<#
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

#>		/// <param name="<#=argName#>"><#=Field.Description#></param>
<#  
	    }
#>		/// <returns></returns>
		[DataObjectMethod(DataObjectMethodType.Select, false)]
		public static <#=Table.Alias#> FindByKeyForEdit(<#=sb1#>)
		{
			<#=Table.Alias#> entity = Find(new String[]{<#=sb2#>}, new Object[]{<#=sb3#>});
			if (entity == null)
			{
				entity = new <#=Table.Alias#>();
			}
			return entity;
		}

		/// <summary>
		/// 根据主键查询一个<#=Table.Description#>实体对象
		/// </summary>
<#
        sb1=new StringBuilder();
        sb2=new StringBuilder();
        sb3=new StringBuilder();
        StringBuilder sb4=new StringBuilder();
        String[] Args=new String[Table.PrimaryKeys.Length];
        Int32 i=0;

		foreach(IDataColumn Field in Table.PrimaryKeys)
	    {
            if(sb1.Length>0) sb1.Append(", ");
            sb1.Append(Field.DataType.Name+" ");
            String argName=Field.Alias.ToLower();
            if(argName==Field.Alias) argName="__"+Field.Alias;
            sb1.Append(argName);
            Args[i++]=argName;

            if(sb2.Length>0) sb2.Append(", ");
            sb2.Append("_."+Field.Alias);
            
            if(sb3.Length>0) sb3.Append(", ");
            sb3.Append(argName);
            
            if(sb4.Length>0) sb4.Append("And");
            sb4.Append(Field.Alias);

#>		/// <param name="<#=argName#>"><#=Field.Description#></param>
<#  
	    }
#>		/// <returns></returns>
		[DataObjectMethod(DataObjectMethodType.Select, false)]
		public static <#=Table.Alias#> FindBy<#=sb4#>(<#=sb1#>)
		{
			return Find(new String[]{<#=sb2#>}, new Object[]{<#=sb3#>});<#
            if(Table.PrimaryKeys.Length==1){
#>
			// 实体缓存
			//return Meta.Cache.Entities.Find(_.<#=Table.PrimaryKeys[0].Alias#>, <#=Args[0]#>);
			// 单对象缓存
			//return Meta.SingleCache[<#=Args[0]#>];<#}#>
		}     
<#
}

// 根据索引，增加多个方法
if(Table.Indexes!=null&&Table.Indexes.Count>0){
    foreach (IDataIndex di in Table.Indexes){
        if(di.Columns==null||di.Columns.Length<1)continue;

        IDataColumn[] columns=Table.GetColumns(di.Columns);

        String returnType=Table.Alias;
        String action="Find";
        String IsAll=String.Empty;
        if (!di.Unique){
            returnType=String.Format("EntityList<{0}>",Table.Alias);
            IsAll="All";
            action="FindAll";
        }
        StringBuilder sb1=new StringBuilder();
        StringBuilder sb2=new StringBuilder();
        StringBuilder sb3=new StringBuilder();
        StringBuilder sb4=new StringBuilder();
        StringBuilder sb5=new StringBuilder();
        String[] Args=new String[columns.Length];

		for(int i=0;i<columns.Length;i++)
	    {
            IDataColumn Field=columns[i];

            if(sb1.Length>0) sb1.Append(", ");
            sb1.Append(Field.DataType.Name+" ");
            String argName=Field.Alias.ToLower();
            if(argName==Field.Alias) argName="__"+Field.Alias;
            sb1.Append(argName);
            Args[i]=argName;
   
            if(sb2.Length>0) sb2.Append(", ");
            sb2.Append("_."+Field.Alias);
            
            if(sb3.Length>0) sb3.Append(", ");
            sb3.Append(argName);
            
            if(sb4.Length>0) sb4.Append("And");
            sb4.Append(Field.Alias);
            
            if(sb5.Length>0) sb5.Append("、");
            if(!String.IsNullOrEmpty(Field.Description))
                sb5.Append(Field.Description);
            else
                sb5.Append(Field.Alias);
        }
#>
		/// <summary>
		/// 根据<#=sb5#>查找
		/// </summary>
<#
		for(int i=0;i<columns.Length;i++)
	    {
#>		/// <param name="<#=Args[i]#>"><#=columns[i].Description#></param>
<#  
	    }
#>		/// <returns></returns>
		[DataObjectMethod(DataObjectMethodType.Select, false)]
		public static <#=returnType#> Find<#=IsAll#>By<#=sb4#>(<#=sb1#>)
		{
			return <#=action#>(new String[]{<#=sb2#>}, new Object[]{<#=sb3#>});<#
        if(columns.Length==1){
            String pname=columns[0].Alias;
#>
			// 实体缓存
			//return Meta.Cache.Entities.<#=action#>(_.<#=pname#>, <#=Args[0]#>);<#if(di.Unique){#>
			// 单对象缓存
			//return Meta.SingleCache[<#=Args[0]#>];<#}}#>
		}
<#
    }
}
#>
        #endregion

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
		public static EntityList<<#=Table.Alias#>> Search(String key, String orderClause, Int32 startRowIndex, Int32 maximumRows)
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
                        if (item.Type != typeof(String)) continue;
                        // 只要前五项
                        if (++n > 5) break;

                        if (n > 1) sb.Append(" Or ");
                        sb.AppendFormat("{0} like '%{1}%'", Meta.FormatName(item.Name), keys[i]);
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