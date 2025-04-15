namespace NewLife.Log;

/// <summary>写日志接口。支持写应用功能日志</summary>
public interface ILogProvider
{
    /// <summary>写日志</summary>
    /// <param name="action">动作</param>
    /// <param name="success">是否成功</param>
    /// <param name="content">内容</param>
    void WriteLog(String action, Boolean success, String content);
}

/// <summary>日志功能接口</summary>
public interface ILogFeature
{
    /// <summary>日志。非空，默认为Logger.Null</summary>
    ILog Log { get; set; }
}

/// <summary>日志功能扩展</summary>
public static class LogFeatureExtensions
{
    /// <summary>写日志</summary>
    /// <param name="logFeature">日志功能</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">格式化参数，特殊处理时间日期和异常对象</param>
    public static void WriteLog(this ILogFeature logFeature, String format, params Object?[] args) => logFeature.Log?.Info(format, args);
}