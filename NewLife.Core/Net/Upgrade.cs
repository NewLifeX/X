using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using NewLife.Http;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;

namespace NewLife.Net;

/// <summary>升级更新</summary>
/// <remarks>
/// 优先比较版本Version，再比较时间Time。
/// 自动更新的难点在于覆盖正在使用的exe/dll文件，通过改名可以解决。
/// </remarks>
public class Upgrade
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>服务器地址</summary>
    public String Server { get; set; }

    /// <summary>版本</summary>
    public Version? Version { get; set; }

    /// <summary>本地编译时间</summary>
    public DateTime Time { get; set; }

    /// <summary>更新目录</summary>
    public String UpdatePath { get; set; } = "Update";

    /// <summary>目标目录</summary>
    public String DestinationPath { get; set; } = ".";

    /// <summary>超链接信息</summary>
    public Link? Link { get; set; }

    /// <summary>缓存文件。同名文件不再下载，默认false</summary>
    public Boolean CacheFile { get; set; } = true;

    /// <summary>更新源文件</summary>
    public String? SourceFile { get; set; }

    /// <summary>下载超时时间（毫秒）。默认30000，超时按失败处理</summary>
    public Int32 DownloadTimeout { get; set; } = 30_000;
    #endregion

    #region 构造
    /// <summary>实例化一个升级对象实例，获取当前应用信息</summary>
    public Upgrade()
    {
        Name = nameof(Upgrade);

        var asm = Assembly.GetEntryAssembly();
        if (asm != null)
        {
            var name = asm.GetName();
            if (name != null)
            {
                Version = name.Version;
                Name = name.Name ?? nameof(Upgrade);
            }

            var ax = AssemblyX.Create(asm);
            if (ax != null) Time = ax.Compile;
        }

        Server = NewLife.Setting.Current.PluginServer;
    }
    #endregion

    #region 方法
    /// <summary>获取版本信息，检查是否需要更新</summary>
    /// <returns></returns>
    public Boolean Check()
    {
        // 删除备份文件
        DeleteBackup(DestinationPath);

        Exception? lastError = null;
        foreach (var url in Server.Split(',', ';'))
        {
            if (url.IsNullOrEmpty()) continue;

            WriteLog("Checking resource package {0}", url);
            try
            {
                var web = CreateClient();
                var html = web.GetString(url);
                var links = Link.Parse(html, url, item => !item.Name.IsNullOrEmpty() && item.Name.ToLower().Contains(Name.ToLower()));
                if (links == null || links.Length == 0)
                {
                    WriteLog("Resource package not found");
                    continue;
                }

                // 先比较版本
                if (Version > new Version(0, 0))
                {
                    var link = links.OrderByDescending(e => e.Version).FirstOrDefault()!;
                    if (link.Version > Version)
                    {
                        Link = link;
                        WriteLog("Online version [{0}] is newer: {1}>{2}", link.FullName, link.Version, Version);

                        return true;
                    }
                    else
                        WriteLog("Online version [{0}] is older: {1}<={2}", link.FullName, link.Version, Version);
                }
                // 再比较时间
                else
                {
                    var link = links.OrderByDescending(e => e.Time).FirstOrDefault()!;
                    // 只有文件时间大于编译时间才更新，需要考虑文件编译后过一段时间才打包
                    if (link.Time > Time.AddMinutes(10))
                    {
                        Link = link;
                        WriteLog("Online version [{0}] is newer: {1}>{2}", link.FullName, link.Time, Time);

                        return true;
                    }
                    else
                        WriteLog("Online version [{0}] is older: {1}<={2}", link.FullName, link.Time, Time);
                }

                // 本地址没有较新版本，继续检查下一个地址，多镜像之间互为备份
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        // 所有地址都失败时至少留下一条日志，避免升级检查静默失效、无从排查
        if (lastError != null) WriteLog("Update check failed: {0}", lastError.Message);

        return false;
    }

    /// <summary>开始更新</summary>
    public void Download()
    {
        var link = Link ?? throw new Exception(L.T("No new version available!"));
        if (link.Url.IsNullOrEmpty()) throw new Exception(L.T("The upgrade package address is invalid!"));

        //Download(link.Url, link.FullName);

        var file = !link.FullName.IsNullOrEmpty() ? UpdatePath.CombinePath(link.FullName).GetBasePath() : Path.GetTempFileName();

        WaitDownload(ct => DownloadAsync(link.Url, file, link.Hash, ct), link.Url);
    }

    /// <summary>等待下载任务，超时按失败处理（取消后台任务并抛超时）</summary>
    private void WaitDownload(Func<CancellationToken, Task> download, String url)
    {
        var timeout = DownloadTimeout > 0 ? DownloadTimeout : 30_000;

        using var cts = new CancellationTokenSource();
        var task = Task.Factory.StartNew(() => download(cts.Token), TaskCreationOptions.LongRunning).Unwrap();

        if (!task.Wait(timeout))
        {
            // 超时按失败处理：取消后台下载并抛超时，避免未完成的文件被当成下载成功（日志误报“下载完成”）
            cts.Cancel();
            // 后台任务将因取消而异常结束，挂观察器避免未观测任务异常
            _ = task.ContinueWith(t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

            throw new TimeoutException(L.T("Download timeout [{0}ms]: {1}", timeout, url));
        }
    }

    /// <summary>开始更新</summary>
    /// <param name="url">下载源</param>
    /// <param name="fileName">文件名</param>
    public void Download(String url, String? fileName)
    {
        // 如果更新包不存在，则下载
        var file = !fileName.IsNullOrEmpty() ?
            UpdatePath.CombinePath(fileName).GetBasePath() :
            Path.GetTempFileName();
        if (!CacheFile && File.Exists(file)) File.Delete(file); ;
        if (!File.Exists(file))
        {
            WriteLog("Preparing to download {0} to {1}", url, file);

            var sw = Stopwatch.StartNew();

            var web = CreateClient();
            WaitDownload(ct => web.DownloadFileAsync(url, file, ct), url);

            sw.Stop();
            WriteLog("Download completed. Size: {0:n0} bytes, elapsed: {1:n0} ms", file.AsFile().Length, sw.ElapsedMilliseconds);
        }

        SourceFile = file;
    }

    /// <summary>开始更新</summary>
    /// <param name="url">下载源</param>
    /// <param name="fileName">文件名</param>
    /// <param name="expectedHash">预期哈希字符串，支持带算法前缀或自动识别</param>
    /// <param name="cancellationToken">取消通知</param>
    public async Task DownloadAsync(String url, String fileName, String? expectedHash, CancellationToken cancellationToken)
    {
        // 如果更新包不存在，则下载
        var file = !fileName.IsNullOrEmpty() ?
            UpdatePath.CombinePath(fileName).GetBasePath() :
            Path.GetTempFileName();
        if (!CacheFile && File.Exists(file)) File.Delete(file); ;
        if (!File.Exists(file))
        {
            WriteLog("Preparing to download {0} to {1}", url, file);

            var sw = Stopwatch.StartNew();

            var web = CreateClient();
            await web.DownloadFileAsync(url, file, expectedHash, cancellationToken).ConfigureAwait(false);

            sw.Stop();
            WriteLog("Download completed. Size: {0:n0} bytes, elapsed: {1:n0} ms", file.AsFile().Length, sw.ElapsedMilliseconds);
        }

        SourceFile = file;
    }

    /// <summary>检查并执行更新操作</summary>
    public Boolean Update()
    {
        // 删除备份文件
        DeleteBackup(DestinationPath);

        var file = SourceFile;

        if (file.IsNullOrEmpty() || !File.Exists(file)) return false;

        WriteLog("Update package found: {0}", file);

        // 解压更新程序包
        if (!file.EndsWithIgnoreCase(".zip", ".7z")) return false;

        var tmp = Path.GetTempPath().CombinePath(Path.GetFileNameWithoutExtension(file));
        WriteLog("Extracting update package to temporary directory {0}", tmp);
        file.AsFile().Extract(tmp, true);

        // 拷贝替换更新
        CopyAndReplace(tmp, DestinationPath);

        //// 删除备份文件
        //DeleteBackup(DestinationPath);
        //!!! 先别急着删除，在Linux上，删除正在使用的文件可能导致进程崩溃

        WriteLog("Update succeeded.");

        return true;
    }

    /// <summary>启动当前应用的新进程。当前进程退出</summary>
    public void Run()
    {
        // 启动进程
#if NET6_0_OR_GREATER
        // 单文件与 AOT 发布下 EntryAssembly.Location 为空，改用进程路径；命令行首项是入口程序集路径
        var cargs = Environment.GetCommandLineArgs();
        var (exe, args) = ResolveRestartTarget(Environment.ProcessPath, cargs.Length > 0 ? cargs[0] : null);
#else
        var (exe, args) = ResolveRestartTarget(Assembly.GetEntryAssembly()?.Location, null);
#endif
        if (exe.IsNullOrEmpty()) return;

        if (args.IsNullOrEmpty())
        {
            WriteLog("Starting process {0}", exe);
            Process.Start(exe);
        }
        else
        {
            WriteLog("Starting process {0} {1}", exe, args);
            Process.Start(exe, args);
        }

        WriteLog("Exiting current process");
        if (!Runtime.IsConsole) Process.GetCurrentProcess().CloseMainWindow();
        Environment.Exit(0);
        Process.GetCurrentProcess().Kill();
    }

    /// <summary>解析重启目标：要启动的可执行文件与启动参数</summary>
    /// <remarks>
    /// <para>框架依赖部署（<c>dotnet App.dll</c>）下进程路径是 dotnet 主机而不是应用，直接启动会起一个无参数的裸主机
    /// （打印用法后立即退出），而当前进程随即退出，等于服务停机；此时以「主机 + 入口程序集」启动。</para>
    /// <para>apphost 与单文件/AOT 下进程路径本身就是应用，不需要参数。主机形态却拿不到入口程序集时返回空，
    /// 由调用方保持现状——既不启动裸主机，也不退出。</para>
    /// </remarks>
    /// <param name="processPath">当前进程的可执行文件路径</param>
    /// <param name="entryPath">入口程序集路径（命令行首项）</param>
    /// <returns>可执行文件与启动参数；可执行为空表示无法重启，参数为空表示直接启动该可执行文件</returns>
    internal static (String? Exe, String? Args) ResolveRestartTarget(String? processPath, String? entryPath)
    {
        if (processPath.IsNullOrEmpty()) return (null, null);

        // 进程路径不是 dotnet 主机时，它本身就是应用
        if (!String.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase)) return (processPath, null);

        return entryPath.IsNullOrEmpty() ? (null, null) : (processPath, entryPath);
    }
    #endregion

    #region 辅助
    private HttpClient? _Client;
    private HttpClient CreateClient()
    {
        if (_Client != null) return _Client;

        return _Client = new HttpClient().SetUserAgent();
    }

    /// <summary>删除备份文件</summary>
    /// <param name="dest">目标目录</param>
    public void DeleteBackup(String dest)
    {
        // 删除备份
        var di = dest.AsDirectory();
        var fs = di.GetAllFiles("*.del", true);
        foreach (var item in fs)
        {
            WriteLog("Delete {0}", item);
            try
            {
                item.Delete();
            }
            catch { }
        }
    }

    /// <summary>
    /// 解压缩
    /// </summary>
    /// <param name="fileName"></param>
    public String Extract(String fileName)
    {
        WriteLog("Extract {0}", fileName);

        var source = Path.GetTempPath().CombinePath(Path.GetFileNameWithoutExtension(fileName));
        WriteLog("Extracting update package to temporary directory {0}", source);
        fileName.AsFile().Extract(source, true);

        //var source = fileName.TrimEnd(".zip");
        //if (Directory.Exists(source)) Directory.Delete(source, true);
        //source.EnsureDirectory(false);
        //fileName.AsFile().Extract(source, true);

        return source;
    }

    /// <summary>拷贝并替换。正在使用锁定的文件不可删除，但可以改名</summary>
    /// <param name="source">源目录</param>
    /// <param name="dest">目标目录</param>
    public void CopyAndReplace(String source, String dest)
    {
        WriteLog("CopyAndReplace {0} => {1}", source, dest);

        var di = source.AsDirectory();

        // 来源目录根，用于截断
        var root = di.FullName.EnsureEnd(Path.DirectorySeparatorChar.ToString());
        var normalizedRoot = Path.GetFullPath(root).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).EnsureEnd(Path.DirectorySeparatorChar.ToString());
        foreach (var item in di.GetAllFiles(null, true))
        {
            var full = Path.GetFullPath(item.FullName).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var name = full.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) ? full[normalizedRoot.Length..] : full;
            var dst = dest.CombinePath(name).GetBasePath();

            // 如果是应用配置文件，不要更新
            if (dst.EndsWithIgnoreCase(".exe.config") ||
                dst.EqualIgnoreCase("appsettings.json")) continue;

            // 拷贝覆盖
            WriteLog("Copy {0}", name);
            try
            {
                item.CopyTo(dst.EnsureDirectory(true), true);
            }
            catch
            {
                // 如果是exe/dll，则先改名，因为可能无法覆盖
                if (/*dst.EndsWithIgnoreCase(".exe", ".dll") &&*/ File.Exists(dst))
                {
                    //// 先尝试删除
                    //WriteLog("Delete {0}", item);
                    //try
                    //{
                    //    File.Delete(dst);
                    //}
                    //catch
                    //{
                    // 直接Move文件，不要删除，否则Linux上可能导致当前进程退出
                    WriteLog("Move {0}", item);
                    var del = dst + ".del";
                    if (File.Exists(del)) File.Delete(del);
                    File.Move(dst, del);
                    //}

                    item.CopyTo(dst, true);
                }
            }
        }

        // 删除临时目录
        WriteLog("Delete {0}", di.FullName);
        di.Delete(true);
    }
    #endregion

    #region 日志
    /// <summary>日志对象</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>输出日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args) => Log?.InfoPrefixed("[" + Name + "]", format, args);
    #endregion
}