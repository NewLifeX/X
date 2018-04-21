using System;
using NewLife.Reflection;

namespace NewLife.Model
{
    /// <summary>用于创建对象的工厂接口</summary>
    /// <typeparam name="T"></typeparam>
    public interface IFactory<T>
    {
        /// <summary>创建对象实例</summary>
        /// <param name="args"></param>
        /// <returns></returns>
        T Create(Object args = null);
    }

    /// <summary>反射创建对象的工厂</summary>
    /// <typeparam name="T"></typeparam>
    public class Factory<T> : IFactory<T>
    {
        /// <summary>创建对象实例</summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual T Create(Object args = null) => (T)typeof(T).CreateInstance();
    }
}