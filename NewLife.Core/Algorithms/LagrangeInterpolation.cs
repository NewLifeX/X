using System;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 拉格朗日插值
    /// </summary>
    public class LagrangeInterpolation
    {
        /// <summary>
        /// X各点坐标组成的数组
        /// </summary>
        public Int32[] Times { get; set; }

        /// <summary>
        /// X各点对应的Y坐标值组成的数组
        /// </summary>
        public Double[] Values { get; set; }

        /// <summary>
        /// x数组或者y数组中元素的个数, 注意两个数组中的元素个数需要一样
        /// </summary>
        public Int32 Threshold { get; set; }

        /// <summary>
        /// 初始化拉格朗日插值
        /// </summary>
        /// <param name="times">X各点坐标组成的数组</param>
        /// <param name="values">X各点对应的Y坐标值组成的数组</param>
        public LagrangeInterpolation(Int32[] times, Double[] values)
        {
            Times = times;
            Values = values;
            Threshold = times.Length;
        }

        /// <summary>
        /// 获得某个横坐标对应的Y坐标值
        /// </summary>
        /// <param name="time">x坐标值</param>
        /// <returns></returns>
        public Double GetValue(Int32 time)
        {
            //返回值
            var value = 0.0;
            //如果初始的离散点为空, 返回0
            if (Threshold < 1) return value;

            //如果初始的离散点只有1个, 返回该点对应的Y值
            if (Threshold == 1)
            {
                value = Values[0];
                return value;
            }
            //如果初始的离散点只有2个, 进行线性插值并返回插值
            if (Threshold == 2)
            {
                value = (Values[0] * (time - Times[1]) - Values[1] * (time - Times[0])) / (Times[0] - Times[1]);
                return value;
            }
            //用于累乘数组始末下标
            Int32 start, end;
            //如果插值点小于第一个点X坐标, 取数组前3个点做插值
            if (time <= Times[1])
            {
                start = 0;
                end = 2;
            }
            //如果插值点大于等于最后一个点X坐标, 取数组最后3个点做插值
            else if (time >= Times[Threshold - 2])
            {
                start = Threshold - 3;
                end = Threshold - 1;
            }
            //除了上述的一些特殊情况, 通常情况如下
            else
            {
                start = 1;
                end = Threshold;
                Int32 temp;
                //使用二分法决定选择哪三个点做插值
                while ((end - start) != 1)
                {
                    temp = (start + end) / 2;
                    if (time < Times[temp - 1])
                        end = temp;
                    else
                        start = temp;
                }
                start--;
                end--;
                //看插值点跟哪个点比较靠近
                if (Math.Abs(time - Times[start]) < Math.Abs(time - Times[end]))
                    start--;
                else
                    end++;
            }
            //这时已经确定了取哪三个点做插值, 第一个点为x[start]
            Double valueTemp;
            //注意是二次的插值公式
            for (var i = start; i <= end; i++)
            {
                valueTemp = 1.0;
                for (var j = start; j <= end; j++)
                    if (j != i)
                        valueTemp *= (time - Times[j]) / (Double)(Times[i] - Times[j]);
                value += valueTemp * Values[i];
            }
            return value;
        }
    }
}