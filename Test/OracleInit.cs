using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Security.Permissions;
using System.Reflection;
using Oracle.DataAccess.Client;
using System.Collections.Specialized;
using System.Configuration;
using Microsoft.Win32;

namespace Test
{
    [RegistryPermission(SecurityAction.Assert, Unrestricted = true)]
    internal class OracleInit
    {
        public static bool bSetDllDirectoryInvoked = false;
        private static OperatingSystem os = Environment.OSVersion;
        private static int m_nMajorVer = os.Version.Major;
        private static int m_nMinorVer = os.Version.Minor;
        private static Timer m_timer;
        public static bool s_bFromConfigFileAppEdition = false;
        public static bool s_bFromConfigFileDbNRI = false;
        public static bool s_bFromConfigFileDP = false;
        public static bool s_bFromConfigFileMaxStatementCacheSize = false;
        public static bool s_bFromConfigFileMP = false;
        public static bool s_bFromConfigFileOrclPerm = false;
        public static bool s_bFromConfigFileSelfTuning = false;
        public static string s_DllPath;
        private static object s_lockObj = new object();

        private static string GetAssemblyVersion()
        {
            string fullName = Assembly.GetAssembly(typeof(OracleConnection)).FullName;
            int startIndex = fullName.IndexOf("Version=") + 8;
            int index = fullName.IndexOf(",", startIndex);
            if (index > startIndex && startIndex > 0) return fullName.Substring(startIndex, index - startIndex);
            return null;
        }

        [ConfigurationPermission(SecurityAction.Assert, Unrestricted = true)]
        private static string GetDllDirectory(string version)
        {
            OraTrace.m_configSection = ConfigurationManager.GetSection("oracle.dataaccess.client") as NameValueCollection;
            OraTrace.m_configSectionRead = true;
            if (OraTrace.m_configSection != null)
            {
                string str = OraTrace.m_configSection["Edition"];
                if (str != null)
                {
                    string[] strArray = str.Split(new char[] { ',' });
                    if (strArray.Length > 1) str = strArray[strArray.Length - 1];
                }
                if (str != null && str != string.Empty)
                {
                    OraTrace.m_appEdition = str;
                    s_bFromConfigFileAppEdition = true;
                }
                string s = OraTrace.m_configSection["MetadataPooling"];
                if (s != null)
                {
                    string[] strArray2 = s.Split(new char[] { ',' });
                    if (strArray2.Length > 1) s = strArray2[strArray2.Length - 1];
                }
                if (s != null && s != string.Empty)
                {
                    OraTrace.m_MetadataPooling = int.Parse(s);
                    s_bFromConfigFileMP = true;
                }
                string str3 = OraTrace.m_configSection["DbNotificationRegInterval"];
                if (str3 != null)
                {
                    string[] strArray3 = str3.Split(new char[] { ',' });
                    if (strArray3.Length > 1) str3 = strArray3[strArray3.Length - 1];
                }
                if (str3 != null && str3 != string.Empty)
                {
                    OraTrace.m_DBNotificationRegInterval = int.Parse(str3);
                    s_bFromConfigFileDbNRI = true;
                }
                string str4 = OraTrace.m_configSection["DemandOraclePermission"];
                if (str4 != null)
                {
                    string[] strArray4 = str4.Split(new char[] { ',' });
                    if (strArray4.Length > 1) str4 = strArray4[strArray4.Length - 1];
                }
                if (str4 != null && str4 != string.Empty)
                {
                    OraTrace.m_demandOrclPermission = int.Parse(str4);
                    s_bFromConfigFileOrclPerm = true;
                }
                string str5 = OraTrace.m_configSection["SelfTuning"];
                if (str5 != null)
                {
                    string[] strArray5 = str5.Split(new char[] { ',' });
                    if (strArray5.Length > 1) str5 = strArray5[strArray5.Length - 1];
                }
                if (str5 != null && str5 != string.Empty)
                {
                    OraTrace.m_selfTuning = Convert.ToBoolean(int.Parse(str5));
                    s_bFromConfigFileSelfTuning = true;
                }
                string str6 = OraTrace.m_configSection["MaxStatementCacheSize"];
                if (str6 != null)
                {
                    string[] strArray6 = str6.Split(new char[] { ',' });
                    if (strArray6.Length > 1) str6 = strArray6[strArray6.Length - 1];
                }
                if (str6 != null && str6 != string.Empty)
                {
                    OraTrace.InitializeMaxStatementCacheSize(int.Parse(str6));
                    s_bFromConfigFileMaxStatementCacheSize = true;
                }
                string str7 = OraTrace.m_configSection["DllPath"];
                if (str7 != null)
                {
                    string[] strArray7 = str7.Split(new char[] { ',' });
                    if (strArray7.Length > 1) str7 = strArray7[strArray7.Length - 1];
                }
                if (str7 != null && str7 != string.Empty)
                {
                    s_DllPath = str7;
                    s_bFromConfigFileDP = true;
                }
            }
            RegistryKey key2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Oracle\ODP.NET");
            if (key2 != null)
            {
                string[] subKeyNames = key2.GetSubKeyNames();
                for (int i = 0; i < subKeyNames.Length; i++)
                {
                    if (version == subKeyNames[i])
                    {
                        RegistryKey key3 = key2.OpenSubKey(version);
                        if (key3 != null)
                        {
                            if (!s_bFromConfigFileMP)
                            {
                                string str8 = key3.GetValue("MetadataPooling") as string;
                                if (str8 != null) OraTrace.m_MetadataPooling = int.Parse(str8);
                            }
                            if (!s_bFromConfigFileAppEdition)
                            {
                                string str9 = key3.GetValue("Edition") as string;
                                if (str9 != null) OraTrace.m_appEdition = str9;
                            }
                            if (!s_bFromConfigFileDbNRI)
                            {
                                string str10 = key3.GetValue("DbNotificationRegInterval") as string;
                                if (str10 != null) OraTrace.m_DBNotificationRegInterval = int.Parse(str10);
                            }
                            if (!s_bFromConfigFileOrclPerm)
                            {
                                string str11 = key3.GetValue("DemandOraclePermission") as string;
                                if (str11 != null) OraTrace.m_demandOrclPermission = int.Parse(str11);
                            }
                            if (!s_bFromConfigFileSelfTuning)
                            {
                                string str12 = key3.GetValue("SelfTuning") as string;
                                if (str12 != null) OraTrace.m_selfTuning = Convert.ToBoolean(int.Parse(str12));
                            }
                            if (!s_bFromConfigFileMaxStatementCacheSize)
                            {
                                string str13 = key3.GetValue("MaxStatementCacheSize") as string;
                                if (str13 != null) OraTrace.InitializeMaxStatementCacheSize(int.Parse(str13));
                            }
                            if (!s_bFromConfigFileDP) s_DllPath = key3.GetValue("DllPath") as string;
                        }
                    }
                }
            }
            return s_DllPath;
        }

