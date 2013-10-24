using System;

namespace NewLife.Net.Sdp
{
    /// <summary>SDP 时间</summary>
    public class SdpTime
    {
        #region 属性
        private Int64 _StartTime;
        /// <summary>开始时间。1900年以来的秒数</summary>
        public Int64 StartTime { get { return _StartTime; } set { _StartTime = value; } }

        private Int64 _StopTime;
        /// <summary>停止时间。1900年以来的秒数</summary>
        public Int64 StopTime { get { return _StopTime; } set { _StopTime = value; } }
        #endregion

        #region 方法
        /// <summary>分析</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static SdpTime Parse(string value)
        {
            if (value == null) throw new ArgumentNullException("value");

            value = value.Trim();

            if (!value.ToLower().StartsWith("t=")) throw new NetException("Invalid SDP Time('t=') value '" + value + "'.");

            value = value.Substring(2);

            string[] values = value.Split(' ');
            if (values.Length != 2) throw new NetException("Invalid SDP Time('t=') value '" + value + "'.");

            var entity = new SdpTime();
            entity.StartTime = Convert.ToInt64(values[0]);
            entity.StopTime = Convert.ToInt64(values[1]);
            return entity;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "t=" + StartTime + " " + StopTime + "\r\n";
        }
        #endregion
    }
}