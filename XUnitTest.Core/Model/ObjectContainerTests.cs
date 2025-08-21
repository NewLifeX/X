using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Model;

public class ObjectContainerTests
{
    [Fact]
    public void Current()
    {
        var ioc = ObjectContainer.Current;
        var provider = ObjectContainer.Provider;

        var ioc2 = provider.GetValue("Container") as IObjectContainer;
        Assert.NotNull(ioc2);

        Assert.Equal(ioc, ioc2);
    }

    [Fact]
    public void Add()
    {
        var ioc = new ObjectContainer();
        ioc.Add(new ServiceDescriptor(typeof(MemoryCache)));
        ioc.TryAdd(new ServiceDescriptor(typeof(MemoryCache)));

        Assert.Equal(1, ioc.Count);

        var services = ioc.GetValue("_list") as IList<IObject>;
        Assert.Single(services);
        Assert.Equal(typeof(MemoryCache), services[0].ServiceType);
        Assert.Null(services[0].ImplementationType);
        Assert.Equal(ObjectLifetime.Singleton, services[0].Lifetime);
    }

    [Fact]
    public void Register()
    {
        var ioc = new ObjectContainer();
        ioc.Register(typeof(MemoryCache), null, null);
        ioc.Register(typeof(ICache), typeof(MemoryCache), null);

        Assert.Equal(2, ioc.Count);
    }

    [Fact]
    public void GetService()
    {
        var ioc = new ObjectContainer();
        ioc.Register(typeof(MemoryCache), null, null);
        ioc.Register(typeof(ICache), typeof(MemoryCache), null);

        var mc = ioc.GetService(typeof(MemoryCache));
        Assert.NotNull(mc);

        //var rds = ioc.Resolve(typeof(Redis));
        //Assert.NotNull(rds);
        //var rds2 = ioc.Resolve(typeof(Redis));
        //Assert.NotEqual(rds, rds2);

        var cache = ioc.GetService(typeof(ICache));
        Assert.NotNull(cache);
    }

    [Fact]
    public void ResolveOrder()
    {
        var services = ObjectContainer.Current;
        services.AddSingleton<ICache, MemoryCache>();
        services.AddSingleton<ICache, MyCache>();

        var provider = services.BuildServiceProvider();
        var cache = provider.GetService<ICache>();
        Assert.Equal(typeof(MyCache), cache.GetType());

        var cs = provider.GetServices<ICache>().ToArray();
        Assert.Equal(2, cs.Length);
        Assert.Equal(typeof(MyCache), cs[0].GetType());
        Assert.Equal(typeof(MemoryCache), cs[1].GetType());
    }

    class MyCache : MemoryCache { }

    [Fact]
    public void AddSingleton()
    {
        var ioc = new ObjectContainer();
        var services = ioc.Services;

        ioc.AddSingleton<ICache, MemoryCache>();
        Assert.Equal(1, ioc.Count);
        Assert.True(ioc.GetService<ICache>() is MemoryCache);

        ioc.AddSingleton<ICache>(p => new MemoryCache());
        Assert.True(ioc.GetService<ICache>() is MemoryCache);

        Assert.Equal(2, services.Count);
        Assert.Equal(ObjectLifetime.Singleton, services[0].Lifetime);

        var serviceProvider = ioc.BuildServiceProvider();
        var obj = serviceProvider.GetService<ICache>();
        Assert.True(obj is MemoryCache);

        var obj2 = serviceProvider.GetService<ICache>();
        Assert.Equal(obj, obj2);

        var objs = serviceProvider.GetServices<ICache>().ToList();
        Assert.Equal(2, objs.Count);
        Assert.Equal(obj, objs[0]);
        Assert.NotEqual(obj, objs[1]);
        Assert.NotEqual(objs[0], objs[1]);
    }

    [Fact]
    public void AddScoped()
    {
        var ioc = new ObjectContainer();
        var services = ioc.Services;

        ioc.AddScoped<ICache, MemoryCache>();
        Assert.Equal(1, ioc.Count);
        Assert.True(ioc.GetService<ICache>() is MemoryCache);

        ioc.AddScoped<ICache>(p => new MemoryCache());
        Assert.True(ioc.GetService<ICache>() is MemoryCache);

        Assert.Equal(2, services.Count);
        Assert.Equal(ObjectLifetime.Scoped, services[0].Lifetime);

        var root = ioc.BuildServiceProvider();
        {
            var serviceProvider = root;
            var obj = serviceProvider.GetService<ICache>();
            Assert.True(obj is MemoryCache);

            var obj2 = serviceProvider.GetService<ICache>();
            Assert.NotEqual(obj, obj2);

            var objs = serviceProvider.GetServices<ICache>().ToList();
            Assert.Equal(2, objs.Count);
            Assert.NotEqual(obj, objs[0]);
            Assert.NotEqual(obj, objs[1]);
            Assert.NotEqual(objs[0], objs[1]);
        }

        {
            using var scope = root.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var obj = serviceProvider.GetService<ICache>();
            Assert.True(obj is MemoryCache);

            var instance = root.GetService<ICache>();
            Assert.NotEqual(obj, instance);

            var obj2 = serviceProvider.GetService<ICache>();
            Assert.Equal(obj, obj2);
            Assert.NotEqual(obj2, instance);

            var objs = serviceProvider.GetServices<ICache>().ToList();
            Assert.Equal(2, objs.Count);
            Assert.NotEqual(obj, objs[0]);
            Assert.NotEqual(obj, objs[1]);
            Assert.NotEqual(objs[0], objs[1]);
        }

        {
            var serviceProvider = root;
            var obj = serviceProvider.GetService<ICache>();
            Assert.True(obj is MemoryCache);

            var obj2 = serviceProvider.GetService<ICache>();
            Assert.NotEqual(obj, obj2);

            var objs = serviceProvider.GetServices<ICache>().ToList();
            Assert.Equal(2, objs.Count);
            Assert.NotEqual(obj, objs[0]);
            Assert.NotEqual(obj, objs[1]);
            Assert.NotEqual(objs[0], objs[1]);
        }
    }

