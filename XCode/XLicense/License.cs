using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using NewLife.Log;

namespace XCode.XLicense
{
#if DEBUG
    /// <summary>
    /// X框架授权许可证
    /// </summary>
    internal static class License
    {
        #region 基本属性
        private static LicenseInfo _Current;
        /// <summary>
        /// 当前授权设置
        /// </summary>
        public static LicenseInfo Current
        {
            get
            {
                //不能试用另外的标记判断是否已经加载
                //用户试用的时候，可能一开始忘了放授权文件，从而出现无效授权，等放上授权文件后，需要重新读取
                //所以，如果没有授权文件，将会一直读取，性能就不是那么好了
                if (_Current == null) _Current = GetLicense();
                return _Current;
            }
        }

        /// <summary>
        /// 内部密钥
        /// </summary>
        internal static String InternalKey
        {
            get
            {
                //这是明文，Key就是使用该明文加密后的字符串，返回的时候需要解密
                //String Text = "qxYw9MTk35bmvhwRxhhf6rjaWqu4SogV4CgGjXRdMQU2akGs5kv/7JP8rgSN3WA92oOh7B8Q9TV3Ux31n4H0hKteSbPmiE47J6u9wBv7tpIp52etQalua1pj2n3IDnDOq1gpB2J3WCELo2De83rCJsiHGoxT9bMPFrk3qFT3QTIMpLenluGsRcYMKZC4B0qTdd1xNgMWdXXUYDDvbd3tOHM9P3KzZHwC40GlWtlTad90VktcmFnSMq9LpqSi61Rr4mc57WuVB59Q666el14i44hGcRZaEK51/fdeu3ZwTh06CQxlRFNH2WukqGXZFEFO7ZxT+kONjYD+pU0TLkMoYtoPUrjukoLQaymQfIH0DnizKNSvwlpK7AP7Pme2OBz8BieO8emRerXne1bJYXvz533GYeqYe1eDQWnprGBUVA3LidkDfqk7gy5eJGwMx3mJhig8/cTIbkoqxjTlTEWU0POeaj+n3nDbOSZOXkOMe7w14MeZD/sJwrgDZiGL/QtmS5w2cPIjJdvzJrFjAag/lUyufV7fqcfTxt1s51dOlLtiRKZ+Th0LRtRb+zD2fxgkWROqjhGXKxRSsmD1hg3aGh1ECM2lcVeKRCHU9hy0/GVSH/peTJ1OvzCAtrYTqH9DHQEP2DafYWMcssy+TYijPAWxfi+loW3u4IG8M6PSn8EpzvaIIsNodQytjv0EEqsKQJTSGq3e47ZVyPl9Vkm6Lf0fqunU7tql2tyTJkFYXqq2Ouu8TouMMCpmTlYmMq4TWBqo9FmG98pC5xxL0n3gnZ+ZA6qdtN8fVmMh2n/NnwVcqO5lamo7PpXteD2DnXW4KMksnvVoYebyTy3s7BcCdev0wJnJ0kJHXWXTX3Lj9pwUipf4";
                String key = "mcy3p61tWka23Yl+e+s6YqcpZn86Su5vSfE3IylHReCWg6b3wrhwTpYwis41apClXodEBzIl5H3aO3Q2UQrR6LYgH+D8MPajQ87RTUPpr73IFJ3UmunPj7QFBDY/xNn8vVh1/U/xgGSsreUv0QNpPGCyUydFAGml2GRplcOjGps34XJIU1J65MYNIft9Jjb+pn9M7mQFJ+24UbeuE7u0yMJsCfzblJ08/fNbS3yU4r/VgFQnmQGY4jFWw/jSCwOJ126xibOBmwPOjj+WCcL5qEDgQyfrOo9BPJiS7XQhdq9QMQcZu+zhHQgR9J/yZ09NLg3vY0d3UsdZ96zUUASrkxRpmSmeQ80Whs1RADPW58DS8Ui0iB7cvf4YMe1X4KAgrPiUh7vBzCW6QOvbEedTIPGc+OFlsxSCRzQxHqLiIZshbF4vB2nKURzlnKVFenqRXELym6MlrL/7l2Xmk4IZr0MNfVsHrjy9O3Hgiq2vEubyudWuZ046zNyDUx9kmFOj/O13QIna4RaqO8gx3Z/XQhVaWy1jUzLPGV/Wg3rsk+/3HVN96HUdmmsWFkgdtbC23C5IFnYEWfedmu73iE7bSr6uno1jyAtQY/pouM9bzwvLgQflAMSo7F6zVI+cV4z4rm7zaBbrTT6fD5CPbvbnjyZYwa3lP46UMLm6vDy9/EeCvRqTmR0OEPmdhG3q2yKViThgxU6JimVcTjRjTMbPNFR51BdmZUe/8amPHHij73EVg8HJeuDPbR/aIMaNbsghjjGSJyGSaXUtqu0NUCaXSUA5odLR+i5S0aZYq5YaG10N000ArZage22kj1eVk9tHOiJWqXrCfTPs/GsWsqouoCDcgk8SoJ6mkPkKoKJKIMGOgIOBufPVJ70QUCag4uW4pgLC83BgdOxkn9FuBeHZl81Jo9N1qWsnHqQPrfntgEVBIBvAWyIEa1BZiXoEW6PPzC5qd7K1CPcoFXt25qfsbXPT8tnA/wZLqqQiKVQFcMhjj6xlP1kp8ZilBRLaWy1+3MeJFxY+Ahyu3u8Si4bZim4v9UTg43BcrTDxoFpeWedbcwhtXKIyXe84rNceUqoO9TRLKXCQTJj4MWKL3lQfS534DM0DNGBnk5VG0GqMJrePp3wjG1Az/6yI2EyzrnU/cdi11rKajNr/Be7VWI3iEg==";
                //RC4解密
                return Encoding.UTF8.GetString(Encrypt(Convert.FromBase64String(key), "XCode.nnhy.org"));
            }
        }

