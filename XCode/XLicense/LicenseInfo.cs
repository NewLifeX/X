using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Collections;
using System.Reflection;

namespace XCode.XLicense
{
    /// <summary>
    /// 授权信息
    /// </summary>
    internal class LicenseInfo
    {
        #region 基本信息
        private LicenseItemInt32 _EvaluationPeriod = new LicenseItemInt32();
        /// <summary>
        /// 执行时间。以分为单位，执行指定时间后，程序退出。
        /// </summary>
        public LicenseItemInt32 EvaluationPeriod
        {
            get { return _EvaluationPeriod; }
            set { _EvaluationPeriod = value; }
        }

        private LicenseItemDateTime _ExpirationDate = new LicenseItemDateTime();
        /// <summary>
        /// 有效试用期。某天之前有效。
        /// </summary>
        public LicenseItemDateTime ExpirationDate
        {
            get { return _ExpirationDate; }
            set { _ExpirationDate = value; }
        }

        private LicenseItemInt32 _MaxIP = new LicenseItemInt32();
        /// <summary>
        /// 最大在线IP数。按一分钟统计。
        /// </summary>
        public LicenseItemInt32 MaxIP
        {
            get { return _MaxIP; }
            set { _MaxIP = value; }
        }

        private LicenseItemInt32 _MaxSession = new LicenseItemInt32();
        /// <summary>
        /// 最大会话数。按一分钟统计。
        /// </summary>
        public LicenseItemInt32 MaxSession
        {
            get { return _MaxSession; }
            set { _MaxSession = value; }
        }

        private LicenseItemInt32 _MaxEntity = new LicenseItemInt32();
        /// <summary>
        /// 最大实体类数
        /// </summary>
        public LicenseItemInt32 MaxEntity
        {
            get { return _MaxEntity; }
            set { _MaxEntity = value; }
        }

        private LicenseItemInt32 _MaxDbConnect = new LicenseItemInt32();
        /// <summary>
        /// 最大数据库连接数
        /// </summary>
        public LicenseItemInt32 MaxDbConnect
        {
            get { return _MaxDbConnect; }
            set { _MaxDbConnect = value; }
        }

        private LicenseItemInt32 _MaxCache = new LicenseItemInt32();
        /// <summary>
        /// 最大缓存数
        /// </summary>
        public LicenseItemInt32 MaxCache
        {
            get { return _MaxCache; }
            set { _MaxCache = value; }
        }

        private LicenseItemString _WebSiteList = new LicenseItemString();
        /// <summary>
        /// 可用站点列表
        /// </summary>
        public LicenseItemString WebSiteList
        {
            get { return _WebSiteList; }
            set { _WebSiteList = value; }
        }
        #endregion

        #region 硬件信息
        private LicenseItemString _MachineName = new LicenseItemString();
        /// <summary>
        /// 机器名
        /// </summary>
        public LicenseItemString MachineName
        {
            get { return _MachineName; }
            set { _MachineName = value; }
        }

        private LicenseItemString _BaseBoard = new LicenseItemString();
        /// <summary>
        /// 主板序列号
        /// </summary>
        public LicenseItemString BaseBoard
        {
            get { return _BaseBoard; }
            set { _BaseBoard = value; }
        }

        private LicenseItemString _Processors = new LicenseItemString();
        /// <summary>
        /// CPU序列号
        /// </summary>
        public LicenseItemString Processors
        {
            get { return _Processors; }
            set { _Processors = value; }
        }

        private LicenseItemString _DiskDrives = new LicenseItemString();
        /// <summary>
        /// 磁盘序列号
        /// </summary>
        public LicenseItemString DiskDrives
        {
            get { return _DiskDrives; }
            set { _DiskDrives = value; }
        }

        private LicenseItemString _Macs = new LicenseItemString();
        /// <summary>
        /// 网卡地址
        /// </summary>
        public LicenseItemString Macs
        {
            get { return _Macs; }
            set { _Macs = value; }
        }

