using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    class DataFactory : IDataFactory
    {
        #region 属性
        private IServiceProvider _Provider;
        /// <summary>服务提供者</summary>
        public IServiceProvider Provider
        {
            get { return _Provider; }
            set { _Provider = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 创建接口对象，优先使用服务提供者提供的实现，然后选择默认实现
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        I Create<I, T>() where T : I
        {
            Type type = null;
            if (Provider != null) type = Provider.GetService(typeof(I)) as Type;
            if (type == null) type = typeof(T);

            return (I)TypeX.CreateInstance(type);
        }
        #endregion

        #region IDataFactory 成员
        public IDataTable CreateTable()
        {
            return Create<IDataTable, XTable>();
        }

        public IDataColumn CreateColumn()
        {
            return Create<IDataColumn, XField>();
        }

        public IDataForeignKey CreateForeignKey()
        {
            return Create<IDataForeignKey, XForeignKey>();
        }

        public IDataIndex CreateIndex()
        {
            return Create<IDataIndex, XIndex>();
        }
        #endregion
    }
}