        /// <summary>
        /// 公钥
        /// </summary>
        internal static String PublicKey
        {
            get
            {
                String key = "SE3SBT/eiSI1M2bvHnWuNBGLx+tH8sq9DfCB9UFPUqI/BJ1JNqTB8+VR4OaxmuX48h5oss2zp8lNracPggacMAeE0p/V/BI/7rSjYEuk6UDu6+6cje1WANehg+aMT3ZpEszjZB6cGe1rb5GkDUqkVjjgH4sojUphQxrQWEQw50+JOYPJ73VIK4PLjj79zoUfKC6cY/G0YGdMNUUDc2zNHugYsi613RbmTGy/RrvoGPNdk44nnTZWwklOLvMPlDqlU178YtuJ4/Ws8mUqyHAfzQS+ZAmjdtlNoqTLRGglSBvbP9+Mm18ulhaugycC/X498Inh";
                //RC4解密
                return Encoding.UTF8.GetString(Encrypt(Convert.FromBase64String(key), InternalKey));
            }
        }

        //只允许授权管理器使用私钥
#if ISMANAGE
        /// <summary>
        /// 私钥
        /// </summary>
        internal static String PrivateKey
        {
            get
            {
                String key = "SE3SBT/eiSI1M2bvHnWuNBGLx+tH8sq9DfCB9UFPUqI/BJ1JNqTB8+VR4OaxmuX48h5oss2zp8lNracPggacMAeE0p/V/BI/7rSjYEuk6UDu6+6cje1WANehg+aMT3ZpEszjZB6cGe1rb5GkDUqkVjjgH4sojUphQxrQWEQw50+JOYPJ73VIK4PLjj79zoUfKC6cY/G0YGdMNUUDc2zNHugYsi613RbmTGy/RrvoGPNdk44nnTZWwklOLvMPlDqlU178YtuJ4/Ws8mUqyHAfzQS+ZAmjdtlNoqTLRGglSBvbP9+Mm19R+m6X8SYr4W8pvKmU8w+QhzpwwoeX/NLOELMWyK32kssx4JOYmorqpi6iU2x0PVLQ6h6lmzbCbLLcTozL5xchERO8aNq4Y9rDRFwqFrfNF1kPbCperlev1ubpExqER6pk2ZP7jI9Afi7V7V58/243y9HOpt7VzZoRyJEl998+Sralwk8WPviUvqmYSMdanhjMGzChxn8MNEFgGS4CBeI4usSkrjWBTvaRs+z7m9Gektcq/hVjX+cE+MTpvhvi+J3ZnE4LX18LzL/9qY9KUSaPlTaICiFOWNBxS+WNY8zXm6JDt932Ze92iynMyy4BPmCJ8n7SITvKbvFJKZ8kbjO2XomAyxAxlNG3krmlzu7HTVKWy51oL2UlQIrgnCJxYI8wLjPu3rnPQ2vzYbCeDQwEePT2aldAAg6mo5D0An2fZRSpLHeEd/OjmijsKB2EvmYX5EhZ8gAsCDdAtGLlGUZzBHVmm+ES4jmociddtdAYq3NXaMyMZ04GMtWvNgfLraj4K0NOPrmT5xH7Bo1fLCXxFfGckAgFFVsIIhezGu3xWe9pDG44TbwY1bsMMNJRZD1NAxu0CmSMc1KB8Gse68RAB3AKBxMyVyCJJtbE/09BHk0Jhkr5ILxzHyQhq8IVBlPp/wKob8z5/MoCOkUWmZ+4eYxhU4x9RrL0u2oK5hUvTwsIQqMI7B/FpCzd3DUBXci9L7umYDB7wJR36vCem5eTN70RNvahrzKEPRcpMGwx4SucVA7z6cM55KHHindXHk9vho6RqtUIXYwVYOhQrgl0SBViSvB+E2H3d4ZvP/bixSguluhaxO2pbW6+3hwkyTl5i9LwmUqBM4MIVV6i0dDlNtnIXcuYe18hLOYlc4IYDP2TLmu0B26eUOOUlSnIVsPLKoTJqErIfGyVb4B4";
                //RC4解密
                return Encoding.UTF8.GetString(Encrypt(Convert.FromBase64String(key), InternalKey));
            }
        }
#endif
        #endregion

