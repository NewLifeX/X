using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Runtime.CompilerServices;
using System.Configuration;
using System.Collections.Specialized;

namespace Test
{
    internal class OraTrace
    {
        private const int DEFAULT_STMT_CACHE_SIZE = 0;
        internal const uint LEVEL_CONPOOL = 2;
        internal const uint LEVEL_ENTRY = 1;
        internal const uint LEVEL_EXIT = 1;
        internal const uint LEVEL_GRID_CR = 0x10;
        internal const uint LEVEL_GRID_RLB = 0x20;
        internal const uint LEVEL_MINIDUMP = 8;
        internal const uint LEVEL_MTS = 4;
        internal const uint LEVEL_NONE = 0;
        internal const uint LEVEL_SQL = 1;
        internal const uint LEVEL_TUNING = 0x40;
        internal static string m_appEdition;
        internal static NameValueCollection m_configSection;
        internal static bool m_configSectionRead;
        internal static int m_DBNotificationPort = -1;
        internal static int m_DBNotificationRegInterval = 0;
        internal static int m_demandOrclPermission = 0;
        internal static int m_fetchArrayPooling = 1;
        internal static int m_FetchSize = 0x20000;
        private static int m_maxStatementCacheSize = 100;
        private static object m_maxStatementCacheSizeLock = new object();
        internal static int m_MetadataPooling = 1;
        internal static string m_MetaDataXml;
        //internal static PerfCounterLevel m_PerformanceCounters;
        internal static int m_PSPE = 1;
        internal static bool m_RegistryRead;
        internal static object m_regReadSync = new object();
        internal static bool m_selfTuning = true;
        internal static int m_StmtCacheSize = 0;
        internal static int m_threadPoolMaxSize = -1;
        internal static uint m_TraceLevel = 0;

