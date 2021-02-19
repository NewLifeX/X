using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Model;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Model
{
    public class HostTests
    {
        [Fact]
        public void TestHost()
        {
            var services = ObjectContainer.Current;

            var redis = new Redis();
            redis.Init("server=127.0.0.1:6379;db=3");
            services.AddSingleton(redis);

            services.AddTransient<RedisService>();
            services.AddTransient<DbService>();

            var host = services.BuildHost();
            host.Add<MyService>();

            //host.Run();
            var task = host.RunAsync();
            task.Wait(1_000);

            var host2 = host as Host;
            Assert.Equal(1, host2.Services.Count);

            var my = host2.Services[0] as MyService;
            Assert.NotNull(my);
            Assert.True(my.DbCount >= 10);
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
                    set.DataPath = "../Data";
                    set.Save();
                }

                //todo 初始化数据库
                Count = Rand.Next(10, 99);
            }
        }

        private class RedisService
        {
            private readonly Redis _redis;

            public Int32 Count { get; set; }

            public RedisService(Redis redis) => _redis = redis;

            public void InitRedis()
            {
                _redis.Set("Name", "Stone", 3600);
                Count = _redis.Count;
            }
        }
    }
}
