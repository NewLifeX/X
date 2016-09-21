using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using XCode;
using XCode.Cache;
using XCode.Configuration;

namespace XCode.Cache
{
    /// <summary>统计字段缓存</summary>
    /// <typeparam name="TEntity"></typeparam>
    [DisplayName("统计字段缓存")]
    public class FieldCache<TEntity> : EntityCache<TEntity> where TEntity : Entity<TEntity>, new()
    {
        private FieldItem _field;

        /// <summary>最大行数。默认20</summary>
        public Int32 MaxRows { get; set; } = 20;

        /// <summary>对指定字段使用实体缓存</summary>
        /// <param name="field"></param>
        public FieldCache(FieldItem field)
        {
            WaitFirst = false;
            Expire = 10 * 60;
            FillListMethod = () =>
            {
                // 根据数量降序
                var id = field.Table.Identity;
                return Entity<TEntity>.FindAll(field.GroupBy(), id.Desc(), id.Count() & field, 0, MaxRows);
            };

            _field = field;
        }

        /// <summary>获取所有类别名称</summary>
        /// <returns></returns>
        public IDictionary<String, String> FindAllName()
        {
            var id = _field.Table.Identity;
            var list = Entities.ToList().Take(MaxRows).ToList();

            var dic = new Dictionary<String, String>();
            foreach (var entity in list)
            {
                var k = entity[_field.Name];

                // 非字符串16进制显示
                if (k is String)
                {
                    var v = "{0} ({1:n0})".F(k, entity[id.Name]);
                    dic[k + ""] = v;
                }
                else
                {
                    var v = "{0:x4} ({1:n0})".F(k, entity[id.Name]);
                    dic[k + ""] = v;
                }
            }
            return dic;
        }

        #region 辅助
        /// <summary>输出名称</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var type = GetType();
            return "{0}<{1}>[{2}]".F(type.GetDisplayName() ?? type.Name, typeof(TEntity).FullName, _field.Name);
        }
        #endregion
    }
}