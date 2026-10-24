# Tools/Scripts

仓库级 PowerShell 脚本：CI 门禁与本地开发工具。与 `Benchmark/`、`Samples/` 下随项目走的脚本不同，这些脚本服务于整个仓库。

约定：脚本位于 `Tools/Scripts/`，用「上两级」定位仓库根目录；`check-english-exceptions.ps1` 与 `sync-readme-*.ps1` 另提供 `-Root` 供下游仓库复用。

## 脚本索引

| 脚本 | 用途 | 阻断 CI | 调用方 |
|---|---|---|---|
| `run-tests.ps1` | 本地全量并行跑测：逐工程构建一次，全部宿主并发 `dotnet test`，单宿主超时兜底 + trx 汇总；`-Repeat` 做稳定性压测 | 否 | 本地命令行 |
| `check-english-exceptions.ps1` | 运行时消息（异常、日志、控制台、埋点标签）不得含中文 | 是 | `test.yml`；`run-tests.ps1` 前置 |
| `check-locales.ps1` | 多语言词表键集、占位符、格式说明符一致性 | 是 | `test.yml`；`run-tests.ps1` 前置 |
| `check-package-consumer.ps1` | 下游仅 `PackageReference` + `Locales/*.yml` 的端到端验证（打包、nupkg 内容、翻译与英文回退） | 是 | `test.yml`、`publish.yml`、`publish-beta.yml` |
| `check-api-compat.ps1` | 以 NuGet 已发布版本为基线的 API 兼容校验（Package Validation），本地复现 CI 门禁 | 否（CI 观察期） | `publish-beta.yml` |
| `sync-readme-languages.ps1` | 按 `readme-languages.json` 生成各 README 的语言切换行 | 是（`-Check`） | `readme.yml` |
| `sync-readme-metrics.ps1` | 按 `readme-metrics.json` 写入指标徽章与标记数字 | 是（`-Check`） | `readme.yml` |
| `sync-readme-package.ps1` | 由 `README.md` 生成 NuGet 包内 `README.nuget.md`，相对链接改写为绝对地址 | 是（`-Check`） | `readme.yml` |

## 配置与产物

- 配置描述根目录的 README，留在仓库根：`readme-languages.json`、`readme-metrics.json`、`readme-package.json`
- 产物均已被 gitignore：`TestResults/<时间戳>/`（跑测 trx 与日志）、`.tmp/api-compat/`、`.tmp/package-consumer/`

## 常用命令

```powershell
pwsh -File Tools/Scripts/run-tests.ps1 -List            # 列出全部测试宿主
pwsh -File Tools/Scripts/run-tests.ps1 -Only "Core-*"   # 只跑名称匹配的宿主
pwsh -File Tools/Scripts/check-locales.ps1              # 词表一致性校验
pwsh -File Tools/Scripts/sync-readme-package.ps1 -Check # README 包版校验
```

新增、重命名或删除脚本时，同步更新 `.github/workflows/` 中的引用与 `paths` 过滤，以及 `Doc/` 下的相关文档。