        private LicenseItemString _IPs = new LicenseItemString();
        /// <summary>
        /// IP地址
        /// </summary>
        public LicenseItemString IPs
        {
            get { return _IPs; }
            set { _IPs = value; }
        }
        #endregion

        #region 用户信息
        private LicenseItemString _UserName = new LicenseItemString();
        /// <summary>
        /// 用户名
        /// </summary>
        public LicenseItemString UserName
        {
            get
            {
                if (String.IsNullOrEmpty(_UserName)) _UserName = "新生命（http://www.nnhy.org）";
                return _UserName;
            }
            set { _UserName = value; }
        }

        private LicenseItemString _EndUser = new LicenseItemString();
        /// <summary>
        /// 最终用户
        /// </summary>
        public LicenseItemString EndUser
        {
            get
            {
                if (String.IsNullOrEmpty(_EndUser)) _EndUser = "试用用户";
                return _EndUser;
            }
            set { _EndUser = value; }
        }
        #endregion

        #region 文件签名
        private LicenseItemString _FileSignature = new LicenseItemString();
        /// <summary>
        /// 文件签名。保护自己，防止自己被修改。
        /// </summary>
        public LicenseItemString FileSignature
        {
            get { return _FileSignature; }
            set { _FileSignature = value; }
        }
        #endregion

        #region 授权项集合
        private Dictionary<String, LicenseItem> _Items;
        /// <summary>
        /// 授权项集合
        /// </summary>
        public Dictionary<String, LicenseItem> Items
        {
            get
            {
                if (_Items != null) return _Items;
                _Items = new Dictionary<String, LicenseItem>();
                _Items.Add("Item1", EvaluationPeriod);
                _Items.Add("Item2", ExpirationDate);
                _Items.Add("Item3", MaxIP);
                _Items.Add("Item4", MaxSession);
                _Items.Add("Item5", MaxEntity);
                _Items.Add("Item6", MaxDbConnect);
                _Items.Add("Item7", MaxCache);

                _Items.Add("Item8", MachineName);
                _Items.Add("Item9", BaseBoard);
                _Items.Add("ItemA", Processors);
                _Items.Add("ItemB", DiskDrives);
                _Items.Add("ItemC", Macs);
                _Items.Add("ItemD", IPs);

                _Items.Add("ItemE", FileSignature);

                _Items.Add("ItemF", UserName);
                _Items.Add("ItemG", EndUser);

                _Items.Add("ItemH", WebSiteList);

                return _Items;
            }
        }
        #endregion

        #region 读取/校验 硬件信息
        /// <summary>
        /// 读取硬件信息到本对象中
        /// </summary>
        public void ReadHardInfo()
        {
            MachineName = HardInfo.MachineName;
            BaseBoard = HardInfo.BaseBoard;
            Processors = HardInfo.Processors;
            DiskDrives = HardInfo.DiskDrives;
            Macs = HardInfo.Macs;
            IPs = HardInfo.IPs;
        }

        /// <summary>
        /// 校验硬件信息。仅检查启用项。
        /// </summary>
        /// <returns></returns>
        private Boolean CheckHardInfo()
        {
            if (MachineName.Enable && MachineName != HardInfo.MachineName) return false;
            if (BaseBoard.Enable && BaseBoard != HardInfo.BaseBoard) return false;
            if (Processors.Enable && Processors != HardInfo.Processors) return false;
            if (DiskDrives.Enable && DiskDrives != HardInfo.DiskDrives) return false;
            if (Macs.Enable && Macs != HardInfo.Macs) return false;
            if (IPs.Enable && IPs != HardInfo.IPs) return false;
            return true;
        }
        #endregion

