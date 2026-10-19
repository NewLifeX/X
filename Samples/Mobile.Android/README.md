# 新生命移动测试台（Mobile）

一个示例，四个项目：系统信息 + 21 个兼容性冒烟用例 + XTrace 日志。Android/iOS 桌面显示名为“新生命测试台”，桌面图标使用新生命四叶草，首页标题为“新生命移动测试台”。所有项目输出统一到仓库根目录 `Bin\Mobile.*`。

| 项目 | 用途 |
|---|---|
| `Mobile.Core` | `MachineInfoReport`（系统信息）与 21 个冒烟用例、`SmokeRunner` |
| `Mobile.Stardust.Core` | 可选星尘接入：`NewLife.Stardust` + `MobileAgent`，设备信息复用 `MachineInfo` |
| `Mobile.Console` | 桌面宿主：默认系统信息，`--smoke` / `--all` 跑冒烟 |
| `Mobile.Android` | 原生 Activity 控制台：系统信息 / 运行冒烟 / 连接星尘（可选） / 复制 / 清空 |
| `Mobile.iOS` | 原生 UIViewController：系统信息 / 运行冒烟 / 连接星尘（可选），CI 产出 `smoke.json` |

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
| `Aot` | Mono AOT（normal + profiled） | 12.47 MB | 默认交付；启动更快，Release 默认开启 |
| `NativeAot` | NativeAOT | 15.40 MB | 已实测可构建（实验性）；需 Android NDK 26.3，`pack-android.ps1` 自动定位 clang/LLVM；API 36 x86_64 模拟器冒烟 21/21 通过，未在真机验证 |

> NativeAOT 模式需要 Android NDK：先执行 `sdkmanager "ndk;26.3.11579264"`；`pack-android.ps1` 会自动定位 NDK 并把 clang/LLVM 加入 PATH。

- APK 同时包含 `arm64-v8a` 和 `x86_64`，可安装到真机和模拟器。
- `-Signed.apk` 使用本机 debug keystore 签名，可拷贝到其他手机侧载安装；正式分发需配置 release keystore。
- 复制安装：

  ```powershell
  adb install -r Bin\Mobile.Android\apk\Mobile-Aot.apk
  ```

## 星尘接入（可选）

默认构建**不引用** `NewLife.Stardust`，保持离线冒烟与全裁剪/AOT 门禁。需要把设备信息上报到星尘平台时，用 `-Stardust` 构建：

```powershell
# 构建、安装并跑冒烟（同时自动连接星尘）
pwsh Samples/Mobile.Android/run-android.ps1 -Mode Aot -Smoke -Stardust

# 只构建、安装并连接星尘
pwsh Samples/Mobile.Android/run-android.ps1 -Mode Aot -Stardust
```

- 服务端默认 `http://star.newlifex.com:6600`，产品编码 `StarAgent-Mobile`；
- 首次登录由服务端分配节点编码与密钥，保存在应用私有目录的 `staragent.json`；
- 设备信息优先读取厂商市场名称（如 `HONOR 20 PRO`）、系统设备名称（如 `FeiFan`）和 SoC 友好名称（如 `Huawei Kirin 980`）；
- Android 10+ 普通应用无法读取 IMEI/MEID，序列号也通常不可读；此时硬件唯一标识继续使用 `android_id`；
- 服务端密钥校验失败时，客户端会自动清空本地密钥并重试一次，触发服务端动态注册；日志会输出 `Secret=已保存/空` 便于排查；
- 星尘构建使用 `TrimMode=partial` 裁剪并保持 AOT，避免 Android 10 真机 JIT 冷启动时 `n_onCreate` 未注册崩溃；默认冒烟构建不受影响；
- iOS：`INCLUDE_STARDUST=true bash Samples/Mobile.iOS/run-ios.sh`，`Info.plist` 已为 `star.newlifex.com` 添加 HTTP 例外。

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