    [Fact]
    public void AddTransient()
    {
        var ioc = new ObjectContainer();
        var services = ioc.Services;

        ioc.AddTransient<ICache, MemoryCache>();
        Assert.Equal(1, ioc.Count);
        Assert.True(ioc.GetService<ICache>() is MemoryCache);

        ioc.AddTransient<ICache>(p => new MemoryCache());
        Assert.True(ioc.GetService<ICache>() is MemoryCache);

        Assert.Equal(2, services.Count);
        Assert.Equal(ObjectLifetime.Transient, services[0].Lifetime);

        var serviceProvider = ioc.BuildServiceProvider();
        var obj = serviceProvider.GetService<ICache>();
        Assert.True(obj is MemoryCache);

        var obj2 = serviceProvider.GetService<ICache>();
        Assert.NotEqual(obj, obj2);

        var objs = serviceProvider.GetServices<ICache>().ToList();
        Assert.Equal(2, objs.Count);
        Assert.NotEqual(obj, objs[0]);
        Assert.NotEqual(obj, objs[1]);
        Assert.NotEqual(objs[0], objs[1]);
    }

    [Fact]
    public void BuildServiceProvider()
    {
        var ioc = new ObjectContainer();

        ioc.AddTransient<ICache, MemoryCache>();

        var provider = ioc.BuildServiceProvider();

        var cache = provider.GetService(typeof(ICache));
        var cache2 = provider.GetService(typeof(ICache));
        Assert.NotNull(cache);
        Assert.NotNull(cache2);
        Assert.NotEqual(cache, cache2);
    }

    [Fact]
    public void TestMutilConstructor()
    {
        {
            var ioc = new ObjectContainer();
            ioc.AddSingleton<ICache, MemoryCache>();
            ioc.AddTransient<MyService>();

            var svc = ioc.GetService<MyService>();
            Assert.Equal(1, svc.Kind);
        }

        {
            var ioc = new ObjectContainer();
            ioc.AddSingleton<MemoryCache>();
            ioc.AddTransient<MyService>();

            var svc = ioc.GetService<MyService>();
            Assert.Equal(2, svc.Kind);
        }

        {
            var ioc = new ObjectContainer();
            ioc.AddSingleton<ICache, MemoryCache>();
            ioc.AddTransient<MyService>();

            var svc = ioc.GetService<MyService>();
            Assert.Equal(1, svc.Kind);
        }

        {
            var ioc = new ObjectContainer();
            ioc.AddSingleton<ICache, MemoryCache>();
            ioc.AddTransient<MyService>();

            var svc = ioc.GetService<MyService>();
            Assert.Equal(1, svc.Kind);
        }

        {
            var ioc = new ObjectContainer();
            ioc.AddSingleton<ICache, MemoryCache>();
            ioc.AddSingleton<ILog>(XTrace.Log);
            ioc.AddTransient<MyService>();

            var svc = ioc.GetService<MyService>();
            Assert.Equal(3, svc.Kind);
        }
    }

    private class MyService
    {
        public Int32 Kind { get; set; }

        public MyService() => Kind = 1;

        public MyService(MemoryCache redis) => Kind = 2;

        public MyService(ICache cache, ILog log) => Kind = 3;
    }

    //[Fact]
    //public void AddApiHttpClient()
    //{
    //    var ioc = new ObjectContainer();

    //    var config = new ConfigProvider();
    //    config["orderService"] = "3*http://127.0.0.1:1234,5*http://10.0.0.1:1234";
    //    ioc.AddSingleton<IConfigProvider>(config);
    //    ioc.AddSingleton<IApiClient>(provider => new ApiHttpClient(provider, "orderService"));

    //    var prv = ioc.BuildServiceProvider();

    //    var client = prv.GetService<IApiClient>() as ApiHttpClient;
    //    var ss = client.Services;
    //    Assert.Equal(2, ss.Count);
    //    Assert.Equal(3, ss[0].Weight);
    //    Assert.Equal("http://127.0.0.1:1234/", ss[0].Address + "");
    //    Assert.Equal(5, ss[1].Weight);
    //    Assert.Equal("http://10.0.0.1:1234/", ss[1].Address + "");

    //    // 改变无关配置，不影响对象属性
    //    config["orderRedis"] = "server=10.0.0.1:6379;password=word;db=13";
    //    config.SaveAll();
    //    Assert.Equal(ss, client.Services);

    //    // 改变配置数据，影响对象属性
    //    config["orderService"] = "3*http://127.0.0.1:1234,7*http://192.168.0.1:1234,5*http://10.0.0.1:1234";
    //    config.SaveAll();

    //    Assert.NotEqual(ss, client.Services);
    //    ss = client.Services;
    //    Assert.Equal(3, ss.Count);
    //    Assert.Equal(3, ss[0].Weight);
    //    Assert.Equal("http://127.0.0.1:1234/", ss[0].Address + "");
    //    Assert.Equal(7, ss[1].Weight);
    //    Assert.Equal("http://192.168.0.1:1234/", ss[1].Address + "");
    //    Assert.Equal(5, ss[2].Weight);
    //    Assert.Equal("http://10.0.0.1:1234/", ss[2].Address + "");
    //}
}