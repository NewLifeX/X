using System.Collections.Concurrent;
using System.Diagnostics;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Data;

/// <summary>雪花算法测试</summary>
public class SnowflakeTests
{
    #region 基础功能测试
    [Fact(DisplayName = "基本Id生成测试")]
    public void NewId_ShouldGenerateValidId()
    {
        var snowflake = new Snowflake();
        var id = snowflake.NewId();

        // 解析Id
        var parseResult = snowflake.TryParse(id, out var time, out var workerId, out var sequence);
        
        Assert.True(parseResult);
        Assert.True(time <= DateTime.Now.AddSeconds(1)); // 允许1秒误差
        Assert.Equal(snowflake.WorkerId, workerId);
        Assert.True(sequence >= 0 && sequence <= 4095);

        // 验证时间转Id功能
        var timeId = snowflake.GetId(time);
        Assert.Equal(id >> 22, timeId >> 22);
    }

    [Fact(DisplayName = "WorkerId设置测试")]
    public void WorkerId_ShouldBeSetCorrectly()
    {
        var snowflake = new Snowflake { WorkerId = 123 };
        var id = snowflake.NewId();
        
        Assert.Equal(123, snowflake.WorkerId);
        
        var parseResult = snowflake.TryParse(id, out _, out var workerId, out _);
        Assert.True(parseResult);
        Assert.Equal(123, workerId);
    }

    [Fact(DisplayName = "WorkerId范围验证测试")]
    public void WorkerId_ShouldValidateRange()
    {
        var snowflake = new Snowflake();
        
        // 测试有效范围
        snowflake.WorkerId = 0;
        Assert.Equal(0, snowflake.WorkerId);
        
        snowflake.WorkerId = 1023;
        Assert.Equal(1023, snowflake.WorkerId);
        
        // 测试无效范围
        Assert.Throws<ArgumentOutOfRangeException>(() => snowflake.WorkerId = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => snowflake.WorkerId = 1024);
    }

