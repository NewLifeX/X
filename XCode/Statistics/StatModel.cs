using System;
using System.Collections.Generic;
using NewLife.Reflection;
using NewLife;

namespace XCode.Statistics
{
    /// <summary>统计模型</summary>
    /// <typeparam name="T"></typeparam>
    public class StatModel<T> : StatModel/*, IEqualityComparer<T>*/ where T : StatModel<T>, new()
    {
        #region 方法
        /// <summary>拷贝</summary>
        /// <param name="model"></param>
        public virtual void Copy(T model)
        {
            Time = model.Time;
            Level = model.Level;
        }

        /// <summary>克隆到目标类型</summary>
        /// <returns></returns>
        public virtual T Clone()
        {
            var model = GetType().CreateInstance() as T;
            model.Copy(this);
            // 克隆不能格式化时间，否则会丢失时间精度
            //Time = GetDate(model.Level);

            return model;
        }

        /// <summary>分割为多个层级</summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        public virtual List<T> Split(params StatLevels[] levels)
        {
            var list = new List<T>();
            foreach (var item in levels)
            {
                var st = Clone();
                st.Level = item;
                st.Time = st.GetDate(item);

                list.Add(st);
            }

            return list;
        }
        #endregion

        #region 相等比较
        ///// <summary>相等</summary>
        ///// <param name="x"></param>
        ///// <param name="y"></param>
        ///// <returns></returns>
        //public virtual Boolean Equals(T x, T y)
        //{
        //    if (x == null) return y == null;
        //    if (y != null) return false;

        //    return x.Level == y.Level && x.Time == y.Time;
        //}

        ///// <summary>获取哈希</summary>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //public virtual Int32 GetHashCode(T obj)
        //{
        //    return Level.GetHashCode() ^ Time.GetHashCode();
        //}
        #endregion
    }

    /// <summary>统计模型</summary>
    public class StatModel
    {
        #region 属性
        /// <summary>时间</summary>
        public DateTime Time { get; set; }

        /// <summary>层级</summary>
        public StatLevels Level { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public StatModel() { }
        #endregion

        #region 方法
        /// <summary>获取不同层级的时间。选择层级区间的开头</summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public DateTime GetDate(StatLevels level)
        {
            var dt = Time;
            switch (level)
            {
                case StatLevels.All: return new DateTime(1, 1, 1);
                case StatLevels.Year: return new DateTime(dt.Year, 1, 1);
                case StatLevels.Month: return new DateTime(dt.Year, dt.Month, 1);
                case StatLevels.Day: return dt.Date;
                case StatLevels.Hour: return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
                case StatLevels.Minute: return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
                default:
                    break;
            }

            return dt;
        }

        /// <summary>数据库时间转显示字符串</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var dt = Time;
            switch (Level)
            {
                case StatLevels.All: return "全局";
                case StatLevels.Year: return $"{dt:yyyy}";
                case StatLevels.Month: return $"{dt:yyyy-MM}";
                case StatLevels.Day: return $"{dt:yyyy-MM-dd}";
                case StatLevels.Hour: return $"{dt:yyyy-MM-dd HH}";
                case StatLevels.Minute: return $"{dt:yyyy-MM-dd HH:mm}";
                default: return Level + "";
            }
        }

        /// <summary>使用参数填充</summary>
        /// <param name="ps">请求参数</param>
        /// <param name="defLevel">默认级别</param>
        public virtual void Fill(IDictionary<String, String> ps, StatLevels defLevel = StatLevels.Day)
        {
            //this.Copy(ps, true);

            foreach (var pi in GetType().GetProperties())
            {
                if (!pi.CanWrite) continue;
                if (pi.GetIndexParameters().Length > 0) continue;

                if (ps.TryGetValue(pi.Name, out var val))
                {
                    if (pi.PropertyType.IsInt() || pi.PropertyType.IsEnum)
                        this.SetValue(pi, val.ToInt(-1));
                    else
                        this.SetValue(pi, val.ChangeType(pi.PropertyType));
                }
            }

            if (ps[nameof(Level)].ToInt(-1) < 0)
            {
                Level = defLevel;
                ps[nameof(Level)] = (Int32)Level + "";
            }

            // 格式化时间
            Time = GetDate(Level);
        }
        #endregion

        #region 相等比较
        /// <summary>相等</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Boolean Equals(Object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj == null) return false;

            if (obj is StatModel model) return Level == model.Level && Time == model.Time;

            return false;
        }

        /// <summary>获取哈希</summary>
        /// <returns></returns>
        public override Int32 GetHashCode() => Level.GetHashCode() ^ Time.GetHashCode();

        ///// <summary>相等</summary>
        ///// <param name="x"></param>
        ///// <param name="y"></param>
        ///// <returns></returns>
        //public static Boolean operator ==(StatModel x, StatModel y)
        //{
        //    if (ReferenceEquals(x, y)) return true;
        //    if ((Object)x == null || (Object)y == null) return false;

        //    return x.Equals(y);
        //}

        ///// <summary>不等</summary>
        ///// <param name="x"></param>
        ///// <param name="y"></param>
        ///// <returns></returns>
        //public static Boolean operator !=(StatModel x, StatModel y) => !(x == y);
        #endregion
    }
}