        #region 属性
        /// <summary>
        /// 开始时间。用户评估执行时间。
        /// </summary>
        private static DateTime StartTime = DateTime.Now;

        /// <summary>
        /// IP列表
        /// </summary>
        private static Dictionary<String, DateTime> IPList = new Dictionary<String, DateTime>();

        /// <summary>
        /// 会话列表
        /// </summary>
        private static Dictionary<String, DateTime> SessionList = new Dictionary<String, DateTime>();

        /// <summary>
        /// 列表锁定专用对象
        /// </summary>
        private static Object ListLockObj = new Object();

        #region 数据库连接数
        private static Int32 _DbConnectCount;
        /// <summary>
        /// 数据库连接数
        /// </summary>
        public static Int32 DbConnectCount
        {
            get { return _DbConnectCount; }
            set
            {
                //Trace.WriteLine("设置：DbConnectCount=" + value);
                _DbConnectCount = value;
                if (Current.MaxDbConnect.Enable && Current.MaxDbConnect.IntVal < _DbConnectCount)
                {
                    ShowErr(String.Format("当前数据库连接数是：{0}，已超过授权数：{1}。", _DbConnectCount, Current.MaxDbConnect.IntVal));
                }
            }
        }
        #endregion

        #region 缓存数
        private static Int32 _CacheCount;
        /// <summary>
        /// 缓存数
        /// </summary>
        public static Int32 CacheCount
        {
            get { return _CacheCount; }
            set
            {
                //Trace.WriteLine("设置：CacheCount=" + value);
                _CacheCount = value;
                if (Current.MaxCache.Enable && Current.MaxCache.IntVal < _CacheCount)
                {
                    ShowErr(String.Format("当前缓存数是：{0}，已超过授权数：{1}。", _CacheCount, Current.MaxCache.IntVal));
                }
            }
        }
        #endregion

