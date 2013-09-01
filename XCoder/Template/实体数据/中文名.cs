using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace <#=Config.NameSpace#>
{<#
    String tdis=Table.DisplayName;
    if(!String.IsNullOrEmpty(tdis)) tdis=tdis.Replace("\r\n"," ").Replace("\\", "\\\\").Replace("'", "").Replace("\"", "");
    String tdes=Table.Description;
    if(!String.IsNullOrEmpty(tdes)) tdes=tdes.Replace("\r\n"," ").Replace("\\", "\\\\").Replace("'", "").Replace("\"", "");
    if(String.IsNullOrEmpty(tdis)) tdis=tdes;
    #>
    /// <summary><#=tdis#></summary><# if(tdis!=tdes){#>
    /// <remarks><#=tdes#></remarks><#}#>
    [Serializable]
    [DataObject]
    [Description("<#=tdes#>")]<#
foreach(IDataIndex di in Table.Indexes){if(di.Columns==null||di.Columns.Length<1)continue;#>
    [BindIndex("<#=di.Name#>", <#=di.Unique.ToString().ToLower()#>, "<#=String.Join(",", di.Columns)#>")]<#
}
foreach(IDataRelation dr in Table.Relations){#>
    [BindRelation("<#=dr.Column#>", <#=dr.Unique.ToString().ToLower()#>, "<#=dr.RelationTable#>", "<#=dr.RelationColumn#>")]<#}#>
    [BindTable("<#=Table.TableName#>", Description = "<#=tdes#>", ConnName = "<#=Config.EntityConnName#>", DbType = DatabaseType.<#=Table.DbType#><#if(Table.IsView){#>, IsView = true<#}#>)]<#
if(!Config.RenderGenEntity){#>
    public partial class <#=Table.Name#> : I<#=Table.Name#><#
}else{#>
    public partial class <#=Table.Name#><TEntity> : I<#=Table.Name#><#
}#>
    {
        #region 属性<#
        foreach(IDataColumn Field in Table.Columns)
        {
            String des=Field.Description;
            if(!String.IsNullOrEmpty(des)) des=des.Replace("\r\n"," ").Replace("\\", "\\\\").Replace("'", "").Replace("\"", "");
            String dis = Field.DisplayName;
             if(!String.IsNullOrEmpty(dis)) dis=dis.Replace("\r\n"," ").Replace("'", " ").Replace("\"", "");
#>
        private <#=Field.DataType.Name#> _<#=Field.Name#>;
        /// <summary><#=des#></summary>
        [DisplayName("<#=dis#>")]
        [Description("<#=des#>")]
        [DataObjectField(<#=Field.PrimaryKey.ToString().ToLower()#>, <#=Field.Identity.ToString().ToLower()#>, <#=Field.Nullable.ToString().ToLower()#>, <#=Field.Length#>)]
        [BindColumn(<#=Field.ID#>, "<#=Field.ColumnName#>", "<#=des#>", <#=Field.Default==null?"null":"\""+Field.Default.Replace("\\", "\\\\")+"\""#>, "<#=Field.RawType#>", <#=Field.Precision#>, <#=Field.Scale#>, <#=Field.IsUnicode.ToString().ToLower()#>)]
        public virtual <#=Field.DataType.Name#> <#=Field.Name#>
        {
            get { return _<#=Field.Name#>; }
            set { if (OnPropertyChanging(__.<#=Field.Name#>, value)) { _<#=Field.Name#> = value; OnPropertyChanged(__.<#=Field.Name#>); } }
        }
<#
        }
#>        #endregion

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
                    case __.<#=Field.Name#> : return _<#=Field.Name#>;<#
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
                    case __.<#=Field.Name#> : _<#=Field.Name#> = Convert.To<#=Field.DataType.Name#>(value); break;<#
        }else{
#>
                    case __.<#=Field.Name#> : _<#=Field.Name#> = (<#=Field.DataType.Name#>)value; break;<#
        }
    }
#>
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得<#=tdis#>字段信息的快捷方式</summary>
        public partial class _
        {<#
       foreach(IDataColumn Field in Table.Columns)
      {
            String des=Field.Description;
            if(!String.IsNullOrEmpty(des)) des=des.Replace("\r\n"," ");
#>
            ///<summary><#=des#></summary>
            public static readonly Field <#=Field.Name#> = FindByName(__.<#=Field.Name#>);
<#
      }
#>
            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得<#=tdis#>字段名称的快捷方式</summary>
        partial class __
        {<#
       foreach(IDataColumn Field in Table.Columns)
      {
            String des=Field.Description;
            if(!String.IsNullOrEmpty(des)) des=des.Replace("\r\n"," ");
#>
            ///<summary><#=des#></summary>
            public const String <#=Field.Name#> = "<#=Field.Name#>";
<#
      }
#>
        }
        #endregion
    }

    /// <summary><#=tdis#>接口</summary><# if(tdis!=tdes){#>
    /// <remarks><#=tdes#></remarks><#}#>
    public partial interface I<#=Table.Name#>
    {
        #region 属性<#
        foreach(IDataColumn Field in Table.Columns)
        {
            String des=Field.Description;
            if(!String.IsNullOrEmpty(des)) des=des.Replace("\r\n"," ");
#>
        /// <summary><#=des#></summary>
        <#=Field.DataType.Name#> <#=Field.Name#> { get; set; }
<#
        }
#>        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}