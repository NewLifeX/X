# NewLife.Core - Kernbibliothek

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=v12)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)

🌐 [中文](Readme.MD) | [English](README.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Español](README.es.md) | [Français](README.fr.md) | [Русский](README.ru.md) | [Português](README.pt.md) | [العربية](README.ar.md)

Das NewLife-Framework ist ein umfassendes .NET-Komponentenökosystem, das hochperformante Infrastruktur für den Aufbau skalierbarer Anwendungen bereitstellt. Es bietet grundlegende Funktionen wie Protokollierung, Netzwerk, Serialisierung, Caching und Multithreading und bildet die Grundlage für eine breite Palette von Anwendungen – von Webdiensten bis hin zu IoT-Geräten.  

Tutorial zur Kernbibliothek: [https://newlifex.com/core](https://newlifex.com/core)  

> ⚠️ Diese Übersetzung kann veraltet sein; maßgeblich ist das [chinesische README](Readme.MD).

## Framework-Überblick

NewLife besteht aus mehreren Komponentenbibliotheken, deren Fundament NewLife.Core bildet. Das Framework unterstützt mehrere Zielframeworks von net45 bis net10.0 (.NET Framework 4.5 bis .NET 10.0) und erreicht damit breite Kompatibilität in unterschiedlichen Umgebungen.

**NativeAOT**: Die Kernpfade werden bereits unterstützt (25 praxiserprobte Szenarien + CI-Regressionsschutz); dynamische Fähigkeiten wie Plugins und Skripte werden konstruktionsbedingt nicht unterstützt; zu den Grenzen siehe [AOT-Kompatibilitätsanalyse (中文)](Doc/AOT兼容性分析.md).



## Dokumentationsindex

Das Verzeichnis `Doc/` des Repositorys pflegt ein Dokumentationssystem für die Fähigkeiten (Anforderungsdefinition → Funktionsverfolgung → Architekturentwurf):

- [Anforderungsdokument (中文)](Doc/需求文档.md): Vision, Kernziele und funktionale Anforderungen
- [Funktionsliste (中文)](Doc/功能清单.md): Statusverfolgung von Implementierung/Test/Kommentaren aller Funktionspunkte
- [Architekturentwurf (中文)](Doc/架构设计.md): Schichten, Modulkomponenten und zentrale Designentscheidungen
- [Verzeichnis der Kernbibliothek (中文)](Doc/核心库目录.md): Artikelindex der Kernkomponenten
- [AOT-Kompatibilitätsanalyse (中文)](Doc/AOT兼容性分析.md): Unterstützungsbereich von NativeAOT, Messergebnisse (Smoke-Test 29/29), Nutzungshinweise und bekannte Grenzen

## Schnellstart

Der einfachste Weg, die NewLife-Komponenten zu nutzen, ist die Referenzierung über NuGet – suchen Sie beispielsweise in der NuGet-Verwaltung des Projekts nach `NewLife.Core` und binden Sie es ein.

Die NewLife-Komponenten werden seit über 20 Jahren von der Community gemeinsam entwickelt und stehen unter der MIT-Open-Source-Lizenz – **jeder darf sie beliebig ändern und erneut veröffentlichen** (ohne Angabe der Quelle)! Viele Unternehmen bauen darauf ihre interne Entwicklungsplattform auf und können sogar eine private Anpassung erreichen, indem sie im Quellcode alle Zeichenfolgen `NewLife` durch den eigenen Firmennamen ersetzen.

Das Team pflegt stets eine offene Haltung: Das Projekt lässt sich nicht nur in Visual Studio (neueste offizielle Version) durch Öffnen der Projektmappe kompilieren, sondern ist auch mit der Kompilierung über die Kommandozeile `dotnet build` kompatibel. Die Projektdateien verzichten zugunsten von Einfachheit und Benutzerfreundlichkeit auf komplexe Funktionen – wirklich einsatzbereit direkt nach dem Auspacken.

Wir veröffentlichen das Strong-Name-Zertifikat `newlife.snk`, um das eigenständige Kompilieren und Ersetzen der Assemblys zu unterstützen.



Führen Sie in der Kommandozeile die folgenden Befehle aus, um die NewLife-Komponenten schnell auszuprobieren:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```



## Hauptfunktionen


| Modulkategorie | Funktionspunkt | Hauptmerkmale |
| ---------------- | ---------------------------------------------------------------------------- | ------------------------------------------------ |
| **Basiserweiterungen** | [Typkonvertierung Utility](https://newlifex.com/core/utility) | Bietet effiziente Typkonvertierung mit Unterstützung für verschiedene Formate und Ausnahmebehandlung. |
|  | [Zeichenketten-Erweiterung StringHelper](https://newlifex.com/core/string_helper) | Enthält vielfältige Zeichenkettenoperationen wie Ausschneiden, Kodierung und Formatierung. |
|  | [Pfad-Erweiterung PathHelper](https://newlifex.com/core/path_helper) | Verarbeitet verschiedene Pfadformate intelligent und ermöglicht plattformübergreifende Codekompatibilität. |
|  | [IO-Erweiterung IOHelper](https://newlifex.com/core/io_helper) | Optimiert IO-Vorgänge und bietet effiziente Konvertierung zwischen Datenströmen und Byte-Arrays. |
|  | [Prozess-Erweiterung ProcessHelper](https://newlifex.com/core/process_helper) | Vereinfacht Prozessvorgänge und bietet komfortable Methoden zum Abrufen und Verwalten von Prozessinformationen. |
|  | [Laufzeitinformationen Runtime](https://newlifex.com/core/runtime) | Bietet einfache Methoden zum Abrufen von Laufzeitinformationen der Anwendung für Debugging und Diagnose. |
|  | [Disposable-Basisklasse DisposeBase](https://newlifex.com/core/disposebase) | Bietet ein standardisiertes Ressourcenfreigabemuster und verhindert wirksam Speicher- und Ressourcenlecks. |
|  | [Reflexions-Erweiterung Reflect](https://newlifex.com/core/reflect) | Leistungsstarkes Reflexionswerkzeug für schnelles Lesen und Schreiben von Objektmembern. |
|  | [Sicherheits-Erweiterung SecurityHelper](https://newlifex.com/core/security_helper) | Integriert Verfahren wie RSA/AES/DES/MD5 für Ver- und Entschlüsselung und vereinfacht Sicherheitsvorgänge. |
| **Protokollierung und Tracing** | [Protokollierung ILog](https://newlifex.com/core/log) | Unterstützt verschiedene Log-Ausgabekanäle und bietet leistungsstarke asynchrone Schreibfunktionen. |
|  | [Ablaufverfolgung ITracer](https://newlifex.com/core/tracer) | Leistungsstarkes APM-Performance-Tracing mit Unterstützung für die Integration in die Stardust-Plattform. |
| **Zeitplanung** | [Erweiterter Timer TimerX](https://newlifex.com/core/timerx) | Hochpräziser Timer mit Cron-Zeitplanung und asynchroner periodischer Ausführung. |
|  | [Cron-Ausdrücke](https://newlifex.com/core/cron) | Bietet zeitgesteuerte Aufgaben ähnlich Linux Cron mit sekundengenauer Übereinstimmung. |
|  | [Zeitanbieter TimeProvider](/core/time_provider) | Einheitliche Zeitabstraktion und hochpräzise Laufzeitmessung für versionsübergreifende Kompatibilität. |
| **Konfigurationssystem** | [Konfigurationssystem Config](https://newlifex.com/core/config) | Einheitliches Konfigurationsframework mit Unterstützung für mehrere Formate, Quellen und Hot-Reload. |
|  | [Konfigurationsanbieter IConfigProvider](/core/config_provider) | Einheitliche Konfigurationsschnittstelle mit Callback- und Konfigurationsknotenmodell. |
|  | [Kernkonfiguration Setting](https://newlifex.com/core/setting) | Verwaltet die Kernkonfiguration von Komponenten und steuert Basisverhalten wie Protokollierung und Netzwerk. |
|  | [Systemkonfiguration SysConfig](https://newlifex.com/core/sysconfig) | Verwaltet die Anwendungssystemkonfiguration mit Unterstützung für lokale, verteilte und verschlüsselte Konfiguration. |
| **Serialisierung** | [JSON-Serialisierung](https://newlifex.com/core/json) | Leistungsfähiges JSON-Werkzeug mit Unterstützung für Serialisierung in vielen komplexen Szenarien. |
|  | [XML-Serialisierung](https://newlifex.com/core/xml) | Bietet XML-Serialisierung und -Deserialisierung mit gegenseitiger Konvertierung zwischen Objekten und XML. |
|  | [Binäre Serialisierung](https://newlifex.com/core/binary) | Leistungsstarke binäre Serialisierung mit Unterstützung für komplexe Objektgraphen und Versionsverwaltung. |
|  | [Erweiterte binäre Serialisierung](/core/binary_advanced) | Fortgeschrittene Anleitung für durchgängig allokationsfreie Kodierung und Dekodierung von Protokollnachrichten. |
|  | [CSV-Parser CsvFile](https://newlifex.com/core/csv_file) | Schnelles Lesen und Schreiben von CSV-Dateien mit benutzerdefinierten Trennzeichen und Datenbindung. |
|  | [CSV-Datenbank CsvDb](https://newlifex.com/core/csv_db) | Nutzt CSV-Dateien als Datenbank mit grundlegenden CRUD-Operationen. |
|  | [Leichtgewichtiger Excel-Reader ExcelReader](https://newlifex.com/core/excel_reader) | Schnelles Lesen von Excel-Dateien ohne Office-Abhängigkeit, auch bei großen Datenmengen. |
| **Caching** | [Einheitliche Cache-Schnittstelle ICache](https://newlifex.com/core/icache) | Definiert standardisierte Cache-Operationen und bietet Implementierungen wie Arbeitsspeicher und Redis. |
|  | [Cache-Architektur ICacheProvider](https://newlifex.com/core/icacheprovider) | Dient der Verwaltung und Bereitstellung von ICache-Instanzen und unterstützt das Nebeneinander mehrerer Caches. |
|  | [Speichercache MemoryCache](https://newlifex.com/core/memory_cache) | Leistungsstarker lokaler In-Memory-Cache mit Ablauf- und Kapazitätsrichtlinien. |
|  | [Objektpool ObjectPool](https://newlifex.com/core/object_pool) | Wiederverwendung teurer Objekte zur Leistungssteigerung und Verringerung des GC-Drucks. |
|  | [Objektpool Pool](/core/pool) | Leistungsstarker universeller Objektpool auf CAS-Basis zur Verringerung des GC-Drucks. |
|  | [Verteilter Cache NewLife.Redis](https://newlifex.com/core/redis) | Redis-basierte verteilte Cache-Implementierung mit Unterstützung für verschiedene Datenstrukturen. |
|  | [Vektorspeicher IVectorStore](/core/vector_store) | Schnittstelle zur Verwaltung von Vektormengen mit Top-K-Ähnlichkeitssuche; bietet eine Vektorsuche-Infrastruktur für KI-Anwendungen. |
| **Hochleistungspuffer** | [Datenpaket IPacket](https://newlifex.com/core/packet) | Kern-Datenpaketschnittstelle mit Zero-Copy-Slicing und verketteten Paketen. |
|  | [Puffer Buffers](/core/buffers) | Bibliothek für Hochleistungs-Pufferoperationen; bündelt Komponenten wie Span und gepoolte Writer. |
|  | [Gepoolter Writer PooledByteBufferWriter](/core/pooled_buffer_writer) | Gepoolter Pufferschreiber auf ArrayPool-Basis mit dynamischer Erweiterung. |
|  | [Span-Reader SpanReader](/core/span_reader) | Allokationsfreier Hochleistungs-Parser für Binärströme mit Unterstützung für Little- und Big-Endian. |
|  | [Span-Writer SpanWriter](/core/span_writer) | Allokationsfreier Hochleistungs-Schreiber für Binärdaten mit Unterstützung für Little- und Big-Endian. |
|  | [Span-Hilfen SpanHelper](/core/span_helper) | Sammlung von Erweiterungsmethoden für Span/ReadOnlySpan zur Vereinfachung von Speicheroperationen. |
|  | [Sequenzleser SequenceReader](/core/sequence_reader) | Sequenzieller Leser über Segmentgrenzen hinweg (für ältere Frameworks per Shim ergänzt, in allen 18 TFMs verfügbar). |
| **Anwendungsframework** | [Objektcontainer ObjectContainer](https://newlifex.com/core/object_container) | Leichtgewichtige Dependency Injection mit Unterstützung für mehrere Lebenszyklen und Injektionsmodi. |
|  | [Leichtgewichtiger Anwendungshost Host](https://newlifex.com/core/host) | Vereinfacht das Hosting von Anwendungen und erleichtert den Aufbau lang laufender Hintergrunddienste. |
|  | [Plugin-Framework IPlugin](https://newlifex.com/core/plugin) | Bietet einen standardisierten Mechanismus zur Plugin-Verwaltung für modulare Anwendungsentwicklung. |
|  | [Nebenläufigkeitsmodell Actor](https://newlifex.com/core/actor) | Vereinfachtes Nebenläufigkeitsmodell; Verarbeitung über Nachrichtenwarteschlangen erhöht den Durchsatz. |
|  | [Pipeline-Modell Pipeline](/core/pipeline) ⚠️ Stillgelegt, ersetzt durch den Protokollmodus (Frame-Pump) | Bidirektionales Verarbeitungskettenmodell zur Vereinheitlichung der Abläufe Read/Write/Open/Close. |
|  | [Verzögerte Warteschlange DeferredQueue](/core/deferred_queue) | Aggregiert hochfrequente Änderungen und schreibt sie gebündelt, um Write Amplification zu verringern. |
|  | [Ereignisbus EventBus](/core/eventbus) | Prozessinterne Ereignisverteilung mit Unterstützung für Themen-Routing und warteschlangenbasierte Nachrichten. |
|  | [Schwachreferenz-Delegat WeakAction](https://newlifex.com/core/weak_action) | Löst Speicherlecks bei Ereignisabonnements und ermöglicht sichere Ereignisbehandlung. |
|  | [Skript-Engine ScriptEngine](https://newlifex.com/core/script_engine) | Unterstützt dynamisches Kompilieren und Ausführen von C# und bietet flexible Skripterweiterungen. |
|  | [Komprimierung und Dekomprimierung](https://newlifex.com/core/compression) | Integriert mehrere Komprimierungsalgorithmen und bietet eine einheitliche Schnittstelle zur Datenkomprimierung. |
|  | [Archivkomprimierung Tar und 7Zip](/core/tar_7zip) | Behandelt fortgeschrittene Archivierungs- und Komprimierungsverfahren mit Tar/Tar.Gz und 7Zip. |
|  | [Maschineninformationen MachineInfo](https://newlifex.com/core/machine_info) | Umfassendes Abrufen von Hardwareinformationen unter Windows und Linux. |
|  | [Pinyin-Bibliothek PinYin](https://newlifex.com/core/pinyin) | Effiziente Bibliothek zur Umwandlung chinesischer Schriftzeichen in Pinyin mit Voll- und Kurzschreibweise und hervorragender Leistung. |
|  | [Datensatz DbTable](https://newlifex.com/core/dbtable) | Im Arbeitsspeicher gehaltene Datentabelle für komfortable Datenverarbeitung und -analyse. |
|  | [Seitennummerierungsparameter PageParameter](https://newlifex.com/core/page_parameter) | Kapselt die Paginierungslogik und unterstützt Paginierung in Szenarien wie Web und API. |
|  | [Snowflake-Algorithmus Snowflake](https://newlifex.com/core/snow_flake) | Algorithmus zur Erzeugung verteilter eindeutiger IDs, garantiert Eindeutigkeit auch bei hoher Nebenläufigkeit. |
| **Sicherheit und Token** | [Verteiltes digitales Signatur-Token TokenProvider](https://newlifex.com/core/token_provider) | Dient der Erzeugung und Validierung verteilter Token und sichert die Kommunikation zwischen mehreren Knoten. |
|  | [Universelles Web-Token JwtBuilder](https://newlifex.com/core/jwt) | Bietet Erzeugung, Dekodierung und Validierung von JWT-Token und vereinfacht die Web-Authentifizierung. |
|  | [Token-HTTP-Filter TokenHttpFilter](https://newlifex.com/core/token_http_filter) | Fügt Token automatisch in HTTP-Anfragen ein und erneuert sie bei 401/403 automatisch. |
| **Cloud-Dienste** | [Geo-Hash GeoHash](https://newlifex.com/core/geo_hash) | Kodiert Längen- und Breitengrade als Zeichenkette für die Suche nach nahe gelegenen Orten. |
|  | ⚠️ Baidu Maps/Amap wurden in eigenständige NuGet-Pakete ausgelagert. |  |
| **Netzwerkbibliothek** | [Netzwerkserver NetServer](https://newlifex.com/core/netserver) | Leistungsstarker TCP/UDP/Unix-Domain-Socket-Server mit Kapselung von Sitzungen und Datenverarbeitung. |
|  | [Netzwerkclient NetClient](/core/netclient) | Einheitlicher TCP/UDP/Unix-Netzwerkclient, vereinfacht Verbindungsaufbau sowie Senden und Empfangen. |
|  | [Nachricht IMessage](/core/message) | Anfrage-Antwort-Nachrichtenvertrag mit Richtungssemantik sowie Frame-Lesen und -Schreiben. |
|  | [Nachrichtenprotokoll-Stack MessagePump](/core/message_protocol) | Frame-Pump und zustandsloser Codec im selben Stack, unterstützt Streaming großer Frames und Backpressure. |
|  | [Datenpipeline Pipe](/core/packet_pipe) | Zero-Copy-Datenpipeline in Anlehnung an die Form von System.IO.Pipelines. |
|  | [Einfaches Remote-Nachrichtenaustauschprotokoll SRMP](https://newlifex.com/core/srmp) | Leichtgewichtiges RPC-Protokoll mit binärer Serialisierung und UDP-Übertragung. |
|  | [Lastverteilung und Failover LoadBalancer](/core/load_balancer) | Bietet drei Multi-Node-Scheduling-Strategien: Failover, Round-Robin und Racing. |
|  | [RPC-Kommunikation ApiServer](https://newlifex.com/core/api_server) | ⚠️ Verschoben nach [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
|  | [RPC-Kommunikation ApiClient](https://newlifex.com/core/api_client) | ⚠️ Verschoben nach [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
|  | [HTTP-Client ApiHttpClient](https://newlifex.com/core/api_http) | Kapselt HttpClient und vereinfacht Aufruf und Verarbeitung von Web-APIs. |
|  | [HTTP-Server HttpServer (eingebettete Geräte)](https://newlifex.com/core/httpserver) | Leichtgewichtiger HTTP-Server mit Middleware-Pipeline, parametrisierten Routen und Dienst für statische Dateien. |
|  | [Netzwerk-Download WebClientX](https://newlifex.com/core/webclient) | Erweiterter WebClient mit Multithreading und fortsetzbaren Downloads. |
|  | [WebSocket-Server (leistungsstarkes Werkzeug für IoT-Downlink-Benachrichtigungen)](https://newlifex.com/core/websocket) | Leistungsstarker WebSocket-Server, geeignet für IoT-Nachrichten-Push. |
|  | [WebSocket-Bidirektionalkommunikation](/core/websocket_guide) | Behandelt Handshake, Frame-Verarbeitung, Heartbeat und Verbindungsschließen in der Praxis. |
|  | [Dateispeicher-Client EasyClient](/core/easy_io) | HTTP-Dateispeicher-Client für die Anbindung an EasyIO oder OSS. |



## NewLife-Projektmatrix
Alle Projekte unterstützen standardmäßig net10.0/netstandard2.1/netstandard2.0/net4.62/net4.5; ältere Versionen (2024.0801) unterstützen net4.0/net2.0  

| Projekt | Jahr | Beschreibung |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
| Basiskomponenten |  | Unterstützen andere Middleware- sowie Produktprojekte |
| [NewLife.Core](https://github.com/NewLifeX/X) | 2002 | Kernbibliothek: Protokollierung, Konfiguration, Cache, Netzwerk, Serialisierung, APM-Performance-Tracing |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 2005 | Big-Data-Middleware, zig Milliarden Datensätze pro Tabelle, MySql/SQLite/SqlServer/Oracle/PostgreSql/DaMeng, automatische Tabellenpartitionierung, Lese-/Schreibtrennung |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 2005 | Netzwerkbibliothek, Durchsatz im Zehn-Millionen-Bereich pro Server (22,66 Mio. tps), Millionen Verbindungen pro Server (4 Mio. TCP-Langzeitverbindungen) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011 | Protokollkommunikationsbibliothek mit C/S-Anwendungs-Kommunikationsframework, unterstützt Http/RPC-Kommunikationsframeworks, hoher Durchsatz, IoT-Geräte mit geringem Overhead und einfacher Anbindung |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 2010 | Cube-Schnellentwicklungsplattform mit integrierten Benutzerberechtigungen, SSO-Anmeldung, OAuth-Server u. a., in Projekten mit 10 Milliarden Datensätzen pro Tabelle validiert |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 2008 | Dienstverwaltungskomponente, installiert Anwendungen als Betriebssystem-Daemons: Windows-Dienste, Linux-Systemd |
| [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero) | 2020 | Zero-Gerüst der nullten Generation, Projektvorlagen NewLife.Templates auf Basis des NewLife-Komponentenökosystems, Web, WebApi, Service |
| Middleware |  | Anbindung an bekannte Middleware-Plattformen |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | 2017 | Redis-Client, Latenz im Mikrosekundenbereich, Millionen-Durchsatz, umfangreiche Nachrichtenwarteschlangen, in Projekten mit zig Milliarden Datensätzen validiert |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018 | Vollständig verwalteter RocketMQ-Client, unterstützt Apache RocketMQ und Alibaba Cloud Message Queue, in Projekten im Milliardenbereich validiert |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | 2019 | IoT-Nachrichtenprotokoll, MqttClient/MqttServer, Client unterstützt Alibaba Cloud IoT |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 2022 | IoT-Standardbibliothek, definiert verschiedene Kommunikationsprotokoll-Standards und -Spezifikationen für das IoT |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 2022 | ModbusTcp/ModbusRTU/ModbusASCII, implementiert auf Basis der IoT-Standardbibliothek, unterstützt ZeroIoT-Plattform und IoTEdge-Gateway |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 2022 | Siemens-SPS-Protokoll, implementiert auf Basis der IoT-Standardbibliothek, unterstützt IoT-Plattform und IoTEdge |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 2022 | Kartenkomponentenbibliothek, kapselt Baidu Maps, Amap, Tencent Maps und Tianditu |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 2023 | Audio-Codec-Bibliothek, PCM/ADPCMA/G711A/G722U/WAV/AAC |
| Produktplattform |  | Plattformprodukte der Produktebene, einsatzbereit nach Kompilierung und Bereitstellung, individuell anpassbar |
| [Stardust](https://github.com/NewLifeX/Stardust) | 2018 | Stardust, verteilte Serviceplattform: Knotenverwaltung, APM-Überwachungszentrale, Konfigurationszentrale, Registrierungszentrale, Veröffentlichungszentrale |
| [AntJob](https://github.com/NewLifeX/AntJob) | 2019 | Ameisen-Scheduler, verteilte Big-Data-Rechenplattform (Echtzeit/Offline), Sharding-Prinzip des Ameisenumzugs, in Projekten mit Billionen Datensätzen validiert |
| [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP) | 2021 | Unternehmens-ERP: Produktverwaltung, Kundenverwaltung, Vertriebsverwaltung, Lieferantenverwaltung |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 2006 | CrazyCoder-Werkzeug, zahlreiche Entwicklerwerkzeuge: Netzwerk, serielle Schnittstelle, Ver- und Entschlüsselung, reguläre Ausdrücke, Modbus, MQTT |
| [EasyIO](https://github.com/NewLifeX/EasyIO) | 2023 | Einfacher Dateispeicher, unterstützt zentrale Dateiablage in verteilten Systemen. |
| [XProxy](https://github.com/NewLifeX/XProxy) | 2005 | Produktreifer Reverse-Proxy: NAT-Proxy, Http-Proxy |
| [HttpMeter](https://github.com/NewLifeX/HttpMeter) | 2022 | HTTP-Lasttestwerkzeug |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 2015 | Git-Quellcodeverwaltungssystem |
| [SmartOS](https://github.com/NewLifeX/SmartOS) | 2014 | Eingebettetes Betriebssystem, vollständig unabhängig und eigenständig, unterstützt die ARM-Cortex-M-Chiparchitektur |
| [SmartA2](https://github.com/NewLifeX/SmartA2) | 2019 | Eingebetteter Industriecomputer, IoT-Edge-Gateway, leistungsstarker .NET8-Host, eingesetzt in Industrie, Landwirtschaft, Verkehr und Medizin |
| FIoT-IoT-Plattform | 2020 | Ganzheitliche IoT-Lösung für Bauwesen, Umweltschutz und Landwirtschaft, Integration von Hard-/Software und Big-Data-Analyse, in Projekten mit Hunderttausenden Datenpunkten pro Server validiert |



---
# NewLife-Entwicklungsteam
![XCode](https://newlifex.com/logo.png)

Das NewLife-Team wurde 2002 gegründet und ist Anbieter von Lösungen für die IoT-Branche des neuen Zeitalters; es widmet sich der Beratung zu Hard- und Softwareanwendungen sowie Systemarchitekturplanung und Entwicklungsdienstleistungen. Über 5,23 Millionen NuGet-Downloads insgesamt. Die Kernkomponenten des Teams (XCode / Redis / Stardust / AntJob / IoT usw.) sind in Branchen wie Energieversorgung, Industrie, Verkehr, Umweltschutz, Medizin sowie Museen und Kulturgütern in großem Umfang im Einsatz.

Wir verbessern uns kontinuierlich und streben danach, ein langfristig vertrauenswürdiger Partner für .NET-, IoT- und verteilte Technologien zu sein.

Website: https://newlifex.com   Open-Source-Organisation: https://github.com/NewLifeX  
WeChat-Offizieller-Account:  
![智能大石头](https://newlifex.com/stone.jpg)

---
> Wenn dieses Projekt dir hilft, freuen wir uns über einen Star, das Teilen mit dem Team oder Feedback und Verbesserungsvorschläge über Issues/PRs.
