# NewLife.Core - المكتبة الأساسية

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
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md)
<!-- lang:end -->

إطار NewLife منظومة مكوّنات .NET شاملة، يوفّر بنية أساسية عالية الأداء لبناء تطبيقات قابلة للتوسّع. يقدّم وظائف أساسية مثل التسجيل والشبكات والتسلسل والتخزين المؤقت وتعدّد الخيوط، ليكون أساسًا لسلسلة من التطبيقات تمتدّ من خدمات Web إلى أجهزة IoT.  

دليل المكتبة الأساسية: [https://newlifex.com/core](https://newlifex.com/core)  

> ⚠️ قد تتأخر هذه الترجمة عن أحدث نسخة؛ المحتوى المعتمد هو [README الصيني](Readme.MD).


**المحتويات**: [نظرة عامة على الإطار](#نظرة-عامة-على-الإطار) · [لماذا NewLife.Core](#لماذا-newlifecore) · [البدء السريع](#البدء-السريع) · [الميزات الرئيسية](#الميزات-الرئيسية) · [فهرس الوثائق](#فهرس-الوثائق) · [هيكل المستودع](#هيكل-المستودع) · [البناء والاختبار](#البناء-والاختبار) · [مصفوفة مشاريع NewLife](#مصفوفة-مشاريع-newlife) · [المساهمة والملاحظات](#المساهمة-والملاحظات) · [الترخيص](#الترخيص)

## نظرة عامة على الإطار

يتكوّن NewLife من عدة مكتبات مكوّنات، وتوفّر NewLife.Core الأساس لها. يدعم الإطار أهداف الترجمة المتعددة net45~net10.0 (من .NET Framework 4.5 إلى .NET 10.0)، ما يتيح توافقًا واسعًا في بيئات مختلفة.

**NativeAOT**: المسار الأساسي مدعوم (اختبار فعلي في 29 سيناريو + CI لمنع الانحدار)؛ القدرات الديناميكية مثل الإضافات والسكربتات غير مدعومة بحكم التصميم، ويمكن الاطلاع على الحدود في [تحليل توافق AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) (中文).


## لماذا NewLife.Core

- **حزمة واحدة للبنية التحتية الشائعة**: التسجيل، التهيئة، التخزين المؤقت، الشبكة، التسلسل، الجدولة، حاوية الكائنات وحقن التبعيات — مرجع واحد يكفي.
- **تغطية واسعة للإصدارات**: 11 إطار عمل مستهدفًا في حزمة واحدة (net45 → net10.0، بما في ذلك إصدارات Windows)؛ نفس الواجهة البرمجية للأنظمة القديمة والمشاريع الجديدة.
- **جاهز لـ NativeAOT**: تم قياس 29 سيناريو أساسيًا وحمايتها بمصفوفة اختبار CI متعددة RID؛ الحدود في [تحليل توافق AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md).
- **تبعيات محدودة وترخيص متسامح**: تعتمد على BCL وحزم التوافق من Microsoft فقط؛ ترخيص MIT يسمح بالتعديل وإعادة التسمية (انظر [الترخيص](#الترخيص)).

## البدء السريع

### التثبيت

```bash
dotnet add package NewLife.Core
```

### مثال بسيط

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // سجل الطرفية (يُكتب إلى ملف أيضًا)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // ذاكرة مؤقتة
cache.Set("key", "value", 60);          // ينتهي بعد 60 ثانية
var value = cache.Get<String>("key");   // قراءة
```

### البناء والتخصيص

مكوّنات NewLife من إبداع المجتمع منذ أكثر من 20 عامًا وتُستخدم بموجب ترخيص MIT مفتوح المصدر، **ويحق لأي شخص تعديلها وإعادة نشرها كما يشاء** (دون الحاجة إلى ذكر المصدر)! وعند بناء كثير من الشركات أُطر تطوير داخلية على أساسها، يمكنها حتى تحقيق تخصيص خاص بها عبر استبدال جميع سلاسل `NewLife` في الشيفرة المصدرية باسم الشركة.

يتبنّى الفريق دائمًا نهجًا منفتحًا؛ فهو لا يدعم فقط فتح الحل وبنائه في VisualStudio (أحدث إصدار رسمي)، بل يتوافق أيضًا مع البناء عبر سطر الأوامر `dotnet build`، وقد استُبعدت الميزات المعقّدة من ملفات المشروع سعيًا إلى البساطة وسهولة الاستخدام، ليكون جاهزًا للاستخدام مباشرة.

ننشر شهادة التسمية القوية `newlife.snk` لدعم بناء واستبدال التجميعات بشكل مستقل.

### قالب المشروع

شغّل الأوامر التالية في سطر الأوامر لتجربة مكوّنات NewLife سريعًا:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

## فهرس الوثائق

### وثائق الاستخدام

- [فهرس المكتبة الأساسية](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md) (中文): فهرس مقالات المكوّنات الأساسية
- [تحليل توافق AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) (中文): نطاق دعم NativeAOT والنتائج الفعلية (اختبار سريع 29/29) وإرشادات الاستخدام والحدود المعروفة

### وثائق التطوير

يحتفظ المستودع في دليل `Doc/` بمنظومة وثائق القدرات (تعريف المتطلبات → تتبّع الميزات → تصميم البنية):

- [وثيقة المتطلبات](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md) (中文): الرؤية والأهداف الأساسية والمتطلبات الوظيفية
- [قائمة الميزات](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md) (中文): تتبّع حالة التنفيذ/الاختبار/التعليقات لجميع نقاط الميزات
- [تصميم البنية](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md) (中文): التقسيم الطبقي ومكوّنات الوحدات وقرارات التصميم الرئيسية

## الميزات الرئيسية


<details>
<summary><b>التوسّعات الأساسية</b> (9)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [تحويل الأنواع Utility](https://newlifex.com/core/utility) | توفّر تحويلًا فعّالًا للأنواع، مع دعم مختلف الصيغ ومعالجة الاستثناءات. |
| [إضافات السلاسل StringHelper](https://newlifex.com/core/string_helper) | تضمّ عمليات نصية متنوعة مثل الاقتصاص والترميز والتنسيق وغيرها. |
| [إضافات المسارات PathHelper](https://newlifex.com/core/path_helper) | تعالج صيغ المسارات المتعددة بذكاء، لتحقيق توافق الشيفرة عبر المنصات. |
| [إضافات البيانات IOHelper](https://newlifex.com/core/io_helper) | تحسّن عمليات الإدخال/الإخراج، وتوفّر تحويلًا فعّالًا بين تدفقات البيانات ومصفوفات البايت. |
| [إضافات العمليات ProcessHelper](https://newlifex.com/core/process_helper) | تبسّط عمليات العمليات، وتوفّر طرقًا ملائمة للحصول على معلومات العمليات وإدارتها. |
| [معلومات وقت التشغيل Runtime](https://newlifex.com/core/runtime) | توفّر طرقًا بسيطة للحصول على معلومات وقت تشغيل التطبيق، لتسهيل التصحيح والتشخيص. |
| [قابل للتدمير DisposeBase](https://newlifex.com/core/disposebase) | توفّر نمطًا قياسيًا لتحرير الموارد، يمنع تسرّب الذاكرة والموارد بفعالية. |
| [إضافات الانعكاس Reflect](https://newlifex.com/core/reflect) | أداة انعكاس عالية الأداء، لقراءة وكتابة أعضاء الكائنات بسرعة. |
| [إضافات الأمان SecurityHelper](https://newlifex.com/core/security_helper) | تدمج تشفير وفك تشفير RSA/AES/DES/MD5 وغيرها، لتبسيط العمليات الأمنية. |

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
| [بنية التخزين المؤقت ICacheProvider](https://newlifex.com/core/icacheprovider) | تُستخدم لإدارة نسخ ICache وتوفيرها، مع دعم وجود ذاكرات مؤقتة متعددة معًا. |
| [ذاكرة التخزين المؤقت MemoryCache](https://newlifex.com/core/memory_cache) | ذاكرة تخزين مؤقت عالية الأداء على جهاز واحد، تدعم سياسات الانتهاء والسعة. |
| [مجمّع الكائنات ObjectPool](https://newlifex.com/core/object_pool) | يُستخدم لإعادة استخدام الكائنات المكلفة، لتحسين الأداء وتقليل ضغط GC. |
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
| [قارئ التسلسل SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | قارئ متسلسل عبر المقاطع (الإطارات القديمة مكتملة عبر حشوات، ومتاح في جميع 11 TFM). |

</details>

<details>
<summary><b>إطار التطبيق</b> (16)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [حاوية الكائنات ObjectContainer](https://newlifex.com/core/object_container) | حقن تبعيات خفيف، يدعم دورات حياة متعددة وأنماط حقن متنوعة. |
| [مضيف التطبيق الخفيف Host](https://newlifex.com/core/host) | يبسّط استضافة التطبيقات، لبناء خدمات خلفية طويلة التشغيل بسهولة. |
| [إطار الإضافات IPlugin](https://newlifex.com/core/plugin) | يوفّر آلية موحّدة لإدارة الإضافات، لتحقيق التطوير المعياري للتطبيق. |
| [نموذج التوازي Actor](https://newlifex.com/core/actor) | نموذج برمجة تزامنية مبسّط، يرفع الإنتاجية عبر المعالجة بقوائم الرسائل. |
| [نموذج الأنابيب Pipeline](https://github.com/NewLifeX/X/blob/master/Doc/管道模型Pipeline.md) ⚠️ تم إيقافه، واستُبدل بنمط البروتوكول (مضخة الإطارات) | نموذج سلسلة معالجة ثنائي الاتجاه، يوحّد عمليات Read/Write/Open/Close. |
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
| [رمز التوقيع الرقمي الموزّع TokenProvider](https://newlifex.com/core/token_provider) | يُستخدم لتوليد الرموز الموزّعة والتحقق منها، ويضمن الأمان بين العقد المتعددة. |
| [الرمز العام لـ Web JwtBuilder](https://newlifex.com/core/jwt) | يوفّر توليد رموز JWT وفك ترميزها والتحقق منها، لتبسيط التحقق في Web. |
| [فلتر رموز HTTP TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | يحقن الرمز تلقائيًا في طلبات HTTP، ويعترض 401/403 ويجدّد الرمز تلقائيًا. |

</details>

<details>
<summary><b>خدمات السحابة</b> (1)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [تجزئة الإحداثيات GeoHash](https://newlifex.com/core/geo_hash) | يرمّز خط الطول والعرض إلى سلسلة نصية، للبحث عن المواقع القريبة. |

</details>

<details>
<summary><b>مكتبة الشبكات</b> (15)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| [خادم الشبكة NetServer](https://newlifex.com/core/netserver) | خادم عالي الأداء لمقابس TCP/UDP/Unix domain، يغلّف الجلسات ومعالجة البيانات. |
| [عميل الشبكة NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | عميل شبكة موحّد TCP/UDP/Unix، يبسّط الاتصال والإرسال والاستقبال. |
| [الرسالة IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | عقد رسائل طلب-استجابة، يحمل دلالة الاتجاه وقراءة الإطارات وكتابتها. |
| [مكدّس بروتوكول الرسائل MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | مكدّس واحد يجمع مضخة الإطارات ومبرمج/فاكّ ترميز عديم الحالة، ويدعم الأجسام المتدفقة ذات الإطارات الكبيرة والضغط العكسي. |
| [أنبوب البيانات Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | أنبوب بيانات بدون نسخ، بمحاذاة شكل System.IO.Pipelines. |
| [بروتوكول تبادل الرسائل البعيد المبسّط SRMP](https://newlifex.com/core/srmp) | بروتوكول RPC خفيف، يدعم التسلسل الثنائي والنقل عبر UDP. |
| [موازنة التحميل وتجاوز الأعطال LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | يوفّر ثلاث استراتيجيات لتوزيع العقد المتعددة: Failover/التدوير/التنافس. |
| [اتصال RPC ApiServer](https://newlifex.com/core/api_server) | ⚠️ تم نقله إلى [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [اتصال RPC ApiClient](https://newlifex.com/core/api_client) | ⚠️ تم نقله إلى [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [عميل HTTP ApiHttpClient](https://newlifex.com/core/api_http) | مبني على تغليف HttpClient، يبسّط استدعاء Web API ومعالجته. |
| [خادم HTTP HttpServer (الأجهزة المدمجة)](https://newlifex.com/core/httpserver) | خادم Http خفيف، يدعم خط الوسائط البرمجية والتوجيه ذا المعاملات وخدمة الملفات الثابتة. |
| [تنزيل الشبكة WebClientX](https://newlifex.com/core/webclient) | نسخة محسّنة من WebClient، تدعم التنزيل متعدّد الخيوط والتنزيل القابل للاستئناف. |
| [خادم WebSocket (أداة رائعة للإشعارات الهابطة في إنترنت الأشياء)](https://newlifex.com/core/websocket) | خادم WebSocket عالي الأداء، مناسب لدفع رسائل إنترنت الأشياء. |
| [اتصال WebSocket ثنائي الاتجاه](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | يشرح المصافحة ومعالجة الإطارات ونبضات القلب وإغلاق الاتصال عمليًا. |
| [عميل تخزين الملفات EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | عميل تخزين ملفات عبر HTTP، يتكامل مع EasyIO أو OSS. |

</details>

> ⚠️ تم استخراج خرائط Baidu/Gaode إلى حزمة NuGet مستقلة.


## هيكل المستودع

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

## البناء والاختبار

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- تشغيل الاختبارات الكامل بالتوازي: `run-tests.ps1` (مضيفات متعددة بالتوازي + مهلة + ملخص trx)
- اختبار NativeAOT السريع: `AotSmoke/`، وCI في `.github/workflows/aot.yml`
- قياسات الأداء: `Benchmark/` (BenchmarkDotNet)

## مصفوفة مشاريع NewLife

تدعم جميع المشاريع افتراضيًا net10.0/netstandard2.1/netstandard2.0/net462/net4.5، بينما الإصدارات القديمة (2024.0801) تدعم net4.0/net2.0  

|                               المشروع                               | السنة | الوصف                                                                                        |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
|                             المكوّنات الأساسية                             |       | تدعم بقية البرمجيات الوسيطة ومشاريع المنتجات                                                                  |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | المكتبة الأساسية: التسجيل والإعدادات والتخزين المؤقت والشبكات والتسلسل وتتبّع أداء APM                                         |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | وسيط بيانات ضخمة، جدول واحد بمستوى مئة مليار سجل، MySql/SQLite/SqlServer/Oracle/PostgreSql/DaMeng، تقسيم تلقائي للجداول، فصل القراءة عن الكتابة |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | مكتبة شبكات، إنتاجية بمستوى عشرة ملايين على جهاز واحد (22.66 مليون tps)، وملايين الاتصالات على جهاز واحد (4 ملايين اتصال Tcp طويل الأمد)                     |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011  | مكتبة اتصال بروتوكولية، توفّر إطار اتصال لتطبيقات CS، وتدعم إطاري اتصال Http/RPC، بإنتاجية عالية وتكلفة منخفضة وسهولة اتصال لأجهزة إنترنت الأشياء        |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | منصة تطوير سريعة Cube، تدمج صلاحيات المستخدمين وتسجيل الدخول الموحّد SSO وخدمة OAuth، ومُثبّتة بمشروع بمستوى 10 مليارات سجل في جدول واحد               |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | مكوّن إدارة الخدمات، يثبّت التطبيق كخدمة حارسة لنظام التشغيل، خدمات Windows وSystemd في Linux                   |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | قالب Zero، قوالب مشاريع NewLife.Templates المبنية على منظومة مكوّنات NewLife، Web وWebApi وService        |
|                              البرمجيات الوسيطة                              |       | تتكامل مع منصات البرمجيات الوسيطة المعروفة                                                                          |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | عميل Redis، زمن استجابة بمستوى الميكروثانية، إنتاجية بمستوى الملايين، قوائم رسائل غنية، ومُثبّت بمشروع بمستوى عشرات المليارات من البيانات                   |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | عميل RocketMQ مُدار بالكامل، يدعم Apache RocketMQ وخدمة رسائل Alibaba Cloud، ومُثبّت بمشروع بمستوى المليارات                     |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | بروتوكول رسائل إنترنت الأشياء، MqttClient/MqttServer، والعميل يدعم Alibaba Cloud IoT                               |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | مكتبة معايير IoT، تعرّف مختلف مواصفات ومعايير بروتوكولات الاتصال في مجال إنترنت الأشياء                                             |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)   | 2022  | ModbusTcp/ModbusRTU/ModbusASCII، مبنية على مكتبة معايير IoT، تدعم منصة ZeroIoT وبوابة IoTEdge            |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)  | 2022  | بروتوكول Siemens PLC، مبني على مكتبة معايير IoT، يدعم منصة IoT وIoTEdge                                      |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)      | 2022  | مكتبة مكوّنات الخرائط، تغلّف خرائط Baidu وGaode وTencent وTianditu                                        |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)    | 2023  | مكتبة ترميز وفك ترميز الصوت، PCM/ADPCMA/G711A/G722U/WAV/AAC                                                |
|                             منصات المنتجات                             |       | بمستوى منصات المنتجات، جاهزة للاستخدام بعد البناء والنشر، مع تخصيص فردي                                                      |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | Stardust، منصة خدمات موزّعة: إدارة العقد، مركز مراقبة APM، مركز الإعدادات، مركز التسجيل، مركز النشر                   |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | AntJob، جدولة موزّعة، منصة حوسبة بيانات ضخمة (فورية/غير متصلة)، بفكرة التقسيم "نقل النمل"، ومُثبّتة بمشروع بمستوى تريليونات البيانات         |
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)      | 2021  | ERP للمؤسسات: إدارة المنتجات، إدارة العملاء، إدارة المبيعات، إدارة الموردين                                           |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | أداة CrazyCoder، أدوات مطوّرين عديدة: الشبكات، المنفذ التسلسلي، التشفير وفك التشفير، التعبيرات النمطية، Modbus، MQTT                      |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)           | 2023  | تخزين ملفات مبسّط، يدعم التخزين المركزي للملفات في الأنظمة الموزّعة.                                                |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | وكيل عكسي بمستوى المنتج، وكيل NAT، وكيل Http                                                           |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)        | 2022  | أداة اختبار ضغط Http                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | نظام إدارة الشيفرة المصدرية Git                                                                           |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | نظام تشغيل مدمج، مستقل تمامًا، يدعم بنية شرائح ARM Cortex-M                                      |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)          | 2019  | حاسوب صناعي مدمج، بوابة حوسبة حافة لإنترنت الأشياء، مضيف .NET8 عالي الأداء، يُستخدم في الصناعة والزراعة والنقل والطب             |
|                          منصة FIoT لإنترنت الأشياء                          | 2020  | حلول إنترنت الأشياء المتكاملة: البناء وحماية البيئة والزراعة، تكامل البرمجيات والعتاد وتحليل البيانات الضخمة، ومُثبّتة بمشروع بمستوى مئة ألف نقطة على جهاز واحد      |


---

## فريق تطوير NewLife

![XCode](https://newlifex.com/logo.png)

تأسّس فريق NewLife في عام 2002، وهو مزوّد حلول لقطاع إنترنت الأشياء في العصر الجديد، ويسعى إلى تقديم استشارات حلول البرمجيات والعتاد وتخطيط بنية الأنظمة وخدمات التطوير. وبلغت التنزيلات التراكمية من NuGet أكثر من 5.23 مليون. وقد طُبّقت المكوّنات الأساسية للفريق (XCode / Redis / Stardust / AntJob / IoT وغيرها) على نطاق واسع في قطاعات الكهرباء والصناعة والنقل وحماية البيئة والطب والمتاحف.

نواصل التحسين، سعيًا لأن نكون شريكًا تقنيًا جديرًا بالثقة على المدى الطويل في .NET / IoT / الأنظمة الموزّعة.

الموقع: https://newlifex.com   المنظمة مفتوحة المصدر: https://github.com/NewLifeX  
حساب WeChat الرسمي:  
![智能大石头](https://newlifex.com/stone.jpg)

## المساهمة والملاحظات

- قبل الإرسال تأكد من نجاح `dotnet build` واختبارات الوحدة ذات الصلة؛ تتبع أساليب الشيفرة ورسائل الالتزام اتفاقيات المستودع.
- أبلغ عن الأخطاء والاقتراحات عبر [Issue](https://github.com/NewLifeX/X/issues)؛ نرحّب بـ Stars وPRs.
- راجع [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md) لملاحظات الإصدارات.

## الترخيص

هذا المشروع مرخّص بموجب [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE)؛ شهادة الاسم القوي `newlife.snk` منشورة لتتمكن من البناء واستبدال التجميعات بنفسك.

> إذا كان هذا المشروع مفيدًا لك، فنرحّب بـ Star أو مشاركته مع فريقك، أو إرسال اقتراحات التحسين عبر Issue/PR.
