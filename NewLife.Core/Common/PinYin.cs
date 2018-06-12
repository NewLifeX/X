using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Reflection;
using NewLife.Collections;
#if !__CORE__
using NewLife.Log;
using NewLife.Web;
#endif

namespace NewLife.Common
{
    /// <summary>汉字拼音转换类</summary>
    public class PinYin
    {
        #region 数组信息
        private static Int32[] pyValue = new Int32[]
        {
            -20319, -20317, -20304, -20295, -20292, -20283, -20265, -20257, -20242,
            -20230, -20051, -20036, -20032, -20026, -20002, -19990, -19986, -19982,
            -19976, -19805, -19784, -19775, -19774, -19763, -19756, -19751, -19746,
            -19741, -19739, -19728, -19725, -19715, -19540, -19531, -19525, -19515,
            -19500, -19484, -19479, -19467, -19289, -19288, -19281, -19275, -19270,
            -19263, -19261, -19249, -19243, -19242, -19238, -19235, -19227, -19224,
            -19218, -19212, -19038, -19023, -19018, -19006, -19003, -18996, -18977,
            -18961, -18952, -18783, -18774, -18773, -18763, -18756, -18741, -18735,
            -18731, -18722, -18710, -18697, -18696, -18526, -18518, -18501, -18490,
            -18478, -18463, -18448, -18447, -18446, -18239, -18237, -18231, -18220,
            -18211, -18201, -18184, -18183, -18181, -18012, -17997, -17988, -17970,
            -17964, -17961, -17950, -17947, -17931, -17928, -17922, -17759, -17752,
            -17733, -17730, -17721, -17703, -17701, -17697, -17692, -17683, -17676,
            -17496, -17487, -17482, -17468, -17454, -17433, -17427, -17417, -17202,
            -17185, -16983, -16970, -16942, -16915, -16733, -16708, -16706, -16689,
            -16664, -16657, -16647, -16474, -16470, -16465, -16459, -16452, -16448,
            -16433, -16429, -16427, -16423, -16419, -16412, -16407, -16403, -16401,
            -16393, -16220, -16216, -16212, -16205, -16202, -16187, -16180, -16171,
            -16169, -16158, -16155, -15959, -15958, -15944, -15933, -15920, -15915,
            -15903, -15889, -15878, -15707, -15701, -15681, -15667, -15661, -15659,
            -15652, -15640, -15631, -15625, -15454, -15448, -15436, -15435, -15419,
            -15416, -15408, -15394, -15385, -15377, -15375, -15369, -15363, -15362,
            -15183, -15180, -15165, -15158, -15153, -15150, -15149, -15144, -15143,
            -15141, -15140, -15139, -15128, -15121, -15119, -15117, -15110, -15109,
            -14941, -14937, -14933, -14930, -14929, -14928, -14926, -14922, -14921,
            -14914, -14908, -14902, -14894, -14889, -14882, -14873, -14871, -14857,
            -14678, -14674, -14670, -14668, -14663, -14654, -14645, -14630, -14594,
            -14429, -14407, -14399, -14384, -14379, -14368, -14355, -14353, -14345,
            -14170, -14159, -14151, -14149, -14145, -14140, -14137, -14135, -14125,
            -14123, -14122, -14112, -14109, -14099, -14097, -14094, -14092, -14090,
            -14087, -14083, -13917, -13914, -13910, -13907, -13906, -13905, -13896,
            -13894, -13878, -13870, -13859, -13847, -13831, -13658, -13611, -13601,
            -13406, -13404, -13400, -13398, -13395, -13391, -13387, -13383, -13367,
            -13359, -13356, -13343, -13340, -13329, -13326, -13318, -13147, -13138,
            -13120, -13107, -13096, -13095, -13091, -13076, -13068, -13063, -13060,
            -12888, -12875, -12871, -12860, -12858, -12852, -12849, -12838, -12831,
            -12829, -12812, -12802, -12607, -12597, -12594, -12585, -12556, -12359,
            -12346, -12320, -12300, -12120, -12099, -12089, -12074, -12067, -12058,
            -12039, -11867, -11861, -11847, -11831, -11798, -11781, -11604, -11589,
            -11536, -11358, -11340, -11339, -11324, -11303, -11097, -11077, -11067,
            -11055, -11052, -11045, -11041, -11038, -11024, -11020, -11019, -11018,
            -11014, -10838, -10832, -10815, -10800, -10790, -10780, -10764, -10587,
            -10544, -10533, -10519, -10331, -10329, -10328, -10322, -10315, -10309,
            -10307, -10296, -10281, -10274, -10270, -10262, -10260, -10256, -10254
        };

