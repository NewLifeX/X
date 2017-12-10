using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;

namespace XCode.Statistics
{
    /// <summary>统计助手类</summary>
    public static class StatHelper
    {
        private static String _Last;
        /// <summary>获取 或 新增 统计对象</summary>
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
                    Time = model.GetDate(model.Level),
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

            if (st != null) st.UpdateTime = DateTime.Now;

            return st;
        }
    }
}