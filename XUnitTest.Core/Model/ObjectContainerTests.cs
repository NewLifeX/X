﻿using System;
using System.Collections.Generic;
using NewLife.Caching;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Remoting;
using Xunit;

namespace XUnitTest.Model
{
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
            ioc.Add(new ObjectMap { ServiceType = typeof(Redis) });
            ioc.TryAdd(new ObjectMap { ServiceType = typeof(Redis) });

            Assert.Equal(1, ioc.Count);

            var services = ioc.GetValue("_list") as IList<IObject>;
            Assert.Equal(1, services.Count);
            Assert.Equal(typeof(Redis), services[0].ServiceType);
            Assert.Null(services[0].ImplementationType);
            Assert.Equal(ObjectLifetime.Singleton, services[0].Lifttime);
        }

        [Fact]
        public void Register()
        {
            var ioc = new ObjectContainer();
            ioc.Register(typeof(Redis), null, null);
            ioc.Register(typeof(ICache), typeof(Redis), null);

            Assert.Equal(2, ioc.Count);
        }

        [Fact]
        public void Resolve()
        {
            var ioc = new ObjectContainer();
            ioc.Register(typeof(Redis), null, null);
            ioc.Register(typeof(ICache), typeof(Redis), null);

            var mc = ioc.Resolve(typeof(MemoryCache));
            Assert.Null(mc);

            var rds = ioc.Resolve(typeof(Redis));
            Assert.NotNull(rds);
            var rds2 = ioc.Resolve(typeof(Redis));
            Assert.NotEqual(rds, rds2);

            var cache = ioc.Resolve(typeof(ICache));
            Assert.NotNull(cache);
        }

        [Fact]
        public void AddSingleton()
        {
            var ioc = new ObjectContainer();
            var services = ioc.GetValue("_list") as IList<IObject>;

            ioc.AddSingleton(typeof(ICache), typeof(Redis));
            Assert.Equal(1, ioc.Count);
            Assert.Equal(1, services.Count);
            Assert.True(ioc.Resolve<ICache>() is Redis);

            ioc.AddSingleton<ICache, MemoryCache>();
            Assert.Equal(1, ioc.Count);
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            ioc.AddSingleton(typeof(ICache), p => new Redis());
            Assert.True(ioc.Resolve<ICache>() is Redis);

            ioc.AddSingleton<ICache>(p => new MemoryCache());
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            ioc.AddSingleton(typeof(ICache), new Redis());
            Assert.True(ioc.Resolve<ICache>() is Redis);

            ioc.TryAddSingleton(typeof(ICache), typeof(MemoryCache));
            Assert.True(ioc.Resolve<ICache>() is Redis);

            Assert.Equal(1, services.Count);
            Assert.Equal(ObjectLifetime.Singleton, services[0].Lifttime);
        }

        [Fact]
        public void AddTransient()
        {
            var ioc = new ObjectContainer();
            var services = ioc.GetValue("_list") as IList<IObject>;

            ioc.AddTransient(typeof(ICache), typeof(Redis));
            Assert.Equal(1, ioc.Count);
            Assert.Equal(1, services.Count);
            Assert.True(ioc.Resolve<ICache>() is Redis);

            ioc.AddTransient<ICache, MemoryCache>();
            Assert.Equal(1, ioc.Count);
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            ioc.AddTransient(typeof(ICache), p => new Redis());
            Assert.True(ioc.Resolve<ICache>() is Redis);

            ioc.AddTransient<ICache>(p => new MemoryCache());
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            ioc.TryAddTransient(typeof(ICache), typeof(Redis));
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            Assert.Equal(1, services.Count);
            Assert.Equal(ObjectLifetime.Transient, services[0].Lifttime);
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
                ioc.AddSingleton<ICache, Redis>();
                ioc.AddTransient<MyService>();

                var svc = ioc.Resolve<MyService>();
                Assert.Equal(1, svc.Kind);
            }

            {
                var ioc = new ObjectContainer();
                ioc.AddSingleton<Redis>();
                ioc.AddTransient<MyService>();

                var svc = ioc.Resolve<MyService>();
                Assert.Equal(2, svc.Kind);
            }

            {
                var ioc = new ObjectContainer();
                ioc.AddSingleton<ICache, Redis>();
                ioc.AddTransient<MyService>();

                var svc = ioc.Resolve<MyService>();
                Assert.Equal(1, svc.Kind);
            }

            {
                var ioc = new ObjectContainer();
                ioc.AddSingleton<ICache, Redis>();
                ioc.AddTransient<MyService>();

                var svc = ioc.Resolve<MyService>();
                Assert.Equal(1, svc.Kind);
            }

            {
                var ioc = new ObjectContainer();
                ioc.AddSingleton<ICache, Redis>();
                ioc.AddSingleton<ILog>(XTrace.Log);
                ioc.AddTransient<MyService>();

                var svc = ioc.Resolve<MyService>();
                Assert.Equal(3, svc.Kind);
            }
        }

        private class MyService
        {
            public Int32 Kind { get; set; }

            public MyService() => Kind = 1;

            public MyService(Redis redis) => Kind = 2;

            public MyService(ICache cache, ILog log) => Kind = 3;
        }

        [Fact]
        public void AddApiHttpClient()
        {
            var ioc = new ObjectContainer();

            var config = new ConfigProvider();
            config["orderService"] = "3*http://127.0.0.1:1234,5*http://10.0.0.1:1234";
            ioc.AddSingleton<IConfigProvider>(config);
            ioc.AddSingleton<IApiClient>(provider => new ApiHttpClient(provider, "orderService"));

            var prv = ioc.BuildServiceProvider();

            var client = prv.GetService<IApiClient>() as ApiHttpClient;
            var ss = client.Services;
            Assert.Equal(2, ss.Count);
            Assert.Equal(3, ss[0].Weight);
            Assert.Equal("http://127.0.0.1:1234/", ss[0].Address + "");
            Assert.Equal(5, ss[1].Weight);
            Assert.Equal("http://10.0.0.1:1234/", ss[1].Address + "");

            // 改变无关配置，不影响对象属性
            config["orderRedis"] = "server=10.0.0.1:6379;password=word;db=13";
            config.SaveAll();
            Assert.Equal(ss, client.Services);

            // 改变配置数据，影响对象属性
            config["orderService"] = "3*http://127.0.0.1:1234,7*http://192.168.0.1:1234,5*http://10.0.0.1:1234";
            config.SaveAll();

            Assert.NotEqual(ss, client.Services);
            ss = client.Services;
            Assert.Equal(3, ss.Count);
            Assert.Equal(3, ss[0].Weight);
            Assert.Equal("http://127.0.0.1:1234/", ss[0].Address + "");
            Assert.Equal(7, ss[1].Weight);
            Assert.Equal("http://192.168.0.1:1234/", ss[1].Address + "");
            Assert.Equal(5, ss[2].Weight);
            Assert.Equal("http://10.0.0.1:1234/", ss[2].Address + "");
        }
    }
}