        private static String[] pyName = new String[]
        {
             "A", "Ai", "An", "Ang", "Ao", "Ba", "Bai", "Ban", "Bang", "Bao", "Bei",
             "Ben", "Beng", "Bi", "Bian", "Biao", "Bie", "Bin", "Bing", "Bo", "Bu",
             "Ba", "Cai", "Can", "Cang", "Cao", "Ce", "Ceng", "Cha", "Chai", "Chan",
             "Chang", "Chao", "Che", "Chen", "Cheng", "Chi", "Chong", "Chou", "Chu",
             "Chuai", "Chuan", "Chuang", "Chui", "Chun", "Chuo", "Ci", "Cong", "Cou",
             "Cu", "Cuan", "Cui", "Cun", "Cuo", "Da", "Dai", "Dan", "Dang", "Dao", "De",
             "Deng", "Di", "Dian", "Diao", "Die", "Ding", "Diu", "Dong", "Dou", "Du",
             "Duan", "Dui", "Dun", "Duo", "E", "En", "Er", "Fa", "Fan", "Fang", "Fei",
             "Fen", "Feng", "Fo", "Fou", "Fu", "Ga", "Gai", "Gan", "Gang", "Gao", "Ge",
             "Gei", "Gen", "Geng", "Gong", "Gou", "Gu", "Gua", "Guai", "Guan", "Guang",
             "Gui", "Gun", "Guo", "Ha", "Hai", "Han", "Hang", "Hao", "He", "Hei", "Hen",
             "Heng", "Hong", "Hou", "Hu", "Hua", "Huai", "Huan", "Huang", "Hui", "Hun",
             "Huo", "Ji", "Jia", "Jian", "Jiang", "Jiao", "Jie", "Jin", "Jing", "Jiong",
             "Jiu", "Ju", "Juan", "Jue", "Jun", "Ka", "Kai", "Kan", "Kang", "Kao", "Ke",
             "Ken", "Keng", "Kong", "Kou", "Ku", "Kua", "Kuai", "Kuan", "Kuang", "Kui",
             "Kun", "Kuo", "La", "Lai", "Lan", "Lang", "Lao", "Le", "Lei", "Leng", "Li",
             "Lia", "Lian", "Liang", "Liao", "Lie", "Lin", "Ling", "Liu", "Long", "Lou",
             "Lu", "Lv", "Luan", "Lue", "Lun", "Luo", "Ma", "Mai", "Man", "Mang", "Mao",
             "Me", "Mei", "Men", "Meng", "Mi", "Mian", "Miao", "Mie", "Min", "Ming", "Miu",
             "Mo", "Mou", "Mu", "Na", "Nai", "Nan", "Nang", "Nao", "Ne", "Nei", "Nen",
             "Neng", "Ni", "Nian", "Niang", "Niao", "Nie", "Nin", "Ning", "Niu", "Nong",
             "Nu", "Nv", "Nuan", "Nue", "Nuo", "O", "Ou", "Pa", "Pai", "Pan", "Pang",
             "Pao", "Pei", "Pen", "Peng", "Pi", "Pian", "Piao", "Pie", "Pin", "Ping",
             "Po", "Pu", "Qi", "Qia", "Qian", "Qiang", "Qiao", "Qie", "Qin", "Qing",
             "Qiong", "Qiu", "Qu", "Quan", "Que", "Qun", "Ran", "Rang", "Rao", "Re",
             "Ren", "Reng", "Ri", "Rong", "Rou", "Ru", "Ruan", "Rui", "Run", "Ruo",
             "Sa", "Sai", "San", "Sang", "Sao", "Se", "Sen", "Seng", "Sha", "Shai",
             "Shan", "Shang", "Shao", "She", "Shen", "Sheng", "Shi", "Shou", "Shu",
             "Shua", "Shuai", "Shuan", "Shuang", "Shui", "Shun", "Shuo", "Si", "Song",
             "Sou", "Su", "Suan", "Sui", "Sun", "Suo", "Ta", "Tai", "Tan", "Tang",
             "Tao", "Te", "Teng", "Ti", "Tian", "Tiao", "Tie", "Ting", "Tong", "Tou",
             "Tu", "Tuan", "Tui", "Tun", "Tuo", "Wa", "Wai", "Wan", "Wang", "Wei",
             "Wen", "Weng", "Wo", "Wu", "Xi", "Xia", "Xian", "Xiang", "Xiao", "Xie",
             "Xin", "Xing", "Xiong", "Xiu", "Xu", "Xuan", "Xue", "Xun", "Ya", "Yan",
             "Yang", "Yao", "Ye", "Yi", "Yin", "Ying", "Yo", "Yong", "You", "Yu",
             "Yuan", "Yue", "Yun", "Za", "Zai", "Zan", "Zang", "Zao", "Ze", "Zei",
             "Zen", "Zeng", "Zha", "Zhai", "Zhan", "Zhang", "Zhao", "Zhe", "Zhen",
             "Zheng", "Zhi", "Zhong", "Zhou", "Zhu", "Zhua", "Zhuai", "Zhuan",
             "Zhuang", "Zhui", "Zhun", "Zhuo", "Zi", "Zong", "Zou", "Zu", "Zuan",
             "Zui", "Zun", "Zuo"
         };