        #region 实体数
        private static Int32 _EntityCount;
        /// <summary>
        /// 实体数
        /// </summary>
        public static Int32 EntityCount
        {
            get { return _EntityCount; }
            set
            {
                //Trace.WriteLine("设置：EntityCount=" + value);
                _EntityCount = value;
                if (Current.MaxEntity.Enable && Current.MaxEntity.IntVal < _EntityCount)
                {
                    ShowErr(String.Format("当前实体数是：{0}，已超过授权数：{1}。", _EntityCount, Current.MaxEntity.IntVal));
                }
            }
        }
        #endregion

        private static List<String> _Filters;
        /// <summary>可用站点列表</summary>
        public static List<String> Filters
        {
            get
            {
                if (_Filters == null)
                {
                    List<String> list = new List<String>();
                    if (!String.IsNullOrEmpty(Current.WebSiteList.Value))
                    {
                        String[] ss = Current.WebSiteList.Value.Split(new Char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (String item in ss)
                        {
                            list.Add(item.Trim());
                        }
                    }
                    _Filters = list;
                }
                return _Filters;
            }
            //set{_Filters=value;} 
        }

        private static List<String> _LocalFilters;
        /// <summary>本地站点列表</summary>
        public static List<String> LocalFilters
        {
            get
            {
                if (_LocalFilters == null)
                {
                    _LocalFilters = new List<String>();
                    _LocalFilters.Add("127.0.0.1");
                    _LocalFilters.Add("localhost");
                    _LocalFilters.Add(HardInfo.MachineName.ToLower());
                    if (!String.IsNullOrEmpty(HardInfo.IPs))
                    {
                        String[] ss = HardInfo.IPs.Split(' ');
                        foreach (String item in ss)
                        {
                            _LocalFilters.Add(item);
                        }
                    }
                }
                return _LocalFilters;
            }
            //set { _LocalFilters = value; }
        }
        #endregion

        #region 编码/解码
        /// <summary>
        /// 授权编码
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static String Encode(String text)
        {
            String xml = text;
#if ISMANAGE
            //签名。XCode生成的Lic文件不包含签名，防止私钥泄露
            xml += "$"+ License.Sign(Encoding.UTF8.GetBytes(xml), PrivateKey);
#else
            xml += "$";
#endif
            //DES加密
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            //IV的明文
            String key = "mama shuo wo zhang da le";
            //加密IV
            des.GenerateIV();
            String iv = Convert.ToBase64String(Encrypt(des.IV, key));
            //生成一个Key
            des.GenerateKey();
            key = Convert.ToBase64String(des.Key);
            key = key + "|" + iv;
            //DES加密
            xml = Encrypt(xml, key);
            xml += "@" + key;
            //RC4加密
            Byte[] bts = Encrypt(Encoding.UTF8.GetBytes(xml), InternalKey);
            return Convert.ToBase64String(bts);
        }

        //*****************************************
        //授权读取过程
        //
        //读取->Base64字符串转字节码->RC4用InternalKey解密->字节码转字符串->
        //字符串按@拆为两部分，前数据后密码
        //密码按|分为两部分，前密码后是XCode经RC4加密后的IV
        //DES解密
        //字符串按$拆为两部分，前数据，后签名
        //签名校验
        //
        //*****************************************

        /// <summary>
        /// 授权解码
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static String Decode(String text)
        {
            //解Base64编码
            Byte[] bts = Convert.FromBase64String(text);
            //RC4解密
            bts = Encrypt(bts, InternalKey);
            String key = Encoding.UTF8.GetString(bts);
            //DES解密
            key = Decrypt(key.Substring(0, key.IndexOf("@")), key.Substring(key.IndexOf("@") + 1));
            //验证签名
            String xml = key.Substring(0, key.LastIndexOf("$"));
            key = key.Substring(key.LastIndexOf("$") + 1);
#if !ISMANAGE
            //XCode的解码函数才需要校验签名
            if (!Verify(Encoding.UTF8.GetBytes(xml), key, PublicKey)) return null;
#endif
            return xml;
        }
        #endregion

        #region 读取授权
        /// <summary>
        /// 读取授权。如果成功，则返回授权；否则返回试用授权。
        /// 不需要建立文件监视，因为如果授权文件改变，程序域将会重启。
        /// </summary>
        /// <returns></returns>
        private static LicenseInfo GetLicense()
        {
            try
            {
                String key = ReadLicense();
                //无法读出授权信息，以试用版方式运行。
                if (String.IsNullOrEmpty(key))
                {
                    _Current = LicenseInfo.Trial;
                    return _Current;
                }

                String xml = Decode(key);
                if (String.IsNullOrEmpty(xml))
                {
                    ShowErr("非法授权文件！");
                    _Current = LicenseInfo.Trial;
                    return _Current;
                }
                _Current = LicenseInfo.FromXML(xml);

                //马上检查文件签名授权
                if (!CheckFileSign(_Current))
                {
                    ShowErr("本授权只针对特定版本的XCode，请不要使用其它版本的XCode！");
                    _Current = LicenseInfo.Trial;
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.ToString());
                _Current = LicenseInfo.Trial;
            }
            //放在后面返回，任何错误时都能将其设置为试用版。
            return _Current;
        }

        /// <summary>
        /// 读取授权文件
        /// </summary>
        /// <returns></returns>
        private static String ReadLicense()
        {
            // 获取授权文件路径
            String path = GetPath();
            // 读取授权文件
            String filepath;//= Path.Combine(path, Environment.MachineName + ".lic");
            //如果不存在针对本机的授权文件，则使用通用授权文件
            //if (!File.Exists(filepath)) filepath = Path.Combine(path, "X.lic");
            //写入的时候写机器名，读取的时候读X.lic
            filepath = Path.Combine(path, "X.lic");

            //没有授权文件，准备写机器码文件
            if (!File.Exists(filepath))
            {
                filepath = Path.Combine(path, Environment.MachineName + "授权申请.lic");
                if (!File.Exists(filepath))
                {
                    MakeTrail(filepath);
                }
                return null;
            }

            String key = String.Empty;
            //using (StreamReader sr = new StreamReader(filepath, Encoding.UTF8))
            //{
            //    key = sr.ReadToEnd();
            //    sr.Close();
            //}
            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                Byte[] bts = new Byte[fs.Length];
                fs.Read(bts, 0, bts.Length);
                key = Encoding.UTF8.GetString(bts);
            }
            return key;
        }