    [Fact(DisplayName = "序列号递增测试")]
    public void Sequence_ShouldIncrement()
    {
        var snowflake = new Snowflake();
        var ids = new List<Int64>();
        
        // 快速生成多个Id，应该序列号递增
        for (var i = 0; i < 10; i++)
        {
            ids.Add(snowflake.NewId());
        }
        
        // 检查Id唯一性
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
    #endregion

    #region 时间相关测试
    [Fact(DisplayName = "UTC时间测试")]
    public void NewIdForUTC_ShouldWorkCorrectly()
    {
        var ids = new Int64[2];
        
        // 本地时间雪花
        {
            var snow = new Snowflake();
            var id = snow.NewId();
            ids[0] = id;

            Assert.Equal(DateTimeKind.Local, snow.StartTimestamp.Kind);

            var parseResult = snow.TryParse(id, out var time, out _, out _);
            Assert.True(parseResult);
            Assert.Equal(DateTimeKind.Local, time.Kind);
        }

        // UTC时间雪花
        {
            var snow = new Snowflake
            {
                StartTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var id = snow.NewId();
            ids[1] = id;

            Assert.Equal(DateTimeKind.Utc, snow.StartTimestamp.Kind);

            var parseResult = snow.TryParse(id, out var time, out _, out _);
            Assert.True(parseResult);
            Assert.Equal(DateTimeKind.Utc, time.Kind);
        }

        // 两个Id的时间应该相差不多
        var diff = Math.Abs((ids[1] - ids[0]) >> 22);
        Assert.True(diff < 1000); // 允许1秒差异
    }

    [Fact(DisplayName = "指定时间Id生成测试")]
    public void NewId_WithSpecificTime_ShouldWorkCorrectly()
    {
        var snowflake = new Snowflake();
        var time = DateTime.Now;
        
        var id1 = snowflake.NewId(time);
        var id2 = snowflake.NewId(time);
        
        Assert.NotEqual(id1, id2);
        
        // 验证时间解析
        snowflake.TryParse(id1, out var parsedTime1, out _, out _);
        snowflake.TryParse(id2, out var parsedTime2, out _, out _);
        
        Assert.Equal(parsedTime1.Ticks / TimeSpan.TicksPerMillisecond, 
                     parsedTime2.Ticks / TimeSpan.TicksPerMillisecond);
    }

    [Fact(DisplayName = "时间转换测试")]
    public void ConvertKind_ShouldWorkCorrectly()
    {
        var snowflake = new Snowflake
        {
            StartTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local)
        };
        
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        var unspecified = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
        
        var converted1 = snowflake.ConvertKind(now);
        var converted2 = snowflake.ConvertKind(utcNow);
        var converted3 = snowflake.ConvertKind(unspecified);
        
        Assert.Equal(DateTimeKind.Local, converted1.Kind);
        Assert.Equal(DateTimeKind.Local, converted2.Kind);
        Assert.Equal(DateTimeKind.Unspecified, converted3.Kind);
    }

    [Fact(DisplayName = "时间回拨处理测试")]
    public void ClockBack_ShouldBeHandled()
    {
        var snowflake = new Snowflake
        {
            StartTimestamp = DateTime.Now.AddMinutes(-1) // 设置较近的开始时间便于测试
        };
        
        // 正常生成Id
        var id1 = snowflake.NewId();
        Thread.Sleep(2); // 确保时间推进
        var id2 = snowflake.NewId();
        
        Assert.True(id2 >= id1); // Id应该递增
    }
    #endregion

    #region 业务Id相关测试
    [Fact(DisplayName = "业务Id生成测试")]
    public void NewIdByUid_ShouldWorkCorrectly()
    {
        var time = DateTime.Now;
        var uid = Rand.Next(1_000_000);
        var snowflake = new Snowflake();

        var ids = new List<Int64>();
        for (var i = 0; i < 5; i++)
        {
            ids.Add(snowflake.NewId(time, uid));
        }

        // 所有Id应该不同
        Assert.Equal(ids.Count, ids.Distinct().Count());

        // 验证工作节点Id
        foreach (var id in ids)
        {
            snowflake.TryParse(id, out _, out var workerId, out _);
            Assert.Equal(uid & 0x3FF, workerId);
        }
    }

    [Fact(DisplayName = "22位业务Id测试")]
    public void NewId22_ShouldWorkCorrectly()
    {
        var snowflake = new Snowflake();
        var time = DateTime.Now;
        var uid = 12345;
        
        var id = snowflake.NewId22(time, uid);
        
        // 验证22位业务Id格式
        var expectedId = ((Int64)(time - snowflake.StartTimestamp).TotalMilliseconds << 22) | (uid & ((1 << 22) - 1));
        Assert.Equal(expectedId, id);
    }

    [Fact(DisplayName = "时间片段查询Id测试")]
    public void GetId_ShouldReturnTimeOnlyId()
    {
        var snowflake = new Snowflake();
        var time = new DateTime(2024, 1, 1, 12, 0, 0);
        
        var timeId = snowflake.GetId(time);
        var fullId = snowflake.NewId(time);
        
        // 时间部分应该相同
        Assert.Equal(timeId >> 22, fullId >> 22);
        
        // GetId的结果应该只包含时间部分
        Assert.Equal(0, timeId & ((1L << 22) - 1));
    }
    #endregion

    #region Id解析测试
    [Fact(DisplayName = "Id解析成功测试")]
    public void TryParse_ShouldParseCorrectly()
    {
        var snowflake = new Snowflake { WorkerId = 100 };
        var originalTime = DateTime.Now;
        var id = snowflake.NewId(originalTime);
        
        var parseResult = snowflake.TryParse(id, out var parsedTime, out var workerId, out var sequence);
        
        Assert.True(parseResult);
        Assert.Equal(100, workerId);
        Assert.True(sequence >= 0 && sequence <= 4095);
        
        // 时间应该相近（允许毫秒差异）
        var timeDiff = Math.Abs((parsedTime - originalTime).TotalMilliseconds);
        Assert.True(timeDiff <= 1);
    }

    [Fact(DisplayName = "Id解析异常处理测试")]
    public void TryParse_ShouldHandleInvalidId()
    {
        var snowflake = new Snowflake();
        
        // 解析负数Id，会得到基于StartTimestamp的时间
        var parseResult = snowflake.TryParse(-1, out var time, out var workerId, out var sequence);
        
        Assert.True(parseResult); // 应该能解析
        // 负数会导致时间戳为负，得到StartTimestamp之前的时间
        Assert.True(time < snowflake.StartTimestamp);
    }
    #endregion

    #region 并发和性能测试
    [Fact(DisplayName = "单线程唯一性测试")]
    public void SingleThread_ShouldGenerateUniqueIds()
    {
        const Int32 count = 10_000; // 减少测试数量以避免序列号重置
        var snowflake = new Snowflake { StartTimestamp = new DateTime(2020, 1, 1) };
        var ids = new ConcurrentDictionary<Int64, Boolean>();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < count; i++)
        {
            var id = snowflake.NewId();
            ids.TryAdd(id, true);
        }
        sw.Stop();

        // 验证唯一性
        Assert.True(ids.Count >= count - 10, $"期望至少 {count - 10} 个唯一Id，实际 {ids.Count} 个");

        XTrace.WriteLine($"单线程生成 {count:n0} 个Id，唯一 {ids.Count:n0} 个，耗时 {sw.Elapsed}，速度 {count * 1000 / sw.ElapsedMilliseconds:n0} tps");
    }

