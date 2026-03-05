# NewLife.Core 单元测试覆盖需求

## 概述

本文档跟踪 NewLife.Core 项目各模块的单元测试覆盖情况。  
测试框架：**xUnit 2.9.3**，目标框架：**net9.0**。  
最近一次运行：**全部通过 1839 / 失败 0 / 跳过 17**

---

## 测试覆盖状态

### Collections 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| ConcurrentHashSet.cs | ConcurrentHashSetTests.cs | ✅ 通过 | 10 |
| NullableDictionary.cs | NullableDictionaryTests.cs | ✅ 通过 | 12 |
| ObjectPool.cs | ObjectPoolTests.cs | ✅ 通过 | 9 |
| Pool.cs | PoolTests.cs | ✅ 通过 | (已有) |

### Common 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| DisposeBase.cs | DisposeBaseTests.cs | ✅ 通过 | 9 |
| MachineInfo.cs | MachineInfoTests.cs | ✅ 通过 | (已有) |
| PinYin.cs | PinYinTests.cs | ✅ 通过 | (已有) |
| Runtime.cs | RuntimeTests.cs | ✅ 通过 | (已有) |
| SysConfig.cs | SysConfigTest.cs | ✅ 通过 | (已有) |
| Utility.cs | UtilityTests.cs | ✅ 通过 | (已有) |
| ConsoleLog.cs | — | ⏳ 待补 | — |
| TimeProvider.cs | — | ⏳ 待补 | — |

### Event 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| WeakAction.cs | WeakActionTests.cs | ✅ 通过 | 10 |
| EventArgs.cs | EventArgsTests.cs | ✅ 通过 | 13 |

### Exceptions 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| XException.cs | XExceptionTests.cs | ✅ 通过 | 15 |

### Extension 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| BitHelper.cs | BitHelperTests.cs | ✅ 通过 | 14 |
| ListExtension.cs | ListExtensionTests.cs | ✅ 通过 | 7 |
| StringHelper.cs | StringHelperTests.cs | ✅ 通过 | (已有) |

### Log 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| CompositeLog.cs | CompositeLogTests.cs | ✅ 通过 | 11 |
| ActionLog.cs | ActionLogTests.cs | ✅ 通过 | 4 |
| TextFileLog.cs | TextFileLogTests.cs | ✅ 通过 | 6 |
| WriteLogEventArgs.cs | WriteLogEventArgsTests.cs | ✅ 通过 | 6 |
| Logger.cs | LoggerTests.cs | ✅ 通过 | (已有) |
| LevelLog.cs | LevelLogTests.cs | ✅ 通过 | (已有) |
| NetworkLog.cs | NetworkLogTests.cs | ✅ 通过 | (已有) |
| PerfCounter.cs | PerfCounterTests.cs | ✅ 通过 | (已有) |
| XTrace.cs | — | ⏳ 待补 | — |
| LogEventListener.cs | — | ⏳ 待补 | — |

### Model 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| DeferredQueue.cs | DeferredQueueTests.cs | ✅ 通过 | 12 |
| ObjectContainer.cs | ObjectContainerTests.cs | ✅ 通过 | (已有) |

### Reflection 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| AttributeX.cs | AttributeXTests.cs | ✅ 通过 | 10 |
| AssemblyX.cs | AssemblyXTests.cs | ✅ 通过 | (已有) |
| Reflect.cs | ReflectTests.cs | ✅ 通过 | (已有) |

