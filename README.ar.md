# NewLife.Core - المكتبة الأساسية

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md)
<!-- lang:end -->

**NewLife.Core** هو أساس منظومة NewLife: حزمة واحدة للتسجيل والتهيئة والتخزين المؤقت والشبكات والتسلسل والجدولة وحاوية الكائنات وتتبّع APM. جاهز لـ NativeAOT، ويضم رسائل وقت التشغيل بـ <!--locale-count:start-->31<!--locale-count:end--> لغة، ويستهدف 11 إطار عمل من net45 إلى net10.0، ويشغّل XCode وCube وRedis وStardust وأكثر من 30 مشروعًا شقيقًا.

دليل المكتبة الأساسية: [https://newlifex.com/core](https://newlifex.com/core)  

**المحتويات**: [البدء السريع](#البدء-السريع) · [لماذا NewLife.Core](#لماذا-newlifecore) · [الميزات الرئيسية](#الميزات-الرئيسية) · [خريطة المنظومة](#خريطة-المنظومة) · [الوثائق](#الوثائق) · [المساهمة](#المساهمة) · [الفريق](#الفريق) · [الترخيص](#الترخيص)

## البدء السريع

### التثبيت

```bash
dotnet add package NewLife.Core
```

### مثال بسيط

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // console log (also writes to file)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // in-memory cache
cache.Set("key", "value", 60);          // expires in 60 seconds
var value = cache.Get<String>("key");   // read back
```

### قالب المشروع

شغّل الأوامر التالية في سطر الأوامر لتجربة مكوّنات NewLife سريعًا:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### دعم المنصات

| البند | التغطية |
| --- | --- |
| أطر العمل المستهدفة | 11 إطارًا في حزمة واحدة: net45، net461، net462، netstandard2.0، netstandard2.1، net6.0، net6.0-windows، net8.0، net8.0-windows، net10.0، net10.0-windows |
| أنظمة التشغيل والمعماريات | Windows، Linux، macOS، Android، iOS؛ x64، x86، arm64، arm، loongarch64، riscv64، s390x |
| NativeAOT | تم قياس <!--aot-scenarios:start-->31<!--aot-scenarios:end--> سيناريو أساسيًا؛ مصفوفة اختبار CI متعددة RID (win-x64 وlinux-x64 وlinux-arm64 وlinux-loongarch64 بوابات حاجزة). الإضافات والسكربتات غير متوافقة مع AOT بحكم التصميم — انظر [تحليل توافق AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) |
| التوطين في وقت التشغيل | <!--locale-count:start-->31<!--locale-count:end--> لغة مدمجة؛ تُوطَّن رسائل السجل والاستثناءات في وقت التشغيل دون تغيير مواضع الاستدعاء (انظر [التوطين](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)) |
| الأطر القديمة | net40: `dotnet add package NewLife.Core --version 10.10.2024.801-net40` (net40/net45/net461)؛ net20: `--version 10.10.2024.701-net20` (net20 فقط). إصدارات مجمّدة من 2024-08 لا تتابع تحديثات الفرع الرئيسي. |

## لماذا NewLife.Core

- **حزمة واحدة للبنية التحتية الشائعة**: التسجيل والتهيئة والتخزين المؤقت والشبكة والتسلسل والجدولة وحاوية الكائنات وحقن التبعيات — مرجع واحد يكفي.
- **تغطية واسعة ووقت تشغيل حديث**: 11 إطار عمل مستهدفًا في حزمة واحدة (net45 → net10.0، بما في ذلك إصدارات Windows)، مع جاهزية لـ NativeAOT في المسار الأساسي ورسائل وقت التشغيل مترجمة إلى <!--locale-count:start-->31<!--locale-count:end--> لغة — نفس الواجهة البرمجية للأنظمة القديمة والمشاريع الجديدة.
- **مُثبتة على نطاق المنظومة**: الأساس نفسه يشغّل XCode (جداول واحدة بمستوى 10 مليارات صف) وNet (1.77 مليون pps / 14.5 Gbps، TCP 1KB) وRedis وStardust (عشرات المليارات من السجلات).
- **تبعيات محدودة وترخيص متسامح**: تعتمد على BCL وحزم التوافق من Microsoft فقط؛ ترخيص MIT يسمح بالتعديل وإعادة التسمية (انظر [الترخيص](#الترخيص)).

## الميزات الرئيسية

<details>
<summary><b>التوسّعات الأساسية</b> (10)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [تحويل الأنواع Utility](https://newlifex.com/core/utility) | توفّر تحويلًا فعّالًا للأنواع، مع دعم مختلف الصيغ ومعالجة الاستثناءات. |
| [إضافات السلاسل StringHelper](https://newlifex.com/core/string_helper) | تضمّ عمليات نصية متنوعة مثل الاقتصاص والترميز والتنسيق وغيرها. |
| [إضافات المسارات PathHelper](https://newlifex.com/core/path_helper) | تعالج صيغ المسارات المتعددة بذكاء، لتحقيق توافق الشيفرة عبر المنصات. |
| [إضافات البيانات IOHelper](https://newlifex.com/core/io_helper) | تحسّن عمليات الإدخال/الإخراج، وتوفّر تحويلًا فعّالًا بين تدفقات البيانات ومصفوفات البايت. |
| [إضافات العمليات ProcessHelper](https://newlifex.com/core/process_helper) | تبسّط عمليات العمليات، وتوفّر طرقًا ملائمة للحصول على معلومات العمليات وإدارتها. |
| [معلومات وقت التشغيل Runtime](https://newlifex.com/core/runtime) | توفّر طرقًا بسيطة للحصول على معلومات وقت تشغيل التطبيق، لتسهيل التصحيح والتشخيص. |
| [تحرير الموارد DisposeBase](https://newlifex.com/core/disposebase) | توفّر نمطًا قياسيًا لتحرير الموارد، يمنع تسرّب الذاكرة والموارد بفعالية. |
| [إضافات الانعكاس Reflect](https://newlifex.com/core/reflect) | أداة انعكاس عالية الأداء، لقراءة وكتابة أعضاء الكائنات بسرعة. |
| [إضافات الأمان SecurityHelper](https://newlifex.com/core/security_helper) | تدمج تشفير وفك تشفير RSA/AES/DES/MD5 وغيرها، لتبسيط العمليات الأمنية. |
| [تجزئة الإحداثيات GeoHash](https://newlifex.com/core/geo_hash) | يرمّز خط الطول والعرض إلى سلسلة نصية، للبحث عن المواقع القريبة. |

</details>

<details>
<summary><b>التسجيل والتتبّع</b> (2)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [التسجيل ILog](https://newlifex.com/core/log) | يدعم طرقًا متعددة لإخراج السجلات، ويوفّر كتابة غير متزامنة قوية. |
| [تتبّع المسار ITracer](https://newlifex.com/core/tracer) | تتبّع أداء APM قوي، يدعم التكامل مع منصة Stardust. |

</details>

<details>
<summary><b>الجدولة الزمنية</b> (3)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [المؤقّت المتقدّم TimerX](https://newlifex.com/core/timerx) | مؤقّت عالي الدقة، يدعم جدولة Cron والتنفيذ الدوري غير المتزامن. |
| [تعبير Cron](https://newlifex.com/core/cron) | يوفّر مهامًا مجدولة شبيهة بـ Cron في Linux، ويدعم المطابقة على مستوى الثانية. |
| [موفّر الوقت TimeProvider](https://newlifex.com/core/timerx_timeprovider) | تجريد موحّد للوقت وحساب عالي الدقة للزمن المنقضي، لتسهيل التوافق بين الإصدارات. |

</details>

<details>
<summary><b>نظام الإعدادات</b> (4)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [نظام الإعدادات Config](https://newlifex.com/core/config) | إطار إعدادات موحّد، يدعم صيغًا متعددة ومصادر متعددة وآلية إعادة التحميل الفوري. |
| [موفّر الإعدادات IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | واجهة إعدادات موحّدة، تدعم الاستدعاءات العكسية ونموذج عُقد الإعدادات. |
| [الإعدادات الأساسية Setting](https://newlifex.com/core/setting) | تدير إعدادات المكوّنات الأساسية، وتتحكّم في السلوكيات الأساسية مثل التسجيل والشبكات. |
| [إعدادات النظام SysConfig](https://newlifex.com/core/sysconfig) | تدير إعدادات نظام التطبيق، وتدعم الإعدادات المحلية والموزّعة والمشفّرة. |

</details>

<details>
<summary><b>التسلسل</b> (7)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [تسلسل Json](https://newlifex.com/core/json) | أداة Json عالية الأداء، تدعم التسلسل في سيناريوهات معقّدة متعددة. |
| [تسلسل Xml](https://newlifex.com/core/xml) | توفّر تسلسل XML وإلغاء تسلسله، وتدعم التحويل المتبادل بين الكائنات وXML. |
| [التسلسل الثنائي](https://newlifex.com/core/binary) | تسلسل ثنائي عالي الأداء، يدعم مخططات الكائنات المعقّدة والتحكّم في الإصدارات. |
| [التسلسل الثنائي المتقدّم](https://newlifex.com/core/advanced_binary) | دليل متقدّم للترميز وفك الترميز دون أي تخصيص على الإطلاق لرسائل البروتوكول. |
| [تحليل Csv عبر CsvFile](https://newlifex.com/core/csv_file) | قراءة وكتابة سريعة لملفات CSV، مع دعم فواصل مخصّصة وربط البيانات. |
| [قاعدة بيانات Csv عبر CsvDb](https://newlifex.com/core/csv_db) | استخدام ملفات CSV كقاعدة بيانات، مع دعم عمليات الإضافة والحذف والتعديل والاستعلام الأساسية. |
| [قارئ Excel خفيف ExcelReader](https://newlifex.com/core/excel_reader) | لا يحتاج إلى Office، ويقرأ ملفات Excel بسرعة، مع دعم كميات البيانات الكبيرة. |

</details>

<details>
<summary><b>التخزين المؤقت</b> (7)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [واجهة التخزين المؤقت الموحّدة ICache](https://newlifex.com/core/icache) | تعرّف عمليات تخزين مؤقت قياسية، وتوفّر تنفيذات متعددة مثل الذاكرة وRedis. |
| [بنية التخزين المؤقت ICacheProvider](https://newlifex.com/core/icacheprovider) | تدير نسخ ICache وتوفّرها، مع دعم وجود ذاكرات مؤقتة متعددة معًا. |
| [ذاكرة التخزين المؤقت MemoryCache](https://newlifex.com/core/memory_cache) | ذاكرة تخزين مؤقت عالية الأداء على جهاز واحد، تدعم سياسات الانتهاء والسعة. |
| [مجمّع الكائنات ObjectPool](https://newlifex.com/core/object_pool) | يعيد استخدام الكائنات المكلفة، لتحسين الأداء وتقليل ضغط GC. |
| [مجمّع الكائنات Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | مجمّع كائنات عام عالي الأداء قائم على CAS، يقلّل ضغط GC. |
| [التخزين المؤقت الموزّع NewLife.Redis](https://newlifex.com/core/redis) | تنفيذ تخزين مؤقت موزّع قائم على Redis، يدعم هياكل بيانات متعددة. |
| [تخزين المتجهات IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | واجهة إدارة مجموعات المتجهات، تدعم استرجاع التشابه Top-K، وتوفّر بنية تحتية للاسترجاع المتجهي لتطبيقات AI. |

</details>

<details>
<summary><b>المخازن المؤقتة عالية الأداء</b> (7)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [حزمة البيانات IPacket](https://newlifex.com/core/packet) | واجهة حزمة بيانات أساسية للشرائح بدون نسخ والحزم المتسلسلة. |
| [المخازن المؤقتة Buffers](https://newlifex.com/core/buffers) | مكتبة عمليات مخازن مؤقتة عالية الأداء، تجمع مكوّنات مثل Span والكتابة المُجمّعة. |
| [كاتب مُجمّع PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | كاتب مخزن مؤقت مُجمّع قائم على ArrayPool، يدعم التوسّع الديناميكي. |
| [قارئ Span SpanReader](https://newlifex.com/core/span_reader) | محلّل تدفّق ثنائي عالي الأداء بدون تخصيص، يدعم ترتيب البايتات الكبير والصغير. |
| [كاتب Span SpanWriter](https://newlifex.com/core/span_writer) | كاتب بيانات ثنائية عالي الأداء بدون تخصيص، يدعم ترتيب البايتات الكبير والصغير. |
| [مساعد Span SpanHelper](https://newlifex.com/core/span_helper) | مجموعة طرق إضافية لـ Span/ReadOnlySpan، تبسّط عمليات الذاكرة. |
| [قارئ التسلسل SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | قارئ متسلسل عبر المقاطع (الأطر القديمة مكتملة عبر حشوات، ومتاح في جميع 11 TFM). |

</details>

<details>
<summary><b>إطار التطبيق</b> (15)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [حاوية الكائنات ObjectContainer](https://newlifex.com/core/object_container) | حقن تبعيات خفيف، يدعم دورات حياة متعددة وأنماط حقن متنوعة. |
| [مضيف التطبيق الخفيف Host](https://newlifex.com/core/host) | يبسّط استضافة التطبيقات، لبناء خدمات خلفية طويلة التشغيل بسهولة. |
| [إطار الإضافات IPlugin](https://newlifex.com/core/plugin) | يوفّر آلية موحّدة لإدارة الإضافات، لتحقيق التطوير المعياري للتطبيق. |
| [نموذج التوازي Actor](https://newlifex.com/core/actor) | نموذج برمجة تزامنية مبسّط، يرفع الإنتاجية عبر المعالجة بقوائم الرسائل. |
| [قائمة الانتظار المؤجّلة DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | يجمع التغييرات عالية التكرار ويعالجها على دفعات، لتقليل تضخيم الكتابة. |
| [ناقل الأحداث EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | توزيع أحداث داخل العملية، يدعم توجيه المواضيع والرسائل بنمط الطوابير. |
| [تفويض مرجعي ضعيف WeakAction](https://newlifex.com/core/weak_action) | يحلّ مشكلة تسرّب الذاكرة في اشتراكات الأحداث، لتحقيق معالجة أحداث آمنة. |
| [محرّك السكربتات ScriptEngine](https://newlifex.com/core/script_engine) | يدعم التصريف والتشغيل الديناميكي لـ C#، ويوفّر قدرة توسيع مرنة عبر السكربتات. |
| [الضغط وفك الضغط](https://newlifex.com/core/compression) | يدمج خوارزميات ضغط متعددة، ويوفّر واجهة موحّدة لضغط البيانات. |
| [الأرشفة المضغوطة Tar و7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | يغطّي الاستخدامات المتقدّمة للأرشفة والضغط عبر Tar/Tar.Gz و7Zip. |
| [معلومات الجهاز MachineInfo](https://newlifex.com/core/machine_info) | يحصل على معلومات العتاد بشكل شامل، ويدعم نظامي Windows وLinux. |
| [مكتبة Pinyin PinYin](https://newlifex.com/core/pinyin) | مكتبة فعّالة لتحويل الأحرف الصينية إلى Pinyin، تدعم التهجئة الكاملة والمختصرة بأداء متميّز. |
| [جدول البيانات DbTable](https://newlifex.com/core/dbtable) | جدول بيانات في الذاكرة، يسهّل معالجة البيانات وتحليلها. |
| [معاملات الترقيم PageParameter](https://newlifex.com/core/page_parameter) | يغلّف منطق الترقيم، ويدعم الترقيم في سيناريوهات متعددة مثل Web وAPI. |
| [خوارزمية Snowflake](https://newlifex.com/core/snow_flake) | خوارزمية توليد معرّفات فريدة موزّعة، تضمن تفرّد المعرّفات في ظل التزامن العالي. |

</details>

<details>
<summary><b>الأمان والرموز</b> (3)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [رمز التوقيع الرقمي الموزّع TokenProvider](https://newlifex.com/core/token_provider) | يولّد الرموز الموزّعة ويتحقق منها، ويضمن الأمان بين العقد المتعددة. |
| [الرمز العام لـ Web JwtBuilder](https://newlifex.com/core/jwt) | يوفّر توليد رموز JWT وفك ترميزها والتحقق منها، لتبسيط التحقق في Web. |
| [فلتر رموز HTTP TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | يحقن الرمز تلقائيًا في طلبات HTTP، ويعترض 401/403 ويجدّد الرمز تلقائيًا. |

</details>

<details>
<summary><b>مكتبة الشبكات</b> (13)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [خادم الشبكة NetServer](https://newlifex.com/core/netserver) | خادم عالي الأداء لمقابس TCP/UDP/Unix domain، يغلّف الجلسات ومعالجة البيانات. |
| [عميل الشبكة NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | عميل شبكة موحّد TCP/UDP/Unix، يبسّط الاتصال والإرسال والاستقبال. |
| [الرسالة IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | عقد رسائل طلب-استجابة، يحمل دلالة الاتجاه وقراءة الإطارات وكتابتها. |
| [مكدّس بروتوكول الرسائل MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | مكدّس واحد يجمع مضخة الإطارات ومبرمج/فاكّ ترميز عديم الحالة، ويدعم الأجسام المتدفقة ذات الإطارات الكبيرة والضغط العكسي. |
| [أنبوب البيانات Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | أنبوب بيانات بدون نسخ، بمحاذاة شكل System.IO.Pipelines. |
| [بروتوكول تبادل الرسائل البعيد المبسّط SRMP](https://newlifex.com/core/srmp) | بروتوكول RPC خفيف، يدعم التسلسل الثنائي والنقل عبر UDP. |
| [موازنة التحميل وتجاوز الأعطال LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | يوفّر ثلاث استراتيجيات لتوزيع العقد المتعددة: Failover والتدوير والتنافس. |
| [عميل HTTP ApiHttpClient](https://newlifex.com/core/api_http) | مبني على تغليف HttpClient، يبسّط استدعاء Web API ومعالجته. |
| [خادم HTTP HttpServer (الأجهزة المدمجة)](https://newlifex.com/core/httpserver) | خادم Http خفيف، يدعم خط الوسائط البرمجية والتوجيه ذا المعاملات وخدمة الملفات الثابتة. |
| [تنزيل الشبكة WebClientX](https://newlifex.com/core/webclient) | نسخة محسّنة من WebClient، تدعم التنزيل متعدّد الخيوط والتنزيل القابل للاستئناف. |
| [خادم WebSocket (أداة قوية لإشعارات إنترنت الأشياء الهابطة)](https://newlifex.com/core/websocket) | خادم WebSocket عالي الأداء، مناسب لدفع رسائل إنترنت الأشياء. |
| [اتصال WebSocket ثنائي الاتجاه](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | يشرح المصافحة ومعالجة الإطارات ونبضات القلب وإغلاق الاتصال عمليًا. |
| [عميل تخزين الملفات EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | عميل تخزين ملفات عبر HTTP، مخصّص لـ EasyIO أو OSS. |

</details>

> انتقل RPC (`ApiServer`/`ApiClient`) إلى [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting).

## خريطة المنظومة

NewLife منظومة متعدّدة الطبقات من مشاريع مفتوحة المصدر تُصدَر بإصدارات مستقلة. الخريطة أدناه مشتركة حرفيًا بين جميع مستودعات NewLife؛ وكل مشروع يرتبط بمستودعه وبحزمته الرئيسية على NuGet.

| المشروع | الوظيفة | التنزيلات |
| --- | --- | --- |
| **الأساس** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | التسجيل والتهيئة والتخزين المؤقت والشبكات والتسلسل والجدولة وتتبّع أداء APM. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | وسيط بيانات: جداول واحدة بمستوى عشرات المليارات من الصفوف، تقسيم تلقائي للجداول، فصل القراءة عن الكتابة، وأكثر من 10 قواعد بيانات. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | مكتبة شبكات عالية الأداء: 1.77 مليون pps / 14.5 Gbps على جهاز واحد (TCP 1KB، 2026-10)، و4 ملايين اتصال TCP طويل الأمد. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | إطار اتصال بروتوكولي وRPC لتطبيقات العميل/الخادم وأجهزة إنترنت الأشياء. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | يثبّت تطبيقات .NET كخدمات حارسة لنظام التشغيل: خدمات Windows وsystemd في Linux. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | قوالب مشاريع `dotnet new` للطرفية وweb وAPI والخدمات والبيانات وإنترنت الأشياء. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **البيانات والمراسلة** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | عميل Redis بزمن استجابة بالميكروثانية، وملايين العمليات في الثانية، وقوائم رسائل مدمجة. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | عميل RocketMQ مُدار بالكامل لـ Apache RocketMQ وخدمة رسائل Alibaba Cloud. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | عميل وخادم MQTT، بما في ذلك الاتصال بـ Alibaba Cloud IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | قاعدة بيانات هجينة بلغة C#: مدمجة أو شبكية، وعلائقية، وسلاسل زمنية، وقائمة رسائل، وKV في واحد. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | برنامج تشغيل MySql وطبقة مساعدة مبنية على مكدّس بيانات NewLife. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web والذكاء الاصطناعي والأنظمة الموزّعة** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | إطار منصة إدارة سريعة: الصلاحيات، والدخول الموحّد SSO، وخادم OAuth، وتوليد واجهات CRUD. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | منصة خدمات موزّعة: إدارة العقد، وAPM، ومركز الإعدادات، ومركز التسجيل، ومركز النشر. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | منصة موزّعة للمهام وجدولة البيانات الفورية، مع التقسيم وإعادة المحاولة وإعادة التعبئة. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | عميل موحّد لـ 46 مزوّد LLM بالإضافة إلى ChatAI، تطبيق ويب جاهز للمحادثة. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **الصناعة وإنترنت الأشياء** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | مكتبة معايير إنترنت الأشياء التي تعرّف عقود البروتوكولات وتجريدات الأجهزة. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | تنفيذ Modbus TCP/RTU/ASCII فوق NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | بروتوكول Siemens PLC فوق NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | بروتوكول Omron PLC فوق NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | بروتوكول Mitsubishi Melsec PLC فوق NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | عميل OPC للوصول إلى البيانات الصناعية. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | مكدّس بروتوكول BACnet لأتمتة المباني. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | منصة إنترنت أشياء مفتوحة المصدر وخفيفة، مع إدارة البوابات والأجهزة. | — |
| **تليماتيكا المركبات** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | بروتوكول تحديد مواقع المركبات JT/T 808: أكثر من 70 جسم رسالة، 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | بروتوكول JT/T 809 بين المنصات لجهات إدارة النقل. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | فيديو داخل المركبة JT/T 1078: تحليل RTP وإعادة التجميع إلى FLV/fMP4 والبث المباشر. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **مكوّنات المجال** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | حزمة واحدة لـ Excel وWord وPPT وPDF وأكثر من 15 صيغة مستندات. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | صوت عبر المنصات: مرمّزات، وإدخال/إخراج الأجهزة، وDSP، وحاويات، وبث. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | مرمّزات الفيديو وصيغ الحاويات ومعالجة الإطارات والبث. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | واجهة موحّدة فوق Baidu وAMap وTencent وTianditu مع الترميز الجغرافي وتحويل الإحداثيات. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | تحديد الموقع الجغرافي لعناوين IPv4 بقاعدة بيانات غير متصلة مُعيّنة في الذاكرة، وواجهة API عبر الإنترنت كبديل. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **الأدوات والمنتجات** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | صندوق أدوات المطوّر: الشبكات والمنفذ التسلسلي والتشفير والتعبيرات النمطية وModbus وMQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | نظام استضافة ذاتية لإدارة الشيفرة المصدرية Git. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | يثبّت بيئة تشغيل .NET المطابقة تلقائيًا عند التشغيل من مضيف أقدم. | — |

> تعرض الشارات الحزمة الرئيسية لكل مشروع؛ أما حزم السمات والإضافات والحزم التابعة فمذكورة في صفحاتها على NuGet. الحزم التي تتجاوز 30 حزمة وغير المذكورة هنا (FTP وLoRa وإضافات Office وبرامج تشغيل البروتوكولات وغيرها) متاحة في [منظمة NewLifeX](https://github.com/NewLifeX) وعلى [newlifex.com](https://newlifex.com).

## الوثائق

### الأدلة

- [فهرس المكتبة الأساسية](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): فهرس مقالات المكوّنات الأساسية، مجمّعًا حسب الوحدة
- [تحليل توافق AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): نطاق NativeAOT، والنتائج المقاسة (اختبار سريع <!--aot-smoke:start-->31/31<!--aot-smoke:end-->)، ومصفوفة RID والحدود المعروفة
- [التوطين](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md): اللغات المدعومة والاستخدام الأساسي
- [البنية المعمارية للجوال](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md): مكدّس .NET للجوال، واختيار إطار واجهة المستخدم، وتغليف APK، وملاحظات AOT
- [أمثلة](https://github.com/NewLifeX/X/tree/master/Samples): أمثلة الطرفية والخدمة وHttp وإنترنت الأشياء والجوال

### ملاحظات الإصدار

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md): تاريخ الإصدارات

## المساهمة

### البناء والاختبار

يُفتح الحل في Visual Studio (أحدث إصدار رسمي) ويُبنى أيضًا عبر .NET CLI؛ وتبقى ملفات المشروع بسيطة عمدًا بحيث يُبنى أي نسخ جديد مباشرة دون إعداد إضافي.

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- تشغيل الاختبارات الكامل بالتوازي: `run-tests.ps1` (مضيفات متعددة بالتوازي + مهلة + ملخص trx)
- اختبار NativeAOT السريع: `AotSmoke/`، وCI في `.github/workflows/aot.yml`
- قياسات الأداء: `Benchmark/` (BenchmarkDotNet)

### هيكل المستودع

| الدليل | الوصف |
| --- | --- |
| `NewLife.Core/` | شيفرة المكتبة الأساسية (التسجيل، الشبكة، التخزين المؤقت، التسلسل، …) |
| `NewLife.Core.SourceGenerator/` | مولّد الشيفرة الذي يُوزّع داخل الحزمة ضمن `analyzers/` |
| `Doc/` | وثائق الإمكانات وفهرس المقالات |
| `Samples/` | أمثلة الطرفية / الخدمة / Http / إنترنت الأشياء / الجوال |
| `Benchmark/` | قياسات أداء BenchmarkDotNet |
| `XUnitTest.Core/` | اختبارات الوحدة الرئيسية (net10.0) |
| `XUnitTest.Compat/`, `XUnitTest.Net6/` | اختبارات التوافق بين الإصدارات |
| `AotSmoke/` | مشروع اختبار NativeAOT السريع |

### الإصدار

ادفع وسمًا يبدأ بـ `v` (مثل `v12.0.2026.0927`) لتفعيل `.github/workflows/publish.yml`. يحزم سير العمل الحزمة وينشرها إلى NuGet.org وGitHub Packages، وينشئ GitHub Release مأخوذة ملاحظاته من القسم المقابل في `ChangeLog.md` (تتضمّن الملفات المرفقة nupkg وsnupkg). التشغيل اليدوي يقتصر على نشر الحزم ولا ينشئ Release.

### وثائق التطوير

يحتفظ دليل `Doc/` بمنظومة وثائق قدرات تعكس الشيفرة: المتطلبات → تتبّع الميزات → البنية. وهذه الوثائق مكتوبة بالصينية.

- [وثيقة المتطلبات](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): الرؤية والأهداف الأساسية والمتطلبات الوظيفية
- [قائمة الميزات](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): حالة التنفيذ والاختبار والتعليقات لكل ميزة
- [تصميم البنية](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): التقسيم الطبقي ومكوّنات الوحدات وقرارات التصميم الرئيسية

### الملاحظات

- قبل الإرسال تأكد من نجاح `dotnet build` واختبارات الوحدة ذات الصلة؛ واتبع اتفاقيات المستودع في أسلوب الشيفرة ورسائل الالتزام.
- أبلغ عن الأخطاء والاقتراحات عبر [Issue](https://github.com/NewLifeX/X/issues)؛ نرحّب بـ Stars وPRs.

## الفريق

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

تأسّست NewLife في عام 2002، وأمضت أكثر من عقدين في منظومة .NET وإنترنت الأشياء، وتدير ما يقارب 100 مشروع مفتوح المصدر (34 منها مشاريع رئيسية)، وتقدّم حلولًا متكاملة للبرمجيات والأجهزة لقطاعات الكهرباء والخدمات اللوجستية والرعاية الصحية والتحكّم الصناعي والإنترنت. ونلتزم بمبدأ "لا أسبقية في التعلّم — الإتقان يكسب الاحترام".

نُزّلت المكوّنات مفتوحة المصدر <!--nuget-total:start-->6.48M<!--nuget-total:end--> مرة من NuGet (على مستوى المنظومة بأكملها، حتى 2026-10).

الموقع: https://newlifex.com · المنظمة مفتوحة المصدر: https://github.com/NewLifeX

<details>
<summary>حساب WeChat الرسمي: 智能大石头 (امسح للاشتراك)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## الترخيص

هذا المشروع مرخّص بموجب [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE)؛ شهادة الاسم القوي `newlife.snk` منشورة لتتمكن من بناء التجميعات واستبدالها بنفسك، ويمكن تعديل الشيفرة المصدرية وإعادة توزيعها دون ذكر المصدر.

> إذا كان هذا المشروع مفيدًا لك، فنرحّب بـ Star أو مشاركته مع فريقك، أو إرسال اقتراحات التحسين عبر Issue/PR.

