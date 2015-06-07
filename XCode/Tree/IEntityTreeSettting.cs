using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace XCode
{
    /// <summary>实体树设置</summary>
    public interface IEntityTreeSetting
    {
        #region 设置型属性
        /// <summary>关联键名称，一般是主键，如ID</summary>
        String Key { get; set; }

        /// <summary>关联父键名，一般是Parent加主键，如ParentID</summary>
        String Parent { get; set; }

        /// <summary>排序字段，默认是"Sorting", "Sort", "Rank"之一</summary>
        String Sort { get; set; }

        /// <summary>名称键名，如Name，否则使用第一个非自增字段</summary>
        /// <remarks>影响NodeName、TreeNodeName、TreeNodeName2、FindByPath、GetFullPath、GetFullPath2等</remarks>
        String Name { get; set; }

        /// <summary>文本键名</summary>
        String Text { get; set; }

        /// <summary>是否缓存Childs、AllChilds、Parent等</summary>
        Boolean EnableCaching { get; set; }

        /// <summary>是否大排序，较大排序值在前面</summary>
        Boolean BigSort { get; set; }

        /// <summary>允许的最大深度。默认0，不限制</summary>
        Int32 MaxDeepth { get; set; }
        #endregion
    }

    /// <summary>实体树设置</summary>
    public class EntityTreeSetting<TEntity> : IEntityTreeSetting where TEntity : Entity<TEntity>, new()
    {
        #region 属性
        private IEntityOperate _Factory = Entity<TEntity>.Meta.Factory;
        /// <summary>实体操作者</summary>
        public IEntityOperate Factory { get { return _Factory; } set { _Factory = value; } }
        #endregion

        #region 设置型属性
        private String _Key;
        /// <summary>关联键名称，一般是主键，如ID</summary>
        public virtual String Key { get { return _Key ?? (_Key = Factory.Unique.Name); } set { _Key = value; } }

        private String _Parent;
        /// <summary>关联父键名，一般是Parent加主键，如ParentID</summary>
        public virtual String Parent
        {
            get
            {
                if (_Parent != null) return _Parent;

                var name = "Parent" + Key;
                // 不区分大小写的比较
                if (Factory.FieldNames.Contains(name, StringComparer.OrdinalIgnoreCase)) return name;

                return _Parent = "";
            }
            set { _Parent = value; }
        }

        private static String _Sort;
        /// <summary>排序字段，默认是"Sorting", "Sort", "Rank"之一</summary>
        public virtual String Sort
        {
            get
            {
                if (_Sort == null)
                {
                    // Empty与null不同，可用于区分是否已计算
                    _Sort = String.Empty;

                    var names = new String[] { "Sorting", "Sort", "Rank", "DisplayOrder", "Order" };
                    var fs = Factory.FieldNames;
                    foreach (var name in names)
                    {
                        // 不区分大小写的比较
                        if (fs.Contains(name, StringComparer.OrdinalIgnoreCase))
                        {
                            _Sort = name;
                            break;
                        }
                    }
                }
                return _Sort;
            }
            set { _Sort = value; }
        }

        private String _Name;
        /// <summary>名称键名，如Name，否则使用第一个非自增字段</summary>
        /// <remarks>影响NodeName、TreeNodeName、TreeNodeName2、FindByPath、GetFullPath、GetFullPath2等</remarks>
        public virtual String Name
        {
            get
            {
                if (_Name != null) return _Name;

                return _Name = Factory.FieldNames.Contains("Name", StringComparer.OrdinalIgnoreCase) ? "Name" : Factory.Fields.Where(f => !f.IsIdentity).FirstOrDefault().Name;
            }
            set { _Name = value; }
        }

        private String _Text;
        /// <summary>文本键名</summary>
        public virtual String Text
        {
            get
            {
                return Factory.FieldNames.Where(f => f.EqualIgnoreCase("Text", "Display", "DisplayName")).FirstOrDefault();
            }
            set { _Text = value; }
        }

        private Boolean _EnableCaching = true;
        /// <summary>是否缓存Childs、AllChilds、Parent等</summary>
        public virtual Boolean EnableCaching { get { return _EnableCaching; } set { _EnableCaching = value; } }

        private Boolean _BigSort = true;
        /// <summary>是否大排序，较大排序值在前面</summary>
        public virtual Boolean BigSort { get { return _BigSort; } set { _BigSort = value; } }

        private Int32 _MaxDeepth;
        /// <summary>允许的最大深度。默认0，不限制</summary>
        public virtual Int32 MaxDeepth { get { return _MaxDeepth; } set { _MaxDeepth = value; } }
        #endregion
    }
}