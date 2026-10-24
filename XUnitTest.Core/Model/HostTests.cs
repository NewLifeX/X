using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Model;
using Xunit;

namespace XUnitTest.Model
{
    public class HostTests
    {
        [Fact]
        public async Task TestHost()
        {
            var services = ObjectContainer.Current;

            var cache = MemoryCache.Instance;
            services.AddSingleton<ICache>(cache);

            services.AddTransient<RedisService>();
            services.AddTransient<DbService>();

            var host = services.BuildHost();
            host.Add<MyService>();

            var task = host.RunAsync();
            // 模拟 Wait(3s) 语义：超时不抛异常，继续执行（后续断言自然失败）
            await Task.WhenAny(task, Task.Delay(3_000));

            var host2 = host as Host;
            Assert.NotNull(host2);
            Assert.Single(host2.HostedServices);

            var my = host2.HostedServices[0] as MyService;
            Assert.NotNull(my);
            Assert.Equal(99, my.DbCount);
            Assert.True(my.CacheCount > 0);
        }

        private class MyService : IHostedService
        {
            private readonly DbService _dbService;
            private readonly RedisService _redisService;

            public Int32 DbCount => _dbService.Count;
            public Int32 CacheCount => _redisService.Count;

            public MyService(DbService dbService, RedisService redisService)
            {
                _dbService = dbService;
                _redisService = redisService;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await Task.Yield();

                _dbService.Init();
                _redisService.InitRedis();
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private class DbService
        {
            public Int32 Count { get; set; }

            public void Init()
            {
                // 配置
                var set = NewLife.Setting.Current;
                if (set.IsNew)
                {
                    // 仅模拟“首次运行初始化配置并落盘”，不能改写 DataPath 等决定全局工作目录的字段：
                    // 它们会被保存进 Core.config 并被后续用例（如 SevenZip/TarFile）读取，
                    // 一旦指向输出目录之外，多个目标框架并行运行时会共用同一批临时文件而互相破坏
                    set.Save();
                }

                Count = 99;
            }
        }

        private class RedisService
        {
            private readonly ICache _cache;

            public Int32 Count { get; set; }

            public RedisService(ICache cache) => _cache = cache;

            public void InitRedis()
            {
                _cache.Set("Name", "Stone", 3600);
                Count = _cache.Count;
            }
        }
    }
}
