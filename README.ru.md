# NewLife.Core - Базовая библиотека

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
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

Фреймворк NewLife — это всеобъемлющая экосистема компонентов .NET, предоставляющая высокопроизводительную инфраструктуру для создания масштабируемых приложений. Он обеспечивает базовые функции, такие как логирование, сеть, сериализация, кэширование и многопоточность, и служит основой для целого ряда приложений — от веб-сервисов до IoT-устройств.

Учебник по базовой библиотеке: [https://newlifex.com/core](https://newlifex.com/core)

> ⚠️ Этот перевод может отставать от оригинала; актуальная версия — [китайский README](Readme.MD).

**Содержание**: [Обзор фреймворка](#обзор-фреймворка) · [Почему NewLife.Core](#почему-newlifecore) · [Быстрый старт](#быстрый-старт) · [Основные возможности](#основные-возможности) · [Индекс документации](#индекс-документации) · [Структура репозитория](#структура-репозитория) · [Сборка и тестирование](#сборка-и-тестирование) · [Матрица проектов NewLife](#матрица-проектов-newlife) · [Участие и обратная связь](#участие-и-обратная-связь) · [Лицензия](#лицензия)

## Обзор фреймворка

NewLife состоит из множества библиотек компонентов, основу которых предоставляет NewLife.Core. Фреймворк поддерживает мультицелевые платформы net45~net10.0 (.NET Framework 4.5 – .NET 10.0), обеспечивая широкую совместимость в различных средах.

**NativeAOT**: ключевые цепочки уже поддерживаются (29 проверенных сценариев + CI-защита от регрессий); динамические возможности, такие как плагины и скрипты, по замыслу не поддерживаются, границы описаны в [Анализ AOT-совместимости (中文)](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md).


## Почему NewLife.Core

- **Одна библиотека для базовой инфраструктуры**: логирование, конфигурация, кэш, сеть, сериализация, планировщик, контейнер объектов и DI — достаточно одной ссылки.
- **Широкий охват версий**: 11 целевых платформ в одном пакете (net45 → net10.0, включая варианты Windows); один и тот же API для старых систем и новых проектов.
- **Поддержка NativeAOT**: 29 сценариев ядра проверены, контроль — матрица CI-смоук-тестов по нескольким RID; границы в [анализе совместимости с AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md).
- **Сдержанные зависимости, свободная лицензия**: только BCL и официальные пакеты совместимости Microsoft; MIT — можно изменять код и переименовывать (см. [Лицензия](#лицензия)).

## Быстрый старт

### Установка

```bash
dotnet add package NewLife.Core
```

### Минимальный пример

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // лог в консоль (также пишется в файл)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // кэш в памяти
cache.Set("key", "value", 60);          // истекает через 60 секунд
var value = cache.Get<String>("key");   // чтение
```

### Сборка и настройка

Компоненты NewLife создаются сообществом уже более 20 лет и распространяются по открытой лицензии MIT: **любой может свободно изменять и повторно распространять их** (без указания источника)! Многие компании, строя на этой основе внутренние фреймворки разработки, могут даже выполнить приватную кастомизацию простой массовой заменой всех строк `NewLife` в исходном коде на название своей компании.

Команда всегда придерживается открытого подхода: поддерживается как компиляция через открытие решения в Visual Studio (последняя официальная версия), так и сборка из командной строки с помощью `dotnet build`; файлы проектов намеренно лишены сложных возможностей ради простоты и удобства — всё действительно работает «из коробки».

Мы публикуем сертификат строгого именования `newlife.snk`, чтобы вы могли самостоятельно компилировать и заменять сборки.

### Шаблон проекта

Команды ниже позволяют быстро познакомиться с компонентами NewLife из командной строки:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

## Индекс документации

### Документация по использованию

- [Каталог базовой библиотеки (中文)](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): индекс статей о ключевых компонентах
- [Анализ AOT-совместимости (中文)](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): область поддержки NativeAOT, результаты практических проверок (смоук-тесты 29/29), руководство по использованию и известные границы

### Документация по разработке

В каталоге `Doc/` репозитория поддерживается система документации по возможностям (определение требований → отслеживание функций → проектирование архитектуры):

- [Документ требований (中文)](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): видение, ключевые цели и функциональные требования
- [Список функций (中文)](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): отслеживание статуса реализации, тестирования и документирования всех функциональных точек
- [Проектирование архитектуры (中文)](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): слои, модульные компоненты и ключевые проектные решения

## Основные возможности


<details>
<summary><b>Базовые расширения</b> (9)</summary>

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
| [База данных CSV CsvDb](https://newlifex.com/core/csv_db) | Позволяет использовать CSV-файлы как базу данных с базовыми операциями добавления, удаления, изменения и поиска. |
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
<summary><b>Прикладной фреймворк</b> (16)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Контейнер объектов ObjectContainer](https://newlifex.com/core/object_container) | Лёгкое внедрение зависимостей с поддержкой множества жизненных циклов и режимов внедрения. |
| [Лёгкий хост приложения Host](https://newlifex.com/core/host) | Упрощает хостинг приложений, позволяя легко создавать длительно работающие фоновые службы. |
| [Фреймворк плагинов IPlugin](https://newlifex.com/core/plugin) | Предоставляет стандартизированный механизм управления плагинами для модульной разработки приложений. |
| [Модель параллелизма Actor](https://newlifex.com/core/actor) | Упрощённая модель конкурентного программирования, повышающая пропускную способность за счёт обработки через очереди сообщений. |
| [Модель конвейера Pipeline](https://github.com/NewLifeX/X/blob/master/Doc/管道模型Pipeline.md) ⚠️ выведена из эксплуатации, заменена протокольной моделью (фреймовый насос) | Двунаправленная модель цепочки обработки, унифицирующая процессы Read/Write/Open/Close. |
| [Отложенная очередь DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | Агрегирует частые изменения и сохраняет их пакетами, снижая усиление записи. |
| [Шина событий EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | Внутрипроцессное распределение событий с поддержкой маршрутизации по темам и очередями сообщений. |
| [Делегат слабых ссылок WeakAction](https://newlifex.com/core/weak_action) | Решает проблему утечек памяти при подписке на события, обеспечивая безопасную обработку событий. |
| [Движок скриптов ScriptEngine](https://newlifex.com/core/script_engine) | Поддерживает динамическую компиляцию и выполнение C# для гибкого расширения возможностей скриптами. |
| [Сжатие и распаковка](https://newlifex.com/core/compression) | Интегрирует множество алгоритмов сжатия с единым интерфейсом для сжатия данных. |
| [Архивы Tar и 7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Продвинутые сценарии архивирования и сжатия Tar/Tar.Gz и 7Zip. |
| [Информация о машине MachineInfo](https://newlifex.com/core/machine_info) | Полное получение информации об оборудовании с поддержкой Windows и Linux. |
| [Библиотека пиньиня PinYin](https://newlifex.com/core/pinyin) | Эффективная библиотека преобразования китайских иероглифов в пиньинь с поддержкой полного и сокращённого написания, высокая производительность. |
| [Набор данных DbTable](https://newlifex.com/core/dbtable) | Таблица данных в памяти, удобная для обработки и анализа данных. |
| [Параметры страницы PageParameter](https://newlifex.com/core/page_parameter) | Инкапсулирует логику постраничного вывода для Web, API и других сценариев. |
| [Алгоритм Snowflake](https://newlifex.com/core/snow_flake) | Алгоритм генерации распределённых уникальных ID, гарантирующий уникальность при высокой нагрузке. |

</details>

<details>
<summary><b>Безопасность и токены</b> (3)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Распределённые токены с цифровой подписью TokenProvider](https://newlifex.com/core/token_provider) | Служит для генерации и проверки распределённых токенов, обеспечивая безопасность между узлами. |
| [Универсальный веб-токен JwtBuilder](https://newlifex.com/core/jwt) | Предоставляет генерацию, декодирование и проверку JWT-токенов, упрощая веб-аутентификацию. |
| [HTTP-фильтр токенов TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | Автоматически внедряет токен в HTTP-запросы и продлевает его при перехвате 401/403. |

</details>

<details>
<summary><b>Облачные сервисы</b> (1)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Геохэш GeoHash](https://newlifex.com/core/geo_hash) | Кодирует координаты в строку для поиска ближайших местоположений. |

</details>

<details>
<summary><b>Сетевая библиотека</b> (15)</summary>

| Возможность | Основные характеристики |
| --- | --- |
| [Сетевой сервер NetServer](https://newlifex.com/core/netserver) | Высокопроизводительный сервер TCP/UDP/Unix-сокетов с инкапсуляцией сессий и обработки данных. |
| [Сетевой клиент NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | Единый сетевой клиент TCP/UDP/Unix, упрощающий подключение и обмен данными. |
| [Сообщение IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | Контракт сообщений «запрос — ответ», несущий семантику направления и чтение-запись кадров. |
| [Стек протокола сообщений MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | Единый стек «фреймовый насос + кодек без состояния» с поддержкой больших потоковых кадров и обратного давления. |
| [Конвейер данных Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | Конвейер данных с нулевым копированием, повторяющий форму System.IO.Pipelines. |
| [Простой протокол обмена удалёнными сообщениями SRMP](https://newlifex.com/core/srmp) | Лёгкий RPC-протокол с поддержкой бинарной сериализации и передачи по UDP. |
| [Балансировка нагрузки и отказоустойчивость LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Предоставляет три стратегии планирования между узлами: Failover, поочерёдную и состязательную. |
| [RPC-связь ApiServer](https://newlifex.com/core/api_server) | ⚠️ перенесён в [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [RPC-связь ApiClient](https://newlifex.com/core/api_client) | ⚠️ перенесён в [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [HTTP-клиент ApiHttpClient](https://newlifex.com/core/api_http) | Обёртка над HttpClient, упрощающая вызов и обработку Web API. |
| [HTTP-сервер HttpServer (встраиваемые устройства)](https://newlifex.com/core/httpserver) | Лёгкий HTTP-сервер с поддержкой конвейера промежуточного ПО, параметризованной маршрутизации и раздачи статических файлов. |
| [Сетевая загрузка WebClientX](https://newlifex.com/core/webclient) | Улучшенная версия WebClient с поддержкой многопоточной загрузки и докачки с точки останова. |
| [WebSocket-сервер (незаменимый инструмент нисходящих уведомлений IoT)](https://newlifex.com/core/websocket) | Высокопроизводительный WebSocket-сервер для push-уведомлений интернета вещей. |
| [Двунаправленная связь WebSocket](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | Описывает практику рукопожатия, обработки кадров, heartbeat и закрытия соединений. |
| [Клиент файлового хранилища EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | HTTP-клиент файлового хранилища для работы с EasyIO или OSS. |

</details>

> ⚠️ Baidu Maps / Amap перенесены в отдельный пакет NuGet.


## Структура репозитория

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

## Сборка и тестирование

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- Полный параллельный прогон: `run-tests.ps1` (несколько хостов параллельно + таймаут + сводка trx)
- Смоук-тест NativeAOT: `AotSmoke/`, CI в `.github/workflows/aot.yml`
- Бенчмарки: `Benchmark/` (BenchmarkDotNet)

## Матрица проектов NewLife

Каждый проект по умолчанию поддерживает net10.0/netstandard2.1/netstandard2.0/net462/net4.5; старые версии (2024.0801) поддерживают net4.0/net2.0

| Проект | Год | Описание |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
| Базовые компоненты | | Поддерживают другие промежуточные слои и продуктовые проекты |
| [NewLife.Core](https://github.com/NewLifeX/X) | 2002 | Базовая библиотека: логирование, конфигурация, кэширование, сеть, сериализация, APM-трассировка производительности |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 2005 | Промежуточное ПО для больших данных: десятки миллиардов записей в одной таблице, MySql/SQLite/SqlServer/Oracle/PostgreSql/DaMeng, автоматическое секционирование таблиц, разделение чтения и записи |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 2005 | Сетевая библиотека: десятки миллионов операций в секунду на одном сервере (22,66 млн tps), миллионы соединений на одном сервере (4 млн длительных TCP-соединений) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011 | Библиотека протокольной связи: фреймворк связи для клиент-серверных приложений, поддержка Http/RPC, высокая пропускная способность, низкие накладные расходы и простое подключение IoT-устройств |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 2010 | Платформа быстрой разработки Cube: права пользователей, SSO-вход, сервер OAuth и другое; проверена проектом с 10 млрд записей в одной таблице |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 2008 | Компонент управления службами: устанавливает приложение как демон операционной системы — служба Windows или systemd в Linux |
| [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero) | 2020 | Каркас Zero: шаблоны проектов NewLife.Templates на основе экосистемы компонентов NewLife — Web, WebApi, Service |
| Промежуточное ПО | | Интеграция с известными платформами промежуточного ПО |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | 2017 | Клиент Redis: микросекундные задержки, миллионная пропускная способность, богатые очереди сообщений; проверен проектом с десятками миллиардов записей |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018 | Полностью управляемый клиент RocketMQ с поддержкой Apache RocketMQ и очередей сообщений Alibaba Cloud; проверен проектами миллиардного масштаба |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | 2019 | Протокол сообщений интернета вещей: MqttClient/MqttServer; клиент поддерживает Alibaba Cloud IoT |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 2022 | Стандартная библиотека IoT: определяет спецификации различных протоколов связи в области интернета вещей |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 2022 | ModbusTcp/ModbusRTU/ModbusASCII на основе стандартной библиотеки IoT; поддержка платформы ZeroIoT и шлюза IoTEdge |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 2022 | Протокол Siemens PLC на основе стандартной библиотеки IoT; поддержка платформы IoT и IoTEdge |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 2022 | Библиотека картографических компонентов: обёртки для Baidu Maps, Amap, Tencent Maps и Tianditu |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 2023 | Библиотека аудиокодеков: PCM/ADPCMA/G711A/G722U/WAV/AAC |
| Продуктовые платформы | | Продукты платформенного уровня: собрал, развернул и пользуйся, с индивидуальной настройкой |
| [Stardust](https://github.com/NewLifeX/Stardust) | 2018 | Stardust (Звёздная пыль) — распределённая сервисная платформа: управление узлами, центр APM-мониторинга, центр конфигурации, центр регистрации, центр публикации |
| [AntJob](https://github.com/NewLifeX/AntJob) | 2019 | AntJob (муравьиное планирование) — распределённая платформа вычислений больших данных (реальное время/офлайн), идея шардинга «переноски муравьями»; проверена проектами триллионного масштаба |
| [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP) | 2021 | Корпоративная ERP: управление продуктами, клиентами, продажами и поставщиками |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 2006 | CrazyCoder («мастер кода»): множество инструментов разработчика — сеть, последовательный порт, шифрование и дешифрование, регулярные выражения, Modbus, MQTT |
| [EasyIO](https://github.com/NewLifeX/EasyIO) | 2023 | Простое файловое хранилище с поддержкой централизованного хранения файлов в распределённых системах |
| [XProxy](https://github.com/NewLifeX/XProxy) | 2005 | Промышленный обратный прокси: NAT-прокси, HTTP-прокси |
| [HttpMeter](https://github.com/NewLifeX/HttpMeter) | 2022 | Инструмент нагрузочного тестирования HTTP |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 2015 | Система управления исходным кодом Git |
| [SmartOS](https://github.com/NewLifeX/SmartOS) | 2014 | Встраиваемая операционная система, полностью самостоятельная и независимая, поддержка архитектуры чипов ARM Cortex-M |
| [SmartA2](https://github.com/NewLifeX/SmartA2) | 2019 | Встраиваемый промышленный компьютер, пограничный шлюз IoT, высокопроизводительный хост .NET8; применяется в промышленности, сельском хозяйстве, на транспорте и в медицине |
| Платформа IoT FIoT | 2020 | Комплексное решение для интернета вещей: строительство, экология, сельское хозяйство; интеграция ПО, аппаратного обеспечения и анализа больших данных; проверено проектом с сотнями тысяч точек на одном сервере |


---

## Команда разработчиков NewLife

![XCode](https://newlifex.com/logo.png)

Команда NewLife (新生命) основана в 2002 году — это поставщик решений для индустрии интернета вещей новой эпохи, предоставляющий консультации по программно-аппаратным решениям, планирование системной архитектуры и услуги разработки. Суммарное число загрузок NuGet — более 5,23 млн. Ключевые компоненты команды (XCode / Redis / Stardust / AntJob / IoT и другие) масштабно применяются в энергетике, промышленности, на транспорте, в охране окружающей среды, медицине, музейном деле и других отраслях.

Мы постоянно совершенствуемся, стремясь стать надёжным партнёром в области .NET / IoT / распределённых технологий, которому можно доверять в долгосрочной перспективе.

Сайт: https://newlifex.com   Организация с открытым исходным кодом: https://github.com/NewLifeX  
Официальный аккаунт WeChat:  
![智能大石头](https://newlifex.com/stone.jpg)

## Участие и обратная связь

- Перед отправкой убедитесь, что `dotnet build` и соответствующие модульные тесты проходят; стиль кода и сообщения коммитов — по конвенциям репозитория.
- Ошибки и предложения — через [Issue](https://github.com/NewLifeX/X/issues); Stars и PR приветствуются.
- История изменений — в [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md).

## Лицензия

Проект распространяется по лицензии [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE); сертификат строгого имени `newlife.snk` опубликован, чтобы вы могли самостоятельно собрать и заменить сборки.

> Если этот проект оказался вам полезен, поставьте Star, поделитесь им с командой или отправьте предложения по улучшению через Issue/PR.