        public static void Initialize()
        {
            int errCode = -1;
            string assemblyVersion = GetAssemblyVersion();
            if (m_nMajorVer >= 5 && m_nMinorVer > 0 || m_nMajorVer >= 6)
            {
                lock (s_lockObj)
                {
                    if (!bSetDllDirectoryInvoked && assemblyVersion != null && assemblyVersion != string.Empty)
                    {
                        string dllDirectory = GetDllDirectory(assemblyVersion);
                        if (dllDirectory != null && dllDirectory != string.Empty)
                        {
                            try
                            {
                                if (OpsInit.GetFileAttributes(dllDirectory + @"\oci.dll") == -1)
                                {
                                    string fileName = dllDirectory + @"\..\OCI.DLL";
                                    if (OpsInit.GetFileAttributes(fileName) != -1) OpsInit.LoadLibrary(fileName);
                                }
                                errCode = OpsInit.SetDllDirectory(dllDirectory);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            if (!OraTrace.m_RegistryRead)
            {
                lock (OraTrace.m_regReadSync)
                {
                    if (!OraTrace.m_RegistryRead)
                    {
                        bSetDllDirectoryInvoked = true;
                        try
                        {
                            errCode = OpsInit.CheckVersionCompatibility(assemblyVersion);
                            //if (errCode != 0) throw new OracleException(errCode);
                        }
                        catch 
                        {
                            //throw new OracleException(ErrRes.INIT_DLL_VERSION_MISMATCH);
                        }
                        OraTrace.GetConfigInfo();
                        try
                        {
                            TimerCallback callback = new TimerCallback(OracleInit.TimerCallbackFunc);
                            uint dueTime = 0x8cebc16;
                            m_timer = new Timer(callback, null, dueTime, dueTime);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private static void TimerCallbackFunc(object state) { }
    }
}