        #region 变量定义
        // GB2312-80 标准规范中第一个汉字的机内码.即"啊"的机内码
        private const Int32 firstChCode = -20319;
        // GB2312-80 标准规范中最后一个汉字的机内码.即"齄"的机内码
        private const Int32 lastChCode = -2050;
        // GB2312-80 标准规范中最后一个一级汉字的机内码.即"座"的机内码
        private const Int32 lastOfOneLevelChCode = -10247;
        // 配置中文字符
        //static Regex regex = new Regex("[\u4e00-\u9fa5]$");
        #endregion
        #endregion

        /// <summary>取拼音第一个字段</summary>        
        /// <param name="ch"></param>        
        /// <returns></returns>        
        public static String GetFirst(Char ch)
        {
            var rs = Get(ch);
            if (!String.IsNullOrEmpty(rs)) rs = rs.Substring(0, 1);

            return rs;
        }

        /// <summary>取拼音第一个字段</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String GetFirst(String str)
        {
            if (str.IsNullOrEmpty()) return String.Empty;

            var sb = Pool.StringBuilder.Get();
            var chs = str.ToCharArray();

            for (var i = 0; i < chs.Length; i++)
            {
                sb.Append(GetFirst(chs[i]));
            }

            return sb.Put(true);
        }

        /// <summary>取拼音第一个字段</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String GetFirstOne(String str)
        {
            if (str.IsNullOrEmpty()) return String.Empty;

            var sb = Pool.StringBuilder.Get();
            var chs = str.ToCharArray();
            if (chs.Length > 0) sb.Append(GetFirst(chs[0]));

            return sb.Put(true);
        }
        private static Encoding gb2312;
        /// <summary>获取单字拼音</summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static String Get(Char ch)
        {
            // 拉丁字符            
            if (ch <= '\x00FF') return ch.ToString();

            // 标点符号、分隔符            
            if (Char.IsPunctuation(ch) || Char.IsSeparator(ch)) return ch.ToString();

            // 非中文字符            
            if (ch < '\x4E00' || ch > '\x9FA5') return ch.ToString();

            if (gb2312 == null) gb2312 = Encoding.GetEncoding("gb2312");
            var arr = gb2312.GetBytes(ch.ToString());
            var chr = (Int16)arr[0] * 256 + (Int16)arr[1] - 65536;

            // 单字符--英文或半角字符  
            if (chr > 0 && chr < 160) return ch.ToString();

            #region 中文字符处理

            // 判断是否超过GB2312-80标准中的汉字范围
            if (chr > lastChCode || chr < firstChCode) return ch.ToString();

            // 如果是在一级汉字中
            if (chr <= lastOfOneLevelChCode)
            {
                // 将一级汉字分为12块,每块33个汉字.
                for (var k = 11; k >= 0; k--)
                {
                    var p = k * 33;
                    // 从最后的块开始扫描,如果机内码大于块的第一个机内码,说明在此块中
                    if (chr >= pyValue[p])
                    {
                        // 遍历块中的每个音节机内码,从最后的音节机内码开始扫描,
                        // 如果音节内码小于机内码,则取此音节
                        for (var i = p + 32; i >= p; i--)
                        {
                            if (pyValue[i] <= chr) return pyName[i];
                        }
                        break;
                    }
                }
            }
            #endregion 中文字符处理

#if !__CORE__
            // 调用微软类库
            var ss = GetPinYinByMS(ch);
            if (ss != null && ss.Length > 0) return FixMS(ss[0], false);
#endif

            return String.Empty;
        }

