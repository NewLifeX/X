using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Design;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife.Model
{
    /// <summary>
    /// 按名称检索程序集或类型
    /// </summary>
    public class TypeResolutionService : ITypeResolutionService
    {
        #region 构造
        IServiceProvider _provider;
        ITypeResolutionService _baseservice;

        /// <summary>
        /// 实例化
        /// </summary>
        public TypeResolutionService() { }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="baseservice"></param>
        public TypeResolutionService(IServiceProvider provider, ITypeResolutionService baseservice)
        {
            _provider = provider;
            _baseservice = baseservice;
        }
        #endregion

        #region 方法
        //void DoAction(Action<ITypeResolutionService> action)
        //{
        //    // 从上一个服务中找
        //    if (_baseservice != null) action(_baseservice);

        //    // 从服务提供者中找
        //    if (_provider != null)
        //    {
        //        ITypeResolutionService service = ServiceProvider.GetService<ITypeResolutionService>(_provider);
        //        if (service != null) action(service);
        //    }
        //}

        //TResult DoAction<TResult>(Func<ITypeResolutionService, TResult> func)
        //{
        //    // 从上一个服务中找
        //    if (_baseservice != null)
        //    {
        //        TResult rs = func(_baseservice);
        //        if (!Object.Equals(rs, default(TResult))) return rs;
        //    }

        //    // 从服务提供者中找
        //    if (_provider != null)
        //    {
        //        ITypeResolutionService service = ServiceProvider.GetService<ITypeResolutionService>(_provider);
        //        if (service != null)
        //        {
        //            TResult rs = func(service);
        //            if (!Object.Equals(rs, default(TResult))) return rs;
        //        }
        //    }

        //    return default(TResult);
        //}
        #endregion

        #region ITypeResolutionService 成员
        /// <summary>
        /// 获取请求的程序集
        /// </summary>
        /// <param name="name"></param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        public virtual Assembly GetAssembly(AssemblyName name, bool throwOnError)
        {
            // 从上一个服务中找
            ITypeResolutionService service = _baseservice;
            if (service != null)
            {
                Assembly asm = service.GetAssembly(name, throwOnError);
                if (asm != null) return asm;
            }
            // 从服务提供者中找
            if (_provider != null)
            {
                service = ServiceProvider.GetService<ITypeResolutionService>(_provider);
                if (service != null)
                {
                    Assembly asm = service.GetAssembly(name, throwOnError);
                    if (asm != null) return asm;
                }
            }

            //Assembly asm = DoAction<Assembly>(item => item.GetAssembly(name, throwOnError));

            // 反射
            if (throwOnError)
                return Assembly.Load(name);
            else
            {
                try
                {
                    return Assembly.Load(name);
                }
                catch { return null; }
            }
        }

        /// <summary>
        /// 获取请求的程序集
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Assembly GetAssembly(AssemblyName name)
        {
            return GetAssembly(name, false);
        }

        /// <summary>
        /// 获取从中加载程序集的文件的路径
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual string GetPathOfAssembly(AssemblyName name)
        {
            // 从上一个服务中找
            ITypeResolutionService service = _baseservice;
            if (service != null)
            {
                String path = service.GetPathOfAssembly(name);
                if (path != null) return path;
            }
            // 从服务提供者中找
            if (_provider != null)
            {
                service = ServiceProvider.GetService<ITypeResolutionService>(_provider);
                if (service != null)
                {
                    String path = service.GetPathOfAssembly(name);
                    if (path != null) return path;
                }
            }

            List<AssemblyX> list = AssemblyX.GetAssemblies();
            if (list != null)
            {
                foreach (AssemblyX item in list)
                {
                    if (item.Asm.GetName() == name) return item.Asm.Location;
                }
            }
            list = AssemblyX.ReflectionOnlyGetAssemblies();
            if (list != null)
            {
                foreach (AssemblyX item in list)
                {
                    if (item.Asm.GetName() == name) return item.Asm.Location;
                }
            }
            return null;
        }

        /// <summary>
        /// 用指定的名称加载类型
        /// </summary>
        /// <param name="name"></param>
        /// <param name="throwOnError"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public virtual Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            // 从上一个服务中找
            ITypeResolutionService service = _baseservice;
            if (service != null)
            {
                Type type = service.GetType(name, throwOnError, ignoreCase);
                if (type != null) return type;
            }
            // 从服务提供者中找
            if (_provider != null)
            {
                service = ServiceProvider.GetService<ITypeResolutionService>(_provider);
                if (service != null)
                {
                    Type type = service.GetType(name, throwOnError, ignoreCase);
                    if (type != null) return type;
                }
            }

            if (throwOnError) return TypeX.GetType(name);

            try
            {
                return TypeX.GetType(name);
            }
            catch { return null; }
        }

        /// <summary>
        /// 用指定的名称加载类型
        /// </summary>
        /// <param name="name"></param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        public Type GetType(string name, bool throwOnError)
        {
            return GetType(name, throwOnError, false);
        }

        /// <summary>
        /// 用指定的名称加载类型
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Type GetType(string name)
        {
            return GetType(name, false, false);
        }

        /// <summary>
        /// 将引用添加到指定程序集中
        /// </summary>
        /// <param name="name"></param>
        public virtual void ReferenceAssembly(AssemblyName name)
        {
            // 从上一个服务中找
            ITypeResolutionService service = _baseservice;
            if (service != null) service.ReferenceAssembly(name);
            // 从服务提供者中找
            if (_provider != null)
            {
                service = ServiceProvider.GetService<ITypeResolutionService>(_provider);
                if (service != null) service.ReferenceAssembly(name);
            }
        }
        #endregion
    }
}