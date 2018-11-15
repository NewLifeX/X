using System;
using System.Linq;
using NewLife.Log;

namespace XCode.Statistics
{
    /// <summary>统计助手类</summary>
    public static class StatHelper
    {
        private static String _Last;
        /// <summary>获取 或 新增 统计对象</summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="model"></param>
        /// <param name="find"></param>
        /// <param name="onCreate"></param>
        /// <returns></returns>
        public static TEntity GetOrAdd<TEntity, TModel>(TModel model, Func<TModel, Boolean, TEntity> find, Action<TEntity> onCreate = null)
            where TModel : StatModel
            where TEntity : Entity<TEntity>, IStat, new()
        {
            if (model == null) return null;

            var st = find(model, true);
            //查询到结果保存
            if (st == null)
            {
                st = new TEntity
                {
                    Level = model.Level,
                    Time = model.Time,
                    CreateTime = DateTime.Now,
                };

                onCreate?.Invoke(st);

                // 插入失败时，再次查询
                try
                {
                    st.Insert();
                }
                catch (Exception ex)
                {
                    st = find(model, false);
                    if (st == null)
                    {
                        ex = ex.GetTrue();
                        if (ex.Message != _Last)
                        {
                            _Last = ex.Message;
                            XTrace.WriteException(ex);
                        }

                        return null;
                    }
                }
            }

            // 设置所有累加字段为脏数据
            var df = Entity<TEntity>.Meta.Factory.AdditionalFields;
            if (df != null && df.Count > 0 && st is IEntity st2 && !st2.HasDirty)
            {
                foreach (var di in df)
                {
                    st2.Dirtys[di] = true;
                }
            }

            if (st != null) st.UpdateTime = DateTime.Now;

            return st;
        }
    }
}