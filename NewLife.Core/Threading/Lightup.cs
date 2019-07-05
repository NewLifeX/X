using System;
using System.Linq;
using System.Reflection;
using System.Threading;

#if NET4
namespace System
{
    internal abstract class Lightup
    {
        private static readonly Type[] EmptyTypes = new Type[0];

        private readonly Type _type;

        protected Lightup(Type type)
        {
            _type = type;
        }

        protected bool TryGet<T>(ref Delegate storage, string propertyName, out T value)
        {
            return TryCall(ref storage, "get_" + propertyName, out value);
        }

        protected T Get<T>(ref Delegate storage, string propertyName)
        {
            return Call<T>(ref storage, "get_" + propertyName);
        }

        protected void Set<T>(ref Delegate storage, string propertyName, T value)
        {
            Call(ref storage, "set_" + propertyName, value);
        }

        protected void Set<TI, TV>(ref Delegate storage, TI instance, string propertyName, TV value)
        {
            Call(ref storage, instance, "set_" + propertyName, value);
        }

        protected bool TrySet<TI, TV>(ref Delegate storage, TI instance, string propertyName, TV value)
        {
            return TryCall(ref storage, instance, "set_" + propertyName, value);
        }

        protected bool TryCall<T>(ref Delegate storage, string methodName, out T returnValue)
        {
            Func<T> methodAccessor = GetMethodAccessor<Func<T>>(ref storage, methodName, true);
            if (methodAccessor == null)
            {
                returnValue = default(T);
                return false;
            }
            returnValue = methodAccessor.Invoke();
            return true;
        }

        protected T Call<T>(ref Delegate storage, string methodName)
        {
            Func<T> methodAccessor = GetMethodAccessor<Func<T>>(ref storage, methodName, true);
            if (methodAccessor == null)
            {
                throw new InvalidOperationException();
            }
            return methodAccessor.Invoke();
        }

        protected void Call(ref Delegate storage, string methodName)
        {
            Action methodAccessor = GetMethodAccessor<Action>(ref storage, methodName, true);
            if (methodAccessor == null)
            {
                throw new InvalidOperationException();
            }
            methodAccessor.Invoke();
        }

        protected bool TryCall<TI, TV>(ref Delegate storage, TI instance, string methodName, TV parameter)
        {
            Action<TI, TV> methodAccessor = GetMethodAccessor<Action<TI, TV>>(ref storage, methodName, false);
            if (methodAccessor == null)
            {
                return false;
            }
            methodAccessor.Invoke(instance, parameter);
            return true;
        }

        protected bool TryCall<TI, TV1, TV2>(ref Delegate storage, TI instance, string methodName, TV1 parameter1, TV2 parameter2)
        {
            Action<TI, TV1, TV2> methodAccessor = GetMethodAccessor<Action<TI, TV1, TV2>>(ref storage, methodName, false);
            if (methodAccessor == null)
            {
                return false;
            }
            methodAccessor.Invoke(instance, parameter1, parameter2);
            return true;
        }

        protected void Call<TI, TV>(ref Delegate storage, TI instance, string methodName, TV parameter)
        {
            Action<TI, TV> methodAccessor = GetMethodAccessor<Action<TI, TV>>(ref storage, methodName, false);
            if (methodAccessor == null)
            {
                throw new InvalidOperationException();
            }
            methodAccessor.Invoke(instance, parameter);
        }

        protected void Call<T>(ref Delegate storage, string methodName, T parameter)
        {
            Action<T> methodAccessor = GetMethodAccessor<Action<T>>(ref storage, methodName, true);
            if (methodAccessor == null)
            {
                throw new InvalidOperationException();
            }
            methodAccessor.Invoke(parameter);
        }

        protected static T Create<T>(params object[] parameters)
        {
            Type[] argumentTypes = Enumerable.ToArray(Enumerable.Select<object, Type>(parameters, (object p) => p.GetType()));
            Func<object[], T> func = CreateActivator<T>(argumentTypes);
            return func.Invoke(parameters);
        }

        protected abstract object GetInstance();

        private static Func<object[], T> CreateActivator<T>(Type[] argumentTypes)
        {
            if (typeof(T).GetConstructor(argumentTypes) == null) return null;

            return (object[] arguments) => (T)((object)Activator.CreateInstance(typeof(T), arguments));
        }

        private Delegate CreateMethodAccessor(Type type, string name, bool bindInstance = true)
        {
            if (_type == null) return null;

            Type[] methodArgumentTypes = LightupServices.GetMethodArgumentTypes(type, bindInstance);
            MethodInfo method = _type.GetMethod(name, methodArgumentTypes);
            if (method == null) return null;

            return LightupServices.CreateDelegate(type, bindInstance ? GetInstance() : null, method);
        }

        protected T GetMethodAccessor<T>(ref Delegate storage, string name, bool bindInstance = true)
        {
            return (T)((object)GetMethodAccessor(ref storage, typeof(T), name, bindInstance));
        }

        protected Delegate GetMethodAccessor(ref Delegate storage, Type type, string name, bool bindInstance = true)
        {
            if (storage == null)
            {
                var dlg = CreateMethodAccessor(type, name, bindInstance);
                Interlocked.CompareExchange(ref storage, dlg, null);
            }
            if (storage != LightupServices.NotFound) return storage;

            return null;
        }
    }
}
#endif