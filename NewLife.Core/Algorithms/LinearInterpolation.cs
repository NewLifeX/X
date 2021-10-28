using System;
using NewLife.Data;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 线性插值
    /// </summary>
    public class LinearInterpolation : IInterpolation
    {
        /// <summary>
        /// 插值处理
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="prev">上一个点</param>
        /// <param name="next">下一个点</param>
        /// <param name="current">当前点</param>
        /// <returns></returns>
        public Double Process(TimePoint[] data, Int32 prev, Int32 next, Int32 current)
        {
            var dt = (data[next].Value - data[prev].Value) / (data[next].Time - data[prev].Time);
            return data[prev].Value + (current - data[prev].Time) * dt;
        }

        /// <summary>
        /// 线性插值（返回插值后的数组，包括起止点）
        /// </summary>
        /// <param name="d1">起始值</param>
        /// <param name="d2">终止值</param>
        /// <param name="num">插值后数组插值后长度（包括起止点）</param>
        /// <returns>插值后结果</returns>
        public static Double[] Process(Double d1, Double d2, Int32 num)
        {
            var data = new Double[num];
            var dt = (d2 - d1) / (num - 1);
            for (var i = 0; i < num; i++)
            {
                data[i] = d1 + (i * dt);
            }
            return data;
        }

        ///// <summary>
        ///// 插值处理
        ///// </summary>
        ///// <param name="data">原始数据</param>
        ///// <param name="size">桶大小。如60/3600/86400</param>
        ///// <param name="offset">偏移量。时间不是对齐零点时使用</param>
        ///// <returns></returns>
        //public TimePoint[] Process(TimePoint[] data, Int32 size, Int32 offset = 0)
        //{
        //    //var v = (data[0].Time / size) * size + offset;

        //    TimePoint tp = default;
        //    var s = Int64.MinValue;

        //    // 遍历每一个点
        //    var list = new List<TimePoint>();
        //    for (var i = 0; i < data.Length; i++)
        //    {
        //        var v = (data[i].Time / size) * size + offset;
        //        if (v > s)
        //        {

        //        }
        //    }
        //}
    }
}