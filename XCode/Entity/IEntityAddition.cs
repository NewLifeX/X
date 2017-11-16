using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace XCode
{
    /// <summary>实体累加接口。实现Count=Count+123的效果</summary>
    public interface IEntityAddition
    {
        #region 属性
        /// <summary>实体对象</summary>
        IEntity Entity { get; set; }
        #endregion

        #region 累加
        /// <summary>设置累加字段</summary>
        /// <param name="names">字段集合</param>
        void Set(IEnumerable<String> names);

        /// <summary>获取快照</summary>
        /// <returns></returns>
        IDictionary<String, Object[]> Get();

        /// <summary>使用快照重置</summary>
        /// <param name="value"></param>
        void Reset(IDictionary<String, Object[]> value);
        #endregion
    }

    /// <summary>实体累加接口。实现Count+=1的效果</summary>
    class EntityAddition : IEntityAddition
    {
        #region 属性
        /// <summary>实体对象</summary>
        public IEntity Entity { get; set; }
        #endregion

        #region 累加
        [NonSerialized]
        private ConcurrentDictionary<String, Object> _Data;

        /// <summary>设置累加字段</summary>
        /// <param name="names">字段集合</param>
        public void Set(IEnumerable<String> names)
        {
            // 检查集合是否为空
            if (_Data == null) _Data = new ConcurrentDictionary<String, Object>();

            foreach (var item in names)
            {
                _Data.TryAdd(item, Entity[item]);
            }
        }

        public IDictionary<String, Object[]> Get()
        {
            var dic = new Dictionary<String, Object[]>();

            var df = _Data;
            if (df == null) return dic;

            foreach (var item in df)
            {
                var vs = new Object[2];
                dic[item.Key] = vs;

                vs[0] = Entity[item.Key];
                vs[1] = item.Value;
            }

            return dic;
        }

        public void Reset(IDictionary<String, Object[]> value)
        {
            if (value == null || value.Count == 0) return;

            var df = _Data;
            if (df == null) return;

            foreach (var item in df)
            {
                var vs = value[item.Key];
                if (vs != null && vs.Length > 0) df[item.Key] = vs[0];
            }
        }
        #endregion

        #region 静态
        public static IList<IEntity> SetField(IList<IEntity> list)
        {
            if (list == null || list.Count < 1) return list;

            var entityType = list[0].GetType();
            var factory = EntityFactory.CreateOperate(entityType);
            var fs = factory.AdditionalFields;
            if (fs.Count > 0)
            {
                foreach (EntityBase entity in list)
                {
                    if (entity != null) entity.Addition.Set(fs);
                }
            }

            return list;
        }

        public static void SetField(EntityBase entity)
        {
            if (entity == null) return;

            var factory = EntityFactory.CreateOperate(entity.GetType());
            var fs = factory.AdditionalFields;
            if (fs.Count > 0) entity.Addition.Set(fs);
        }
        #endregion
    }
}