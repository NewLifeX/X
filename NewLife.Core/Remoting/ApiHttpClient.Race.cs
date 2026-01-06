using System.Collections.Generic;
using NewLife.Http;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Remoting;

public partial class ApiHttpClient
{
    /// <summary>竞速下载文件到本地并校验哈希（可取消）</summary>
    /// <remarks>
    /// 并行请求所有可用服务地址（<see cref="Service.NextTime"/> 小于当前时间）。
    /// - expectedHash 非空：
    ///   - useHeadCheck=true：并行发起 HEAD+下载任务，谁先通过哈希校验谁继续下载，同时取消其它任务；
    ///   - useHeadCheck=false：直接并行 GET 获取响应头，按响应头哈希与 expectedHash 是否匹配选择服务；
    /// - expectedHash 为空：无法做先行校验，直接并行 GET，选取最快返回响应头且无异常的任务继续下载。
    /// 
    /// 被选中的任务会继续读取响应内容并保存到 fileName，其它任务将被取消。
    /// </remarks>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="fileName">目标文件名</param>
    /// <param name="expectedHash">预期哈希字符串，支持带算法前缀或自动识别</param>
    /// <param name="useHeadCheck">是否使用HEAD请求做先行检查。仅当 expectedHash 非空时有效</param>
    /// <param name="cancellationToken">取消通知</param>
    public virtual async Task DownloadFileRaceAsync(String requestUri, String fileName, String? expectedHash, Boolean useHeadCheck = false, CancellationToken cancellationToken = default)
    {
        if (Services.Count == 0) throw new InvalidOperationException("Service address not added!");

        // 埋点
        var action = requestUri.StartsWithIgnoreCase("http://", "https://")
            ? new Uri(requestUri).AbsolutePath.TrimStart('/')
            : requestUri;
        using var span = Tracer?.NewSpan($"race:{action}", expectedHash);

        // 获取可用服务列表，若全部不可用则重置后再获取
        var available = Services.Where(e => e.NextTime < DateTime.Now).ToList();
        if (available.Count == 0)
        {
            foreach (var svc in Services) svc.NextTime = DateTime.MinValue;
            available = Services.ToList();
        }

        // 单节点直接下载
        if (available.Count == 1)
        {
            await DownloadFromServiceAsync(available[0], requestUri, fileName, expectedHash, cancellationToken).ConfigureAwait(false);
            return;
        }

        // 无预期哈希时禁用HEAD检查
        if (expectedHash.IsNullOrEmpty()) useHeadCheck = false;

        try
        {
            if (useHeadCheck)
            {
                // 并行发起 HEAD+下载 任务，谁先通过哈希校验谁继续下载
                await RaceHeadThenDownloadAsync(requestUri, fileName, expectedHash!, available, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // GET 竞速选服务并下载
                await RaceGetAndDownloadAsync(requestUri, fileName, expectedHash, available, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>并行发起 HEAD+下载 任务，谁先通过哈希校验谁继续下载，同时取消其它任务</summary>
    private async Task RaceHeadThenDownloadAsync(String requestUri, String fileName, String expectedHash, IList<Service> services, CancellationToken cancellationToken)
    {
        using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
#if NET46_OR_GREATER || NETSTANDARD || NETCOREAPP
        var winnerTcs = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
#else
        var winnerTcs = new TaskCompletionSource<Boolean>();
#endif

        // 每个服务一个任务：HEAD 检查 -> 通过则取消其它 -> 下载文件
        var tasks = services.Select(svc => HeadCheckThenDownloadAsync(svc, requestUri, fileName, expectedHash, raceCts, winnerTcs, cancellationToken)).ToList();

        try
        {
            // 等待所有任务完成（成功或失败）
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // 检查是否有胜出者
            if (!winnerTcs.Task.IsCompleted || !winnerTcs.Task.Result)
                throw new InvalidOperationException("No available service nodes passed hash check!");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // 如果有胜出者成功完成，忽略其它任务的异常
            if (winnerTcs.Task.IsCompleted && winnerTcs.Task.Result) return;
            throw;
        }
    }

    /// <summary>单个服务的 HEAD 检查 + 下载任务</summary>
    private async Task HeadCheckThenDownloadAsync(Service service, String requestUri, String fileName, String expectedHash, CancellationTokenSource raceCts, TaskCompletionSource<Boolean> winnerTcs, CancellationToken cancellationToken)
    {
        try
        {
            // HEAD 检查
            var client = EnsureClient(service);
            using var headRequest = BuildRequest(HttpMethod.Head, requestUri, null, null);

            var filter = Filter;
            if (filter != null) await filter.OnRequest(client, headRequest, this, raceCts.Token).ConfigureAwait(false);

            using var headResponse = await client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, raceCts.Token).ConfigureAwait(false);

            if (filter != null) await filter.OnResponse(client, headResponse, this, raceCts.Token).ConfigureAwait(false);

            if (!headResponse.IsSuccessStatusCode) return;

            // 检查哈希
            var hash = ExtractHashFromHeaders(headResponse);
            if (hash.IsNullOrEmpty() || !MatchHash(expectedHash, hash)) return;

            // 哈希匹配！尝试成为胜出者
            if (!winnerTcs.TrySetResult(true)) return; // 已有胜出者

            // 取消其它任务
#if NET8_0_OR_GREATER
            await raceCts.CancelAsync().ConfigureAwait(false);
#else
            raceCts.Cancel();
#endif

            // 独自下载文件（使用原始 cancellationToken，不受 raceCts 影响）
            await DownloadFromServiceAsync(service, requestUri, fileName, expectedHash, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (raceCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // 被其它任务取消，正常退出
        }
    }

    /// <summary>GET 竞速选服务并直接下载</summary>
    private async Task RaceGetAndDownloadAsync(String requestUri, String fileName, String? expectedHash, IList<Service> services, CancellationToken cancellationToken)
    {
        using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = services.Select(svc => SendRequestAsync(svc, HttpMethod.Get, requestUri, raceCts.Token)).ToList();

        Service? selectedService = null;
        HttpResponseMessage? selectedResponse = null;

        try
        {
            while (tasks.Count > 0)
            {
                var completed = await Task.WhenAny(tasks).ConfigureAwait(false);
                tasks.Remove(completed);

                var (svc, res, _) = await completed.ConfigureAwait(false);
                if (res == null || !res.IsSuccessStatusCode)
                {
                    res?.Dispose();
                    continue;
                }

                // 无预期哈希，直接选首个成功响应
                if (expectedHash.IsNullOrEmpty())
                {
                    selectedService = svc;
                    selectedResponse = res;
                    break;
                }

                // 有预期哈希，匹配则选中
                var hash = ExtractHashFromHeaders(res);
                if (!hash.IsNullOrEmpty() && MatchHash(expectedHash, hash))
                {
                    selectedService = svc;
                    selectedResponse = res;
                    break;
                }

                // 暂存首个成功响应作为兜底
                if (selectedResponse == null)
                {
                    selectedService = svc;
                    selectedResponse = res;
                }
                else
                {
                    res.Dispose();
                }
            }

            if (selectedService == null || selectedResponse == null)
                throw new InvalidOperationException("No available service nodes!");

            // 取消其它任务
#if NET8_0_OR_GREATER
            await raceCts.CancelAsync().ConfigureAwait(false);
#else
            raceCts.Cancel();
#endif

            // 下载文件
            await SaveResponseToFileAsync(selectedService, selectedResponse, fileName, expectedHash, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // 清理未选中的响应
            foreach (var task in tasks)
            {
                try { (await task.ConfigureAwait(false)).Response?.Dispose(); } catch { }
            }
        }
    }

    /// <summary>发送请求并返回响应</summary>
    private async Task<(Service Service, HttpResponseMessage? Response, Exception? Error)> SendRequestAsync(Service service, HttpMethod method, String requestUri, CancellationToken cancellationToken)
    {
        try
        {
            var client = EnsureClient(service);
            using var request = BuildRequest(method, requestUri, null, null);

            var filter = Filter;
            if (filter != null) await filter.OnRequest(client, request, this, cancellationToken).ConfigureAwait(false);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (filter != null) await filter.OnResponse(client, response, this, cancellationToken).ConfigureAwait(false);

            return (service, response, null);
        }
        catch (Exception ex)
        {
            return (service, null, ex);
        }
    }

    /// <summary>从指定服务下载文件</summary>
    private async Task DownloadFromServiceAsync(Service service, String requestUri, String fileName, String? expectedHash, CancellationToken cancellationToken)
    {
        _currentService = service;
        Source = service.Name;

        var client = EnsureClient(service);
        using var request = BuildRequest(HttpMethod.Get, requestUri, null, null);

        var filter = Filter;
        try
        {
            if (filter != null) await filter.OnRequest(client, request, this, cancellationToken).ConfigureAwait(false);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (filter != null) await filter.OnResponse(client, response, this, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

#if NET5_0_OR_GREATER
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

            await HttpHelper.SaveFileAsync(stream, fileName, expectedHash, cancellationToken).ConfigureAwait(false);
            Current = service;
        }
        catch (Exception ex)
        {
            if (filter != null) await filter.OnError(client, ex, this, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>保存响应内容到文件</summary>
    private async Task SaveResponseToFileAsync(Service service, HttpResponseMessage response, String fileName, String? expectedHash, CancellationToken cancellationToken)
    {
        try
        {
            _currentService = service;
            Source = service.Name;

#if NET5_0_OR_GREATER
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

            await HttpHelper.SaveFileAsync(stream, fileName, expectedHash, cancellationToken).ConfigureAwait(false);
            Current = service;
        }
        finally
        {
            response.Dispose();
        }
    }

    /// <summary>确保服务有可用的 HttpClient</summary>
    private HttpClient EnsureClient(Service service)
    {
        var client = service.Client;
        if (client == null)
        {
            if (service.CreateTime.Year < 2000) Log?.Debug("使用[{0}]：{1}", service.Name, service.Address);

            client = CreateClient();
            client.BaseAddress = service.Address;
            if (!service.Token.IsNullOrEmpty()) Token = service.Token;

            service.Client = client;
            service.CreateTime = DateTime.Now;
        }

        if (client.BaseAddress == null) client.BaseAddress = service.Address;
        return client;
    }

    /// <summary>从响应头提取哈希值</summary>
    private static String? ExtractHashFromHeaders(HttpResponseMessage response)
    {
        var headers = response.Headers;
        var contentHeaders = response.Content.Headers;

        // RFC3230 Digest: algorithm=hashValue
        if (headers.TryGetValues("Digest", out var digestValues))
        {
            var v = digestValues.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!v.IsNullOrEmpty())
            {
                var p = v.IndexOf('=');
                if (p > 0) return $"{v[..p]}${v[(p + 1)..]}";

                //return $"md5${v}";
            }
        }

        // X-File-Hash: algorithm:hashValue
        if (headers.TryGetValues("X-File-Hash", out var xfhValues))
        {
            var v = xfhValues.FirstOrDefault();
            if (!v.IsNullOrEmpty())
            {
                var p = v.IndexOf(':');
                if (p > 0) return $"{v[..p]}${v[(p + 1)..]}";

                //return $"md5${v}";
            }
        }

        // X-Content-MD5 / Content-MD5
        if (headers.TryGetValues("X-Content-MD5", out var md5Values) || contentHeaders.TryGetValues("Content-MD5", out md5Values))
        {
            var v = md5Values.FirstOrDefault()?.Trim().Trim('"');
            if (!v.IsNullOrEmpty()) return v.Contains('$') ? v : $"md5${v}";
        }
        if (headers.TryGetValues("X-Content-SHA256", out var sha256Values) || contentHeaders.TryGetValues("Content-SHA256", out sha256Values))
        {
            var v = sha256Values.FirstOrDefault()?.Trim().Trim('"');
            if (!v.IsNullOrEmpty()) return v.Contains('$') ? v : $"sha256${v}";
        }

        // ETag
        var etag = headers.ETag?.Tag?.Trim().Trim('"');
        if (!etag.IsNullOrEmpty()) return etag.Contains('$') ? etag : $"{InferAlgorithm(etag)}${etag}";

        return null;
    }

    /// <summary>匹配哈希值</summary>
    private static Boolean MatchHash(String expectedHash, String actualHash)
    {
        if (expectedHash.IsNullOrEmpty() || actualHash.IsNullOrEmpty()) return false;

        // 统一分隔符
        expectedHash = expectedHash.Replace(':', '$');
        actualHash = actualHash.Replace(':', '$');

        var p1 = expectedHash.IndexOf('$');
        var p2 = actualHash.IndexOf('$');

        var expAlg = p1 > 0 ? expectedHash[..p1] : InferAlgorithm(expectedHash);
        var expHash = (p1 > 0 ? expectedHash[(p1 + 1)..] : expectedHash).Trim('"');

        var actAlg = p2 > 0 ? actualHash[..p2] : InferAlgorithm(actualHash);
        var actHash = (p2 > 0 ? actualHash[(p2 + 1)..] : actualHash).Trim('"');

        return expAlg.EqualIgnoreCase(actAlg) && expHash.EqualIgnoreCase(actHash);
    }

    /// <summary>根据哈希长度推断算法</summary>
    private static String InferAlgorithm(String hash)
    {
        var len = hash?.Trim().Trim('"').Length ?? 0;
        return len switch
        {
            8 => "crc32",
            16 or 32 => "md5",
            40 => "sha1",
            64 => "sha256",
            128 => "sha512",
            _ => "md5"
        };
    }

    /// <summary>竞速调用，并行请求所有可用服务地址，选取最快成功返回的结果</summary>
    /// <remarks>
    /// 并行请求所有可用服务地址（<see cref="Service.NextTime"/> 小于当前时间）。
    /// 选取最快成功返回响应且状态码正常的任务，读取并解析结果，同时取消其它任务。
    /// </remarks>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="method">请求方法</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task<TResult?> InvokeRaceAsync<TResult>(HttpMethod method, String action, Object? args = null, CancellationToken cancellationToken = default)
    {
        if (Services.Count == 0) throw new InvalidOperationException("Service address not added!");

        var returnType = typeof(TResult);

        // 埋点
        using var span = Tracer?.NewSpan($"race:{action}", args);

        // 获取可用服务列表，若全部不可用则重置后再获取
        var available = Services.Where(e => e.NextTime < DateTime.Now).ToList();
        if (available.Count == 0)
        {
            foreach (var svc in Services) svc.NextTime = DateTime.MinValue;
            available = Services.ToList();
        }

        // 单节点直接调用
        if (available.Count == 1)
        {
            return await InvokeOnServiceAsync<TResult>(available[0], method, action, args, returnType, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await RaceInvokeAsync<TResult>(method, action, args, returnType, available, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>竞速调用，并行请求所有可用服务地址，选取最快成功返回的结果</summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public Task<TResult?> InvokeRaceAsync<TResult>(String action, Object? args = null, CancellationToken cancellationToken = default)
    {
        var method = HttpMethod.Post;
#if NETCOREAPP || NETSTANDARD2_1
        if (args == null || args.GetType().IsBaseType() || action.StartsWithIgnoreCase("Get") || action.Contains("/get", StringComparison.OrdinalIgnoreCase))
            method = HttpMethod.Get;
#else
        if (args == null || args.GetType().IsBaseType() || action.StartsWithIgnoreCase("Get") || action.IndexOf("/get", StringComparison.OrdinalIgnoreCase) >= 0)
            method = HttpMethod.Get;
#endif

        return InvokeRaceAsync<TResult>(method, action, args, cancellationToken);
    }

    /// <summary>GET 竞速调用</summary>
    private async Task<TResult?> RaceInvokeAsync<TResult>(HttpMethod method, String action, Object? args, Type returnType, IList<Service> services, CancellationToken cancellationToken)
    {
        using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = services.Select(svc => SendInvokeRequestAsync(svc, method, action, args, returnType, raceCts.Token)).ToList();

        Service? selectedService = null;
        HttpResponseMessage? selectedResponse = null;

        try
        {
            while (tasks.Count > 0)
            {
                var completed = await Task.WhenAny(tasks).ConfigureAwait(false);
                tasks.Remove(completed);

                var (svc, res, _) = await completed.ConfigureAwait(false);
                if (res == null || !res.IsSuccessStatusCode)
                {
                    res?.Dispose();
                    continue;
                }

                // 选取首个成功响应
                selectedService = svc;
                selectedResponse = res;
                break;
            }

            if (selectedService == null || selectedResponse == null)
                throw new InvalidOperationException("No available service nodes!");

            // 取消其它任务
#if NET8_0_OR_GREATER
            await raceCts.CancelAsync().ConfigureAwait(false);
#else
            raceCts.Cancel();
#endif

            // 处理响应
            return await ProcessInvokeResponseAsync<TResult>(selectedService, selectedResponse, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // 清理未选中的响应
            foreach (var task in tasks)
            {
                try { (await task.ConfigureAwait(false)).Response?.Dispose(); } catch { }
            }
        }
    }

    /// <summary>发送调用请求并返回响应</summary>
    private async Task<(Service Service, HttpResponseMessage? Response, Exception? Error)> SendInvokeRequestAsync(Service service, HttpMethod method, String action, Object? args, Type returnType, CancellationToken cancellationToken)
    {
        try
        {
            var client = EnsureClient(service);
            var request = BuildRequest(method, action, args, returnType);

            var filter = Filter;
            if (filter != null) await filter.OnRequest(client, request, this, cancellationToken).ConfigureAwait(false);

            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (filter != null) await filter.OnResponse(client, response, this, cancellationToken).ConfigureAwait(false);

            return (service, response, null);
        }
        catch (Exception ex)
        {
            return (service, null, ex);
        }
    }

    /// <summary>在指定服务上执行调用</summary>
    private async Task<TResult?> InvokeOnServiceAsync<TResult>(Service service, HttpMethod method, String action, Object? args, Type returnType, CancellationToken cancellationToken)
    {
        _currentService = service;
        Source = service.Name;

        var client = EnsureClient(service);
        var request = BuildRequest(method, action, args, returnType);

        var filter = Filter;
        try
        {
            if (filter != null) await filter.OnRequest(client, request, this, cancellationToken).ConfigureAwait(false);

            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (filter != null) await filter.OnResponse(client, response, this, cancellationToken).ConfigureAwait(false);

            var jsonHost = JsonHost ?? ServiceProvider?.GetService<IJsonHost>() ?? JsonHelper.Default;
            var result = await ApiHelper.ProcessResponse<TResult>(response, CodeName, DataName, jsonHost).ConfigureAwait(false);

            Current = service;
            return result;
        }
        catch (Exception ex)
        {
            if (filter != null) await filter.OnError(client, ex, this, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>处理调用响应</summary>
    private async Task<TResult?> ProcessInvokeResponseAsync<TResult>(Service service, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            _currentService = service;
            Source = service.Name;

            var jsonHost = JsonHost ?? ServiceProvider?.GetService<IJsonHost>() ?? JsonHelper.Default;
            var result = await ApiHelper.ProcessResponse<TResult>(response, CodeName, DataName, jsonHost).ConfigureAwait(false);

            Current = service;
            return result;
        }
        finally
        {
            response.Dispose();
        }
    }
}
