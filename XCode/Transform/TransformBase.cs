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
            //return Transform(DAL.Create(srcConn), DAL.Create(desConn));

            var tf = new T();
            tf.SrcConn = srcConn;
            tf.DesConn = desConn;

            return tf.Transform();
        }

        ///// <summary>把一个链接的数据全部导入到另一个链接</summary>
        ///// <param name="srcDal"></param>
        ///// <param name="desDal"></param>
        ///// <returns></returns>
        //public static Int32 Transform(DAL srcDal, DAL desDal)
        //{
        //    var tf = new T();
        //    tf.SrcDal = srcDal;
        //    tf.DesDal = desDal;

        //    return tf.Transform();
        //}
        #endregion

        #region 属性
        //private DAL _SrcDal;
        ///// <summary>源</summary>
        //public DAL SrcDal { get { return _SrcDal; } set { _SrcDal = value; } }

        //private DAL _DesDal;
        ///// <summary>目的</summary>
        //public DAL DesDal { get { return _DesDal; } set { _DesDal = value; } }

        private String _SrcConn;
        /// <summary>源</summary>
        public String SrcConn { get { return _SrcConn; } set { _SrcConn = value; } }

        private String _DesConn;
        /// <summary>目的</summary>
        public String DesConn { get { return _DesConn; } set { _DesConn = value; } }

        private ICollection<String> _TableNames;
        /// <summary>要导数据的表，为空表示全部</summary>
        public ICollection<String> TableNames { get { return _TableNames ?? (_TableNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase)); } set { _TableNames = value; } }

        private Int32 _BatchSize = 1000;
        /// <summary>每批处理多少行数据，默认1000</summary>
        public Int32 BatchSize { get { return _BatchSize; } set { _BatchSize = value; } }
        #endregion

        #region 方法
        /// <summary>把一个链接的数据全部导入到另一个链接</summary>
        /// <returns></returns>
        public abstract Int32 Transform();
        #endregion
    }
}