        internal OraTrace() { }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal static void CreateMiniDump(object state)
        {
            //MiniDumpInfo info = (MiniDumpInfo)state;
            //try
            //{
            //    OpsTrace.CreateMiniDump(info.threadId, info.pExPtrs);
            //}
            //catch (Exception exception)
            //{
            //    Trace(1, new string[] { " (ERROR) CreateMiniDump: " + exception.GetType().ToString() + ": " + exception.Message + "\n" });
            //}
            //info.evt.Set();
        }

        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true), ConfigurationPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static void GetConfigInfo()
        {
            if (!m_configSectionRead)
            {
                m_configSection = ConfigurationManager.GetSection("oracle.dataaccess.client") as NameValueCollection;
                m_configSectionRead = true;
            }
            try
            {
                string s = null;
                string str2 = null;
                string str3 = null;
                string str4 = null;
                string str5 = null;
                string strA = null;
                string str7 = null;
                string str8 = null;
                string str9 = null;
                string strTraceFileName = null;
                string str11 = null;
                string str12 = null;
                string str13 = null;
                uint chkConStatus = 0;
                uint dynamicEnlist = 0;
                int ociEvnts = 0;
                int perfCounters = 0;
                int stmtCacheWithUdts = 0;
                uint traceOption = 0;
                uint udtCacheSize = 0;
                if (m_configSection != null)
                {
                    s = m_configSection["CheckConStatus"];
                    str2 = m_configSection["DynamicEnlistment"];
                    str3 = m_configSection["FetchSize"];
                    str4 = m_configSection["OCI_EVENTS"];
                    str5 = m_configSection["PerformanceCounters"];
                    strA = m_configSection["PromotableTransaction"];
                    str7 = m_configSection["StatementCacheSize"];
                    str8 = m_configSection["StatementCacheWithUdts"];
                    str9 = m_configSection["ThreadPoolMaxSize"];
                    strTraceFileName = m_configSection["TraceFileName"];
                    str11 = m_configSection["TraceLevel"];
                    str12 = m_configSection["TraceOption"];
                    str13 = m_configSection["UdtCacheSize"];
                    if (s != null)
                    {
                        string[] strArray = s.Split(new char[] { ',' });
                        if (strArray.Length > 1) s = strArray[strArray.Length - 1];
                    }
                    if (str2 != null)
                    {
                        string[] strArray2 = str2.Split(new char[] { ',' });
                        if (strArray2.Length > 1) str2 = strArray2[strArray2.Length - 1];
                    }
                    if (str3 != null)
                    {
                        string[] strArray3 = str3.Split(new char[] { ',' });
                        if (strArray3.Length > 1) str3 = strArray3[strArray3.Length - 1];
                    }
                    if (str4 != null)
                    {
                        string[] strArray4 = str4.Split(new char[] { ',' });
                        if (strArray4.Length > 1) str4 = strArray4[strArray4.Length - 1];
                    }
                    if (str5 != null)
                    {
                        string[] strArray5 = str5.Split(new char[] { ',' });
                        if (strArray5.Length > 1) str5 = strArray5[strArray5.Length - 1];
                    }
                    if (strA != null)
                    {
                        string[] strArray6 = strA.Split(new char[] { ',' });
                        if (strArray6.Length > 1) strA = strArray6[strArray6.Length - 1];
                    }
                    if (str7 != null)
                    {
                        string[] strArray7 = str7.Split(new char[] { ',' });
                        if (strArray7.Length > 1) str7 = strArray7[strArray7.Length - 1];
                    }
                    if (str8 != null)
                    {
                        string[] strArray8 = str8.Split(new char[] { ',' });
                        if (strArray8.Length > 1) str8 = strArray8[strArray8.Length - 1];
                    }
                    if (str9 != null)
                    {
                        string[] strArray9 = str9.Split(new char[] { ',' });
                        if (strArray9.Length > 1) str9 = strArray9[strArray9.Length - 1];
                    }
                    if (strTraceFileName != null)
                    {
                        string[] strArray10 = strTraceFileName.Split(new char[] { ',' });
                        if (strArray10.Length > 1) strTraceFileName = strArray10[strArray10.Length - 1];
                    }
                    if (str11 != null)
                    {
                        string[] strArray11 = str11.Split(new char[] { ',' });
                        if (strArray11.Length > 1) str11 = strArray11[strArray11.Length - 1];
                    }
                    if (str12 != null)
                    {
                        string[] strArray12 = str12.Split(new char[] { ',' });
                        if (strArray12.Length > 1) str12 = strArray12[strArray12.Length - 1];
                    }
                    if (str13 != null)
                    {
                        string[] strArray13 = str13.Split(new char[] { ',' });
                        if (strArray13.Length > 1) str13 = strArray13[strArray13.Length - 1];
                    }
                    if (s != null && s != string.Empty)
                        chkConStatus = uint.Parse(s);
                    else
                        s = null;
                    if (str2 != null && str2 != string.Empty)
                        dynamicEnlist = uint.Parse(str2);
                    else
                        str2 = null;
                    if (str3 != null && str3 != string.Empty)
                        m_FetchSize = int.Parse(str3);
                    else
                        str3 = null;
                    if (str4 != null && str4 != string.Empty)
                        ociEvnts = int.Parse(str4);
                    else
                        str4 = null;
                    if (str5 != null && str5 != string.Empty)
                    {
                        perfCounters = int.Parse(str5);
                        if (perfCounters < 0) perfCounters = 0;
                        //m_PerformanceCounters = (PerfCounterLevel)perfCounters;
                    }
                    else
                        str5 = null;
                    if (strA != null && strA != string.Empty)
                    {
                        if (string.Compare(strA, "local", true) == 0) m_PSPE = 0;
                    }
                    else
                        strA = null;
                    if (str7 != null && str7 != string.Empty)
                        m_StmtCacheSize = int.Parse(str7);
                    else
                        str7 = null;
                    if (str8 != null && str8 != string.Empty)
                        stmtCacheWithUdts = int.Parse(str8);
                    else
                        str8 = null;
                    if (str9 != null && str9 != string.Empty)
                        m_threadPoolMaxSize = int.Parse(str9);
                    else
                        str9 = null;
                    if (strTraceFileName == string.Empty) strTraceFileName = null;
                    if (str11 != null && str11 != string.Empty)
                        m_TraceLevel = uint.Parse(str11);
                    else
                        str11 = null;
                    if (str12 != null && str12 != string.Empty)
                        traceOption = uint.Parse(str12);
                    else
                        str12 = null;
                    if (str13 != null && str13 != string.Empty)
                        udtCacheSize = uint.Parse(str13);
                    else
                        str13 = null;
                }
                //OpsTrace.SyncInfo(s, str2, str3, str4, str5, strA, str7, str8, str9, strTraceFileName, str11, str12, str13, chkConStatus, dynamicEnlist, ref m_FetchSize, ociEvnts, ref perfCounters, ref m_PSPE, ref m_StmtCacheSize, stmtCacheWithUdts, ref m_threadPoolMaxSize, ref m_TraceLevel, traceOption, udtCacheSize, ref m_fetchArrayPooling);
                if (str5 == null)
                {
                    if (perfCounters < 0) perfCounters = 0;
                    //m_PerformanceCounters = (PerfCounterLevel)perfCounters;
                }
                if (m_configSection != null)
                {
                    string str14 = m_configSection["DbNotificationPort"];
                    if (str14 != null)
                    {
                        string[] strArray14 = str14.Split(new char[] { ',' });
                        if (strArray14.Length > 1) str14 = strArray14[strArray14.Length - 1];
                    }
                    if (str14 != null && str14 != string.Empty) m_DBNotificationPort = int.Parse(str14);
                    m_MetaDataXml = m_configSection["MetaDataXml"];
                    if (m_MetaDataXml != null)
                    {
                        string[] strArray15 = m_MetaDataXml.Split(new char[] { ',' });
                        if (strArray15.Length > 1) m_MetaDataXml = strArray15[strArray15.Length - 1];
                    }
                }
                if (m_MetaDataXml != null && m_MetaDataXml != string.Empty)
                    Trace(1, new string[] { " (CONFIG) (MetaDataXml : %s)\n", m_MetaDataXml });
                else
                    Trace(1, new string[] { " (CONFIG) (MetaDataXml : %s)\n", "<none>" });
                Trace(1, new string[] { " (CONFIG) (DbNotificationPort : %s)\n", m_DBNotificationPort.ToString() });
                string str15 = " (%s) (ThreadPoolMaxSize : %s [Original: %s; Set: %s; Post-Set: %s])\n";
                string str16 = null;
                if (str9 != null && str9 != string.Empty)
                    str16 = "CONFIG";
                else
                    str16 = "REGISTRY";
                uint maxWorkerThreads = 0;
                uint maxIOCompletionThreads = 0;
                //CThreadPool.GetMaxThreads(out maxWorkerThreads, out maxIOCompletionThreads);
                //if (m_threadPoolMaxSize > 0 && m_threadPoolMaxSize != maxWorkerThreads) CThreadPool.SetMaxThreads((uint)m_threadPoolMaxSize, maxIOCompletionThreads);
                uint num10 = 0;
                //CThreadPool.GetMaxThreads(out num10, out maxIOCompletionThreads);
                Trace(1, new string[] { str15, str16, m_threadPoolMaxSize.ToString(), maxWorkerThreads.ToString(), m_threadPoolMaxSize.ToString(), num10.ToString() });
                Trace(1, new string[] { " (%s) (DllPath : %s)\n", OracleInit.s_bFromConfigFileDP ? "CONFIG" : "REGISTRY", OracleInit.s_DllPath });
                Trace(1, new string[] { " (%s) (MetadataPooling : %s)\n", OracleInit.s_bFromConfigFileMP ? "CONFIG" : "REGISTRY", m_MetadataPooling.ToString() });
                Trace(1, new string[] { " (%s) (AppEdition : %s)\n", OracleInit.s_bFromConfigFileMP ? "CONFIG" : "REGISTRY", m_appEdition });
                Trace(1, new string[] { " (%s) (DemandOraclePermission  : %s)\n", OracleInit.s_bFromConfigFileOrclPerm ? "CONFIG" : "REGISTRY", m_demandOrclPermission.ToString() });
                Trace(1, new string[] { " (%s) (Self Tuning  : %s)\n", OracleInit.s_bFromConfigFileSelfTuning ? "CONFIG" : "REGISTRY", m_selfTuning.ToString() });
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (m_configSection != null)
                {
                    m_configSection.Remove("CheckConStatus");
                    m_configSection.Remove("DbNotificationPort");
                    m_configSection.Remove("DbNotificationRegInterval");
                    m_configSection.Remove("DllPath");
                    m_configSection.Remove("DynamicEnlistment");
                    m_configSection.Remove("FetchSize");
                    m_configSection.Remove("MetadataPooling");
                    m_configSection.Remove("DemandOraclePermission");
                    m_configSection.Remove("SelfTuning");
                    m_configSection.Remove("MaxStatementCacheSize");
                    m_configSection.Remove("Edition");
                    m_configSection.Remove("MetaDataXml");
                    m_configSection.Remove("OCI_EVENTS");
                    m_configSection.Remove("PerformanceCounters");
                    m_configSection.Remove("PromotableTransaction");
                    m_configSection.Remove("StatementCacheSize");
                    m_configSection.Remove("StatementCacheWithUdts");
                    m_configSection.Remove("ThreadPoolMaxSize");
                    m_configSection.Remove("TraceFileName");
                    m_configSection.Remove("TraceLevel");
                    m_configSection.Remove("TraceOption");
                    m_configSection.Remove("UdtCacheSize");
                }
                m_RegistryRead = true;
            }
        }

