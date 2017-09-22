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
    [DisplayName("统计字段")]
    public class FieldCache<TEntity> : EntityCache<TEntity> where TEntity : Entity<TEntity>, new()
    {
        private FieldItem _field;
        private FieldItem _Unique;

        /// <summary>最大行数。默认20</summary>
        public Int32 MaxRows { get; set; } = 20;

        /// <summary>获取显示名的委托</summary>
        public Func<TEntity, String> GetDisplay { get; set; }

        /// <summary>显示名格式化字符串，两个参数是名称和个数</summary>
        public String DisplayFormat { get; set; } = "{0} ({1:n0})";

        /// <summary>对指定字段使用实体缓存</summary>
        /// <param name="field"></param>
        public FieldCache(FieldItem field)
        {
            WaitFirst = false;
            Expire = 10 * 60;
            FillListMethod = () =>
            {
                return Entity<TEntity>.FindAll(_field.GroupBy(), _Unique.Desc(), _Unique.Count() & _field, 0, MaxRows);
            };

            _field = field;
            {
                var tb = field.Table;
                var id = tb.Identity;
                if (id == null && tb.PrimaryKeys.Length == 1) id = tb.PrimaryKeys[0];
                _Unique = id ?? throw new Exception("{0}缺少唯一主键，无法使用缓存".F(tb.TableName));
            }
        }

        /// <summary>获取所有类别名称</summary>
        /// <returns></returns>
        public IDictionary<String, String> FindAllName()
        {
            //var id = _field.Table.Identity;
            var list = Entities.Take(MaxRows).ToList();

            var dic = new Dictionary<String, String>();
            foreach (var entity in list)
            {
                var k = entity[_field.Name] + "";
                var v = k;
                if (GetDisplay != null)
                {
                    v = GetDisplay(entity);
                    if (v.IsNullOrEmpty()) v = "[{0}]".F(k);
                }

                dic[k] = DisplayFormat.F(v, entity[_Unique.Name]);
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