# NewLife.Core - コアライブラリ

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)
[![test](https://github.com/NewLifeX/X/actions/workflows/test.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/test.yml)
<!-- metrics:start -->
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-31%2F31-brightgreen?logo=dotnet)](Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-11%20%28net45%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Core)
[![locales](https://img.shields.io/badge/locales-31-2ea44f?logo=dotnet)](Doc/多语言本地化.md)
<!-- metrics:end -->
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm%20%7C%20loongarch64%20%7C%20riscv64%20%7C%20s390x-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/X?logo=github)](https://github.com/NewLifeX/X/graphs/contributors)

<!-- lang:start -->
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

**NewLife.Core** は NewLife エコシステムの基盤です。ログ、構成、キャッシュ、ネットワーク、シリアライズ、スケジューリング、オブジェクトコンテナ、APM トレーシングを 1 つのパッケージで提供します。net45 から net10.0 までの 11 個のターゲットフレームワークに対応し、XCode、Cube、Redis、Stardust など 30 以上の兄弟プロジェクトを支えています。

コアライブラリのチュートリアル：[https://newlifex.com/core](https://newlifex.com/core)  

> ℹ️ 本日本語版は [English README](README.md) と同時にメンテナンスしています。内容が一致しない場合は英語版を正式な基準とします。

**目次**：[クイックスタート](#クイックスタート) · [NewLife.Core を選ぶ理由](#newlifecore-を選ぶ理由) · [主な機能](#主な機能) · [エコシステム](#エコシステム) · [ドキュメントとサンプル](#ドキュメントとサンプル) · [コントリビューション](#コントリビューション) · [新生命開発チーム](#新生命開発チーム) · [ライセンス](#ライセンス)

## クイックスタート

### インストール

```bash
dotnet add package NewLife.Core
```

### 最小サンプル

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // console log (also writes to file)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // in-memory cache
cache.Set("key", "value", 60);          // expires in 60 seconds
var value = cache.Get<String>("key");   // read back
```

### テンプレートの雛形

コマンドラインで次のコマンドを実行すると、NewLife コンポーネントをすぐに体験できます：

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### プラットフォーム対応

| 項目 | 対応範囲 |
| --- | --- |
| ターゲットフレームワーク | 単一パッケージで 11 個：net45、net461、net462、netstandard2.0、netstandard2.1、net6.0、net6.0-windows、net8.0、net8.0-windows、net10.0、net10.0-windows |
| OS とアーキテクチャ | Windows、Linux、macOS、Android、iOS；x64、x86、arm64、arm、loongarch64、riscv64、s390x |
| NativeAOT | <!--aot-scenarios:start-->31<!--aot-scenarios:end--> 個のコアシナリオを実測済み。複数 RID の CI スモークマトリクスで検証します（win-x64、linux-x64、linux-arm64、linux-loongarch64 はブロッキングゲート）。プラグインやスクリプトなどの動的機能は設計上 AOT 非対応です。境界は [AOT互換性分析](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) を参照してください |

## NewLife.Core を選ぶ理由

- **共通インフラを 1 パッケージで**：ログ、構成、キャッシュ、ネットワーク、シリアライズ、スケジューリング、オブジェクトコンテナと DI を 1 つの参照で利用できます。
- **幅広いバージョン対応**：単一パッケージで 11 個のターゲットフレームワーク（net45 → net10.0、Windows 変種を含む）。レガシーシステムと新規プロジェクトが同じ API を共有できます。
- **エコシステム規模で実証済み**：同じ基盤が XCode（単一テーブルで百億件級）、Net（2266 万 tps）、Redis と Stardust（数百億件のデータ量）を支えています。
- **依存は控えめ、ライセンスは寛容**：BCL と Microsoft の互換パッケージのみ。MIT ライセンスのため、ソースの改変や自社名への変更も可能です（[ライセンス](#ライセンス)）。

## 主な機能

<details>
<summary><b>基礎拡張</b> (10)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [型変換Utility](https://newlifex.com/core/utility) | 効率的な型変換を提供し、さまざまな形式と例外処理をサポートします。 |
| [文字列拡張StringHelper](https://newlifex.com/core/string_helper) | 切り取り、エンコード、書式設定など、多様な文字列操作を含みます。 |
| [パス拡張PathHelper](https://newlifex.com/core/path_helper) | 多様なパス形式をインテリジェントに処理し、コードのクロスプラットフォーム互換性を実現します。 |
| [データ拡張IOHelper](https://newlifex.com/core/io_helper) | IO 操作を最適化し、効率的なデータストリームとバイト配列の変換を提供します。 |
| [プロセス拡張ProcessHelper](https://newlifex.com/core/process_helper) | プロセス操作を簡素化し、プロセス情報の取得と管理のための便利なメソッドを提供します。 |
| [ランタイム情報Runtime](https://newlifex.com/core/runtime) | アプリケーションのランタイム情報を取得する簡単な方法を提供し、デバッグや診断に便利です。 |
| [破棄可能DisposeBase](https://newlifex.com/core/disposebase) | 標準的なリソース解放パターンを提供し、メモリとリソースのリークを効果的に防ぎます。 |
| [リフレクション拡張Reflect](https://newlifex.com/core/reflect) | 高性能なリフレクションツールで、オブジェクトメンバーの読み書きを高速に行います。 |
| [セキュリティ拡張SecurityHelper](https://newlifex.com/core/security_helper) | RSA/AES/DES/MD5 など多様な暗号化・復号を統合し、セキュリティ操作を簡素化します。 |
| [緯度経度ハッシュGeoHash](https://newlifex.com/core/geo_hash) | 緯度経度を文字列にエンコードし、近隣位置の検索に使用します。 |

</details>

<details>
<summary><b>ログとトレース</b> (2)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [ログILog](https://newlifex.com/core/log) | 多様なログ出力方式をサポートし、強力な非同期書き込み機能を提供します。 |
| [トレーシングITracer](https://newlifex.com/core/tracer) | 強力な APM パフォーマンストレーシングで、Stardust プラットフォームとの連携をサポートします。 |

</details>

<details>
<summary><b>タイマースケジューリング</b> (3)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [高機能タイマーTimerX](https://newlifex.com/core/timerx) | 高精度タイマーで、Cron スケジューリングと非同期の周期実行をサポートします。 |
| [Cron式](https://newlifex.com/core/cron) | Linux Cron に似たスケジュールタスクを提供し、秒単位のマッチングをサポートします。 |
| [タイムプロバイダーTimeProvider](https://newlifex.com/core/timerx_timeprovider) | 統一された時間抽象と高精度な経過時間計算を提供し、クロスバージョン互換に便利です。 |

</details>

<details>
<summary><b>構成システム</b> (4)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [構成システムConfig](https://newlifex.com/core/config) | 統一された構成フレームワークで、複数形式、複数ソース、ホットリロードをサポートします。 |
| [構成プロバイダーIConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | 統一された構成インターフェイスで、コールバックと構成ノードモデルをサポートします。 |
| [コア構成Setting](https://newlifex.com/core/setting) | コンポーネントの中核構成を管理し、ログやネットワークなどの基本動作を制御します。 |
| [システム構成SysConfig](https://newlifex.com/core/sysconfig) | アプリケーションのシステム構成を管理し、ローカル、分散、暗号化構成をサポートします。 |

</details>

<details>
<summary><b>シリアライズ</b> (7)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [Jsonシリアライズ](https://newlifex.com/core/json) | 高性能な Json ツールで、多様な複雑シーンのシリアライズをサポートします。 |
| [Xmlシリアライズ](https://newlifex.com/core/xml) | XML のシリアライズとデシリアライズを提供し、オブジェクトと XML の相互変換をサポートします。 |
| [バイナリシリアライズ](https://newlifex.com/core/binary) | 高性能なバイナリシリアライズで、複雑なオブジェクトグラフとバージョン管理をサポートします。 |
| [高度なバイナリシリアライズ](https://newlifex.com/core/advanced_binary) | プロトコルメッセージ全体をゼロアロケーションでエンコード/デコードする上級ガイドです。 |
| [CsvパーサーCsvFile](https://newlifex.com/core/csv_file) | CSV ファイルを高速に読み書きし、カスタム区切り文字とデータバインドをサポートします。 |
| [CsvデータベースCsvDb](https://newlifex.com/core/csv_db) | CSV ファイルをデータベースとして使用し、基本的な作成・読み取り・更新・削除をサポートします。 |
| [軽量ExcelリーダーExcelReader](https://newlifex.com/core/excel_reader) | Office 依存なしで Excel を高速に読み取り、大量データをサポートします。 |

</details>

<details>
<summary><b>キャッシュ</b> (7)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [統一キャッシュインターフェイスICache](https://newlifex.com/core/icache) | 標準的なキャッシュ操作を定義し、メモリや Redis など複数の実装を提供します。 |
| [キャッシュアーキテクチャICacheProvider](https://newlifex.com/core/icacheprovider) | ICache インスタンスを管理・提供し、複数キャッシュの共存をサポートします。 |
| [メモリキャッシュMemoryCache](https://newlifex.com/core/memory_cache) | 高性能なスタンドアロン型メモリキャッシュで、有効期限と容量ポリシーをサポートします。 |
| [オブジェクトプールObjectPool](https://newlifex.com/core/object_pool) | 高コストなオブジェクトを再利用し、性能を高めて GC 負荷を軽減します。 |
| [オブジェクトプールPool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | CAS ベースの高性能な汎用オブジェクトプールで、GC 負荷を軽減します。 |
| [分散キャッシュNewLife.Redis](https://newlifex.com/core/redis) | Redis ベースの分散キャッシュ実装で、多様なデータ構造をサポートします。 |
| [ベクトルストアIVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | ベクトルコレクション管理インターフェイスで、Top-K 類似度検索をサポートし、AI アプリケーションにベクトル検索の基盤を提供します。 |

</details>

<details>
<summary><b>高性能バッファ</b> (7)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [データパケットIPacket](https://newlifex.com/core/packet) | ゼロコピーのスライスとチェーン型パケットを実現する中核のデータパケットインターフェイスです。 |
| [バッファBuffers](https://newlifex.com/core/buffers) | 高性能なバッファ操作ライブラリで、Span やプール化ライターなどのコンポーネントを集約しています。 |
| [プール化ライターPooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | ArrayPool ベースのプール化バッファライターで、動的拡張をサポートします。 |
| [SpanリーダーSpanReader](https://newlifex.com/core/span_reader) | ゼロアロケーションの高性能バイナリストリームパーサーで、リトル/ビッグエンディアンをサポートします。 |
| [SpanライターSpanWriter](https://newlifex.com/core/span_writer) | ゼロアロケーションの高性能バイナリデータライターで、リトル/ビッグエンディアンをサポートします。 |
| [SpanヘルパーSpanHelper](https://newlifex.com/core/span_helper) | Span/ReadOnlySpan の拡張メソッド集で、メモリ操作を簡素化します。 |
| [シーケンスリーダーSequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | セグメントをまたいで順次読み取るリーダー（旧フレームワークではシムで補完、全 11 TFM で利用可能）。 |

</details>

<details>
<summary><b>アプリケーションフレームワーク</b> (15)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [オブジェクトコンテナObjectContainer](https://newlifex.com/core/object_container) | 軽量な依存性注入で、多様なライフサイクルと注入パターンをサポートします。 |
| [軽量アプリケーションホストHost](https://newlifex.com/core/host) | アプリケーションのホスティングを簡素化し、長時間稼働するバックグラウンドサービスを簡単に構築できます。 |
| [プラグインフレームワークIPlugin](https://newlifex.com/core/plugin) | 標準化されたプラグイン管理機構を提供し、アプリケーションのモジュール化開発を実現します。 |
| [並行モデルActor](https://newlifex.com/core/actor) | 簡素化された並行プログラミングモデルで、メッセージキューの処理によってスループットを向上させます。 |
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
<summary><b>ネットワークライブラリ</b> (13)</summary>

| 機能 | 主な特徴 |
| --- | --- |
| [ネットワークサーバーNetServer](https://newlifex.com/core/netserver) | 高性能な TCP/UDP/Unix ドメインソケットサーバーで、セッションとデータ処理をカプセル化しています。 |
| [ネットワーククライアントNetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | 統一された TCP/UDP/Unix ネットワーククライアントで、接続と送受信を簡素化します。 |
| [メッセージIMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | 要求-応答メッセージのコントラクトで、方向セマンティクスとフレームの読み書きを担います。 |
| [メッセージプロトコルスタックMessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | フレームポンプ + ステートレスコーデックの単一スタックで、大フレームのストリーミングボディとバックプレッシャーをサポートします。 |
| [データパイプPipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | ゼロコピーのデータパイプで、System.IO.Pipelines の形態に合わせています。 |
| [簡易リモートメッセージ交換プロトコルSRMP](https://newlifex.com/core/srmp) | 軽量な RPC プロトコルで、バイナリシリアライズと UDP 転送をサポートします。 |
| [負荷分散とフェイルオーバーLoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Failover、ポーリング、レーシングの 3 種類のマルチノードスケジューリング戦略を提供します。 |
| [HTTPクライアントApiHttpClient](https://newlifex.com/core/api_http) | HttpClient をベースにカプセル化し、Web API の呼び出しと処理を簡素化します。 |
| [HTTPサーバーHttpServer（組み込みデバイス）](https://newlifex.com/core/httpserver) | 軽量な Http サーバーで、ミドルウェアパイプライン、パラメーター化ルーティング、静的ファイル配信をサポートします。 |
| [ネットワークダウンロードWebClientX](https://newlifex.com/core/webclient) | 拡張版 WebClient で、マルチスレッドとレジューム可能なダウンロードをサポートします。 |
| [WebSocketサーバー（IoT ダウンリンク通知の利器）](https://newlifex.com/core/websocket) | 高性能な WebSocket サーバーで、IoT のメッセージプッシュに適しています。 |
| [WebSocket双方向通信](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | ハンドシェイク、フレーム処理、ハートビート、接続クローズの実践を紹介します。 |
| [ファイルストレージクライアントEasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | EasyIO や OSS に接続する HTTP ファイルストレージクライアントです。 |

</details>

> RPC（`ApiServer`/`ApiClient`）は [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) に移動しました。

## エコシステム

NewLife は、独立したバージョン管理を行うオープンソースプロジェクトの階層型エコシステムです。以下のレイヤーマップはすべての NewLife リポジトリで同一の内容を共有しており、詳細表では各プロジェクトの位置づけと実績を示します。

| レイヤー | プロジェクト |
| --- | --- |
| 基盤 | Core · XCode · Net · Remoting · Agent · Templates |
| データとメッセージング | Redis · RocketMQ · MQTT · NovaDb · MySql |
| Web、AI、分散 | Cube · Stardust · AntJob · NewLife.AI |
| 産業と IoT | IoT · Modbus · Siemens · Omron · Melsec · OPC · BACnet · ZeroIoT |
| 車載テレマティクス | JT808 · JT809 · JT1078 |
| ドメインコンポーネント | Office · Audio · Video · Map · IP |
| ツールと製品 | CrazyCoder · GitCandy · LuckyClover |

### プロジェクト詳細

| プロジェクト | 概要 | 人気度 |
| --- | --- | --- |
| **基盤** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | ログ、構成、キャッシュ、ネットワーク、シリアライズ、スケジューリング、APM トレーシング。 | ★1.9k · 191万 |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | データミドルウェア：単一テーブルで数百億件級、自動シャーディング、読み書き分離、10 種類以上のデータベースに対応。 | ★104 · 54.1万 |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 高性能ネットワークライブラリ：単一マシンで 2266 万 tps、400 万 Tcp 長接続。 | ★313 · 7.45万 |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | CS アプリケーションと IoT デバイス向けのプロトコル通信と RPC フレームワーク。 | ★16 · 27.2万 |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | .NET アプリケーションを OS デーモンとしてインストール：Windows サービスと Linux systemd。 | ★45 · 10.8万 |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | コンソール、Web、API、サービス、データ、IoT 向けの `dotnet new` プロジェクトテンプレート。 | ★82 · 1.49万 |
| **データとメッセージング** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | マイクロ秒級の遅延、数百万 ops/s のスループット、メッセージキュー内蔵の Redis クライアント。 | ★878 · 72.3万 |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Apache RocketMQ と Alibaba Cloud Message Queue に対応した純マネージド RocketMQ クライアント。 | ★225 · 12万 |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT クライアントとサーバー。Alibaba Cloud IoT への接続を含みます。 | ★150 · 9.94万 |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 製ハイブリッドデータベース：組み込み/ネットワークの両モード、リレーショナル、時系列、メッセージキュー、KV を 1 つに。 | ★12 · 3.4k |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | NewLife データスタック上に構築された MySql ドライバーとヘルパー層。 | ★23 · 8.4k |
| **Web、AI、分散** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 高速管理プラットフォームフレームワーク：権限、SSO、OAuth サーバー、CRUD スキャフォールディング。 | ★637 · 18.6万 |
| [Stardust](https://github.com/NewLifeX/Stardust) | 分散サービスプラットフォーム：ノード管理、APM、構成センター、登録センター、リリースセンター。 | ★354 · 27.4万 |
| [AntJob](https://github.com/NewLifeX/AntJob) | シャーディング、リトライ、バックフィルに対応した分散タスクとリアルタイムデータのスケジューリングプラットフォーム。 | ★429 · 5.68万 |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 46 社の LLM プロバイダーに統一接続するクライアントと、すぐに使える Web チャットアプリ ChatAI。 | ★20 · 1.14万 |
| **産業と IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | プロトコル契約とデバイス抽象を定義する IoT 標準ライブラリ。 | ★30 · 10.4万 |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | NewLife.IoT 上に実装された Modbus TCP/RTU/ASCII。 | ★27 · 4.87万 |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | NewLife.IoT 上に実装されたシーメンス PLC プロトコル。 | ★30 · 1.43万 |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | NewLife.IoT 上に実装されたオムロン PLC プロトコル。 | ★8 · 1.11万 |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | NewLife.IoT 上に実装された三菱 Melsec PLC プロトコル。 | ★34 · 1.08万 |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | 産業データアクセス向けの OPC クライアント。 | ★9 · — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet ビル管理プロトコルスタック。 | ★8 · 9k |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | ゲートウェイとデバイス管理を備えた軽量なオープンソース IoT プラットフォーム。 | ★10 · — |
| **車載テレマティクス** | | |
| NewLife.JT808 | JT/T 808 車両位置情報プロトコル：70 以上のメッセージボディ、2011/2013/2019 に対応。 | — |
| NewLife.JT809 | 運輸管理部門向けの JT/T 809 プラットフォーム間プロトコル。 | — · 135 |
| NewLife.JT1078 | JT/T 1078 車載ビデオ：RTP 解析、FLV/fMP4 リマックス、ライブ配信。 | — |
| **ドメインコンポーネント** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Excel、Word、PPT、PDF など 15 以上のドキュメント形式を 1 パッケージで。 | ★11 · 1.29万 |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | クロスプラットフォーム音声：コーデック、デバイス IO、DSP、コンテナ、ストリーミング。 | ★5 · 6.6k |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | ビデオコーデック、コンテナ形式、フレーム処理、ストリーミング。 | ★2 · 610 |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 百度地図、高徳地図、テンセント地図、天地図を統一インターフェイス化。ジオコーディングと座標変換に対応。 | ★15 · 2.48万 |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 位置情報：オフラインのメモリマップデータベースとオンライン API のフォールバック。 | ★16 · 8.47万 |
| **ツールと製品** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 開発者向けツールボックス：ネットワーク、シリアルポート、暗号化、正規表現、Modbus、MQTT。 | ★330 · 4.21万 |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | セルフホスト型 Git ソースコード管理システム。 | ★49 · — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 古いホストから起動した際に対応する .NET ランタイムを自動インストールします。 | ★13 · — |

> ここに記載していない 30 以上のパッケージ（FTP、LoRa、Office 拡張、各種プロトコルドライバーなど）は [NewLifeX 組織](https://github.com/NewLifeX) と [newlifex.com](https://newlifex.com) で公開しています。

## ドキュメントとサンプル

### 利用ドキュメント

- [コアライブラリ目次](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md)：モジュール別に分類したコアコンポーネント記事のインデックス
- [AOT互換性分析](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md)：NativeAOT の対応範囲、実測結果（スモーク <!--aot-smoke:start-->31/31<!--aot-smoke:end-->）、RID マトリクスと既知の境界
- [多言語ローカライズ](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)：メッセージモデル、言語テーブル、下流での上書き、実行時の言語追加
- [下流の多言語対応チェックリスト](https://github.com/NewLifeX/X/blob/master/Doc/下游多语言接入清单.md)：統合手順、言語ルール、CI ゲート、バッジテンプレート
- [モバイル技術アーキテクチャ](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md)：.NET モバイルスタック、UI フレームワーク選定、APK パッケージング、AOT の注意点
- [Samples](https://github.com/NewLifeX/X/tree/master/Samples)：コンソール、サービス、Http、IoT、モバイルのサンプル

### リリースノート

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md)：バージョン履歴

## コントリビューション

### ビルドとテスト

ソリューションは Visual Studio（最新リリース）で開いてビルドでき、.NET CLI でもビルドできます。プロジェクトファイルはあえてシンプルに保っているため、クローン直後からそのままビルドできます。

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- 全量並列実行：`run-tests.ps1`（マルチホスト並列、タイムアウト保護、trx 集計）
- NativeAOT スモーク：`AotSmoke/`、CI は `.github/workflows/aot.yml`
- ベンチマーク：`Benchmark/`（BenchmarkDotNet）

### リポジトリ構成

| ディレクトリ | 説明 |
| --- | --- |
| `NewLife.Core/` | コアライブラリのソース（ログ、ネットワーク、キャッシュ、シリアライズなど） |
| `NewLife.Core.SourceGenerator/` | ソースジェネレーター。パッケージ内の `analyzers/` として同梱されます |
| `Doc/` | 機能ドキュメントと記事インデックス |
| `Samples/` | コンソール / サービス / Http / IoT / モバイルのサンプル |
| `Benchmark/` | BenchmarkDotNet のパフォーマンスベンチマーク |
| `XUnitTest.Core/` | メインの単体テスト（net10.0） |
| `XUnitTest.Compat/`、`XUnitTest.Net6/` | クロスバージョン互換性テスト |
| `AotSmoke/` | NativeAOT スモークプロジェクト |

### リリース

`v` で始まるタグ（例：`v12.0.2026.0927`）をプッシュすると `.github/workflows/publish.yml` が起動します。ワークフローはパッケージを作成して NuGet.org と GitHub Packages に公開し、`ChangeLog.md` の該当セクションからノートを取得した GitHub Release を作成します（アセットには nupkg と snupkg が含まれます）。手動実行ではパッケージの公開のみを行い、Release は作成しません。

### 開発ドキュメント

`Doc/` ディレクトリには、コードと同期した機能ドキュメント体系（要件 → 機能追跡 → アーキテクチャ設計）を維持しています。これらのドキュメントは中国語で記述されています。

- [要件ドキュメント](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md)：ビジョン、コア目標と機能要件
- [機能一覧](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md)：すべての機能ポイントの実装/テスト/コメントの状態追跡
- [アーキテクチャ設計](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md)：レイヤー構成、モジュールコンポーネントと重要な設計決定

### フィードバック

- 提出前に `dotnet build` と関連する単体テストが通ることを確認してください。コードスタイルとコミットメッセージはリポジトリの規約に従います。
- 不具合や提案は [Issue](https://github.com/NewLifeX/X/issues) へ。Star と PR も歓迎します。

## 新生命開発チーム

新生命チーム（NewLife）は 2002 年に設立され、.NET、IoT、分散システムに注力しています。オープンソースコンポーネントは NuGet で累計 <!--nuget-total:start-->6.04M<!--nuget-total:end--> 回ダウンロードされ（2026-10 時点）、電力、産業、交通、環境保護、医療、文化・博物館などのプロジェクトで利用されています。

ウェブサイト：https://newlifex.com · オープンソース組織：https://github.com/NewLifeX  
WeChat 公式アカウント：  
![智能大石头](https://newlifex.com/stone.jpg)

## ライセンス

本プロジェクトは [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE) ライセンスです。強名付け証明書 `newlife.snk` を公開しているため、自身でビルドしてアセンブリを置き換えられます。ソースを改変して再配布する際に出典表示は不要です。

> このプロジェクトがお役に立てば、ぜひ Star を付け、チームに共有するか、Issue/PR で改善提案をお寄せください。
