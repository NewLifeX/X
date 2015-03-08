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
        String Key { get; }

        /// <summary>关联父键名，一般是Parent加主键，如ParentID</summary>
        String Parent { get; }

        /// <summary>排序字段，默认是"Sorting", "Sort", "Rank"之一</summary>
        String Sort { get; }

        /// <summary>名称键名，如Name，否则使用第一个非自增字段</summary>
        /// <remarks>影响NodeName、TreeNodeName、TreeNodeName2、FindByPath、GetFullPath、GetFullPath2等</remarks>
        String Name { get; }

        /// <summary>是否缓存Childs、AllChilds、Parent等</summary>
        Boolean EnableCaching { get; }

        /// <summary>是否大排序，较大排序值在前面</summary>
        Boolean BigSort { get; }

        /// <summary>允许的最大深度。默认0，不限制</summary>
        Int32 MaxDeepth { get; }
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
        /// <summary>关联键名称，一般是主键，如ID</summary>
        public virtual String Key { get { return Factory.Unique.Name; } }

        /// <summary>关联父键名，一般是Parent加主键，如ParentID</summary>
        public virtual String Parent
        {
            get
            {
                var name = "Parent" + Key;
                // 不区分大小写的比较
                if (Factory.FieldNames.Contains(name, StringComparer.OrdinalIgnoreCase)) return name;

                return null;
            }
        }

        private static String _SortingKeyName;
        /// <summary>排序字段，默认是"Sorting", "Sort", "Rank"之一</summary>
        public virtual String Sort
        {
            get
            {
                if (_SortingKeyName == null)
                {
                    // Empty与null不同，可用于区分是否已计算
                    _SortingKeyName = String.Empty;

                    var names = new String[] { "Sorting", "Sort", "Rank", "DisplayOrder", "Order" };
                    var fs = Factory.FieldNames;
                    foreach (var name in names)
                    {
                        // 不区分大小写的比较
                        if (fs.Contains(name, StringComparer.OrdinalIgnoreCase))
                        {
                            _SortingKeyName = name;
                            break;
                        }
                    }
                }
                return _SortingKeyName;
            }
        }

        /// <summary>名称键名，如Name，否则使用第一个非自增字段</summary>
        /// <remarks>影响NodeName、TreeNodeName、TreeNodeName2、FindByPath、GetFullPath、GetFullPath2等</remarks>
        public virtual String Name { get { return Factory.FieldNames.Contains("Name", StringComparer.OrdinalIgnoreCase) ? "Name" : Factory.Fields.Where(f => !f.IsIdentity).FirstOrDefault().Name; } }

        /// <summary>是否缓存Childs、AllChilds、Parent等</summary>
        public virtual Boolean EnableCaching { get { return true; } }

        /// <summary>是否大排序，较大排序值在前面</summary>
        public virtual Boolean BigSort { get { return true; } }

        /// <summary>允许的最大深度。默认0，不限制</summary>
        public virtual Int32 MaxDeepth { get { return 0; } }
        #endregion
    }
}