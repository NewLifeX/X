using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
        private String[] _Names;
        private Object[] _Values;

        /// <summary>设置累加字段</summary>
        /// <param name="names">字段集合</param>
        public void Set(IEnumerable<String> names)
        {
            var ns = new List<String>();
            var vs = new List<Object>();
            foreach (var item in names)
            {
                ns.Add(item);
                vs.Add(Entity[item]);
            }

            _Names = ns.ToArray();
            _Values = vs.ToArray();
        }

        public IDictionary<String, Object[]> Get()
        {
            var dic = new Dictionary<String, Object[]>();
            if (_Names == null) return dic;

            for (var i = 0; i < _Names.Length; i++)
            {
                var key = _Names[i];

                var vs = new Object[2];
                dic[key] = vs;

                vs[0] = Entity[key];
                vs[1] = _Values[i];
            }

            return dic;
        }

        public void Reset(IDictionary<String, Object[]> dfs)
        {
            if (dfs == null || dfs.Count == 0) return;
            if (_Names == null) return;

            for (var i = 0; i < _Names.Length; i++)
            {
                var key = _Names[i];
                if (dfs.TryGetValue(key, out var vs) && vs != null && vs.Length > 0) _Values[i] = vs[0];
            }
        }
        #endregion

        #region 静态
        public static void SetField(IEnumerable<IEntity> list)
        {
            if (list == null) return;

            var first = list.FirstOrDefault();
            if (first == null) return;

            var fs = first.GetType().AsFactory().AdditionalFields;
            if (fs.Count > 0)
            {
                foreach (EntityBase entity in list)
                {
                    if (entity != null) entity.Addition.Set(fs);
                }
            }
        }

        public static void SetField(EntityBase entity)
        {
            if (entity == null) return;

            var fs = entity.GetType().AsFactory().AdditionalFields;
            if (fs.Count > 0) entity.Addition.Set(fs);
        }
        #endregion
    }
}