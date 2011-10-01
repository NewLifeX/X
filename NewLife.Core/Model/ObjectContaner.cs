using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;

namespace NewLife.Model
{
    class ObjectContaner
    {
        private static Dictionary<Type, object> _stores = null;
        private static Dictionary<Type, object> Stores { get { return _stores ?? (_stores = new Dictionary<Type, object>()); } }

        private static Dictionary<string, object> CreateConstructorParameter(Type type)
        {
            Dictionary<string, object> paramArray = new Dictionary<string, object>();

            ConstructorInfo[] cis = type.GetConstructors();
            if (cis.Length > 1) throw new Exception("target object has more then one constructor,container can't peek one for you.");

            foreach (ParameterInfo pi in cis[0].GetParameters())
            {
                if (Stores.ContainsKey(pi.ParameterType)) paramArray.Add(pi.Name, GetInstance(pi.ParameterType));
            }
            return paramArray;
        }

        public static object GetInstance(Type type)
        {
            /* 这里是重点！
             * 1，如果容器里面没有这个类型，则直接的创建对象返回
             * 2，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回
             * 3，如果容器里面包含这个类型，并且指向的实例不为空，则返回
             * 4，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回
             * 
             * 这里有一点跟我以往的想法非常不同，我都习惯没有对象的时候，创建并加入字典。
             * 这里采用两种方式，注册类型的时候，如果指定了实例，则表示这个类型对应单一的实例；
             * 如果不指定实例，则表示支持该类型，每次创建。
             */

            Object obj = null;
            if (!Stores.TryGetValue(type, out obj)) return Activator.CreateInstance(type, false);

            // 构造函数注入
            ConstructorInfo[] cis = type.GetConstructors();
            if (cis.Length != 0)
            {
                Dictionary<string, object> paramArray = CreateConstructorParameter(type);
                List<object> cArray = new List<object>();
                foreach (ParameterInfo pi in cis[0].GetParameters())
                {
                    if (paramArray.ContainsKey(pi.Name))
                        cArray.Add(paramArray[pi.Name]);
                    else
                        cArray.Add(null);
                }
                return cis[0].Invoke(cArray.ToArray());
            }
            else if (obj != null)
                return obj;
            else
            {
                obj = Activator.CreateInstance(type, false);
                // 赋值注入
                foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(obj))
                {
                    if (!pd.IsReadOnly && Stores.ContainsKey(pd.PropertyType)) pd.SetValue(obj, GetInstance(pd.PropertyType));
                }
                return obj;
            }
        }

        public static void Register(Type type, object obj)
        {
            if (Stores.ContainsKey(type))
                Stores[type] = obj;
            else
                Stores.Add(type, obj);
        }

        public static void Register(Type type)
        {
            if (!Stores.ContainsKey(type)) Stores.Add(type, null);
        }
    }
}