        internal static String GetPath()
        {
            String path;
            if (HttpContext.Current != null)
            {
                //path = HttpContext.Current.Server.MapPath("~/");
                path = HttpContext.Current.Server.MapPath("~/Bin");
            }
            else
            {
                path = AppDomain.CurrentDomain.BaseDirectory;
                if (!String.IsNullOrEmpty(AppDomain.CurrentDomain.RelativeSearchPath))
                    path = Path.Combine(path, AppDomain.CurrentDomain.RelativeSearchPath);
            }
            return path;
        }

        internal static void MakeTrail(String filepath)
        {
            LicenseInfo info = LicenseInfo.Trial;
            info.ReadHardInfo();
            //写入当前URL
            if (HttpContext.Current != null)
            {
                info.WebSiteList.Enable = true;
                info.WebSiteList = HttpContext.Current.Request.Url.ToString();
            }
            String path = GetPath();
            path = Path.Combine(path, "XCode.dll");
            if (File.Exists(path))
            {
                info.FileSignature.Enable = true;
                Byte[] data = File.ReadAllBytes(path);
                if (data != null)
                {
                    MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                    data = md5.ComputeHash(data);
                    info.FileSignature = Convert.ToBase64String(data);
                }
            }
            String content = info.ToXML();
            //Byte[] bts = Encrypt(Encoding.UTF8.GetBytes(content), InternalKey);
            content = Encode(content);
            Byte[] bts = Encoding.UTF8.GetBytes(content);
            //content = Convert.ToBase64String(bts);
            //using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.UTF8))
            //{
            //    sw.Write(content);
            //    sw.Close();
            //}
            using (FileStream fs = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.Write(bts, 0, bts.Length);
                fs.SetLength(bts.Length);
            }
        }
        #endregion

