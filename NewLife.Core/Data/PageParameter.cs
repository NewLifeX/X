using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Data;

/// <summary>分页参数信息。可携带统计和数据权限扩展查询等信息</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/page_parameter
/// 
/// 支持两种排序模式：
/// 1. Sort属性：单字段排序，自动解析方向，适合前端传参，会进行SQL安全性校验
/// 2. OrderBy属性：复杂排序语句，优先级更高，不进行SQL安全性校验
/// 
/// 支持两种分页模式：
/// 1. PageIndex模式：基于页码的分页（默认）
/// 2. StartRow模式：基于起始行的分页，设置后PageIndex将被忽略
/// </remarks>
public class PageParameter
{
    #region 核心属性
    private String? _Sort;

    /// <summary>排序字段，前台接收，便于做SQL安全性校验</summary>
    /// <remarks>
    /// 一般用于接收单个排序字段，可以带上Asc/Desc，这里会自动拆分。
    /// 极少数情况下，前端需要传递多个字段排序，这时候可以使用OrderBy。
    /// 
    /// OrderBy优先级更高，且支持手写复杂排序语句（不做SQL安全性校验）。
    /// 如果设置Sort，OrderBy将被清空。
    /// </remarks>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public virtual String? Sort
    {
        get => _Sort;
        set
        {
            _Sort = value;

            // 自动识别带有Asc/Desc的排序
            if (!_Sort.IsNullOrEmpty() && !_Sort.Contains(','))
            {
                _Sort = _Sort.Trim();
                var p = _Sort.LastIndexOf(' ');
                if (p > 0)
                {
                    var dir = _Sort[(p + 1)..];
                    if (dir.EqualIgnoreCase("asc"))
                    {
                        Desc = false;
                        _Sort = _Sort[..p].Trim();
                    }
                    else if (dir.EqualIgnoreCase("desc"))
                    {
                        Desc = true;
                        _Sort = _Sort[..p].Trim();
                    }
                }
            }

            OrderBy = null;
        }
    }

    /// <summary>是否降序</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public virtual Boolean Desc { get; set; }

    /// <summary>页面索引。从1开始，默认1</summary>
    /// <remarks>如果设定了开始行，分页时将不再使用PageIndex</remarks>
    public virtual Int32 PageIndex { get; set; } = 1;

    /// <summary>页面大小。默认20，若为0表示不分页</summary>
    public virtual Int32 PageSize { get; set; } = 20;
    #endregion

    #region 扩展属性
    /// <summary>总记录数</summary>
    public virtual Int64 TotalCount { get; set; }

    /// <summary>页数</summary>
    public virtual Int64 PageCount => PageSize <= 0 ? 1 :
        (TotalCount + PageSize - 1) / PageSize; // 使用更简洁的向上取整公式

    /// <summary>自定义排序字句。常用于用户自定义排序，不经过SQL安全性校验</summary>
    /// <remarks>
    /// OrderBy优先级更高，且支持手写复杂排序语句（不做SQL安全性校验）。
    /// 如果设置Sort，OrderBy将被清空。
    /// </remarks>
    public virtual String? OrderBy { get; set; }

    /// <summary>开始行</summary>
    /// <remarks>如果设定了开始行，分页时将不再使用PageIndex</remarks>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public virtual Int64 StartRow { get; set; } = -1;

    /// <summary>是否获取总记录数。默认false</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public Boolean RetrieveTotalCount { get; set; }

    /// <summary>状态。用于传递统计、扩展查询等用户数据</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public virtual Object? State { get; set; }

    /// <summary>是否获取统计。默认false</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public Boolean RetrieveState { get; set; }
    #endregion

    #region 构造函数
    /// <summary>实例化分页参数</summary>
    public PageParameter() { }

    /// <summary>通过另一个分页参数来实例化当前分页参数</summary>
    /// <param name="pm">源分页参数</param>
    public PageParameter(PageParameter pm) => CopyFrom(pm);
    #endregion

    #region 方法
    /// <summary>从另一个分页参数拷贝到当前分页参数</summary>
    /// <param name="pm">源分页参数</param>
    /// <returns>返回当前实例，支持链式调用</returns>
    public virtual PageParameter CopyFrom(PageParameter pm)
    {
        if (pm == null) return this;

        // 排序相关
        OrderBy = pm.OrderBy;
        Sort = pm.Sort;
        Desc = pm.Desc;

        // 分页相关
        PageIndex = pm.PageIndex;
        PageSize = pm.PageSize;
        StartRow = pm.StartRow;

        // 统计相关
        TotalCount = pm.TotalCount;
        RetrieveTotalCount = pm.RetrieveTotalCount;

        // 扩展相关
        State = pm.State;
        RetrieveState = pm.RetrieveState;

        return this;
    }

    /// <summary>获取表示分页参数唯一性的键值，可用作缓存键</summary>
    /// <returns>分页参数的唯一标识字符串</returns>
    public virtual String GetKey() => $"{PageIndex}-{PageCount}-{OrderBy}";

    /// <summary>验证分页参数的有效性</summary>
    /// <returns>如果参数有效返回true，否则返回false</returns>
    public virtual Boolean IsValid() => PageIndex > 0 && PageSize >= 0;

    /// <summary>重置分页参数到默认状态</summary>
    public virtual void Reset()
    {
        PageIndex = 1;
        PageSize = 20;
        Sort = null;
        OrderBy = null;
        Desc = false;
        StartRow = -1;
        TotalCount = 0;
        RetrieveTotalCount = false;
        State = null;
        RetrieveState = false;
    }
    #endregion
}