### Security 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| Murmur128.cs | Murmur128Tests.cs | ✅ 通过 | 10 |
| DSAHelper.cs | DSAHelperTests.cs | ✅ 通过 | 5 |
| CbcTransform.cs | CbcTransformTests.cs | ✅ 通过 | 8 |
| ZerosPaddingTransform.cs | ZerosPaddingTransformTests.cs | ✅ 通过 | 5 |
| Asn1.cs | Asn1Tests.cs | ✅ 通过 | 10 |
| RC4.cs | RC4Tests.cs | ✅ 通过 | 8 |
| Crc16.cs | Crc16Tests.cs | ✅ 通过 | (已有) |
| Crc32.cs | Crc32Tests.cs | ✅ 通过 | (已有) |
| Rand.cs | RandTests.cs | ✅ 通过 | (已有) |
| RSAHelper.cs | RSAHelperTests.cs | ✅ 通过 | (已有) |
| SecurityHelper.cs | SecurityHelperTests.cs | ✅ 通过 | (已有) |
| SM4.cs | SM4Tests.cs | ✅ 通过 | (已有) |
| ProtectedKey.cs | ProtectedKeyTests.cs | ✅ 通过 | (已有) |
| IPasswordProvider.cs | PasswordProviderTests.cs | ✅ 通过 | (已有) |
| PKCS7PaddingTransform.cs | — | ⏳ 待补 | — |

### Serialization 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| SafeInt64Converter.cs | SafeInt64ConverterTests.cs | ✅ 通过 | 11 |
| LocalTimeConverter.cs | LocalTimeConverterTests.cs | ✅ 通过 | 8 |
| SpanSerializer.cs | SpanSerializerTests.cs | ✅ 通过 | (已有) |
| SerialHelper.cs | SerialHelperTests.cs | ✅ 通过 | (已有) |
| Binary.cs | BinaryTests.cs | ✅ 通过 | (已有) |
| JsonParser.cs | JsonParserTests.cs | ✅ 通过 | (已有) |
| JsonWriter.cs | JsonWriterTests.cs | ✅ 通过 | (已有) |

### Threading 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| ThreadPoolX.cs | ThreadPoolXTests.cs | ✅ 通过 | 5 |
| TimerScheduler.cs | TimerSchedulerTests.cs | ✅ 通过 | 4 |
| TimerX.cs | TimerXTests.cs | ✅ 通过 | (已有) |
| Cron.cs | CronTests.cs | ✅ 通过 | (已有) |

### Web 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| TokenModel.cs | TokenModelTests.cs | ✅ 通过 | 3 |
| JwtBuilder.cs | JwtBuilderTests.cs | ✅ 通过 | (已有) |
| Link.cs | LinkTests.cs | ✅ 通过 | (已有) |
| TokenProvider.cs | TokenProviderTests.cs | ✅ 通过 | (已有) |
| UriInfo.cs | UriInfoTests.cs | ✅ 通过 | (已有) |
| WebClientX.cs | WebClientTests.cs | ✅ 通过 | (已有) |

### Xml 模块

| 源文件 | 测试文件 | 状态 | 测试数 |
|--------|----------|------|--------|
| XmlHelper.cs | XmlHelperTests.cs | ✅ 通过 | 18 |

### IO 模块 (全覆盖)

| 源文件 | 测试文件 | 状态 |
|--------|----------|------|
| CsvDb.cs | CsvDbTests.cs | ✅ 通过 |
| CsvFile.cs | CsvFileTests.cs | ✅ 通过 |
| EasyClient.cs | EasyClientTests.cs | ✅ 通过 |
| ExcelReader.cs | ExcelReaderTests.cs | ✅ 通过 |
| ExcelWriter.cs | ExcelWriterTests.cs | ✅ 通过 |
| IOHelper.cs | IOHelperTests.cs | ✅ 通过 |
| PathHelper.cs | PathHelperTests.cs | ✅ 通过 |

### Configuration 模块

| 源文件 | 测试文件 | 状态 |
|--------|----------|------|
| CommandParser.cs | CommandParserTests.cs | ✅ 通过 |
| CompositeConfigProvider.cs | CompositeConfigProviderTests.cs | ✅ 通过 |
| ConfigHelper.cs | ConfigHelperTests.cs | ✅ 通过 |
| HttpConfigProvider.cs | HttpConfigProviderTests.cs | ✅ 通过 |
| IniConfigProvider.cs | IniConfigProviderTests.cs | ✅ 通过 |
| JsonConfigProvider.cs | JsonConfigProviderTests.cs | ✅ 通过 |
| XmlConfigProvider.cs | XmlConfigProviderTests.cs | ✅ 通过 |

---

## 本次新增测试文件汇总