        #region 加载、保存
        /// <summary>
        /// 保存
        /// </summary>
        /// <returns></returns>
        public String ToXML()
        {
            try
            {
                XmlDocument Doc = new XmlDocument();
                XmlElement elm = Doc.CreateElement(this.GetType().Name);
                Doc.AppendChild(elm);

                foreach (String item in Items.Keys)
                {
                    Items[item].ToXml(Doc, item);
                }

                return Doc.OuterXml;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static LicenseInfo FromXML(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;
            try
            {
                XmlDocument Doc = new XmlDocument();
                Doc.LoadXml(xml);

                LicenseInfo li = new LicenseInfo();

                foreach (String item in li.Items.Keys)
                {
                    li.Items[item].FromXml(Doc, item);
                }

                return li;
            }
            catch { return null; }
        }
        #endregion

        #region 静态默认项
        //private static LicenseInfo _Develop;
        ///// <summary>
        ///// 开发版。
        ///// 不限制硬件、执行时间。
        ///// 限制试用期。
        ///// </summary>
        //public static LicenseInfo Develop
        //{
        //    get
        //    {
        //        if (_Develop != null) return _Develop;

        //        return _Develop;
        //    }
        //}

        private static LicenseInfo _Trial;
        /// <summary>
        /// 试用版。
        /// 不限制硬件。
        /// 限制执行时间、试用期。
        /// </summary>
        public static LicenseInfo Trial
        {
            get
            {
                if (_Trial != null) return _Trial;
                _Trial = new LicenseInfo();
                _Trial.EvaluationPeriod = 30;
                //_Trial.ExpirationDate = new DateTime(2010, 10, 1);

                if (Compile > new DateTime(2010, 6, 25) && Compile < new DateTime(2010, 12, 31))
                    _Trial.ExpirationDate = Compile.AddMonths(3);
                else
                    _Trial.ExpirationDate = new DateTime(2010, 10, 1);

                _Trial.MaxIP = 10;
                _Trial.MaxSession = 20;
                _Trial.MaxEntity = 20;
                _Trial.MaxDbConnect = 1;
                _Trial.MaxCache = 100;
                _Trial.UserName = "新生命（http://www.nnhy.org）";
                _Trial.EndUser = "试用用户";
                _Trial.WebSiteList.Enable = true;
                return _Trial;
            }
        }

        private static String _Version;
        /// <summary>
        /// 程序集版本
        /// </summary>
        public static String Version
        {
            get
            {
                if (String.IsNullOrEmpty(_Version))
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    _Version = asm.GetName().Version.ToString();
                    if (String.IsNullOrEmpty(_Version)) _Version = "1.0";
                }
                return _Version;
            }
        }

        private static DateTime _Compile;
        /// <summary>编译</summary>
        public static DateTime Compile
        {
            get
            {
                if (_Compile <= DateTime.MinValue)
                {
                    String[] ss = Version.Split(new Char[] { '.' });
                    Int32 d = Convert.ToInt32(ss[2]);
                    Int32 s = Convert.ToInt32(ss[3]);

                    DateTime dt = new DateTime(2000, 1, 1);
                    dt = dt.AddDays(d).AddSeconds(s * 2);

                    _Compile = dt;
                }
                return _Compile;
            }
        }

        //private static LicenseInfo _Official;
        ///// <summary>
        ///// 正式版。
        ///// 限制硬件。
        ///// </summary>
        //public static LicenseInfo Official
        //{
        //    get
        //    {
        //        if (_Official != null) return _Official;

        //        return _Official;
        //    }
        //}

        //private static LicenseInfo _Enterprise;
        ///// <summary>
        ///// 企业版。
        ///// 不限制硬件。
        ///// </summary>
        //public static LicenseInfo Enterprise
        //{
        //    get
        //    {
        //        if (_Enterprise != null) return _Enterprise;
        //        //_Enterprise = new LicenseInfo();
        //        //foreach (LicenseItem item in _Enterprise.Items.Values)
        //        //{
        //        //    item.Enable = false;
        //        //}
        //        return _Enterprise;
        //    }
        //}
        #endregion
    }
}