# NewLife.Core - Biblioteca Principal

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

**NewLife.Core** es el fundamento del ecosistema NewLife: un solo paquete para registro de logs, configuración, caché, red, serialización, programación de tareas, contenedor de objetos y seguimiento APM. Cubre 11 frameworks de destino desde net45 hasta net10.0 y sustenta XCode, Cube, Redis, Stardust y más de 30 proyectos hermanos.

Tutorial de la biblioteca principal: [https://newlifex.com/core](https://newlifex.com/core)  

> ⚠️ Esta traducción puede quedar desactualizada; la versión autorizada es el [README en inglés](README.md).

**Índice**: [Inicio rápido](#inicio-rápido) · [Por qué NewLife.Core](#por-qué-newlifecore) · [Funcionalidades principales](#funcionalidades-principales) · [Ecosistema](#ecosistema) · [Documentación](#documentación) · [Contribución](#contribución) · [Equipo](#equipo) · [Licencia](#licencia)

## Inicio rápido

### Instalación

```bash
dotnet add package NewLife.Core
```

### Ejemplo mínimo

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // console log (also writes to file)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // in-memory cache
cache.Set("key", "value", 60);          // expires in 60 seconds
var value = cache.Get<String>("key");   // read back
```

### Plantilla de proyecto

Ejecuta los siguientes comandos en la línea de comandos para probar rápidamente los componentes de NewLife:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### Compatibilidad de plataformas

| Elemento | Cobertura |
| --- | --- |
| Frameworks de destino | 11 en un solo paquete: net45, net461, net462, netstandard2.0, netstandard2.1, net6.0, net6.0-windows, net8.0, net8.0-windows, net10.0, net10.0-windows |
| Sistema operativo y arquitectura | Windows, Linux, macOS, Android, iOS; x64, x86, arm64, arm, loongarch64, riscv64, s390x |
| NativeAOT | <!--aot-scenarios:start-->31<!--aot-scenarios:end--> escenarios del núcleo medidos; matriz de smoke de CI multi-RID (win-x64, linux-x64, linux-arm64 y linux-loongarch64 son puertas bloqueantes). Los plugins y el scripting no son compatibles con AOT por diseño; consulta el [análisis de compatibilidad AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) |

## Por qué NewLife.Core

- **Una sola referencia para la infraestructura común**: registro de logs, configuración, caché, red, serialización, programación de tareas, contenedor de objetos e inyección de dependencias; una única referencia es suficiente.
- **Amplia cobertura de versiones**: 11 frameworks de destino en un solo paquete (net45 → net10.0, incluidas las variantes de Windows); la misma API para sistemas heredados y proyectos nuevos.
- **Probado a escala de ecosistema**: la misma base ejecuta XCode (tablas únicas de 10 000 millones de filas), Net (22,66 M tps), Redis y Stardust (decenas de miles de millones de registros).
- **Dependencias contenidas y licencia permisiva**: solo la BCL y paquetes de compatibilidad de Microsoft; licencia MIT, por lo que puedes modificar y renombrar (ver [Licencia](#licencia)).

## Funcionalidades principales

<details>
<summary><b>Extensiones básicas</b> (10)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Conversión de tipos Utility](https://newlifex.com/core/utility) | Ofrece conversión de tipos eficiente, con soporte de diversos formatos y manejo de excepciones. |
| [Extensión de cadenas StringHelper](https://newlifex.com/core/string_helper) | Incluye múltiples operaciones de cadenas, como recorte, codificación y formato, entre otras. |
| [Extensión de rutas PathHelper](https://newlifex.com/core/path_helper) | Gestiona de forma inteligente múltiples formatos de ruta para lograr la compatibilidad multiplataforma del código. |
| [Extensión de datos IOHelper](https://newlifex.com/core/io_helper) | Optimiza las operaciones de E/S y ofrece conversiones eficientes entre flujos de datos y matrices de bytes. |
| [Extensión de procesos ProcessHelper](https://newlifex.com/core/process_helper) | Simplifica las operaciones con procesos y ofrece métodos cómodos para obtener y gestionar información de procesos. |
| [Información de tiempo de ejecución Runtime](https://newlifex.com/core/runtime) | Proporciona métodos sencillos para obtener información en tiempo de ejecución de la aplicación, facilitando la depuración y el diagnóstico. |
| [Objeto desechable DisposeBase](https://newlifex.com/core/disposebase) | Proporciona un patrón estándar de liberación de recursos que evita eficazmente fugas de memoria y de recursos. |
| [Extensión de reflexión Reflect](https://newlifex.com/core/reflect) | Herramienta de reflexión de alto rendimiento para leer y escribir miembros de objetos con rapidez. |
| [Extensión de seguridad SecurityHelper](https://newlifex.com/core/security_helper) | Integra múltiples algoritmos de cifrado y descifrado como RSA/AES/DES/MD5, simplificando las operaciones de seguridad. |
| [Hash de latitud y longitud GeoHash](https://newlifex.com/core/geo_hash) | Codifica la latitud y la longitud en cadenas para la búsqueda de ubicaciones cercanas. |

</details>

<details>
<summary><b>Registro y seguimiento</b> (2)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Registro ILog](https://newlifex.com/core/log) | Admite múltiples formas de salida de registros y ofrece una potente escritura asíncrona. |
| [Seguimiento de trazas ITracer](https://newlifex.com/core/tracer) | Potente seguimiento de rendimiento APM, con soporte de integración con la plataforma Stardust. |

</details>

<details>
<summary><b>Programación temporizada</b> (3)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Temporizador avanzado TimerX](https://newlifex.com/core/timerx) | Temporizador de alta precisión, con soporte de programación Cron y ejecución periódica asíncrona. |
| [Expresión Cron](https://newlifex.com/core/cron) | Ofrece tareas programadas similares a Cron de Linux, con coincidencia a nivel de segundos. |
| [Proveedor de tiempo TimeProvider](https://newlifex.com/core/timerx_timeprovider) | Abstracción de tiempo unificada y cálculo de duración de alta precisión, para facilitar la compatibilidad entre versiones. |

</details>

<details>
<summary><b>Sistema de configuración</b> (4)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Sistema de configuración Config](https://newlifex.com/core/config) | Framework de configuración unificado, con soporte de múltiples formatos, múltiples orígenes y recarga en caliente. |
| [Proveedor de configuración IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | Interfaz de configuración unificada, con soporte de callbacks y modelo de nodos de configuración. |
| [Configuración central Setting](https://newlifex.com/core/setting) | Gestiona la configuración central de los componentes y controla comportamientos básicos como registros y redes. |
| [Configuración del sistema SysConfig](https://newlifex.com/core/sysconfig) | Gestiona la configuración del sistema de la aplicación, con soporte de configuración local, distribuida y cifrada. |

</details>

<details>
<summary><b>Serialización</b> (7)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Serialización Json](https://newlifex.com/core/json) | Herramienta JSON de alto rendimiento, con soporte de serialización en diversos escenarios complejos. |
| [Serialización Xml](https://newlifex.com/core/xml) | Proporciona serialización y deserialización XML, con conversión bidireccional entre objetos y XML. |
| [Serialización binaria](https://newlifex.com/core/binary) | Serialización binaria de alto rendimiento, con soporte de grafos de objetos complejos y control de versiones. |
| [Serialización binaria avanzada](https://newlifex.com/core/advanced_binary) | Guía avanzada de codificación y decodificación sin asignaciones durante todo el proceso de los mensajes de protocolo. |
| [Análisis de Csv CsvFile](https://newlifex.com/core/csv_file) | Lectura y escritura rápida de archivos CSV, con soporte de delimitadores personalizados y enlace de datos. |
| [Base de datos Csv CsvDb](https://newlifex.com/core/csv_db) | Usa archivos CSV como base de datos, con soporte de operaciones básicas de creación, consulta, actualización y eliminación. |
| [Lector de Excel ligero ExcelReader](https://newlifex.com/core/excel_reader) | Lee Excel rápidamente sin dependencias de Office, con soporte de grandes volúmenes de datos. |

</details>

<details>
<summary><b>Caché</b> (7)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Interfaz de caché unificada ICache](https://newlifex.com/core/icache) | Define operaciones de caché estándar y ofrece múltiples implementaciones como memoria y Redis. |
| [Arquitectura de caché ICacheProvider](https://newlifex.com/core/icacheprovider) | Se usa para gestionar y proporcionar instancias de ICache, con soporte de múltiples cachés coexistentes. |
| [Caché en memoria MemoryCache](https://newlifex.com/core/memory_cache) | Caché en memoria local de alto rendimiento, con soporte de políticas de expiración y capacidad. |
| [Grupo de objetos ObjectPool](https://newlifex.com/core/object_pool) | Se usa para reutilizar objetos costosos, mejorando el rendimiento y reduciendo la presión del GC. |
| [Grupo de objetos Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | Grupo de objetos genérico de alto rendimiento basado en CAS, que reduce la presión del GC. |
| [Caché distribuida NewLife.Redis](https://newlifex.com/core/redis) | Implementación de caché distribuida basada en Redis, con soporte de múltiples estructuras de datos. |
| [Almacén de vectores IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | Interfaz de gestión de colecciones de vectores, con soporte de búsqueda de similitud Top-K, que proporciona infraestructura de búsqueda vectorial para aplicaciones de IA. |

</details>

<details>
<summary><b>Búfer de alto rendimiento</b> (7)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Paquete de datos IPacket](https://newlifex.com/core/packet) | Interfaz central de paquetes de datos para segmentación sin copia y paquetes encadenados. |
| [Búferes Buffers](https://newlifex.com/core/buffers) | Biblioteca de operaciones de búfer de alto rendimiento que reúne componentes como Span y escritura con agrupación. |
| [Escritor con agrupación PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | Escritor de búfer con agrupación basado en ArrayPool, con soporte de expansión dinámica. |
| [Lector Span SpanReader](https://newlifex.com/core/span_reader) | Analizador de flujos binarios de alto rendimiento sin asignaciones, con soporte de little/big endian. |
| [Escritor Span SpanWriter](https://newlifex.com/core/span_writer) | Escritor de datos binarios de alto rendimiento sin asignaciones, con soporte de little/big endian. |
| [Auxiliar Span SpanHelper](https://newlifex.com/core/span_helper) | Conjunto de métodos de extensión para Span/ReadOnlySpan que simplifica las operaciones de memoria. |
| [Lector secuencial SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | Lector secuencial entre segmentos (en frameworks antiguos se completa mediante shims; disponible en los 11 TFM). |

</details>

<details>
<summary><b>Framework de aplicaciones</b> (15)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Contenedor de objetos ObjectContainer](https://newlifex.com/core/object_container) | Inyección de dependencias ligera, con soporte de múltiples ciclos de vida y modos de inyección. |
| [Host de aplicación ligero Host](https://newlifex.com/core/host) | Simplifica el alojamiento de aplicaciones y facilita la creación de servicios en segundo plano de larga duración. |
| [Framework de plugins IPlugin](https://newlifex.com/core/plugin) | Proporciona un mecanismo estandarizado de gestión de plugins para el desarrollo modular de aplicaciones. |
| [Modelo de paralelismo Actor](https://newlifex.com/core/actor) | Modelo de programación concurrente simplificado que mejora el rendimiento mediante el procesamiento de colas de mensajes. |
| [Cola diferida DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | Agrega cambios de alta frecuencia y los persiste por lotes, reduciendo la amplificación de escrituras. |
| [Bus de eventos EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | Distribución de eventos dentro del proceso, con soporte de enrutamiento por tema y mensajes estilo cola. |
| [Delegado de referencia débil WeakAction](https://newlifex.com/core/weak_action) | Resuelve el problema de fugas de memoria de las suscripciones a eventos y permite un manejo seguro de eventos. |
| [Motor de scripts ScriptEngine](https://newlifex.com/core/script_engine) | Admite la compilación y ejecución dinámica de C#, proporcionando una extensibilidad flexible mediante scripts. |
| [Compresión y descompresión](https://newlifex.com/core/compression) | Integra múltiples algoritmos de compresión y ofrece una interfaz unificada para comprimir datos. |
| [Archivado comprimido Tar y 7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Cubre el uso avanzado de archivado y compresión para Tar/Tar.Gz y 7Zip. |
| [Información de la máquina MachineInfo](https://newlifex.com/core/machine_info) | Obtiene información completa del hardware, con soporte de sistemas Windows y Linux. |
| [Biblioteca de pinyin PinYin](https://newlifex.com/core/pinyin) | Biblioteca eficiente de conversión de caracteres chinos a pinyin, con soporte de escritura completa y abreviada y un rendimiento superior. |
| [Conjunto de datos DbTable](https://newlifex.com/core/dbtable) | Tabla de datos en memoria para un procesamiento y análisis de datos cómodos. |
| [Parámetros de paginación PageParameter](https://newlifex.com/core/page_parameter) | Encapsula la lógica de paginación, con soporte en escenarios como Web y API. |
| [Algoritmo Snowflake Snowflake](https://newlifex.com/core/snow_flake) | Algoritmo de generación de identificadores únicos distribuidos que garantiza la unicidad bajo alta concurrencia. |

</details>

<details>
<summary><b>Seguridad y tokens</b> (3)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Token de firma digital distribuida TokenProvider](https://newlifex.com/core/token_provider) | Genera y valida tokens distribuidos, garantizando la seguridad entre múltiples nodos. |
| [Token web universal JwtBuilder](https://newlifex.com/core/jwt) | Proporciona generación, decodificación y validación de tokens JWT, simplificando la autenticación Web. |
| [Filtro HTTP de token TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | Inyecta automáticamente el token en las solicitudes HTTP e intercepta 401/403 para renovarlo automáticamente. |

</details>

<details>
<summary><b>Biblioteca de red</b> (13)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Servidor de red NetServer](https://newlifex.com/core/netserver) | Servidor de sockets TCP/UDP/Unix de alto rendimiento que encapsula sesiones y manejo de datos. |
| [Cliente de red NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | Cliente de red TCP/UDP/Unix unificado que simplifica la conexión y el envío/recepción. |
| [Mensaje IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | Contrato de mensaje solicitud-respuesta con semántica de dirección y lectura/escritura de tramas. |
| [Pila de protocolo de mensajes MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | Pila única de bomba de tramas + códec sin estado, con soporte de cuerpos de trama en streaming de gran tamaño y contrapresión. |
| [Canalización de datos Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | Canalización de datos sin copia alineada con la forma de System.IO.Pipelines. |
| [Protocolo simple de intercambio de mensajes remotos SRMP](https://newlifex.com/core/srmp) | Protocolo RPC ligero con soporte de serialización binaria y transporte UDP. |
| [Equilibrio de carga y conmutación por error LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Ofrece tres estrategias de programación multi-nodo: conmutación por error, sondeo y carrera. |
| [Cliente HTTP ApiHttpClient](https://newlifex.com/core/api_http) | Basado en un contenedor de HttpClient, simplifica la invocación y el manejo de API Web. |
| [Servidor HTTP HttpServer (dispositivos embebidos)](https://newlifex.com/core/httpserver) | Servidor HTTP ligero con soporte de canalizaciones de middleware, enrutamiento con parámetros y servicio de archivos estáticos. |
| [Descarga de red WebClientX](https://newlifex.com/core/webclient) | WebClient mejorado con soporte de descargas multihilo y reanudables. |
| [Servidor WebSocket (herramienta ideal para notificaciones descendentes en IoT)](https://newlifex.com/core/websocket) | Servidor WebSocket de alto rendimiento, adecuado para la notificación de mensajes en IoT. |
| [Comunicación bidireccional WebSocket](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | Presenta prácticas de handshake, manejo de tramas, heartbeat y cierre de conexión. |
| [Cliente de almacenamiento de archivos EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | Cliente HTTP de almacenamiento de archivos para EasyIO u OSS. |

</details>

> RPC (`ApiServer`/`ApiClient`) se ha trasladado a [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting).

## Ecosistema

NewLife es un ecosistema por capas de proyectos de código abierto con versionado independiente. El mapa de capas siguiente se comparte literalmente en todos los repositorios de NewLife; la tabla detallada añade el posicionamiento y la tracción de cada proyecto.

| Capa | Proyectos |
| --- | --- |
| Fundación | Core · XCode · Net · Remoting · Agent · Templates |
| Datos y mensajería | Redis · RocketMQ · MQTT · NovaDb · MySql |
| Web, IA y distribuido | Cube · Stardust · AntJob · NewLife.AI |
| Industrial e IoT | IoT · Modbus · Siemens · Omron · Melsec · OPC · BACnet · ZeroIoT |
| Telemática vehicular | JT808 · JT809 · JT1078 |
| Componentes de dominio | Office · Audio · Video · Map · IP |
| Herramientas y productos | CrazyCoder · GitCandy · LuckyClover |

### Detalles de los proyectos

| Proyecto | Qué hace | Tracción |
| --- | --- | --- |
| **Fundación** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Registro de logs, configuración, caché, red, serialización, programación de tareas y seguimiento APM. | ★1.9k · 1.91M |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Middleware de datos: tablas únicas a escala de decenas de miles de millones, particionado automático, separación lectura/escritura y más de 10 bases de datos. | ★104 · 541k |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Biblioteca de red de alto rendimiento: 22,66 M tps por máquina y 4 M conexiones TCP persistentes. | ★313 · 74.5k |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Framework de comunicación por protocolo y RPC para aplicaciones cliente/servidor y dispositivos IoT. | ★16 · 272k |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Instala aplicaciones .NET como demonios del sistema operativo: servicios de Windows y systemd de Linux. | ★45 · 108k |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | Plantillas de proyecto `dotnet new` para consola, web, API, servicio, datos e IoT. | ★82 · 14.9k |
| **Datos y mensajería** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Cliente Redis con latencia de microsegundos, millones de operaciones por segundo y colas de mensajes integradas. | ★878 · 723k |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Cliente RocketMQ totalmente administrado para Apache RocketMQ y Alibaba Cloud Message Queue. | ★225 · 120k |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | Cliente y servidor MQTT, incluida la conectividad con Alibaba Cloud IoT. | ★150 · 99.4k |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Base de datos híbrida en C#: embebida o en red, relacional, series temporales, cola de mensajes y KV en una sola. | ★12 · 3.4k |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | Controlador y capa de ayuda de MySql construidos sobre la pila de datos de NewLife. | ★23 · 8.4k |
| **Web, IA y distribuido** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Framework rápido para plataformas de administración: permisos, SSO, servidor OAuth y andamiaje CRUD. | ★637 · 186k |
| [Stardust](https://github.com/NewLifeX/Stardust) | Plataforma distribuida de servicios: gestión de nodos, APM, centro de configuración, registro y centro de despliegues. | ★354 · 274k |
| [AntJob](https://github.com/NewLifeX/AntJob) | Plataforma de tareas distribuidas y programación de datos en tiempo real con particionado, reintentos y relleno histórico. | ★429 · 56.8k |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Cliente unificado para 46 proveedores de LLM más ChatAI, una aplicación web de chat lista para usar. | ★20 · 11.4k |
| **Industrial e IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | Biblioteca estándar de IoT que define contratos de protocolo y abstracciones de dispositivos. | ★30 · 104k |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Implementación de Modbus TCP/RTU/ASCII sobre NewLife.IoT. | ★27 · 48.7k |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Protocolo para PLC Siemens sobre NewLife.IoT. | ★30 · 14.3k |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Protocolo para PLC Omron sobre NewLife.IoT. | ★8 · 11.1k |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Protocolo para PLC Mitsubishi Melsec sobre NewLife.IoT. | ★34 · 10.8k |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | Cliente OPC para el acceso a datos industriales. | ★9 · — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | Pila de protocolo BACnet para la automatización de edificios. | ★8 · 9k |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Plataforma IoT de código abierto y ligera con gestión de pasarelas y dispositivos. | ★10 · — |
| **Telemática vehicular** | | |
| NewLife.JT808 | Protocolo de posicionamiento vehicular JT/T 808: más de 70 cuerpos de mensaje, 2011/2013/2019. | — |
| NewLife.JT809 | Protocolo plataforma a plataforma JT/T 809 para autoridades de transporte. | — · 135 |
| NewLife.JT1078 | Vídeo a bordo JT/T 1078: análisis RTP, remultiplexado FLV/fMP4 y transmisión en directo. | — |
| **Componentes de dominio** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Un solo paquete para Excel, Word, PPT, PDF y más de 15 formatos de documento. | ★11 · 12.9k |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Audio multiplataforma: códecs, E/S de dispositivos, DSP, contenedores y streaming. | ★5 · 6.6k |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Códecs de vídeo, formatos de contenedor, procesamiento de fotogramas y streaming. | ★2 · 610 |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Interfaz unificada sobre Baidu, AMap, Tencent y Tianditu con geocodificación y conversión de coordenadas. | ★15 · 24.8k |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | Geolocalización IPv4 con una base de datos offline mapeada en memoria y una API en línea de respaldo. | ★16 · 84.7k |
| **Herramientas y productos** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Caja de herramientas del desarrollador: red, puerto serie, criptografía, expresiones regulares, Modbus y MQTT. | ★330 · 42.1k |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Sistema de gestión de código fuente Git autoalojado. | ★49 · — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Instala automáticamente el runtime de .NET correspondiente cuando se ejecuta desde un host más antiguo. | ★13 · — |

> Los más de 30 paquetes no listados aquí (FTP, LoRa, extensiones de Office, controladores de protocolo, etc.) están disponibles en la [organización NewLifeX](https://github.com/NewLifeX) y en [newlifex.com](https://newlifex.com).

## Documentación

### Guías

- [Contenido de la biblioteca principal](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): índice de artículos de los componentes principales, agrupados por módulo
- [Análisis de compatibilidad AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): alcance de NativeAOT, conclusiones medidas (smoke <!--aot-smoke:start-->31/31<!--aot-smoke:end-->), matriz de RID y límites conocidos
- [Localización](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md): modelo de mensajes, tablas de idioma, sobrescrituras en proyectos derivados y adición de idiomas en tiempo de ejecución
- [Lista de verificación de localización para proyectos derivados](https://github.com/NewLifeX/X/blob/master/Doc/下游多语言接入清单.md): pasos de integración, reglas de idioma, puertas de CI y plantillas de insignias
- [Arquitectura móvil](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md): pila móvil de .NET, selección del framework de UI, empaquetado APK y notas de AOT
- [Ejemplos](https://github.com/NewLifeX/X/tree/master/Samples): ejemplos de consola, servicio, Http, IoT y móvil

### Notas de versión

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md): historial de versiones

## Contribución

### Compilar y probar

La solución se abre en Visual Studio (última versión estable) y también compila con la CLI de .NET; los archivos de proyecto se mantienen deliberadamente simples para que un clon recién descargado compile sin configuración adicional.

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- Ejecución totalmente paralela: `run-tests.ps1` (paralelismo multi-host, protección por tiempo de espera y resumen trx)
- Smoke de NativeAOT: `AotSmoke/`, CI en `.github/workflows/aot.yml`
- Benchmarks: `Benchmark/` (BenchmarkDotNet)

### Estructura del repositorio

| Directorio | Descripción |
| --- | --- |
| `NewLife.Core/` | Código fuente de la biblioteca principal (registro de logs, red, caché, serialización, …) |
| `NewLife.Core.SourceGenerator/` | Generador de código fuente que se distribuye como `analyzers/` dentro del paquete |
| `Doc/` | Documentación de capacidades e índice de artículos |
| `Samples/` | Ejemplos de consola / servicio / Http / IoT / móvil |
| `Benchmark/` | Benchmarks de rendimiento con BenchmarkDotNet |
| `XUnitTest.Core/` | Pruebas unitarias principales (net10.0) |
| `XUnitTest.Compat/`, `XUnitTest.Net6/` | Pruebas de compatibilidad entre versiones |
| `AotSmoke/` | Proyecto de smoke de NativeAOT |

### Publicación

Envía una etiqueta con prefijo `v` (por ejemplo `v12.0.2026.0927`) para activar `.github/workflows/publish.yml`. El flujo empaqueta el paquete, lo publica en NuGet.org y GitHub Packages, y crea una GitHub Release cuyas notas se toman de la sección correspondiente de `ChangeLog.md` (los activos incluyen nupkg y snupkg). Una ejecución manual solo publica paquetes y no crea una Release.

### Documentación de desarrollo

El directorio `Doc/` mantiene un conjunto de documentación de capacidades que refleja el código: requisitos → seguimiento de funcionalidades → arquitectura. Estos documentos están escritos en chino.

- [Documento de requisitos](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): visión, objetivos principales y requisitos funcionales
- [Lista de funcionalidades](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): estado de implementación, pruebas y comentarios de cada funcionalidad
- [Diseño de arquitectura](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): capas, componentes y decisiones clave de diseño

### Comentarios y sugerencias

- Antes de enviar, asegúrate de que `dotnet build` y las pruebas unitarias relevantes pasan; sigue las convenciones del repositorio para el estilo de código y los mensajes de commit.
- Informa de errores y sugerencias mediante [Issue](https://github.com/NewLifeX/X/issues); las Stars y los PR son bienvenidos.

## Equipo

NewLife se fundó en 2002 y se centra en .NET, IoT y los sistemas distribuidos. Sus componentes de código abierto se han descargado <!--nuget-total:start-->6.04M<!--nuget-total:end--> veces desde NuGet (a fecha de 2026-10) y se usan en proyectos de energía, industria, transporte, protección ambiental, salud y patrimonio cultural.

Sitio web: https://newlifex.com · Organización de código abierto: https://github.com/NewLifeX  
Cuenta oficial de WeChat:  
![智能大石头](https://newlifex.com/stone.jpg)

## Licencia

Este proyecto se distribuye bajo la licencia [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE); el certificado de nombre seguro `newlife.snk` se publica para que puedas compilar y reemplazar los ensamblados por tu cuenta, y el código fuente puede modificarse y redistribuirse sin atribución.

> Si este proyecto te resulta útil, te invitamos a darle una Star, compartirlo con tu equipo o enviar sugerencias de mejora mediante Issue/PR.
