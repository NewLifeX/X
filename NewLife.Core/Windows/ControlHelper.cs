using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using NewLife.Reflection;

namespace System.Windows.Forms
{
    /// <summary>控件助手</summary>
    public static class ControlHelper
    {
        #region 在UI线程上执行委托
        /// <summary>执行无参委托</summary>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static void Invoke(this Control control, Func method)
        {
            control.Invoke(method);
        }

        /// <summary>执行仅返回值委托</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static TResult Invoke<TResult>(this Control control, Func<TResult> method)
        {
            return (TResult)control.Invoke(method);
        }

        /// <summary>执行单一参数无返回值的委托</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static void Invoke<T>(this Control control, Action<T> method, T arg)
        {
            control.Invoke(method, arg);
        }

        /// <summary>执行单一参数和返回值的委托</summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static TResult Invoke<T, TResult>(this Control control, Func<T, TResult> method, T arg)
        {
            return (TResult)control.Invoke(method, arg);
        }

        /// <summary>执行二参数无返回值的委托</summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <param name="arg"></param>
        /// <param name="arg2"></param>
        public static void Invoke<T, T2>(this Control control, Action<T, T2> method, T arg, T2 arg2)
        {
            control.Invoke(method, arg, arg2);
        }

        /// <summary>执行二参数和返回值的委托</summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <param name="arg"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public static TResult Invoke<T, T2, TResult>(this Control control, Func<T, T2, TResult> method, T arg, T2 arg2)
        {
            return (TResult)control.Invoke(method, arg, arg2);
        }
        #endregion
    }
}