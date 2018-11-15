using System;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace Test
{
    /// <summary>测试表格</summary>
    [Serializable]
    [DataObject]
    [Description("测试表格")]
    [BindTable("TestTable", Description = "测试表格", ConnName = "Log2", DbType = DatabaseType.SQLite)]
    public partial class TestTable : ITestTable
    {
        #region 属性
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("Id", "编号", "int")]
        public Int32 Id { get { return _Id; } set { if (OnPropertyChanging(__.Id, value)) { _Id = value; OnPropertyChanged(__.Id); } } }

        private String _Title;
        /// <summary>标题</summary>
        [DisplayName("标题")]
        [Description("标题")]
        [DataObjectField(false, false, true, 250)]
        [BindColumn("Title", "标题", "nvarchar(250)", Master = true)]
        public String Title { get { return _Title; } set { if (OnPropertyChanging(__.Title, value)) { _Title = value; OnPropertyChanged(__.Title); } } }

        private String _Content;
        /// <summary>描述</summary>
        [DisplayName("描述")]
        [Description("描述")]
        [DataObjectField(false, false, true, -1)]
        [BindColumn("Content", "描述", "ntext")]
        public String Content { get { return _Content; } set { if (OnPropertyChanging(__.Content, value)) { _Content = value; OnPropertyChanged(__.Content); } } }

        private String _TitleColor;
        /// <summary>类别名称颜色</summary>
        [DisplayName("类别名称颜色")]
        [Description("类别名称颜色")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("TitleColor", "类别名称颜色", "nvarchar(50)")]
        public String TitleColor { get { return _TitleColor; } set { if (OnPropertyChanging(__.TitleColor, value)) { _TitleColor = value; OnPropertyChanged(__.TitleColor); } } }

        private Int32 _PageSize;
        /// <summary>每页显示数量</summary>
        [DisplayName("每页显示数量")]
        [Description("每页显示数量")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("PageSize", "每页显示数量", "int")]
        public Int32 PageSize { get { return _PageSize; } set { if (OnPropertyChanging(__.PageSize, value)) { _PageSize = value; OnPropertyChanged(__.PageSize); } } }

        private Int32 _PId;
        /// <summary>上级ID</summary>
        [DisplayName("上级ID")]
        [Description("上级ID")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("PId", "上级ID", "int")]
        public Int32 PId { get { return _PId; } set { if (OnPropertyChanging(__.PId, value)) { _PId = value; OnPropertyChanged(__.PId); } } }

        private Int32 _Level;
        /// <summary>级别</summary>
        [DisplayName("级别")]
        [Description("级别")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Level", "级别", "int")]
        public Int32 Level { get { return _Level; } set { if (OnPropertyChanging(__.Level, value)) { _Level = value; OnPropertyChanged(__.Level); } } }

        private Boolean _IsHide;
        /// <summary>是否隐藏</summary>
        [DisplayName("是否隐藏")]
        [Description("是否隐藏")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("IsHide", "是否隐藏", "bit")]
        public Boolean IsHide { get { return _IsHide; } set { if (OnPropertyChanging(__.IsHide, value)) { _IsHide = value; OnPropertyChanged(__.IsHide); } } }

        private Int32 _Counts;
        /// <summary>详情数量，缓存</summary>
        [DisplayName("详情数量")]
        [Description("详情数量，缓存")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Counts", "详情数量，缓存", "int")]
        public Int32 Counts { get { return _Counts; } set { if (OnPropertyChanging(__.Counts, value)) { _Counts = value; OnPropertyChanged(__.Counts); } } }

        private Int32 _Rank;
        /// <summary>排序</summary>
        [DisplayName("排序")]
        [Description("排序")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Rank", "排序", "int")]
        public Int32 Rank { get { return _Rank; } set { if (OnPropertyChanging(__.Rank, value)) { _Rank = value; OnPropertyChanged(__.Rank); } } }

        private Decimal _Price;
        /// <summary>价格</summary>
        [DisplayName("价格")]
        [Description("价格")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Price", "价格", "money", Precision = 12, Scale = 2)]
        public Decimal Price { get { return _Price; } set { if (OnPropertyChanging(__.Price, value)) { _Price = value; OnPropertyChanged(__.Price); } } }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public override Object this[String name]
        {
            get
            {
                switch (name)
                {
                    case __.Id: return _Id;
                    case __.Title: return _Title;
                    case __.Content: return _Content;
                    case __.TitleColor: return _TitleColor;
                    case __.PageSize: return _PageSize;
                    case __.PId: return _PId;
                    case __.Level: return _Level;
                    case __.IsHide: return _IsHide;
                    case __.Counts: return _Counts;
                    case __.Rank: return _Rank;
                    case __.Price: return _Price;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.Id: _Id = Convert.ToInt32(value); break;
                    case __.Title: _Title = Convert.ToString(value); break;
                    case __.Content: _Content = Convert.ToString(value); break;
                    case __.TitleColor: _TitleColor = Convert.ToString(value); break;
                    case __.PageSize: _PageSize = Convert.ToInt32(value); break;
                    case __.PId: _PId = Convert.ToInt32(value); break;
                    case __.Level: _Level = Convert.ToInt32(value); break;
                    case __.IsHide: _IsHide = Convert.ToBoolean(value); break;
                    case __.Counts: _Counts = Convert.ToInt32(value); break;
                    case __.Rank: _Rank = Convert.ToInt32(value); break;
                    case __.Price: _Price = Convert.ToDecimal(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得测试表格字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field Id = FindByName(__.Id);

            /// <summary>标题</summary>
            public static readonly Field Title = FindByName(__.Title);

            /// <summary>描述</summary>
            public static readonly Field Content = FindByName(__.Content);

            /// <summary>类别名称颜色</summary>
            public static readonly Field TitleColor = FindByName(__.TitleColor);

            /// <summary>每页显示数量</summary>
            public static readonly Field PageSize = FindByName(__.PageSize);

            /// <summary>上级ID</summary>
            public static readonly Field PId = FindByName(__.PId);

            /// <summary>级别</summary>
            public static readonly Field Level = FindByName(__.Level);

            /// <summary>是否隐藏</summary>
            public static readonly Field IsHide = FindByName(__.IsHide);

            /// <summary>详情数量，缓存</summary>
            public static readonly Field Counts = FindByName(__.Counts);

            /// <summary>排序</summary>
            public static readonly Field Rank = FindByName(__.Rank);

            /// <summary>价格</summary>
            public static readonly Field Price = FindByName(__.Price);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
        }

        /// <summary>取得测试表格字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String Id = "Id";

            /// <summary>标题</summary>
            public const String Title = "Title";

            /// <summary>描述</summary>
            public const String Content = "Content";

            /// <summary>类别名称颜色</summary>
            public const String TitleColor = "TitleColor";

            /// <summary>每页显示数量</summary>
            public const String PageSize = "PageSize";

            /// <summary>上级ID</summary>
            public const String PId = "PId";

            /// <summary>级别</summary>
            public const String Level = "Level";

            /// <summary>是否隐藏</summary>
            public const String IsHide = "IsHide";

            /// <summary>详情数量，缓存</summary>
            public const String Counts = "Counts";

            /// <summary>排序</summary>
            public const String Rank = "Rank";

            /// <summary>价格</summary>
            public const String Price = "Price";
        }
        #endregion
    }

    /// <summary>测试表格接口</summary>
    public partial interface ITestTable
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 Id { get; set; }

        /// <summary>标题</summary>
        String Title { get; set; }

        /// <summary>描述</summary>
        String Content { get; set; }

        /// <summary>类别名称颜色</summary>
        String TitleColor { get; set; }

        /// <summary>每页显示数量</summary>
        Int32 PageSize { get; set; }

        /// <summary>上级ID</summary>
        Int32 PId { get; set; }

        /// <summary>级别</summary>
        Int32 Level { get; set; }

        /// <summary>是否隐藏</summary>
        Boolean IsHide { get; set; }

        /// <summary>详情数量，缓存</summary>
        Int32 Counts { get; set; }

        /// <summary>排序</summary>
        Int32 Rank { get; set; }

        /// <summary>价格</summary>
        Decimal Price { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}