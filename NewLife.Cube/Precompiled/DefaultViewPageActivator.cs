using System;
using System.Web.Mvc;
namespace RazorGenerator.Mvc
{
	internal class DefaultViewPageActivator : IViewPageActivator
	{
		private static class Nested
		{
			internal static readonly DefaultViewPageActivator Instance;
			static Nested()
			{
				DefaultViewPageActivator.Nested.Instance = new DefaultViewPageActivator();
			}
		}
		private readonly Func<IDependencyResolver> _resolverThunk;
		public static DefaultViewPageActivator Current
		{
			get
			{
				return DefaultViewPageActivator.Nested.Instance;
			}
		}
		public DefaultViewPageActivator() : this(null)
		{
		}
		public DefaultViewPageActivator(IDependencyResolver resolver)
		{
			if (resolver == null)
			{
				this._resolverThunk = (() => DependencyResolver.Current);
			}
			else
			{
				this._resolverThunk = (() => resolver);
			}
		}
		public object Create(ControllerContext controllerContext, Type type)
		{
			return this._resolverThunk().GetService(type) ?? Activator.CreateInstance(type);
		}
	}
}