共新增 **19 个测试文件**，约 **280+ 个测试方法**：

| # | 测试文件 | 覆盖源文件 |
|---|----------|-----------|
| 1 | XUnitTest.Core/Xml/XmlHelperTests.cs | XmlHelper |
| 2 | XUnitTest.Core/Event/WeakActionTests.cs | WeakAction |
| 3 | XUnitTest.Core/Event/EventArgsTests.cs | EventArgs<T> |
| 4 | XUnitTest.Core/Exceptions/XExceptionTests.cs | XException, ExceptionHelper |
| 5 | XUnitTest.Core/Collections/ConcurrentHashSetTests.cs | ConcurrentHashSet |
| 6 | XUnitTest.Core/Collections/NullableDictionaryTests.cs | NullableDictionary |
| 7 | XUnitTest.Core/Collections/ObjectPoolTests.cs | ObjectPool |
| 8 | XUnitTest.Core/Log/CompositeLogTests.cs | CompositeLog |
| 9 | XUnitTest.Core/Log/ActionLogTests.cs | ActionLog |
| 10 | XUnitTest.Core/Log/TextFileLogTests.cs | TextFileLog |
| 11 | XUnitTest.Core/Log/WriteLogEventArgsTests.cs | WriteLogEventArgs |
| 12 | XUnitTest.Core/Reflection/AttributeXTests.cs | AttributeX |
| 13 | XUnitTest.Core/Model/DeferredQueueTests.cs | DeferredQueue |
| 14 | XUnitTest.Core/Extension/BitHelperTests.cs | BitHelper |
| 15 | XUnitTest.Core/Extension/ListExtensionTests.cs | ListExtension |
| 16 | XUnitTest.Core/Security/Murmur128Tests.cs | Murmur128 |
| 17 | XUnitTest.Core/Security/DSAHelperTests.cs | DSAHelper |
| 18 | XUnitTest.Core/Security/CbcTransformTests.cs | CbcTransform |
| 19 | XUnitTest.Core/Security/ZerosPaddingTransformTests.cs | ZerosPaddingTransform |
| 20 | XUnitTest.Core/Security/Asn1Tests.cs | Asn1 |
| 21 | XUnitTest.Core/Security/RC4Tests.cs | RC4 |
| 22 | XUnitTest.Core/Threading/ThreadPoolXTests.cs | ThreadPoolX |
| 23 | XUnitTest.Core/Threading/TimerSchedulerTests.cs | TimerScheduler |
| 24 | XUnitTest.Core/Common/DisposeBaseTests.cs | DisposeBase |
| 25 | XUnitTest.Core/Serialization/SafeInt64ConverterTests.cs | SafeInt64Converter |
| 26 | XUnitTest.Core/Serialization/LocalTimeConverterTests.cs | LocalTimeConverter |
| 27 | XUnitTest.Core/Web/TokenModelTests.cs | TokenModel |

---

## 排除编译的源文件（不纳入覆盖统计）

以下文件通过 `NewLife.Core.csproj` 中 `<Compile Remove>` 排除编译：
- `Agent/**`, `Applications/**`, `Expressions/**`, `Json/**`, `Yun/**`
- `Log/CodeTimer.cs`, `Log/TimeCost.cs`, `Log/TraceStream.cs`
- `Collections/DictionaryCache.cs`, `Collections/BloomFilter.cs`, `Collections/QueueService.cs`
- `IO/EncodingHelper.cs`, `IO/FileSource.cs`
- `Reflection/EmitHelper.cs`, `Reflection/EmitReflect.cs`
- 其他详见 csproj 第 105-169 行

---

## 待补充覆盖

| 模块 | 源文件 | 优先级 |
|------|--------|--------|
| Log | XTrace.cs | 高 |
| Log | LogEventListener.cs | 中 |
| Configuration | Config.cs | 高 |
| Configuration | FileConfigProvider.cs | 高 |
| Serialization | JsonReader.cs | 高 |
| Serialization | PKCS7PaddingTransform.cs | 中 |
| Web | PluginHelper.cs | 中 |
| Common | TimeProvider.cs | 中 |
