using System;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 双线性插值
    /// </summary>
    public class BilinearInterpolation
    {
        public static Single[,] Process(Single[,] array, Int32 timeLength, Int32 valueLength)
        {
            var rs = new Single[timeLength, valueLength];

            var scale1 = (Single)array.GetLength(0) / timeLength;
            var scale2 = (Single)array.GetLength(1) / valueLength;

            for (var i = 0; i < timeLength; i++)
            {
                for (var j = 0; j < valueLength; j++)
                {
                    var d1 = i * scale1;
                    var d2 = j * scale2;
                    var n1 = (Int32)Math.Floor(d1);
                    var n2 = (Int32)Math.Floor(d2);
                    var leftUp = (d1 - n1) * (d2 - n2);
                    var rightUp = (n1 + 1 - d1) * (d2 - n2);
                    var rightDown = (n1 + 1 - d1) * (n2 + 1 - d2);
                    var leftDown = (d1 - n1) * (n2 + 1 - d2);
                    rs[i, j] = 
                        array[n1, n2] * rightDown + 
                        array[n1 + 1, n2] * leftDown + 
                        array[n1 + 1, n2 + 1] * leftUp + 
                        array[n1, n2 + 1] * rightUp;
                }
            }

            return rs;
        }
    }
}