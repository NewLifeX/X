﻿using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using RuntimeHelpers = System.Runtime.CompilerServices.RuntimeHelpers;
using SecureString = System.Security.SecureString;

namespace NewLife.Security
{
    /// <summary>证书</summary>
    /// <remarks>http://blogs.msdn.com/b/dcook/archive/2008/11/25/creating-a-self-signed-certificate-in-c.aspx</remarks>
    public class Certificate
    {
        /// <summary>建立自签名证书</summary>
        /// <param name="x500"></param>
        /// <returns></returns>
        public static Byte[] CreateSelfSignCertificatePfx(String x500)
        {
            var dt = DateTime.UtcNow;
            return CreateSelfSignCertificatePfx(x500, dt, dt.AddYears(2), (SecureString)null);
        }

        /// <summary>建立自签名证书</summary>
        /// <param name="x500"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static Byte[] CreateSelfSignCertificatePfx(String x500, DateTime startTime, DateTime endTime) => CreateSelfSignCertificatePfx(x500, startTime, endTime, (SecureString)null);

        /// <summary>建立自签名证书</summary>
        /// <param name="x500"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="insecurePassword"></param>
        /// <returns></returns>
        public static Byte[] CreateSelfSignCertificatePfx(String x500, DateTime startTime, DateTime endTime, String insecurePassword)
        {
            SecureString password = null;

            try
            {
                if (!String.IsNullOrEmpty(insecurePassword))
                {
                    password = new SecureString();
                    foreach (var ch in insecurePassword)
                    {
                        password.AppendChar(ch);
                    }

                    password.MakeReadOnly();
                }

                return CreateSelfSignCertificatePfx(x500, startTime, endTime, password);
            }
            finally
            {
                if (password != null) password.Dispose();
            }
        }

        /// <summary>建立自签名证书</summary>
        /// <param name="x500">例如CN=SelfSignCertificate;C=China;OU=NewLife;O=Development Team;E=nnhy@vip.qq.com，其中CN是显示名</param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static Byte[] CreateSelfSignCertificatePfx(String x500, DateTime startTime, DateTime endTime, SecureString password)
        {
            if (String.IsNullOrEmpty(x500)) x500 = "CN=" + Environment.MachineName;

            //X500DistinguishedNameFlags flag = X500DistinguishedNameFlags.UseUTF8Encoding;
            return CreateSelfSignCertificatePfx(new X500DistinguishedName(x500), startTime, endTime, password);
        }

        /// <summary>建立自签名证书</summary>
        /// <param name="distName"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static Byte[] CreateSelfSignCertificatePfx(X500DistinguishedName distName, DateTime startTime, DateTime endTime, SecureString password)
        {
            var containerName = Guid.NewGuid().ToString();

            var dataHandle = new GCHandle();
            var providerContext = IntPtr.Zero;
            var cryptKey = IntPtr.Zero;
            var certContext = IntPtr.Zero;
            var certStore = IntPtr.Zero;
            var storeCertContext = IntPtr.Zero;
            var passwordPtr = IntPtr.Zero;

#if !NET50
            RuntimeHelpers.PrepareConstrainedRegions();
#endif

            try
            {
                Check(NativeMethods.CryptAcquireContextW(
                    out providerContext,
                    containerName,
                    null,
                    1, // PROV_RSA_FULL
                    8)); // CRYPT_NEWKEYSET

                Check(NativeMethods.CryptGenKey(
                    providerContext,
                    1, // AT_KEYEXCHANGE
                    1, // CRYPT_EXPORTABLE
                    out cryptKey));

                var nameData = distName.RawData;

                dataHandle = GCHandle.Alloc(nameData, GCHandleType.Pinned);
                var nameBlob = new CryptoApiBlob(nameData.Length, dataHandle.AddrOfPinnedObject());

                var kpi = new CryptKeyProviderInformation
                {
                    ContainerName = containerName,
                    ProviderType = 1, // PROV_RSA_FULL
                    KeySpec = 1 // AT_KEYEXCHANGE
                };

                var startSystemTime = ToSystemTime(startTime);
                var endSystemTime = ToSystemTime(endTime);
                certContext = NativeMethods.CertCreateSelfSignCertificate(
                    providerContext,
                    ref nameBlob,
                    0,
                    ref kpi,
                    IntPtr.Zero, // default = SHA1RSA
                    ref startSystemTime,
                    ref endSystemTime,
                    IntPtr.Zero);
                Check(certContext != IntPtr.Zero);
                dataHandle.Free();

                certStore = NativeMethods.CertOpenStore(
                    "Memory", // sz_CERT_STORE_PROV_MEMORY
                    0,
                    IntPtr.Zero,
                    0x2000, // CERT_STORE_CREATE_NEW_FLAG
                    IntPtr.Zero);
                Check(certStore != IntPtr.Zero);

                Check(NativeMethods.CertAddCertificateContextToStore(
                    certStore,
                    certContext,
                    1, // CERT_STORE_ADD_NEW
                    out storeCertContext));

                NativeMethods.CertSetCertificateContextProperty(
                    storeCertContext,
                    2, // CERT_KEY_PROV_INFO_PROP_ID
                    0,
                    ref kpi);

                if (password != null)
                {
                    passwordPtr = Marshal.SecureStringToCoTaskMemUnicode(password);
                }

                var pfxBlob = new CryptoApiBlob();
                Check(NativeMethods.PFXExportCertStoreEx(
                    certStore,
                    ref pfxBlob,
                    passwordPtr,
                    IntPtr.Zero,
                    7)); // EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY

                var pfxData = new Byte[pfxBlob.DataLength];
                dataHandle = GCHandle.Alloc(pfxData, GCHandleType.Pinned);
                pfxBlob.Data = dataHandle.AddrOfPinnedObject();
                Check(NativeMethods.PFXExportCertStoreEx(
                    certStore,
                    ref pfxBlob,
                    passwordPtr,
                    IntPtr.Zero,
                    7)); // EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY
                dataHandle.Free();

                return pfxData;
            }
            finally
            {
                if (passwordPtr != IntPtr.Zero) Marshal.ZeroFreeCoTaskMemUnicode(passwordPtr);

                if (dataHandle.IsAllocated) dataHandle.Free();

                if (certContext != IntPtr.Zero) NativeMethods.CertFreeCertificateContext(certContext);

                if (storeCertContext != IntPtr.Zero) NativeMethods.CertFreeCertificateContext(storeCertContext);

                if (certStore != IntPtr.Zero) NativeMethods.CertCloseStore(certStore, 0);

                if (cryptKey != IntPtr.Zero) NativeMethods.CryptDestroyKey(cryptKey);

                if (providerContext != IntPtr.Zero)
                {
                    NativeMethods.CryptReleaseContext(providerContext, 0);
                    NativeMethods.CryptAcquireContextW(
                        out providerContext,
                        containerName,
                        null,
                        1, // PROV_RSA_FULL
                        0x10); // CRYPT_DELETEKEYSET
                }
            }
        }

