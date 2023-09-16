using System.Diagnostics.CodeAnalysis;

namespace NewLife.Threading;

/// <summary>轻量级Cron表达式</summary>
/// <remarks>
/// 基本构成：秒+分+时+天+月+星期
/// 每段构成：
///     * 所有可能的值，该类型片段全部可选
///     , 列出枚举值
///     - 范围，横杠表示的一个区间可选
///     / 指定数值的增量，在上述可选数字内，间隔多少选一个
///     ? 不指定值，仅日期和星期域支持该字符
///     # 确定每个月第几个星期几，仅星期域支持该字符
///     数字，具体某个数值可选
///     逗号多选，逗号分隔的多个数字或区间可选
/// </remarks>
/// <example>
/// */2 每两秒一次
/// 0,1,2 * * * * 每分钟的0秒1秒2秒各一次
/// 5/20 * * * * 每分钟的5秒25秒45秒各一次
/// * 1-10,13,25/3 * * * 每小时的1分4分7分10分13分25分，每一秒各一次
/// 0 0 0 1 * * 每个月1日的0点整
/// 0 0 2 * * 1-5 每个工作日的凌晨2点
/// 
/// 星期部分采用Linux和.NET风格，0表示周日，1表示周一。
/// 可设置Sunday为1，1表示周日，2表示周一。
/// 
/// 参考文档 https://help.aliyun.com/document_detail/64769.html
/// </example>
public class Cron
{
    #region 属性
    /// <summary>秒数集合</summary>
    public Int32[]? Seconds { get; set; }

    /// <summary>分钟集合</summary>
    public Int32[]? Minutes { get; set; }

    /// <summary>小时集合</summary>
    public Int32[]? Hours { get; set; }

    /// <summary>日期集合</summary>
    public Int32[]? DaysOfMonth { get; set; }

    /// <summary>月份集合</summary>
    public Int32[]? Months { get; set; }

    /// <summary>星期集合。key是星期数，value是第几个，负数表示倒数</summary>
    public IDictionary<Int32, Int32>? DaysOfWeek { get; set; }

    /// <summary>星期天偏移量。周日对应的数字，默认0。1表示周日时，2表示周一</summary>
    public Int32 Sunday { get; set; }

    private String? _expression;
    #endregion

    #region 构造
    /// <summary>实例化Cron表达式</summary>
    public Cron() { }

    /// <summary>实例化Cron表达式</summary>
    /// <param name="expression"></param>
    public Cron(String expression) => Parse(expression);

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => _expression ?? nameof(Cron);
    #endregion

    #region 方法
    /// <summary>指定时间是否位于表达式之内</summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public Boolean IsTime(DateTime time)
    {
        if (Seconds == null || Minutes == null || Hours == null || DaysOfMonth == null || Months == null) return false;

        // 基础时间判断
        if (!Seconds.Contains(time.Second) ||
            !Minutes.Contains(time.Minute) ||
            !Hours.Contains(time.Hour) ||
            !DaysOfMonth.Contains(time.Day) ||
            !Months.Contains(time.Month)
            ) return false;

        var w = (Int32)time.DayOfWeek + Sunday;
        if (DaysOfWeek == null || !DaysOfWeek.TryGetValue(w, out var index)) return false;

        // 第几个星期几判断
        if (index > 0)
        {
            var start = new DateTime(time.Year, time.Month, 1);
            for (var dt = start; dt <= time.Date; dt = dt.AddDays(1))
            {
                if (dt.DayOfWeek == time.DayOfWeek) index--;
            }
            if (index != 0) return false;
        }
        else if (index < 0)
        {
            var start = new DateTime(time.Year, time.Month, 1);
            for (var dt = start.AddMonths(1).AddDays(-1); dt >= time.Date; dt = dt.AddDays(-1))
            {
                if (dt.DayOfWeek == time.DayOfWeek) index++;
            }
            if (index != 0) return false;
        }

        return true;
    }

    /// <summary>分析表达式</summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public Boolean Parse(String expression)
    {
        var ss = expression.Split(' ');
        if (ss.Length == 0) return false;

        if (!TryParse(ss[0], 0, 60, out var vs)) return false;
        Seconds = vs;
        if (!TryParse(ss.Length > 1 ? ss[1] : "*", 0, 60, out vs)) return false;
        Minutes = vs;
        if (!TryParse(ss.Length > 2 ? ss[2] : "*", 0, 24, out vs)) return false;
        Hours = vs;
        if (!TryParse(ss.Length > 3 ? ss[3] : "*", 1, 32, out vs)) return false;
        DaysOfMonth = vs;
        if (!TryParse(ss.Length > 4 ? ss[4] : "*", 1, 13, out vs)) return false;
        Months = vs;

        var dic = new Dictionary<Int32, Int32>();
        if (!TryParseWeek(ss.Length > 5 ? ss[5] : "*", 0, 7, dic)) return false;
        DaysOfWeek = dic;

        _expression = expression;

        return true;
    }

