using Stardust.Models;

namespace Mobile.Stardust;

/// <summary>移动平台适配接口。宿主应用实现设备信息采集、存储路径与进程重启</summary>
public interface IMobilePlatform
{
    /// <summary>平台名称。Android/iOS</summary>
    String Name { get; }

    /// <summary>设备唯一标识。同一设备重装应用后应尽量保持不变</summary>
    String DeviceId { get; }

    /// <summary>设备名称。如手机型号</summary>
    String DeviceName { get; }

    /// <summary>操作系统名称</summary>
    String OSName { get; }

    /// <summary>操作系统版本</summary>
    String OSVersion { get; }

    /// <summary>处理器架构。如 arm64-v8a、arm64</summary>
    String Architecture { get; }

    /// <summary>获取应用私有数据目录，用于保存配置与日志</summary>
    /// <returns>可写目录</returns>
    String GetDataPath();

    /// <summary>填充节点信息。登录时上报的静态信息</summary>
    /// <param name="info">节点信息</param>
    void FillNodeInfo(NodeInfo info);

    /// <summary>填充心跳信息。心跳时上报的动态信息</summary>
    /// <param name="info">心跳信息</param>
    void FillPingInfo(PingInfo info);

    /// <summary>重启当前应用</summary>
    void RestartApp();
}