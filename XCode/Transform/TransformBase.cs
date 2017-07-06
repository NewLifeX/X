using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using NewLife.Collections;

namespace XCode.Transform
{
    /// <summary>数据转换基类</summary>
    /// <typeparam name="T"></typeparam>
    public abstract class TransformBase<T> where T : TransformBase<T>, new()
    {
        #region 静态
        /// <summary>把一个链接的数据全部导入到另一个链接</summary>
        /// <param name="srcConn"></param>
        /// <param name="desConn"></param>
        /// <returns></returns>
        public static Int32 Transform(String srcConn, String desConn)
        {
            var tf = new T();
            tf.SrcConn = srcConn;
            tf.DesConn = desConn;

            return tf.Transform();
        }
        #endregion

        #region 属性
        /// <summary>源</summary>
        public String SrcConn { get; set; }

        /// <summary>目的</summary>
        public String DesConn { get; set; }

        /// <summary>要导数据的表，为空表示全部</summary>
        public ICollection<String> TableNames { get; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        /// <summary>每批处理多少行数据，默认1000</summary>
        public Int32 BatchSize { get; set; } = 1000;
        #endregion

        #region 方法
        /// <summary>把一个链接的数据全部导入到另一个链接</summary>
        /// <returns></returns>
        public abstract Int32 Transform();
        #endregion
    }
}