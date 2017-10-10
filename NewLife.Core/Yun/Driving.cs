using System;

namespace NewLife.Yun
{
    /// <summary>驾车距离和时间</summary>
    public class Driving
    {
        /// <summary>距离。单位千米</summary>
        public Int32 Distance { get; set; }

        /// <summary>路线耗时。单位秒</summary>
        public Int32 Duration { get; set; }
    }
}