        internal static void InitializeMaxStatementCacheSize(int newMaxStatementCacheSize) { m_maxStatementCacheSize = newMaxStatementCacheSize; }

        internal static void SetMaxStatementCacheSize(int newMaxStatementCacheSize)
        {
            if (newMaxStatementCacheSize < m_maxStatementCacheSize)
            {
                lock (m_maxStatementCacheSizeLock)
                {
                    if (newMaxStatementCacheSize < m_maxStatementCacheSize)
                    {
                        if (m_TraceLevel != 0) Trace(0x40, new string[] { string.Concat(new object[] { " (INFO) OraTrace::SetMaxStatementCacheSize(): Max Statement Cache Size changed from ", m_maxStatementCacheSize, " to ", newMaxStatementCacheSize, "\n" }) });
                        m_maxStatementCacheSize = newMaxStatementCacheSize;
                    }
                }
            }
        }

        internal static void Trace(uint TraceLevel, params string[] args)
        {
            if ((TraceLevel & m_TraceLevel) == TraceLevel)
            {
                try
                {
                    //OpsTrace.Trace(TraceLevel, args);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        internal static void TraceExceptionInfo(Exception ex) { TraceExceptionInfo(ex, true); }

        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
        internal static void TraceExceptionInfo(Exception ex, bool bCreateMiniDump)
        {
            if (ex is ThreadAbortException) bCreateMiniDump = false;
            int lastErrorCode = 0;
            int exceptionCode = 0;
            if (bCreateMiniDump)
            {
                try
                {
                    //OpsTrace.GetLastErrorCode(out lastErrorCode);
                }
                catch (Exception exception)
                {
                    Trace(1, new string[] { " (ERROR) GetLastErrorCode: " + exception.GetType().ToString() + ": " + exception.ToString() + "\n" });
                }
                try
                {
                    exceptionCode = Marshal.GetExceptionCode();
                }
                catch (Exception exception2)
                {
                    Trace(1, new string[] { " (ERROR) Marshal.GetExceptionCode: " + exception2.GetType().ToString() + ": " + exception2.ToString() + "\n" });
                }
                //MiniDumpInfo state = new MiniDumpInfo();
                //state.threadId = AppDomain.GetCurrentThreadId();
                //state.pExPtrs = Marshal.GetExceptionPointers();
                //ThreadPool.QueueUserWorkItem(new WaitCallback(OraTrace.CreateMiniDump), state);
                //state.evt.WaitOne();
                Trace(1, new string[] { " (EXCPT) Lvl0: (Type=" + ex.GetType().ToString() + ") (Msg=" + ex.Message + ") (Win32Err=" + lastErrorCode.ToString() + ") (Code=" + exceptionCode.ToString("x") + ") (Stack=" + ex.StackTrace + ")\n" });
            }
            else
                Trace(1, new string[] { " (EXCPT) Lvl0: (Type=" + ex.GetType().ToString() + ") (Msg=" + ex.Message + ") (Stack=" + ex.StackTrace + ")\n" });
            Exception innerException = ex.InnerException;
            int num3 = 1;
            while (innerException != null)
            {
                if (num3 > 9) return;
                if (bCreateMiniDump)
                    Trace(1, new string[] { " (EXCPT) Lvl" + num3.ToString() + ": (Type=" + ex.GetType().ToString() + ") (Msg=" + ex.Message + ") (Win32Err=" + lastErrorCode.ToString() + ") (Code=" + exceptionCode.ToString("x") + ") (Stack=" + ex.StackTrace + ")\n" });
                else
                    Trace(1, new string[] { " (EXCPT) Lvl" + num3.ToString() + ": (Type=" + ex.GetType().ToString() + ") (Msg=" + ex.Message + ") (Stack=" + ex.StackTrace + ")\n" });
                innerException = innerException.InnerException;
                num3++;
            }
            return;
        }

        internal static int MaxStatementCacheSize { get { return m_maxStatementCacheSize; } }
    }
}
