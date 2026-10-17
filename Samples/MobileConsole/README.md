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

桌面宿主与 Android 宿主共用 `MobileConsole.Core` 的采集逻辑，可先在桌面上确认输出格式。Android Release 启用 `TrimMode=full`，与 MobileSmoke 的移动端门禁一致。

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

`MachineInfo` 的 Android 分支已补齐系统、硬件、内存与 `android_id`；电池/温度由本示例的 `IMachineInfo` Provider 读取 Android 原生信息填充。iOS 仍未补齐（见 `Doc/功能清单.md` 的 IMP-25）。

日志同时写入 UI 文本控件、logcat 和应用外部文件目录：

- `report.txt`：完整文本报告；
- `YYYY_MM_DD.log`：`XTrace` 文本日志（含时间、线程等字段）。

> `adb pull /storage/emulated/0/Android/data/com.newlife.mobileconsole/files/Log/report.txt`
