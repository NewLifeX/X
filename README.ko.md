# NewLife.Core - 핵심 라이브러리

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![AtomGit Stars](https://atomgit.com/NewLifeX/X/star/badge.svg)](https://atomgit.com/NewLifeX/X)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=v12)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)

🌐 [中文](Readme.MD) | [English](README.en.md) | [日本語](README.ja.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Русский](README.ru.md) | [Português](README.pt.md) | [العربية](README.ar.md)

NewLife 프레임워크는 포괄적인 .NET 컴포넌트 생태계로, 확장 가능한 애플리케이션을 구축하기 위한 고성능 기반 인프라를 제공합니다. 로깅, 네트워크, 직렬화, 캐시, 멀티스레딩 등의 기본 기능을 제공하며, 웹 서비스부터 IoT 장치에 이르기까지 다양한 애플리케이션의 기반이 됩니다.  

핵심 라이브러리 튜토리얼: [https://newlifex.com/core](https://newlifex.com/core)  

> ⚠️ 이 번역은 업데이트가 늦어질 수 있습니다. 정식 내용은 [중국어 README](Readme.MD)를 기준으로 합니다.



## 프레임워크 개요

NewLife는 여러 컴포넌트 라이브러리로 구성되어 있으며, NewLife.Core가 기반을 제공합니다. 이 프레임워크는 net45~net10.0 다중 대상 프레임워크(.NET Framework 4.5 ~ .NET 10.0)를 지원하여 다양한 환경에서 폭넓은 호환성을 실현합니다.

**NativeAOT**: 핵심 경로는 지원 완료(25개 시나리오 실측 + CI 회귀 방지). 플러그인, 스크립트 등 동적 기능은 설계상 지원하지 않으며, 경계는 [AOT 호환성 분석](Doc/AOT兼容性分析.md) (中文) 문서를 참조하세요.



## 문서 색인

저장소의 `Doc/` 디렉터리는 역량 문서 체계(요구 정의 → 기능 추적 → 아키텍처 설계)를 유지합니다:

- [요구사항 문서](Doc/需求文档.md) (中文): 비전, 핵심 목표 및 기능 요구사항
- [기능 목록](Doc/功能清单.md) (中文): 모든 기능 항목의 구현/테스트/주석 상태 추적
- [아키텍처 설계](Doc/架构设计.md) (中文): 계층, 모듈 컴포넌트 및 핵심 설계 결정
- [핵심 라이브러리 목차](Doc/核心库目录.md) (中文): 핵심 컴포넌트 문서 색인
- [AOT 호환성 분석](Doc/AOT兼容性分析.md) (中文): NativeAOT 지원 범위, 실측 결론(스모크 29/29), 사용 지침 및 알려진 경계

## 빠르게 시작하기

NewLife 컴포넌트를 사용하는 가장 간편한 방법은 NuGet에서 참조하는 것입니다. 예를 들어 프로젝트의 NuGet 관리에서 `NewLife.Core`를 검색하여 추가하면 됩니다.

NewLife 컴포넌트는 커뮤니티가 20년 넘게 함께 만들어 왔으며 MIT 오픈소스 라이선스를 사용하므로, **누구나 임의로 수정하고 다시 배포할 수 있습니다**(출처 표시 불필요)! 많은 기업이 이를 기반으로 내부 개발 프레임워크를 구축할 때, 소스 코드의 모든 `NewLife` 문자열을 자사 이름으로 일괄 교체하여 사설화 맞춤 구성까지 구현할 수 있습니다.

팀은 항상 개방적인 태도를 견지하며, VisualStudio(최신 정식 버전)로 솔루션을 열어 컴파일하는 것뿐 아니라 `dotnet build` 명령줄 컴파일도 호환합니다. 프로젝트 파일은 복잡한 기능을 배제하여 단순하고 사용하기 쉬움을 추구하며, 그야말로 개봉 즉시 사용(out of the box)을 실현합니다.

우리는 독립적으로 어셈블리를 컴파일하여 교체할 수 있도록 강력한 이름 키 `newlife.snk`를 공개합니다.



명령줄에서 다음 명령을 실행하면 NewLife 컴포넌트를 빠르게 체험할 수 있습니다:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```



## 주요 기능


| 모듈 분류            | 기능                                                                         | 주요 특징                                        |
| -------------------- | ---------------------------------------------------------------------------- | ------------------------------------------------ |
| **기본 확장**        | [타입 변환Utility](https://newlifex.com/core/utility)                        | 효율적인 타입 변환을 제공하고 다양한 형식과 예외 처리를 지원합니다. |
|                      | [문자열 확장StringHelper](https://newlifex.com/core/string_helper)           | 자르기, 인코딩, 서식 지정 등 다양한 문자열 작업을 포함합니다. |
|                      | [경로 확장PathHelper](https://newlifex.com/core/path_helper)                 | 다양한 경로 형식을 지능적으로 처리하여 코드의 크로스 플랫폼 호환성을 구현합니다. |
|                      | [데이터 확장IOHelper](https://newlifex.com/core/io_helper)                   | IO 작업을 최적화하고 효율적인 데이터 스트림과 바이트 배열 변환을 제공합니다. |
|                      | [프로세스 확장ProcessHelper](https://newlifex.com/core/process_helper)       | 프로세스 작업을 단순화하고 프로세스 정보를 가져오고 관리하는 편리한 방법을 제공합니다. |
|                      | [런타임 정보Runtime](https://newlifex.com/core/runtime)                      | 애플리케이션 런타임 정보를 얻는 간단한 방법을 제공하여 디버깅과 진단을 돕습니다. |
|                      | [해제 가능DisposeBase](https://newlifex.com/core/disposebase)                | 표준 리소스 해제 패턴을 제공하여 메모리 및 리소스 누수를 효과적으로 방지합니다. |
|                      | [리플렉션 확장Reflect](https://newlifex.com/core/reflect)                    | 고성능 리플렉션 도구로 객체 멤버의 읽기/쓰기 작업을 빠르게 수행합니다. |
|                      | [보안 확장SecurityHelper](https://newlifex.com/core/security_helper)         | RSA/AES/DES/MD5 등 다양한 암호화/복호화를 통합하여 보안 작업을 단순화합니다. |
| **로깅 및 추적**     | [로깅ILog](https://newlifex.com/core/log)                                    | 다양한 로그 출력 방식을 지원하고 강력한 비동기 쓰기 기능을 제공합니다. |
|                      | [분산 추적ITracer](https://newlifex.com/core/tracer)                         | 강력한 APM 성능 추적을 제공하며 Stardust 플랫폼과의 통합을 지원합니다. |
| **예약 스케줄링**    | [고급 타이머TimerX](https://newlifex.com/core/timerx)                        | 고정밀 타이머로 Cron 스케줄링과 비동기 주기 실행을 지원합니다. |
|                      | [Cron 표현식](https://newlifex.com/core/cron)                                | Linux Cron과 유사한 예약 작업을 제공하며 초 단위 일치를 지원합니다. |
|                      | [시간 제공자TimeProvider](/core/time_provider)                               | 통합 시간 추상화와 고정밀 경과 시간 계산을 제공하여 버전 간 호환성을 돕습니다. |
| **구성 시스템**      | [구성 시스템Config](https://newlifex.com/core/config)                        | 통합 구성 프레임워크로 다중 형식, 다중 소스, 핫 리로드 메커니즘을 지원합니다. |
|                      | [구성 공급자IConfigProvider](/core/config_provider)                          | 통합 구성 인터페이스로 콜백과 구성 노드 모델을 지원합니다. |
|                      | [핵심 구성Setting](https://newlifex.com/core/setting)                        | 컴포넌트 핵심 구성을 관리하고 로깅, 네트워크 등 기본 동작을 제어합니다. |
|                      | [시스템 구성SysConfig](https://newlifex.com/core/sysconfig)                  | 애플리케이션 시스템 구성을 관리하고 로컬, 분산 및 암호화 구성을 지원합니다. |
| **직렬화**           | [Json 직렬화](https://newlifex.com/core/json)                                | 고성능 Json 도구로 다양한 복잡한 시나리오의 직렬화를 지원합니다. |
|                      | [Xml 직렬화](https://newlifex.com/core/xml)                                  | XML 직렬화와 역직렬화를 제공하고 객체와 XML 간 상호 변환을 지원합니다. |
|                      | [이진 직렬화](https://newlifex.com/core/binary)                              | 고성능 이진 직렬화로 복잡한 객체 그래프와 버전 관리를 지원합니다. |
|                      | [고급 이진 직렬화](/core/binary_advanced)                                    | 프로토콜 메시지 전 과정 무할당 인코딩/디코딩 심화 가이드입니다. |
|                      | [Csv 구문 분석CsvFile](https://newlifex.com/core/csv_file)                   | CSV 파일을 빠르게 읽고 쓰며 사용자 지정 구분자와 데이터 바인딩을 지원합니다. |
|                      | [Csv 데이터베이스CsvDb](https://newlifex.com/core/csv_db)                    | CSV 파일을 데이터베이스로 사용하며 기본적인 추가/삭제/수정/조회를 지원합니다. |
|                      | [경량 Excel 판독기ExcelReader](https://newlifex.com/core/excel_reader)       | Office 의존성 없이 Excel을 빠르게 읽으며 대용량 데이터를 지원합니다. |
| **캐시**             | [통합 캐시 인터페이스ICache](https://newlifex.com/core/icache)               | 표준 캐시 작업을 정의하고 메모리, Redis 등 다양한 구현을 제공합니다. |
|                      | [캐시 아키텍처ICacheProvider](https://newlifex.com/core/icacheprovider)      | ICache 인스턴스를 관리하고 제공하며 다중 캐시 공존을 지원합니다. |
|                      | [메모리 캐시MemoryCache](https://newlifex.com/core/memory_cache)             | 고성능 단일 노드 메모리 캐시로 만료 및 용량 정책을 지원합니다. |
|                      | [객체 풀ObjectPool](https://newlifex.com/core/object_pool)                   | 비용이 큰 객체를 재사용하여 성능을 높이고 GC 압력을 줄입니다. |
|                      | [객체 풀Pool](/core/pool)                                                    | CAS 기반의 고성능 범용 객체 풀로 GC 압력을 낮춥니다. |
|                      | [분산 캐시NewLife.Redis](https://newlifex.com/core/redis)                    | Redis 기반 분산 캐시 구현으로 다양한 데이터 구조를 지원합니다. |
|                      | [벡터 저장소IVectorStore](/core/vector_store)                                | 벡터 컬렉션 관리 인터페이스로 Top-K 유사도 검색을 지원하며, AI 애플리케이션에 벡터 검색 인프라를 제공합니다. |
| **고성능 버퍼**      | [데이터 패킷IPacket](https://newlifex.com/core/packet)                       | 제로 카피 슬라이싱과 체인 패킷의 핵심 데이터 패킷 인터페이스입니다. |
|                      | [버퍼Buffers](/core/buffers)                                                 | 고성능 버퍼 작업 라이브러리로 Span과 풀링 쓰기 등 컴포넌트를 모았습니다. |
|                      | [풀링 작성기PooledByteBufferWriter](/core/pooled_buffer_writer)              | ArrayPool 기반 풀링 버퍼 작성기로 동적 확장을 지원합니다. |
|                      | [Span 판독기SpanReader](/core/span_reader)                                   | 무할당 고성능 이진 스트림 파서로 빅/리틀 엔디언을 지원합니다. |
|                      | [Span 작성기SpanWriter](/core/span_writer)                                   | 무할당 고성능 이진 데이터 작성기로 빅/리틀 엔디언을 지원합니다. |
|                      | [Span 도우미SpanHelper](/core/span_helper)                                   | Span/ReadOnlySpan 확장 메서드 모음으로 메모리 작업을 단순화합니다. |
|                      | [시퀀스 판독기SequenceReader](/core/sequence_reader)                         | 세그먼트 간 순차 판독기(구버전 프레임워크는 심으로 보완, 전체 18개 TFM 사용 가능). |
| **애플리케이션 프레임워크** | [객체 컨테이너ObjectContainer](https://newlifex.com/core/object_container) | 경량 종속성 주입으로 다양한 수명 주기와 주입 모드를 지원합니다. |
|                      | [경량 애플리케이션 호스트Host](https://newlifex.com/core/host)               | 애플리케이션 호스팅을 단순화하여 장시간 실행되는 백그라운드 서비스를 손쉽게 구축합니다. |
|                      | [플러그인 프레임워크IPlugin](https://newlifex.com/core/plugin)               | 표준화된 플러그인 관리 메커니즘을 제공하여 애플리케이션 모듈화 개발을 구현합니다. |
|                      | [병렬 모델Actor](https://newlifex.com/core/actor)                            | 단순화된 동시성 프로그래밍 모델로 메시지 큐 처리를 통해 처리량을 높입니다. |
|                      | [파이프라인 모델Pipeline](/core/pipeline) ⚠️ 은퇴, 프로토콜 모드(프레임 펌프)로 대체 | 양방향 처리 체인 모델로 Read/Write/Open/Close 흐름을 통일합니다. |
|                      | [지연 큐DeferredQueue](/core/deferred_queue)                                 | 빈번한 변경을 집계하여 일괄 처리로 반영하고 쓰기 증폭을 줄입니다. |
|                      | [이벤트 버스EventBus](/core/eventbus)                                        | 프로세스 내 이벤트 분배로 토픽 라우팅과 큐 방식 메시지를 지원합니다. |
|                      | [약한 참조 대리자WeakAction](https://newlifex.com/core/weak_action)          | 이벤트 구독 메모리 누수 문제를 해결하고 안전한 이벤트 처리를 구현합니다. |
|                      | [스크립트 엔진ScriptEngine](https://newlifex.com/core/script_engine)         | C# 동적 컴파일 실행을 지원하고 유연한 스크립트 확장 기능을 제공합니다. |
|                      | [압축/해제](https://newlifex.com/core/compression)                           | 다양한 압축 알고리즘을 통합하고 통합 인터페이스로 데이터를 압축합니다. |
|                      | [압축 아카이브Tar와 7Zip](/core/tar_7zip)                                    | Tar/Tar.Gz와 7Zip 고급 아카이브 압축 사용법을 다룹니다. |
|                      | [머신 정보MachineInfo](https://newlifex.com/core/machine_info)               | 하드웨어 정보를 종합적으로 얻으며 Windows와 Linux 시스템을 지원합니다. |
|                      | [병음 라이브러리PinYin](https://newlifex.com/core/pinyin)                    | 효율적인 한자-병음 변환 라이브러리로 전체 병음과 약어 병음을 지원하며 성능이 뛰어납니다. |
|                      | [데이터셋DbTable](https://newlifex.com/core/dbtable)                         | 메모리 내 데이터 테이블로 데이터 처리와 분석을 편리하게 합니다. |
|                      | [페이징 매개변수PageParameter](https://newlifex.com/core/page_parameter)     | 페이징 로직을 캡슐화하여 Web과 API 등 다양한 시나리오의 페이징을 지원합니다. |
|                      | [스노플레이크 알고리즘Snowflake](https://newlifex.com/core/snow_flake)       | 분산 고유 ID 생성 알고리즘으로 높은 동시성에서 ID 고유성을 보장합니다. |
| **보안 및 토큰**     | [분산 디지털 서명 토큰TokenProvider](https://newlifex.com/core/token_provider) | 분산 토큰을 생성하고 검증하며 여러 노드 간 보안을 보장합니다. |
|                      | [Web 범용 토큰JwtBuilder](https://newlifex.com/core/jwt)                     | JWT 토큰의 생성, 디코딩, 검증을 제공하여 Web 인증을 단순화합니다. |
|                      | [토큰 HTTP 필터TokenHttpFilter](https://newlifex.com/core/token_http_filter) | HTTP 요청에 토큰을 자동 주입하고 401/403을 가로채 자동 갱신합니다. |
| **클라우드 서비스**  | [경위도 해시GeoHash](https://newlifex.com/core/geo_hash)                     | 경위도를 문자열로 인코딩하여 주변 위치 검색에 사용합니다. |
|                      | ⚠️ Baidu 지도/Amap 지도는 독립 NuGet 패키지로 추출되었습니다.                |                                                  |
| **네트워크 라이브러리** | [네트워크 서버NetServer](https://newlifex.com/core/netserver)              | 고성능 TCP/UDP/Unix 도메인 소켓 서버로 세션과 데이터 처리를 캡슐화했습니다. |
|                      | [네트워크 클라이언트NetClient](/core/netclient)                              | 통합 TCP/UDP/Unix 네트워크 클라이언트로 연결과 송수신을 단순화합니다. |
|                      | [메시지IMessage](/core/message)                                              | 요청-응답 메시지 계약으로 방향 의미와 프레임 읽기/쓰기를 담당합니다. |
|                      | [메시지 프로토콜 스택MessagePump](/core/message_protocol)                    | 프레임 펌프 + 무상태 코덱 단일 스택으로 대형 프레임 스트리밍 본문과 백프레셔를 지원합니다. |
|                      | [데이터 파이프Pipe](/core/packet_pipe)                                       | 제로 카피 데이터 파이프로 System.IO.Pipelines 형태에 맞췄습니다. |
|                      | [간이 원격 메시지 교환 프로토콜SRMP](https://newlifex.com/core/srmp)         | 경량 RPC 프로토콜로 이진 직렬화와 UDP 전송을 지원합니다. |
|                      | [부하 분산 및 장애 조치LoadBalancer](/core/load_balancer)                    | Failover/라운드 로빈/경쟁 세 가지 다중 노드 스케줄링 전략을 제공합니다. |
|                      | [RPC 통신ApiServer](https://newlifex.com/core/api_server)                    | ⚠️ [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting)(으)로 이동되었습니다. |
|                      | [RPC 통신ApiClient](https://newlifex.com/core/api_client)                    | ⚠️ [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting)(으)로 이동되었습니다. |
|                      | [HTTP 클라이언트ApiHttpClient](https://newlifex.com/core/api_http)           | HttpClient 기반으로 캡슐화하여 Web API 호출과 처리를 단순화합니다. |
|                      | [HTTP 서버HttpServer(임베디드 장치)](https://newlifex.com/core/httpserver)   | 경량 Http 서버로 미들웨어 파이프라인, 매개변수화 라우팅과 정적 파일 서비스를 지원합니다. |
|                      | [네트워크 다운로드WebClientX](https://newlifex.com/core/webclient)           | 강화된 WebClient로 멀티스레드, 이어받기 다운로드를 지원합니다. |
|                      | [WebSocket 서버(IoT 하향 알림 필수 도구)](https://newlifex.com/core/websocket) | 고성능 WebSocket 서버로 IoT 메시지 푸시에 적합합니다. |
|                      | [WebSocket 양방향 통신](/core/websocket_guide)                                | 핸드셰이크, 프레임 처리, 하트비트와 연결 종료 실무를 소개합니다. |
|                      | [파일 저장소 클라이언트EasyClient](/core/easy_io)                            | HTTP 파일 저장소 클라이언트로 EasyIO 또는 OSS와 연동합니다. |



## 뉴라이프 프로젝트 매트릭스
각 프로젝트는 기본적으로 net10.0/netstandard2.1/netstandard2.0/net4.62/net4.5를 지원하며, 구버전(2024.0801)은 net4.0/net2.0을 지원합니다.  

|                               프로젝트                                | 연도  | 설명                                                                                             |
| :-------------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------------ |
|                              기본 컴포넌트                            |       | 다른 미들웨어와 제품 프로젝트를 지원합니다.                                                       |
|          [NewLife.Core](https://github.com/NewLifeX/X)                | 2002  | 핵심 라이브러리, 로깅, 구성, 캐시, 네트워크, 직렬화, APM 성능 추적                                |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)         | 2005  | 빅데이터 미들웨어, 단일 테이블 수백억 건, MySql/SQLite/SqlServer/Oracle/PostgreSql/达梦, 자동 분할 테이블, 읽기/쓰기 분리 |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)           | 2005  | 네트워크 라이브러리, 단일 노드 수천만 건 처리량(2266만 tps), 단일 노드 수백만 연결(400만 Tcp 장기 연결) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting)      | 2011  | 프로토콜 통신 라이브러리, CS 애플리케이션 통신 프레임워크 제공, Http/RPC 통신 프레임워크 지원, 높은 처리량, IoT 장치 저비용 손쉬운 접속 |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)          | 2010  | 魔方 고속 개발 플랫폼, 사용자 권한, SSO 로그인, OAuth 서버 등을 통합, 단일 테이블 100억 건 프로젝트 검증 |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)         | 2008  | 서비스 관리 컴포넌트, 애플리케이션을 운영체제 데몬으로 설치, Windows 서비스, Linux의 Systemd    |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)          | 2020  | Zero 제로 세대 스캐폴드, NewLife 컴포넌트 생태계 기반 프로젝트 템플릿 NewLife.Templates, Web, WebApi, Service |
|                                미들웨어                               |       | 유명 미들웨어 플랫폼과 연동합니다.                                                                |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)         | 2017  | Redis 클라이언트, 마이크로초 수준 지연, 백만 수준 처리량, 풍부한 메시지 큐, 수백억 건 데이터량 프로젝트 검증 |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ)      | 2018  | RocketMQ 순수 관리형 클라이언트, Apache RocketMQ와 알리바바 클라우드 메시지 큐 지원, 십억 건 규모 프로젝트 검증 |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)          | 2019  | IoT 메시지 프로토콜, MqttClient/MqttServer, 클라이언트가 알리바바 클라우드 IoT 지원             |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)           | 2022  | IoT 표준 라이브러리, IoT 분야의 각종 통신 프로토콜 표준 규범 정의                                 |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)        | 2022  | ModbusTcp/ModbusRTU/ModbusASCII, IoT 표준 라이브러리 기반 구현, ZeroIoT 플랫폼과 IoTEdge 게이트웨이 지원 |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)       | 2022  | 지멘스 PLC 프로토콜, IoT 표준 라이브러리 기반 구현, IoT 플랫폼과 IoTEdge 지원                    |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)           | 2022  | 지도 컴포넌트 라이브러리, Baidu 지도, Amap 지도, Tencent 지도, Tianditu 지도 캡슐화               |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)         | 2023  | 오디오 코덱 라이브러리, PCM/ADPCMA/G711A/G722U/WAV/AAC                                          |
|                              제품 플랫폼                              |       | 제품 플랫폼급, 컴파일 및 배포 즉시 사용, 개별 맞춤 설정                                           |
|         [Stardust](https://github.com/NewLifeX/Stardust)              | 2018  | Stardust(星尘), 분산 서비스 플랫폼, 노드 관리, APM 모니터링 센터, 구성 센터, 등록 센터, 배포 센터   |
|           [AntJob](https://github.com/NewLifeX/AntJob)                | 2019  | AntJob(蚂蚁调度), 분산 빅데이터 컴퓨팅 플랫폼(실시간/오프라인), 蚂蚁搬家 분할 사상, 조 단위 데이터량 프로젝트 검증 |
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)           | 2021  | 기업 ERP, 제품 관리, 고객 관리, 판매 관리, 공급업체 관리                                          |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)              | 2006  | 码神 도구, 다양한 개발자 도구, 네트워크, 시리얼 포트, 암호화/복호화, 정규식, Modbus, MQTT         |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)                | 2023  | 간편 파일 저장소, 분산 시스템에서 파일 중앙 집중 저장을 지원합니다.                              |
|           [XProxy](https://github.com/NewLifeX/XProxy)                | 2005  | 제품급 역방향 프록시, NAT 프록시, Http 프록시                                                    |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)             | 2022  | Http 부하 테스트 도구                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)              | 2015  | Git 소스 코드 관리 시스템                                                                        |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)               | 2014  | 임베디드 운영체제, 완전 독자 개발, ARM Cortex-M 칩 아키텍처 지원                                 |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)               | 2019  | 임베디드 산업용 컴퓨터, IoT 엣지 게이트웨이, 고성능 .NET8 호스트, 산업, 농업, 교통, 의료 분야 적용 |
|                          FIoT IoT 플랫폼                              | 2020  | IoT 종합 솔루션, 건축, 환경 보호, 농업, 소프트웨어/하드웨어 및 빅데이터 분석 일체화, 단일 노드 십만 수준 포인트 프로젝트 검증 |



---
# 뉴라이프 개발팀
![XCode](https://newlifex.com/logo.png)

뉴라이프 팀(NewLife)은 2002년에 설립된 새로운 시대의 IoT 산업 솔루션 제공자로서, 소프트웨어·하드웨어 응용 방안 자문, 시스템 아키텍처 기획과 개발 서비스를 제공합니다. NuGet 누적 다운로드 523만+. 팀의 핵심 컴포넌트(XCode / Redis / Stardust / AntJob / IoT 등)는 전력, 산업, 교통, 환경 보호, 의료, 문화재 등의 업계에서 대규모로 적용되었습니다.

우리는 지속적으로 개선하여 오랫동안 신뢰받는 .NET / IoT / 분산 기술 파트너가 되고자 합니다.

웹사이트: https://newlifex.com   오픈소스 조직: https://github.com/NewLifeX  
위챗 공식 계정:  
![智能大石头](https://newlifex.com/stone.jpg)

---
> 이 프로젝트가 도움이 되셨다면 Star를 눌러 주시고, 팀에 공유하거나 Issue/PR로 개선 제안을 남겨 주세요.