        /// <summary>获取多音节拼音</summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static String[] GetMulti(Char ch)
        {
#if !__CORE__
            // 多音节优先使用微软库
            var ss = GetPinYinByMS(ch);
            // 去掉最后的音调
            if (ss != null) return ss.Select(e => FixMS(e, true)).ToArray();
#endif

            return new String[] { Get(ch) };
        }

        /// <summary>把汉字转换成拼音(全拼)</summary>
        /// <param name="str">汉字字符串</param>
        /// <returns>转换后的拼音(全拼)字符串</returns>
        public static String Get(String str)
        {
            if (str.IsNullOrEmpty()) return String.Empty;

            var sb = Pool.StringBuilder.Get();
            var chs = str.ToCharArray();

            for (var j = 0; j < chs.Length; j++)
            {
                sb.Append(Get(chs[j]));
            }

            return sb.Put(true);
        }

#if !__CORE__
        static Boolean _inited = false;
        static Type _type;
        /// <summary>从微软拼音库获取拼音，包括音调</summary>
        /// <param name="chr"></param>
        /// <returns></returns>
        public static String[] GetPinYinByMS(Char chr)
        {
            if (_type == null)
            {
                if (_inited) return null;
                _inited = true;

                _type = PluginHelper.LoadPlugin("ChineseChar", "微软拼音库", "ChnCharInfo.dll", "PinYin");
                if (_type == null) XTrace.WriteLine("未找到微软拼音库ChineseChar类");
            }
            if (_type == null) return null;

            var list = _type.CreateInstance(chr).GetValue("Pinyins", false) as IList<String>;
            if (list == null || list.Count == 0) return null;

            return list.Where(e => !String.IsNullOrEmpty(e)).ToArray();
        }

        private static String FixMS(String py, Boolean yinJie)
        {
            if (py.IsNullOrEmpty()) return py;

            if (!yinJie)
            {
                // 去掉最后的音调
                var ch = py[py.Length - 1];
                if (ch >= '0' && ch <= '9') py = py.Substring(0, py.Length - 1);
            }

            // 驼峰
            return py[0] + py.Substring(1, py.Length - 1).ToLower();
        }
#endif
    }
}