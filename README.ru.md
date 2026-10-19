# NewLife.Core - Базовая библиотека

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

**NewLife.Core** — фундамент экосистемы NewLife: один пакет для логирования, конфигурации, кэширования, сетевого взаимодействия, сериализации, планирования, контейнера объектов и APM-трассировки. Он поддерживает 11 целевых платформ — от net45 до net10.0 — и служит основой для XCode, Cube, Redis, Stardust и более 30 родственных проектов.

Учебник по базовой библиотеке: [https://newlifex.com/core](https://newlifex.com/core)  

**Содержание**: [Быстрый старт](#быстрый-старт) · [Почему NewLife.Core](#почему-newlifecore) · [Основные возможности](#основные-возможности) · [Экосистема](#экосистема) · [Документация](#документация) · [Участие](#участие) · [Команда](#команда) · [Лицензия](#лицензия)

## Быстрый старт

### Установка

```bash
dotnet add package NewLife.Core
```

### Минимальный пример

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // console log (also writes to file)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // in-memory cache
cache.Set("key", "value", 60);          // expires in 60 seconds
var value = cache.Get<String>("key");   // read back
```

### Шаблон проекта

Выполните следующие команды в командной строке, чтобы быстро познакомиться с компонентами NewLife:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### Поддержка платформ

| Пункт | Охват |
| --- | --- |
| Целевые платформы | 11 в одном пакете: net45, net461, net462, netstandard2.0, netstandard2.1, net6.0, net6.0-windows, net8.0, net8.0-windows, net10.0, net10.0-windows |
| ОС и архитектуры | Windows, Linux, macOS, Android, iOS; x64, x86, arm64, arm, loongarch64, riscv64, s390x |
| NativeAOT | <!--aot-scenarios:start-->31<!--aot-scenarios:end--> проверенных ключевых сценариев; мульти-RID матрица CI-смоук-тестов (win-x64, linux-x64, linux-arm64 и linux-loongarch64 — блокирующие шлюзы). Плагины и скрипты по замыслу несовместимы с AOT — см. [Анализ AOT-совместимости](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) |

## Почему NewLife.Core

- **Один пакет для базовой инфраструктуры**: логирование, конфигурация, кэш, сеть, сериализация, планировщик, контейнер объектов и DI — достаточно одной ссылки.
- **Широкий охват версий**: 11 целевых платформ в одном пакете (net45 → net10.0, включая варианты Windows); один и тот же API для старых систем и новых проектов.
- **Проверено в масштабе экосистемы**: на этой основе работают XCode (отдельные таблицы на 10 млрд строк), Net (22,66M tps), Redis и Stardust (десятки миллиардов записей).
- **Сдержанные зависимости, свободная лицензия**: только BCL и официальные пакеты совместимости Microsoft; MIT — можно изменять код и переименовывать (см. [Лицензия](#лицензия)).

## Основные возможности

<details>
<summary><b>Базовые расширения</b> (10)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Преобразование типов Utility](https://newlifex.com/core/utility) | Обеспечивает эффективное преобразование типов с поддержкой различных форматов и обработкой исключений. |
| [Строковые расширения StringHelper](https://newlifex.com/core/string_helper) | Включает множество строковых операций: обрезку, кодирование, форматирование и другое. |
| [Расширения путей PathHelper](https://newlifex.com/core/path_helper) | Интеллектуально обрабатывает различные форматы путей, обеспечивая кроссплатформенную совместимость кода. |
| [Расширения данных IOHelper](https://newlifex.com/core/io_helper) | Оптимизирует операции ввода-вывода и обеспечивает эффективное преобразование между потоками данных и массивами байтов. |
| [Расширения процессов ProcessHelper](https://newlifex.com/core/process_helper) | Упрощает работу с процессами, предоставляя удобные методы получения и управления информацией о процессах. |
| [Информация о среде выполнения Runtime](https://newlifex.com/core/runtime) | Предоставляет простые методы получения сведений о среде выполнения приложения для отладки и диагностики. |
| [Освобождаемый ресурс DisposeBase](https://newlifex.com/core/disposebase) | Предоставляет стандартный шаблон освобождения ресурсов, эффективно предотвращая утечки памяти и ресурсов. |
| [Расширения рефлексии Reflect](https://newlifex.com/core/reflect) | Высокопроизводительный инструмент рефлексии для быстрого чтения и записи членов объектов. |
| [Расширения безопасности SecurityHelper](https://newlifex.com/core/security_helper) | Интегрирует RSA/AES/DES/MD5 и другие алгоритмы шифрования, упрощая безопасные операции. |
| [Геохэш GeoHash](https://newlifex.com/core/geo_hash) | Кодирует широту и долготу в строку для поиска ближайших местоположений. |

</details>

<details>
<summary><b>Логирование и трассировка</b> (2)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Логирование ILog](https://newlifex.com/core/log) | Поддерживает множество способов вывода журналов и предоставляет мощную асинхронную запись. |
| [Трассировка ITracer](https://newlifex.com/core/tracer) | Мощная APM-трассировка производительности с поддержкой интеграции с платформой Stardust. |

</details>

<details>
<summary><b>Планирование по расписанию</b> (3)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Продвинутый таймер TimerX](https://newlifex.com/core/timerx) | Высокоточный таймер с поддержкой Cron-планирования и асинхронного периодического выполнения. |
| [Cron-выражения](https://newlifex.com/core/cron) | Предоставляет задачи по расписанию, аналогичные Linux Cron, с поддержкой совпадений с точностью до секунды. |
| [Поставщик времени TimeProvider](https://newlifex.com/core/timerx_timeprovider) | Единая абстракция времени и высокоточный расчёт длительности для кросс-версионной совместимости. |

</details>

<details>
<summary><b>Система конфигурации</b> (4)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Система конфигурации Config](https://newlifex.com/core/config) | Единый фреймворк конфигурации с поддержкой множества форматов, источников и горячей перезагрузки. |
| [Поставщик конфигурации IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | Единый интерфейс конфигурации с поддержкой обратных вызовов и модели узлов конфигурации. |
| [Базовая конфигурация Setting](https://newlifex.com/core/setting) | Управляет ключевой конфигурацией компонентов, такой как логирование и сеть. |
| [Системная конфигурация SysConfig](https://newlifex.com/core/sysconfig) | Управляет конфигурацией прикладной системы с поддержкой локальных, распределённых и зашифрованных настроек. |

</details>

<details>
<summary><b>Сериализация</b> (7)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Сериализация Json](https://newlifex.com/core/json) | Высокопроизводительный инструмент Json с поддержкой сериализации в различных сложных сценариях. |
| [Сериализация Xml](https://newlifex.com/core/xml) | Обеспечивает сериализацию и десериализацию XML, а также взаимное преобразование объектов и XML. |
| [Бинарная сериализация](https://newlifex.com/core/binary) | Высокопроизводительная бинарная сериализация с поддержкой сложных графов объектов и управления версиями. |
| [Продвинутая бинарная сериализация](https://newlifex.com/core/advanced_binary) | Расширенное руководство по кодированию и декодированию протокольных сообщений без выделений памяти на всём пути. |
| [Парсинг CSV CsvFile](https://newlifex.com/core/csv_file) | Быстрое чтение и запись CSV-файлов с поддержкой пользовательских разделителей и привязки данных. |
| [База данных CSV CsvDb](https://newlifex.com/core/csv_db) | Позволяет использовать CSV-файлы как базу данных с поддержкой базовых операций CRUD. |
| [Лёгкий читатель Excel ExcelReader](https://newlifex.com/core/excel_reader) | Быстрое чтение Excel без зависимости от Office с поддержкой больших объёмов данных. |

</details>

<details>
<summary><b>Кэширование</b> (7)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Единый интерфейс кэша ICache](https://newlifex.com/core/icache) | Определяет стандартные операции кэширования и предоставляет реализации для памяти, Redis и других хранилищ. |
| [Архитектура кэширования ICacheProvider](https://newlifex.com/core/icacheprovider) | Управляет экземплярами ICache и предоставляет их, поддерживая одновременную работу нескольких кэшей. |
| [Кэш в памяти MemoryCache](https://newlifex.com/core/memory_cache) | Высокопроизводительный локальный кэш в памяти с политиками устаревания и ограничения ёмкости. |
| [Пул объектов ObjectPool](https://newlifex.com/core/object_pool) | Повторно использует дорогостоящие объекты, повышая производительность и снижая нагрузку на GC. |
| [Пул объектов Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | Высокопроизводительный универсальный пул объектов на основе CAS, снижающий нагрузку на GC. |
| [Распределённый кэш NewLife.Redis](https://newlifex.com/core/redis) | Реализация распределённого кэша на основе Redis с поддержкой множества структур данных. |
| [Векторное хранилище IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | Интерфейс управления коллекциями векторов с поддержкой поиска Top-K по сходству — инфраструктура векторного поиска для AI-приложений. |

</details>

<details>
<summary><b>Высокопроизводительные буферы</b> (7)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Пакет данных IPacket](https://newlifex.com/core/packet) | Ключевой интерфейс пакета данных с zero-copy срезами и цепочечными пакетами. |
| [Буферы Buffers](https://newlifex.com/core/buffers) | Библиотека высокопроизводительных буферных операций, объединяющая компоненты Span и пуловой записи. |
| [Пуловой писатель PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | Буферный писатель на основе ArrayPool с поддержкой динамического расширения. |
| [Читатель Span SpanReader](https://newlifex.com/core/span_reader) | Высокопроизводительный парсер бинарных потоков без выделений памяти с поддержкой порядка байтов. |
| [Писатель Span SpanWriter](https://newlifex.com/core/span_writer) | Высокопроизводительный писатель бинарных данных без выделений памяти с поддержкой порядка байтов. |
| [Помощник Span SpanHelper](https://newlifex.com/core/span_helper) | Набор методов расширения Span/ReadOnlySpan, упрощающий операции с памятью. |
| [Последовательный читатель SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | Читатель последовательности через сегменты (в старых фреймворках добавлен через шимы, доступен во всех 11 TFM). |

</details>

<details>
<summary><b>Прикладной фреймворк</b> (15)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Контейнер объектов ObjectContainer](https://newlifex.com/core/object_container) | Лёгкое внедрение зависимостей с поддержкой множества жизненных циклов и режимов внедрения. |
| [Лёгкий хост приложения Host](https://newlifex.com/core/host) | Упрощает хостинг приложений, позволяя легко создавать длительно работающие фоновые службы. |
| [Фреймворк плагинов IPlugin](https://newlifex.com/core/plugin) | Предоставляет стандартизированный механизм управления плагинами для модульной разработки приложений. |
| [Модель параллелизма Actor](https://newlifex.com/core/actor) | Упрощённая модель конкурентного программирования, повышающая пропускную способность за счёт обработки через очереди сообщений. |
| [Отложенная очередь DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | Агрегирует частые изменения и сохраняет их пакетами, снижая усиление записи. |
| [Шина событий EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | Внутрипроцессное распределение событий с поддержкой маршрутизации по темам и очередями сообщений. |
| [Делегат слабых ссылок WeakAction](https://newlifex.com/core/weak_action) | Решает проблему утечек памяти при подписке на события, обеспечивая безопасную обработку событий. |
| [Движок скриптов ScriptEngine](https://newlifex.com/core/script_engine) | Поддерживает динамическую компиляцию и выполнение C# для гибкого расширения возможностей скриптами. |
| [Сжатие и распаковка](https://newlifex.com/core/compression) | Интегрирует множество алгоритмов сжатия с единым интерфейсом для сжатия данных. |
| [Архивы Tar и 7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Продвинутые сценарии архивирования и сжатия Tar/Tar.Gz и 7Zip. |
| [Информация о машине MachineInfo](https://newlifex.com/core/machine_info) | Полное получение информации об оборудовании с поддержкой Windows и Linux. |
| [Библиотека пиньиня PinYin](https://newlifex.com/core/pinyin) | Эффективная библиотека преобразования китайских иероглифов в пиньинь с поддержкой полного и сокращённого написания и превосходной производительностью. |
| [Набор данных DbTable](https://newlifex.com/core/dbtable) | Таблица данных в памяти, удобная для обработки и анализа данных. |
| [Параметры страницы PageParameter](https://newlifex.com/core/page_parameter) | Инкапсулирует логику постраничного вывода для Web, API и других сценариев. |
| [Алгоритм Snowflake](https://newlifex.com/core/snow_flake) | Алгоритм генерации распределённых уникальных ID, гарантирующий уникальность при высокой нагрузке. |

</details>

<details>
<summary><b>Безопасность и токены</b> (3)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Распределённые токены с цифровой подписью TokenProvider](https://newlifex.com/core/token_provider) | Генерирует и проверяет распределённые токены, обеспечивая безопасность между узлами. |
| [Универсальный веб-токен JwtBuilder](https://newlifex.com/core/jwt) | Предоставляет генерацию, декодирование и проверку JWT-токенов, упрощая веб-аутентификацию. |
| [HTTP-фильтр токенов TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | Автоматически внедряет токен в HTTP-запросы и продлевает его при перехвате 401/403. |

</details>

<details>
<summary><b>Сетевая библиотека</b> (13)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Сетевой сервер NetServer](https://newlifex.com/core/netserver) | Высокопроизводительный сервер TCP/UDP/Unix-сокетов с инкапсуляцией сессий и обработки данных. |
| [Сетевой клиент NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | Единый сетевой клиент TCP/UDP/Unix, упрощающий подключение и обмен данными. |
| [Сообщение IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | Контракт сообщений «запрос — ответ», несущий семантику направления и чтение-запись кадров. |
| [Стек протокола сообщений MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | Единый стек «фреймовый насос + кодек без состояния» с поддержкой больших потоковых кадров и обратного давления. |
| [Конвейер данных Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | Конвейер данных с нулевым копированием, повторяющий форму System.IO.Pipelines. |
| [Простой протокол обмена удалёнными сообщениями SRMP](https://newlifex.com/core/srmp) | Лёгкий RPC-протокол с поддержкой бинарной сериализации и передачи по UDP. |
| [Балансировка нагрузки и отказоустойчивость LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Предоставляет три стратегии планирования между узлами: Failover, поочерёдную и состязательную. |
| [HTTP-клиент ApiHttpClient](https://newlifex.com/core/api_http) | Обёртка над HttpClient, упрощающая вызов и обработку Web API. |
| [HTTP-сервер HttpServer (встраиваемые устройства)](https://newlifex.com/core/httpserver) | Лёгкий HTTP-сервер с поддержкой конвейера промежуточного ПО, параметризованной маршрутизации и раздачи статических файлов. |
| [Сетевая загрузка WebClientX](https://newlifex.com/core/webclient) | Улучшенная версия WebClient с поддержкой многопоточной загрузки и докачки с точки останова. |
| [WebSocket-сервер (мощный инструмент нисходящих уведомлений IoT)](https://newlifex.com/core/websocket) | Высокопроизводительный WebSocket-сервер для push-уведомлений интернета вещей. |
| [Двунаправленная связь WebSocket](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | Описывает практику рукопожатия, обработки кадров, heartbeat и закрытия соединений. |
| [Клиент файлового хранилища EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | HTTP-клиент файлового хранилища для EasyIO или OSS. |

</details>

> RPC (`ApiServer`/`ApiClient`) перенесён в [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting).

## Экосистема

NewLife — это многоуровневая экосистема независимо версионируемых open-source проектов. Карта слоёв ниже дословно используется во всех репозиториях NewLife; подробная таблица дополняет её позиционированием и популярностью каждого проекта.

| Слой | Проекты |
| --- | --- |
| Базовый | Core · XCode · Net · Remoting · Agent · Templates |
| Данные и обмен сообщениями | Redis · RocketMQ · MQTT · NovaDb · MySql |
| Web, AI и распределённые системы | Cube · Stardust · AntJob · NewLife.AI |
| Промышленность и IoT | IoT · Modbus · Siemens · Omron · Melsec · OPC · BACnet · ZeroIoT |
| Телематика транспорта | JT808 · JT809 · JT1078 |
| Прикладные компоненты | Office · Audio · Video · Map · IP |
| Инструменты и продукты | CrazyCoder · GitCandy · LuckyClover |

### Детали проектов

| Проект | Что делает | Популярность |
| --- | --- | --- |
| **Базовый** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Логирование, конфигурация, кэширование, сеть, сериализация, планирование и APM-трассировка. | ★1.9k · 1.91M |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Промежуточное ПО данных: отдельные таблицы масштаба десятков миллиардов строк, автоматическое секционирование, разделение чтения и записи, более 10 баз данных. | ★104 · 541k |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Высокопроизводительная сетевая библиотека: 22,66M tps на машину, 4M долгоживущих TCP-соединений. | ★313 · 74.5k |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Протокольная связь и RPC-фреймворк для клиент-серверных приложений и IoT-устройств. | ★16 · 272k |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Устанавливает .NET-приложения как демоны ОС: службы Windows и systemd в Linux. | ★45 · 108k |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | Шаблоны проектов `dotnet new` для консоли, web, API, службы, данных и IoT. | ★82 · 14.9k |
| **Данные и обмен сообщениями** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Клиент Redis с микросекундной задержкой, миллионами операций в секунду и встроенными очередями сообщений. | ★878 · 723k |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Полностью управляемый клиент RocketMQ для Apache RocketMQ и Alibaba Cloud Message Queue. | ★225 · 120k |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | Клиент и сервер MQTT, включая подключение к Alibaba Cloud IoT. | ★150 · 99.4k |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Гибридная база данных на C#: встроенная или сетевая, реляционная, временных рядов, очередь сообщений и KV в одном. | ★12 · 3.4k |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | Драйвер MySql и вспомогательный слой на основе стека данных NewLife. | ★23 · 8.4k |
| **Web, AI и распределённые системы** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Фреймворк быстрой разработки админ-платформ: права, SSO, сервер OAuth и CRUD-скаффолдинг. | ★637 · 186k |
| [Stardust](https://github.com/NewLifeX/Stardust) | Распределённая сервисная платформа: управление узлами, APM, центр конфигурации, реестр и центр публикации. | ★354 · 274k |
| [AntJob](https://github.com/NewLifeX/AntJob) | Распределённая платформа задач и планирования данных в реальном времени с секционированием, повторами и дозаполнением. | ★429 · 56.8k |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Единый клиент для 46 поставщиков LLM плюс ChatAI — готовое веб-приложение для чата. | ★20 · 11.4k |
| **Промышленность и IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | Стандартная библиотека IoT, определяющая контракты протоколов и абстракции устройств. | ★30 · 104k |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Реализация Modbus TCP/RTU/ASCII поверх NewLife.IoT. | ★27 · 48.7k |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Протокол Siemens PLC поверх NewLife.IoT. | ★30 · 14.3k |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Протокол Omron PLC поверх NewLife.IoT. | ★8 · 11.1k |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Протокол Mitsubishi Melsec PLC поверх NewLife.IoT. | ★34 · 10.8k |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | Клиент OPC для доступа к промышленным данным. | ★9 · — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | Стек протокола автоматизации зданий BACnet. | ★8 · 9k |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Лёгкая open-source платформа IoT с управлением шлюзами и устройствами. | ★10 · — |
| **Телематика транспорта** | | |
| NewLife.JT808 | Протокол позиционирования транспорта JT/T 808: более 70 типов сообщений, 2011/2013/2019. | — |
| NewLife.JT809 | Межплатформенный протокол JT/T 809 для органов управления транспортом. | — · 135 |
| NewLife.JT1078 | Автомобильное видео JT/T 1078: разбор RTP, ремультиплексирование в FLV/fMP4 и прямая трансляция. | — |
| **Прикладные компоненты** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Один пакет для Excel, Word, PPT, PDF и более 15 форматов документов. | ★11 · 12.9k |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Кроссплатформенное аудио: кодеки, ввод-вывод устройств, DSP, контейнеры и потоковая передача. | ★5 · 6.6k |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Видеокодеки, форматы контейнеров, обработка кадров и потоковая передача. | ★2 · 610 |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Единый интерфейс к Baidu, AMap, Tencent и Tianditu с геокодированием и преобразованием координат. | ★15 · 24.8k |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | Геолокация IPv4 с офлайн-базой, отображаемой в память, и онлайн-API как резервом. | ★16 · 84.7k |
| **Инструменты и продукты** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Инструменты разработчика: сеть, последовательный порт, шифрование, регулярные выражения, Modbus и MQTT. | ★330 · 42.1k |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Самостоятельно размещаемая система управления исходным кодом Git. | ★49 · — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Автоматически устанавливает подходящую среду выполнения .NET при запуске на старом хосте. | ★13 · — |

> Более 30 пакетов, не перечисленных здесь (FTP, LoRa, расширения Office, драйверы протоколов и другие), доступны в [организации NewLifeX](https://github.com/NewLifeX) и на [newlifex.com](https://newlifex.com).

## Документация

### Руководства

- [Каталог базовой библиотеки](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): индекс статей о ключевых компонентах, сгруппированный по модулям
- [Анализ AOT-совместимости](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): область поддержки NativeAOT, измеренные результаты (смоук <!--aot-smoke:start-->31/31<!--aot-smoke:end-->), матрица RID и известные ограничения
- [Локализация](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md): модель сообщений, таблицы языков, переопределения в downstream-проектах и добавление языков во время выполнения
- [Чек-лист локализации для downstream-проектов](https://github.com/NewLifeX/X/blob/master/Doc/下游多语言接入清单.md): шаги интеграции, правила языков, шлюзы CI и шаблоны бейджей
- [Мобильная архитектура](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md): мобильный стек .NET, выбор UI-фреймворка, упаковка APK и заметки об AOT
- [Примеры](https://github.com/NewLifeX/X/tree/master/Samples): примеры для консоли, службы, Http, IoT и мобильных платформ

### Заметки о выпуске

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md): история версий

## Участие

### Сборка и тестирование

Решение открывается в Visual Studio (последняя официальная версия) и также собирается через .NET CLI; файлы проектов остаются намеренно простыми, чтобы свежий клон собирался сразу «из коробки».

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- Полный параллельный прогон: `run-tests.ps1` (несколько хостов параллельно, защита по таймауту и сводка trx)
- Смоук-тест NativeAOT: `AotSmoke/`, CI в `.github/workflows/aot.yml`
- Бенчмарки: `Benchmark/` (BenchmarkDotNet)

### Структура репозитория

| Каталог | Описание |
| --- | --- |
| `NewLife.Core/` | Исходный код основной библиотеки (логирование, сеть, кэш, сериализация и т. д.) |
| `NewLife.Core.SourceGenerator/` | Генератор исходного кода, поставляемый в пакете как `analyzers/` |
| `Doc/` | Документация по возможностям и индекс статей |
| `Samples/` | Примеры для консоли / службы / Http / IoT / мобильных платформ |
| `Benchmark/` | Бенчмарки производительности BenchmarkDotNet |
| `XUnitTest.Core/` | Основные модульные тесты (net10.0) |
| `XUnitTest.Compat/`, `XUnitTest.Net6/` | Тесты совместимости между версиями |
| `AotSmoke/` | Проект смоук-теста NativeAOT |

### Выпуск

Отправьте тег с префиксом `v` (например, `v12.0.2026.0927`), чтобы запустить `.github/workflows/publish.yml`. Рабочий процесс упаковывает пакет, публикует его в NuGet.org и GitHub Packages и создаёт GitHub Release, примечания к которому берутся из соответствующего раздела `ChangeLog.md` (в артефакты входят nupkg и snupkg). Ручной запуск только публикует пакеты и не создаёт Release.

### Документация по разработке

В каталоге `Doc/` поддерживается набор документации по возможностям, зеркально отражающий код: требования → отслеживание функций → архитектура. Эти документы написаны на китайском языке.

- [Документ требований](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): видение, ключевые цели и функциональные требования
- [Список функций](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): статус реализации, тестирования и документирования каждой функциональной точки
- [Проектирование архитектуры](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): слои, модульные компоненты и ключевые проектные решения

### Обратная связь

- Перед отправкой убедитесь, что `dotnet build` и соответствующие модульные тесты проходят; соблюдайте конвенции репозитория в стиле кода и сообщениях коммитов.
- Ошибки и предложения — через [Issue](https://github.com/NewLifeX/X/issues); Stars и PR приветствуются.

## Команда

NewLife основана в 2002 году и занимается .NET, IoT и распределёнными системами. Её open-source компоненты скачали <!--nuget-total:start-->6.04M<!--nuget-total:end--> раз с NuGet (по состоянию на 2026-10); они применяются в проектах энергетики, промышленности, транспорта, охраны окружающей среды, медицины и культурного наследия.

Сайт: https://newlifex.com · Open source организация: https://github.com/NewLifeX  
Официальный аккаунт WeChat:  
![智能大石头](https://newlifex.com/stone.jpg)

## Лицензия

Проект распространяется по лицензии [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE); сертификат строгого имени `newlife.snk` опубликован, чтобы вы могли самостоятельно собрать и заменить сборки, а исходный код можно изменять и распространять без указания авторства.

> Если этот проект оказался вам полезен, поставьте Star, поделитесь им с командой или отправьте предложения по улучшению через Issue/PR.

