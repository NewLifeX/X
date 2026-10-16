using System.IO;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.IO;
using NewLife.Log;
using NewLife.Remoting;
using Xunit;

namespace XUnitTest.IO;

/// <summary>EasyClient 对象存储契约测试</summary>
/// <remarks>
/// <para>本地化契约测试，不连接任何外部服务。由 <see cref="EasyIoStub"/> 在动态端口上复刻 EasyIO 服务端
/// （NewLife.EasyIO/Controllers/IOController + ApiControllerBase）的线上契约：</para>
/// <list type="bullet">
/// <item><description>路由 /io/{action}，按 PUT/GET/DELETE 区分动作；</description></item>
/// <item><description>GET/DELETE 参数走查询串，PUT 原始字节走请求体；Get 命中文件时返回原始字节流（FileContentResult 不经包装）；</description></item>
/// <item><description>其余业务响应与异常统一为 HTTP 200 + {code,data,traceId} 包装（ObjectResult 经 ActionFilter 包装，异常以 code=500 包装）。</description></item>
/// </list>
/// </remarks>
[Collection("IO")]
public class EasyClientTests
{
    private static readonly Byte[] _data = "学无先后达者为师".GetBytes();

    #region Put/Get 往返
    [Fact(DisplayName = "上传下载往返，内容与元数据一致")]
    public async Task PutAndGetRoundTrip()
    {
        using var stub = new EasyIoStub();
        var client = stub.CreateClient();

        var before = DateTime.Now.AddMinutes(-1);
        var inf = await client.PutAsync("aa.txt", (ArrayPacket)_data);
        var after = DateTime.Now.AddMinutes(1);

        Assert.NotNull(inf);
        Assert.Equal("aa.txt", inf.Name);
        Assert.Equal(_data.Length, inf.Length);
        Assert.False(inf.IsDirectory);
        Assert.InRange(inf.Time, before, after);
        // 客户端把上传内容回填到 Data
        Assert.Equal(_data, inf.Data.ReadBytes());

        var inf2 = await client.GetAsync("aa.txt");
        Assert.NotNull(inf2);
        Assert.Equal("aa.txt", inf2.Name);
        Assert.Equal(_data.Length, inf2.Length);
        Assert.Equal(_data, inf2.Data.ReadBytes());

        // 子目录 + 中文名称：验证标识在 Url 编码/解码链路上往返无损
        var name = "子目录/文档.txt";
        inf = await client.PutAsync(name, (ArrayPacket)_data);
        Assert.NotNull(inf);
        Assert.Equal(name, inf.Name);
        Assert.Equal(_data.Length, inf.Length);

        inf2 = await client.GetAsync(name);
        Assert.NotNull(inf2);
        Assert.Equal(name, inf2.Name);
        Assert.Equal(_data, inf2.Data.ReadBytes());
    }

    [Fact(DisplayName = "获取不存在的对象：返回非 null（服务端包装文本被当作内容）")]
    public async Task GetMissingReturnsWrappedError()
    {
        using var stub = new EasyIoStub();
        var client = stub.CreateClient();

        var inf = await client.GetAsync("missing.txt");

        // 现行契约：服务端异常以 HTTP 200 + {code:500,data:"文件不存在"} 包装返回，
        // 客户端对 IPacket 返回值按原始字节透传、不解析包装，故返回非 null，内容为错误 Json 原文。
        // 用例锁定这一互操作现状：将来服务端改用非 200 状态码、或客户端开始解析包装时，本用例会失败提醒同步修订。
        Assert.NotNull(inf);
        Assert.Equal("missing.txt", inf.Name);
        Assert.NotNull(inf.Data);
        Assert.Contains("文件不存在", inf.Data.ToStr());
    }
    #endregion

