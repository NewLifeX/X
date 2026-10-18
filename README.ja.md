# NewLife.Core - コアライブラリ

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)
[![test](https://github.com/NewLifeX/X/actions/workflows/test.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/test.yml)
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-29%2F29-brightgreen?logo=dotnet)](Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-11%20%28net45%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Core)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm32%20%7C%20LoongArch64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/X?logo=github)](https://github.com/NewLifeX/X/graphs/contributors)

<!-- lang:start -->
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

NewLife フレームワークは、拡張可能なアプリケーションを構築するための高性能な基盤を提供する、包括的な .NET コンポーネントエコシステムです。ログ、ネットワーク、シリアライズ、キャッシュ、マルチスレッドなどの基本機能を提供し、Web サービスから IoT デバイスに至るまでの一連のアプリケーションの基盤となります。  

コアライブラリのチュートリアル：[https://newlifex.com/core](https://newlifex.com/core)  

> ⚠️ 本翻訳は更新が遅れる場合があります。正式な内容は[中文 README](Readme.MD)を参照してください。

**目次**: [フレームワーク概要](#フレームワーク概要) · [NewLife.Core を選ぶ理由](#newlifecore-を選ぶ理由) · [クイックスタート](#クイックスタート) · [主な機能](#主な機能) · [ドキュメント索引](#ドキュメント索引) · [リポジトリ構成](#リポジトリ構成) · [ビルドとテスト](#ビルドとテスト) · [新生命プロジェクトマトリックス](#新生命プロジェクトマトリックス) · [貢献とフィードバック](#貢献とフィードバック) · [ライセンス](#ライセンス)

## フレームワーク概要

NewLife は複数のコンポーネントライブラリで構成されており、NewLife.Core がその基盤を提供します。このフレームワークは net45～net10.0 のマルチターゲットフレームワーク（.NET Framework 4.5 から .NET 10.0 まで）をサポートし、さまざまな環境で幅広い互換性を実現します。

**NativeAOT**：コアパスはサポート済み（29 シナリオの実測 + CI による回帰防止）。プラグインやスクリプトなどの動的機能は設計上サポートされず、その境界は [AOT互換性分析 (中文)](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) を参照してください。


## NewLife.Core を選ぶ理由

- **共通インフラを 1 パッケージで**：ログ、構成、キャッシュ、ネットワーク、シリアライズ、スケジューリング、オブジェクトコンテナと DI を 1 つの参照で利用できます。
- **幅広いバージョン対応**：単一パッケージで 11 個のターゲットフレームワーク（net45 → net10.0、Windows 変種を含む）。レガシーシステムと新規プロジェクトで同じ API を共有できます。
- **NativeAOT 対応**：コア経路の 29 シナリオを実測し、複数 RID の CI スモークマトリクスで検証。境界は [AOT 互換性分析](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) を参照。
- **依存は控えめ、ライセンスは寛容**：BCL と Microsoft の互換パッケージのみ。MIT ライセンスのため、改変や自社名への変更も可能です（[ライセンス](#ライセンス)）。

## クイックスタート

### インストール

```bash
dotnet add package NewLife.Core
```

### 最小サンプル

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // コンソールログ（ファイルにも出力）
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // メモリキャッシュ
cache.Set("key", "value", 60);          // 60 秒で期限切れ
var value = cache.Get<String>("key");   // 読み出し
```

### ビルドとカスタマイズ

NewLife コンポーネントはコミュニティが 20 年以上にわたって共に創り上げてきたもので、MIT オープンソースライセンスを採用しています。**誰でも自由に改変して再配布できます**（出典の明記は不要）。多くの企業がこれを基盤に社内開発フレームワークを構築しており、ソースコード内の `NewLife` 文字列をすべて自社名に一括置換することでプライベート化カスタマイズを実現することもできます。

チームは常にオープンな姿勢を貫いており、VisualStudio（最新の正式版）でソリューションを開いてビルドできるほか、`dotnet build` コマンドラインでのビルドにも対応しています。プロジェクトファイルは複雑な機能を排してシンプルで使いやすさを追求し、真の意味でのアウトオブボックスを実現しています。

独自にコンパイルしてアセンブリを置き換えられるように、厳密名証明書 `newlife.snk` を公開しています。

### テンプレートの雛形

コマンドラインで次のコマンドを実行すると、NewLife コンポーネントをすぐに体験できます：

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

## ドキュメント索引

### 利用ドキュメント

- [コアライブラリ目次 (中文)](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md)：コアコンポーネントの記事インデックス
- [AOT互換性分析 (中文)](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md)：NativeAOT のサポート範囲、実測結果（スモーク 29/29）、利用ガイドと既知の境界

### 開発ドキュメント

リポジトリの `Doc/` ディレクトリでは、機能ドキュメント体系（要件定義 → 機能追跡 → アーキテクチャ設計）を維持しています：

- [要件ドキュメント (中文)](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md)：ビジョン、コア目標と機能要件
- [機能一覧 (中文)](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md)：すべての機能ポイントの実装/テスト/コメントの状態追跡
- [アーキテクチャ設計 (中文)](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md)：レイヤー構成、モジュールコンポーネントと重要な設計決定

## 主な機能


<details>
<summary><b>基礎拡張</b> (9)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [型変換Utility](https://newlifex.com/core/utility) | 効率的な型変換を提供し、各種フォーマットと例外処理をサポートします。 |
| [文字列拡張StringHelper](https://newlifex.com/core/string_helper) | 切り出し、エンコード、フォーマットなど、多様な文字列操作を含みます。 |
| [パス拡張PathHelper](https://newlifex.com/core/path_helper) | 多様なパス形式をスマートに処理し、コードのクロスプラットフォーム互換性を実現します。 |
| [データ拡張IOHelper](https://newlifex.com/core/io_helper) | IO 操作を最適化し、効率的なデータストリームとバイト配列の変換を提供します。 |
| [プロセス拡張ProcessHelper](https://newlifex.com/core/process_helper) | プロセス操作を簡素化し、プロセス情報の取得・管理に便利なメソッドを提供します。 |
| [ランタイム情報Runtime](https://newlifex.com/core/runtime) | アプリケーションのランタイム情報を取得する簡単なメソッドを提供し、デバッグと診断に役立ちます。 |
| [破棄可能DisposeBase](https://newlifex.com/core/disposebase) | 標準的なリソース解放パターンを提供し、メモリとリソースのリークを効果的に防止します。 |
| [リフレクション拡張Reflect](https://newlifex.com/core/reflect) | 高性能なリフレクションツールで、オブジェクトメンバーの読み書きを高速に行います。 |
| [セキュリティ拡張SecurityHelper](https://newlifex.com/core/security_helper) | RSA/AES/DES/MD5 などの各種暗号化・復号を統合し、セキュリティ操作を簡素化します。 |

</details>

<details>
<summary><b>ログとトレース</b> (2)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [ログILog](https://newlifex.com/core/log) | 各種のログ出力方式をサポートし、強力な非同期書き込み機能を提供します。 |
| [リンクトレースITracer](https://newlifex.com/core/tracer) | 強力な APM パフォーマンス追跡を提供し、Stardust プラットフォームとの統合をサポートします。 |

</details>

<details>
<summary><b>タイマースケジューリング</b> (3)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [高機能タイマーTimerX](https://newlifex.com/core/timerx) | 高精度タイマーで、Cron スケジューリングと非同期の周期実行をサポートします。 |
| [Cron式](https://newlifex.com/core/cron) | Linux Cron に似たスケジュールタスクを提供し、秒単位のマッチングをサポートします。 |
| [時間プロバイダーTimeProvider](https://newlifex.com/core/timerx_timeprovider) | 統一された時間抽象と高精度の経過時間計算を提供し、バージョン間の互換性を容易にします。 |

</details>

<details>
<summary><b>構成システム</b> (4)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [構成システムConfig](https://newlifex.com/core/config) | 統一された構成フレームワークで、複数フォーマット、複数ソース、ホットリロード機構をサポートします。 |
| [構成プロバイダーIConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | 統一された構成インターフェイスで、コールバックと構成ノードモデルをサポートします。 |
| [コア構成Setting](https://newlifex.com/core/setting) | コンポーネントのコア構成を管理し、ログやネットワークなどの基本動作を制御します。 |
| [システム構成SysConfig](https://newlifex.com/core/sysconfig) | アプリケーションのシステム構成を管理し、ローカル、分散、暗号化構成をサポートします。 |

</details>

<details>
<summary><b>シリアライズ</b> (7)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [Jsonシリアライズ](https://newlifex.com/core/json) | 高性能な Json ツールで、さまざまな複雑なシナリオでのシリアライズをサポートします。 |
| [Xmlシリアライズ](https://newlifex.com/core/xml) | XML シリアライズとデシリアライズを提供し、オブジェクトと XML の相互変換をサポートします。 |
| [バイナリシリアライズ](https://newlifex.com/core/binary) | 高性能なバイナリシリアライズで、複雑なオブジェクトグラフとバージョン管理をサポートします。 |
| [高度なバイナリシリアライズ](https://newlifex.com/core/advanced_binary) | プロトコルメッセージを全工程ゼロアロケーションでエンコード/デコードする上級ガイド。 |
| [Csv解析CsvFile](https://newlifex.com/core/csv_file) | CSV ファイルを高速に読み書きし、カスタム区切り文字とデータバインディングをサポートします。 |
| [CsvデータベースCsvDb](https://newlifex.com/core/csv_db) | CSV ファイルをデータベースとして利用でき、基本的な追加・削除・更新・照会をサポートします。 |
| [軽量ExcelリーダーExcelReader](https://newlifex.com/core/excel_reader) | Office への依存が不要で、Excel を高速に読み取り、大量データをサポートします。 |

</details>

<details>
<summary><b>キャッシュ</b> (7)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [統一キャッシュインターフェイスICache](https://newlifex.com/core/icache) | 標準的なキャッシュ操作を定義し、メモリ、Redis など複数の実装を提供します。 |
| [キャッシュアーキテクチャICacheProvider](https://newlifex.com/core/icacheprovider) | ICache インスタンスの管理と提供に使用し、複数キャッシュの併用をサポートします。 |
| [メモリキャッシュMemoryCache](https://newlifex.com/core/memory_cache) | 高性能なシングルマシン向けメモリキャッシュで、有効期限と容量ポリシーをサポートします。 |
| [オブジェクトプールObjectPool](https://newlifex.com/core/object_pool) | 高コストなオブジェクトの再利用に使用し、性能を向上させ GC 負荷を軽減します。 |
| [オブジェクトプールPool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | CAS ベースの高性能な汎用オブジェクトプールで、GC 負荷を軽減します。 |
| [分散キャッシュNewLife.Redis](https://newlifex.com/core/redis) | Redis ベースの分散キャッシュ実装で、多様なデータ構造をサポートします。 |
| [ベクトルストアIVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | ベクトル集合管理インターフェイス。Top-K 類似度検索をサポートし、AI アプリケーションにベクトル検索基盤を提供します。 |

</details>

<details>
<summary><b>高性能バッファ</b> (7)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [データパケットIPacket](https://newlifex.com/core/packet) | ゼロコピースライスとチェーン化パケットの中核となるデータパケットインターフェイス。 |
| [バッファBuffers](https://newlifex.com/core/buffers) | 高性能バッファ操作ライブラリで、Span やプール化書き込みなどのコンポーネントを集約します。 |
| [プール化ライターPooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | ArrayPool ベースのプール化バッファライターで、動的拡張をサポートします。 |
| [SpanリーダーSpanReader](https://newlifex.com/core/span_reader) | ゼロアロケーションの高性能バイナリストリームパーサーで、ビッグエンディアン/リトルエンディアンをサポートします。 |
| [SpanライターSpanWriter](https://newlifex.com/core/span_writer) | ゼロアロケーションの高性能バイナリデータライターで、ビッグエンディアン/リトルエンディアンをサポートします。 |
| [SpanヘルパーSpanHelper](https://newlifex.com/core/span_helper) | Span/ReadOnlySpan の拡張メソッド集で、メモリ操作を簡素化します。 |
| [シーケンスリーダーSequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | セグメントをまたいで順次読み取るリーダー（旧フレームワークではシムで補完、全 11 TFM で利用可能）。 |

</details>

<details>
<summary><b>アプリケーションフレームワーク</b> (16)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [オブジェクトコンテナObjectContainer](https://newlifex.com/core/object_container) | 軽量な依存性注入で、多様なライフサイクルと注入パターンをサポートします。 |
| [軽量アプリケーションホストHost](https://newlifex.com/core/host) | アプリケーションのホスティングを簡素化し、長時間稼働するバックグラウンドサービスを簡単に構築できます。 |
| [プラグインフレームワークIPlugin](https://newlifex.com/core/plugin) | 標準化されたプラグイン管理機構を提供し、アプリケーションのモジュール化開発を実現します。 |
| [並行モデルActor](https://newlifex.com/core/actor) | 簡素化された並行プログラミングモデルで、メッセージキューの処理によってスループットを向上させます。 |
| [パイプラインモデルPipeline](https://github.com/NewLifeX/X/blob/master/Doc/管道模型Pipeline.md) ⚠️ 廃止済み、プロトコルモデル（フレームポンプ）に置き換え | 双方向処理チェーンモデルで、Read/Write/Open/Close のフローを統一します。 |
| [遅延キューDeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | 高頻度の変更を集約してバッチ処理で永続化し、書き込み増幅を低減します。 |
| [イベントバスEventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | プロセス内のイベント配信を実現し、トピックルーティングとキュー型メッセージをサポートします。 |
| [弱参照デリゲートWeakAction](https://newlifex.com/core/weak_action) | イベント購読のメモリリーク問題を解決し、安全なイベント処理を実現します。 |
| [スクリプトエンジンScriptEngine](https://newlifex.com/core/script_engine) | C# の動的コンパイルと実行をサポートし、柔軟なスクリプト拡張機能を提供します。 |
| [圧縮・解凍](https://newlifex.com/core/compression) | 各種の圧縮アルゴリズムを統合し、データ圧縮のための統一インターフェイスを提供します。 |
| [圧縮アーカイブTarと7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Tar/Tar.Gz と 7Zip の高度なアーカイブ圧縮の使い方を網羅します。 |
| [マシン情報MachineInfo](https://newlifex.com/core/machine_info) | ハードウェア情報を網羅的に取得し、Windows と Linux システムをサポートします。 |
| [ピンインライブラリPinYin](https://newlifex.com/core/pinyin) | 漢字をピンインに変換する高効率ライブラリで、完全ピンインと略式ピンインをサポートし、性能にも優れます。 |
| [データセットDbTable](https://newlifex.com/core/dbtable) | メモリ内のデータテーブルで、データ処理と分析を容易にします。 |
| [ページングパラメータPageParameter](https://newlifex.com/core/page_parameter) | ページングロジックをカプセル化し、Web や API など多様なシーンのページングをサポートします。 |
| [スノーフレークアルゴリズムSnowflake](https://newlifex.com/core/snow_flake) | 分散環境で一意な ID を生成するアルゴリズムで、高並行下での ID の一意性を保証します。 |

</details>

<details>
<summary><b>セキュリティとトークン</b> (3)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [分散デジタル署名トークンTokenProvider](https://newlifex.com/core/token_provider) | 分散トークンの生成と検証に使用し、マルチノード間のセキュリティを確保します。 |
| [Web汎用トークンJwtBuilder](https://newlifex.com/core/jwt) | JWT トークンの生成、デコード、検証を提供し、Web 認証を簡素化します。 |
| [トークンHTTPフィルターTokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | HTTP リクエストにトークンを自動注入し、401/403 をインターセプトして自動更新します。 |

</details>

<details>
<summary><b>クラウドサービス</b> (1)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [緯度経度ハッシュGeoHash](https://newlifex.com/core/geo_hash) | 緯度経度を文字列にエンコードし、近隣位置の検索に使用します。 |

</details>

<details>
<summary><b>ネットワークライブラリ</b> (15)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [ネットワークサーバーNetServer](https://newlifex.com/core/netserver) | 高性能な TCP/UDP/Unix ドメインソケットサーバーで、セッションとデータ処理をカプセル化しています。 |
| [ネットワーククライアントNetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | 統一された TCP/UDP/Unix ネットワーククライアントで、接続と送受信を簡素化します。 |
| [メッセージIMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | 要求-応答メッセージのコントラクトで、方向セマンティクスとフレームの読み書きを担います。 |
| [メッセージプロトコルスタックMessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | フレームポンプ + ステートレスコーデックの単一スタックで、大フレームのストリーミングボディとバックプレッシャーをサポートします。 |
| [データパイプPipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | ゼロコピーのデータパイプで、System.IO.Pipelines の形態に合わせています。 |
| [簡易リモートメッセージ交換プロトコルSRMP](https://newlifex.com/core/srmp) | 軽量な RPC プロトコルで、バイナリシリアライズと UDP 転送をサポートします。 |
| [負荷分散とフェイルオーバーLoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Failover/ラウンドロビン/レースの 3 種類のマルチノードスケジューリング戦略を提供します。 |
| [RPC通信ApiServer](https://newlifex.com/core/api_server) | ⚠️ [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) に移動しました |
| [RPC通信ApiClient](https://newlifex.com/core/api_client) | ⚠️ [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) に移動しました |
| [HTTPクライアントApiHttpClient](https://newlifex.com/core/api_http) | HttpClient をベースにカプセル化し、Web API の呼び出しと処理を簡素化します。 |
| [HTTPサーバーHttpServer（組み込みデバイス）](https://newlifex.com/core/httpserver) | 軽量な Http サーバーで、ミドルウェアパイプライン、パラメーター化ルーティング、静的ファイルサービスをサポートします。 |
| [ネットワークダウンロードWebClientX](https://newlifex.com/core/webclient) | WebClient の強化版で、マルチスレッドとレジューム対応ダウンロードをサポートします。 |
| [WebSocketサーバー（IoT 下り通知の切り札）](https://newlifex.com/core/websocket) | 高性能な WebSocket サーバーで、IoT のメッセージプッシュに適しています。 |
| [WebSocket双方向通信](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | ハンドシェイク、フレーム処理、ハートビート、接続クローズの実践を紹介します。 |
| [ファイルストレージクライアントEasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | HTTP ファイルストレージクライアントで、EasyIO や OSS と連携します。 |

</details>

> ⚠️ 百度地図/高徳地図は独立した NuGet パッケージに分離されました。


## リポジトリ構成

| ディレクトリ | 説明 |
| --- | --- |
| `NewLife.Core/` | コアライブラリのソース（ログ、ネットワーク、キャッシュ、シリアライズなど） |
| `NewLife.Core.SourceGenerator/` | パッケージ内の `analyzers/` として同梱されるソースジェネレーター |
| `Doc/` | 機能ドキュメントと記事索引 |
| `Samples/` | コンソール / サービス / Http / IoT / モバイルのサンプル |
| `Benchmark/` | BenchmarkDotNet による性能ベンチマーク |
| `XUnitTest.Core/` | メインの単体テスト（net10.0） |
| `XUnitTest.Compat/`、`XUnitTest.Net6/` | バージョン間の互換性テスト |
| `AotSmoke/` | NativeAOT スモークプロジェクト |

## ビルドとテスト

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- 全並列テスト：`run-tests.ps1`（マルチホスト並列 + タイムアウト + trx 集計）
- NativeAOT スモーク：`AotSmoke/`、CI は `.github/workflows/aot.yml`
- ベンチマーク：`Benchmark/`（BenchmarkDotNet）

## 新生命プロジェクトマトリックス

各プロジェクトはデフォルトで net10.0/netstandard2.1/netstandard2.0/net462/net4.5 をサポートし、旧版（2024.0801）では net4.0/net2.0 をサポートします。  

| プロジェクト | 年 | 説明 |
| :---: | :---: | --- |
| 基礎コンポーネント |  | その他のミドルウェアや製品プロジェクトを支えます |
| [NewLife.Core](https://github.com/NewLifeX/X) | 2002 | コアライブラリ。ログ、構成、キャッシュ、ネットワーク、シリアライズ、APM パフォーマンス追跡 |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 2005 | ビッグデータミドルウェア。単一テーブルで百億件級、MySql/SQLite/SqlServer/Oracle/PostgreSql/達夢、自動シャーディング、読み書き分離 |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 2005 | ネットワークライブラリ。単一マシンで千万級スループット（2266万tps）、単一マシンで百万級接続（400万Tcp長接続） |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011 | プロトコル通信ライブラリ。CS アプリケーション通信フレームワークを提供し、Http/RPC 通信フレームワークをサポート、高スループット、IoT デバイスを低オーバーヘッドで容易に接続 |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 2010 | 魔方（Cube）クイック開発プラットフォーム。ユーザー権限、SSO ログイン、OAuth サーバーなどを統合し、単一テーブル 100 億件級のプロジェクトで検証済み |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 2008 | サービス管理コンポーネント。アプリケーションを OS のデーモンとしてインストール。Windows サービス、Linux の Systemd |
| [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero) | 2020 | Zero 零代スキャフォールディング。NewLife コンポーネントエコシステムに基づくプロジェクトテンプレート NewLife.Templates、Web、WebApi、Service |
| ミドルウェア |  | 有名なミドルウェアプラットフォームと連携 |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | 2017 | Redis クライアント。マイクロ秒級の遅延、百万級のスループット、豊富なメッセージキュー、百億級データ量のプロジェクトで検証済み |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018 | RocketMQ 純マネージドクライアント。Apache RocketMQ と Alibaba Cloud メッセージキューをサポート、十億級プロジェクトで検証 |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | 2019 | IoT メッセージプロトコル。MqttClient/MqttServer、クライアントは Alibaba Cloud IoT をサポート |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 2022 | IoT 標準ライブラリ。IoT 分野の各種通信プロトコル標準仕様を定義 |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 2022 | ModbusTcp/ModbusRTU/ModbusASCII。IoT 標準ライブラリを基に実装し、ZeroIoT プラットフォームと IoTEdge ゲートウェイをサポート |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 2022 | Siemens PLC プロトコル。IoT 標準ライブラリを基に実装し、IoT プラットフォームと IoTEdge をサポート |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 2022 | 地図コンポーネントライブラリ。百度地図、高徳地図、テンセント地図、天地図をカプセル化 |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 2023 | オーディオコーデックライブラリ。PCM/ADPCMA/G711A/G722U/WAV/AAC |
| 製品プラットフォーム |  | 製品プラットフォーム級。コンパイルとデプロイですぐに使用でき、個別カスタマイズが可能 |
| [Stardust](https://github.com/NewLifeX/Stardust) | 2018 | Stardust、分散サービスプラットフォーム。ノード管理、APM モニタリングセンター、構成センター、登録センター、リリースセンター |
| [AntJob](https://github.com/NewLifeX/AntJob) | 2019 | AntJob、分散ビッグデータ計算プラットフォーム（リアルタイム/オフライン）。アリの引っ越しに着想を得たシャーディング思想、兆級データ量のプロジェクトで検証済み |
| [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP) | 2021 | 企業向け ERP。製品管理、顧客管理、販売管理、サプライヤー管理 |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 2006 | 開発者向けツール集。数多くの開発者ツール。ネットワーク、シリアルポート、暗号化・復号、正規表現、Modbus、MQTT |
| [EasyIO](https://github.com/NewLifeX/EasyIO) | 2023 | 簡易ファイルストレージ。分散システムでのファイル集中ストレージをサポート。 |
| [XProxy](https://github.com/NewLifeX/XProxy) | 2005 | 製品レベルのリバースプロキシ。NAT プロキシ、Http プロキシ |
| [HttpMeter](https://github.com/NewLifeX/HttpMeter) | 2022 | Http 負荷テストツール |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 2015 | Git ソースコード管理システム |
| [SmartOS](https://github.com/NewLifeX/SmartOS) | 2014 | 組み込みオペレーティングシステム。完全に独立自社開発で、ARM Cortex-M チップアーキテクチャをサポート |
| [SmartA2](https://github.com/NewLifeX/SmartA2) | 2019 | 組み込み産業用コンピュータ、IoT エッジゲートウェイ、高性能 .NET8 ホスト。工業、農業、交通、医療分野に適用 |
| FIoT IoTプラットフォーム | 2020 | IoT トータルソリューション。建築、環境保護、農業向けで、ソフトウェア・ハードウェアおよびビッグデータ分析を一体化、単一マシンで十万級ポイントのプロジェクトで検証済み |


---

## 新生命開発チーム

![XCode](https://newlifex.com/logo.png)

新生命チーム（NewLife）は 2002 年に設立され、新時代の IoT 業界ソリューションの提供者として、ソフトウェアおよびハードウェアアプリケーションのソリューションコンサルティング、システムアーキテクチャの計画・開発サービスを提供しています。NuGet の累計ダウンロード数は 523 万回以上。チームの中核コンポーネント（XCode / Redis / Stardust / AntJob / IoT など）は、電力、工業、交通、環境保護、医療、文化・博物館などの業界で大規模に採用されています。

私たちは改善を続け、長期的に信頼される .NET / IoT / 分散技術パートナーとなることを目指します。

ウェブサイト：https://newlifex.com   オープンソース組織：https://github.com/NewLifeX  
WeChat 公式アカウント：  
![智能大石头](https://newlifex.com/stone.jpg)

## 貢献とフィードバック

- 提出前に `dotnet build` と関連する単体テストが通ることを確認してください。コードスタイルとコミットメッセージはリポジトリの規約に従います。
- 不具合や提案は [Issue](https://github.com/NewLifeX/X/issues) へ。Star と PR も歓迎します。
- 変更履歴は [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md) を参照してください。

## ライセンス

本プロジェクトは [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE) ライセンスです。強名付け証明書 `newlife.snk` を公開しており、独自にビルドしてアセンブリを置き換えられます。

> このプロジェクトがお役に立てば、ぜひ Star を付け、チームに共有するか、Issue/PR で改善提案をお寄せください。
