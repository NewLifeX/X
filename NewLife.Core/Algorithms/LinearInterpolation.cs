using System;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 线性插值
    /// </summary>
    public class LinearInterpolation
    {
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
    }
}