# NewLife.Core - Kernbibliothek

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

**NewLife.Core** ist das Fundament des NewLife-Ökosystems: ein Paket für Protokollierung, Konfiguration, Caching, Netzwerk, Serialisierung, Zeitplanung, Objektcontainer und APM-Tracing. Es ist NativeAOT-bereit, liefert Laufzeitmeldungen in <!--locale-count:start-->31<!--locale-count:end--> Sprachen, unterstützt 11 Zielframeworks von net45 bis net10.0 und treibt XCode, Cube, Redis, Stardust und über 30 Schwesterprojekte an.

Tutorial zur Kernbibliothek: [https://newlifex.com/core](https://newlifex.com/core)  

> ⚠️ Diese Übersetzung kann veraltet sein; maßgeblich ist das [englische README](README.md).

**Inhalt**: [Schnellstart](#schnellstart) · [Warum NewLife.Core](#warum-newlifecore) · [Hauptfunktionen](#hauptfunktionen) · [Ökosystem-Karte](#ökosystem-karte) · [Dokumentation](#dokumentation) · [Beitragen](#beitragen) · [Team](#team) · [Lizenz](#lizenz)

## Schnellstart

### Installation

```bash
dotnet add package NewLife.Core
```

### Minimales Beispiel

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // console log (also writes to file)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // in-memory cache
cache.Set("key", "value", 60);          // expires in 60 seconds
var value = cache.Get<String>("key");   // read back
```

### Template-Gerüst

Führe die folgenden Befehle in einer Kommandozeile aus, um die NewLife-Komponenten schnell kennenzulernen:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### Plattformunterstützung

| Element | Abdeckung |
| --- | --- |
| Zielframeworks | 11 in einem Paket: net45, net461, net462, netstandard2.0, netstandard2.1, net6.0, net6.0-windows, net8.0, net8.0-windows, net10.0, net10.0-windows |
| Betriebssystem und Architektur | Windows, Linux, macOS, Android, iOS; x64, x86, arm64, arm, loongarch64, riscv64, s390x |
| NativeAOT | <!--aot-scenarios:start-->31<!--aot-scenarios:end--> Kernszenarien gemessen; Multi-RID-CI-Smoke-Matrix (win-x64, linux-x64, linux-arm64 und linux-loongarch64 sind blockierende Gates). Plugins und Skripting sind konstruktionsbedingt nicht AOT-kompatibel – siehe [AOT-Kompatibilitätsanalyse](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) |
| Laufzeitlokalisierung | <!--locale-count:start-->31<!--locale-count:end--> integrierte Sprachen; Log- und Ausnahmemeldungen werden zur Laufzeit lokalisiert, ohne Aufrufstellen zu ändern (siehe [Lokalisierung](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)) |
| Legacy-Frameworks | net40: `dotnet add package NewLife.Core --version 10.10.2024.801-net40` (net40/net45/net461); net20: `--version 10.10.2024.701-net20` (nur net20). Eingefrorene Builds von 2024-08, die der Hauptlinie nicht folgen. |

## Warum NewLife.Core

- **Ein Paket für die gemeinsame Infrastruktur**: Protokollierung, Konfiguration, Caching, Netzwerk, Serialisierung, Zeitplanung, Objektcontainer und DI – eine einzige Referenz genügt.
- **Breite Abdeckung, moderne Laufzeit**: 11 Zielframeworks in einem Paket (net45 → net10.0, inklusive Windows-Varianten), NativeAOT-bereit auf dem Kernpfad und Laufzeitmeldungen in <!--locale-count:start-->31<!--locale-count:end--> Sprachen lokalisiert – dieselbe API für Altsysteme und neue Projekte.
- **Im Ökosystem-Maßstab erprobt**: dieselbe Basis betreibt XCode (einzelne Tabellen mit 10 Milliarden Zeilen), Net (1,77 Mio. pps / 14,5 Gbps, TCP 1KB), Redis und Stardust (zig Milliarden Datensätze).
- **Zurückhaltende Abhängigkeiten, freizügige Lizenz**: nur BCL und Microsoft-Kompatibilitätspakete; MIT, daher lassen sich Quellen ändern und umbenennen (siehe [Lizenz](#lizenz)).

## Hauptfunktionen

<details>
<summary><b>Basiserweiterungen</b> (10)</summary>

| Funktion | Hauptmerkmale |
| --- | --- |
| [Typkonvertierung Utility](https://newlifex.com/core/utility) | Bietet effiziente Typkonvertierung mit Unterstützung verschiedener Formate und Ausnahmebehandlung. |
| [Zeichenketten-Erweiterung StringHelper](https://newlifex.com/core/string_helper) | Enthält vielfältige Zeichenkettenoperationen wie Kürzen, Kodieren und Formatieren. |
| [Pfad-Erweiterung PathHelper](https://newlifex.com/core/path_helper) | Behandelt verschiedene Pfadformate intelligent und ermöglicht plattformübergreifende Kompatibilität des Codes. |
| [IO-Erweiterung IOHelper](https://newlifex.com/core/io_helper) | Optimiert IO-Operationen und bietet effiziente Konvertierung zwischen Datenströmen und Byte-Arrays. |
| [Prozess-Erweiterung ProcessHelper](https://newlifex.com/core/process_helper) | Vereinfacht Prozessoperationen mit bequemen Methoden zum Abrufen und Verwalten von Prozessinformationen. |
| [Laufzeitinformationen Runtime](https://newlifex.com/core/runtime) | Bietet einfache Methoden zum Abrufen von Laufzeitinformationen der Anwendung, was Debugging und Diagnose erleichtert. |
| [Disposable-Basisklasse DisposeBase](https://newlifex.com/core/disposebase) | Bietet ein standardisiertes Muster zur Ressourcenfreigabe, das Speicher- und Ressourcenlecks wirksam verhindert. |
| [Reflexions-Erweiterung Reflect](https://newlifex.com/core/reflect) | Hochperformantes Reflexionswerkzeug zum schnellen Lesen und Schreiben von Objektmembern. |
| [Sicherheits-Erweiterung SecurityHelper](https://newlifex.com/core/security_helper) | Integriert verschiedene Ver- und Entschlüsselungsalgorithmen wie RSA/AES/DES/MD5 und vereinfacht Sicherheitsoperationen. |
| [Geo-Hash GeoHash](https://newlifex.com/core/geo_hash) | Kodiert Breiten- und Längengrad in Zeichenketten für die Suche nach nahe gelegenen Orten. |

</details>

<details>
<summary><b>Protokollierung und Tracing</b> (2)</summary>

| Funktion | Hauptmerkmale |
| --- | --- |
| [Protokollierung ILog](https://newlifex.com/core/log) | Unterstützt mehrere Formen der Logausgabe und bietet leistungsstarkes asynchrones Schreiben. |
| [Ablaufverfolgung ITracer](https://newlifex.com/core/tracer) | Leistungsstarkes APM-Performance-Tracing mit Unterstützung für die Integration in die Stardust-Plattform. |

</details>

<details>
<summary><b>Zeitplanung</b> (3)</summary>

| Funktion | Hauptmerkmale |
| --- | --- |
| [Erweiterter Timer TimerX](https://newlifex.com/core/timerx) | Hochpräziser Timer mit Unterstützung für Cron-Planung und asynchrone periodische Ausführung. |
| [Cron-Ausdrücke](https://newlifex.com/core/cron) | Bietet Linux-Cron-ähnliche geplante Aufgaben mit sekundengenauer Übereinstimmung. |
| [Zeitanbieter TimeProvider](https://newlifex.com/core/timerx_timeprovider) | Einheitliche Zeitabstraktion und hochpräzise Laufzeitberechnung, praktisch für versionsübergreifende Kompatibilität. |

</details>

<details>
<summary><b>Konfigurationssystem</b> (4)</summary>

| Funktion | Hauptmerkmale |
| --- | --- |
| [Konfigurationssystem Config](https://newlifex.com/core/config) | Einheitliches Konfigurationsframework mit Unterstützung mehrerer Formate, mehrerer Quellen und Hot Reload. |
| [Konfigurationsanbieter IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | Einheitliche Konfigurationsschnittstelle mit Unterstützung für Callbacks und ein Konfigurationsknotenmodell. |
| [Kernkonfiguration Setting](https://newlifex.com/core/setting) | Verwaltet die Konfiguration der Kernkomponenten und steuert grundlegende Verhaltensweisen wie Logging und Netzwerk. |
| [Systemkonfiguration SysConfig](https://newlifex.com/core/sysconfig) | Verwaltet die Systemkonfiguration der Anwendung mit Unterstützung für lokale, verteilte und verschlüsselte Konfiguration. |

</details>

<details>
<summary><b>Serialisierung</b> (7)</summary>

| Funktion | Hauptmerkmale |
| --- | --- |
| [JSON-Serialisierung](https://newlifex.com/core/json) | Hochperformantes JSON-Werkzeug mit Unterstützung für Serialisierung in verschiedenen komplexen Szenarien. |
| [XML-Serialisierung](https://newlifex.com/core/xml) | Bietet XML-Serialisierung und -Deserialisierung mit Konvertierung zwischen Objekten und XML in beide Richtungen. |
| [Binäre Serialisierung](https://newlifex.com/core/binary) | Hochperformante binäre Serialisierung mit Unterstützung komplexer Objektgraphen und Versionskontrolle. |
| [Erweiterte binäre Serialisierung](https://newlifex.com/core/advanced_binary) | Fortgeschrittene Anleitung zur zuweisungsfreien Kodierung und Dekodierung über den gesamten Protokollnachrichtenfluss. |
| [CSV-Parser CsvFile](https://newlifex.com/core/csv_file) | Schnelles Lesen und Schreiben von CSV-Dateien mit Unterstützung benutzerdefinierter Trennzeichen und Datenbindung. |
| [CSV-Datenbank CsvDb](https://newlifex.com/core/csv_db) | Verwendet CSV-Dateien als Datenbank mit Unterstützung grundlegender Erstellen-, Lesen-, Aktualisieren- und Löschen-Operationen. |
| [Leichtgewichtiger Excel-Reader ExcelReader](https://newlifex.com/core/excel_reader) | Liest Excel schnell ohne Office-Abhängigkeiten und unterstützt große Datenmengen. |

</details>

<details>
<summary><b>Caching</b> (7)</summary>

| Funktion | Hauptmerkmale |
| --- | --- |
| [Einheitliche Cache-Schnittstelle ICache](https://newlifex.com/core/icache) | Definiert standardmäßige Cache-Operationen und bietet mehrere Implementierungen wie Speicher und Redis. |
| [Cache-Architektur ICacheProvider](https://newlifex.com/core/icacheprovider) | Verwaltet und liefert ICache-Instanzen und unterstützt mehrere gleichzeitig existierende Caches. |
| [Speichercache MemoryCache](https://newlifex.com/core/memory_cache) | Hochperformanter lokaler Speichercache mit Unterstützung für Ablauf- und Kapazitätsrichtlinien. |
| [Objektpool ObjectPool](https://newlifex.com/core/object_pool) | Wiederverwendet teure Objekte, verbessert die Leistung und reduziert den GC-Druck. |
| [Objektpool Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | Hochperformanter universeller Objektpool auf CAS-Basis, der den GC-Druck reduziert. |
| [Verteilter Cache NewLife.Redis](https://newlifex.com/core/redis) | Redis-basierte verteilte Cache-Implementierung mit Unterstützung mehrerer Datenstrukturen. |
| [Vektorspeicher IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | Schnittstelle zur Verwaltung von Vektorsammlungen mit Unterstützung der Top-K-Ähnlichkeitssuche; liefert die Vektorabruf-Infrastruktur für KI-Anwendungen. |

</details>

<details>
<summary><b>Hochleistungspuffer</b> (7)</summary>

| Funktion | Hauptmerkmale |
| --- | --- |
| [Datenpaket IPacket](https://newlifex.com/core/packet) | Zentrale Datenpaket-Schnittstelle für Zero-Copy-Slicing und verkettete Pakete. |
| [Puffer Buffers](https://newlifex.com/core/buffers) | Hochperformante Pufferoperationsbibliothek, die Komponenten wie Span und gepoolte Writer bündelt. |
| [Gepoolter Writer PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | ArrayPool-basierter gepoolter Puffer-Writer mit Unterstützung dynamischer Erweiterung. |
| [Span-Reader SpanReader](https://newlifex.com/core/span_reader) | Zuweisungsfreier Hochleistungs-Parser für Binärströme mit Unterstützung für Little/Big Endian. |
| [Span-Writer SpanWriter](https://newlifex.com/core/span_writer) | Zuweisungsfreier Hochleistungs-Writer für Binärdaten mit Unterstützung für Little/Big Endian. |
| [Span-Hilfen SpanHelper](https://newlifex.com/core/span_helper) | Sammlung von Span/ReadOnlySpan-Erweiterungsmethoden, die Speicheroperationen vereinfacht. |
| [Sequenzleser SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | Segmentübergreifender sequenzieller Leser (für ältere Frameworks mit Shims ergänzt; auf allen 11 TFMs verfügbar). |

</details>

<details>
<summary><b>Anwendungsframework</b> (15)</summary>

| Funktion | Hauptmerkmale |
| --- | --- |
| [Objektcontainer ObjectContainer](https://newlifex.com/core/object_container) | Leichtgewichtige Abhängigkeitsinjektion mit Unterstützung mehrerer Lebenszyklen und Injektionsmuster. |
| [Leichtgewichtiger Anwendungshost Host](https://newlifex.com/core/host) | Vereinfacht das Hosten von Anwendungen und erleichtert den Aufbau langlaufender Hintergrunddienste. |
| [Plugin-Framework IPlugin](https://newlifex.com/core/plugin) | Bietet einen standardisierten Plugin-Verwaltungsmechanismus für die modulare Anwendungsentwicklung. |
| [Nebenläufigkeitsmodell Actor](https://newlifex.com/core/actor) | Vereinfachtes Nebenläufigkeitsmodell, das den Durchsatz durch Nachrichtenwarteschlangenverarbeitung erhöht. |
| [Verzögerte Warteschlange DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | Bündelt hochfrequente Änderungen und persistiert sie in Stapeln, wodurch Write Amplification reduziert wird. |
| [Ereignisbus EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | Ereignisverteilung innerhalb des Prozesses mit Unterstützung für Themen-Routing und nachrichtenähnliche Warteschlangen. |
| [Schwachreferenz-Delegat WeakAction](https://newlifex.com/core/weak_action) | Löst das Speicherleck-Problem von Ereignisabonnements und ermöglicht sichere Ereignisbehandlung. |
| [Skript-Engine ScriptEngine](https://newlifex.com/core/script_engine) | Unterstützt dynamische C#-Kompilierung und -Ausführung und bietet flexible Skript-Erweiterbarkeit. |
| [Komprimierung und Dekomprimierung](https://newlifex.com/core/compression) | Integriert mehrere Komprimierungsalgorithmen und bietet eine einheitliche Schnittstelle zur Datenkomprimierung. |
| [Archivkomprimierung Tar und 7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Behandelt die fortgeschrittene Nutzung der Archivkomprimierung für Tar/Tar.Gz und 7Zip. |
| [Maschineninformationen MachineInfo](https://newlifex.com/core/machine_info) | Ruft umfassende Hardwareinformationen ab und unterstützt Windows- und Linux-Systeme. |
| [Pinyin-Bibliothek PinYin](https://newlifex.com/core/pinyin) | Effiziente Bibliothek zur Umwandlung chinesischer Zeichen in Pinyin mit Unterstützung von Voll- und Kurzschreibweise bei überlegener Leistung. |
| [Datensatz DbTable](https://newlifex.com/core/dbtable) | Eine In-Memory-Datentabelle für bequeme Datenverarbeitung und -analyse. |
| [Seitennummerierungsparameter PageParameter](https://newlifex.com/core/page_parameter) | Kapselt die Paging-Logik und unterstützt Paginierung in Szenarien wie Web und API. |
| [Snowflake-Algorithmus Snowflake](https://newlifex.com/core/snow_flake) | Algorithmus zur Erzeugung verteilter eindeutiger IDs, der die Eindeutigkeit unter hoher Nebenläufigkeit garantiert. |

</details>

<details>
<summary><b>Sicherheit und Token</b> (3)</summary>

| Funktion | Hauptmerkmale |
| --- | --- |
| [Verteiltes digitales Signatur-Token TokenProvider](https://newlifex.com/core/token_provider) | Erzeugt und validiert verteilte Tokens und gewährleistet die Sicherheit zwischen mehreren Knoten. |
| [Universelles Web-Token JwtBuilder](https://newlifex.com/core/jwt) | Bietet Erzeugung, Dekodierung und Validierung von JWT-Tokens und vereinfacht die Web-Authentifizierung. |
| [Token-HTTP-Filter TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | Fügt Tokens automatisch in HTTP-Anfragen ein und fängt 401/403 zur automatischen Erneuerung ab. |

</details>

<details>
<summary><b>Netzwerkbibliothek</b> (13)</summary>

| Funktion | Hauptmerkmale |
| --- | --- |
| [Netzwerkserver NetServer](https://newlifex.com/core/netserver) | Hochperformanter TCP/UDP/Unix-Domain-Socket-Server, der Sitzungen und Datenverarbeitung kapselt. |
| [Netzwerkclient NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | Einheitlicher TCP/UDP/Unix-Netzwerkclient, der Verbindung und Senden/Empfangen vereinfacht. |
| [Nachricht IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | Request-Response-Nachrichtenvertrag mit Richtungssemantik und Frame-Lesen/-Schreiben. |
| [Nachrichtenprotokoll-Stack MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | Einheitlicher Stack aus Frame-Pump und zustandslosem Codec mit Unterstützung großer Streaming-Frame-Bodies und Backpressure. |
| [Datenpipeline Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | Zero-Copy-Datenpipeline, ausgerichtet an der Form von System.IO.Pipelines. |
| [Einfaches Remote-Nachrichtenaustauschprotokoll SRMP](https://newlifex.com/core/srmp) | Leichtgewichtiges RPC-Protokoll mit Unterstützung binärer Serialisierung und UDP-Transport. |
| [Lastverteilung und Failover LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Bietet drei Multi-Node-Scheduling-Strategien: Failover, Polling und Racing. |
| [HTTP-Client ApiHttpClient](https://newlifex.com/core/api_http) | Basierend auf einer HttpClient-Kapselung, vereinfacht Aufruf und Handhabung von Web-APIs. |
| [HTTP-Server HttpServer (eingebettete Geräte)](https://newlifex.com/core/httpserver) | Leichtgewichtiger HTTP-Server mit Unterstützung von Middleware-Pipelines, parametrisiertem Routing und statischer Dateiauslieferung. |
| [Netzwerk-Download WebClientX](https://newlifex.com/core/webclient) | Erweiterter WebClient mit Unterstützung für mehrthreadige und fortsetzbare Downloads. |
| [WebSocket-Server (leistungsstarkes Werkzeug für IoT-Downlink-Benachrichtigungen)](https://newlifex.com/core/websocket) | Hochperformanter WebSocket-Server, geeignet für IoT-Nachrichten-Push. |
| [WebSocket-Bidirektionalkommunikation](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | Stellt Vorgehensweisen für Handshake, Frame-Behandlung, Heartbeat und Verbindungsabbau vor. |
| [Dateispeicher-Client EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | HTTP-Dateispeicher-Client für EasyIO oder OSS. |

</details>

> RPC (`ApiServer`/`ApiClient`) ist nach [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) umgezogen.

## Ökosystem-Karte

NewLife ist ein geschichtetes Ökosystem unabhängig versionierter Open-Source-Projekte. Die folgende Karte wird wortgleich in allen NewLife-Repositories verwendet; jedes Projekt verlinkt auf sein Repository und sein primäres NuGet-Paket.

| Projekt | Funktion | Downloads |
| --- | --- | --- |
| **Grundlage** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Protokollierung, Konfiguration, Caching, Netzwerk, Serialisierung, Zeitplanung und APM-Tracing. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Daten-Middleware: einzelne Tabellen in der Größenordnung von zig Milliarden Zeilen, automatisches Sharding, Lese-/Schreibtrennung, über 10 Datenbanken. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Hochperformante Netzwerkbibliothek: 1,77 Mio. pps / 14,5 Gbps auf einer Maschine (TCP 1KB, 2026-10), 4 Mio. langlebige TCP-Verbindungen. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Protokollkommunikation und RPC-Framework für Client/Server-Anwendungen und IoT-Geräte. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installiert .NET-Anwendungen als Betriebssystem-Dienste: Windows-Dienste und Linux-systemd. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new`-Projektvorlagen für Konsole, Web, API, Dienst, Daten und IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Daten & Messaging** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis-Client mit Mikrosekunden-Latenz, Millionen Operationen pro Sekunde und integrierten Nachrichtenwarteschlangen. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Vollständig verwalteter RocketMQ-Client für Apache RocketMQ und Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT-Client und -Server, einschließlich Alibaba Cloud IoT-Anbindung. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Hybriddatenbank in C#: eingebettet oder im Netzwerk, relational, Zeitreihen, Nachrichtenwarteschlange und KV in einem. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql-Treiber und Hilfsschicht auf Basis des NewLife-Datenstacks. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, KI & verteilt** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Schnelles Framework für Administrationsplattformen: Berechtigungen, SSO, OAuth-Server und CRUD-Gerüst. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Verteilte Dienstplattform: Knotenverwaltung, APM, Konfigurationszentrum, Registry und Release-Center. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Plattform für verteilte Aufgaben und Echtzeit-Datenplanung mit Sharding, Wiederholungen und Backfill. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Einheitlicher Client für 46 LLM-Anbieter plus ChatAI, eine sofort einsatzbereite Web-Chat-Anwendung. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industrie & IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT-Standardbibliothek mit Protokollverträgen und Geräteabstraktionen. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus-TCP/RTU/ASCII-Implementierung auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Siemens-SPS-Protokoll auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Omron-SPS-Protokoll auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Mitsubishi-Melsec-SPS-Protokoll auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC-Client für industriellen Datenzugriff. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet-Protokollstack für die Gebäudeautomation. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Leichtgewichtige Open-Source-IoT-Plattform mit Gateway- und Geräteverwaltung. | — |
| **Fahrzeugtelematik** | | |
| NewLife.JT808 | JT/T-808-Fahrzeugortungsprotokoll: über 70 Nachrichtenkörper, 2011/2013/2019. | ![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads) |
| NewLife.JT809 | JT/T-809-Plattform-zu-Plattform-Protokoll für Verkehrsbehörden. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| NewLife.JT1078 | JT/T-1078-Fahrzeugvideo: RTP-Parsing, FLV/fMP4-Remuxing und Live-Streaming. | ![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads) |
| **Domänenkomponenten** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Ein Paket für Excel, Word, PPT, PDF und über 15 Dokumentformate. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Plattformübergreifendes Audio: Codecs, Geräte-I/O, DSP, Container und Streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Videocodecs, Containerformate, Frame-Verarbeitung und Streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Einheitliche Schnittstelle für Baidu, AMap, Tencent und Tianditu mit Geokodierung und Koordinatenumrechnung. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4-Geolokalisierung mit einer offline speichergemappten Datenbank und einer Online-API als Fallback. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Werkzeuge & Produkte** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Entwickler-Toolbox: Netzwerk, serielle Schnittstelle, Krypto, Regex, Modbus und MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Selbst gehostetes Git-Quellcode-Verwaltungssystem. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installiert automatisch die passende .NET-Laufzeit, wenn es von einem älteren Host gestartet wird. | — |

> Die Badges zeigen das primäre Paket jedes Projekts; Theme-, Erweiterungs- und Satellitenpakete sind auf den jeweiligen NuGet-Seiten aufgeführt. JT808/JT1078 werden gerade veröffentlicht – ihre Badges zeigen Zahlen, sobald die Pakete live sind. Die hier nicht aufgeführten über 30 Pakete (FTP, LoRa, Office-Erweiterungen, Protokolltreiber und mehr) sind in der [NewLifeX-Organisation](https://github.com/NewLifeX) und auf [newlifex.com](https://newlifex.com) verfügbar.

## Dokumentation

### Leitfäden

- [Inhalt der Kernbibliothek](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): Index der Artikel zu Kernkomponenten, nach Modulen gruppiert
- [AOT-Kompatibilitätsanalyse](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): NativeAOT-Umfang, gemessene Ergebnisse (Smoke <!--aot-smoke:start-->31/31<!--aot-smoke:end-->), RID-Matrix und bekannte Grenzen
- [Lokalisierung](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md): Nachrichtenmodell, Sprachtabellen, Überschreibungen in nachgelagerten Projekten und Hinzufügen von Sprachen zur Laufzeit
- [Checkliste zur Lokalisierung für nachgelagerte Projekte](https://github.com/NewLifeX/X/blob/master/Doc/下游多语言接入清单.md): Integrationsschritte, Sprachregeln, CI-Gates und Badge-Vorlagen
- [Mobile-Architektur](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md): .NET-Mobile-Stack, Auswahl des UI-Frameworks, APK-Paketierung und AOT-Hinweise
- [Beispiele](https://github.com/NewLifeX/X/tree/master/Samples): Beispiele für Konsole, Dienst, Http, IoT und Mobile

### Versionshinweise

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md): Versionsverlauf

## Beitragen

### Erstellen und testen

Die Lösung lässt sich in Visual Studio (aktuelle Version) öffnen und ebenso mit der .NET-CLI erstellen; die Projektdateien bleiben bewusst einfach, sodass ein frischer Klon ohne weitere Einrichtung baut.

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- Vollständig paralleler Lauf: `run-tests.ps1` (Multi-Host-Parallelität, Timeout-Schutz und trx-Zusammenfassung)
- NativeAOT-Smoke: `AotSmoke/`, CI in `.github/workflows/aot.yml`
- Benchmarks: `Benchmark/` (BenchmarkDotNet)

### Repository-Struktur

| Verzeichnis | Beschreibung |
| --- | --- |
| `NewLife.Core/` | Quellen der Kernbibliothek (Protokollierung, Netzwerk, Caching, Serialisierung, …) |
| `NewLife.Core.SourceGenerator/` | Quellgenerator, der als `analyzers/` im Paket ausgeliefert wird |
| `Doc/` | Funktionsdokumentation und Artikelindex |
| `Samples/` | Beispiele für Konsole / Dienst / Http / IoT / Mobile |
| `Benchmark/` | BenchmarkDotNet-Leistungsbenchmarks |
| `XUnitTest.Core/` | Zentrale Unit-Tests (net10.0) |
| `XUnitTest.Compat/`, `XUnitTest.Net6/` | Versionsübergreifende Kompatibilitätstests |
| `AotSmoke/` | NativeAOT-Smoke-Projekt |

### Veröffentlichung

Ein Tag mit Präfix `v` (zum Beispiel `v12.0.2026.0927`) löst `.github/workflows/publish.yml` aus. Der Workflow packt das Paket, veröffentlicht es auf NuGet.org und GitHub Packages und erstellt ein GitHub Release, dessen Hinweise aus dem passenden Abschnitt von `ChangeLog.md` stammen (Artefakte umfassen nupkg und snupkg). Ein manueller Lauf veröffentlicht nur Pakete und erstellt kein Release.

### Entwicklungsdokumentation

Das Verzeichnis `Doc/` enthält eine Funktionsdokumentation, die den Code spiegelt: Anforderungen → Funktionsverfolgung → Architektur. Diese Dokumente sind auf Chinesisch verfasst.

- [Anforderungsdokument](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): Vision, Kernziele und funktionale Anforderungen
- [Funktionsliste](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): Implementierungs-, Test- und Kommentarstatus jedes Features
- [Architekturdesign](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): Schichtung, Komponenten und zentrale Designentscheidungen

### Feedback

- Stelle vor dem Einreichen sicher, dass `dotnet build` und die relevanten Unit-Tests erfolgreich sind; befolge die Repository-Konventionen für Codestil und Commit-Nachrichten.
- Melde Fehler und Vorschläge über [Issue](https://github.com/NewLifeX/X/issues); Stars und PRs sind willkommen.

## Team

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

NewLife wurde 2002 gegründet und widmet sich seit mehr als zwei Jahrzehnten dem .NET- und IoT-Ökosystem, betreut fast 100 Open-Source-Projekte (34 davon Hauptprojekte) und liefert integrierte Hard- und Softwarelösungen für die Bereiche Energie, Logistik, Gesundheitswesen, Industrieautomation und Internet. Unser Leitspruch lautet „Lernen kennt kein Dienstalter – wer es beherrscht, wird zum Lehrmeister“.

Die Open-Source-Komponenten wurden <!--nuget-total:start-->6.48M<!--nuget-total:end--> Mal von NuGet heruntergeladen (ökosystemweit, Stand 2026-10).

Website: https://newlifex.com · Open-Source-Organisation: https://github.com/NewLifeX

<details>
<summary>WeChat-Offizieller Account: 智能大石头 (zum Folgen scannen)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## Lizenz

Dieses Projekt steht unter der [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE)-Lizenz; das Strong-Name-Zertifikat `newlife.snk` wird veröffentlicht, damit du Assemblys selbst erstellen und ersetzen kannst, und der Quellcode darf ohne Namensnennung geändert und weitergegeben werden.

> Wenn dieses Projekt für dich hilfreich ist, gib ihm gerne einen Stern, teile es mit deinem Team oder reiche Verbesserungsvorschläge über Issue/PR ein.
