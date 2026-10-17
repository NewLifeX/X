# 新生命移动测试台（Mobile）

一个示例，四个项目：系统信息 + 21 个兼容性冒烟用例 + XTrace 日志。Android/iOS 桌面显示名为“新生命测试台”，首页标题为“新生命移动测试台”。所有项目输出统一到仓库根目录 `Bin\Mobile.*`。

| 项目 | 用途 |
|---|---|
| `Mobile.Core` | `MachineInfoReport`（系统信息）与 21 个冒烟用例、`SmokeRunner` |
| `Mobile.Console` | 桌面宿主：默认系统信息，`--smoke` / `--all` 跑冒烟 |
| `Mobile.Android` | 原生 Activity 控制台：系统信息 / 运行冒烟 / 复制 / 清空 |
| `Mobile.iOS` | 原生 UIViewController：系统信息 / 运行冒烟，CI 产出 `smoke.json` |

> 仓库根目录的 `AotSmoke` 是 Windows/Linux 的 NativeAOT 冒烟，不属于移动端示例。

## 桌面

```powershell
dotnet run --project Samples/Mobile.Console -c Release
dotnet run --project Samples/Mobile.Console -c Release -- --smoke
dotnet run --project Samples/Mobile.Console -c Release -- --all
```

输出目录：`Bin\Mobile.Console`。

## Android

1. 安装工具链（首次）：

   ```powershell
   pwsh Samples/Mobile.Android/setup-android.ps1
   ```

2. 连接手机并确认设备在线：

   ```powershell
   adb devices
   ```

3. 构建、安装、启动并抓取系统信息：

   ```powershell
   pwsh Samples/Mobile.Android/run-android.ps1
   ```

4. 跑冒烟测试（自动触发，不依赖 UI 点击）：

   ```powershell
   pwsh Samples/Mobile.Android/run-android.ps1 -Smoke
   ```

   多台设备用 `-Device <serial>`；已有 APK 时可加 `-SkipBuild`。

日志与报告：

- logcat 标签：`Mobile`
- 屏幕：可滚动、可长按选择、可复制
- 应用外部目录：

  ```text
  /storage/emulated/0/Android/data/com.newlife.mobile/files/Log/report.txt
  /storage/emulated/0/Android/data/com.newlife.mobile/files/Log/smoke.txt
  /storage/emulated/0/Android/data/com.newlife.mobile/files/Log/YYYY_MM_DD.log
  ```

- 脚本日志：`Bin\Mobile.Android\artifacts\android-*.log`

## 打包 APK

```powershell
pwsh Samples/Mobile.Android/pack-android.ps1 -Mode Aot
pwsh Samples/Mobile.Android/pack-android.ps1 -Mode Jit
pwsh Samples/Mobile.Android/pack-android.ps1 -Mode NativeAot   # 实验性
```

产物统一在：

```text
Bin\Mobile.Android\apk\Mobile-Aot.apk
Bin\Mobile.Android\apk\Mobile-Jit.apk
Bin\Mobile.Android\apk\Mobile-NativeAot.apk
```

| 模式 | 运行时 | 实测大小 | 说明 |
|---|---|---:|---|
| `Jit` | Mono JIT | 9.40 MB | 构建最快，调试最方便；启动/JIT 开销较大 |
| `Aot` | Mono AOT（normal + profiled） | 12.10 MB | 默认交付；启动更快，Release 默认开启 |
| `NativeAot` | NativeAOT | 未完成 | Android 上实验性；需要 clang/LLVM/Android NDK，反射与动态能力受限 |

- APK 同时包含 `arm64-v8a` 和 `x86_64`，可安装到真机和模拟器。
- `-Signed.apk` 使用本机 debug keystore 签名，可拷贝到其他手机侧载安装；正式分发需配置 release keystore。
- 复制安装：

  ```powershell
  adb install -r Bin\Mobile.Android\apk\Mobile-Aot.apk
  ```

## iOS

Windows 只能编译 iOS 类库/应用，不能运行模拟器。iOS 冒烟通过 macOS 或 GitHub Actions 的 `macos-26` 执行：

```bash
bash Samples/Mobile.iOS/run-ios.sh
```

脚本构建 `iossimulator-arm64`、启动模拟器、安装 `com.newlife.mobile`、以 `--smoke` 启动并轮询应用数据目录中的 `smoke.json`。产物复制到 `Bin/Mobile.iOS/artifacts/ios-smoke.json`。

## CI

`.github/workflows/mobile.yml`：

- Android：`ubuntu-latest` + KVM + `reactivecircus/android-emulator-runner`（API 36），运行 `run-android.ps1 -Smoke`。
- iOS：`macos-26` + iOS 模拟器（无需 Apple 账号），运行 `run-ios.sh`。
- 上传 `Bin/Mobile.Android/**` 与 `Bin/Mobile.iOS/**`。