    private static Boolean TryParse(String value, Int32 start, Int32 max, out Int32[] vs)
    {
        // 固定值，最为常见，优先计算
        if (Int32.TryParse(value, out var n))
        {
            vs = new Int32[] { n };
            return true;
        }

        var rs = new List<Int32>();
        vs = new Int32[0];

        // 递归处理混合值
        if (value.Contains(','))
        {
            foreach (var item in value.Split(','))
            {
                if (!TryParse(item, start, max, out var arr)) return false;
                if (arr.Length > 0) rs.AddRange(arr);
            }
            vs = rs.ToArray();
            return true;
        }

        // 步进值
        var step = 1;
        var p = value.IndexOf('/');
        if (p > 0)
        {
            step = value[(p + 1)..].ToInt();
            value = value[..p];
        }

        // 连续范围
        var s = start;
        if (value is "*" or "?")
            s = 0;
        else if ((p = value.IndexOf('-')) > 0)
        {
            s = value[..p].ToInt();
            max = value[(p + 1)..].ToInt() + 1;
        }
        else if (Int32.TryParse(value, out n))
            s = n;
        else
            return false;

        for (var i = s; i < max; i += step)
        {
            if (i >= start) rs.Add(i);
        }

        vs = rs.ToArray();
        return true;
    }

    private static Boolean TryParseWeek(String value, Int32 start, Int32 max, IDictionary<Int32, Int32> weeks)
    {
        // 固定值，最为常见，优先计算
        if (Int32.TryParse(value, out var n))
        {
            weeks[n] = 0;
            return true;
        }

        // 递归处理混合值
        if (value.Contains(','))
        {
            foreach (var item in value.Split(','))
            {
                if (!TryParseWeek(item, start, max, weeks)) return false;
            }
            return true;
        }

        // 步进值
        var step = 1;
        var v = value;
        var p = value.IndexOf('/');
        if (p > 0)
        {
            step = value[(p + 1)..].ToInt();
            v = value[..p];
        }

        // 第几个星期几
        var index = 0;
        p = v.IndexOf('#');
        if (p > 0)
        {
            var str = v[(p + 1)..];
            if (str.StartsWithIgnoreCase("L"))
                index = -str[1..].ToInt();
            else
                index = str.ToInt();
            v = v[..p];
            step = 7;
        }

        // 连续范围
        var s = start;
        if (v is "*" or "?")
            s = 0;
        else if ((p = v.IndexOf('-')) > 0)
        {
            s = v[..p].ToInt();
            max = v[(p + 1)..].ToInt() + 1;
            step = 1;
        }
        else if (Int32.TryParse(v, out n))
            s = n;
        else
            return false;

        for (var i = s; i < max; i += step)
        {
            if (i >= start) weeks.Add(i, index);
        }

        return true;
    }

    /// <summary>获得指定时间之后的下一次执行时间，不含指定时间</summary>
    /// <remarks>
    /// 如果指定时间带有毫秒，则向前对齐。如09:14.123的"15 * * *"下一次是10:15而不是09：15
    /// </remarks>
    /// <param name="time">从该时间秒的下一秒算起的下一个执行时间</param>
    /// <returns>下一次执行时间（秒级），如果没有匹配则返回最小时间</returns>
    public DateTime GetNext(DateTime time)
    {
        // 如果指定时间带有毫秒，则向前对齐。如09:14.123格式化为09:15，计算下一次就从09:16开始
        var start = time.Trim();
        if (start != time)
            start = start.AddSeconds(2);
        else
            start = start.AddSeconds(1);

        // 设置末尾，避免死循环越界
        var end = time.AddYears(1);
        for (var dt = start; dt < end; dt = dt.AddSeconds(1))
        {
            if (IsTime(dt)) return dt;
        }

        return DateTime.MinValue;
    }

    /// <summary>获得与指定时间时间符合表达式的最远时间（秒级）</summary>
    /// <param name="time"></param>
    public DateTime GetPrevious(DateTime time)
    {
        // 如果指定时间带有毫秒，则向前对齐。如09:14.123格式化为09:15，计算下一次就从09:16开始
        var start = time.Trim();
        if (start != time)
            start = start.AddSeconds(-1);
        else
            start = start.AddSeconds(-2);

        // 设置末尾，避免死循环越界
        var end = time.AddYears(-1);
        var last = false;
        for (var dt = start; dt > end; dt = dt.AddSeconds(-1))//过去一年内
        {
            if (last == false)
            {
                last = IsTime(dt);//找真值
            }
            else
            {
                if (IsTime(dt) == false)//真值找到了找假值
                {
                    return dt.AddSeconds(1);//减多了，返回真值
                }
            }
            //if (last == true && IsTime(dt) == false) return dt.AddSeconds(1);
            //last = IsTime(dt);
        }

        return DateTime.MinValue;
    }
    #endregion
}