    #region 参数校验
    [Fact(DisplayName = "客户端参数校验：空标识与空数组抛出 ArgumentNullException")]
    public async Task ClientArgumentsValidate()
    {
        var client = new EasyClient { Server = "http://127.0.0.1:1" };

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.PutAsync(null!, (ArrayPacket)_data));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.PutAsync("", (ArrayPacket)_data));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAsync(""));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetUrlAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.ExistsAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAsync((String)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAsync((String[])null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteAsync([]));
    }

    [Fact(DisplayName = "GetUrl：服务端未实现时抛出 ApiException(500)")]
    public async Task GetUrlNotImplemented()
    {
        using var stub = new EasyIoStub();
        var client = stub.CreateClient();
        await client.PutAsync("file.txt", (ArrayPacket)_data);

        // 服务端 GetUrl 抛 NotImplementedException，经 ApiControllerBase 包装为 code=500 的业务错误
        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetUrlAsync("file.txt"));
        Assert.Equal(500, ex.Code);

        ex = await Assert.ThrowsAsync<ApiException>(() => client.GetUrlAsync("missing.txt"));
        Assert.Equal(500, ex.Code);
        Assert.Contains("文件不存在", ex.Message);
    }
    #endregion

    #region Exists
    [Fact(DisplayName = "Exists 直接判定：服务端实现该接口时返回布尔值")]
    public async Task ExistsWithEndpoint()
    {
        using var stub = new EasyIoStub(true);
        var client = stub.CreateClient();

        await client.PutAsync("file.txt", (ArrayPacket)_data);

        Assert.True(await client.ExistsAsync("file.txt"));
        Assert.False(await client.ExistsAsync("missing.txt"));
    }

    [Fact(DisplayName = "Exists 回退分支：服务端无该接口时以 Get 探测")]
    public async Task ExistsFallbackWithoutEndpoint()
    {
        // 不注册 /io/Exists，与现行 EasyIO 服务端一致（IOController 无 Exists 动作）
        using var stub = new EasyIoStub();
        var client = stub.CreateClient();

        await client.PutAsync("file.txt", (ArrayPacket)_data);

        // 服务端 404 → 客户端 catch 后回退 Get：文件存在即 true
        Assert.True(await client.ExistsAsync("file.txt"));

        // 文件不存在时，回退的 Get 同样返回非 null（同 GetMissingReturnsWrappedError 的包装透传），
        // 因此当前实现下也返回 true。锁定现状，服务端补上 Exists 接口后应改走直接判定。
        Assert.True(await client.ExistsAsync("missing.txt"));
    }
    #endregion

    #region Delete
    [Fact(DisplayName = "删除返回数量：文件、目录与不存在")]
    public async Task DeleteReturnsCount()
    {
        using var stub = new EasyIoStub();
        var client = stub.CreateClient();

        await client.PutAsync("del/1.txt", (ArrayPacket)_data);
        await client.PutAsync("del/2.txt", (ArrayPacket)_data);

        Assert.Equal(1, await client.DeleteAsync("del/1.txt"));
        Assert.Equal(0, await client.DeleteAsync("del/1.txt"));
        Assert.Equal(0, await client.DeleteAsync("del/missing.txt"));

        // 标识以斜杠结尾时走目录删除分支；非空目录删除失败，先清空文件
        Assert.Equal(1, await client.DeleteAsync("del/2.txt"));
        Assert.Equal(1, await client.DeleteAsync("del/"));
        Assert.Equal(0, await client.DeleteAsync("del/"));

        // 批量删除为逐项求和
        await client.PutAsync("b1.txt", (ArrayPacket)_data);
        Assert.Equal(1, await client.DeleteAsync(["b1.txt", "b0.txt"]));
    }
    #endregion

    #region Search
    [Fact(DisplayName = "搜索：通配模式过滤与 start/count 切片")]
    public async Task SearchPatternAndSlice()
    {
        using var stub = new EasyIoStub();
        var client = await CreateSearchFixtureAsync(stub);

        var names = new[] { "test/1.txt", "test/2.txt", "test/3.txt" };

        // 目录 test 下共 3 个 txt（aa.txt 在根目录、aa.log 后缀不同），take=2 截断
        var infs = await client.SearchAsync("test/*.txt", 0, 2);
        Assert.NotNull(infs);
        Assert.Equal(2, infs.Count);
        Assert.All(infs, e => Assert.Contains(e.Name, names));

        // skip=1 后剩余 2 个；文件系统枚举顺序不稳定，切片断言只校验数量与集合归属
        infs = await client.SearchAsync("test/*.txt", 1, 4);
        Assert.NotNull(infs);
        Assert.Equal(2, infs.Count);
        Assert.All(infs, e => Assert.Contains(e.Name, names));

        // skip=2 后剩余 1 个
        infs = await client.SearchAsync("test/*.txt", 2, 4);
        Assert.NotNull(infs);
        Assert.Single(infs);
        Assert.Contains(infs[0].Name, names);

        // 后缀过滤：只取 log，根目录文件不越界
        infs = await client.SearchAsync("test/*.log", 0, 100);
        Assert.NotNull(infs);
        Assert.Single(infs);
        Assert.Equal("test/aa.log", infs[0].Name);
    }

    [Fact(DisplayName = "搜索：目录条目优先，start 在目录与文件间累计")]
    public async Task SearchListsDirectoriesFirst()
    {
        using var stub = new EasyIoStub();
        var client = await CreateSearchFixtureAsync(stub);
        await client.PutAsync("test/sub/leaf.txt", (ArrayPacket)_data);

        var infs = await client.SearchAsync("test", 0, 100);
        Assert.NotNull(infs);

        // 服务端先列子目录再列文件；目录名不带尾部斜杠，路径统一换成 /
        Assert.Equal("test/sub", infs[0].Name);

        // EasyIO 契约：列完目录后 start 增加目录个数，文件侧再跳过同等数量，
        // 这里 1 个子目录 + 4 个文件 → 1 个目录 + 3 个文件。
        // 文件系统枚举顺序不稳定，被跳过的具体文件不确定，只断言数量与集合归属
        var names = new[] { "test/aa.log", "test/1.txt", "test/2.txt", "test/3.txt" };
        Assert.Equal(4, infs.Count);
        Assert.All(infs.Skip(1), e => Assert.Contains(e.Name, names));
        Assert.Equal(3, infs.Skip(1).Select(e => e.Name).Distinct().Count());
    }

    [Fact(DisplayName = "搜索：根目录默认参数与不存在的目录")]
    public async Task SearchDefaultAndMissingDirectory()
    {
        using var stub = new EasyIoStub();
        var client = await CreateSearchFixtureAsync(stub);

        // 无参搜索等价于根目录：1 个子目录 + 1 个文件；按 start 累计语义文件被跳过，仅返回目录
        var infs = await client.SearchAsync();
        Assert.NotNull(infs);
        Assert.Single(infs);
        Assert.Equal("test", infs[0].Name);

        // 目录不存在：服务端返回 data=null，客户端返回 null 而非空列表
        var infs2 = await client.SearchAsync("nope", 0, 10);
        Assert.Null(infs2);
    }

    private static async Task<EasyClient> CreateSearchFixtureAsync(EasyIoStub stub)
    {
        var client = stub.CreateClient();

        await client.PutAsync("aa.txt", (ArrayPacket)_data);
        await client.PutAsync("test/aa.log", (ArrayPacket)_data);
        for (var i = 1; i <= 3; i++)
        {
            await client.PutAsync($"test/{i}.txt", (ArrayPacket)_data);
        }

        return client;
    }
    #endregion

    #region 契约桩
    /// <summary>EasyIO 服务端契约桩</summary>
    /// <remarks>
    /// 用 NewLife.Http 的 HttpServer 在动态端口上复刻 EasyIO 服务端的必要行为：
    /// 查询串参数、PUT 原始字节体；业务结果与异常统一 200 + {code,data,traceId} 包装；Get 命中文件返回原始字节流。
    /// 存储根目录为实例独占的临时目录，随实例释放。
    /// </remarks>
    private sealed class EasyIoStub : IDisposable
    {
        private readonly HttpServer _server;

        /// <summary>存储根目录</summary>
        public String Root { get; }

        /// <param name="enableExists">是否注册 /io/Exists 接口。默认 false，与现行 EasyIO 服务端一致</param>
        public EasyIoStub(Boolean enableExists = false)
        {
            Root = Path.Combine(Path.GetTempPath(), "nl_easyio_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(Root);

            // 单协议 HTTP，动态端口由系统分配
            _server = new HttpServer { Port = 0, Log = XTrace.Log };
            _server.MapPut("/io/Put", Put);
            _server.MapGet("/io/Get", Get);
            _server.MapGet("/io/GetUrl", GetUrl);
            _server.MapDelete("/io/Delete", Delete);
            _server.MapGet("/io/Search", Search);
            if (enableExists) _server.MapGet("/io/Exists", Exists);
            _server.Start();
        }

        /// <summary>创建指向本桩的客户端</summary>
        public EasyClient CreateClient() => new() { Server = $"http://127.0.0.1:{_server.Port}" };

        public void Dispose()
        {
            _server.TryDispose();

            // 清理失败不影响用例结论
            try { Root.AsDirectory().Delete(true); } catch { }
        }

        private String GetPath(String id) => Path.GetFullPath(Path.Combine(Root, id));

        private static void Ok(IHttpContext context, Object? data) => context.WriteJson(new { code = 0, data, traceId = (String?)null });

        private static void Error(IHttpContext context, Int32 code, String message) => context.WriteJson(new { code, data = message, traceId = (String?)null });

        private void Put(IHttpContext context)
        {
            var id = context.Parameters["id"] + "";
            if (id.IsNullOrEmpty())
            {
                Error(context, 500, new ArgumentNullException(nameof(id)).Message);
                return;
            }

            var fileName = GetPath(id);
            var dir = Path.GetDirectoryName(fileName);
            if (!dir.IsNullOrEmpty()) Directory.CreateDirectory(dir);

            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                var body = context.Request.Body;
                if (body != null)
                {
                    var buf = body.ReadBytes();
                    fs.Write(buf, 0, buf.Length);
                }
                fs.SetLength(fs.Length);
            }

            var fi = new FileInfo(fileName);
            Ok(context, new { name = id, length = fi.Length, time = fi.LastWriteTime, IsDirectory = false });
        }

        private void Get(IHttpContext context)
        {
            var id = context.Parameters["id"] + "";
            if (id.IsNullOrEmpty())
            {
                Error(context, 500, "找不到记录！id=" + id);
                return;
            }

            var fileName = GetPath(id);
            if (!File.Exists(fileName))
            {
                Error(context, 500, "文件不存在");
                return;
            }

            // 对应 FileContentResult：原始字节流，不经 {code,data} 包装
            context.Response.ContentType = "application/octet-stream";
            context.Response.Body = (ArrayPacket)File.ReadAllBytes(fileName);
        }

        private void GetUrl(IHttpContext context)
        {
            var id = context.Parameters["id"] + "";
            if (id.IsNullOrEmpty())
            {
                Error(context, 500, "找不到记录！id=" + id);
                return;
            }

            var fileName = GetPath(id);
            if (!File.Exists(fileName))
            {
                Error(context, 500, "文件不存在");
                return;
            }

            // 与 EasyIO 服务端一致：todo 未实现，异常被包装为 code=500
            Error(context, 500, new NotImplementedException().Message);
        }

        private void Delete(IHttpContext context)
        {
            var id = context.Parameters["id"] + "";
            if (id.IsNullOrEmpty())
            {
                Error(context, 500, "找不到记录！id=" + id);
                return;
            }

            var path = GetPath(id);
            Int32 count;
            if (path.EndsWith('/') || path.EndsWith('\\'))
            {
                var di = new DirectoryInfo(path);
                if (!di.Exists)
                {
                    Ok(context, 0);
                    return;
                }

                di.Delete();
                count = 1;
            }
            else
            {
                var fi = new FileInfo(path);
                if (!fi.Exists)
                {
                    Ok(context, 0);
                    return;
                }

                fi.Delete();
                count = 1;
            }

            Ok(context, count);
        }

        private void Exists(IHttpContext context)
        {
            var id = context.Parameters["id"] + "";
            if (id.IsNullOrEmpty())
            {
                Error(context, 500, "找不到记录！id=" + id);
                return;
            }

            Ok(context, File.Exists(GetPath(id)));
        }

        private void Search(IHttpContext context)
        {
            var pattern = context.Parameters["pattern"] + "";
            var start = (context.Parameters["start"] + "").ToInt();
            var count = (context.Parameters["count"] + "").ToInt();

            if (count <= 0) count = 100;
            if (start <= 0) start = 0;

            var dir = "";
            var pt = "*";
            if (!pattern.IsNullOrEmpty())
            {
                var p = pattern.LastIndexOfAny(['/', '\\']);
                if (p >= 0 && pattern[(p + 1)..].Contains('*'))
                {
                    dir = pattern[..p];
                    pt = pattern[(p + 1)..];
                }
                else
                {
                    dir = pattern;
                }
            }

            var di = new DirectoryInfo(GetPath(dir));
            if (!di.Exists)
            {
                Ok(context, null);
                return;
            }

            var rs = new List<Object>();

            // 子目录列表
            var dis = di.GetDirectories(pt);
            if (dis.Length > 0)
            {
                foreach (var item in dis.Skip(start).Take(count))
                {
                    rs.Add(new { name = Relative(item.FullName), time = item.LastWriteTime });
                }
                start += dis.Length;
                count -= rs.Count;
            }
            if (count == 0)
            {
                Ok(context, rs);
                return;
            }

            // 文件列表
            var fis = di.GetFiles(pt);
            if (fis.Length > 0)
            {
                foreach (var item in fis.Skip(start).Take(count))
                {
                    rs.Add(new { name = Relative(item.FullName), time = item.LastWriteTime });
                }
            }

            Ok(context, rs);
        }

        private String Relative(String fullName) => Path.GetRelativePath(Root, fullName).Replace('\\', '/');
    }
    #endregion
}
