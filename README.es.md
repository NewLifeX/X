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
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm32%20%7C%20LoongArch64%20%7C%20riscv64%20%7C%20s390x-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/X?logo=github)](https://github.com/NewLifeX/X/graphs/contributors)

<!-- lang:start -->
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

NewLife es un ecosistema integral de componentes .NET que ofrece una infraestructura de alto rendimiento para construir aplicaciones escalables. Proporciona funcionalidades básicas como registro de logs, redes, serialización, caché y multihilo, y sirve como base para una amplia gama de aplicaciones, desde servicios web hasta dispositivos IoT.

Tutorial de la biblioteca principal: [https://newlifex.com/core](https://newlifex.com/core)

> ⚠️ Esta traducción puede quedar desactualizada; la versión autorizada es el [README en chino](Readme.MD).

**Índice**: [Descripción general del framework](#descripción-general-del-framework) · [Por qué NewLife.Core](#por-qué-newlifecore) · [Inicio rápido](#inicio-rápido) · [Funcionalidades principales](#funcionalidades-principales) · [Índice de documentación](#índice-de-documentación) · [Estructura del repositorio](#estructura-del-repositorio) · [Compilar y probar](#compilar-y-probar) · [Matriz de proyectos de NewLife](#matriz-de-proyectos-de-newlife) · [Contribución y comentarios](#contribución-y-comentarios) · [Licencia](#licencia)

## Descripción general del framework

NewLife está compuesto por múltiples bibliotecas de componentes, y NewLife.Core proporciona la base. El framework es compatible con múltiples marcos de destino desde net45 hasta net10.0 (.NET Framework 4.5 a .NET 10.0), lo que permite una amplia compatibilidad en distintos entornos.

**NativeAOT**: las rutas principales ya son compatibles (<!--aot-scenarios:start-->31<!--aot-scenarios:end--> escenarios probados + CI contra regresiones); las capacidades dinámicas como plugins y scripts no son compatibles por diseño; consulta los límites en [Análisis de compatibilidad AOT (中文)](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md).


## Por qué NewLife.Core

- **Una biblioteca para la infraestructura común**: registro, configuración, caché, red, serialización, programación, contenedor de objetos e inyección de dependencias, con una sola referencia.
- **Amplia cobertura de versiones**: 11 frameworks de destino en un solo paquete (net45 → net10.0, incluidas variantes de Windows); la misma API para sistemas heredados y proyectos nuevos.
- **Compatible con NativeAOT**: <!--aot-scenarios:start-->31<!--aot-scenarios:end--> escenarios del núcleo medidos y protegidos por una matriz de CI multi-RID; los límites están en el [análisis de compatibilidad AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md).
- **Dependencias contenidas y licencia permisiva**: solo la BCL y paquetes de compatibilidad de Microsoft; licencia MIT, se permite modificar y renombrar (ver [Licencia](#licencia)).

## Inicio rápido

### Instalación

```bash
dotnet add package NewLife.Core
```

### Ejemplo mínimo

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // log de consola (también escribe en archivo)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // caché en memoria
cache.Set("key", "value", 60);          // expira en 60 segundos
var value = cache.Get<String>("key");   // lectura
```

### Compilar y personalizar

Los componentes de NewLife han sido creados conjuntamente por la comunidad durante más de 20 años y usan la licencia de código abierto MIT. **Cualquiera puede modificarlos y redistribuirlos libremente** (sin necesidad de indicar la fuente). Muchas empresas que construyen sus frameworks de desarrollo internos sobre esta base pueden incluso personalizarlos de forma privada reemplazando en masa todas las cadenas `NewLife` del código fuente por el nombre de su compañía.

El equipo mantiene siempre una actitud abierta: no solo admite abrir y compilar la solución con Visual Studio (última versión estable), sino que también es compatible con la compilación por línea de comandos mediante `dotnet build`; los archivos de proyecto evitan funcionalidades complejas en busca de la simplicidad y la facilidad de uso, logrando realmente funcionar directamente tras la instalación.

Publicamos el certificado de nombre seguro `newlife.snk` para permitir compilar y reemplazar los ensamblados por cuenta propia.

### Plantilla de proyecto

Ejecuta los siguientes comandos en la línea de comandos para probar rápidamente los componentes de NewLife:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

## Índice de documentación

### Documentación de uso

- [Catálogo de la biblioteca principal (中文)](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): índice de artículos de los componentes principales
- [Análisis de compatibilidad AOT (中文)](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): alcance de soporte de NativeAOT, conclusiones de pruebas reales (smoke <!--aot-smoke:start-->31/31<!--aot-smoke:end-->), guía de uso y límites conocidos

### Documentación de desarrollo

El directorio `Doc/` del repositorio mantiene el sistema de documentación de capacidades (definición de requisitos → seguimiento de funcionalidades → diseño de arquitectura):

- [Documento de requisitos (中文)](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): visión, objetivos principales y requisitos funcionales
- [Lista de funcionalidades (中文)](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): seguimiento del estado de implementación/pruebas/comentarios de todos los puntos funcionales
- [Diseño de arquitectura (中文)](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): capas, componentes de módulos y decisiones clave de diseño

## Funcionalidades principales


<details>
<summary><b>Extensiones básicas</b> (9)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Conversión de tipos Utility](https://newlifex.com/core/utility) | Ofrece conversión de tipos eficiente, con soporte de diversos formatos y manejo de excepciones. |
| [Extensión de cadenas StringHelper](https://newlifex.com/core/string_helper) | Incluye múltiples operaciones de cadenas, como recorte, codificación y formato, entre otras. |
| [Extensión de rutas PathHelper](https://newlifex.com/core/path_helper) | Procesa de forma inteligente múltiples formatos de rutas, logrando compatibilidad multiplataforma del código. |
| [Extensión de datos IOHelper](https://newlifex.com/core/io_helper) | Optimiza las operaciones de E/S y ofrece conversiones eficientes entre flujos de datos y matrices de bytes. |
| [Extensión de procesos ProcessHelper](https://newlifex.com/core/process_helper) | Simplifica las operaciones con procesos y ofrece métodos cómodos para obtener y gestionar información de procesos. |
| [Información de tiempo de ejecución Runtime](https://newlifex.com/core/runtime) | Proporciona métodos sencillos para obtener información en tiempo de ejecución de la aplicación, facilitando la depuración y el diagnóstico. |
| [Objeto desechable DisposeBase](https://newlifex.com/core/disposebase) | Proporciona un patrón estándar de liberación de recursos que evita eficazmente fugas de memoria y de recursos. |
| [Extensión de reflexión Reflect](https://newlifex.com/core/reflect) | Herramienta de reflexión de alto rendimiento para leer y escribir miembros de objetos con rapidez. |
| [Extensión de seguridad SecurityHelper](https://newlifex.com/core/security_helper) | Integra múltiples algoritmos de cifrado y descifrado como RSA/AES/DES/MD5, simplificando las operaciones de seguridad. |

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
| [Lector Span SpanReader](https://newlifex.com/core/span_reader) | Analizador de flujos binarios de alto rendimiento sin asignaciones, con soporte de endianness. |
| [Escritor Span SpanWriter](https://newlifex.com/core/span_writer) | Escritor de datos binarios de alto rendimiento sin asignaciones, con soporte de endianness. |
| [Auxiliar Span SpanHelper](https://newlifex.com/core/span_helper) | Conjunto de métodos de extensión para Span/ReadOnlySpan que simplifica las operaciones de memoria. |
| [Lector secuencial SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | Lector secuencial entre segmentos (en frameworks antiguos se completa mediante shims; disponible en los 11 TFM). |

</details>

<details>
<summary><b>Framework de aplicaciones</b> (16)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Contenedor de objetos ObjectContainer](https://newlifex.com/core/object_container) | Inyección de dependencias ligera, con soporte de múltiples ciclos de vida y modos de inyección. |
| [Host de aplicación ligero Host](https://newlifex.com/core/host) | Simplifica el alojamiento de aplicaciones y facilita la creación de servicios en segundo plano de larga duración. |
| [Framework de plugins IPlugin](https://newlifex.com/core/plugin) | Proporciona un mecanismo estandarizado de gestión de plugins, logrando el desarrollo modular de aplicaciones. |
| [Modelo de paralelismo Actor](https://newlifex.com/core/actor) | Modelo de programación concurrente simplificado que mejora el rendimiento mediante el procesamiento con colas de mensajes. |
| [Modelo de canalización Pipeline](https://github.com/NewLifeX/X/blob/master/Doc/管道模型Pipeline.md) ⚠️ Retirado, reemplazado por el modo de protocolo (bomba de tramas) | Modelo de cadena de procesamiento bidireccional que unifica los flujos Read/Write/Open/Close. |
| [Cola diferida DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | Agrupa cambios de alta frecuencia y los persiste por lotes, reduciendo la amplificación de escritura. |
| [Bus de eventos EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | Distribución de eventos dentro del proceso, con soporte de enrutamiento por temas y mensajes en cola. |
| [Delegado de referencia débil WeakAction](https://newlifex.com/core/weak_action) | Resuelve las fugas de memoria por suscripción de eventos, logrando un manejo seguro de eventos. |
| [Motor de scripts ScriptEngine](https://newlifex.com/core/script_engine) | Admite compilación y ejecución dinámica de C#, ofreciendo una flexible capacidad de extensión mediante scripts. |
| [Compresión y descompresión](https://newlifex.com/core/compression) | Integra múltiples algoritmos de compresión y ofrece una interfaz unificada para comprimir datos. |
| [Archivado comprimido Tar y 7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Cubre el uso avanzado de archivado y compresión con Tar/Tar.Gz y 7Zip. |
| [Información de la máquina MachineInfo](https://newlifex.com/core/machine_info) | Obtiene información completa del hardware, con soporte de sistemas Windows y Linux. |
| [Biblioteca de pinyin PinYin](https://newlifex.com/core/pinyin) | Biblioteca eficiente de conversión de caracteres chinos a pinyin, con soporte de pinyin completo y abreviado, y rendimiento superior. |
| [Conjunto de datos DbTable](https://newlifex.com/core/dbtable) | Tabla de datos en memoria que facilita el procesamiento y el análisis de datos. |
| [Parámetros de paginación PageParameter](https://newlifex.com/core/page_parameter) | Encapsula la lógica de paginación, con soporte de paginación en escenarios como Web y API. |
| [Algoritmo Snowflake Snowflake](https://newlifex.com/core/snow_flake) | Algoritmo de generación de ID único distribuido que garantiza la unicidad del ID en escenarios de alta concurrencia. |

</details>

<details>
<summary><b>Seguridad y tokens</b> (3)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Token de firma digital distribuida TokenProvider](https://newlifex.com/core/token_provider) | Se usa para generar y validar tokens distribuidos, garantizando la seguridad entre múltiples nodos. |
| [Token web universal JwtBuilder](https://newlifex.com/core/jwt) | Proporciona generación, decodificación y validación de tokens JWT, simplificando la autenticación web. |
| [Filtro HTTP de token TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | Inyecta automáticamente el token en las solicitudes HTTP e intercepta los 401/403 para renovarlo automáticamente. |

</details>

<details>
<summary><b>Servicios en la nube</b> (1)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Hash de latitud y longitud GeoHash](https://newlifex.com/core/geo_hash) | Codifica la latitud y la longitud como una cadena, para búsquedas de ubicaciones cercanas. |

</details>

<details>
<summary><b>Biblioteca de red</b> (15)</summary>

| Funcionalidad | Características principales |
| --- | --- |
| [Servidor de red NetServer](https://newlifex.com/core/netserver) | Servidor de sockets de dominio TCP/UDP/Unix de alto rendimiento, que encapsula sesiones y procesamiento de datos. |
| [Cliente de red NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | Cliente de red unificado para TCP/UDP/Unix, que simplifica la conexión y el envío/recepción. |
| [Mensaje IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | Contrato de mensajes de solicitud-respuesta, que soporta semántica de dirección y lectura/escritura de tramas. |
| [Pila de protocolo de mensajes MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | Pila única de bomba de tramas + códec sin estado, con soporte de cuerpos en streaming de tramas grandes y contrapresión. |
| [Canalización de datos Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | Canalización de datos sin copia, alineada con la forma de System.IO.Pipelines. |
| [Protocolo simple de intercambio de mensajes remotos SRMP](https://newlifex.com/core/srmp) | Protocolo RPC ligero, con soporte de serialización binaria y transporte UDP. |
| [Equilibrio de carga y conmutación por error LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Ofrece tres estrategias de programación multinodo: Failover/round-robin/carrera. |
| [Comunicación RPC ApiServer](https://newlifex.com/core/api_server) | ⚠️ Se ha movido a [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [Comunicación RPC ApiClient](https://newlifex.com/core/api_client) | ⚠️ Se ha movido a [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [Cliente HTTP ApiHttpClient](https://newlifex.com/core/api_http) | Encapsulado sobre HttpClient, simplifica la invocación y el procesamiento de API web. |
| [Servidor HTTP HttpServer (dispositivos embebidos)](https://newlifex.com/core/httpserver) | Servidor HTTP ligero, con soporte de canalización de middleware, rutas parametrizadas y servicio de archivos estáticos. |
| [Descarga de red WebClientX](https://newlifex.com/core/webclient) | Versión mejorada de WebClient, con soporte de descargas multihilo y reanudables. |
| [Servidor WebSocket (herramienta ideal para notificaciones descendentes en IoT)](https://newlifex.com/core/websocket) | Servidor WebSocket de alto rendimiento, adecuado para el envío de mensajes push en IoT. |
| [Comunicación bidireccional WebSocket](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | Presenta prácticas de handshake, procesamiento de tramas, latidos y cierre de conexiones. |
| [Cliente de almacenamiento de archivos EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | Cliente de almacenamiento de archivos HTTP, compatible con EasyIO u OSS. |

</details>

> ⚠️ Los mapas de Baidu/Amap se han extraído a paquetes NuGet independientes.


## Estructura del repositorio

| Directorio | Descripción |
| --- | --- |
| `NewLife.Core/` | Código de la biblioteca principal (registro, red, caché, serialización, …) |
| `NewLife.Core.SourceGenerator/` | Generador de código que se distribuye como `analyzers/` en el paquete |
| `Doc/` | Documentación de capacidades e índice de artículos |
| `Samples/` | Ejemplos de consola / servicio / Http / IoT / móvil |
| `Benchmark/` | Benchmarks de rendimiento con BenchmarkDotNet |
| `XUnitTest.Core/` | Pruebas unitarias principales (net10.0) |
| `XUnitTest.Compat/`, `XUnitTest.Net6/` | Pruebas de compatibilidad entre versiones |
| `AotSmoke/` | Proyecto de smoke test de NativeAOT |

## Compilar y probar

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- Pruebas completas en paralelo: `run-tests.ps1` (varios hosts en paralelo + tiempo de espera + resumen trx)
- Smoke test de NativeAOT: `AotSmoke/`, CI en `.github/workflows/aot.yml`
- Benchmarks: `Benchmark/` (BenchmarkDotNet)

## Matriz de proyectos de NewLife

Todos los proyectos son compatibles de forma predeterminada con net10.0/netstandard2.1/netstandard2.0/net462/net4.5; las versiones antiguas (2024.0801) admiten net4.0/net2.0  

| Proyecto | Año | Descripción |
| :---: | :---: | --- |
| Componentes básicos |       | Soporte para otros middleware y proyectos de productos |
| [NewLife.Core](https://github.com/NewLifeX/X) | 2002 | Biblioteca principal: registros, configuración, caché, red, serialización y seguimiento de rendimiento APM |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 2005 | Middleware de big data: tablas individuales de decenas de miles de millones de filas, MySql/SQLite/SqlServer/Oracle/PostgreSql/DaMeng, particionado automático de tablas y separación de lectura/escritura |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 2005 | Biblioteca de red: rendimiento de decenas de millones por máquina (22,66 millones de tps), millones de conexiones por máquina (4 millones de conexiones TCP persistentes) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011 | Biblioteca de comunicación por protocolo: ofrece un framework de comunicación para aplicaciones C/S, compatible con frameworks de comunicación Http/RPC, alto rendimiento y acceso sencillo de bajo coste para dispositivos IoT |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 2010 | Plataforma de desarrollo rápido Cube (cubo mágico): integra permisos de usuario, inicio de sesión SSO, servidor OAuth, etc.; validada en proyectos con tablas individuales de 10.000 millones de filas |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 2008 | Componente de gestión de servicios: instala la aplicación como demonio del sistema operativo, servicio de Windows o Systemd de Linux |
| [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero) | 2020 | Scaffolding Zero: plantillas de proyecto NewLife.Templates basadas en el ecosistema de componentes NewLife; Web, WebApi, Service |
| Middleware |       | Integración con plataformas de middleware reconocidas |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | 2017 | Cliente Redis: latencia de microsegundos, rendimiento de millones de operaciones, rica cola de mensajes; validado en proyectos con decenas de miles de millones de datos |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018 | Cliente RocketMQ totalmente gestionado, compatible con Apache RocketMQ y la cola de mensajes de Alibaba Cloud; validado en proyectos de escala de miles de millones |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | 2019 | Protocolo de mensajería para IoT: MqttClient/MqttServer; el cliente es compatible con Alibaba Cloud IoT |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 2022 | Biblioteca estándar de IoT: define diversas normas y especificaciones de protocolos de comunicación en el ámbito del IoT |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 2022 | ModbusTcp/ModbusRTU/ModbusASCII, implementado sobre la biblioteca estándar IoT, compatible con la plataforma ZeroIoT y la puerta de enlace IoTEdge |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 2022 | Protocolo Siemens PLC, implementado sobre la biblioteca estándar IoT, compatible con la plataforma IoT y IoTEdge |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 2022 | Biblioteca de componentes de mapas: encapsula Baidu Maps, Amap, Tencent Maps y Tianditu |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 2023 | Biblioteca de códec de audio: PCM/ADPCMA/G711A/G722U/WAV/AAC |
| Plataforma de productos |       | Nivel de plataforma de producto: se compila y despliega listo para usar, con personalización individualizada |
| [Stardust](https://github.com/NewLifeX/Stardust) | 2018 | Stardust: plataforma de servicios distribuidos con gestión de nodos, centro de monitoreo APM, centro de configuración, centro de registro y centro de publicación |
| [AntJob](https://github.com/NewLifeX/AntJob) | 2019 | AntJob: plataforma de computación distribuida de big data (en tiempo real/fuera de línea), con la idea de fragmentación de "mudanza de hormigas"; validada en proyectos de billones de datos |
| [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP) | 2021 | ERP empresarial: gestión de productos, gestión de clientes, gestión de ventas y gestión de proveedores |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 2006 | Herramienta CrazyCoder: numerosas utilidades para desarrolladores; red, puerto serie, cifrado y descifrado, expresiones regulares, Modbus, MQTT |
| [EasyIO](https://github.com/NewLifeX/EasyIO) | 2023 | Almacenamiento de archivos simple, con soporte de almacenamiento centralizado de archivos en sistemas distribuidos. |
| [XProxy](https://github.com/NewLifeX/XProxy) | 2005 | Proxy inverso de nivel de producto: proxy NAT y proxy Http |
| [HttpMeter](https://github.com/NewLifeX/HttpMeter) | 2022 | Herramienta de pruebas de estrés HTTP |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 2015 | Sistema de gestión de código fuente Git |
| [SmartOS](https://github.com/NewLifeX/SmartOS) | 2014 | Sistema operativo embebido, totalmente independiente y propio, compatible con la arquitectura de chips ARM Cortex-M |
| [SmartA2](https://github.com/NewLifeX/SmartA2) | 2019 | Computadora industrial embebida, puerta de enlace de borde para IoT, host .NET8 de alto rendimiento; aplicada en industria, agricultura, transporte y medicina |
| Plataforma IoT FIoT | 2020 | Solución integral de IoT: construcción, protección ambiental y agricultura; integración de software, hardware y análisis de big data; validada en proyectos con cientos de miles de puntos por máquina |


---

## Equipo de desarrollo NewLife

![XCode](https://newlifex.com/logo.png)

El equipo NewLife se fundó en 2002 y es un proveedor de soluciones para la industria del IoT de la nueva era, dedicado a ofrecer consultoría de soluciones de aplicaciones de software y hardware, y servicios de planificación de arquitectura de sistemas y desarrollo. Más de 5,23 millones de descargas acumuladas en NuGet. Los componentes principales del equipo (XCode / Redis / Stardust / AntJob / IoT, etc.) se han aplicado a gran escala en sectores como energía eléctrica, industria, transporte, protección ambiental, medicina y patrimonio cultural.

Seguimos mejorando continuamente, con el objetivo de ser un socio tecnológico de .NET / IoT / distribuido digno de confianza a largo plazo.

Sitio web: https://newlifex.com   Organización de código abierto: https://github.com/NewLifeX  
Cuenta oficial de WeChat:  
![智能大石头](https://newlifex.com/stone.jpg)

## Contribución y comentarios

- Antes de enviar, asegúrate de que `dotnet build` y las pruebas unitarias relacionadas pasen; el estilo de código y los mensajes de commit siguen las convenciones del repositorio.
- Informa errores y sugerencias mediante [Issue](https://github.com/NewLifeX/X/issues); las Stars y los PR son bienvenidos.
- Consulta [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md) para las notas de versión.

## Licencia

Este proyecto tiene licencia [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE); el certificado de nombre fuerte `newlife.snk` es público para que puedas compilar y reemplazar ensamblados por tu cuenta.

> Si este proyecto te resulta útil, te invitamos a darle una Star, compartirlo con tu equipo o enviar sugerencias de mejora mediante un Issue/PR.