        private static SystemTime ToSystemTime(DateTime dateTime)
        {
            var fileTime = dateTime.ToFileTime();
            Check(NativeMethods.FileTimeToSystemTime(ref fileTime, out var systemTime));
            return systemTime;
        }

        private static void Check(Boolean nativeCallSucceeded)
        {
            if (!nativeCallSucceeded)
            {
                var error = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(error);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SystemTime
        {
            public Int16 Year;
            public Int16 Month;
            public Int16 DayOfWeek;
            public Int16 Day;
            public Int16 Hour;
            public Int16 Minute;
            public Int16 Second;
            public Int16 Milliseconds;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CryptoApiBlob
        {
            public Int32 DataLength;
            public IntPtr Data;

            public CryptoApiBlob(Int32 dataLength, IntPtr data)
            {
                DataLength = dataLength;
                Data = data;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CryptKeyProviderInformation
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public String ContainerName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String ProviderName;
            public Int32 ProviderType;
            public Int32 Flags;
            public Int32 ProviderParameterCount;
            public IntPtr ProviderParameters; // PCRYPT_KEY_PROV_PARAM
            public Int32 KeySpec;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean FileTimeToSystemTime(
                [In] ref Int64 fileTime,
                out SystemTime systemTime);

            [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean CryptAcquireContextW(
                out IntPtr providerContext,
                [MarshalAs(UnmanagedType.LPWStr)] String container,
                [MarshalAs(UnmanagedType.LPWStr)] String provider,
                Int32 providerType,
                Int32 flags);

            [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean CryptReleaseContext(
                IntPtr providerContext,
                Int32 flags);

            [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean CryptGenKey(
                IntPtr providerContext,
                Int32 algorithmId,
                Int32 flags,
                out IntPtr cryptKeyHandle);

            [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean CryptDestroyKey(
                IntPtr cryptKeyHandle);

            [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean CertStrToNameW(
                Int32 certificateEncodingType,
                IntPtr x500,
                Int32 strType,
                IntPtr reserved,
                [MarshalAs(UnmanagedType.LPArray)][Out] Byte[] encoded,
                ref Int32 encodedLength,
                out IntPtr errorString);

            [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
            public static extern IntPtr CertCreateSelfSignCertificate(
                IntPtr providerHandle,
                [In] ref CryptoApiBlob subjectIssuerBlob,
                Int32 flags,
                [In] ref CryptKeyProviderInformation keyProviderInformation,
                IntPtr signatureAlgorithm,
                [In] ref SystemTime startTime,
                [In] ref SystemTime endTime,
                IntPtr extensions);

            [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean CertFreeCertificateContext(
                IntPtr certificateContext);

            [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
            public static extern IntPtr CertOpenStore(
                [MarshalAs(UnmanagedType.LPWStr)] String storeProvider,
                Int32 messageAndCertificateEncodingType,
                IntPtr cryptProvHandle,
                Int32 flags,
                IntPtr parameters);

            [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean CertCloseStore(
                IntPtr certificateStoreHandle,
                Int32 flags);

            [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean CertAddCertificateContextToStore(
                IntPtr certificateStoreHandle,
                IntPtr certificateContext,
                Int32 addDisposition,
                out IntPtr storeContextPtr);

            [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean CertSetCertificateContextProperty(
                IntPtr certificateContext,
                Int32 propertyId,
                Int32 flags,
                [In] ref CryptKeyProviderInformation data);

            [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean PFXExportCertStoreEx(
                IntPtr certificateStoreHandle,
                ref CryptoApiBlob pfxBlob,
                IntPtr password,
                IntPtr reserved,
                Int32 flags);
        }
    }
}