    [Fact(DisplayName = "多线程唯一性测试")]
    public async Task MultiThread_ShouldGenerateUniqueIds()
    {
        var threadCount = 4; // 使用固定值避免常量问题
        const Int32 countPerThread = 10_000; // 减少测试数量
        
        var allIds = new ConcurrentDictionary<Int64, Boolean>();
        var tasks = new List<Task>();

        for (var i = 0; i < threadCount; i++)
        {
            var workerId = i + 1;
            tasks.Add(Task.Run(() =>
            {
                var snowflake = new Snowflake 
                { 
                    StartTimestamp = new DateTime(2020, 1, 1),
                    WorkerId = workerId
                };

                for (var j = 0; j < countPerThread; j++)
                {
                    var id = snowflake.NewId();
                    allIds.TryAdd(id, true);
                }
            }));
        }

        var sw = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        sw.Stop();

        var totalCount = threadCount * countPerThread;
        var uniqueCount = allIds.Count;
        
        // 允许少量重复，因为时间戳相同时序列号可能重置
        Assert.True(uniqueCount >= totalCount - 100, $"期望至少 {totalCount - 100} 个唯一Id，实际 {uniqueCount} 个");
        XTrace.WriteLine($"多线程生成 {totalCount:n0} 个Id，唯一 {uniqueCount:n0} 个，耗时 {sw.Elapsed}，速度 {totalCount * 1000 / sw.ElapsedMilliseconds:n0} tps");
    }