        #region DES加密解密
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="data">待加密数据</param>
        /// <param name="key">密码</param>
        /// <returns></returns>
        private static String Encrypt(String data, String key)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            String iv = key.Substring(key.IndexOf("|") + 1);
            des.Key = Convert.FromBase64String(key.Substring(0, key.IndexOf("|")));
            des.IV = Encrypt(Convert.FromBase64String(iv), "XCode");
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Read);
            //最大10K
            Byte[] bts = new Byte[10240];
            Int32 count = cs.Read(bts, 0, bts.Length);
            return Convert.ToBase64String(bts, 0, count);
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="data">待解密数据</param>
        /// <param name="key">密码</param>
        /// <returns></returns>
        internal static String Decrypt(String data, String key)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            String iv = key.Substring(key.IndexOf("|") + 1);
            des.Key = Convert.FromBase64String(key.Substring(0, key.IndexOf("|")));
            des.IV = Encrypt(Convert.FromBase64String(iv), "XCode");
            MemoryStream ms = new MemoryStream(Convert.FromBase64String(data));
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Read);
            //最大10K
            Byte[] bts = new Byte[10240];
            Int32 count = cs.Read(bts, 0, bts.Length);
            return Encoding.UTF8.GetString(bts, 0, count);
        }
        #endregion

        #region RC4加密
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="pass">密码</param>
        /// <returns></returns>
        internal static Byte[] Encrypt(Byte[] data, String pass)
        {
            if (data == null || pass == null) return null;
            Byte[] output = new Byte[data.Length];
            Int64 i = 0;
            Int64 j = 0;
            Byte[] mBox = GetKey(Encoding.UTF8.GetBytes(pass), 256);

            // 加密
            for (Int64 offset = 0; offset < data.Length; offset++)
            {
                i = (i + 1) % mBox.Length;
                j = (j + mBox[i]) % mBox.Length;
                Byte temp = mBox[i];
                mBox[i] = mBox[j];
                mBox[j] = temp;
                Byte a = data[offset];
                //Byte b = mBox[(mBox[i] + mBox[j] % mBox.Length) % mBox.Length];
                // mBox[j] 一定比 mBox.Length 小，不需要在取模
                Byte b = mBox[(mBox[i] + mBox[j]) % mBox.Length];
                output[offset] = (Byte)((Int32)a ^ (Int32)b);
            }

            return output;
        }

        /// <summary>
        /// 打乱密码
        /// </summary>
        /// <param name="pass">密码</param>
        /// <param name="kLen">密码箱长度</param>
        /// <returns>打乱后的密码</returns>
        internal static Byte[] GetKey(Byte[] pass, Int32 kLen)
        {
            Byte[] mBox = new Byte[kLen];

            for (Int64 i = 0; i < kLen; i++)
            {
                mBox[i] = (Byte)i;
            }
            Int64 j = 0;
            for (Int64 i = 0; i < kLen; i++)
            {
                j = (j + mBox[i] + pass[i % pass.Length]) % kLen;
                Byte temp = mBox[i];
                mBox[i] = mBox[j];
                mBox[j] = temp;
            }
            return mBox;
        }
        #endregion

        #region RSA签名
        /// <summary>
        /// 签名
        /// </summary>
        /// <param name="data"></param>
        /// <param name="priKey"></param>
        /// <returns></returns>
        internal static String Sign(Byte[] data, String priKey)
        {
            if (data == null | String.IsNullOrEmpty(priKey)) return null;

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            try
            {
                rsa.FromXmlString(priKey);
                return Convert.ToBase64String(rsa.SignHash(md5.ComputeHash(data), "1.2.840.113549.2.5"));
            }
            catch { return null; }
        }
        #endregion

        #region RSA验证签名
        /// <summary>
        /// 验证签名
        /// </summary>
        /// <param name="data">待验证的数据</param>
        /// <param name="signdata">签名</param>
        /// <param name="pubKey">公钥</param>
        /// <returns></returns>
        internal static Boolean Verify(Byte[] data, String signdata, String pubKey)
        {
            if (data == null ||
                data.Length < 1 ||
                String.IsNullOrEmpty(signdata) ||
                String.IsNullOrEmpty(pubKey)) return false;

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            try
            {
                rsa.FromXmlString(pubKey);
                return rsa.VerifyHash(md5.ComputeHash(data), "1.2.840.113549.2.5", Convert.FromBase64String(signdata));
            }
            catch { return false; }
        }
        #endregion

        #region 许可检查
        /// <summary>
        /// 检查授权许可证。
        /// 每次请求这里都会执行一次，所以要注意性能问题。
        /// </summary>
        /// <returns>是否有效授权</returns>
        public static Boolean Check()
        {
            //Trace.WriteLine("设置：Check");
            //检查硬件是否匹配
            //if (!Current.CheckHardInfo())
            //{
            //    ShowErr("机器硬件已被修改。");
            //    return false;
            //}
            //检查机器名
            if (Current.MachineName.Enable && Current.MachineName != HardInfo.MachineName)
            {
                ShowErr(String.Format("你只能在名为{0}的机器上使用该授权。", Current.MachineName));
                return false;
            }
            //检查主板
            if (Current.BaseBoard.Enable && Current.BaseBoard != HardInfo.BaseBoard)
            {
                ShowErr("你只能在指定主板上使用该授权。");
                return false;
            }
            //检查CPU
            if (Current.Processors.Enable && Current.Processors != HardInfo.Processors)
            {
                ShowErr("你只能在指定处理器上使用该授权。");
                return false;
            }
            //检查驱动器
            if (Current.DiskDrives.Enable && Current.DiskDrives != HardInfo.DiskDrives)
            {
                ShowErr("你只能在指定驱动器上使用该授权。请不要把程序移动到别的盘来使用，也不要格式化原授权盘。");
                return false;
            }
            //检查网卡
            if (Current.Macs.Enable && Current.Macs != HardInfo.Macs)
            {
                ShowErr("你只能在指定网卡上使用该授权。");
                return false;
            }
            //检查IP地址
            if (Current.IPs.Enable && Current.IPs != HardInfo.IPs)
            {
                ShowErr(String.Format("你只能在IP为{0}的机器上使用该授权。", Current.IPs));
                return false;
            }
            //检查执行时间
            if (Current.EvaluationPeriod.Enable)
            {
                TimeSpan ts = DateTime.Now - StartTime;
                if (ts.TotalMinutes > Current.EvaluationPeriod.IntVal)
                {
                    ShowErr(String.Format("已执行时间：{0}分钟，超过授权数：{1}分钟。", (Int32)(ts.TotalMinutes), Current.EvaluationPeriod.IntVal));
                    return false;
                }
            }
            //检查有效期
            if (Current.ExpirationDate.Enable)
            {
                if (DateTime.Now > Current.ExpirationDate.DateTimeVal)
                {
                    ShowErr("授权已过期！");
                    return false;
                }
                DateTime dt = new DateTime(2010, 07, 09);
                //检查时间，可能经过调整
                if (DateTime.Now < dt)
                {
                    ShowErr("系统时间无效！");
                    return false;
                }
            }
            //检查IP数
            if (Current.MaxIP.Enable && HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                //如果不是HTTP，直接返回验证失败
                //if (HttpContext.Current == null) return false;
                String cip = HttpContext.Current.Request.UserHostAddress;
                if (!CheckList(IPList, cip, Current.MaxIP.IntVal))
                {
                    ShowErr(String.Format("当前在线数是：{0}，已超过授权数：{1}。", IPList.Count, Current.MaxIP.IntVal));
                    return false;
                }
            }
            //检查会话数
            if (Current.MaxSession.Enable && HttpContext.Current != null && HttpContext.Current.Session != null)
            {
                //如果不是HTTP，直接返回验证失败
                //if (HttpContext.Current == null || HttpContext.Current.Session == null) return false;
                //建立一个Session值，告诉aspnet，应用程序已经使用Session了，不要每次都改变SessionID了。
                HttpContext.Current.Session["XCode_Session_Holder"] = "该Session值仅仅是为了维持SessionID不变！";
                String cid = HttpContext.Current.Session.SessionID;
                if (!CheckList(SessionList, cid, Current.MaxSession.IntVal))
                {
                    ShowErr(String.Format("当前会话数是：{0}，已超过授权数：{1}。", SessionList.Count, Current.MaxSession.IntVal));
                    return false;
                }
            }
            //检查可用站点列表
            if (Current.WebSiteList.Enable && HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                Uri uri = HttpContext.Current.Request.Url;
                String url = uri.ToString().ToLower();
                Boolean pass = false;
                if (LocalFilters.Contains(uri.Host.ToLower())) pass = true;
                if (!pass && Filters.Count > 0)
                {
                    foreach (String item in Filters)
                    {
                        if (url.Contains(item))
                        {
                            pass = true;
                            break;
                        }
                    }
                }
                if (!pass)
                {
                    ShowErr(String.Format("当前请求页{0}不在许可站点列表之中！", uri.ToString()));
                    return false;
                }
            }

            return true;
        }

        private static Boolean CheckList(Dictionary<String, DateTime> List, String item, Int32 max)
        {
            //加入当前item
            if (!List.ContainsKey(item))
            {
                //加双锁，用于多线程
                lock (ListLockObj)
                {
                    if (!List.ContainsKey(item)) List.Add(item, DateTime.Now);
                }
            }
            if (List.Count < max) return true;

            //超过限制，试试删除无效的。超过限制时才删除，这样有利于提高性能
            lock (ListLockObj)
            {
                if (List.Count > max)
                {
                    //每次都遍历，太浪费资源了
                    List<String> todel = new List<String>();
                    foreach (String ip in List.Keys)
                    {
                        if (List[ip].AddMinutes(1) < DateTime.Now) todel.Add(ip);
                    }
                    foreach (String ip in todel)
                    {
                        List.Remove(ip);
                    }
                }
            }
            //没办法了，真的超出了
            if (List.Count > max) return false;
            return true;
        }

        /// <summary>
        /// 检查文件签名
        /// </summary>
        /// <param name="li"></param>
        /// <returns></returns>
        private static Boolean CheckFileSign(LicenseInfo li)
        {
            if (li == null) return false;
            //未启用该限制
            if (!li.FileSignature.Enable) return true;
            if (String.IsNullOrEmpty(li.FileSignature.Value)) return false;
            // 获取授权文件路径
            string path = typeof(License).Module.FullyQualifiedName;
            // 读取授权文件
            string filepath = Path.GetDirectoryName(path) + @"\XCode.dll";
            Byte[] bts;
            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                bts = new Byte[fs.Length];
                fs.Read(bts, 0, bts.Length);
                fs.Close();
            }

            if (!Verify(bts, li.FileSignature, PublicKey)) return false;

            return true;
        }
        #endregion

        #region 错误信息
        /// <summary>
        /// 授权无效时显示错误信息，同时终止Http处理
        /// </summary>
        /// <param name="msg"></param>
        public static void ShowErr(String msg)
        {
            if (HttpContext.Current != null)
            {
                try
                {
                    HttpResponse res = HttpContext.Current.Response;
                    res.Clear();
                    res.Write("你好 <font color=\"red\">" + (_Current != null ? _Current.EndUser.FormatedValue : "未知用户") + "</font>，这是一个无效授权！<BR>");
                    res.Write(msg);
                    res.Write("<BR>请联系 <font color=\"blue\">" + (_Current != null ? _Current.UserName.FormatedValue : "未知用户") + "</font> 购买合适的授权！");
                    res.Flush();
                    res.End();
                }
                catch (ThreadAbortException ex) { throw new Exception("", ex); }
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("你好 ");
                sb.Append((_Current != null ? _Current.EndUser.FormatedValue : "未知用户"));
                sb.Append("，这是一个无效授权！");
                sb.AppendLine();
                sb.AppendLine(msg);
                sb.Append("请联系 ");
                sb.Append((_Current != null ? _Current.UserName.FormatedValue : "未知用户"));
                sb.Append(" 购买合适的授权！");
                throw new InvalidOperationException(sb.ToString());
            }
        }
        #endregion
    }
#endif
}