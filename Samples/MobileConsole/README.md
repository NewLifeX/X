# MobileConsole - Android 上的机器信息“控制台”

Android 没有 Windows 那样的控制台窗口。这个示例把整个 Activity 当成一块控制台：深色背景、等宽字体、可滚动、可长按选择与复制，同时把同样的内容写入 logcat，方便用 `adb logcat` 或脚本抓取。

## 显示内容

- `MachineInfo`：系统、处理器、内存、唯一标识、CPU/网络速度、温度、电池；
- 运行时：框架、RID、进程架构、Mono、进程编号、运行时长；
- 环境与路径：处理器数量、CLR 版本、托管内存、工作集、应用数据目录；
- Android 原生信息：厂商、型号、Android 版本、ABI、屏幕、总内存/可用内存、电池、`android_id`。

## 桌面基线

```powershell
dotnet run --project Samples/MobileConsole/MobileConsole.Console -c Release
```

桌面宿主与 Android 宿主共用 `MobileConsole.Core` 的采集逻辑，可先在桌面上确认输出格式。

## Android 真机

1. 若本机还没有 Android 工具链，先执行：

   ```powershell
   pwsh Samples/MobileSmoke/setup-android.ps1
   ```

2. 手机开启开发者选项与 USB 调试，确认设备在线：

   ```powershell
   adb devices
   ```

3. 构建、安装、启动并抓取输出：

   ```powershell
   pwsh Samples/MobileConsole/run-android.ps1 -Configuration Release
   ```

   多台设备时用 `-Device <serial>` 指定；已安装 APK 时可加 `-SkipBuild`。

4. 结果同时在屏幕上显示，并写入 logcat（标签 `MobileConsole`）：

   ```powershell
   adb logcat -s MobileConsole:I
   ```

   脚本会把日志保存到 `Samples/MobileConsole/artifacts/android-*.log`。

## 已知限制

`MachineInfo` 在 Android/iOS 上的平台分支尚不完整（见 `Doc/功能清单.md` 的 IMP-25），`Memory` 等字段可能为 0。本示例额外读取 Android 原生信息补足显示，但不改变 `MachineInfo` 的现有行为。
