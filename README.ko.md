# NewLife.Core - 핵심 라이브러리

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [العربية](README.ar.md)
<!-- lang:end -->

**NewLife.Core**는 NewLife 생태계의 기반입니다. 로깅, 구성, 캐시, 네트워크, 직렬화, 스케줄링, 객체 컨테이너, APM 추적을 하나의 패키지로 제공합니다. NativeAOT를 지원하며 <!--locale-count:start-->31<!--locale-count:end-->개 언어의 런타임 메시지를 내장하고, 단일 패키지로 net45부터 net10.0까지 11개 대상 프레임워크를 지원하며 XCode, Cube, Redis, Stardust 등 30여 개 형제 프로젝트를 뒷받침합니다.

핵심 라이브러리 튜토리얼: [https://newlifex.com/core](https://newlifex.com/core)  

> ℹ️ 이 한국어판은 [English README](README.md)와 같은 시점에 유지 관리합니다. 두 내용이 다를 경우 영어판을 공식 기준으로 삼습니다.

**목차**: [빠른 시작](#빠른-시작) · [NewLife.Core를 선택하는 이유](#newlifecore를-선택하는-이유) · [주요 기능](#주요-기능) · [생태계 지도](#생태계-지도) · [문서와 예제](#문서와-예제) · [기여하기](#기여하기) · [뉴라이프 개발팀](#뉴라이프-개발팀) · [라이선스](#라이선스)

## 빠른 시작

### 설치

```bash
dotnet add package NewLife.Core
```

### 최소 예제

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // console log (also writes to file)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // in-memory cache
cache.Set("key", "value", 60);          // expires in 60 seconds
var value = cache.Get<String>("key");   // read back
```

### 템플릿 스캐폴딩

명령줄에서 다음 명령을 실행하면 NewLife 컴포넌트를 빠르게 체험할 수 있습니다:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### 플랫폼 지원

| 항목 | 지원 범위 |
| --- | --- |
| 대상 프레임워크 | 단일 패키지 11개: net45, net461, net462, netstandard2.0, netstandard2.1, net6.0, net6.0-windows, net8.0, net8.0-windows, net10.0, net10.0-windows |
| 운영체제와 아키텍처 | Windows, Linux, macOS, Android, iOS; x64, x86, arm64, arm, loongarch64, riscv64, s390x |
| NativeAOT | <!--aot-scenarios:start-->31<!--aot-scenarios:end-->개 핵심 시나리오 실측 완료, 다중 RID CI 스모크 매트릭스로 검증(win-x64, linux-x64, linux-arm64, linux-loongarch64는 차단 게이트). 플러그인과 스크립트 등 동적 기능은 설계상 AOT를 지원하지 않으며, 경계는 [AOT 호환성 분석](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md)을 참고하세요 |
| 런타임 지역화 | <!--locale-count:start-->31<!--locale-count:end-->개 언어 내장; 로그와 예외 메시지를 런타임에 지역화하며 호출 지점을 수정할 필요가 없습니다([다국어 지역화](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md) 참고) |
| 레거시 프레임워크 | net40: `dotnet add package NewLife.Core --version 10.10.2024.801-net40`(net40/net45/net461 포함), net20: `--version 10.10.2024.701-net20`(net20 전용). 둘 다 2024-08 동결 버전이며 메인라인 업데이트를 따르지 않습니다. |

## NewLife.Core를 선택하는 이유

- **공통 인프라를 하나의 패키지로**: 로그, 구성, 캐시, 네트워크, 직렬화, 스케줄링, 객체 컨테이너와 DI를 한 번의 참조로 사용할 수 있습니다.
- **폭넓은 지원 범위와 현대적인 런타임**: 단일 패키지로 11개 대상 프레임워크(net45 → net10.0, Windows 변형 포함)를 지원하고, 핵심 경로는 NativeAOT를 지원하며, 런타임 메시지는 <!--locale-count:start-->31<!--locale-count:end-->개 언어로 지역화되어 있어 레거시 시스템과 신규 프로젝트가 동일한 API를 공유합니다.
- **생태계 규모로 검증**: 동일한 기반이 XCode(단일 테이블 수백억 건), Net(TCP 1KB에서 177만 pps, 14.5 Gbps), Redis와 Stardust(수백억 건 데이터)를 뒷받침합니다.
- **절제된 의존성, 관대한 라이선스**: BCL과 Microsoft 호환 패키지만 사용하며, MIT 라이선스이므로 소스 수정과 사명 변경이 가능합니다([라이선스](#라이선스)).

## 주요 기능

<details>
<summary><b>기본 확장</b> (10)</summary>

| 기능 | 주요 특징 |
| --- | --- |
| [타입 변환Utility](https://newlifex.com/core/utility) | 효율적인 타입 변환을 제공하고 다양한 형식과 예외 처리를 지원합니다. |
| [문자열 확장StringHelper](https://newlifex.com/core/string_helper) | 자르기, 인코딩, 서식 지정 등 다양한 문자열 작업을 포함합니다. |
| [경로 확장PathHelper](https://newlifex.com/core/path_helper) | 다양한 경로 형식을 지능적으로 처리하여 코드의 크로스 플랫폼 호환성을 구현합니다. |
| [데이터 확장IOHelper](https://newlifex.com/core/io_helper) | IO 작업을 최적화하고 효율적인 데이터 스트림과 바이트 배열 변환을 제공합니다. |
| [프로세스 확장ProcessHelper](https://newlifex.com/core/process_helper) | 프로세스 작업을 단순화하고 프로세스 정보 조회와 관리를 위한 편리한 메서드를 제공합니다. |
| [런타임 정보Runtime](https://newlifex.com/core/runtime) | 애플리케이션 런타임 정보를 얻는 간단한 방법을 제공하여 디버깅과 진단에 유용합니다. |
| [삭제 가능DisposeBase](https://newlifex.com/core/disposebase) | 표준 리소스 해제 패턴을 제공하여 메모리와 리소스 누수를 효과적으로 방지합니다. |
| [리플렉션 확장Reflect](https://newlifex.com/core/reflect) | 고성능 리플렉션 도구로 객체 멤버 읽기/쓰기를 빠르게 수행합니다. |
| [보안 확장SecurityHelper](https://newlifex.com/core/security_helper) | RSA/AES/DES/MD5 등 다양한 암호화/복호화를 통합하여 보안 작업을 단순화합니다. |
| [경위도 해시GeoHash](https://newlifex.com/core/geo_hash) | 경위도를 문자열로 인코딩하여 주변 위치 검색에 사용합니다. |

</details>

<details>
<summary><b>로깅 및 추적</b> (2)</summary>

| 기능 | 주요 특징 |
| --- | --- |
| [로그ILog](https://newlifex.com/core/log) | 다양한 로그 출력 방식을 지원하고 강력한 비동기 쓰기 기능을 제공합니다. |
| [추적ITracer](https://newlifex.com/core/tracer) | 강력한 APM 성능 추적으로 Stardust 플랫폼과의 연동을 지원합니다. |

</details>

<details>
<summary><b>예약 스케줄링</b> (3)</summary>

| 기능 | 주요 특징 |
| --- | --- |
| [고급 타이머TimerX](https://newlifex.com/core/timerx) | 고정밀 타이머로 Cron 스케줄링과 비동기 주기 실행을 지원합니다. |
| [Cron 식](https://newlifex.com/core/cron) | Linux Cron과 유사한 예약 작업을 제공하고 초 단위 매칭을 지원합니다. |
| [시간 공급자TimeProvider](https://newlifex.com/core/timerx_timeprovider) | 통일된 시간 추상화와 고정밀 경과 시간 계산을 제공하여 크로스 버전 호환에 편리합니다. |

</details>

<details>
<summary><b>구성 시스템</b> (4)</summary>

| 기능 | 주요 특징 |
| --- | --- |
| [구성 시스템Config](https://newlifex.com/core/config) | 통합 구성 프레임워크로 다중 형식, 다중 원본, 핫 리로드를 지원합니다. |
| [구성 공급자IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | 통합 구성 인터페이스로 콜백과 구성 노드 모델을 지원합니다. |
| [핵심 구성Setting](https://newlifex.com/core/setting) | 컴포넌트 핵심 구성을 관리하고 로그, 네트워크 등 기본 동작을 제어합니다. |
| [시스템 구성SysConfig](https://newlifex.com/core/sysconfig) | 애플리케이션 시스템 구성을 관리하고 로컬, 분산, 암호화 구성을 지원합니다. |

</details>

<details>
<summary><b>직렬화</b> (7)</summary>

| 기능 | 주요 특징 |
| --- | --- |
| [Json 직렬화](https://newlifex.com/core/json) | 고성능 Json 도구로 다양한 복잡한 시나리오의 직렬화를 지원합니다. |
| [Xml 직렬화](https://newlifex.com/core/xml) | XML 직렬화와 역직렬화를 제공하고 객체와 XML 간 변환을 지원합니다. |
| [이진 직렬화](https://newlifex.com/core/binary) | 고성능 이진 직렬화로 복잡한 객체 그래프와 버전 관리를 지원합니다. |
| [고급 이진 직렬화](https://newlifex.com/core/advanced_binary) | 프로토콜 메시지 전체에 걸친 무할당 인코딩/디코딩 고급 가이드입니다. |
| [Csv 파서CsvFile](https://newlifex.com/core/csv_file) | CSV 파일을 빠르게 읽고 쓰며 사용자 지정 구분자와 데이터 바인딩을 지원합니다. |
| [Csv 데이터베이스CsvDb](https://newlifex.com/core/csv_db) | CSV 파일을 데이터베이스로 사용하며 기본 생성, 읽기, 수정, 삭제를 지원합니다. |
| [경량 Excel 리더ExcelReader](https://newlifex.com/core/excel_reader) | Office 의존성 없이 Excel을 빠르게 읽고 대용량 데이터를 지원합니다. |

</details>

<details>
<summary><b>캐시</b> (7)</summary>

| 기능 | 주요 특징 |
| --- | --- |
| [통합 캐시 인터페이스ICache](https://newlifex.com/core/icache) | 표준 캐시 작업을 정의하고 메모리, Redis 등 다양한 구현을 제공합니다. |
| [캐시 아키텍처ICacheProvider](https://newlifex.com/core/icacheprovider) | ICache 인스턴스를 관리하고 제공하며 여러 캐시의 공존을 지원합니다. |
| [메모리 캐시MemoryCache](https://newlifex.com/core/memory_cache) | 고성능 단일 노드 메모리 캐시로 만료와 용량 정책을 지원합니다. |
| [객체 풀ObjectPool](https://newlifex.com/core/object_pool) | 비용이 큰 객체를 재사용하여 성능을 높이고 GC 부담을 줄입니다. |
| [객체 풀Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | CAS 기반의 고성능 범용 객체 풀로 GC 부담을 줄입니다. |
| [분산 캐시NewLife.Redis](https://newlifex.com/core/redis) | Redis 기반 분산 캐시 구현으로 다양한 데이터 구조를 지원합니다. |
| [벡터 저장소IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | 벡터 컬렉션 관리 인터페이스로 Top-K 유사도 검색을 지원하며, AI 애플리케이션에 벡터 검색 기반을 제공합니다. |

</details>

<details>
<summary><b>고성능 버퍼</b> (7)</summary>

| 기능 | 주요 특징 |
| --- | --- |
| [데이터 패킷IPacket](https://newlifex.com/core/packet) | 제로 카피 슬라이스와 체인 패킷을 위한 핵심 데이터 패킷 인터페이스입니다. |
| [버퍼Buffers](https://newlifex.com/core/buffers) | 고성능 버퍼 작업 라이브러리로 Span과 풀링 쓰기 등 컴포넌트를 모았습니다. |
| [풀링 작성기PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | ArrayPool 기반 풀링 버퍼 작성기로 동적 확장을 지원합니다. |
| [Span 판독기SpanReader](https://newlifex.com/core/span_reader) | 무할당 고성능 이진 스트림 파서로 빅/리틀 엔디언을 지원합니다. |
| [Span 작성기SpanWriter](https://newlifex.com/core/span_writer) | 무할당 고성능 이진 데이터 작성기로 빅/리틀 엔디언을 지원합니다. |
| [Span 도우미SpanHelper](https://newlifex.com/core/span_helper) | Span/ReadOnlySpan 확장 메서드 모음으로 메모리 작업을 단순화합니다. |
| [시퀀스 판독기SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | 세그먼트 간 순차 판독기(구버전 프레임워크는 심으로 보완, 전체 11개 TFM 사용 가능). |

</details>

<details>
<summary><b>애플리케이션 프레임워크</b> (15)</summary>

| 기능 | 주요 특징 |
| --- | --- |
| [객체 컨테이너ObjectContainer](https://newlifex.com/core/object_container) | 경량 종속성 주입으로 다양한 수명 주기와 주입 모드를 지원합니다. |
| [경량 애플리케이션 호스트Host](https://newlifex.com/core/host) | 애플리케이션 호스팅을 단순화하여 장시간 실행되는 백그라운드 서비스를 손쉽게 구축합니다. |
| [플러그인 프레임워크IPlugin](https://newlifex.com/core/plugin) | 표준화된 플러그인 관리 메커니즘을 제공하여 애플리케이션 모듈화 개발을 구현합니다. |
| [병렬 모델Actor](https://newlifex.com/core/actor) | 단순화된 동시성 프로그래밍 모델로 메시지 큐 처리를 통해 처리량을 높입니다. |
| [지연 큐DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | 빈번한 변경을 집계하여 일괄 처리로 반영하고 쓰기 증폭을 줄입니다. |
| [이벤트 버스EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | 프로세스 내 이벤트 분배로 토픽 라우팅과 큐 방식 메시지를 지원합니다. |
| [약한 참조 대리자WeakAction](https://newlifex.com/core/weak_action) | 이벤트 구독 메모리 누수 문제를 해결하고 안전한 이벤트 처리를 구현합니다. |
| [스크립트 엔진ScriptEngine](https://newlifex.com/core/script_engine) | C# 동적 컴파일 실행을 지원하고 유연한 스크립트 확장 기능을 제공합니다. |
| [압축/해제](https://newlifex.com/core/compression) | 다양한 압축 알고리즘을 통합하고 통합 인터페이스로 데이터를 압축합니다. |
| [압축 아카이브Tar와 7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Tar/Tar.Gz와 7Zip 고급 아카이브 압축 사용법을 다룹니다. |
| [머신 정보MachineInfo](https://newlifex.com/core/machine_info) | 하드웨어 정보를 종합적으로 얻으며 Windows와 Linux 시스템을 지원합니다. |
| [병음 라이브러리PinYin](https://newlifex.com/core/pinyin) | 효율적인 한자-병음 변환 라이브러리로 전체 병음과 약어 병음을 지원하며 성능이 뛰어납니다. |
| [데이터셋DbTable](https://newlifex.com/core/dbtable) | 메모리 내 데이터 테이블로 데이터 처리와 분석을 편리하게 합니다. |
| [페이징 매개변수PageParameter](https://newlifex.com/core/page_parameter) | 페이징 로직을 캡슐화하여 Web과 API 등 다양한 시나리오의 페이징을 지원합니다. |
| [스노플레이크 알고리즘Snowflake](https://newlifex.com/core/snow_flake) | 분산 고유 ID 생성 알고리즘으로 높은 동시성에서 ID 고유성을 보장합니다. |

</details>

<details>
<summary><b>보안 및 토큰</b> (3)</summary>

| 기능 | 주요 특징 |
| --- | --- |
| [분산 디지털 서명 토큰TokenProvider](https://newlifex.com/core/token_provider) | 분산 토큰을 생성하고 검증하며 여러 노드 간 보안을 보장합니다. |
| [Web 범용 토큰JwtBuilder](https://newlifex.com/core/jwt) | JWT 토큰의 생성, 디코딩, 검증을 제공하여 Web 인증을 단순화합니다. |
| [토큰 HTTP 필터TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | HTTP 요청에 토큰을 자동 주입하고 401/403을 가로채 자동 갱신합니다. |

</details>

<details>
<summary><b>네트워크 라이브러리</b> (13)</summary>

| 기능 | 주요 특징 |
| --- | --- |
| [네트워크 서버NetServer](https://newlifex.com/core/netserver) | 고성능 TCP/UDP/Unix 도메인 소켓 서버로 세션과 데이터 처리를 캡슐화했습니다. |
| [네트워크 클라이언트NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | 통합 TCP/UDP/Unix 네트워크 클라이언트로 연결과 송수신을 단순화합니다. |
| [메시지IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | 요청-응답 메시지 계약으로 방향 의미와 프레임 읽기/쓰기를 담당합니다. |
| [메시지 프로토콜 스택MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | 프레임 펌프 + 무상태 코덱 단일 스택으로 대형 프레임 스트리밍 본문과 백프레셔를 지원합니다. |
| [데이터 파이프Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | 제로 카피 데이터 파이프로 System.IO.Pipelines 형태에 맞췄습니다. |
| [간이 원격 메시지 교환 프로토콜SRMP](https://newlifex.com/core/srmp) | 경량 RPC 프로토콜로 이진 직렬화와 UDP 전송을 지원합니다. |
| [부하 분산 및 장애 조치LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Failover/라운드 로빈/경쟁 세 가지 다중 노드 스케줄링 전략을 제공합니다. |
| [HTTP 클라이언트ApiHttpClient](https://newlifex.com/core/api_http) | HttpClient 기반으로 캡슐화하여 Web API 호출과 처리를 단순화합니다. |
| [HTTP 서버HttpServer(임베디드 장치)](https://newlifex.com/core/httpserver) | 경량 Http 서버로 미들웨어 파이프라인, 매개변수화 라우팅과 정적 파일 서비스를 지원합니다. |
| [네트워크 다운로드WebClientX](https://newlifex.com/core/webclient) | 향상된 WebClient로 멀티스레드와 이어받기 다운로드를 지원합니다. |
| [WebSocket 서버(IoT 하향 알림 도구)](https://newlifex.com/core/websocket) | 고성능 WebSocket 서버로 IoT 메시지 푸시에 적합합니다. |
| [WebSocket 양방향 통신](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | 핸드셰이크, 프레임 처리, 하트비트, 연결 종료 실무를 소개합니다. |
| [파일 저장소 클라이언트EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | EasyIO 또는 OSS에 연동하는 HTTP 파일 저장소 클라이언트입니다. |

</details>

> RPC(`ApiServer`/`ApiClient`)는 [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting)(으)로 이동되었습니다.

## 생태계 지도

NewLife는 독립적으로 버전이 관리되는 오픈소스 프로젝트의 계층형 생태계입니다. 아래 지도는 모든 NewLife 리포지토리에서 그대로 동일하게 공유되며, 각 프로젝트는 코드 리포지토리와 기본 NuGet 패키지로 연결됩니다.

| 프로젝트 | 한 줄 소개 | 다운로드 |
| --- | --- | --- |
| **기반** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | 로깅, 구성, 캐시, 네트워크, 직렬화, 스케줄링, APM 추적. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 데이터 미들웨어: 단일 테이블 수백억 건, 자동 분할 테이블, 읽기/쓰기 분리, 10여 종 데이터베이스 지원. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 고성능 네트워크 라이브러리: 단일 노드 177만 pps, 14.5 Gbps(TCP 1KB, 2026-10 실측), 400만 Tcp 장기 연결. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | CS 애플리케이션과 IoT 장치를 위한 프로토콜 통신 및 RPC 프레임워크. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | .NET 애플리케이션을 운영체제 데몬으로 설치: Windows 서비스와 Linux systemd. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | 콘솔, Web, API, 서비스, 데이터, IoT용 `dotnet new` 프로젝트 템플릿. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **데이터 및 메시징** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis 클라이언트: 마이크로초 수준 지연, 백만 수준 처리량, 메시지 큐 내장. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Apache RocketMQ와 알리바바 클라우드 메시지 큐를 지원하는 순수 관리형 RocketMQ 클라이언트. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT 클라이언트와 서버, 알리바바 클라우드 IoT 연결 포함. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 하이브리드 데이터베이스: 임베디드/네트워크 두 모드, 관계형, 시계열, 메시지 큐, KV를 하나로. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | NewLife 데이터 스택 위에 구축한 MySql 드라이버와 도우미 계층. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, AI 및 분산** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 빠른 관리 플랫폼 프레임워크: 권한, SSO, OAuth 서버, CRUD 스캐폴딩. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | 분산 서비스 플랫폼: 노드 관리, APM, 구성 센터, 등록 센터, 배포 센터. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | 샤딩, 재시도, 백필을 지원하는 분산 작업 및 실시간 데이터 스케줄링 플랫폼. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 46개 LLM 공급자를 통합 연결하는 클라이언트와 바로 사용할 수 있는 ChatAI 웹 채팅 애플리케이션. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **산업 및 IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 프로토콜 계약과 장치 추상을 정의하는 IoT 표준 라이브러리. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | NewLife.IoT 기반으로 구현한 Modbus TCP/RTU/ASCII. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | NewLife.IoT 기반으로 구현한 지멘스 PLC 프로토콜. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | NewLife.IoT 기반으로 구현한 오므론 PLC 프로토콜. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | NewLife.IoT 기반으로 구현한 미쓰비시 Melsec PLC 프로토콜. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | 산업 데이터 수집용 OPC 클라이언트. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet 빌딩 자동화 프로토콜 스택. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | 게이트웨이와 장치 관리를 갖춘 경량 오픈소스 IoT 플랫폼. | — |
| **차량 텔레매틱스** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 차량 위치 프로토콜: 70개 이상 메시지 본문, 2011/2013/2019 지원. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | 운송 관리 부서를 위한 JT/T 809 플랫폼 간 프로토콜. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 차량용 비디오: RTP 파싱, FLV/fMP4 리먹싱, 라이브 스트리밍. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **도메인 컴포넌트** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Excel, Word, PPT, PDF 등 15개 이상 문서 형식을 하나의 패키지로. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 크로스 플랫폼 오디오: 코덱, 장치 IO, DSP, 컨테이너, 스트리밍. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | 비디오 코덱, 컨테이너 형식, 프레임 처리, 스트리밍. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 바이두/고덕/텐센트/톈디투 지도 통합 인터페이스, 지오코딩과 좌표 변환 지원. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 위치 정보: 오프라인 메모리 매핑 데이터베이스와 온라인 API 이중 소스. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **도구 및 제품** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 개발자 도구 모음: 네트워크, 시리얼 포트, 암호화, 정규식, Modbus, MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 셀프 호스팅 Git 소스 코드 관리 시스템. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 구버전 호스트에서 시작할 때 일치하는 .NET 런타임을 자동으로 설치합니다. | — |

> 배지는 각 프로젝트의 기본 패키지를 나타냅니다. 테마, 확장, 위성 패키지는 해당 NuGet 페이지에서 확인하세요. 여기에 나열하지 않은 30여 개 패키지(FTP, LoRa, Office 확장, 각종 프로토콜 드라이버 등)는 [NewLifeX 조직](https://github.com/NewLifeX)과 [newlifex.com](https://newlifex.com)에서 확인할 수 있습니다.

## 문서와 예제

### 사용 문서

- [핵심 라이브러리 목차](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): 모듈별로 분류한 핵심 컴포넌트 문서 색인
- [AOT 호환성 분석](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): NativeAOT 지원 범위, 실측 결론(스모크 <!--aot-smoke:start-->31/31<!--aot-smoke:end-->), RID 매트릭스와 알려진 경계
- [다국어 지역화](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md): 지원 언어와 기본 사용법
- [모바일 아키텍처](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md): .NET 모바일 스택, UI 프레임워크 선정, APK 패키징, AOT 참고 사항
- [Samples](https://github.com/NewLifeX/X/tree/master/Samples): 콘솔, 서비스, Http, IoT, 모바일 예제

### 릴리스 노트

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md): 버전 변경 기록

## 기여하기

### 빌드 및 테스트

솔루션은 Visual Studio(최신 정식 버전)에서 열어 빌드할 수 있으며 .NET CLI로도 빌드됩니다. 프로젝트 파일은 의도적으로 단순하게 유지하여 새로 클론한 뒤 바로 빌드할 수 있습니다.

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- 전체 병렬 실행: `Tools/Scripts/run-tests.ps1`(멀티 호스트 병렬, 타임아웃 보호, trx 집계)
- NativeAOT 스모크: `AotSmoke/`, CI는 `.github/workflows/aot.yml`
- 벤치마크: `Benchmark/`(BenchmarkDotNet)

### 리포지토리 구조

| 디렉터리 | 설명 |
| --- | --- |
| `NewLife.Core/` | 핵심 라이브러리 소스(로깅, 네트워크, 캐시, 직렬화 등) |
| `NewLife.Core.SourceGenerator/` | 소스 생성기, 패키지 내 `analyzers/`로 배포 |
| `Doc/` | 기능 문서와 아티클 색인 |
| `Samples/` | 콘솔 / 서비스 / Http / IoT / 모바일 예제 |
| `Benchmark/` | BenchmarkDotNet 성능 벤치마크 |
| `XUnitTest.Core/` | 주요 단위 테스트(net10.0) |
| `XUnitTest.Compat/`, `XUnitTest.Net6/` | 크로스 버전 호환성 테스트 |
| `AotSmoke/` | NativeAOT 스모크 프로젝트 |

### 릴리스

`v` 접두사 태그(예: `v12.0.2026.1024`)를 푸시하면 `.github/workflows/publish.yml`이 실행됩니다. 워크플로는 패키지를 만들어 NuGet.org와 GitHub Packages에 게시하고, `ChangeLog.md`의 해당 섹션에서 노트를 가져온 GitHub Release를 생성합니다(자산에는 nupkg와 snupkg가 포함됩니다). 수동 실행은 패키지만 게시하며 Release는 만들지 않습니다.

### 개발 문서

`Doc/` 디렉터리는 코드와 동기화된 기능 문서 체계(요구사항 → 기능 추적 → 아키텍처 설계)를 유지합니다. 이 문서들은 중국어로 작성되어 있습니다.

- [요구사항 문서](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): 비전, 핵심 목표와 기능 요구사항
- [기능 목록](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): 모든 기능 항목의 구현/테스트/주석 상태 추적
- [아키텍처 설계](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): 계층, 모듈 컴포넌트와 핵심 설계 결정

### 피드백

- 제출 전에 `dotnet build`와 관련 단위 테스트가 통과하는지 확인하세요. 코드 스타일과 커밋 메시지는 저장소 규약을 따릅니다.
- 버그와 제안은 [Issue](https://github.com/NewLifeX/X/issues)로 알려 주세요. Star와 PR도 환영합니다.

## 뉴라이프 개발팀

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

뉴라이프 팀(NewLife)은 2002년에 설립되어 20여 년간 .NET과 IoT 기술 생태계에 집중하며 100여 개의 오픈소스 프로젝트를 주도하고(이 중 34개가 핵심 프로젝트), 전력, 물류, 의료, 산업 제어, 인터넷 등 산업에 소프트웨어와 하드웨어를 아우르는 통합 솔루션을 제공해 왔습니다. "학문에는 선후가 없으며, 이룬 사람이 스승이다"라는 신조를 따릅니다.

오픈소스 컴포넌트는 NuGet에서 누적 <!--nuget-total:start-->6.48M<!--nuget-total:end-->회 다운로드되었습니다(생태계 전체 기준, 2026-10).

웹사이트: https://newlifex.com · 오픈소스 조직: https://github.com/NewLifeX

<details>
<summary>위챗 공식 계정: 智能大石头(스캔하여 팔로우)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## 라이선스

이 프로젝트는 [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE) 라이선스를 따릅니다. 강력한 이름 인증서 `newlife.snk`를 공개하여 직접 빌드하고 어셈블리를 교체할 수 있으며, 소스를 수정해 출처 표시 없이 다시 배포할 수 있습니다.

> 이 프로젝트가 도움이 되셨다면 Star를 눌러 주시고, 팀에 공유하거나 Issue/PR로 개선 제안을 남겨 주세요.
