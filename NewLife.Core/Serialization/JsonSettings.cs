using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// Json设置
    /// </summary>
    public class JsonSettings : TextReaderWriterSetting
    {
        #region 属性
        private Boolean _Indent;
        /// <summary>缩进</summary>
        public Boolean Indent
        {
            get { return _Indent; }
            set { _Indent = value; }
        }

        private JsDateTimeFormats _JsDateTimeFormat;
        /// <summary>
        /// 指定日期时间输出成什么格式,具体格式说明见JsDateTimeFormats,默认是ISO8601格式
        /// </summary>
        public JsDateTimeFormats JsDateTimeFormat
        {
            get { return _JsDateTimeFormat; }
            set { _JsDateTimeFormat = value; }
        }

        private DateTimeKind _JsDateTimeKind;
        /// <summary>
        /// 指定日期时间输出成什么时间,本地还是UTC时间,默认是UTC时间
        /// </summary>
        public DateTimeKind JsDateTimeKind
        {
            get { return _JsDateTimeKind; }
            set { _JsDateTimeKind = value; }
        }

        private bool _JsEncodeUnicode;
        /// <summary>
        /// 是否编码字符串中Unicode字符为\uXXXX的格式
        /// 
        /// 可以避免乱码问题,但是会增加数据长度
        /// </summary>
        public Boolean JsEncodeUnicode
        {
            get { return _JsEncodeUnicode; }
            set { _JsEncodeUnicode = value; }
        }

        private bool _AllowMultiline;
        /// <summary>
        /// 是否允许输出多行结果,这会便于阅读结果,当为false时可以用作jsonp回调(还需要做字符串转义)
        /// </summary>
        public bool JsMultiline
        {
            get { return _AllowMultiline; }
            set { _AllowMultiline = value; }
        }
        private RepeatedAction _RepeatedActionType;
        /// <summary>
        /// 重复对象的处理方式
        /// </summary>
        public RepeatedAction RepeatedActionType
        {
            get { return _RepeatedActionType; }
            set { _RepeatedActionType = value; }
        }

        private int _DepthLimit;
        /// <summary>
        /// 复合对象的解析深度限制,只有在RepeatedActionType是RepeatedAction.DepthLimit
        /// 
        /// 默认1000,不建议设置过大
        /// 
        /// 关于1000的取值,测试调用堆栈极限程序中大概12273次调用时抛出StackOverflowException异常,而每处理一个ReadObject大概需要9个调用
        /// </summary>
        public int DepthLimit
        {
            get { return _DepthLimit; }
            set { _DepthLimit = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        public JsonSettings()
        {
            // 指定时间的格式
            DateTimeFormat = DateTimeFormats.Milliseconds;
            JsDateTimeKind = DateTimeKind.Utc;
            DepthLimit = 1000;
        }
        #endregion




    }
    /// <summary>
    /// json序列化时用于指定日期时间输出成什么格式
    /// </summary>
    public enum JsDateTimeFormats
    {
        /// <summary>
        /// ISO 8601格式 类似"2011-05-05T05:12:19.123Z"格式的UTC时间
        /// 
        /// 在http://goo.gl/RZoaz中有js端实现,并且在ie8(ie8模式) ff3.5之后都内建提供toJSON()实现
        /// 
        /// 这也是默认格式
        /// </summary>
        ISO8601 = 0,
        /// <summary>
        /// dotnet3.5中System.Web.Script.Serialization.JavaScriptSerializer输出的格式
        /// 
        /// 类似"\/Date(1304572339844)\/"格式的从 UTC 1970.1.1 午夜开始已经经过的毫秒数
        /// </summary>
        DotnetDateTick,
        /// <summary>
        /// 数字,具体值依赖于DateTimeFormat的配置
        /// </summary>
        Tick
    }
    /// <summary>
    /// 重复对象的处理方式
    /// </summary>
    public enum RepeatedAction
    {
        /// <summary>
        /// 限制处理复合对象的深度
        /// </summary>
        DepthLimit

        // TODO 其它的处理方式
    }
}
