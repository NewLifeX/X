using System;
using System.Collections.Generic;
using NewLife.Exceptions;

namespace NewLife.Common
{
    /// <summary>身份证</summary>
    public class IdentityCard
    {
        #region 属性
        private DateTime _Birthday;
        /// <summary>生日</summary>
        public DateTime Birthday
        {
            get { return _Birthday; }
            set { _Birthday = value; }
        }

        /// <summary>性别</summary>
        public enum SexType
        {
            /// <summary>男</summary>
            Man,

            /// <summary>女</summary>
            Woman
        }

        private SexType _Sex;
        /// <summary>性别</summary>
        public SexType Sex
        {
            get { return _Sex; }
            set { _Sex = value; }
        }

        private Boolean _IsOld;
        /// <summary>是否15位旧卡</summary>
        public Boolean IsOld
        {
            get { return _IsOld; }
            set { _IsOld = value; }
        }

        private String _AreaNum;
        /// <summary>地区编码</summary>
        public String AreaNum
        {
            get { return _AreaNum; }
            set { _AreaNum = value; }
        }
        #endregion

        #region 构造函数
        /// <summary>初始化</summary>
        public IdentityCard() { }
        #endregion

        #region 验证
        /// <summary>验证身份证是否合法</summary>
        /// <param name="card"></param>
        /// <returns></returns>
        public static Boolean Valid(String card)
        {
            try
            {
                if (Parse(card) != null) return true;
            }
            catch { }

            return false;
        }
        #endregion

        #region 分析
        private static Dictionary<String, IdentityCard> cache = new Dictionary<String, IdentityCard>();

        /// <summary>使用身份证号码初始化</summary>
        /// <param name="card"></param>
        /// <returns></returns>
        public static IdentityCard Parse(String card)
        {
            if (String.IsNullOrEmpty(card)) return null;
            if (card.Length != 15 && card.Length != 18) return null;

            //转为小写，18位身份证后面有个字母x
            card = card.ToLower();

            IdentityCard ic = null;
            if (cache.TryGetValue(card, out ic)) return ic;
            lock (cache)
            {
                if (cache.TryGetValue(card, out ic)) return ic;

                IdentityCard idc = Parse2(card);
                cache.Add(card, idc);
                return idc;
            }
        }

        private static IdentityCard Parse2(String card)
        {
            String area = card.Substring(0, 6);

            IdentityCard idc = new IdentityCard();
            idc.AreaNum = ParseArea(area);

            if (card.Length == 15)
                idc.ParseBirthdayAndSex15(card);
            else if (card.Length == 18)
            {
                idc.ParseBirthdayAndSex18(card);

                //校验码验证  GB11643-1999标准
                String[] arrVarifyCode = ("1,0,x,9,8,7,6,5,4,3,2").Split(',');
                Int32[] Wi = new Int32[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
                char[] Ai = card.Remove(17).ToCharArray();
                int sum = 0;
                for (int i = 0; i < 17; i++)
                {
                    sum += Wi[i] * (Ai[i] - '0');
                }

                int y = -1;
                Math.DivRem(sum, 11, out y);
                if (arrVarifyCode[y] != card.Substring(17, 1).ToLower())
                    throw new XException("验证码校验失败！"); 
            }

            return idc;
        }
        #endregion

        #region 分析地区
        private static List<Int32> ads = new List<Int32>(new Int32[] { 11, 22, 35, 44, 53, 12, 23, 36, 45, 54, 13, 31, 37, 46, 61, 14, 32, 41, 50, 62, 15, 33, 42, 51, 63, 21, 34, 43, 52, 64, 65, 71, 81, 82, 91 });

        private static String ParseArea(String area)
        {
            Int32 n = 0;
            if (!Int32.TryParse(area, out n)) throw new XException("非法地区编码！");

            String str = area.Substring(0, 2);
            if (!Int32.TryParse(str, out n)) throw new XException("非法省份编码！");

            if (!ads.Contains(n)) throw new XException("没有找到该省份！");

            return area;
        }
        #endregion

        #region 分析生日、性别
        private void ParseBirthdayAndSex15(String card)
        {
            String str = card.Substring(6, 2);
            Int32 n = Int32.Parse(str);

            if (n < 20)
                n = 20;
            else
                n = 19;

            String birth = n.ToString() + card.Substring(6, 6).Insert(2, "-").Insert(5, "-");
            DateTime d = DateTime.MinValue;
            if (!DateTime.TryParse(birth, out d)) throw new XException("生日不正确！");
            Birthday = d;

            //最后一位是性别
            n = Convert.ToInt32(card.Substring(card.Length - 1, 1));
            Boolean man = n % 2 != 0;

            if (man)
                Sex = SexType.Man;
            else
                Sex = SexType.Woman;
        }

        private void ParseBirthdayAndSex18(String card)
        {
            String birth = card.Substring(6, 8).Insert(4, "-").Insert(7, "-");
            DateTime d = DateTime.MinValue;
            if (!DateTime.TryParse(birth, out d)) throw new XException("生日不正确！");
            Birthday = d;

            //倒数第二位是性别
            Int32 n = Convert.ToInt32(card.Substring(card.Length - 2, 1));
            Boolean man = n % 2 != 0;

            if (man)
                Sex = SexType.Man;
            else
                Sex = SexType.Woman;
        }
        #endregion
    }
}