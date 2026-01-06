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
    ///   - useHeadCheck=true：并行发起 HEAD 请求竞速，谁先通过哈希校验谁被选中继续下载，同时取消其它任务；
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
        // 获取可用服务列表
        var available = Services.Where(e => e.NextTime < DateTime.Now).ToList();
        if (available.Count == 0) throw new XException("No available service nodes!");

        // 单节点直接下载
        if (available.Count == 1)
        {
            await DownloadFileAsync(requestUri, fileName, expectedHash, cancellationToken).ConfigureAwait(false);
            return;
        }

        // 无预期哈希时禁用HEAD检查
        if (expectedHash.IsNullOrEmpty()) useHeadCheck = false;

        // 埋点
        using var span = Tracer?.NewSpan(requestUri, expectedHash);

        using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var method = useHeadCheck ? HttpMethod.Head : HttpMethod.Get;
        var tasks = available.Select(svc => SendRaceRequestAsync(svc, method, requestUri, null, null, raceCts.Token)).ToList();

        Service? selectedService = null;
        HttpResponseMessage? selectedResponse = null;

        try
        {
            while (tasks.Count > 0)
            {
                var completed = await Task.WhenAny(tasks).ConfigureAwait(false);
                tasks.Remove(completed);

                var (svc, res, _) = await completed.ConfigureAwait(false);
                if (res != null && res.IsSuccessStatusCode)
                {
                    // 无预期哈希，直接选首个成功响应
                    if (expectedHash.IsNullOrEmpty())
                    {
                        selectedService = svc;
                        selectedResponse = res;
                        break;
                    }

                    // 有预期哈希，匹配则选中；不匹配则丢弃，不作兜底
                    if (MatchHashFromHeaders(res, expectedHash))
                    {
                        selectedService = svc;
                        selectedResponse = res;
                        break;
                    }
                }

                res?.Dispose();
            }

            if (selectedService == null || selectedResponse == null)
                throw new InvalidOperationException("No available service nodes!");

            // 取消其它任务，不等待清理
            raceCts.Cancel();

            // HEAD 模式需要重新发起 GET 下载，GET 模式直接使用已有响应
            var response = selectedResponse;
            if (useHeadCheck)
            {
                selectedResponse.Dispose();

                var client = EnsureClient(selectedService);
                using var request = BuildRequest(HttpMethod.Get, requestUri, null, null);

                response = await SendOnServiceAsync(request, selectedService, client, false, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }

            _currentService = selectedService;
            Source = selectedService.Name;

#if NET5_0_OR_GREATER
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

            await HttpHelper.SaveFileAsync(stream, fileName, expectedHash, cancellationToken).ConfigureAwait(false);
            Current = selectedService;

            response.Dispose();
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
        finally
        {
            // 异步清理未选中的响应，不等待
            _ = CleanupTasksAsync(tasks);
        }
    }

    /// <summary>发送竞速请求并返回响应</summary>
    private async Task<(Service Service, HttpResponseMessage? Response, Exception? Error)> SendRaceRequestAsync(Service service, HttpMethod method, String action, Object? args, Type? returnType, CancellationToken cancellationToken)
    {
        try
        {
            var client = EnsureClient(service);
            using var request = BuildRequest(method, action, args, returnType);

            var response = await SendOnServiceAsync(request, service, client, true, cancellationToken).ConfigureAwait(false);
            return (service, response, null);
        }
        catch (Exception ex)
        {
            return (service, null, ex);
        }
    }

    /// <summary>异步清理任务列表中的响应</summary>
    private static async Task CleanupTasksAsync(IList<Task<(Service Service, HttpResponseMessage? Response, Exception? Error)>> tasks)
    {
        foreach (var task in tasks)
        {
            try { (await task.ConfigureAwait(false)).Response?.Dispose(); } catch { }
        }
    }

    /// <summary>从响应头提取哈希并与预期哈希匹配</summary>
    private static Boolean MatchHashFromHeaders(HttpResponseMessage response, String expectedHash)
    {
        if (expectedHash.IsNullOrEmpty()) return false;

        // 统一预期哈希格式：算法$哈希值
        var (expAlg, expHash) = ParseHash(expectedHash);
        if (expHash.IsNullOrEmpty()) return false;

        var headers = response.Headers;
        var contentHeaders = response.Content.Headers;

        // RFC3230 Digest: algorithm=hashValue
        if (headers.TryGetValues("Digest", out var digestValues))
        {
            var v = digestValues.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (TryMatchHash(v, '=', expAlg, expHash, null)) return true;
        }

        // X-File-Hash: algorithm:hashValue
        if (headers.TryGetValues("X-File-Hash", out var xfhValues))
        {
            if (TryMatchHash(xfhValues.FirstOrDefault(), ':', expAlg, expHash, null)) return true;
        }

        // X-Content-MD5 / Content-MD5
        if (headers.TryGetValues("X-Content-MD5", out var md5Values) || contentHeaders.TryGetValues("Content-MD5", out md5Values))
        {
            if (TryMatchHash(md5Values.FirstOrDefault(), '$', expAlg, expHash, "md5")) return true;
        }

        // X-Content-SHA256 / Content-SHA256
        if (headers.TryGetValues("X-Content-SHA256", out var sha256Values) || contentHeaders.TryGetValues("Content-SHA256", out sha256Values))
        {
            if (TryMatchHash(sha256Values.FirstOrDefault(), '$', expAlg, expHash, "sha256")) return true;
        }

        // ETag
        var etag = headers.ETag?.Tag?.Trim().Trim('"');
        if (!etag.IsNullOrEmpty())
        {
            var p = etag.IndexOf('$');
            var actAlg = p > 0 ? etag[..p] : InferAlgorithm(etag);
            var actHash = (p > 0 ? etag[(p + 1)..] : etag).Trim('"');
            if (actAlg.EqualIgnoreCase(expAlg) && actHash.EqualIgnoreCase(expHash)) return true;
        }

        return false;
    }

    /// <summary>解析哈希字符串为算法和哈希值</summary>
    private static (String Algorithm, String Hash) ParseHash(String hash)
    {
        if (hash.IsNullOrEmpty()) return ("", "");

        hash = hash.Replace(':', '$');
        var p = hash.IndexOf('$');
        var alg = p > 0 ? hash[..p] : InferAlgorithm(hash);
        var val = (p > 0 ? hash[(p + 1)..] : hash).Trim('"');
        return (alg, val);
    }

    /// <summary>尝试匹配哈希值</summary>
    private static Boolean TryMatchHash(String? value, Char separator, String expAlg, String expHash, String? defaultAlg)
    {
        if (value.IsNullOrEmpty()) return false;

        value = value.Trim().Trim('"');
        var p = value.IndexOf(separator);
        var actAlg = p > 0 ? value[..p] : (defaultAlg ?? InferAlgorithm(value));
        var actHash = (p > 0 ? value[(p + 1)..] : value).Trim('"');

        return actAlg.EqualIgnoreCase(expAlg) && actHash.EqualIgnoreCase(expHash);
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
    /// 若全部服务地址被屏蔽（NextTime 大于当前时间），则抛出异常。
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
        // 获取可用服务列表
        var available = Services.Where(e => e.NextTime < DateTime.Now).ToList();
        if (available.Count == 0) throw new XException("No available service nodes!");

        // 单节点直接调用
        if (available.Count == 1) return await InvokeAsync<TResult>(method, action, args, null, cancellationToken).ConfigureAwait(false);

        var returnType = typeof(TResult);

        // 埋点
        using var span = Tracer?.NewSpan(action, args);

        using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = available.Select(svc => SendRaceRequestAsync(svc, method, action, args, returnType, raceCts.Token)).ToList();

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

            // 取消其它任务，不等待
            raceCts.Cancel();

            // 处理响应
            _currentService = selectedService;
            Source = selectedService.Name;

            var jsonHost = JsonHost ?? ServiceProvider?.GetService<IJsonHost>() ?? JsonHelper.Default;
            var result = await ApiHelper.ProcessResponse<TResult>(selectedResponse, CodeName, DataName, jsonHost).ConfigureAwait(false);

            Current = selectedService;
            return result;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
        finally
        {
            // 异步清理未选中的响应，不等待
            _ = CleanupTasksAsync(tasks);
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
}
