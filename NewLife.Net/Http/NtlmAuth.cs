using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace NewLife.Net.Http
{
    /// <summary>NTLM验证</summary>
    [SuppressUnmanagedCodeSecurity]
    sealed class NtlmAuth : IDisposable
    {
        #region 属性
        private string _blob;
        private bool _completed;
        private SecHandle _credentialsHandle;
        private bool _credentialsHandleAcquired;
        private SecBuffer _inputBuffer;
        private SecBufferDesc _inputBufferDesc;
        private SecBuffer _outputBuffer;
        private SecBufferDesc _outputBufferDesc;
        private SecHandle _securityContext;
        private bool _securityContextAcquired;
        private uint _securityContextAttributes;
        private SecurityIdentifier _sid;
        private long _timestamp;
        private const int ISC_REQ_ALLOCATE_MEMORY = 0x100;
        private const int ISC_REQ_CONFIDENTIALITY = 0x10;
        private const int ISC_REQ_DELEGATE = 1;
        private const int ISC_REQ_MUTUAL_AUTH = 2;
        private const int ISC_REQ_PROMPT_FOR_CREDS = 0x40;
        private const int ISC_REQ_REPLAY_DETECT = 4;
        private const int ISC_REQ_SEQUENCE_DETECT = 8;
        private const int ISC_REQ_STANDARD_FLAGS = 20;
        private const int ISC_REQ_USE_SESSION_KEY = 0x20;
        private const int ISC_REQ_USE_SUPPLIED_CREDS = 0x80;
        private const int SEC_E_OK = 0;
        private const int SEC_I_COMPLETE_AND_CONTINUE = 0x90314;
        private const int SEC_I_COMPLETE_NEEDED = 0x90313;
        private const int SEC_I_CONTINUE_NEEDED = 0x90312;
        private const int SECBUFFER_DATA = 1;
        private const int SECBUFFER_EMPTY = 0;
        private const int SECBUFFER_TOKEN = 2;
        private const int SECBUFFER_VERSION = 0;
        private const int SECPKG_CRED_INBOUND = 1;
        private const int SECURITY_NETWORK_DREP = 0;

        public string Blob { get { return _blob; } }

        public bool Completed { get { return _completed; } }

        public SecurityIdentifier SID { get { return _sid; } }
        #endregion

        #region 构造
        public NtlmAuth()
        {
            if (AcquireCredentialsHandle(null, "NTLM", 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref _credentialsHandle, ref _timestamp) != 0) throw new InvalidOperationException();
            _credentialsHandleAcquired = true;
        }

        ~NtlmAuth() { FreeUnmanagedResources(); }

        [DllImport("SECUR32.DLL", CharSet = CharSet.Unicode)]
        private static extern int DeleteSecurityContext(ref SecHandle phContext);

        [DllImport("SECUR32.DLL", CharSet = CharSet.Unicode)]
        private static extern int FreeCredentialsHandle(ref SecHandle phCredential);
        private void FreeUnmanagedResources()
        {
            if (_securityContextAcquired) DeleteSecurityContext(ref _securityContext);
            if (_credentialsHandleAcquired) FreeCredentialsHandle(ref _credentialsHandle);
        }
        void IDisposable.Dispose()
        {
            FreeUnmanagedResources();
            GC.SuppressFinalize(this);
        }
        #endregion

        #region 方法
        public unsafe bool Authenticate(string blobString)
        {
            _blob = null;
            byte[] buffer = Convert.FromBase64String(blobString);
            byte[] inArray = new byte[0x4000];
            fixed (SecHandle* ptrRef = &_securityContext)
            {
                fixed (SecBuffer* ptrRef2 = &_inputBuffer)
                {
                    fixed (SecBuffer* ptrRef3 = &_outputBuffer)
                    {
                        fixed (Byte* ptrRef4 = buffer)
                        {
                            fixed (Byte* ptrRef5 = inArray)
                            {
                                IntPtr zero = IntPtr.Zero;
                                if (_securityContextAcquired) zero = (IntPtr)ptrRef;
                                _inputBufferDesc.ulVersion = 0;
                                _inputBufferDesc.cBuffers = 1;
                                _inputBufferDesc.pBuffers = (IntPtr)ptrRef2;
                                _inputBuffer.cbBuffer = (uint)buffer.Length;
                                _inputBuffer.BufferType = 2;
                                _inputBuffer.pvBuffer = (IntPtr)ptrRef4;
                                _outputBufferDesc.ulVersion = 0;
                                _outputBufferDesc.cBuffers = 1;
                                _outputBufferDesc.pBuffers = (IntPtr)ptrRef3;
                                _outputBuffer.cbBuffer = (uint)inArray.Length;
                                _outputBuffer.BufferType = 2;
                                _outputBuffer.pvBuffer = (IntPtr)ptrRef5;
                                int num = AcceptSecurityContext(ref _credentialsHandle, zero, ref _inputBufferDesc, 20, 0, ref _securityContext, ref _outputBufferDesc, ref _securityContextAttributes, ref _timestamp);
                                if (num == 0x90312)
                                {
                                    _securityContextAcquired = true;
                                    _blob = Convert.ToBase64String(inArray, 0, (int)_outputBuffer.cbBuffer);
                                }
                                else
                                {
                                    if (num != 0) return false;
                                    IntPtr phToken = IntPtr.Zero;
                                    if (QuerySecurityContextToken(ref _securityContext, ref phToken) != 0) return false;
                                    try
                                    {
                                        using (WindowsIdentity identity = new WindowsIdentity(phToken))
                                        {
                                            _sid = identity.User;
                                        }
                                    }
                                    finally
                                    {
                                        CloseHandle(phToken);
                                    }
                                    _completed = true;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        [DllImport("SECUR32.DLL", CharSet = CharSet.Unicode)]
        private static extern int AcceptSecurityContext(ref SecHandle phCredential, IntPtr phContext, ref SecBufferDesc pInput, uint fContextReq, uint TargetDataRep, ref SecHandle phNewContext, ref SecBufferDesc pOutput, ref uint pfContextAttr, ref long ptsTimeStamp);
        [DllImport("SECUR32.DLL", CharSet = CharSet.Unicode)]
        private static extern int AcquireCredentialsHandle(string pszPrincipal, string pszPackage, uint fCredentialUse, IntPtr pvLogonID, IntPtr pAuthData, IntPtr pGetKeyFn, IntPtr pvGetKeyArgument, ref SecHandle phCredential, ref long ptsExpiry);

        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        private static extern int CloseHandle(IntPtr phToken);

        [DllImport("SECUR32.DLL", CharSet = CharSet.Unicode)]
        private static extern int QuerySecurityContextToken(ref SecHandle phContext, ref IntPtr phToken);
        #endregion

        #region 内部结构
        [StructLayout(LayoutKind.Sequential)]
        private struct SecBuffer
        {
            public uint cbBuffer;
            public uint BufferType;
            public IntPtr pvBuffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SecBufferDesc
        {
            public uint ulVersion;
            public uint cBuffers;
            public IntPtr pBuffers;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SecHandle
        {
            public IntPtr dwLower;
            public IntPtr dwUpper;
        }
        #endregion
    }
}