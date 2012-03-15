using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace NewLife.Collections
{
    /// <summary>栈接口</summary>
    /// <remarks>重点解决多线程环境下资源争夺以及使用lock造成性能损失的问题</remarks>
    /// <typeparam name="T"></typeparam>
    public interface IStack<T> : IEnumerable<T>, ICollection, IEnumerable, IDisposable
    {
        /// <summary>向栈压入一个对象</summary>
        /// <param name="item"></param>
        void Push(T item);

        /// <summary>从栈中弹出一个对象</summary>
        /// <returns></returns>
        T Pop();

        /// <summary>尝试从栈中弹出一个对象</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        Boolean TryPop(out T item);

        ///// <summary>获取栈顶对象，不弹栈</summary>
        ///// <returns></returns>
        //T Peek();

        ///// <summary>尝试获取栈顶对象，不弹栈</summary>
        ///// <param name="item"></param>
        ///// <returns></returns>
        //Boolean TryPeek(out T item);
    }
}