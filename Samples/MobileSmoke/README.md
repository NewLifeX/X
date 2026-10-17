# MobileSmoke - NewLife.Core 移动端冒烟

共享冒烟库 + 控制台/Android/iOS 宿主。所有用例只访问 `127.0.0.1` 回环，不依赖外部网络。

- `MobileSmoke.Core`：20 个用例，覆盖平台信息、日志、配置、缓存、定时器、序列化、TCP/HTTP/WebSocket、安全、归档、对象容器、动态能力降级。
- `MobileSmoke.Console`：桌面快速基线。
- `MobileSmoke.Android`：原生 Activity 壳，logcat 标签 `MobileSmoke`。
- `MobileSmoke.iOS`：原生 UIViewController 壳，结果写入应用数据目录。

Release 构建启用 `TrimMode=full`；iOS Release 走 AOT。Debug 只用于快速定位。

## 控制台

```powershell
dotnet run --project Samples/MobileSmoke/MobileSmoke.Console -c Release
```

## Android 真机

1. 安装 JDK 与 Android SDK：

   ```powershell
   pwsh Samples/MobileSmoke/setup-android.ps1
   ```

2. 手机开启开发者选项与 USB 调试，确认设备在线：

   ```powershell
   adb devices
   ```

3. 构建、安装并运行：

   ```powershell
   pwsh Samples/MobileSmoke/run-android.ps1 -Configuration Release
   ```

   多台设备时用 `-Device <serial>` 指定；已安装 APK 时可加 `-SkipBuild`。

Android 11+ 也可用无线调试：`adb pair <ip:port>`，再 `adb connect <ip:port>`。

## Android 模拟器

```powershell
pwsh Samples/MobileSmoke/setup-android.ps1 -WithEmulator -StartEmulator
pwsh Samples/MobileSmoke/run-android.ps1 -Configuration Release
```

模拟器使用 API 36、x86_64、WHPX。若本机未启用 Windows Hypervisor Platform，先在“启用或关闭 Windows 功能”中开启并重启。模拟器与系统镜像约需 2–3 GB 磁盘空间；空间不足时先用真机，CI 会在 GitHub runner 上安装。

## iOS

Windows 只能编译 iOS 类库，不能运行模拟器/真机。iOS 冒烟通过 GitHub Actions 的 `macos-26` 运行：

```bash
bash Samples/MobileSmoke/run-ios.sh
```

脚本会构建 `iossimulator-arm64`、启动 iPhone 模拟器、安装并启动 App，然后轮询应用数据目录中的 `smoke.json`。

## CI

`.github/workflows/mobile.yml` 包含：

- Android：`ubuntu-latest` + KVM + `reactivecircus/android-emulator-runner`（API 36）。
- iOS：`macos-26` + iOS 模拟器（无需 Apple 账号）。

触发方式：手动 `workflow_dispatch`、涉及 `Samples/MobileSmoke/**` / `NewLife.Core/**` 的 PR、每周定时。

## 结果

- Android：`adb logcat -s MobileSmoke:I`，脚本同时保存到 `Samples/MobileSmoke/artifacts/android-*.log`。
- iOS：应用数据目录下的 `smoke.json`，脚本复制到 `artifacts/ios-smoke.json`。
- 文本报告：`LocalApplicationData/MobileSmoke/smoke.txt`。

门禁：`SUMMARY total=... pass=... fail=0 skip=...`。

## 已知限制

- `MachineInfo.Memory` 在 Android/iOS 上可能为 0（当前只实现 Windows/Linux/macOS 分支），用例只记录不判失败。
- `ProcessHelper` 在受限平台可能抛 `PlatformNotSupportedException`，用例标记 SKIP。
- `PluginHelper` 动态加载在 AOT 下按设计不支持，用例验证其抛出明确异常。
- `WebSocket` / HTTP 用例只使用回环地址；Android 已开启 `usesCleartextTraffic`，iOS 已开启 `NSAllowsLocalNetworking`。