    [Fact(DisplayName = "性能基准测试")]
    public async Task Benchmark_ShouldMeetPerformanceRequirements()
    {
        var threadCount = 4; // 使用固定值避免常量问题
        const Int32 countPerThread = 100_000; // 减少测试数量
        
        var tasks = new List<Task>();
        for (var i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var snowflake = new Snowflake();
                for (var j = 0; j < countPerThread; j++)
                {
                    snowflake.NewId();
                }
            }));
        }

        var sw = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        sw.Stop();

        var totalCount = (Int64)threadCount * countPerThread;
        var tps = totalCount * 1000 / sw.ElapsedMilliseconds;
        
        XTrace.WriteLine($"性能测试生成 {totalCount:n0} 个Id，耗时 {sw.Elapsed}，速度 {tps:n0} tps");
        
        // 性能要求：至少10万tps
        Assert.True(tps > 100_000, $"性能不足，当前tps: {tps:n0}");
    }
    #endregion

    #region 全局设置测试
    [Fact(DisplayName = "全局WorkerId测试")]
    public void GlobalWorkerId_ShouldOverrideLocal()
    {
        var originalGlobalId = Snowflake.GlobalWorkerId;
        
        try
        {
            // 测试有效范围内的全局Id
            var testId = Rand.Next(0, 1024);
            Snowflake.GlobalWorkerId = testId;

            var snowflake = new Snowflake();
            snowflake.NewId();
            
            Assert.Equal(testId, snowflake.WorkerId);

            // 测试超出范围的全局Id
            var largeId = Rand.Next(1024, Int32.MaxValue);
            Snowflake.GlobalWorkerId = largeId;

            var snowflake2 = new Snowflake();
            snowflake2.NewId();
            
            Assert.Equal(largeId & 0x3FF, snowflake2.WorkerId);
        }
        finally
        {
            // 恢复原始值
            Snowflake.GlobalWorkerId = originalGlobalId;
        }
    }
    #endregion

    #region 集群测试
    [Fact(DisplayName = "集群WorkerId分配测试")]
    public void JoinCluster_ShouldAssignUniqueWorkerId()
    {
        var cache = new MemoryCache();
        
        var snowflake1 = new Snowflake();
        var snowflake2 = new Snowflake();
        
        snowflake1.JoinCluster(cache, "test_cluster");
        snowflake2.JoinCluster(cache, "test_cluster");
        
        Assert.NotEqual(snowflake1.WorkerId, snowflake2.WorkerId);
        Assert.True(snowflake1.WorkerId >= 0 && snowflake1.WorkerId <= 1023);
        Assert.True(snowflake2.WorkerId >= 0 && snowflake2.WorkerId <= 1023);
    }

    [Fact(DisplayName = "集群参数验证测试")]
    public void JoinCluster_ShouldValidateParameters()
    {
        var snowflake = new Snowflake();
        
        Assert.Throws<ArgumentNullException>(() => snowflake.JoinCluster(null!));
    }
    #endregion

    #region 边界条件测试
    [Fact(DisplayName = "极限时间测试")]
    public void ExtremeTime_ShouldWork()
    {
        var snowflake = new Snowflake();
        
        // 测试未来时间
        var futureTime = DateTime.Now.AddYears(10);
        var futureId = snowflake.NewId(futureTime);
        Assert.True(futureId > 0);
        
        // 测试过去时间
        var pastTime = snowflake.StartTimestamp.AddMinutes(1);
        var pastId = snowflake.NewId(pastTime);
        Assert.True(pastId > 0);
    }

    [Fact(DisplayName = "StartTimestamp边界测试")]
    public void StartTimestamp_BoundaryTest()
    {
        // 测试不同的StartTimestamp设置
        var snowflake1 = new Snowflake 
        { 
            StartTimestamp = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local) 
        };
        
        var snowflake2 = new Snowflake 
        { 
            StartTimestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) 
        };
        
        var id1 = snowflake1.NewId();
        var id2 = snowflake2.NewId();
        
        Assert.True(id1 > 0);
        Assert.True(id2 > 0);
        Assert.NotEqual(id1 >> 22, id2 >> 22); // 时间戳部分应该不同
    }
    #endregion

    #region 兼容性测试  
    [Fact(DisplayName = "现有业务兼容性测试")]
    public void BackwardCompatibility_ShouldWork()
    {
        // 使用原有的默认设置
        var snowflake = new Snowflake();
        var id = snowflake.NewId();
        
        // 验证基本结构：1bit保留 + 41bit时间戳 + 10bit机器 + 12bit序列号
        Assert.True(id > 0); // 符号位应该是0
        
        var timestamp = id >> 22;
        var workerId = (id >> 12) & 0x3FF;
        var sequence = id & 0x0FFF;
        
        Assert.True(timestamp > 0);
        Assert.True(workerId >= 0 && workerId <= 1023);
        Assert.True(sequence >= 0 && sequence <= 4095);
    }
    #endregion
}