using System.Runtime.InteropServices;

namespace NewLife.Windows;

/// <summary>系统电源状态</summary>
public enum PowerLineStatus
{
    /// <summary>脱机状态</summary>
    Offline = 0,
    /// <summary>联机状态</summary>
    Online = 1,
    /// <summary>电源状态未知</summary>
    Unknown = 255
}

/// <summary>充电状态信息</summary>
[Flags]
public enum BatteryChargeStatus
{
    /// <summary>指示电池能量级别较高</summary>
    High = 1,
    /// <summary>指示电池能量级别较低</summary>
    Low = 2,
    /// <summary>指示电池能量严重不足</summary>
    Critical = 4,
    /// <summary>指示电池正在充电</summary>
    Charging = 8,
    /// <summary>指示没有电池存在</summary>
    NoSystemBattery = 0x80,
    /// <summary>指示未知电池状态</summary>
    Unknown = 0xFF
}

/// <summary>电源状态</summary>
public class PowerStatus
{
    private SYSTEM_POWER_STATUS systemPowerStatus;

    /// <summary>当前的系统电源状态</summary>
    public PowerLineStatus PowerLineStatus
    {
        get
        {
            UpdateSystemPowerStatus();
            return (PowerLineStatus)systemPowerStatus.ACLineStatus;
        }
    }

    /// <summary>当前的电池电量状态</summary>
    public BatteryChargeStatus BatteryChargeStatus
    {
        get
        {
            UpdateSystemPowerStatus();
            return (BatteryChargeStatus)systemPowerStatus.BatteryFlag;
        }
    }

    /// <summary>报告的主电池电源的完全充电寿命（以秒为单位）</summary>
    public Int32 BatteryFullLifetime
    {
        get
        {
            UpdateSystemPowerStatus();
            return systemPowerStatus.BatteryFullLifeTime;
        }
    }

    /// <summary>电池剩余电量的近似量</summary>
    public Single BatteryLifePercent
    {
        get
        {
            UpdateSystemPowerStatus();
            var num = systemPowerStatus.BatteryLifePercent / 100f;
            return num > 1f ? 1f : num;
        }
    }

    /// <summary>电池的剩余使用时间的近似秒数</summary>
    public Int32 BatteryLifeRemaining
    {
        get
        {
            UpdateSystemPowerStatus();
            return systemPowerStatus.BatteryLifeTime;
        }
    }

    private void UpdateSystemPowerStatus() => GetSystemPowerStatus(ref systemPowerStatus);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    static extern Boolean GetSystemPowerStatus([In][Out] ref SYSTEM_POWER_STATUS systemPowerStatus);

    struct SYSTEM_POWER_STATUS
    {
        public Byte ACLineStatus;

        public Byte BatteryFlag;

        public Byte BatteryLifePercent;

        public Byte Reserved1;

        public Int32 BatteryLifeTime;

        public Int32 BatteryFullLifeTime;
    }
}