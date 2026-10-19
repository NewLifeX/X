# README 多语言规范

根目录的 `README.md`（English，默认落地页）、`README.zh-CN.md`（简体中文，双权威）与 `README.*.md` 是同一份说明文档的多语种版本。语言切换行由脚本从 `readme-languages.json` 生成，本文规定切换行的**顺序**、**权威关系**、**NuGet 包版**与**维护方式**。

## 定序规则

1. **固定前三**：English（`README.md`，GitHub 默认落地页与 NuGet 包 README 的源文件）→ 简体中文（`README.zh-CN.md`，与英文双权威）→ 繁體中文（`README.zh-TW.md`，与简体同源，紧邻便于对照）。
2. **其余按技术内容语种规模排序**：以 W3Techs「内容语种占比」（全球 top 1000 万站点首页）为依据，一次定序后**冻结**。
3. **新语种一律追加到末尾**，不做全局重排（同一语言的不同书写系统例外：紧跟其主语言，如 `繁體中文` 紧随简体）；只有拿到更可靠的读者分布数据时，才允许做一次整体校正。

### 权威与同步

- **English 与简体中文为双权威**：内容变更必须在同一 PR 内同时更新 `README.md` 与 `README.zh-CN.md`；发布前以英文版为对外口径。
- **其他语种允许滞后**：开头必须写明「本翻译可能滞后，以 [English README](README.md) 为准」。
- **冲突处理**：英文与简体中文不一致时，以英文版为准并回补中文；不得让默认落地页长期落后。

### 当前顺序

| # | 语种 | 文件 | 依据（W3Techs，2025-12-03） |
|---|------|------|------|
| 1 | English | `README.md` | 默认落地页与 NuGet 包 README 的源文件，固定第一位 |
| 2 | 简体中文 | `README.zh-CN.md` | 与英文同批维护，固定第二位 |
| 3 | 繁體中文 | `README.zh-TW.md` | 与简体同源，紧随简体便于对照 |
| 4 | Español | `README.es.md` | 6.0% |
| 5 | Deutsch | `README.de.md` | 5.9% |
| 6 | 日本語 | `README.ja.md` | 5.1% |
| 7 | Français | `README.fr.md` | 4.5% |
| 8 | Português | `README.pt.md` | 4.1% |
| 9 | Русский | `README.ru.md` | 3.7% |
| 10 | 한국어 | `README.ko.md` | 0.8% |
| 11 | العربية | `README.ar.md` | 0.5% |

> 运行时本地化用 `zh-Hant` 统一承接 `zh-TW` / `zh-HK` / `zh-MO`；README 目前只有一份以台湾用语撰写的 `README.zh-TW.md`，若日后需要港式用语再单列。

## 为什么不按语言人口排序

- README 的读者是**开发者**，语种价值取决于「多少开发者能读」，而不是「多少人会说」。印地语、孟加拉语人口排在前列，但本库几乎没有这些读者；韩语人口约 8000 万，.NET 使用密度却很高。
- 人口数据逐年变化，按人口排意味着反复重排；而切换行只有 10 个链接、一屏可扫完，**排序的边际收益很低，重排成本（11 个文件重复编辑与评审噪声）很高**。
- 固定前三可避免通用语被某个大语种挤到后面，同时保留中文用户的第二入口。

> 数据说明：GitHub Traffic 只提供 referrer 与热门路径，没有国家维度；NuGet 也不提供地域下载统计，因此无法直接按「本项目真实流量」定序，暂以网页内容语种占比作代理。若日后能从 `newlifex.com` 访问统计得到国家分布，可按它做一次校正，并在上表记录日期与依据。

## 维护方式

- **单一事实源**：`readme-languages.json`（`code` / `name` / `file`，数组顺序即展示顺序）。
- **生成与校验**：`sync-readme-languages.ps1` 按配置重写各 README 的切换块；加 `-Check` 只校验不写入，不一致时退出码 1。
- **NuGet 包版**：`sync-readme-package.ps1` 按 `readme-package.json` 从 `README.md` 生成 `README.nuget.md`，把所有相对链接改写为 GitHub 绝对地址；加 `-Check` 校验生成物与 `PackageReadmeFile` 目标。
- **CI 门禁**：`.github/workflows/readme.yml` 在 README、配置或脚本变动时执行 `-Check`，顺序不一致或多份副本漂移会直接失败。
- 各 README 只有下面标记块之间的内容由脚本生成，其余内容不受影响：

```markdown
<!-- lang:start -->
🌐 [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [English](README.md) | ...
<!-- lang:end -->
```

## 新增一种语言

1. 翻译整篇 README，文件名用 `README.{BCP 47 语言标签}.md`；English 固定 `README.md`，简体中文固定 `README.zh-CN.md`，繁體中文固定 `README.zh-TW.md`。
2. 在 `readme-languages.json` 的 `languages` 数组**末尾**追加 `{ "code", "name", "file" }`，`name` 用本族语名称。
3. 在正文开头补上与其它版本一致的说明：「本翻译可能滞后，以 [English README](README.md) 为准」。
4. 运行 `pwsh -File sync-readme-languages.ps1` 生成切换行，再运行 `-Check` 确认。
5. 根目录新增了 README 却没登记到配置，脚本会直接报错，避免切换行静默漏掉该语种。

## NuGet 包 README

- `PackageReadmeFile` 一律指向生成的 `README.nuget.md`，不直接使用 `README.md`。
- 生成规则：`sync-readme-package.ps1` 读取 `readme-package.json`，把 Markdown 相对链接改写为 `https://github.com/{owner}/{repo}/blob/{branch}/...`；页内锚点保持不变。
- 生成物带 `<!-- generated ... -->` 头，禁止手工修改；CI 执行 `-Check` 防止漂移。
- 子项目若有独立包 README，在 `readme-package.json` 的 `items` 中增加一条，源文件用该子项目的 English README，输出到同目录 `README.nuget.md`。

## 徽章模板（下游复用）

下游仓不要直接抄 Core 的徽章 URL，按下面的占位符替换 `{owner}`、`{repo}`、`{package}`、`{branch}`、`{workflow}`：

```markdown
![GitHub License](https://img.shields.io/github/license/{owner}/{repo}?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/{package}?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/{package}?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/{package}?label=dev%20nuget&logo=nuget)
[![build](https://github.com/{owner}/{repo}/actions/workflows/{workflow}.yml/badge.svg?branch={branch})](https://github.com/{owner}/{repo}/actions/workflows/{workflow}.yml)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
[![contributors](https://img.shields.io/github/contributors/{owner}/{repo}?logo=github)](https://github.com/{owner}/{repo}/graphs/contributors)
```

规则：

- **必选**：License、NuGet 下载量、NuGet 版本、预发布版本、构建/测试、平台、贡献者。
- **可选**：TFM 数量、locales 数量、AOT 冒烟；没有对应工作流或指标来源就不要放。
- 能用动态 GitHub Actions 徽章的，优先用动态；静态数字（如 `AOT smoke-31/31`、`locales-31`）必须配同步脚本与 CI 校验，避免再次漂移。
- 徽章顺序建议：许可证 → NuGet → CI → 平台/指标 → 贡献者。
- 语言数量用 `locales-{n}`，不要写"支持 31 种语言"之类正文数字；数字变化时只改配置。

## 展示约定

- 语言名一律用**本族语名称**（`日本語`、`Español`），不用英文名，也不用国旗——语言 ≠ 国家。
- 当前页面的语种在切换行中省略自身，避免点到当前页。
- 分隔符统一为 ` | `，行首保留 `🌐`。
